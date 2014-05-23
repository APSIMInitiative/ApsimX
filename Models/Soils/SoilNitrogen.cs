using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using Models.Core;

namespace Models.Soils
{

    [Serializable]
    public class FOMType
    {
        public double amount;
        public double C;
        public double N;
        public double P;
        public double AshAlk;
    }
    [Serializable]
    public class FOMPoolType
    {
        public FOMPoolLayerType[] Layer;
    }
    [Serializable]
    public class FOMPoolLayerType
    {
        public double thickness;
        public double no3;
        public double nh4;
        public double po4;
        public FOMType[] Pool;
    }
    [Serializable]
    public class FOMLayerType
    {
        public string Type = "";
        public FOMLayerLayerType[] Layer;
    }
    [Serializable]
    public class SurfaceOrganicMatterDecompPoolType
    {
        public string Name = "";
        public string OrganicMatterType = "";
        public FOMType FOM;
    }
    [Serializable]
    public class SurfaceOrganicMatterDecompType
    {

        public SurfaceOrganicMatterDecompPoolType[] Pool;
    }


    public struct FOMdecompData
    {
        // lists with values from FOM decompostion
        public double[] dlt_c_hum;
        public double[] dlt_c_biom;
        public double[] dlt_c_atm;
        public double[] dlt_fom_n;
        public double dlt_n_min;
    }
    public class ExternalMassFlowType
    {
        public string PoolClass = "";
        public string FlowType = "";
        public double C;
        public double N;
        public double P;
        public double DM;
        public double SW;
    }
    public class NewSoluteType
    {
        public string OwnerFullPath;
        public string[] solutes;
    }
    public class NitrogenChangedType
    {
        public string Sender = "";
        public string SenderType = "";
        public double[] DeltaNO3;
        public double[] DeltaNH4;
        public double[] DeltaUrea;
    }
    public delegate void NitrogenChangedDelegate(NitrogenChangedType Data);

    public class MergeSoilCNPatchType
    {
        public string Sender = "";
        public string[] AffectedPatches_nm;
        public int[] AffectedPatches_id;
        public bool MergeAll;
    }
    public class FOMLayerLayerType
    {
        public FOMType FOM;
        public double CNR;
        public double LabileP;
    }
    public class AddUrineType
    {
        public double Urinations;
        public double VolumePerUrination;
        public double AreaPerUrination;
        public double Eccentricity;
        public double Urea;
        public double POX;
        public double SO4;
        public double AshAlk;
    }


    /// <summary>
    /// Initially ported from Fortran SoilN model by Eric Zurcher Sept/Oct-2010.
    /// Code tidied up by RCichota on Aug/Sep-2012: mostly modifying how some variables are handled (substitute 'get's by [input]), added regions 
    /// to ease access, updated error messages, moved all soilTemp code to a separate class (the idea is to eliminate it in the future), also added 
    /// some of the constants to xml.
    /// Changes on Sep/Oct-2012 by RCichota, add patch capability: move all code for soil C and N to a separate class (SoilCNPatch), allow several
    /// instances to be initialised, modified inputs to handle the partitioning of incoming N, also modified outputs to sum up the pools from the 
    /// several instances (patches)
    /// </summary>
    [Serializable]
    public partial class SoilNitrogen : Model
    {



        public SoilNitrogen()
        {
            Patch = new List<soilCNPatch>();
            soilCNPatch newPatch = new soilCNPatch(this);
            Patch.Add(newPatch);
            Patch[0].RelativeArea = 1.0;
            Patch[0].PatchName = "base";
            wfpsN2N2O_x = new double[] { 22, 88 };
            wfpsN2N2O_y = new double[] { 0.1, 1 };
        }

        #region Events which we publish

        /// <summary>
        /// Event to communicate other modules of C and/or N changes to/from outside the simulation
        /// </summary>
        public delegate void ExternalMassFlowDelegate(ExternalMassFlowType Data);
        public event ExternalMassFlowDelegate ExternalMassFlow;

        /// <summary>
        /// Event to communicate other modules that solutes have been added to the simulation (owned by SoilNitrogen)
        /// </summary>
        public delegate void NewSoluteDelegate(NewSoluteType Data);
        public event NewSoluteDelegate NewSolute;

        /// <summary>
        /// Event to comunicate other modules (SurfaceOM) that residues have been decomposed
        /// </summary>
        public delegate void SurfaceOrganicMatterDecompDelegate(SurfaceOrganicMatterDecompType Data);
        public event SurfaceOrganicMatterDecompDelegate ActualResidueDecompositionCalculated;

