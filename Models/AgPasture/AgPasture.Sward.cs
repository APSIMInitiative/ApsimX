//-----------------------------------------------------------------------
// <copyright file="AgPasture.Sward.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Models;
using Models.Core;
using Models.Soils;
using Models.PMF;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.AgPasture
{
    /// <summary>A multi-species pasture model</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Sward : Model, ICrop
    {
        #region Links, events and delegates  -------------------------------------------------------------------------------

        //- Links  ----------------------------------------------------------------------------------------------------

        /// <summary>Link to the Soil (provides the soil information).</summary>
        [Link]
        private Soil mySoil = null;

        /// <summary>Link to apsim's Resource Arbitrator module.</summary>
        [Link(IsOptional = true)]
        private Arbitrator.Arbitrator apsimArbitrator = null;

        /// <summary>Link to apsim's Resource Arbitrator module.</summary>
        [Link(IsOptional = true)]
        private SoilArbitrator soilArbitrator = null;

        //- Events  ---------------------------------------------------------------------------------------------------

        /// <summary>Invoked for incorporating soil FOM.</summary>
        /// <param name="Data">The data about biomass deposited by this plant into the soil FOM</param>
        public delegate void FOMLayerDelegate(FOMLayerType Data);

        /// <summary>Occurs when plant is depositing senesced roots.</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>Invoked for incorporating surface OM.</summary>
        /// <param name="Data">The data about biomass deposited by this plant onto the soil surface</param>
        public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);

        /// <summary>Occurs when plant is detaching dead tissues, litter.</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>Invoked for changing soil water due to uptake.</summary>
        /// <param name="Data">The changes in the amount of water for each soil layer</param>
        public delegate void WaterChangedDelegate(WaterChangedType Data);

        /// <summary>Occurs when plant takes up water.</summary>
        public event WaterChangedDelegate WaterChanged;

        /// <summary>Invoked for changing soil nitrogen due to uptake.</summary>
        /// <param name="Data">The changes in the soil N for each soil layer</param>
        public delegate void NitrogenChangedDelegate(NitrogenChangedType Data);

        /// <summary>Occurs when the plant takes up soil N.</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>Gets a list of cultivar names (not used by AgPasture).</summary>
        public string[] CultivarNames
        {
            get { return null; }
        }

        /// <summary>Sows the plants.</summary>
        /// <param name="cultivar">Cultivar type</param>
        /// <param name="population">Plants per area</param>
        /// <param name="depth">Sowing depth</param>
        /// <param name="rowSpacing">space between rows</param>
        /// <param name="maxCover">maximum ground cover</param>
        /// <param name="budNumber">Number of buds</param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
        {
            // sward being sown, sow each species available
            for (int s = 0; s < numSpecies; s++)
                mySpecies[s].Sow(cultivar, population, depth, rowSpacing);

            swardIsAlive = true;
        }

        /// <summary>Returns true if the crop is ready for harvesting.</summary>
        public bool IsReadyForHarvesting { get { return false; } }

        /// <summary>Harvests the crop.</summary>
        public void Harvest() { }

        /// <summary>Ends the crop.</summary>
        /// <remarks>All plant material is moved on to surfaceOM and soilFOM.</remarks>
        public void EndCrop()
        {
            foreach (PastureSpecies species in mySpecies)
                species.EndCrop();
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model parameters  ------------------------------------------------------------------------------------------

        /// <summary>Gets the reference to the species present in the sward.</summary>
        [XmlIgnore]
        public PastureSpecies[] mySpecies { get; private set; }

        /// <summary>The number of species in the sward</summary>
        private int numSpecies = 1;

        /// <summary>Gets the number species in the sward.</summary>
        [XmlIgnore]
        public int NumSpecies
        {
            get { return numSpecies; }
        }

        // - Parameters that are set via user interface  ---------------------------------------------------------------

        /// <summary>Flag whether the sward controls the species routines.</summary>
        private bool isSwardControlled = true;

        /// <summary>Gets or sets whether the sward controls the process flow in all species.</summary>
        [Description("Is the sward controlling the process flow in all pasture species?")]
        public YesNoAnswerSward ControlledBySward
        {
            get
            {
                if (isSwardControlled)
                    return YesNoAnswerSward.yes;
                else
                    return YesNoAnswerSward.no;
            }
            set { isSwardControlled = value == YesNoAnswerSward.yes; }
        }

        /// <summary>A yes or no answer.</summary>
        public enum YesNoAnswerSward
        {
            /// <summary>a positive answer</summary>
            yes,
            /// <summary>a negative answer</summary>
            no
        }

        /// <summary>Flag for the model controlling the water uptake.</summary>
        private string myWaterUptakeSource = "Sward";

        /// <summary>Gets or sets the model controlling the water uptake.</summary>
        /// <remarks>Defaults to 'species' if a resource arbitrator or SWIM3 is present</remarks>
        [Description("Which model is responsible for water uptake ('sward' or pasture 'species')?")]
        public string WaterUptakeSource
        {
            get { return myWaterUptakeSource; }
            set { myWaterUptakeSource = value; }
        }

        /// <summary>Flag for the model controlling the N uptake.</summary>
        private string myNUptakeSource = "Sward";

        /// <summary>Gets or sets the model controlling the N uptake.</summary>
        /// <remarks>Defaults to 'species' if a resource arbitrator or SWIM3 is present</remarks>
        [Description("Which model is responsible for nitrogen uptake ('sward', pasture 'species', or 'apsim')?")]
        public string NUptakeSource
        {
            get { return myNUptakeSource; }
            set { myNUptakeSource = value; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Private variables  -----------------------------------------------------------------------------------------

        /// <summary>Flag whether there is at least on plant alive in the sward.</summary>
        private bool swardIsAlive = true;

        // -- Water variables  ----------------------------------------------------------------------------------------

        /// <summary>Amount of soil water available to the sward, from each soil layer (mm).</summary>
        private double[] swardSoilWaterAvailable;

        /// <summary>Soil water uptake for the whole sward, from each soil layer (mm).</summary>
        private double[] swardSoilWaterUptake;

        // -- Nitrogen variables  -------------------------------------------------------------------------------------

        /// <summary>Amount of NH4-N available for uptake to the whole sward (kg/ha).</summary>
        private double[] swardSoilNH4Available;

        /// <summary>Amount of NO3-N available for uptake to the whole sward (kg/ha).</summary>
        private double[] swardSoilNO3Available;

        /// <summary>Amount of NH4-N taken up by the whole sward (kg/ha).</summary>
        private double[] swardSoilNH4Uptake;

        /// <summary>Amount of NO3-N taken up by the whole sward (kg/ha).</summary>
        private double[] swardSoilNO3Uptake;

        // - General variables  ---------------------------------------------------------------------------------------

        /// <summary>Number of soil layers.</summary>
        private int nLayers = 0;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Constants  -------------------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        public const double CinDM = 0.4;

        /// <summary>Nitrogen to protein conversion factor (kg/kg).</summary>
        public const double N2Protein = 6.25;

        /// <summary>Minimum significant difference between two values</summary>
        public const double Epsilon = 0.000000001;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model outputs  ---------------------------------------------------------------------------------------------

        /// <summary>Gets a value indicating whether the plant is alive.</summary>
        public bool IsAlive
        {
            get { return PlantStatus == "alive"; }
        }

        /// <summary>Gets the plant status (dead, alive, etc).</summary>
        [Description("Plant status (dead, alive, etc)")]
        [Units("")]
        public string PlantStatus
        {
            get
            {
                if (mySpecies.Any(mySpecies => mySpecies.PlantStatus == "alive"))
                    return "alive";
                else
                    return "out";
            }
        }

        #region - DM and C amounts  ----------------------------------------------------------------------------------------

        /// <summary>Gets the total plant C content (kg/ha).</summary>
        [Description("Total amount of C in plants")]
        [Units("kg/ha")]
        public double TotalC
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.TotalWt * CinDM); }
        }

        /// <summary>Gets the plant total dry matter weight (kg/ha).</summary>
        /// <value>The total DM weight.</value>
        [Description("Total dry matter weight of plants")]
        [Units("kg/ha")]
        public double TotalWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.TotalWt); }
        }

        /// <summary>Gets the plant DM weight above ground (kg/ha).</summary>
        [Description("Total dry matter weight of plants above ground")]
        [Units("kg/ha")]
        public double AboveGroundWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.AboveGroundWt); }
        }

        /// <summary>Gets the DM weight of live plant parts above ground (kg/ha).</summary>
        [Description("Total dry matter weight of plants alive above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.AboveGroundLiveWt); }
        }

        /// <summary>Gets the DM weight of dead plant parts above ground (kg/ha).</summary>
        [Description("Total dry matter weight of dead plants above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.AboveGroundDeadWt); }
        }

        /// <summary>Gets the DM weight of the plant below ground (kg/ha).</summary>
        [Description("Total dry matter weight of plants below ground")]
        [Units("kg/ha")]
        public double BelowGroundWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RootWt); }
        }

        /// <summary>Gets the total standing DM weight (kg/ha).</summary>
        [Description("Total dry matter weight of standing plants parts")]
        [Units("kg/ha")]
        public double StandingWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StandingWt); }
        }

        /// <summary>Gets the DM weight of standing live plant material (kg/ha).</summary>
        [Description("Dry matter weight of live standing plants parts")]
        [Units("kg/ha")]
        public double StandingLiveWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StandingLiveWt); }
        }

        /// <summary>Gets the DM weight of standing dead plant material (kg/ha).</summary>
        [Description("Dry matter weight of dead standing plants parts")]
        [Units("kg/ha")]
        public double StandingDeadWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StandingDeadWt); }
        }

        /// <summary>Gets the total DM weight of leaves (kg/ha).</summary>
        [Description("Total dry matter weight of plant's leaves")]
        [Units("kg/ha")]
        public double LeafWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.LeafWt); }
        }

        /// <summary>Gets the total DM weight of stems and sheath (kg/ha).</summary>
        [Description("Total dry matter weight of plant's stems")]
        [Units("kg/ha")]
        public double StemWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StemWt); }
        }

        /// <summary>Gets the total DM weight od stolons (kg/ha).</summary>
        [Description("Total dry matter weight of plant's stolons")]
        [Units("kg/ha")]
        public double StolonWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StolonWt); }
        }

        /// <summary>Gets the total DM weight of roots (kg/ha).</summary>
        [Description("Total dry matter weight of plant's roots")]
        [Units("kg/ha")]
        public double RootWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RootWt); }
        }

        /// <summary>Gets the root DM weight foreach layer (kg/ha).</summary>
        [Description("Root dry matter weight by layer")]
        [Units("kg/ha")]
        public double[] RootLayerWt
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int s = 0; s < numSpecies; s++)
                        result[layer] = mySpecies[s].RootLayerWt[layer];
                return result;
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - C and DM flows  ------------------------------------------------------------------------------------------

        /// <summary>Gets the gross potential growth rate (kg/ha).</summary>
        [Description("Gross potential plant growth (potential C assimilation)")]
        [Units("kg/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.GrossPotentialGrowthWt); }
        }

        /// <summary>Gets the respiration rate (kg/ha).</summary>
        [Description("Respiration rate (DM lost via respiration)")]
        [Units("kg/ha")]
        public double RespirationWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RespirationWt); }
        }

        /// <summary>Gets the net potential growth rate (kg/ha).</summary>
        [Description("Net potential plant growth")]
        [Units("kg/ha")]
        public double NetPotentialGrowthWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.NetPotentialGrowthWt); }
        }

        /// <summary>Gets the potential growth rate after water stress (kg/ha).</summary>
        [Description("Potential growth rate after water stress")]
        [Units("kg/ha")]
        public double PotGrowthWt_Wstress
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.PotGrowthWt_Wstress); }
        }

        /// <summary>Gets the actual growth rate (kg/ha).</summary>
        [Description("Actual plant growth (before littering)")]
        [Units("kg/ha")]
        public double ActualGrowthWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.ActualGrowthWt); }
        }

        /// <summary>Gets the effective growth rate (kg/ha).</summary>
        [Description("Effective growth rate, after turnover")]
        [Units("kg/ha")]
        public double EffectiveGrowthWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.EffectiveGrowthWt); }
        }

        /// <summary>Gets the effective herbage growth rate (kg/ha).</summary>
        [Description("Effective herbage (shoot) growth")]
        [Units("kg/ha")]
        public double HerbageGrowthWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.HerbageGrowthWt); }
        }

        /// <summary>Gets the effective root growth rate (kg/ha).</summary>
        [Description("Effective root growth rate")]
        [Units("kg/ha")]
        public double RootGrowthWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RootGrowthWt); }
        }

        /// <summary>Gets the litter DM weight deposited onto soil surface (kg/ha).</summary>
        [Description("Dry matter amount of litter deposited onto soil surface")]
        [Units("kg/ha")]
        public double LitterDepositionWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.LitterWt); }
        }

        /// <summary>Gets the senesced root DM weight added to soil FOM (kg/ha).</summary>
        [Description("Dry matter amount of senescent roots added to soil FOM")]
        [Units("kg/ha")]
        public double RootSenescenceWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RootSenescedWt); }
        }

        /// <summary>Gets the gross primary productivity (kg/ha).</summary>
        [Description("Gross primary productivity")]
        [Units("kg/ha")]
        public double GPP
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.GPP); }
        }

        /// <summary>Gets the net primary productivity (kg/ha).</summary>
        [Description("Net primary productivity")]
        [Units("kg/ha")]
        public double NPP
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.NPP); }
        }

        /// <summary>Gets the net above-ground primary productivity (kg/ha).</summary>
        [Description("Net above-ground primary productivity")]
        [Units("kg/ha")]
        public double NAPP
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.NAPP); }
        }

        /// <summary>Gets the net below-ground primary productivity (kg/ha).</summary>
        [Description("Net below-ground primary productivity")]
        [Units("kg/ha")]
        public double NBPP
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.NBPP); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - N amounts  -----------------------------------------------------------------------------------------------

        /// <summary>Gets the plant total N content (kg/ha).</summary>
        [Description("Total amount of N in plants")]
        [Units("kg/ha")]
        public double TotalN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.TotalN); }
        }

        /// <summary>Gets the N content in the plant above ground (kg/ha).</summary>
        [Description("Total amount of N in plants above ground")]
        [Units("kg/ha")]
        public double AboveGroundN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.AboveGroundN); }
        }

        /// <summary>Gets the N content in live plant material above ground (kg/ha).</summary>
        [Description("Total amount of N in plants alive above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.AboveGroundLiveN); }
        }

        /// <summary>Gets the N content of dead plant material above ground (kg/ha).</summary>
        [Description("Total amount of N in dead plants above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.AboveGroundDeadN); }
        }

        /// <summary>Gets the N content of plants below ground (kg/ha).</summary>
        [Description("Total amount of N in plants below ground")]
        [Units("kg/ha")]
        public double BelowGroundN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.BelowGroundN); }
        }

        /// <summary>Gets the N content of standing plants (kg/ha).</summary>
        [Description("Total amount of N in standing plants")]
        [Units("kg/ha")]
        public double StandingN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StandingN); }
        }

        /// <summary>Gets the N content of standing live plant material (kg/ha).</summary>
        [Description("Total amount of N in standing alive plants")]
        [Units("kg/ha")]
        public double StandingLiveN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StandingLiveN); }
        }

        /// <summary>Gets the N content  of standing dead plant material (kg/ha).</summary>
        [Description("Total amount of N in dead standing plants")]
        [Units("kg/ha")]
        public double StandingDeadN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StandingDeadN); }
        }

        /// <summary>Gets the total N content of leaves (kg/ha).</summary>
        [Description("Total amount of N in the plant's leaves")]
        [Units("kg/ha")]
        public double LeafN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.LeafN); }
        }

        /// <summary>Gets the total N content of stems and sheath (kg/ha).</summary>
        [Description("Total amount of N in the plant's stems")]
        [Units("kg/ha")]
        public double StemN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StemN); }
        }

        /// <summary>Gets the total N content of stolons (kg/ha).</summary>
        [Description("Total amount of N in the plant's stolons")]
        [Units("kg/ha")]
        public double StolonN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.StolonN); }
        }

        /// <summary>Gets the total N content of roots (kg/ha).</summary>
        [Description("Total amount of N in the plant's roots")]
        [Units("kg/ha")]
        public double RootN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RootN); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - N concentrations  ----------------------------------------------------------------------------------------

        /// <summary>Gets the average N concentration of standing plant material (kg/kg).</summary>
        [Description("Average N concentration of standing plants")]
        [Units("kg/kg")]
        public double StandingNConc
        {
            get { return MathUtilities.Divide(StandingN, StandingWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of leaves (kg/kg).</summary>
        [Description("Average N concentration of leaves")]
        [Units("kg/kg")]
        public double LeafNConc
        {
            get { return MathUtilities.Divide(LeafN, LeafWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of stems and sheath (kg/kg).</summary>
        [Description("Average N concentration in stems")]
        [Units("kg/kg")]
        public double StemNConc
        {
            get { return MathUtilities.Divide(StemN, StemWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of stolons (kg/kg).</summary>
        [Description("Average N concentration in stolons")]
        [Units("kg/kg")]
        public double StolonNConc
        {
            get { return MathUtilities.Divide(StolonN, StolonWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of roots (kg/kg).</summary>
        [Description("Average N concentration in roots")]
        [Units("kg/kg")]
        public double RootNConc
        {
            get { return MathUtilities.Divide(RootN, RootWt, 0.0); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - N flows  -------------------------------------------------------------------------------------------------

        /// <summary>Gets the amount of N remobilised from senesced tissue (kg/ha).</summary>
        [Description("Amount of N potentially remobilisable from senescing tissue")]
        [Units("kg/ha")]
        public double RemobilisableN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RemobilisableSenescedN); }
        }

        /// <summary>Gets the amount of N remobilised from senesced tissue (kg/ha).</summary>
        [Description("Amount of N remobilised from senescing tissue")]
        [Units("kg/ha")]
        public double RemobilisedN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RemobilisedSenescedN); }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable (kg/ha).</summary>
        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kg/ha")]
        public double RemobilisableLuxuryN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RemobilisableLuxuryN); }
        }

        /// <summary>Gets the amount of luxury N remobilised (kg/ha).</summary>
        [Description("Amount of luxury N remobilised")]
        [Units("kg/ha")]
        public double RemobilisedLuxuryN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RemobilisedLuxuryN); }
        }

        /// <summary>Gets the amount of atmospheric N fixed (kg/ha).</summary>
        [Description("Amount of atmospheric N fixed")]
        [Units("kg/ha")]
        public double FixedN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.FixedN); }
        }

        /// <summary>Gets the amount of N required with luxury uptake (kg/ha).</summary>
        [Description("Plant nitrogen requirement with luxury uptake")]
        [Units("kg/ha")]
        public double NitrogenRequiredLuxury
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RequiredLuxuryN); }
        }

        /// <summary>Gets the amount of N required for optimum N content (kg/ha).</summary>
        [Description("Plant nitrogen requirement for optimum growth")]
        [Units("kg/ha")]
        public double NitrogenRequiredOptimum
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.RequiredOptimumN); }
        }

        /// <summary>Gets the amount of N demanded from soil (kg/ha).</summary>
        [Description("Plant nitrogen demand from soil")]
        [Units("kg/ha")]
        public double NitrogenDemand
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.DemandSoilN); }
        }

        /// <summary>Gets the amount of plant available N in soil layer (kg/ha).</summary>
        [Description("Plant available nitrogen in each soil layer")]
        [Units("kg/ha")]
        public double[] NitrogenAvailable
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int sp = 0; sp < numSpecies; sp++)
                        result[layer] = mySpecies[sp].SoilNH4Available[layer] + mySpecies[sp].SoilNO3Available[layer];
                return result;
            }
        }

        /// <summary>Gets the amount of N taken up from each soil layer (kg/ha).</summary>
        [Description("Plant nitrogen uptake from each soil layer")]
        [Units("kg/ha")]
        public double[] NitrogenUptake
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int sp = 0; sp < numSpecies; sp++)
                        result[layer] = mySpecies[sp].SoilNH4Uptake[layer] + mySpecies[sp].SoilNO3Uptake[layer];
                return result;
            }
        }

        /// <summary>Gets the amount of N deposited as litter onto soil surface (kg/ha).</summary>
        [Description("Amount of N deposited as litter onto soil surface")]
        [Units("kg/ha")]
        public double LitterDepositionN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.LitterN); }
        }

        /// <summary>Gets the amount of N from senesced roots added to soil FOM (kg/ha).</summary>
        [Description("Amount of N added to soil FOM by senescent roots")]
        [Units("kg/ha")]
        public double RootSenescenceN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.SenescedRootN); }
        }

        /// <summary>Gets the amount of N in new grown tissue (kg/ha).</summary>
        [Description("Nitrogen amount in new growth")]
        [Units("kg/ha")]
        public double ActualGrowthN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.ActualGrowthN); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Turnover and DM allocation  ------------------------------------------------------------------------------

        /// <summary>Gets the DM weight allocated to shoot (kg/ha).</summary>
        [Description("Dry matter allocated to shoot")]
        [Units("kg/ha")]
        public double DMToShoot
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.ActualGrowthWt * mySpecies.ShootDMAllocation); }
        }

        /// <summary>Gets the DM weight allocated to roots (kg/ha).</summary>
        [Description("Dry matter allocated to roots")]
        [Units("kg/ha")]
        public double DMToRoots
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.ActualGrowthWt * mySpecies.RootDMAllocation); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - LAI and cover  -------------------------------------------------------------------------------------------

        /// <summary>Gets the total plant LAI, leaf area index (m^2/m^2).</summary>
        [Description("Total leaf area index")]
        [Units("m^2/m^2")]
        public double LAITotal
        {
            get { return LAIGreen + LAIDead; }
        }

        /// <summary>Gets the plant's green LAI, leaf area index (m^2/m^2).</summary>
        [Description("Leaf area index of green leaves")]
        [Units("m^2/m^2")]
        public double LAIGreen
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.LAIGreen); }
        }

        /// <summary>Gets the plant's dead LAI, leaf area index (m^2/m^2).</summary>
        [Description("Leaf area index of dead leaves")]
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.LAIDead); }
        }

        /// <summary>Gets the average light extinction coefficient (0-1).</summary>
        [Description("Average light extinction coefficient")]
        [Units("0-1")]
        public double LightExtCoeff
        {
            get
            {
                double result = mySpecies.Sum(mySpecies => mySpecies.LAITotal * mySpecies.LightExtentionCoefficient);
                result /= mySpecies.Sum(mySpecies => mySpecies.LAITotal);

                return result;
            }
        }

        /// <summary>Gets the plant's total cover (0-1).</summary>
        [Description("Fraction of soil covered by plants")]
        [Units("%")]
        public double CoverTotal
        {
            get
            {
                if (LAITotal == 0) return 0;
                return 1.0 - Math.Exp(-LightExtCoeff * LAITotal);
            }
        }

        /// <summary>Gets the plant's green cover (0-1).</summary>
        [Description("Fraction of soil covered by green leaves")]
        [Units("%")]
        public double CoverGreen
        {
            get
            {
                if (LAIGreen == 0) return 0.0;
                return 1.0 - Math.Exp(-LightExtCoeff * LAIGreen);
            }
        }

        /// <summary>Gets the plant's dead cover (0-1).</summary>
        [Description("Fraction of soil covered by dead leaves")]
        [Units("%")]
        public double CoverDead
        {
            get
            {
                if (LAIDead == 0) return 0.0;
                return 1.0 - Math.Exp(-LightExtCoeff * LAIDead);
            }
        }

        /// <summary>Gets the sward's average height (mm).</summary>
        [Description("Average height of sward")]
        [Units("mm")]
        public double Height
        {
            get
            {
                double result = 0.0;
                if (StandingWt > 0.0)
                {
                    for (int s = 0; s < numSpecies; s++)
                        result += mySpecies[s].Height * mySpecies[s].StandingWt;

                    result /= StandingWt;
                }

                return result;
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Root depth and distribution  -----------------------------------------------------------------------------

        /// <summary>Gets sward's average root zone depth (mm).</summary>
        [Description("Depth of root zone")]
        [Units("mm")]
        public double RootZoneDepth
        {
            get { return mySpecies.Max(mySpecies => mySpecies.RootDepth); }
        }

        /// <summary>Gets the root frontier (layer at bottom of root zone).</summary>
        /// <value>The layer at bottom of root zone.</value>
        [Description("Layer at bottom of root zone")]
        [Units("mm")]
        public int RootFrontier
        {
            get { return mySpecies.Max(mySpecies => mySpecies.RootFrontier); }
        }

        /// <summary>Gets the fraction of root dry matter for each soil layer (0-1).</summary>
        /// <value>The root fraction.</value>
        [Description("Fraction of root dry matter for each soil layer")]
        [Units("0-1")]
        public double[] RootWtFraction
        {
            get
            {
                double[] result = new double[nLayers];
                //              rootFraction = RootLayerWt[layer] / RootWt;
                if (RootWt > 0.0)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] = mySpecies.Sum(mySpecies => mySpecies.RootWt * mySpecies.RootWtFraction[layer]) / RootWt;
                return result;
            }
        }

        /// <summary>Gets the sward's average root length density for each soil layer (mm/mm^3).</summary>
        /// <value>The root length density.</value>
        [Description("Root length density")]
        [Units("mm/mm^3")]
        public double[] RLD
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = mySpecies.Sum(mySpecies => mySpecies.RLD[layer]);
                return result;
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Water amounts  -------------------------------------------------------------------------------------------

        /// <summary>Gets the amount of water demanded by plants (mm).</summary>
        /// <value>The water demand.</value>
        [Description("Plant water demand")]
        [Units("mm")]
        public double WaterDemand
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.WaterDemand); }
        }

        /// <summary>Gets the amount of soil water available for uptake (mm).</summary>
        /// <value>The soil available water.</value>
        [Description("Plant available water in soil")]
        [Units("mm")]
        public double[] SoilAvailableWater
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = mySpecies.Sum(mySpecies => mySpecies.SoilAvailableWater[layer]);
                return result;
            }
        }

        /// <summary>Gets the amount of water taken up by the plants (mm).</summary>
        /// <value>The water uptake.</value>
        [Description("Plant water uptake from soil")]
        [Units("mm")]
        public double[] WaterUptake
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = mySpecies.Sum(mySpecies => mySpecies.WaterUptake[layer]);
                return result;
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Growth limiting factors  ---------------------------------------------------------------------------------

        /// <summary>Gets the growth factor due to variations in intercepted radiation (0-1).</summary>
        [Description("Growth factor due to variations in intercepted radiation")]
        [Units("0-1")]
        public double GlfRadnIntercept
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfRadnIntercept * mySpecies.GrossPotentialGrowthWt), GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to variations in atmospheric CO2 (0-1).</summary>
        [Description("Growth limiting factor due to variations in atmospheric CO2")]
        [Units("0-1")]
        public double GlfCO2
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfCO2 * mySpecies.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the average growth limiting factor due to N concentration in the plant (0-1).</summary>
        [Description("Average plant growth limiting factor due to plant N concentration")]
        [Units("0-1")]
        public double GlfNConcentration
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfNContent * mySpecies.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the average growth limiting factor due to temperature (0-1).</summary>
        [Description("Average plant growth limiting factor due to temperature")]
        [Units("0-1")]
        public double GlfTemperature
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfTemperature * mySpecies.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to heat stress (0-1).</summary>
        [Description("Growth limiting factor due to heat stress")]
        [Units("0-1")]
        public double GlfHeatDamage
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfHeatDamage * mySpecies.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to cold stress (0-1).</summary>
        [Description("Growth limiting factor due to cold stress")]
        [Units("0-1")]
        public double GlfColdDamage
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfColdDamage * mySpecies.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the average generic growth limiting factor (arbitrary limitation) (0-1).</summary>
        [Description("Average generic plant growth limiting factor, used at potential growth level")]
        [Units("0-1")]
        public double GlfGeneric
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfGeneric * mySpecies.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the average growth limiting factor due to water availability (0-1).</summary>
        [Description("Average plant growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GlfWaterSupply
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfWaterSupply * mySpecies.LAIGreen), LAIGreen, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to water logging (0-1).</summary>
        [Description("Average growth limiting factor due to lack of soil aeration")]
        [Units("0-1")]
        public double GlfWaterLogging
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfWaterLogging * mySpecies.NetPotentialGrowthWt),
                    NetPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the average growth limiting factor due to N availability (0-1).</summary>
        [Description("Average growth limiting factor due to nitrogen availability")]
        [Units("0-1")]
        public double GlfNSupply
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfNSupply * mySpecies.PotGrowthWt_Wstress),
                    PotGrowthWt_Wstress, 0.0);
            }
        }

        /// <summary>Gets the average generic growth limiting factor due to soil fertility, for nutrients other than N (0-1).</summary>
        [Description("Average generic growth limiting factor due to soil fertility, for nutrients other than N")]
        [Units("0-1")]
        public double GlfSFertility
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.GlfSFertility * mySpecies.PotGrowthWt_Wstress),
                    PotGrowthWt_Wstress, 0.0);
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Harvest variables  ---------------------------------------------------------------------------------------

        /// <summary>Gets the amount of dry matter available for harvesting (kg/ha).</summary>
        /// <value>The harvestable DM weight.</value>
        [Description("Total dry matter amount available for harvesting")]
        [Units("kg/ha")]
        public double HarvestableWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.HarvestableWt); }
        }

        /// <summary>Gets the amount of dry matter harvested (kg/ha).</summary>
        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kg/ha")]
        public double HarvestedWt
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.HarvestedWt); }
        }

        /// <summary>Gets the amount of plant N removed by harvest (kg/ha).</summary>
        [Description("Amount of N removed by harvest")]
        [Units("kg/ha")]
        public double HarvestedN
        {
            get { return mySpecies.Sum(mySpecies => mySpecies.HarvestedN); }
        }

        /// <summary>Gets the average N concentration in harvested material (kg/kg).</summary>
        [Description("average N concentration of harvested material")]
        [Units("kg/kg")]
        public double HarvestedNConc
        {
            get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
        }

        /// <summary>Gets the average digestibility of harvested DM (0-1).</summary>
        [Description("Average digestibility of harvested material")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.HarvestedDigestibility * mySpecies.HarvestedWt), HarvestedWt, 0.0);
            }
        }

        /// <summary>Gets the average ME (metabolisable energy) of harvested DM (MJ/ha).</summary>
        /// <value>The harvested ME.</value>
        [Description("Average ME of harvested material")]
        [Units("MJ/ha")]
        public double HarvestedME
        {
            get { return 16 * HarvestedDigestibility * HarvestedWt; }
        }

        /// <summary>Gets the average herbage digestibility (0-1).</summary>
        [Description("Average digestibility of standing herbage")]
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get { return MathUtilities.Divide(mySpecies.Sum(mySpecies => mySpecies.HerbageDigestibility * mySpecies.StandingWt), StandingWt, 0.0); }
        }

        /// <summary>Gets the average herbage ME, metabolisable energy (MJ/ha).</summary>
        [Description("Average ME of standing herbage")]
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get { return 16 * HerbageDigestibility * StandingWt; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Initialisation methods  ------------------------------------------------------------------------------------

        /// <summary>Called when the simulation is loaded.</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded()
        {
            // get the number and reference to the mySpecies in the sward
            numSpecies = Apsim.Children(this, typeof(PastureSpecies)).Count;
            mySpecies = new PastureSpecies[numSpecies];
            int s = 0;
            foreach (PastureSpecies species in Apsim.Children(this, typeof(PastureSpecies)))
            {
                this.mySpecies[s] = species;
                s += 1;
            }
        }

        /// <summary>Called when the simulation is commencing.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            // check whether uptake is controlled by the sward or by species
            if (apsimArbitrator != null || soilArbitrator != null)
            {
                myWaterUptakeSource = "species";
                myNUptakeSource = "species";
            }

            foreach (PastureSpecies species in mySpecies)
            {
                species.isSwardControlled = isSwardControlled;
                species.myWaterUptakeSource = myWaterUptakeSource;
                species.myNitrogenUptakeSource = myNUptakeSource;
            }

            // get the number of layers in the soil profile
            nLayers = mySoil.Thickness.Length;

            // initialise available N
            swardSoilWaterAvailable = new double[nLayers];
            swardSoilWaterUptake = new double[nLayers];
            swardSoilNH4Available = new double[nLayers];
            swardSoilNO3Available = new double[nLayers];
            swardSoilNH4Uptake = new double[nLayers];
            swardSoilNO3Uptake = new double[nLayers];
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Daily processes  -------------------------------------------------------------------------------------------

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/>instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // clear some variables
            swardSoilWaterAvailable = new double[nLayers];
            swardSoilWaterUptake = new double[nLayers];
            swardSoilNH4Available = new double[nLayers];
            swardSoilNO3Available = new double[nLayers];
            swardSoilNH4Uptake = new double[nLayers];
            swardSoilNO3Uptake = new double[nLayers];
        }

        /// <summary>Performs the calculations for potential growth.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/>instance containing the event data</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (swardIsAlive && isSwardControlled)
            {
                foreach (PastureSpecies species in mySpecies)
                {
                    // Evaluate tissue turnover and get remobilisation (C and N)
                    species.EvaluateTissueTurnoverRates();

                    // Get the potential gross growth
                    species.CalcDailyPotentialGrowth();

                    // Evaluate potential allocation of today's growth
                    species.EvaluateGrowthAllocation();

                }

                // Get the water demand, supply, and uptake
                DoWaterCalculations();

                foreach (PastureSpecies species in mySpecies)
                {
                    // Get the potential growth after water limitations
                    species.CalcGrowthAfterWaterLimitations();

                    // Get the N amount demanded for optimum growth and luxury uptake
                    species.EvaluateNitrogenDemand();
                }
            }
        }

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/>instance containing the event data</param>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (swardIsAlive && isSwardControlled)
            {
                // Get the nitrogen demand, supply, and uptake
                DoNitrogenCalculations();

                foreach (PastureSpecies species in mySpecies)
                {
                    // Get the actual growth, after nutrient limitations but before senescence
                    species.CalcGrowthAfterNutrientLimitations();

                    // Evaluate actual allocation of today's growth
                    species.EvaluateNewGrowthAllocation();

                    // Get the effective growth, after all limitations and senescence
                    species.DoEffectiveGrowth();
                }

                // Send detached tissues (litter and roots) to other modules
                DoSurfaceOMReturn(LitterDepositionWt, LitterDepositionN);
                DoIncorpFomEvent(RootSenescenceWt, RootSenescenceN);
            }
        }

        #region - Water uptake process  ------------------------------------------------------------------------------------

        /// <summary>Performs the water uptake calculations.</summary>
        private void DoWaterCalculations()
        {
            if (myWaterUptakeSource == "sward")
            {
                // Pack the soil information
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                myZone.Name = this.Parent.Name;
                myZone.Water = mySoil.Water;
                myZone.NO3N = mySoil.NO3N;
                myZone.NH4N = mySoil.NH4N;

                // Get the amount of soil available water
                GetSoilAvailableWater(myZone);

                // Water demand computed by MicroClimate

                // Get the amount of water taken up
                GetSoilWaterUptake();

                // Send the delta water to soil water module
                DoSoilWaterUptake();
            }
            else
            {
                //water uptake is controlled at species level, get sward totals
                for (int layer = 0; layer <= RootFrontier; layer++)
                {
                    foreach (PastureSpecies species in mySpecies)
                    {
                        swardSoilWaterAvailable[layer] += species.SoilAvailableWater[layer];
                        swardSoilWaterUptake[layer] += species.WaterUptake[layer];
                    }
                }

                // Send the delta water to soil water module
                DoSoilWaterUptake();
            }
        }

        /// <summary>Finds out the amount of plant available water in the soil, consider all species.</summary>
        /// <param name="myZone">Soil information</param>
        private void GetSoilAvailableWater(ZoneWaterAndN myZone)
        {
            double totalPlantWater;
            double totalSoilWater;
            double waterFraction = 1.0;
            double layerFraction = 1.0;

            // Get the water available as seen by each species
            foreach (PastureSpecies species in mySpecies)
               species.EvaluateSoilWaterAvailable(myZone);

            // Evaluate the available water for whole sward and adjust availability for each species if needed
            for (int layer = 0; layer <= RootFrontier; layer++)
            {
                // Get total as seen by each species
                totalPlantWater = 0.0;
                if (layer == RootFrontier) layerFraction = 0.0;
                foreach (PastureSpecies species in mySpecies)
                {
                    totalPlantWater += species.SoilAvailableWater[layer];
                    if (layer == RootFrontier)
                        layerFraction = Math.Max(layerFraction, species.FractionLayerWithRoots(layer));
                }

                // Get total in the soil
                totalSoilWater = Math.Max(0.0, myZone.Water[layer] - mySoil.SoilWater.LL15mm[layer]) * layerFraction;

                // Sward total is the minimum of the two totals
                swardSoilWaterAvailable[layer] = Math.Min(totalPlantWater, totalSoilWater);
                if (totalPlantWater > totalSoilWater)
                {
                    // adjust the water available for each species
                    waterFraction = MathUtilities.Divide(totalSoilWater, totalPlantWater, 0.0);
                    foreach (PastureSpecies species in mySpecies)
                        species.UpdateAvailableWater(waterFraction);
                }
            }
        }

        /// <summary>Gets the plant water uptake (potential), consider all species.</summary>
        private void GetSoilWaterUptake()
        {
            foreach (PastureSpecies species in mySpecies)
            {
                species.EvaluateSoilWaterUptake();
                for (int layer = 0; layer <= species.roots.BottomLayer; layer++)
                    swardSoilWaterUptake[layer] += species.WaterUptake[layer];
            }
        }

        /// <summary>Sends the delta water to the soil module.</summary>
        private void DoSoilWaterUptake()
        {
            if (swardSoilWaterUptake.Sum() > Epsilon)
            {
                WaterChangedType WaterTakenUp = new WaterChangedType();
                WaterTakenUp.DeltaWater = new double[nLayers];
                for (int layer = 0; layer <= RootFrontier; layer++)
                    WaterTakenUp.DeltaWater[layer] -= swardSoilWaterUptake[layer];

                if (WaterChanged != null)
                    WaterChanged.Invoke(WaterTakenUp);
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Nitrogen uptake process  ---------------------------------------------------------------------------------

        /// <summary>Performs the nitrogen uptake calculations.</summary>
        private void DoNitrogenCalculations()
        {
            if (myNUptakeSource.ToLower() == "sward")
            {
                // Pack the soil information
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                myZone.Name = this.Parent.Name;
                myZone.Water = mySoil.Water;
                myZone.NO3N = mySoil.NO3N;
                myZone.NH4N = mySoil.NH4N;

                // Get the N amount available in the soil
                GetSoilAvailableN(myZone);

                foreach (PastureSpecies species in mySpecies)
                {
                    // Get the N amount fixed through symbiosis
                    species.EvaluateNitrogenFixation();

                    // Evaluate the use of N remobilised and get N amount demanded from soil
                    species.EvaluateSoilNitrogenDemand();

                    // Get N amount taken up from the soil
                    species.EvaluateSoilNitrogenUptake();
                    for (int layer = 0; layer < RootFrontier; layer++)
                    {
                        swardSoilNH4Uptake[layer] += species.SoilNH4Uptake[layer];
                        swardSoilNO3Uptake[layer] += species.SoilNO3Uptake[layer];
                    }

                    // Evaluate whether remobilisation of luxury N is needed
                    species.EvaluateNLuxuryRemobilisation();
                }

                // Send delta N to the soil model
                DoSoilNitrogenUptake();
            }
            else
            {
                //N uptake is controlled at species level, get sward totals
                for (int layer = 0; layer < RootFrontier; layer++)
                {
                    foreach (PastureSpecies species in mySpecies)
                    {
                        swardSoilNH4Available[layer] += species.SoilNH4Available[layer];
                        swardSoilNO3Available[layer] += species.SoilNO3Available[layer];
                        swardSoilNH4Uptake[layer] += species.SoilNH4Uptake[layer];
                        swardSoilNO3Uptake[layer] += species.SoilNO3Uptake[layer];
                    }
                }

                // Send delta N to the soil model
                DoSoilNitrogenUptake();
            }
        }

        /// <summary>Finds out the amount of plant available nitrogen (NH4 and NO3) in the soil, consider all species.</summary>
        /// <param name="myZone">Soil information</param>
        private void GetSoilAvailableN(ZoneWaterAndN myZone)
        {
            double totalSoilNH4;
            double totalSoilNO3;
            double totalPlantNH4;
            double totalPlantNO3;
            double nh4Fraction = 1.0;
            double no3Fraction = 1.0;
            double layerFraction = 1.0;

            // Get the N available as seen by each species
            foreach (PastureSpecies species in mySpecies)
                species.EvaluateSoilNitrogenAvailable(myZone);

            // Evaluate the available N for whole sward and adjust availability for each species if needed
                for (int layer = 0; layer <= RootFrontier; layer++)
            {
                // Get total as seen by each species
                totalPlantNH4 = 0.0;
                totalPlantNO3 = 0.0;
                if (layer == RootFrontier) layerFraction = 0.0;
                    foreach (PastureSpecies species in mySpecies)
                {
                    totalPlantNH4 += species.SoilNH4Available[layer];
                    totalPlantNO3 += species.SoilNO3Available[layer];
                    if (layer == RootFrontier)
                        layerFraction = Math.Max(layerFraction, species.FractionLayerWithRoots(layer));
                }

                // Get total in the soil
                totalSoilNH4 = myZone.NH4N[layer] * layerFraction;
                totalSoilNO3 = myZone.NO3N[layer] * layerFraction;

                // Sward total is the minimum of the two totals
                swardSoilNH4Available[layer] = Math.Min(totalPlantNH4, totalSoilNH4);
                swardSoilNO3Available[layer] = Math.Min(totalPlantNO3, totalSoilNO3);
                if ((totalPlantNH4 > totalSoilNH4) || (totalPlantNO3 > totalSoilNO3))
                {
                    // adjust the N available for each species
                    nh4Fraction = Math.Min(1.0, MathUtilities.Divide(totalSoilNH4, totalPlantNH4, 0.0));
                    no3Fraction = Math.Min(1.0, MathUtilities.Divide(totalSoilNO3, totalPlantNO3, 0.0));
                    foreach (PastureSpecies species in mySpecies)
                    {
                        species.UpdateAvailableNitrogen(nh4Fraction, no3Fraction);
                    }
                }
            }
        }

        /// <summary>Sends the delta nitrogen to the soil module.</summary>
        private void DoSoilNitrogenUptake()
        {
            if ((swardSoilNH4Uptake.Sum() + swardSoilNO3Uptake.Sum()) > Epsilon)
            {
                NitrogenChangedType nitrogenTakenUp = new NitrogenChangedType();
                nitrogenTakenUp.Sender = Name;
                nitrogenTakenUp.SenderType = "Plant";
                nitrogenTakenUp.DeltaNO3 = new double[nLayers];
                nitrogenTakenUp.DeltaNH4 = new double[nLayers];

                for (int layer = 0; layer <= RootFrontier; layer++)
                {
                    nitrogenTakenUp.DeltaNH4[layer] = -swardSoilNH4Uptake[layer];
                    nitrogenTakenUp.DeltaNO3[layer] = -swardSoilNO3Uptake[layer];
                }

                if (NitrogenChanged != null)
                    NitrogenChanged.Invoke(nitrogenTakenUp);
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Other processes  -------------------------------------------------------------------------------------------

        /// <summary>Harvest (remove DM) the sward</summary>
        /// <param name="amount">DM amount</param>
        /// <param name="type">How the amount is interpreted (remove or residual)</param>
        public void Harvest(double amount, string type)
        {
            GrazeType GrazeData = new GrazeType();
            GrazeData.amount = amount;
            GrazeData.type = type;
            OnGraze(GrazeData);
        }

        /// <summary>Graze event, remove DM from sward</summary>
        /// <param name="GrazeData">How amount of DM to remove is defined</param>
        [EventSubscribe("Graze")]
        private void OnGraze(GrazeType GrazeData)
        {
            if ((!swardIsAlive) || StandingWt == 0)
                return;

            // Get the amount that can potentially be removed
            double amountRemovable = mySpecies.Sum(mySpecies => mySpecies.HarvestableWt);

            // get the amount required to remove
            double amountRequired = 0.0;
            if (GrazeData.type.ToLower() == "SetResidueAmount".ToLower())
            { // Remove all DM above given residual amount
                amountRequired = Math.Max(0.0, StandingWt - GrazeData.amount);
            }
            else if (GrazeData.type.ToLower() == "SetRemoveAmount".ToLower())
            { // Attempt to remove a given amount
                amountRequired = Math.Max(0.0, GrazeData.amount);
            }
            else
            {
                Console.WriteLine("  AgPasture - Method to set amount to remove not recognized, command will be ignored");
            }
            // get the actual amount to remove
            double amountToRemove = Math.Min(amountRequired, amountRemovable);

            // get the amounts to remove by mySpecies:
            if (amountRequired > 0.0)
            {
                // get the weights for each mySpecies, consider preference and available DM
                double[] tempWeights = new double[numSpecies];
                double[] tempAmounts = new double[numSpecies];
                double tempTotal = 0.0;
                double totalPreference = mySpecies.Sum(mySpecies => mySpecies.PreferenceForGreenOverDead + 1.0);
                for (int s = 0; s < numSpecies; s++)
                {
                    tempWeights[s] = mySpecies[s].PreferenceForGreenOverDead + 1.0;
                    tempWeights[s] += (totalPreference - tempWeights[s]) * (amountToRemove / amountRemovable);
                    tempAmounts[s] = Math.Max(0.0, mySpecies[s].StandingLiveWt - mySpecies[s].MinimumGreenWt)
                                   + mySpecies[s].StandingDeadWt;
                    tempTotal += tempAmounts[s] * tempWeights[s];
                }

                // do the actual removal for each mySpecies
                for (int s = 0; s < numSpecies; s++)
                {
                    // get the actual fractions to remove for each mySpecies
                        double fractionToHarvest = Math.Max(0.0, Math.Min(1.0, tempWeights[s] * tempAmounts[s] / tempTotal));

                    // remove DM and N for each mySpecies (digestibility is also evaluated)
                    mySpecies[s].RemoveDM(amountToRemove * fractionToHarvest);
                }
            }
        }
        /// <summary>Removes plant material simulating a graze event.</summary>
        /// <param name="amount">DM amount (kg/ha)</param>
        /// <param name="type">How the amount is interpreted (SetResidueAmount or SetRemoveAmount).</param>
        public void Graze(double amount, string type)
        {
            if (swardIsAlive || (HarvestableWt > Epsilon))
            {
                // Get the amount required to remove
                double amountRequired = 0.0;
                if (type.ToLower() == "setresidueamount")
                {
                    // Remove all DM above given residual amount
                    amountRequired = Math.Max(0.0, StandingWt - amount);
                }
                else if (type.ToLower() == "setremoveamount")
                {
                    // Remove a given amount
                    amountRequired = Math.Max(0.0, amount);
                }
                else
                {
                    throw new ApsimXException(this, "Type of amount to remove on graze not recognized (use \'SetResidueAmount\' or \'SetRemoveAmount\'");
                }
                // Get the actual amount to remove
                double amountToRemove = Math.Min(amountRequired, HarvestableWt);

                // Get the amounts to remove by mySpecies:
                if (amountRequired > 0.0)
                {
                    double[] fractionToRemove = new double[numSpecies];
                    for (int s = 0; s < numSpecies; s++)
                    {
                        // get the fraction to required for each mySpecies, partition according to available DM to harvest
                        fractionToRemove[s] = mySpecies[s].HarvestableWt / HarvestableWt;

                        // remove DM and N for each mySpecies (digestibility is also evaluated)
                        mySpecies[s].RemoveDM(amountToRemove * fractionToRemove[s]);
                    }
                }
            }
        }

        /// <summary>Returns a given amount of DM (and N) to surface organic matter.</summary>
        /// <param name="amountDM">DM amount to return (kg/ha)</param>
        /// <param name="amountN">N amount to return (kg/ha)</param>
        private void DoSurfaceOMReturn(double amountDM, double amountN)
        {
            if (BiomassRemoved != null)
            {
                Single dDM = (Single)amountDM;

                BiomassRemovedType BR = new BiomassRemovedType();
                String[] type = new String[] { "grass" };  // TODO: this should be "pasture" ??
                Single[] dltdm = new Single[] { (Single)amountDM };
                Single[] dltn = new Single[] { (Single)amountN };
                Single[] dltp = new Single[] { 0 };         // P not considered here
                Single[] fraction = new Single[] { 1 };     // fraction is always 1.0 here

                BR.crop_type = "grass";   //TODO: this could be the Name, what is the diff between name and type??
                BR.dm_type = type;
                BR.dlt_crop_dm = dltdm;
                BR.dlt_dm_n = dltn;
                BR.dlt_dm_p = dltp;
                BR.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(BR);
            }
        }

        /// <summary>Returns a given amount of DM (and N) to fresh organic matter pool in the soil</summary>
        /// <param name="amountDM">DM amount to return (kg/ha)</param>
        /// <param name="amountN">N amount to return (kg/ha)</param>
        private void DoIncorpFomEvent(double amountDM, double amountN)
        {
            FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[nLayers];

            // ****  RCichota, Jun/2014
            // root senesced are returned to soil (as FOM) considering return is proportional to root mass
            for (int layer = 0; layer < nLayers; layer++)
            {
                FOMType fomData = new FOMType();
                fomData.amount = amountDM * RootWtFraction[layer];
                fomData.N = amountN * RootWtFraction[layer];
                fomData.C = amountDM * RootWtFraction[layer] * CinDM;
                fomData.P = 0.0;              // P not considered here
                fomData.AshAlk = 0.0;         // Ash not considered here

                FOMLayerLayerType layerData = new FOMLayerLayerType();
                layerData.FOM = fomData;
                layerData.CNR = 0.0;        // not used here
                layerData.LabileP = 0;      // not used here

                FOMdataLayer[layer] = layerData;
            }

            if (IncorpFOM != null)
            {
                FOMLayerType FOMData = new FOMLayerType();
                FOMData.Type = "Pasture";
                FOMData.Layer = FOMdataLayer;
                IncorpFOM.Invoke(FOMData);
            }
        }

        /// <summary>Kill parts of all plants in the sward.</summary>
        /// <param name="fractioToKill">Fraction of crop to kill (0-1)</param>
        private void KillCrop(double fractioToKill)
        {
            foreach (PastureSpecies species in mySpecies)
                species.KillCrop(fractioToKill);
        }

        #endregion  --------------------------------------------------------------------------------------------------------
    }
}
