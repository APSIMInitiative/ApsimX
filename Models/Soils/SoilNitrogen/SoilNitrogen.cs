using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Surface;
using Models.Interfaces;

namespace Models.Soils
{
    //------ NewProfile ------
    /// <summary>
    /// strucuture for data associated with the NewProfile event
    /// </summary>
    public class NewProfileType
    {
        /// <summary>
        /// Array of layer depths
        /// </summary>
        public Single[] dlayer;
        /// <summary>
        /// Array of air-dry values
        /// </summary>
        public Single[] air_dry_dep;
        /// <summary>
        /// Array of -15 bar values
        /// </summary>
        public Single[] ll15_dep;
        /// <summary>
        /// Array of drained upper limit values
        /// </summary>
        public Single[] dul_dep;
        /// <summary>
        /// Array of saturated values
        /// </summary>
        public Single[] sat_dep;
        /// <summary>
        /// Array of soil water values
        /// </summary>
        public Single[] sw_dep;
        /// <summary>
        /// Array of bulk density values
        /// </summary>
        public Single[] bd;
    }

    /// <summary>
    /// Delegate for issuing a NewProfile event
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void NewProfileDelegate(NewProfileType Data);
    /// <summary>
    /// Computes the soil C and N processes
    /// </summary>
    /// <remarks>
    /// Implements internal 'patches', which are replicates of state variables and processes used for simulating soil variability
    ///
    /// Based on a more-or-less direct port of the Fortran SoilN model  -  Ported by Eric Zurcher Sept/Oct 2010
    /// Code tidied up by RCichota initially in Aug/Sep-2012 (updates in Feb-Apr/2014, Apr/2015, and Mar-Apr/2016)
    /// Full patch capability ported into ApsimX by Russel McAuliffe in June/2017, tidied up by RCichota (July/2017)
    /// </remarks>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public partial class SoilNitrogen : Model, INutrient
    {

        /// <summary>Initialises a new instance of the <see cref="SoilNitrogen"/> class.</summary>
        public SoilNitrogen()
        {
            Patch = new List<soilCNPatch>();

            soilCNPatch newPatch = new soilCNPatch(this);
            Patch.Add(newPatch);
            Patch[0].RelativeArea = 1.0;
            Patch[0].PatchName = "base";
        }

        #region >>  Events which we publish

        /// <summary>
        /// Event to communicate other modules of C and/or N changes to/from outside the simulation
        /// </summary>
        /// <param name="Data">The data.</param>
        public delegate void ExternalMassFlowDelegate(ExternalMassFlowType Data);

        /// <summary>Occurs when [external mass flow].</summary>
        public event ExternalMassFlowDelegate ExternalMassFlow;

        /// <summary>
        /// Event to comunicate other modules (SurfaceOM) that residues have been decomposed
        /// </summary>
        public delegate void SurfaceOrganicMatterDecompDelegate(SurfaceOrganicMatterDecompType Data);

        #endregion events published

        #region >>  Setup events handlers and methods

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // initialise basic patch values
            Patch[0].PatchName = "base";
            Patch[0].RelativeArea = 1.0;
            Patch[0].CreationDate = Clock.Today;

            // check few initialisation parameters
            CheckParameters();

            // set the size of some arrays
            ResizeLayeredVariables(nLayers);

            // check the initial values of some basic variables
            CheckInitialVariables();

            // set the variables up with their the initial values
            SetInitialValues();
        }

        /// <summary>Reset the state values to those set during the initialisation</summary>
        public void Reset()
        {
            // Save the present C and N status
            StoreStatus();

            // reset the size of arrays
            ResizeLayeredVariables(nLayers);

            // reset C and N variables, i.e. redo initialisation and setup
            SetInitialValues();

            // get the changes of state and publish (let other component to know)
            SendDeltaState();

            mySummary.WriteWarning(this, "Re - setting SoilNitrogen state variables");
        }

        /// <summary>
        /// Checks general initialisation parameters, and let user know of some settings
        /// </summary>
        private void CheckParameters()
        {
            // Get the layering info and set the layer count
            dlayer = Soil.Thickness;
            nLayers = dlayer.Length;

            // get the initial values 
            oc = Soil.Initial.OC;
            FBiom = Soil.FBiom;
            FInert = Soil.FInert;
            HumusCNr = Soil.InitialSoilCNR;
            InitialFOMCNr = Soil.SoilOrganicMatter.FOMCNRatio;
            ph = Soil.Initial.PH;
            NO3ppm = Soil.kgha2ppm(Soil.Initial.NO3N);
            NH4ppm = Soil.kgha2ppm(Soil.Initial.NH4N);
            ureappm = new double[Soil.Thickness.Length];

            // This is needed to initialise values in ApsimX, (they were done in xml file before)
            FOMDecomp_TOptimum = new double[] { 32.0, 32.0 };
            FOMDecomp_TFactorAtZero = new double[] { 0.0, 0.0 };
            FOMDecomp_TCurveCoeff = new double[] { 2.0, 2.0 };
            FOMDecomp_NormWaterContents = new double[] { 0.0, 1.0, 1.5, 2.0, 3.0 };
            FOMDecomp_MoistureFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.5 };