        #endregion

        #region Setup events handlers and methods


        /// <summary>
        /// Performs the initial checks and setup
        /// </summary>
        public override void OnSimulationCommencing()
        {
            Patch = new List<soilCNPatch>();
            soilCNPatch newPatch = new soilCNPatch(this);
            Patch.Add(newPatch);
            Patch[0].RelativeArea = 1.0;
            Patch[0].PatchName = "base";

            // Variable handling when using APSIMX
            initDone = false;
            dlayer = Soil.Thickness;
            bd = Soil.Water.BD;
            sat_dep = Utility.Math.Multiply(Soil.Water.SAT, Soil.Thickness);
            dul_dep = Utility.Math.Multiply(Soil.Water.DUL, Soil.Thickness);
            ll15_dep = Utility.Math.Multiply(Soil.Water.LL15, Soil.Thickness);
            sw_dep = Utility.Math.Multiply(Soil.SW, Soil.Thickness);
            oc = Soil.OC;
            ph = Soil.Analysis.PH;
            salb = Soil.SoilWater.Salb;
            no3ppm = Soil.NO3;
            nh4ppm = Soil.NH4;
            num_residues = 0;
            Tsoil = null;
            simpleST = null;

            fbiom = SoilOrganicMatter.FBiom;
            finert = SoilOrganicMatter.FInert;
            soil_cn = SoilOrganicMatter.SoilCN;
            root_wt = SoilOrganicMatter.RootWt;
            root_cn = SoilOrganicMatter.RootCN;
            enr_a_coeff = SoilOrganicMatter.EnrACoeff;
            enr_b_coeff = SoilOrganicMatter.EnrBCoeff;

            initDone = true;

            // set the size of arrays
            ResizeLayerArrays(dlayer.Length);
            foreach (soilCNPatch aPatch in Patch)
                aPatch.ResizeLayerArrays(dlayer.Length);

            // check few initialisation parameters
            CheckParams();

            // perform initial calculations and setup
            InitCalc();

            // notifify apsim about solutes
            AdvertiseMySolutes();
        }

        /// <summary>
        /// Reset the state values to those set during the initialisation
        /// </summary>
        public void Reset()
        {

            inReset = true;

            // Save present state
            SaveState();

            // reset the size of arrays - so it zeroes them
            ResizeLayerArrays(dlayer.Length);

            // reset patches
            Patch.Clear();
            soilCNPatch newPatch = new soilCNPatch(this);
            Patch.Add(newPatch);

            foreach (soilCNPatch aPatch in Patch)
                aPatch.ResizeLayerArrays(dlayer.Length);

            // reset C and N variables to their initial state
            oc = OC_reset;
            no3ppm = no3ppm_reset;
            nh4ppm = nh4ppm_reset;
            ureappm = ureappm_reset;

            // perform initial calculations and setup
            InitCalc();

            // reset soil temperature
            if (!use_external_st)
            {
                simpleST = new simpleSoilTemp(MetFile.Latitude, MetFile.Tav, MetFile.Amp, MetFile.MinT, MetFile.MaxT);
                Tsoil = simpleST.SoilTemperature(Clock.Today, MetFile.MinT, MetFile.MaxT, MetFile.Radn, salb, dlayer, bd, ll15_dep, sw_dep);
            }

            // get the changes of state and publish (let other component to know)
            DeltaState();

            inReset = false;
        }

        /// <summary>
        /// Check general initialisation parameters, and let user know of some settings
        /// </summary>
        private void CheckParams()
        {

            SoilCNParameterSet = SoilCNParameterSet.Trim();
            NPartitionApproach = NPartitionApproach.Trim();

            // check whether soil temperature is present. If not, check whether the basic params for simpleSoilTemp have been supplied
            if (AllowsimpleSoilTemp)
                use_external_st = (ave_soil_temp != null);

            // check whether ph is supplied, use a default if not - might be better to throw an exception?
            use_external_ph = (ph != null);
            if (!use_external_ph)
            {
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    ph[layer] = 6.0; // ph_ini
            }

            // convert minimum values for nh4 and no3 from ppm to kg/ha
            double convFact = 0;
            for (int layer = 0; layer < dlayer.Length; ++layer)
            {
                convFact = convFactor_kgha2ppm(layer);
                urea_min[layer] = Utility.Math.Divide(ureappm_min, convFact, 0.0);
                nh4_min[layer] = Utility.Math.Divide(nh4ppm_min, convFact, 0.0);
                no3_min[layer] = Utility.Math.Divide(no3ppm_min, convFact, 0.0);
            }

            // Check if all fom values have been supplied
            if (num_fom_types != fract_carb.Length)
                throw new Exception("Number of \"fract_carb\" different to \"fom_type\"");
            if (num_fom_types != fract_cell.Length)
                throw new Exception("Number of \"fract_cell\" different to \"fom_type\"");
            if (num_fom_types != fract_lign.Length)
                throw new Exception("Number of \"fract_lign\" different to \"fom_type\"");

            // Check if all C:N values have been supplied. If not use average C:N ratio in all pools
            if (fomPoolsCNratio == null || fomPoolsCNratio.Length < 3)
            {
                fomPoolsCNratio = new double[3];
                for (int i = 0; i < 3; i++)
                    fomPoolsCNratio[i] = iniFomCNratio;
            }

            // Check if initial fom depth has been supplied, if not assume that initial fom is distributed over the whole profile
            if (iniFomDepth == 0.0)
            {
                for (int i = 0; i < dlayer.Length; ++i)
                    iniFomDepth += dlayer[i];
            }
        }

        /// <summary>
        /// Do the initial setup and calculations - this is also used onReset
        /// </summary>
        private void InitCalc()
        {

            int nLayers = dlayer.Length;

            // Factor to distribute fom over the soil profile. Uses a exponential function and goes till the especified depth
            double[] fom_FracLayer = new double[nLayers];
            double cum_depth = 0.0;
            int deepest_layer = getCumulativeIndex(iniFomDepth, dlayer);
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                fom_FracLayer[layer] = Math.Exp(-3.0 * Math.Min(1.0, Utility.Math.Divide(cum_depth + dlayer[layer], iniFomDepth, 0.0))) *
                    Math.Min(1.0, Utility.Math.Divide(iniFomDepth - cum_depth, dlayer[layer], 0.0));
                cum_depth += dlayer[layer];
            }
            double fom_FracLayer_tot = SumDoubleArray(fom_FracLayer);

            // ensure initial OC has a value for each layer
            Array.Resize(ref OC_reset, nLayers);