            SOMMiner_TOptimum = new double[] { 32.0, 32.0 };
            SOMMiner_TFactorAtZero = new double[] { 0.0, 0.0 };
            SOMMiner_TCurveCoeff = new double[] { 2.0, 2.0 };
            SOMMiner_NormWaterContents = new double[] { 0.0, 1.0, 1.5, 2.0, 3.0 };
            SOMMiner_MoistureFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.5 };

            UreaHydrol_TOptimum = new double[] { 32.0, 32.0 };
            UreaHydrol_TFactorAtZero = new double[] { 0.2, 0.2 };
            UreaHydrol_TCurveCoeff = new double[] { 1.0, 1.0 };
            UreaHydrol_NormWaterContents = new double[] { 0.0, 1.0, 1.4, 2.4, 3.0 };
            UreaHydrol_MoistureFactors = new double[] { 0.2, 0.2, 1.0, 1.0, 0.7 };

            Nitrification_TOptimum = new double[] { 32.0, 32.0 };
            Nitrification_FactorAtZero = new double[] { 0.0, 0.0 };
            Nitrification_CurveCoeff = new double[] { 2.0, 2.0 };
            Nitrification_NormWaterContents = new double[] { 0.0, 1.0, 1.25, 2.0, 3.0 };
            Nitrification_MoistureFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.0 };
            Nitrification_pHValues = new double[] { 0.0, 4.5, 6.0, 8.0, 9.0, 14.0 };
            Nitrification_pHFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.0, 0.0 };

            Nitrification2_TOptimum = new double[] { 32.0, 32.0 };
            Nitrification2_TFactorAtZero = new double[] { 0.0, 0.0 };
            Nitrification2_TCurveCoeff = new double[] { 2.0, 2.0 };
            Nitrification2_NormWaterContents = new double[] { 0.0, 1.0, 1.25, 2.0, 3.0 };
            Nitrification2_MoistureFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.0 };
            Nitritation_pHValues = new double[] { 0.0, 4.5, 6.0, 8.0, 9.0, 14.0 };
            Nitritation_pHFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.0, 0.0 };

            Codenitrification_TOptmimun = new double[] { 50.05, 50.05 };
            Codenitrification_TFactorAtZero = new double[] { 0.1, 0.1 };
            Codenitrification_TCurveCoeff = new double[] { 1000, 1000 };
            Codenitrification_NormWaterContents = new double[] { 0.0, 2.0, 3.0 };
            Codenitrification_MoistureFactors = new double[] { 0.0, 0.0, 1.0 };
            Codenitrification_pHValues = new double[] { 0.0, 4.5, 6.0, 8.0, 9.0, 14.0 };
            Codenitrification_pHFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.0, 0.0 };
            Codenitrification_NHNOValues = new double[] { 0.0, 4.5, 6.0, 8.0, 9.0, 14.0 };
            Codenitrification_NHNOFactors = new double[] { 0.0, 0.0, 1.0, 1.0, 0.0, 0.0 };

            //// NOTE: the values for Topt and CvExp given here reproduce best the original function (it was an exponential, now a power)
            ////  however, values like 50.06 and 1000, or even more sane 50 and 100 should be good enough
            Denitrification_TOptimum = new double[] { 50.0561976737836, 50.0561976737836 };
            Denitrification_TFactorAtZero = new double[] { 0.1, 0.1 };
            Denitrification_TCurveCoeff = new double[] { 67108874, 67108874 };
            Denitrification_NormWaterContents = new double[] { 0.0, 2.0, 3.0 };
            Denitrification_MoistureFactors = new double[] { 0.0, 0.0, 1.0 };
            Denit_WPFSValues = new double[] { 0.0, 28.0, 88.0, 100.0 };
            Denit_WFPSFactors = new double[] { 0.1, 0.1, 1.0, 1.18 };

            // update few parameters if soil type is Sand (for compatibility with classic apsim)
            if (Soil.SoilType != null && Soil.SoilType.Equals("Sand", StringComparison.CurrentCultureIgnoreCase))
            {
                SoilNParameterSet = "sand";
                MBiomassTurnOverRate = new double[] { 0.0324, 0.015 };
                SOMMiner_MoistureFactors = new double[] { 0.05, 0.05, 1.0, 1.0, 0.5 };
                FOMDecomp_MoistureFactors = new double[] { 0.05, 0.05, 1.0, 1.0, 0.5 };
            }

            // check whether ph was supplied, use a default if not - would it be better to throw an exception?
            if (ph == null)
            {
                ph = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    ph[layer] = DefaultInitialpH;
                mySummary.WriteWarning(this, "Soil pH was not supplied to SoilNitrogen, the default value (" 
                    + DefaultInitialpH.ToString("0.00") + ") will be used for all layers");
            }
            
            // Check whether C:N values have been supplied. If not use average C:N ratio in all pools
            if (fomPoolsCNratio == null || fomPoolsCNratio.Length < 3)
            {
                fomPoolsCNratio = new double[3];
                for (int i = 0; i < 3; i++)
                    fomPoolsCNratio[i] = InitialFOMCNr;
            }

            // Check if initial fom depth has been supplied, if not assume that initial fom is distributed over the whole profile
            if (InitialFOMDepth <= epsilon)
                InitialFOMDepth = SumDoubleArray(dlayer);

            // Check if initial root depth has been supplied, if not use whole profile (used to compute plant available N - patches)
            if (rootDepth <= epsilon)
                rootDepth = SumDoubleArray(dlayer);

            // Calculate conversion factor from kg/ha to ppm (mg/kg)
            convFactor = new double[nLayers];
            for (int layer = 0; layer < nLayers; ++layer)
                convFactor[layer] = MathUtilities.Divide(100.0, Soil.BD[layer] * dlayer[layer], 0.0);

            // Check parameters for patches
            if (DepthToTestByLayer <= epsilon)
                layerDepthToTestDiffs = nLayers - 1;
            else
                layerDepthToTestDiffs = getCumulativeIndex(DepthToTestByLayer, dlayer);
        }

        /// <summary>
        /// Checks whether initial values for OM and mineral N were given and make sure all layers have valid values
        /// </summary>
        /// <remarks>
        /// Initial OC values are mandatory, but not for all layers. Zero is assumed for layers not set.
        /// Initial values for mineral N are optional, assume zero if not given
        /// The inital FOM values are given as a total amount which is distributed using an exponential function.
        /// In this procedure the fraction of total FOM that goes in each layer is also computed
        /// </remarks>
        private void CheckInitialVariables()
        {
            // ensure that array for initial OC have a value for each layer
            if (reset_oc.Length < nLayers)
                mySummary.WriteWarning(this, "Values supplied for the initial OC content do not cover all layers - zeroes will be assumed");
            else if (reset_oc.Length > nLayers)
                mySummary.WriteWarning(this, "More values were supplied for the initial OC content than the number of layers - excess will ignored");

            Array.Resize(ref reset_oc, nLayers);

            // ensure that array for initial urea content have a value for each layer
            if (reset_ureappm == null)
                mySummary.WriteWarning(this, "No values were supplied for the initial content of urea - zero will be assumed");
            else if (reset_ureappm.Length < nLayers)
                mySummary.WriteWarning(this, "Values supplied for the initial content of urea do not cover all layers - zeroes will be assumed");
            else if (reset_ureappm.Length > nLayers)
                mySummary.WriteWarning(this, "More values were supplied for the initial content of urea than the number of layers - excess will ignored");

            Array.Resize(ref reset_ureappm, nLayers);

            // ensure that array for initial content of NH4 have a value for each layer
            if (reset_nh4ppm == null)
                mySummary.WriteWarning(this, "No values were supplied for the initial content of nh4 - zero will be assumed");
            else if (reset_nh4ppm.Length < nLayers)
                mySummary.WriteWarning(this, "Values supplied for the initial content of nh4 do not cover all layers - zeroes will be assumed");
            else if (reset_nh4ppm.Length > nLayers)
                mySummary.WriteWarning(this, "More values were supplied for the initial content of nh4 than the number of layers - excess will ignored");

            Array.Resize(ref reset_nh4ppm, nLayers);

            // ensure that array for initial content of NO3 have a value for each layer
            if (reset_no3ppm == null)
                mySummary.WriteWarning(this, "No values were supplied for the initial content of no3 - zero will be assumed");
            else if (reset_no3ppm.Length < nLayers)
                mySummary.WriteWarning(this, "Values supplied for the initial content of no3 do not cover all layers - zeroes will be assumed");
            else if (reset_no3ppm.Length > nLayers)
                mySummary.WriteWarning(this, "More values were supplied for the initial content of no3 than the number of layers - excess will ignored");

            Array.Resize(ref reset_no3ppm, nLayers);

            // compute initial FOM distribution in the soil (FOM fractions)
            FOMiniFraction = new double[nLayers];
            double totFOMfraction = 0.0;
            int deepestLayer = getCumulativeIndex(InitialFOMDepth, dlayer);
            double cumDepth = 0.0;
            double FracLayer = 0.0;
            for (int layer = 0; layer <= deepestLayer; layer++)
            {
                FracLayer = Math.Min(1.0, MathUtilities.Divide(InitialFOMDepth - cumDepth, dlayer[layer], 0.0));
                cumDepth += dlayer[layer];
                FOMiniFraction[layer] = FracLayer * Math.Exp(-InitialFOMDistCoefficient * Math.Min(1.0, MathUtilities.Divide(cumDepth, InitialFOMDepth, 0.0)));
            }

            // get the actuall FOM distribution through layers (adds up to one)
            totFOMfraction = SumDoubleArray(FOMiniFraction);
            for (int layer = 0; layer <= deepestLayer; layer++)
                FOMiniFraction[layer] /= totFOMfraction;

            // initialise some residue decomposition variables (others are set on DailyInitialisation)
            residueName = new string[1] { "none" };
        }

        /// <summary>
        /// Performs the initial setup and calculations
        /// </summary>
        /// <remarks>
        /// This procedure is also used onReset
        /// </remarks>
        private void SetInitialValues()
        {
            // convert and set C an N values over the profile
            for (int layer = 0; layer < nLayers; layer++)
            {
                // get the initial amounts of mineral N (convert from ppm to kg/ha)
                double iniUrea = MathUtilities.Divide(reset_ureappm[layer], convFactor[layer], 0.0);
                double iniNH4 = MathUtilities.Divide(reset_nh4ppm[layer], convFactor[layer], 0.0);
                double iniNO3 = MathUtilities.Divide(reset_no3ppm[layer], convFactor[layer], 0.0);

                // calculate total soil C
                double Soil_OC = reset_oc[layer] * 10000;                       // = (oc/100)*1000000 - convert from % to ppm
                Soil_OC = MathUtilities.Divide(Soil_OC, convFactor[layer], 0.0);  //Convert from ppm to kg/ha

                // calculate inert soil C
                double InertC = FInert[layer] * Soil_OC;
                double InertN = MathUtilities.Divide(InertC, HumusCNr[layer], 0.0);

                // calculate microbial biomass C and N
                double BiomassC = MathUtilities.Divide((Soil_OC - InertC) * FBiom[layer], 1.0 + FBiom[layer], 0.0);
                double BiomassN = MathUtilities.Divide(BiomassC, MBiomassCNr, 0.0);

                // calculate C and N values for humus
                double HumusC = Soil_OC - BiomassC;
                double HumusN = MathUtilities.Divide(HumusC, HumusCNr[layer], 0.0);

                // distribute C over fom pools
                double[] fomPool = new double[3];
                fomPool[0] = Soil.InitialRootWt[layer] * fract_carb[FOMtypeID_reset] * DefaultCarbonInFOM;
                fomPool[1] = Soil.InitialRootWt[layer] * fract_cell[FOMtypeID_reset] * DefaultCarbonInFOM;
                fomPool[2] = Soil.InitialRootWt[layer] * fract_lign[FOMtypeID_reset] * DefaultCarbonInFOM;

                // set the initial values across patches
                for (int k = 0; k < Patch.Count; k++)
                {
                    Patch[k].urea[layer] = iniUrea;
                    Patch[k].nh4[layer] = iniNH4;
                    Patch[k].no3[layer] = iniNO3;
                    Patch[k].inert_c[layer] = InertC;
                    Patch[k].inert_n[layer] = InertN;
                    Patch[k].biom_c[layer] = BiomassC;
                    Patch[k].biom_n[layer] = BiomassN;
                    Patch[k].hum_c[layer] = HumusC;
                    Patch[k].hum_n[layer] = HumusN;
                    Patch[k].fom_c[0][layer] = fomPool[0];
                    Patch[k].fom_c[1][layer] = fomPool[1];
                    Patch[k].fom_c[2][layer] = fomPool[2];
                    Patch[k].fom_n[0][layer] = MathUtilities.Divide(fomPool[0], fomPoolsCNratio[0], 0.0);
                    Patch[k].fom_n[1][layer] = MathUtilities.Divide(fomPool[1], fomPoolsCNratio[1], 0.0);
                    Patch[k].fom_n[2][layer] = MathUtilities.Divide(fomPool[2], fomPoolsCNratio[2], 0.0);
                }

                // set maximum uptake rates for N forms (only really used for AgPasture when patches exist)
                maximumNH4UptakeRate[layer] = reset_MaximumNH4Uptake / convFactor[layer];
                maximumNO3UptakeRate[layer] = reset_MaximumNO3Uptake / convFactor[layer];
            }

            for (int k = 0; k < Patch.Count; k++)
                Patch[k].CalcTotalMineralNInRootZone();

            initDone = true;

            StoreStatus();
        }

        /// <summary>
        /// Sets the size of arrays (with nLayers)
        /// </summary>
        /// <remarks>
        /// This is used during initialisation and whenever the soil profile changes (thus not often at all)
        /// </remarks>
        /// <param name="nLayers">The number of layers</param>
        private void ResizeLayeredVariables(int nLayers)
        {
            Array.Resize(ref inhibitionFactor_Nitrification, nLayers);
            Array.Resize(ref maximumNH4UptakeRate, nLayers);
            Array.Resize(ref maximumNO3UptakeRate, nLayers);

            for (int k = 0; k < Patch.Count; k++)
                Patch[k].ResizeLayeredVariables(nLayers);
        }

        /// <summary>
        /// Clear (zero out) the values of variables storing deltas
        /// </summary>
        /// <remarks>
        /// This is used to zero out the variables that need resetting every day, those that are not necessarily computed everyday
        /// </remarks>
        private void ClearDeltaVariables()
        {
            //Reset potential decomposition variables
            nResidues = 0;
            Array.Resize(ref pot_c_decomp, 0);
            Array.Resize(ref pot_n_decomp, 0);
            Array.Resize(ref pot_p_decomp, 0);
            // this is also cleared onPotentialResidueDecompositionCalculated, but it is here to ensure it will be reset every timestep

            //Reset variables in each patch
            for (int k = 0; k < Patch.Count; k++)
                Patch[k].ClearDeltaVariables();
        }

        /// <summary>
        /// Store today's initial N amounts
        /// </summary>
        private void StoreStatus()
        {
            TodaysInitialN = SumDoubleArray(TotalN);
            TodaysInitialC = SumDoubleArray(TotalC);

            for (int k = 0; k < Patch.Count; k++)
                Patch[k].StoreStatus();
        }

        /// <summary>
        /// Calculates variations in C an N, and publishes MassFlows to APSIM
        /// </summary>
        private void SendDeltaState()
        {
            double dltN = SumDoubleArray(TotalN) - TodaysInitialN;
            double dltC = SumDoubleArray(TotalC) - TodaysInitialC;

            SendExternalMassFlowN(dltN);
            SendExternalMassFlowC(dltC);
        }

        #endregion setup events

        #region >>  Process events handlers and methods

        #region »   Recurrent processes (each timestep)

        /// <summary>
        /// Sets the procedures for the beginning of each time-step
        /// </summary>
        /// <param name="sender">The sender model.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (initDone)
            {
                // store some initial values, so they may be for mass balance
                StoreStatus();

                // clear variables holding deltas
                ClearDeltaVariables();
            }
        }

        /// <summary>
        /// Sets the procedures for the main phase of each time-step
        /// </summary>
        /// <param name="sender">The sender model.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            // Get potential residue decomposition from SurfaceOrganicMatter
            SurfaceOrganicMatterDecompType SurfaceOrganicMatterDecomp = SurfaceOrganicMatter.PotentialDecomposition();
            nResidues = SurfaceOrganicMatterDecomp.Pool.Length;
            OnPotentialResidueDecompositionCalculated(SurfaceOrganicMatterDecomp);

            // calculate C and N processes
            EvaluateProcesses();
        }

        /// <summary>Stes the procedures for the end of each time-step</summary>
        /// <param name="sender">The sender model.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoUpdate")]
        private void OnDoUpdate(object sender, EventArgs e)
        {
            // Check whether patch auto amalgamation is allowed
            if ((Patch.Count > 1) && (patchAutoAmalgamationAllowed))
            {
                if ((patchAmalgamationApproach.ToLower() == "CompareAll".ToLower()) ||
                    (patchAmalgamationApproach.ToLower() == "CompareBase".ToLower()) ||
                    (patchAmalgamationApproach.ToLower() == "CompareAge".ToLower()) ||
                    (patchAmalgamationApproach.ToLower() == "CompareMerge".ToLower()))
                {
                    CheckPatchAutoAmalgamation();
                }
            }
        }

        /// <summary>Check whether patch amalgamation by age is allowed (done on a monthly basis)</summary>
        /// <param name="sender">The sender model.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            // Check whether patch amalgamation by age is allowed (done on a monthly basis)
            if (AllowPatchAmalgamationByAge.ToLower() == "yes")
                CheckPatchAgeAmalgamation();
        }

        /// <summary>
        /// Performs the soil C and N balance processes, at APSIM timestep.
        /// </summary>
        /// <remarks>
        /// The processes considered, in order, are:
        ///  - Decomposition of surface residues
        ///  - Urea hydrolysis
        ///  - Denitrification + N2O production
        ///  - SOM mineralisation (humus then m. biomass) + decomposition of FOM
        ///  - Nitrification + N2O production
        /// Note: potential surface organic matter decomposition is given by SurfaceOM module, only N balance is considered here
        ///  If there is a pond then surfaceOM is inactive, the decomposition of OM is done wholly by the pond module
        ///  Also, different parameters are used for some processes when pond is active
        /// </remarks>
        private void EvaluateProcesses()
        {
            for (int k = 0; k < Patch.Count; k++)
            {
                // 1. Check surface residues decomposition
                Patch[k].DecomposeResidues();

                // 2. Check urea hydrolysis
                Patch[k].ConvertUrea();

                // 3. Check denitrification
                Patch[k].ConvertNitrate();

                // 4. Check transformations of soil organic matter pools
                Patch[k].ConvertSoilOM();

                // 5. Check nitrification
                Patch[k].ConvertAmmonium();

                // 6. check whether values are ok
                Patch[k].CheckVariables();
            }
        }

        #endregion recurrent processes

        #region »   Sporadic processes (not necessarily every timestep)

        /// <summary>
        /// Passes the information about the potential decomposition of surface residues
        /// </summary>
        /// <remarks>
        /// This information is passed by a residue/SurfaceOM module
        /// </remarks>
        /// <param name="SurfaceOrganicMatterDecomp">Data about the potential decomposition of each residue type on soil surface</param>
        [EventSubscribe("PotentialResidueDecompositionCalculated")]
        private void OnPotentialResidueDecompositionCalculated(SurfaceOrganicMatterDecompType SurfaceOrganicMatterDecomp)
        {
            // zero variables by assigning new array
            residueName = new string[nResidues];
            residueType = new string[nResidues];
            pot_c_decomp = new double[nResidues];
            pot_n_decomp = new double[nResidues];
            pot_p_decomp = new double[nResidues];

            // store potential decomposition into appropriate variables
            for (int residue = 0; residue < nResidues; residue++)
            {
                residueName[residue] = SurfaceOrganicMatterDecomp.Pool[residue].Name;
                residueType[residue] = SurfaceOrganicMatterDecomp.Pool[residue].OrganicMatterType;
                pot_c_decomp[residue] = SurfaceOrganicMatterDecomp.Pool[residue].FOM.C;
                pot_n_decomp[residue] = SurfaceOrganicMatterDecomp.Pool[residue].FOM.N;
                // this P decomposition is needed to formulate data required by SOILP - struth, this is very ugly
                pot_p_decomp[residue] = SurfaceOrganicMatterDecomp.Pool[residue].FOM.P;
            }
        }

        /// <summary>
        /// Sends back to SurfaceOM the information about residue decomposition
        /// </summary>
        /// <remarks>
        /// Potential decomposition was gathered early on from the surfaceOM module. SoilNitrogen evaluated whether the 
        /// conditions (C-N balance) allowed the decomposition to happen, and made the changes in the soil accordingly.
        /// Now the actual decomposition rate for each of the residues is sent back to SurfaceOM.
        /// </remarks>
        public SurfaceOrganicMatterDecompType CalculateActualSOMDecomp()
        {
            // Note:
            //   - If there wasn't enough mineral N to decompose, the rate will be reduced to zero !!  - MUST CHECK THE VALIDITY OF THIS

            SurfaceOrganicMatterDecompType ActualSOMDecomp = new SurfaceOrganicMatterDecompType();
            Array.Resize(ref ActualSOMDecomp.Pool, nResidues);

            for (int residue = 0; residue < nResidues; residue++)
            {
                // get the total amount decomposed over all existing patches
                double c_summed = 0.0;
                double n_summed = 0.0;

                for (int k = 0; k < Patch.Count; k++)
                {
                    c_summed += Patch[k].SurfOMActualDecomposition.Pool[residue].FOM.C * Patch[k].RelativeArea;
                    n_summed += Patch[k].SurfOMActualDecomposition.Pool[residue].FOM.N * Patch[k].RelativeArea;
                }

                // pack up the structure to return decompositions to SurfaceOrganicMatter
                ActualSOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType();
                ActualSOMDecomp.Pool[residue].FOM = new FOMType();
                ActualSOMDecomp.Pool[residue].Name = Patch[0].SurfOMActualDecomposition.Pool[residue].Name;
                ActualSOMDecomp.Pool[residue].OrganicMatterType = Patch[0].SurfOMActualDecomposition.Pool[residue].OrganicMatterType;
                ActualSOMDecomp.Pool[residue].FOM.amount = 0.0F;
                ActualSOMDecomp.Pool[residue].FOM.C = c_summed;
                ActualSOMDecomp.Pool[residue].FOM.N = n_summed;
                ActualSOMDecomp.Pool[residue].FOM.P = 0.0F;
                ActualSOMDecomp.Pool[residue].FOM.AshAlk = 0.0F;
                // Note: The values for 'amount', 'P', and 'AshAlk' will not be collected by SurfaceOrganicMatter, so send zero as default.
            }

            return ActualSOMDecomp;
        }

        /// <summary>
        /// Passes the instructions to incorporate FOM to the soil - simple FOM
        /// </summary>
        /// <remarks>
        /// The use of this events is to be avoided, one should use the method IncorporateFOM
        /// </remarks>
        /// <param name="inFOMdata">Data about the FOM to be added to the soil</param>
        [EventSubscribe("IncorpFOM")]
        private void OnIncorpFOM(FOMLayerType inFOMdata)
        {
            DoIncorpFOM(inFOMdata);
        }

        /// <summary>
        /// Passes the instructions to incorporate FOM to the soil - FOM pools
        /// </summary>
        /// <remarks>
        /// In this event, the FOM amount is given already partitioned by pool
        /// </remarks>
        /// <param name="inFOMPoolData">Data about the FOM to be added to the soil</param>
        [EventSubscribe("IncorpFOMPool")]
        private void OnIncorpFOMPool(FOMPoolType inFOMPoolData)
        {
            DoIncorpFOM(inFOMPoolData);
        }

        /// <summary>
        /// Gets the data and forward instructions to incorporate FOM to the soil - simple FOM
        /// </summary>
        /// <remarks>
        /// The data given here contains FOM as a single amount, not split into pools.
        /// This will be partitioned here based on the given fom_type (or default if not given).
        /// The values for C as well as N (or CN ratio) must be supplied or the action is not performed.
        /// Both C an N are partitioned equally, thus the CN ratios of all pools are assumed equal.
        /// If both N and CN ratio are given, the valu of CN ratio is used.
        /// </remarks>
        /// <param name="inFOMdata">Data about the FOM to be added to the soil</param>
        public void DoIncorpFOM(FOMLayerType inFOMdata)
        {
            // get the total amount to be added
            double totalCAmount = 0.0;
            double totalNAmount = 0.0;
            double amountCnotAdded = 0.0;
            double amountNnotAdded = 0.0;

            for (int layer = 0; layer < inFOMdata.Layer.Length; layer++)
            {
                if (layer < nLayers)
                {
                    if (inFOMdata.Layer[layer].FOM.amount >= epsilon)
                    {
                        inFOMdata.Layer[layer].FOM.C = inFOMdata.Layer[layer].FOM.amount * DefaultCarbonInFOM;
                        if (inFOMdata.Layer[layer].CNR > epsilon)
                        {   // we have C:N info - note that this has precedence over N amount
                            totalCAmount += inFOMdata.Layer[layer].FOM.C;
                            inFOMdata.Layer[layer].FOM.N = inFOMdata.Layer[layer].FOM.C / inFOMdata.Layer[layer].CNR;
                            totalNAmount += inFOMdata.Layer[layer].FOM.N;
                        }
                        else if (inFOMdata.Layer[layer].FOM.N > epsilon)
                        {   // we have N info
                            totalCAmount += inFOMdata.Layer[layer].FOM.C;
                            totalNAmount += inFOMdata.Layer[layer].FOM.N;
                        }
                        else
                        {   // no info for N, C will not be added
                            amountCnotAdded += inFOMdata.Layer[layer].FOM.C;
                        }
                    }
                    else if (inFOMdata.Layer[layer].FOM.N >= epsilon)
                    {   // no info for C, N will not be added
                        amountNnotAdded += inFOMdata.Layer[layer].FOM.N;
                    }
                }
                else
                    mySummary.WriteWarning(this, "Information passed contained more layers than the soil, these will be ignored");
            }

            // If any FOM was passed, make the partition into FOM pools
            if ((totalCAmount >= epsilon) && (totalNAmount >= epsilon))
            {
                // check whether a valid FOM type was given
                fom_type = 0;   // use the default if no fom_type was given
                for (int i = 0; i < fom_types.Length; i++)
                {
                    if (inFOMdata.Type == fom_types[i])
                    {
                        fom_type = i;
                        break;
                    }
                }

                // initialise data pack to hole values of fom
                FOMPoolType myFOMPoolData = new FOMPoolType();
                myFOMPoolData.Layer = new FOMPoolLayerType[inFOMdata.Layer.Length];

                // partition the C and N amounts into FOM pools
                for (int layer = 0; layer < inFOMdata.Layer.Length; layer++)
                {
                    if (layer < nLayers)
                    {
                        myFOMPoolData.Layer[layer] = new FOMPoolLayerType();
                        myFOMPoolData.Layer[layer].Pool = new FOMType[3];
                        myFOMPoolData.Layer[layer].Pool[0] = new FOMType();
                        myFOMPoolData.Layer[layer].Pool[1] = new FOMType();
                        myFOMPoolData.Layer[layer].Pool[2] = new FOMType();

                        if (inFOMdata.Layer[layer].FOM.C > epsilon)
                        {
                            myFOMPoolData.Layer[layer].nh4 = 0.0F;
                            myFOMPoolData.Layer[layer].no3 = 0.0F;
                            myFOMPoolData.Layer[layer].Pool[0].C = inFOMdata.Layer[layer].FOM.amount * DefaultCarbonInFOM * fract_carb[fom_type];
                            myFOMPoolData.Layer[layer].Pool[1].C = inFOMdata.Layer[layer].FOM.amount * DefaultCarbonInFOM * fract_cell[fom_type];
                            myFOMPoolData.Layer[layer].Pool[2].C = inFOMdata.Layer[layer].FOM.amount * DefaultCarbonInFOM * fract_lign[fom_type];

                            myFOMPoolData.Layer[layer].Pool[0].N = inFOMdata.Layer[layer].FOM.N * fract_carb[fom_type];
                            myFOMPoolData.Layer[layer].Pool[1].N = inFOMdata.Layer[layer].FOM.N * fract_cell[fom_type];
                            myFOMPoolData.Layer[layer].Pool[2].N = inFOMdata.Layer[layer].FOM.N * fract_lign[fom_type];
                        }
                    }
                }

                // actually add the FOM to soil
                IncorporateFOM(myFOMPoolData);
            }
            else
            {
                // let the user know of any issues
                string aMessage;

                if (amountCnotAdded >= epsilon)
                    aMessage = "only C amount was given (" + amountCnotAdded.ToString("#0.00") + "kg/ha)";
                else if (amountNnotAdded >= epsilon)
                    aMessage = "only N amount was given (" + amountNnotAdded.ToString("#0.00") + "kg/ha)";
                else
                    aMessage = "no amount was given";

                mySummary.WriteWarning(this, "FOM addition was not carried out because " + aMessage);
            }
        }

        /// <summary>
        /// Gets the data and forward instructions to incorporate FOM to the soil - FOM pools
        /// </summary>
        /// <remarks>
        /// In this event, the FOM amount is given already partitioned into pools
        /// The values for C as well as N must be supplied or the action is not performed.
        /// </remarks>
        /// <param name="inFOMData">Data about the FOM to be added to the soil</param>
        public void DoIncorpFOM(FOMPoolType inFOMData)
        {
            // get the total amount to be added
            double totalCAmount = 0.0;
            double totalNAmount = 0.0;
            double amountCnotAdded = 0.0;
            double amountNnotAdded = 0.0;

            for (int layer = 0; layer < inFOMData.Layer.Length; layer++)
            {
                if (layer < nLayers)
                {
                    for (int pool = 0; pool < 3; pool++)
                    {
                        if (inFOMData.Layer[layer].Pool[pool].C >= epsilon)
                        {   // we have both C and N, can add
                            totalCAmount += inFOMData.Layer[layer].Pool[pool].C;
                            totalNAmount += inFOMData.Layer[layer].Pool[pool].N;
                        }
                        else
                        {   // some data is mising, cannot add
                            amountCnotAdded += inFOMData.Layer[layer].Pool[pool].C;
                            amountNnotAdded += inFOMData.Layer[layer].Pool[pool].N;
                        }
                    }
                }
                else
                    mySummary.WriteMessage(this, " Information passed contained more layers than the soil, these will be ignored");
            }

            // actually add the FOM to soil, if valid
            if ((totalCAmount >= epsilon) && (totalNAmount >= epsilon))
                IncorporateFOM(inFOMData);
            else
            {
                // let the user know of any issues
                string aMessage;

                if (amountCnotAdded >= epsilon)
                    aMessage = "only C amount was given (" + amountCnotAdded.ToString("#0.00") + "kg/ha)";
                else if (amountNnotAdded >= epsilon)
                    aMessage = "only N amount was given (" + amountNnotAdded.ToString("#0.00") + "kg/ha)";
                else
                    aMessage = "no amount was given";

                mySummary.WriteWarning(this, "FOM addition was not carried out because " + aMessage);
            }
        }

        /// <summary>
        /// Gets the data about incoming FOM, add to the patch's FOM pools
        /// </summary>
        /// <remarks>
        /// The FOM amount is given already partitioned by pool
        /// </remarks>
        /// <param name="FOMPoolData"></param>
        public void IncorporateFOM(FOMPoolType FOMPoolData)
        {
            for (int k = 0; k < Patch.Count; k++)
            {
                for (int layer = 0; layer < FOMPoolData.Layer.Length; layer++)
                {
                    // update FOM amounts and check values
                    for (int pool = 0; pool < 3; pool++)
                    {
                        Patch[k].fom_c[pool][layer] += FOMPoolData.Layer[layer].Pool[pool].C;
                        Patch[k].fom_n[pool][layer] += FOMPoolData.Layer[layer].Pool[pool].N;
                        CheckNegativeValues(ref Patch[k].fom_c[pool][layer], layer, "fom_c[" + (pool + 1).ToString() + "]", "Patch[" + Patch[k].PatchName + "].IncorporateFOM");
                        CheckNegativeValues(ref Patch[k].fom_n[pool][layer], layer, "fom_n[" + (pool + 1).ToString() + "]", "Patch[" + Patch[k].PatchName + "].IncorporateFOM");
                    }

                    // update mineral N forms and check values
                    Patch[k].nh4[layer] += FOMPoolData.Layer[layer].nh4;
                    Patch[k].no3[layer] += FOMPoolData.Layer[layer].no3;
                    CheckNegativeValues(ref Patch[k].nh4[layer], layer, "nh4", "Patch[" + Patch[k].PatchName + "].IncorporateFOM");
                    CheckNegativeValues(ref Patch[k].no3[layer], layer, "no3", "Patch[" + Patch[k].PatchName + "].IncorporateFOM");
                }
            }
        }

        /// <summary>Gets the changes in mineral N made by other modules</summary>
        /// <param name="NitrogenChanges">The nitrogen changes.</param>
        public void SetNitrogenChanged(NitrogenChangedType NitrogenChanges)
        {
            OnNitrogenChanged(NitrogenChanges);
        }

        /// <summary>
        /// Passes the information about changes in mineral N made by other modules
        /// </summary>
        /// <remarks>
        /// These values will be passed to each existing patch. Generally the values are passed as they come,
        ///  however, if the deltas come from a soil (i.e. leaching) or plant (i.e. uptake) then the values should
        ///  be handled (partioned).  This will be done based on soil N concentration
        /// </remarks>
        /// <param name="NitrogenChanges">The variation (delta) for each mineral N form</param>
        ///
        [EventSubscribe("NitrogenChanged")]
        private void OnNitrogenChanged(NitrogenChangedType NitrogenChanges)
        {
            // get the type of module sending this change
            senderModule = NitrogenChanges.SenderType.ToLower();

            // get the total amount of N in root zone (for partition of plant uptake)
            for (int k = 0; k < Patch.Count; k++)
                Patch[k].CalcTotalMineralNInRootZone();

            // check whether there are significant values, if so pass them to appropriate dlt
            if (hasSignificantValues(NitrogenChanges.DeltaUrea, epsilon))
            {
                if ((Patch.Count > 1) && ((senderModule == "WaterModule".ToLower()) || (senderModule == "Plant".ToLower())))
                {
                    // the values come from a module that requires partition
                    double[][] newDelta = partitionDelta(NitrogenChanges.DeltaUrea, "Urea", patchNPartitionApproach.ToLower());

                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_urea = newDelta[k];
                }
                else
                {
                    // the values come from a module that do not require partition or there is only one patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_urea = NitrogenChanges.DeltaUrea;
                }
            }
            // else{}  No values, no action needed

            if (hasSignificantValues(NitrogenChanges.DeltaNH4, epsilon))
            {
                if ((Patch.Count > 1) && ((senderModule == "WaterModule".ToLower()) || (senderModule == "Plant".ToLower())))
                {
                    // the values come from a module that requires partition
                    double[][] newDelta = partitionDelta(NitrogenChanges.DeltaNH4, "NH4", patchNPartitionApproach.ToLower());

                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_nh4 = newDelta[k];
                }
                else
                {
                    // the values come from a module that do not require partition or there is only one patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_nh4 = NitrogenChanges.DeltaNH4;
                }
            }
            // else{}  No values, no action needed

            if (hasSignificantValues(NitrogenChanges.DeltaNO3, epsilon))
            {
                if ((Patch.Count > 1) && ((senderModule == "WaterModule".ToLower()) || (senderModule == "Plant".ToLower())))
                {
                    // the values come from a module that requires partition
                    double[][] newDelta = partitionDelta(NitrogenChanges.DeltaNO3, "NO3", patchNPartitionApproach.ToLower());

                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_no3 = newDelta[k];
                }
                else
                {
                    // the values come from a module that do not require partition or there is only one patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_no3 = NitrogenChanges.DeltaNO3;
                }
            }
            // else{}  No values, no action needed
        }

        /// <summary>
        /// Passes and handles the information about new patch and add it to patch list
        /// </summary>
        /// <param name="PatchtoAdd">Patch data</param>
        [EventSubscribe("AddSoilCNPatch")]  // RJM TODO check name
        private void OnAddSoilCNPatch(AddSoilCNPatchType PatchtoAdd)
        {
            // data passed with this event:
            //.Sender: the name of the module that raised this event
            //.SuppressMessages: flags wheter massages are suppressed or not (default is not)
            //.DepositionType: the type of deposition:
            //  - ToAllPaddock: No patch is created, add stuff as given to all patches. It is the default;
            //  - ToSpecificPatch: No patch is created, add stuff to given patches;
            //      (recipient patch is given using its index or name; if not supplied, defaults to homogeneous)
            //  - ToNewPatch: create new patch based on an existing patch, add stuff to created patch;
            //      - recipient or base patch is given using index or name; if not supplied, new patch will be based on the base/Patch[0];
            //      - patches are only created is area is larger than a minimum (minPatchArea);
            //      - new areas are proportional to existing patches;
            //  - NewOverlappingPatches: create new patch(es), these overlap with all existing patches, add stuff to created patches;
            //      (new patches are created only if their area is larger than a minimum (minPatchArea))
            //.AffectedPatches_id (AffectedPatchesByIndex): the index of the existing patches affected by new patch
            //.AffectedPatches_nm (AffectedPatchesByName): the name of the existing patches affected by new patch
            //.AreaFraction: the relative area (fraction) of new patches (0-1)
            //.PatchName: the name(s) of the patch(es) being created
            //.Water: amount of water to add per layer (mm), not handled here
            //.Urea: amount of urea to add per layer (kgN/ha)
            //.NH4: amount of ammonium to add per layer (kgN/ha)
            //.NO3: amount of nitrate to add per layer (kgN/ha)
            //.POX: amount of POx to add per layer (kgP/ha), not handled here
            //.SO4: amount of SO4 to add per layer (kgS/ha), not handled here
            //.Ashalk: ash amount to add per layer (mol/ha), not handled here
            //.FOM_C: amount of carbon in fom (all pools) to add per layer (kgC/ha)  - if present, the entry for pools will be ignored
            //.FOM_C_pool1: amount of carbon in fom_pool1 to add per layer (kgC/ha)
            //.FOM_C_pool2: amount of carbon in fom_pool2 to add per layer (kgC/ha)
            //.FOM_C_pool3: amount of carbon in fom_pool3 to add per layer (kgC/ha)
            //.FOM_N.: amount of nitrogen in fom to add per layer (kgN/ha)

            // - here we'll just convert to AddSoilCNPatchwithFOM and raise that event  - This will be deleted in the future

            AddSoilCNPatchwithFOMType PatchData = new AddSoilCNPatchwithFOMType();
            PatchData.Sender = PatchtoAdd.Sender;
            PatchData.SuppressMessages = PatchtoAdd.SuppressMessages;
            PatchData.DepositionType = PatchtoAdd.DepositionType;
            PatchData.AreaNewPatch = PatchtoAdd.AreaFraction;
            PatchData.AffectedPatches_id = PatchtoAdd.AffectedPatches_id;
            PatchData.AffectedPatches_nm = PatchtoAdd.AffectedPatches_nm;
            PatchData.Urea = PatchtoAdd.Urea;
            PatchData.NH4 = PatchtoAdd.NH4;
            PatchData.NO3 = PatchtoAdd.NO3;
            // need to also initialise FOM, even if it is empty
            PatchData.FOM = new AddSoilCNPatchwithFOMFOMType();
            PatchData.FOM.Type = "none";
            PatchData.FOM.Pool = new SoilOrganicMaterialType[3];

            for (int pool = 0; pool < 3; pool++)
                PatchData.FOM.Pool[pool] = new SoilOrganicMaterialType();

            OnAddSoilCNPatchwithFOM(PatchData);
        }

        /// <summary>
        /// Passes and handles the information about new patch and add it to patch list
        /// </summary>
        /// <param name="PatchtoAdd">Patch data</param>
        [EventSubscribe("AddSoilCNPatchwithFOM")]  // RJM TODO check name
        private void OnAddSoilCNPatchwithFOM(AddSoilCNPatchwithFOMType PatchtoAdd)
        {
            // data passed with this event:
            //.Sender: the name of the module that raised this event
            //.SuppressMessages: flags wheter massages are suppressed or not (default is not)
            //.DepositionType: the type of deposition:
            //  - ToAllPaddock: No patch is created, add stuff as given to all patches. It is the default;
            //  - ToSpecificPatch: No patch is created, add stuff to given patches;
            //      (recipient patch is given using its index or name; if not supplied, defaults to homogeneous)
            //  - ToNewPatch: create new patch based on an existing patch, add stuff to created patch;
            //      - recipient or base patch is given using index or name; if not supplied, new patch will be based on base/Patch[0];
            //      - patches are only created is area is larger than a minimum (minPatchArea);
            //      - new areas are proportional to existing patches;
            //  - NewOverlappingPatches: create new patch(es), these overlap with all existing patches, add stuff to created patches;
            //      (new patches are created only if their area is larger than a minimum (minPatchArea))
            //.AffectedPatches_id (AffectedPatchesByIndex): the index of the existing patches affected by new patch
            //.AffectedPatches_nm (AffectedPatchesByName): the name of the existing patches affected by new patch
            //.AreaNewPatch: the relative area (fraction) of new patches (0-1)
            //.PatchName: the name(s) of the patch(es) being created
            //.Water: amount of water to add per layer (mm), not handled here
            //.Urea: amount of urea to add per layer (kgN/ha)
            //.NH4: amount of ammonium to add per layer (kgN/ha)
            //.NO3: amount of nitrate to add per layer (kgN/ha)
            //.POX: amount of POx to add per layer (kgP/ha), not handled here
            //.SO4: amount of SO4 to add per layer (kgS/ha), not handled here
            //.AshAlk: ash amount to add per layer (mol/ha), not handled here
            //.FOM: fresh organic matter to add, per fom pool
            //   .name: name of given pool being altered
            //   .type: type of the given pool being altered (not used here)
            //   .Pool[]: info about FOM pools being added
            //      .type: type of the given pool being altered (not used here)
            //      .type: type of the given pool being altered (not used here)
            //      .C: amount of carbon in given pool to add per layer (kgC/ha)
            //      .N: amount of nitrogen in given pool to add per layer (kgN/ha)
            //      .P: amount of phosphorus (kgC/ha), not handled here
            //      .S: amount of sulphur (kgC/ha), not handled here
            //      .AshAlk: amount of alkaline ash (kg/ha), not handled here

            // check that required data is supplied
            bool isDataOK = true;

            if (PatchtoAdd.DepositionType.ToLower() == "ToNewPatch".ToLower())
            {
                if (PatchtoAdd.AffectedPatches_id.Length == 0 && PatchtoAdd.AffectedPatches_nm.Length == 0)
                {
                    mySummary.WriteMessage(this, " Command to add patch did not supply a valid patch to be used as base for the new one. Command will be ignored.");
                    isDataOK = false;
                }
                else if (PatchtoAdd.AreaNewPatch <= 0.0)
                {
                    mySummary.WriteMessage(this, " Command to add patch did not supply a valid area fraction for the new patch. Command will be ignored.");
                    isDataOK = false;
                }
            }
            else if (PatchtoAdd.DepositionType.ToLower() == "ToSpecificPatch".ToLower())
            {
                if (PatchtoAdd.AffectedPatches_id.Length == 0 && PatchtoAdd.AffectedPatches_nm.Length == 0)
                {
                    mySummary.WriteMessage(this, " Command to add patch did not supply a valid patch to be used as base for the new one. Command will be ignored.");
                    isDataOK = false;
                }
            }
            else if (PatchtoAdd.DepositionType.ToLower() == "NewOverlappingPatches".ToLower())
            {
                if (PatchtoAdd.AreaNewPatch <= 0.0)
                {
                    mySummary.WriteMessage(this, " Command to add patch did not supply a valid area fraction for the new patch. Command will be ignored.");
                    isDataOK = false;
                }
            }
            else if ((PatchtoAdd.DepositionType.ToLower() == "ToAllPaddock".ToLower()) || (PatchtoAdd.DepositionType == ""))
            {
                // assume stuff is added homogeneously and with no patch creation, thus no factors are actually required
            }
            else
            {
                mySummary.WriteMessage(this, " Command to add patch did not supply a valid DepositionType. Command will be ignored.");
                isDataOK = false;
            }

            if (isDataOK)
            {
                List<int> PatchesToAddStuff;

                if ((PatchtoAdd.DepositionType.ToLower() == "ToNewPatch".ToLower()) ||
                    (PatchtoAdd.DepositionType.ToLower() == "NewOverlappingPatches".ToLower()))
                { // New patch(es) will be added
                    AddNewCNPatch(PatchtoAdd);
                }
                else if (PatchtoAdd.DepositionType.ToLower() == "ToSpecificPatch".ToLower())
                {  // add stuff to selected patches, no new patch will be created

                    // 1. get the list of patch id's to which stuff will be added
                    PatchesToAddStuff = CheckPatchIDs(PatchtoAdd.AffectedPatches_id, PatchtoAdd.AffectedPatches_nm);
                    // 2. add the stuff to patches listed
                    AddStuffToPatches(PatchesToAddStuff, PatchtoAdd);
                }
                else
                {  // add stuff to all existing patches, no new patch will be created
                   // 1. create the list of patches receiving stuff (all)
                    PatchesToAddStuff = new List<int>();
                    for (int k = 0; k < Patch.Count; k++)
                        PatchesToAddStuff.Add(k);
                    // 2. add the stuff to patches listed
                    AddStuffToPatches(PatchesToAddStuff, PatchtoAdd);
                }
            }
        }

        /// <summary>
        /// Passes the list of patches that will be merged into one, as defined by user
        /// </summary>
        /// <param name="MergeCNPatch">The list of CNPatches to merge</param>
        [EventSubscribe("MergeSoilCNPatch")]  // RJM TODO check name
        private void OnMergeSoilCNPatch(MergeSoilCNPatchType MergeCNPatch)
        {
            List<int> PatchesToMerge = new List<int>();

            if (MergeCNPatch.MergeAll)
            {
                // all patches will be merged
                for (int k = 0; k < Patch.Count; k++)
                    PatchesToMerge.Add(k);
            }
            else if ((MergeCNPatch.AffectedPatches_id.Length > 1) | (MergeCNPatch.AffectedPatches_nm.Length > 1))
            {
                // get the list of patch id's to which stuff will be added
                PatchesToMerge = CheckPatchIDs(MergeCNPatch.AffectedPatches_id, MergeCNPatch.AffectedPatches_nm);
            }

            // send the list to merger - all values are copied to first patch in the list, remaining will be deleted
            if (PatchesToMerge.Count > 0)
                AmalgamatePatches(PatchesToMerge, MergeCNPatch.SuppressMessages);
        }
        /// <summary>
        /// Comunicate other components that C amount in the soil has changed
        /// </summary>
        /// <param name="dltC">C changes</param>
        private void SendExternalMassFlowC(double dltC)
        {
            if (ExternalMassFlow != null)
            {
                ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();

                if (Math.Abs(dltC) <= epsilon)
                    dltC = 0.0;
                massBalanceChange.FlowType = dltC >= 0 ? "gain" : "loss";
                massBalanceChange.PoolClass = "soil";
                massBalanceChange.N = Math.Abs(dltC);
                ExternalMassFlow.Invoke(massBalanceChange);
            }
        }

        /// <summary>
        /// Comunicate other components that N amount in the soil has changed
        /// </summary>
        /// <param name="dltN">N changes</param>
        private void SendExternalMassFlowN(double dltN)
        {
            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();

            if (Math.Abs(dltN) < epsilon)
                dltN = 0.0;
            massBalanceChange.FlowType = dltN >= epsilon ? "gain" : "loss";
            massBalanceChange.PoolClass = "soil";
            massBalanceChange.N = Math.Abs(dltN);
            if (ExternalMassFlow != null)
            {
                ExternalMassFlow.Invoke(massBalanceChange);
            }
        }

        #endregion sporadic processes

        #endregion processes events

        #region >>  Auxiliary functions

        /// <summary>
        /// Checks whether the variable is significantly negative, considering thresholds
        /// </summary>
        /// <remarks>
        /// Three levels are considered when analying a negative value, these are defined by the warning and the fatal threshold value:
        ///  (1) If the variable is negative, but the value is really small (in absolute terms) than the deviation is considered irrelevant;
        ///  (2) If the value of the variable is negative and greater than the warning threshold, then a warning message is given;
        ///  (3) If the variable value is negative and greater than the fatal threshold, then a fatal error is raised and the calculation stops.
        /// In any case the value any negative value is reset to zero;
        /// </remarks>
        /// <param name="TheValue">Reference to the variable being tested</param>
        /// <param name="layer">The layer to which the variable belongs to</param>
        /// <param name="VariableName">The name of the variable</param>
        /// <param name="MethodName">The name of the method calling the test</param>
        private void CheckNegativeValues(ref double TheValue, int layer, string VariableName, string MethodName)
        {
            // Note: the layer number and the variable name are passed only so that they can be added to the error message

            if (TheValue < FatalNegativeThreshold)
            {
                // Deviation is too large, stop the calculations
                string myMessage = " - " + MethodName + ", attempt to change " + VariableName + "["
                                 + (layer + 1).ToString() + "] to a value(" + TheValue.ToString()
                                 + ") below the fatal threshold (" + FatalNegativeThreshold.ToString() + ")\n";
                TheValue = 0.0;
                throw new Exception(myMessage);
            }
            else if (TheValue < WarningNegativeThreshold)
            {
                // Deviation is small, but warrants a notice to user
                string myMessage = " - " + MethodName + ", attempt to change " + VariableName + "["
                                 + (layer + 1).ToString() + "] to a value(" + TheValue.ToString()
                                 + ") below the warning threshold (" + WarningNegativeThreshold.ToString()
                                 + ". Value will be reset to zero.";
                TheValue = 0.0;
                mySummary.WriteWarning(this, myMessage);
            }
            else if (TheValue < 0.0)
            {
                // Realy small value, likely a minor numeric issue, don't bother to report
                TheValue = 0.0;
            }
            //else { } // Value is positive
        }

        /// <summary>
        /// Computes the fraction of each layer that is between the surface and a given depth
        /// </summary>
        /// <param name="maxDepth">The depth down to which the fractions are computed</param>
        /// <returns>An array with the fraction (0-1) of each layer that is between the surface and maxDepth</returns>
        private double[] FractionLayer(double maxDepth)
        {
            double cumDepth = 0.0;
            double[] result = new double[nLayers];
            int maxLayer = getCumulativeIndex(maxDepth, dlayer);

            for (int layer = 0; layer <= maxLayer; layer++)
            {
                result[layer] = Math.Min(1.0, MathUtilities.Divide(maxDepth - cumDepth, dlayer[layer], 0.0));
                cumDepth += dlayer[layer];
            }

            return result;
        }

        /// <summary>
        /// Find the index at which the cumulative amount is equal or greater than a given value
        /// </summary>
        /// <param name="sumTarget">The target value being sought</param>
        /// <param name="anArray">The array to analyse</param>
        /// <returns>The index of the array item at which the sum is equal or greater than the target</returns>
        private int getCumulativeIndex(double sumTarget, double[] anArray)
        {
            double cum = 0.0f;

            for (int i = 0; i < anArray.Length; i++)
            {
                cum += anArray[i];
                if (cum >= sumTarget)
                    return i;
            }

            return anArray.Length - 1;
        }

        /// <summary>
        /// Check whether there is at least one considerable/significant value in the array
        /// </summary>
        /// <param name="anArray">The array to analyse</param>
        /// <param name="MinValue">The minimum considerable value</param>
        /// <returns>True if there is any value greater than the minimum, false otherwise</returns>
        private bool hasSignificantValues(double[] anArray, double MinValue)
        {
            bool result = false;

            if (anArray != null)
            {
                for (int i = 0; i < anArray.Length; i++)
                {
                    if (Math.Abs(anArray[i]) >= MinValue)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate the sum of all values of an array of doubles
        /// </summary>
        /// <param name="anArray">The array of values</param>
        /// <returns>The sum</returns>
        private double SumDoubleArray(double[] anArray)
        {
            double result = 0.0;

            if (anArray != null)
            {
                for (int i = 0; i < anArray.Length; i++)
                    result += anArray[i];
            }

            return result;
        }

        /// <summary>Check whether there is any considerable values in the array</summary>
        /// <param name="anArray">The array to analyse</param>
        /// <param name="Lowerue">The minimum considerable value</param>
        /// <returns>True if there is any value greater than the minimum, false otherwise</returns>
        private bool hasValues(double[] anArray, double Lowerue)
        {
            bool result = false;

            if (anArray != null)
            {
                foreach (double Value in anArray)
                {
                    if (Math.Abs(Value) > Lowerue)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        #endregion Aux functions
    }
}