            // Distribute an convert C an N values over the profile
            double convFact = 0.0;
            double newValue = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                convFact = convFactor_kgha2ppm(layer);
                // check and distribute the mineral nitrogen
                if (ureappm_reset != null)
                {
                    newValue = Utility.Math.Divide(ureappm_reset[layer], convFact, 0.0);       //Convert from ppm to kg/ha
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].urea[layer] = newValue;
                }
                newValue = Utility.Math.Divide(nh4ppm_reset[layer], convFact, 0.0);       //Convert from ppm to kg/ha
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].nh4[layer] = newValue;
                newValue = Utility.Math.Divide(no3ppm_reset[layer], convFact, 0.0);       //Convert from ppm to kg/ha
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].no3[layer] = newValue;

                // calculate total soil C
                double Soil_OC = OC_reset[layer] * 10000;					// = (oc/100)*1000000 - convert from % to ppm
                Soil_OC = Utility.Math.Divide(Soil_OC, convFact, 0.0);		//Convert from ppm to kg/ha

                // calculate inert soil C
                double InertC = finert[layer] * Soil_OC;

                // calculate microbial biomass C and N
                double BiomassC = Utility.Math.Divide((Soil_OC - InertC) * fbiom[layer], 1.0 + fbiom[layer], 0.0);
                double BiomassN = Utility.Math.Divide(BiomassC, biom_cn, 0.0);

                // calculate C and N values for active humus
                double HumusC = Soil_OC - BiomassC;
                double HumusN = Utility.Math.Divide(HumusC, hum_cn, 0.0);

                // distribute and calculate the fom N and C
                double fom = Utility.Math.Divide(iniFomWt * fom_FracLayer[layer], fom_FracLayer_tot, 0.0);

                for (int k = 0; k < Patch.Count; k++)
                {
                    Patch[k].inert_c[layer] = InertC;
                    Patch[k].biom_c[layer] = BiomassC;
                    Patch[k].biom_n[layer] = BiomassN;
                    Patch[k].hum_c[layer] = HumusC;
                    Patch[k].hum_n[layer] = HumusN;
                    Patch[k].fom_c_pool1[layer] = fom * fract_carb[0] * c_in_fom;
                    Patch[k].fom_c_pool2[layer] = fom * fract_cell[0] * c_in_fom;
                    Patch[k].fom_c_pool3[layer] = fom * fract_lign[0] * c_in_fom;
                    Patch[k].fom_n_pool1[layer] = Utility.Math.Divide(Patch[k].fom_c_pool1[layer], fomPoolsCNratio[0], 0.0);
                    Patch[k].fom_n_pool2[layer] = Utility.Math.Divide(Patch[k].fom_c_pool2[layer], fomPoolsCNratio[1], 0.0);
                    Patch[k].fom_n_pool3[layer] = Utility.Math.Divide(Patch[k].fom_c_pool3[layer], fomPoolsCNratio[2], 0.0);
                }

                // store today's values
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].InitCalc();
            }

            // Calculations for NEW sysbal component
            dailyInitialC = SumDoubleArray(carbon_tot);
            dailyInitialN = SumDoubleArray(nit_tot);

            // Initialise the inhibitor factors
            if (InhibitionFactor_Nitrification == null)
                InhibitionFactor_Nitrification = new double[dlayer.Length];

            initDone = true;
        }

        /// <summary>
        /// Set the size of all public arrays (with nLayers)
        /// </summary>
        /// <param name="nLayers"></param>
        private void ResizeLayerArrays(int nLayers)
        {
            // Note: this doesn't clear the existing values

            Array.Resize(ref Tsoil, nLayers);
            Array.Resize(ref urea_min, nLayers);
            Array.Resize(ref nh4_min, nLayers);
            Array.Resize(ref no3_min, nLayers);
        }

        /// <summary>
        /// Notify any interested module about this module's ownership of solute information.
        /// </summary>
        private void AdvertiseMySolutes()
        {

            if (NewSolute != null)
            {
                string[] solute_names;
                if (useOrganicSolutes)
                {
                    solute_names = new string[7] { "urea", "nh4", "no3", "org_c_pool1", "org_c_pool2", "org_c_pool3", "org_n" };
                }
                else
                { // don't publish the organic solutes
                    solute_names = new string[3] { "urea", "nh4", "no3" };
                }

                NewSoluteType SoluteData = new NewSoluteType();
                SoluteData.OwnerFullPath = FullPath;
                SoluteData.solutes = solute_names;

                NewSolute.Invoke(SoluteData);
            }
        }

        /// <summary>
        /// Stores the total amounts of C an N
        /// </summary>
        private void SaveState()
        {
            // +  Note: needed for both NEW and OLD sysbal component

            dailyInitialN = SumDoubleArray(nit_tot);
            dailyInitialC = SumDoubleArray(carbon_tot);
        }

        /// <summary>
        /// Calculates variations in C an N, and publishes MassFlows to APSIM
        /// </summary>
        private void DeltaState()
        {

            double dltN = SumDoubleArray(nit_tot) - dailyInitialN;
            double dltC = SumDoubleArray(carbon_tot) - dailyInitialC;

            SendExternalMassFlowN(dltN);
            SendExternalMassFlowC(dltC);
        }

        #endregion

        #region Process events handlers and methods

        #region Daily processes

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        [EventSubscribe("PotentialResidueDecompositionCalculated")]
        private void OnPotentialResidueDecompositionCalculated(SurfaceOrganicMatterDecompType SurfaceOrganicMatterDecomp)
        {
            foreach (soilCNPatch aPatch in Patch)
                aPatch.OnPotentialResidueDecompositionCalculated(SurfaceOrganicMatterDecomp);

            num_residues = SurfaceOrganicMatterDecomp.Pool.Length;

            sw_dep = Soil.SoilWater.sw_dep;

            // update soil temperature
            if (use_external_st)
                Tsoil = ave_soil_temp;
            else
            {
                // initialise soil temperature
                if (simpleST == null)
                    simpleST = new simpleSoilTemp(MetFile.Latitude, MetFile.Tav, MetFile.Amp, MetFile.MinT, MetFile.MaxT);

                Tsoil = simpleST.SoilTemperature(Clock.Today, MetFile.MinT, MetFile.MaxT, MetFile.Radn, salb, dlayer, bd, ll15_dep, sw_dep);
            }

            // calculate C and N processes
            //    - Assesses potential decomposition of surface residues;
            //		. adjust decomposition if needed;
            //		. accounts for mineralisation/immobilisation of N;
            //	  - Compute the transformations on soil organic matter (including N mineralisation/immobilition);
            //    - Calculates hydrolysis of urea, nitrification, and denitrification;
            for (int k = 0; k < Patch.Count; k++)
                Patch[k].Process();

            // send actual decomposition back to surface OM
            if (!is_pond_active)
                SendActualResidueDecompositionCalculated();
        }

        /// <summary>
        /// Performs every-day calculations - before begining of day tasks 
        /// </summary>
        /// <param name="time">Today's time</param>
        [EventSubscribe("Tick")]
        private void OnTick(object sender, EventArgs e)
        {
            // + Purpose: reset potential decomposition variables in each patch and get C and N status

            foreach (soilCNPatch aPatch in Patch)
                aPatch.OnTick();

            // Calculations for NEW sysbal component
            SaveState();
        }

        /// <summary>
        /// Performs every-day calculations - end of day processes
        /// </summary>
        [EventSubscribe("EndOfDay")]
        private void OnPost(object sender, EventArgs e)
        {
            // + Purpose: Check patch status and clean up, if possible

            if (Patch.Count > 1000) // must set this to one later
            {
                // we have more than one patch, check whether they are similar enough to be merged
                PatchIDs Patches = new PatchIDs();
                Patches = ComparePatches();

                if (Patches.disappearing.Count > 0)
                {  // there are patches that will be merged
                    for (int k = 0; k < Patches.disappearing.Count; k++)
                    {
                        MergePatches(Patches.recipient[k], Patches.disappearing[k]);
                        Summary.WriteMessage(FullPath, "   merging Patch(" + Patches.disappearing[k].ToString() + ") into Patch(" +
                            Patches.recipient[k].ToString() + "). New patch area = " + Patch[Patches.recipient[k]].RelativeArea.ToString("#0.00#"));
                    }
                }
            }
        }

        /// <summary>
        /// Send back to SurfaceOM the information about residue decomposition
        /// </summary>
        private void SendActualResidueDecompositionCalculated()
        {
            // Note:
            //      Potential decomposition was given to this module by a residue/surfaceOM module. 
            //		Now we explicitly tell the module the actual decomposition
            //      rate for each of its residues.  If there wasn't enough mineral N to decompose, the rate will be reduced from the potential value.

            if (ActualResidueDecompositionCalculated != null)
            {
                // will have to pack the SOMdecomp data from each patch and then invoke the event
                //int num_residues = Patch[0].SOMDecomp.Pool.Length;
                int nLayers = dlayer.Length;

                SurfaceOrganicMatterDecompType SOMDecomp = new SurfaceOrganicMatterDecompType();
                Array.Resize(ref SOMDecomp.Pool, num_residues);

                for (int residue = 0; residue < num_residues; residue++)
                {
                    double c_summed = 0.0F;
                    double n_summed = 0.0F;
                    for (int k = 0; k < Patch.Count; k++)
                    {
                        c_summed += Patch[k].SOMDecomp.Pool[residue].FOM.C * Patch[k].RelativeArea;
                        n_summed += Patch[k].SOMDecomp.Pool[residue].FOM.N * Patch[k].RelativeArea;
                    }

                    SOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType();
                    SOMDecomp.Pool[residue].FOM = new FOMType();
                    SOMDecomp.Pool[residue].Name = Patch[0].SOMDecomp.Pool[residue].Name;
                    SOMDecomp.Pool[residue].OrganicMatterType = Patch[0].SOMDecomp.Pool[residue].OrganicMatterType;
                    SOMDecomp.Pool[residue].FOM.amount = 0.0F;
                    SOMDecomp.Pool[residue].FOM.C = c_summed;
                    SOMDecomp.Pool[residue].FOM.N = n_summed;
                    SOMDecomp.Pool[residue].FOM.P = 0.0F;
                    SOMDecomp.Pool[residue].FOM.AshAlk = 0.0F;
                }

                // send the decomposition information
                ActualResidueDecompositionCalculated.Invoke(SOMDecomp);
            }
        }

        #endregion

        #region Frequent and sporadic processes

        /// <summary>
        /// Partition the given FOM C and N into fractions in each layer (one FOM)
        /// </summary>
        [EventSubscribe("IncorpFOM")]
        private void OnIncorpFOM(FOMLayerType inFOMdata)
        {
            // Note: In this event all FOM is given as one, so it will be assumed that the CN ratios of all fractions are equal

            foreach (soilCNPatch aPatch in Patch)
                aPatch.OnIncorpFOM(inFOMdata);

            fom_type = Patch[0].fom_type;
        }

        /// <summary>
        /// Partition the given FOM C and N into fractions in each layer (FOM pools)
        /// </summary>
        [EventSubscribe("IncorpFOMPool")]
        private void OnIncorpFOMPool(FOMPoolType inFOMPoolData)
        {
            // Note: In this event each of the three pools is given

            foreach (soilCNPatch aPatch in Patch)
                aPatch.OnIncorpFOMPool(inFOMPoolData);
        }



        /// <summary>
        /// Get information about changes in soil profile  (primarily due to erosion)
        /// </summary>
        [EventSubscribe("NewProfile")]
        private void OnNew_profile(NewProfileType NewProfile)
        {
            // Note: are the changes maily (only) due to by erosion? what else??

            // check whether the basic soil parameters are of the right size
            int NewNumLayers = NewProfile.dlayer.Length;
            if (dlayer == null || NewProfile.dlayer.Length != dlayer.Length)
            {
                Array.Resize(ref bd, NewNumLayers);
                Array.Resize(ref sat_dep, NewNumLayers);
                Array.Resize(ref dul_dep, NewNumLayers);
                Array.Resize(ref ll15_dep, NewNumLayers);
                Array.Resize(ref sw_dep, NewNumLayers);
            }

            // assign new values to soil parameters
            double[] new_dlayer = new double[NewNumLayers];
            for (int layer = 0; layer < NewNumLayers; layer++)
            {
                new_dlayer[layer] = (double)NewProfile.dlayer[layer];
                bd[layer] = (double)NewProfile.bd[layer];
                sat_dep[layer] = (double)NewProfile.dul_dep[layer];
                dul_dep[layer] = (double)NewProfile.dul_dep[layer];
                ll15_dep[layer] = (double)NewProfile.ll15_dep[layer];
                sw_dep[layer] = (double)NewProfile.sw_dep[layer];
            }

            // check any variation in the soil C and N properties due to changes in soil profile
            if (soil_loss > 0.0 && AllowProfileReduction)
            {
                foreach (soilCNPatch aPatch in Patch)
                    aPatch.CheckProfile(new_dlayer);
            }

            // reset dlayer
            if (dlayer == null || new_dlayer.Length != dlayer.Length)
                ResizeLayerArrays(new_dlayer.Length);
            Array.Resize(ref dlayer, NewNumLayers);
            for (int layer = 0; layer < NewNumLayers; layer++)
                dlayer[layer] = new_dlayer[layer];
        }

        /// <summary>
        /// Gets the changes in mineral N made by other modules
        /// </summary>
        [EventSubscribe("NitrogenChanged")]
        private void OnNitrogenChanged(NitrogenChangedType NitrogenChanges)
        {
            // Note:
            //     Send deltas to each patch, if delta comes from soil or plant then the values are modified (partioned)
            //      based on N content. If sender is any other module then values are passed to patches as they come

            string module = NitrogenChanges.SenderType.ToLower();
            if ((Patch.Count > 1) && (module == "WaterModule".ToLower()) || (module == "Plant".ToLower()))
            {
                // values supplied by a module from which a different treatment for each patch is required,
                //  they will be partitioned according to the N content in each patch, following:
                //  - If module is Plant (uptake): partition is based on the relative concentration, at each layer, of all patches
                //  - If module is WaterModule (leaching):
                //        . if is removal (negative): partition is equal to a plant uptake
                //        . if is incoming leaching: partition is based on relative concentration on the layer and above

                // 1- consider urea:
                if (hasValues(NitrogenChanges.DeltaUrea, EPSILON))
                {
                    // 1.1-send incoming dlt to be partitioned amongst patches
                    double[][] newDelta = partitionDelta(NitrogenChanges.DeltaUrea, "urea", NPartitionApproach.ToLower());
                    // 1.2- send dlt's to each patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_urea = newDelta[k];
                }

                // 2- consider nh4:
                if (hasValues(NitrogenChanges.DeltaNH4, EPSILON))
                {
                    // 2.1- send incoming dlt to be partitioned amongst patches
                    double[][] newDelta = partitionDelta(NitrogenChanges.DeltaNH4, "nh4", NPartitionApproach.ToLower());
                    // 2.2- send dlt's to each patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_nh4 = newDelta[k];
                }

                // 3- consider no3:
                if (hasValues(NitrogenChanges.DeltaNO3, EPSILON))
                {
                    // 3.1- send incoming dlt to be partitioned amongst patches
                    double[][] newDelta = partitionDelta(NitrogenChanges.DeltaNO3, "no3", NPartitionApproach.ToLower());
                    // 3.2- send dlt's to each patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_no3 = newDelta[k];
                }
            }
            else
            {
                // values will passed to patches as they come
                for (int k = 0; k < Patch.Count; k++)
                {
                    Patch[k].dlt_urea = NitrogenChanges.DeltaUrea;
                    Patch[k].dlt_nh4 = NitrogenChanges.DeltaNH4;
                    Patch[k].dlt_no3 = NitrogenChanges.DeltaNO3;
                }
            }
        }

        /// <summary>
        /// Get the information about urine being added
        /// </summary>
        /// <param name="UrineAdded">Urine deposition data (includes urea N amount, volume, area affected, etc)</param>
        [EventSubscribe("AddUrine")]
        private void OnAddUrine(AddUrineType UrineAdded)
        {

            // Starting with the minimalist version. To be updated by Val's group to include a urine patch algorithm

            // test for adding urine patches  -RCichota
            // if VolumePerUrination = 0.0 then no patch will be added, otherwise a patch will be added (based on 'base' patch)
            // assuming new PatchArea is passed as a fraction and this will be subtracted from original
            // urea will be added to the top layer for now

            double[] newUrea = new double[dlayer.Length];
            newUrea[0] = UrineAdded.Urea;

            if (UrineAdded.VolumePerUrination > 0.0)
            {
                SplitPatch(0);
                double oldArea = Patch[0].RelativeArea;
                double newArea = oldArea * (1 - UrineAdded.AreaPerUrination);
                Patch[0].RelativeArea = newArea;
                int k = Patch.Count - 1;
                Patch[k].RelativeArea = oldArea * UrineAdded.AreaPerUrination;
                Patch[k].PatchName = "Patch" + k.ToString();
                if (UrineAdded.Urea > EPSILON)
                    Patch[k].dlt_urea = newUrea;
            }
            else
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_urea = newUrea;
        }

        /// <summary>
        /// Gets and handles the information about new patch and add it to patch list
        /// </summary>
        /// <param name="PatchtoAdd">Patch data</param>
        [EventSubscribe("AddSoilCNPatc")]
        private void OnAddSoilCNPatch(AddSoilCNPatchType PatchtoAdd)
        {
            // data passed with this event:
            //.Sender: the name of the module that raised this event
            //.DepositionType: the type of deposition:
            //  - ToAllPaddock: No patch is created, add stuff as given to all patches. It is the default;
            //  - ToSpecificPatch: No patch is created, add stuff to given patches;
            //		(recipient patch is given using its index or name; if not supplied, defaults to homogeneous)
            //  - ToNewPatch: create new patch based on an existing patch, add stuff to created patch;
            //		- recipient or base patch is given using index or name; if not supplied, new patch will be based on the base/Patch[0];
            //      - patches are only created is area is larger than a minimum (minPatchArea);
            //      - new areas are proportional to existing patches;
            //  - NewOverlappingPatches: create new patch(es), these overlap with all existing patches, add stuff to created patches;
            //		(new patches are created only if their area is larger than a minimum (minPatchArea))
            //.AffectedPatches_id (AffectedPatchesByIndex): the index of the existing patches to which urine will be added
            //.AffectedPatches_nm (AffectedPatchesByName): the name of the existing patches to which urine will be added
            //.AreaFraction: the relative area of the patch (0-1)
            //.PatchName: the name(s) of the patch)es) being created
            //.Water: amount of water to add per layer (mm), not handled here
            //.Urea: amount of urea to add per layer (kgN/ha)
            //.Urea: amount of urea to add (per layer) - Do we need other N forms?
            //.NH4: amount of ammonium to add per layer (kgN/ha)
            //.NO3: amount of nitrate to add per layer (kgN/ha)
            //.POX: amount of POx to add per layer (kgP/ha)
            //.SO4: amount of SO4 to add per layer (kgS/ha)
            //.Ashalk: ash amount to add per layer (mol/ha)
            //.FOM_C: amount of carbon in fom (all pools) to add per layer (kgC/ha)  - if present, the entry for pools will be ignored
            //.FOM_C_pool1: amount of carbon in fom_pool1 to add per layer (kgC/ha)
            //.FOM_C_pool2: amount of carbon in fom_pool2 to add per layer (kgC/ha)
            //.FOM_C_pool3: amount of carbon in fom_pool3 to add per layer (kgC/ha)
            //.FOM_N.: amount of nitrogen in fom to add per layer (kgN/ha)

            List<int> PatchesToAddStuff = new List<int>();

            if ((PatchtoAdd.DepositionType.ToLower() == "ToNewPatch".ToLower()) ||
                (PatchtoAdd.DepositionType.ToLower() == "NewOverlappingPatches".ToLower()))
            { // New patch(es) will be added
                AddNewCNPatch(PatchtoAdd);
            }
            else if (PatchtoAdd.DepositionType.ToLower() == "ToSpecificPatch".ToLower())
            {  // add stuff to selected patches, no new patch will be created

                // 1. get the list of patch id's to which stuff will be added
                int[] PatchIDs = CheckPatchIDs(PatchtoAdd.AffectedPatches_id, PatchtoAdd.AffectedPatches_nm);
                // 2. create the list of patches receiving stuff
                for (int i = 0; i < PatchIDs.Length; i++)
                    PatchesToAddStuff.Add(PatchIDs[i]);
                // 3. add the stuff to patches listed
                AddStuffToPatches(PatchesToAddStuff, PatchtoAdd);
            }
            else
            {  // add urine to all existing patches, no new patch will be created

                // 1. create the list of patches receiving stuff (all)
                for (int k = 0; k < Patch.Count; k++)
                    PatchesToAddStuff.Add(k);
                // 2. add the stuff to patches listed
                AddStuffToPatches(PatchesToAddStuff, PatchtoAdd);
            }
        }


        /// <summary>
        /// Gets the list of patches that will be merge into one, as defined by user
        /// </summary>
        /// <param name="MergeCNPatch">The list of CNPatches to merge</param>
        [EventSubscribe("MergeSoilCNPatc")]
        private void OnMergeSoilCNPatch(MergeSoilCNPatchType MergeCNPatch)
        {
            if ((MergeCNPatch.AffectedPatches_id.Length > 1) | (MergeCNPatch.AffectedPatches_nm.Length > 1))
            {
                // get the list of patch id's to which stuff will be added
                List<int> PatchesToMerge = new List<int>();
                int[] PatchIDs = CheckPatchIDs(MergeCNPatch.AffectedPatches_id, MergeCNPatch.AffectedPatches_nm);
                for (int i = 0; i < PatchIDs.Length; i++)
                    PatchesToMerge.Add(PatchIDs[i]);

                // send the list to merger
                AmalgamatePatches(PatchesToMerge);
            }
        }

        /// <summary>
        /// Comunicate other components that N amount in the soil has changed
        /// </summary>
        /// <param name="dltN">N changes</param>
        private void SendExternalMassFlowN(double dltN)
        {

            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();
            if (Math.Abs(dltN) <= EPSILON)
                dltN = 0.0;
            massBalanceChange.FlowType = dltN >= 0 ? "gain" : "loss";
            massBalanceChange.PoolClass = "soil";
            massBalanceChange.N = (float)Math.Abs(dltN);
            ExternalMassFlow.Invoke(massBalanceChange);
        }

        /// <summary>
        /// Comunicate other components that C amount in the soil has changed
        /// </summary>
        /// <param name="dltC">C changes</param>
        private void SendExternalMassFlowC(double dltC)
        {

            ExternalMassFlowType massBalanceChange = new ExternalMassFlowType();
            if (Math.Abs(dltC) <= EPSILON)
                dltC = 0.0;
            massBalanceChange.FlowType = dltC >= 0 ? "gain" : "loss";
            massBalanceChange.PoolClass = "soil";
            massBalanceChange.N = (float)Math.Abs(dltC);
            ExternalMassFlow.Invoke(massBalanceChange);
        }

        #endregion

        #endregion

        #region Auxiliary functions

        /// <summary>
        /// Conversion factor: kg/ha to ppm (mg/kg)
        /// </summary>
        /// <param name="Layer">layer to calculate</param>
        /// <returns>conversion factor</returns>
        private double convFactor_kgha2ppm(int Layer)
        {
            if (bd == null || dlayer == null || bd.Length == 0 || dlayer.Length == 0)
            {
                return 0.0;
                throw new Exception(" Error on computing convertion factor, kg/ha to ppm. Value for dlayer or bulk density not valid");
            }
            return Utility.Math.Divide(100.0, bd[Layer] * dlayer[Layer], 0.0);
        }

        /// <summary>
        /// Check whether there is any considerable values in the array
        /// </summary>
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
                foreach (double Value in anArray)
                    result += Value;
            }
            return result;
        }

        /// <summary>
        /// Find the index at which the cumulative amount is equal or greater than 'sum'
        /// </summary>
        /// <param name="sumTarget">The target value</param>
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

        #endregion
    }

}