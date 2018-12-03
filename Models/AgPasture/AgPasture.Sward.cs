//--------------------------------------------------------------------------------------------------------------------------
// <copyright file="AgPasture.Sward.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
// </copyright>
//--------------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
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
    /// <summary>
    /// # [Name]
    /// A multi-species pasture model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Sward : Model
    {
        #region Links, events and delegates  -------------------------------------------------------------------------------

        ////- Links >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Link to the Soil (provides the soil information).</summary>
        [Link]
        private Soil mySoil = null;

        ////- Events >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

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

        #endregion  --------------------------------------------------------------------------------------------------------

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>Gets a list of cultivar names (not used by AgPasture).</summary>
        public string[] CultivarNames
        {
            get { return null; }
        }

        /// <summary>Sows the plants.</summary>
        /// <param name="cultivar">The cultivar type</param>
        /// <param name="population">The number of plants per area</param>
        /// <param name="depth">The sowing depth</param>
        /// <param name="rowSpacing">The space between rows</param>
        /// <param name="maxCover">The maximum ground cover (optional)</param>
        /// <param name="budNumber">The number of buds (optional)</param>
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

            swardIsAlive = false;
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model parameters  ------------------------------------------------------------------------------------------

        /// <summary>Gets the reference to the species present in the sward.</summary>
        [XmlIgnore]
        public PastureSpecies[] mySpecies { get; private set; }

        ////- Parameters defining controls >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

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
            /// <summary>a positive answer.</summary>
            yes,
            /// <summary>a negative answer.</summary>
            no
        }

        /// <summary>Flag for the model controlling the water uptake.</summary>
        private string myWaterUptakeSource = "Sward";

        /// <summary>Gets or sets the model controlling the water uptake.</summary>
        /// <remarks>Defaults to 'species' if a resource arbitrator or SWIM3 is present.</remarks>
        [Description("Which model is responsible for water uptake ('sward' or pasture 'species')?")]
        public string WaterUptakeSource
        {
            get { return myWaterUptakeSource; }
            set { myWaterUptakeSource = value; }
        }

        /// <summary>Flag for the model controlling the N uptake.</summary>
        private string myNUptakeSource = "Sward";

        /// <summary>Gets or sets the model controlling the N uptake.</summary>
        /// <remarks>Defaults to 'species' if a resource arbitrator or SWIM3 is present.</remarks>
        [Description("Which model is responsible for nitrogen uptake ('sward', pasture 'species', or 'apsim')?")]
        public string NUptakeSource
        {
            get { return myNUptakeSource; }
            set { myNUptakeSource = value; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Private variables  -----------------------------------------------------------------------------------------

        ////- General variables >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Flag whether there is at least on plant alive in the sward.</summary>
        private bool swardIsAlive = false;

        /// <summary>Number of species in the sward.</summary>
        private int numSpecies = 1;

        /// <summary>Number of soil layers.</summary>
        private int nLayers;

        ////- Water uptake >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Amount of soil water available to the sward, from each soil layer (mm).</summary>
        private double[] swardSoilWaterAvailable;

        /// <summary>Soil water uptake for the whole sward, from each soil layer (mm).</summary>
        private double[] swardSoilWaterUptake;

        ////- N demand and uptake >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Amount of NH4-N available for uptake to the whole sward (kg/ha).</summary>
        private double[] swardSoilNH4Available;

        /// <summary>Amount of NO3-N available for uptake to the whole sward (kg/ha).</summary>
        private double[] swardSoilNO3Available;

        /// <summary>Amount of NH4-N taken up by the whole sward (kg/ha).</summary>
        private double[] swardSoilNH4Uptake;

        /// <summary>Amount of NO3-N taken up by the whole sward (kg/ha).</summary>
        private double[] swardSoilNO3Uptake;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Constants  -------------------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        public const double CarbonInDM = 0.4;

        /// <summary>Nitrogen to protein conversion factor (kg/kg).</summary>
        public const double N2Protein = 6.25;

        /// <summary>Minimum significant difference between two values.</summary>
        public const double Epsilon = 0.000000001;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model outputs  ---------------------------------------------------------------------------------------------

        ////- General properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the flag signalling whether plant is alive (true/false).</summary>
        [Description("Flag signalling whether plant is alive")]
        [Units("true/false")]
        public bool IsAlive
        {
            get { return PlantStatus == "alive"; }
        }

        /// <summary>Gets the plant status (dead, alive, etc.).</summary>
        [Description("Plant status (dead, alive, etc.)")]
        [Units("-")]
        public string PlantStatus
        {
            get
            {
                if (mySpecies.Any(species => species.PlantStatus == "alive"))
                    return "alive";
                else
                    return "out";
            }
        }

        /// <summary>Gets the number of species in the sward.</summary>
        [Description("Number of species in the sward")]
        [Units("-")]
        public int NumSpecies
        {
            get { return numSpecies; }
        }

        ////- DM and C outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the total amount of C in the plant (kgC/ha).</summary>
        [Description("Total amount of C in the plant")]
        [Units("kg/ha")]
        public double TotalC
        {
            get { return mySpecies.Sum(species => species.TotalWt * CarbonInDM); }
        }

        /// <summary>Gets the total dry matter weight of plant (kgDM/ha).</summary>
        [Description("Total dry matter weight of plant")]
        [Units("kg/ha")]
        public double TotalWt
        {
            get { return mySpecies.Sum(species => species.TotalWt); }
        }

        /// <summary>Gets the dry matter weight of the plant above ground (kgDM/ha).</summary>
        [Description("Dry matter weight of the plant above ground")]
        [Units("kg/ha")]
        public double AboveGroundWt
        {
            get { return mySpecies.Sum(species => species.AboveGroundWt); }
        }

        /// <summary>Gets the dry matter weight of live tissues above ground (kgDM/ha).</summary>
        [Description("Dry matter weight of live tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveWt
        {
            get { return mySpecies.Sum(species => species.AboveGroundLiveWt); }
        }

        /// <summary>Gets the dry matter weight of dead tissues above ground (kgDM/ha).</summary>
        [Description("Dry matter weight of dead tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadWt
        {
            get { return mySpecies.Sum(species => species.AboveGroundDeadWt); }
        }

        /// <summary>Gets the dry matter weight of the plant below ground (kgDM/ha).</summary>
        [Description("Dry matter weight of the plant below ground")]
        [Units("kg/ha")]
        public double BelowGroundWt
        {
            get { return mySpecies.Sum(species => species.BelowGroundWt); }
        }

        /// <summary>Gets the dry matter weight of standing herbage (kgDM/ha).</summary>
        [Description("Dry matter weight of standing herbage")]
        [Units("kg/ha")]
        public double StandingHerbageWt
        {
            get { return mySpecies.Sum(species => species.StandingHerbageWt); }
        }

        /// <summary>Gets the dry matter weight of live standing herbage (kgDM/ha).</summary>
        [Description("Dry matter weight of live standing herbage")]
        [Units("kg/ha")]
        public double StandingLiveHerbageWt
        {
            get { return mySpecies.Sum(species => species.StandingLiveHerbageWt); }
        }

        /// <summary>Gets the dry matter weight of dead standing herbage (kgDM/ha).</summary>
        [Description("Dry matter weight of dead standing herbage")]
        [Units("kg/ha")]
        public double StandingDeadHerbageWt
        {
            get { return mySpecies.Sum(species => species.StandingDeadHerbageWt); }
        }

        /// <summary>Gets the dry matter weight of plant's leaves (kgDM/ha).</summary>
        [Description("Dry matter weight of plant's leaves")]
        [Units("kg/ha")]
        public double LeafWt
        {
            get { return mySpecies.Sum(species => species.LeafWt); }
        }

        /// <summary>Gets the dry matter weight of plant's stems and sheath (kgDM/ha).</summary>
        [Description("Dry matter weight of plant's stems and sheath")]
        [Units("kg/ha")]
        public double StemWt
        {
            get { return mySpecies.Sum(species => species.StemWt); }
        }

        /// <summary>Gets the dry matter weight of plant's stolons (kgDM/ha).</summary>
        [Description("Dry matter weight of plant's stolons")]
        [Units("kg/ha")]
        public double StolonWt
        {
            get { return mySpecies.Sum(species => species.StolonWt); }
        }

        /// <summary>Gets the dry matter weight of plant's roots (kgDM/ha).</summary>
        [Description("Dry matter weight of plant's roots")]
        [Units("kg/ha")]
        public double RootWt
        {
            get { return mySpecies.Sum(species => species.RootWt); }
        }

        /// <summary>Gets the dry matter weight of roots in each soil layer ().</summary>
        [Description("Dry matter weight of roots in each soil layer")]
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

        ////- N amount outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the total amount of N in the plant (kgN/ha).</summary>
        [Description("Total amount of N in the plant")]
        [Units("kg/ha")]
        public double TotalN
        {
            get { return mySpecies.Sum(species => species.TotalN); }
        }

        /// <summary>Gets the amount of N in the plant above ground (kgN/ha).</summary>
        [Description("Amount of N in the plant above ground")]
        [Units("kg/ha")]
        public double AboveGroundN
        {
            get { return mySpecies.Sum(species => species.AboveGroundN); }
        }

        /// <summary>Gets the amount of N in live tissues above ground (kgN/ha).</summary>
        [Description("Amount of N in live tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveN
        {
            get { return mySpecies.Sum(species => species.AboveGroundLiveN); }
        }

        /// <summary>Gets the amount of N in dead tissues above ground (kgN/ha).</summary>
        [Description("Amount of N in dead tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadN
        {
            get { return mySpecies.Sum(species => species.AboveGroundDeadN); }
        }

        /// <summary>Gets the amount of N in the plant below ground (kgN/ha).</summary>
        [Description("Amount of N in the plant below ground")]
        [Units("kg/ha")]
        public double BelowGroundN
        {
            get { return mySpecies.Sum(species => species.BelowGroundN); }
        }

        /// <summary>Gets the amount of N in standing herbage (kgN/ha).</summary>
        [Description("Amount of N in standing herbage")]
        [Units("kg/ha")]
        public double StandingHerbageN
        {
            get { return mySpecies.Sum(species => species.StandingHerbageN); }
        }

        /// <summary>Gets the amount of N in live standing herbage (kgN/ha).</summary>
        [Description("Amount of N in live standing herbage")]
        [Units("kg/ha")]
        public double StandingLiveHerbageN
        {
            get { return mySpecies.Sum(species => species.StandingLiveHerbageN); }
        }

        /// <summary>Gets the N content  of standing dead plant material (kg/ha).</summary>
        [Description("Amount of N in dead standing herbage")]
        [Units("kg/ha")]
        public double StandingDeadHerbageN
        {
            get { return mySpecies.Sum(species => species.StandingDeadHerbageN); }
        }

        /// <summary>Gets the amount of N in the plant's leaves (kgN/ha).</summary>
        [Description("Amount of N in the plant's leaves")]
        [Units("kg/ha")]
        public double LeafN
        {
            get { return mySpecies.Sum(species => species.LeafN); }
        }

        /// <summary>Gets the amount of N in the plant's stems and sheath (kgN/ha).</summary>
        [Description("Amount of N in the plant's stems and sheath")]
        [Units("kg/ha")]
        public double StemN
        {
            get { return mySpecies.Sum(species => species.StemN); }
        }

        /// <summary>Gets the amount of N in the plant's stolons (kgN/ha).</summary>
        [Description("Amount of N in the plant's stolons")]
        [Units("kg/ha")]
        public double StolonN
        {
            get { return mySpecies.Sum(species => species.StolonN); }
        }

        /// <summary>Gets the amount of N in the plant's roots (kgN/ha).</summary>
        [Description("Amount of N in the plant's roots")]
        [Units("kg/ha")]
        public double RootN
        {
            get { return mySpecies.Sum(species => species.RootN); }
        }

        ////- N concentration outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the average N concentration in the plant above ground (kgN/kgDM).</summary>
        [Description("Average N concentration in the plant above ground")]
        [Units("kg/kg")]
        public double AboveGroundNConc
        {
            get { return MathUtilities.Divide(AboveGroundN, AboveGroundWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in standing herbage (kgN/kgDM).</summary>
        [Description("Average N concentration in standing herbage")]
        [Units("kg/kg")]
        public double StandingHerbageNConc
        {
            get { return MathUtilities.Divide(StandingHerbageN, StandingHerbageWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's leaves (kgN/kgDM).</summary>
        [Description("Average N concentration in plant's leaves")]
        [Units("kg/kg")]
        public double LeafNConc
        {
            get { return MathUtilities.Divide(LeafN, LeafWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's stems (kgN/kgDM).</summary>
        [Description("Average N concentration in plant's stems")]
        [Units("kg/kg")]
        public double StemNConc
        {
            get { return MathUtilities.Divide(StemN, StemWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's stolons (kgN/kgDM).</summary>
        [Description("Average N concentration in plant's stolons")]
        [Units("kg/kg")]
        public double StolonNConc
        {
            get { return MathUtilities.Divide(StolonN, StolonWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's roots (kgN/kgDM).</summary>
        [Description("Average N concentration in plant's roots")]
        [Units("kg/kg")]
        public double RootNConc
        {
            get { return MathUtilities.Divide(RootN, RootWt, 0.0); }
        }

        ////- DM growth and senescence outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the gross potential growth rate (kgDM/ha).</summary>
        [Description("Gross potential growth rate")]
        [Units("kg/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return mySpecies.Sum(species => species.GrossPotentialGrowthWt); }
        }

        /// <summary>Gets the net potential growth rate, after respiration (kgDM/ha).</summary>
        [Description("Net potential growth rate, after respiration")]
        [Units("kg/ha")]
        public double NetPotentialGrowthWt
        {
            get { return mySpecies.Sum(species => species.NetPotentialGrowthWt); }
        }

        /// <summary>Gets the net potential growth rate after water stress (kgDM/ha).</summary>
        [Description("Net potential growth rate after water stress")]
        [Units("kg/ha")]
        public double NetPotentialGrowthAfterWaterWt
        {
            get { return mySpecies.Sum(species => species.NetPotentialGrowthAfterWaterWt); }
        }

        /// <summary>Gets the net potential growth rate after nutrient stress (kgDM/ha).</summary>
        [Description("Net potential growth rate after nutrient stress")]
        [Units("kg/ha")]
        public double NetPotentialGrowthAfterNutrientWt
        {
            get { return mySpecies.Sum(species => species.NetPotentialGrowthAfterNutrientWt); }
        }

        /// <summary>Gets the net, or actual, plant growth rate (kgDM/ha).</summary>
        [Description("Net, or actual, plant growth rate")]
        [Units("kg/ha")]
        public double NetGrowthWt
        {
            get { return mySpecies.Sum(species => species.NetGrowthWt); }
        }

        /// <summary>Gets the net herbage growth rate (above ground) (kgDM/ha).</summary>
        [Description("Net herbage growth rate (above ground)")]
        [Units("kg/ha")]
        public double HerbageGrowthWt
        {
            get { return mySpecies.Sum(species => species.HerbageGrowthWt); }
        }

        /// <summary>Gets the net root growth rate (kgDM/ha).</summary>
        [Description("Net root growth rate")]
        [Units("kg/ha")]
        public double RootGrowthWt
        {
            get { return mySpecies.Sum(species => species.RootGrowthWt); }
        }

        /// <summary>Gets the dry matter weight of detached dead material deposited onto soil surface (kgDM/ha).</summary>
        [Description("Dry matter weight of detached dead material deposited onto soil surface")]
        [Units("kg/ha")]
        public double LitterDepositionWt
        {
            get { return mySpecies.Sum(species => species.LitterDepositionWt); }
        }

        /// <summary>Gets the dry matter weight of detached dead roots added to soil FOM (kgDM/ha).</summary>
        [Description("Dry matter weight of detached dead roots added to soil FOM")]
        [Units("kg/ha")]
        public double RootDetachedWt
        {
            get { return mySpecies.Sum(species => species.RootDetachedWt); }
        }

        /// <summary>Gets the gross primary productivity (kgC/ha).</summary>
        [Description("Gross primary productivity")]
        [Units("kg/ha")]
        public double GPP
        {
            get { return mySpecies.Sum(species => species.GPP); }
        }

        /// <summary>Gets the net primary productivity (kgC/ha).</summary>
        [Description("Net primary productivity")]
        [Units("kg/ha")]
        public double NPP
        {
            get { return mySpecies.Sum(species => species.NPP); }
        }

        /// <summary>Gets the net above-ground primary productivity (kgC/ha).</summary>
        [Description("Net above-ground primary productivity")]
        [Units("kg/ha")]
        public double NAPP
        {
            get { return mySpecies.Sum(species => species.NAPP); }
        }

        /// <summary>Gets the net below-ground primary productivity (kgC/ha).</summary>
        [Description("Net below-ground primary productivity")]
        [Units("kg/ha")]
        public double NBPP
        {
            get { return mySpecies.Sum(species => species.NBPP); }
        }

        ////- N flows outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the amount of senesced N potentially remobilisable (kgN/ha).</summary>
        [Description("Amount of senesced N potentially remobilisable")]
        [Units("kg/ha")]
        public double RemobilisableSenescedN
        {
            get { return mySpecies.Sum(species => species.RemobilisableSenescedN); }
        }

        /// <summary>Gets the amount of senesced N actually remobilised (kgN/ha).</summary>
        [Description("Amount of senesced N actually remobilised")]
        [Units("kg/ha")]
        public double RemobilisedSenescedN
        {
            get { return mySpecies.Sum(species => species.RemobilisedSenescedN); }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable (kgN/ha).</summary>
        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kg/ha")]
        public double RemobilisableLuxuryN
        {
            get { return mySpecies.Sum(species => species.RemobilisableLuxuryN); }
        }

        /// <summary>Gets the amount of luxury N actually remobilised (kgN/ha).</summary>
        [Description("Amount of luxury N actually remobilised")]
        [Units("kg/ha")]
        public double RemobilisedLuxuryN
        {
            get { return mySpecies.Sum(species => species.RemobilisedLuxuryN); }
        }

        /// <summary>Gets the amount of atmospheric N fixed by symbiosis (kgN/ha).</summary>
        [Description("Amount of atmospheric N fixed by symbiosis")]
        [Units("kg/ha")]
        public double FixedN
        {
            get { return mySpecies.Sum(species => species.FixedN); }
        }

        /// <summary>Gets the amount of N required with luxury uptake (kgN/ha).</summary>
        [Description("Amount of N required with luxury uptake")]
        [Units("kg/ha")]
        public double DemandAtLuxuryN
        {
            get { return mySpecies.Sum(species => species.DemandAtLuxuryN); }
        }

        /// <summary>Gets the amount of N required for optimum growth (kgN/ha).</summary>
        [Description("Amount of N required for optimum growth")]
        [Units("kg/ha")]
        public double DemandAtOptimumN
        {
            get { return mySpecies.Sum(species => species.DemandAtOptimumN); }
        }

        /// <summary>Gets the amount of N demanded from the soil (kgN/ha).</summary>
        [Description("Amount of N demanded from the soil")]
        [Units("kg/ha")]
        public double SoilDemandN
        {
            get { return mySpecies.Sum(species => species.SoilDemandN); }
        }

        /// <summary>Gets the amount of plant available N in the soil (kgN/ha).</summary>
        [Description("Amount of plant available N in the soil")]
        [Units("kg/ha")]
        public double[] SoilAvailableN
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

        /// <summary>Gets the amount of N taken up from the soil (kgN/ha).</summary>
        [Description("Amount of N taken up from the soil")]
        [Units("kg/ha")]
        public double[] SoilUptakeN
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

        /// <summary>Gets the amount of N in detached dead material deposited onto soil surface (kgN/ha).</summary>
        [Description("Amount of N in detached dead material deposited onto soil surface")]
        [Units("kg/ha")]
        public double LitterDepositionN
        {
            get { return mySpecies.Sum(species => species.LitterDepositionN); }
        }

        /// <summary>Gets the amount of N in detached dead roots added to soil FOM (kgN/ha).</summary>
        [Description("Amount of N in detached dead roots added to soil FOM")]
        [Units("kg/ha")]
        public double RootDetachedN
        {
            get { return mySpecies.Sum(species => species.RootDetachedN); }
        }

        /// <summary>Gets the amount of N in new growth (kgN/ha).</summary>
        [Description("Amount of N in new growth")]
        [Units("kg/ha")]
        public double NetGrowthN
        {
            get { return mySpecies.Sum(species => species.NetGrowthN); }
        }

        ////- Water related outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the amount of water demanded by the plant (mm).</summary>
        [Description("Amount of water demanded by the plant")]
        [Units("mm")]
        public double WaterDemand
        {
            get { return mySpecies.Sum(species => species.WaterDemand); }
        }

        /// <summary>Gets the amount of plant available water in each soil layer (mm).</summary>
        [Description("Amount of plant available water in each soil layer")]
        [Units("mm")]
        public double[] WaterAvailable
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = mySpecies.Sum(species => species.WaterAvailable[layer]);
                return result;
            }
        }

        /// <summary>Gets the amount of water taken up from each soil layer (mm).</summary>
        [Description("Amount of water taken up from each soil layer")]
        [Units("mm")]
        public double[] WaterUptake
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = mySpecies.Sum(species => species.WaterUptake[layer]);
                return result;
            }
        }

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the growth factor due to variations in intercepted radiation (0-1).</summary>
        [Description("Growth factor due to variations in intercepted radiation")]
        [Units("0-1")]
        public double GlfRadnIntercept
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfRadnIntercept * species.GrossPotentialGrowthWt), GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth factor due to variations in atmospheric CO2 (0-1).</summary>
        [Description("Growth factor due to variations in atmospheric CO2")]
        [Units("0-1")]
        public double GlfCO2
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfCO2 * species.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth factor due to variations in plant N concentration (0-1).</summary>
        [Description("Growth factor due to variations in plant N concentration")]
        [Units("0-1")]
        public double GlfNContent
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfNContent * species.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth factor due to variations in air temperature (0-1).</summary>
        [Description("Growth factor due to variations in air temperature")]
        [Units("0-1")]
        public double GlfTemperature
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfTemperature * species.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth factor due to heat damage stress (0-1).</summary>
        [Description("Growth factor due to heat damage stress")]
        [Units("0-1")]
        public double GlfHeatDamage
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfHeatDamage * species.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth factor due to cold damage stress (0-1).</summary>
        [Description("Growth factor due to cold damage stress")]
        [Units("0-1")]
        public double GlfColdDamage
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfColdDamage * species.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the generic factor affecting potential plant growth [0-1]: (0-1).</summary>
        [Description("Generic factor affecting potential plant growth [0-1]:")]
        [Units("0-1")]
        public double GlfGeneric
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfGeneric * species.GrossPotentialGrowthWt),
                    GrossPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to water deficit (0-1).</summary>
        [Description("Growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GlfWaterSupply
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfWaterSupply * species.NetPotentialGrowthWt),
                    NetPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to water logging (0-1).</summary>
        [Description("Growth limiting factor due to water logging")]
        [Units("0-1")]
        public double GlfWaterLogging
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfWaterLogging * species.NetPotentialGrowthWt),
                    NetPotentialGrowthWt, 0.0);
            }
        }

        /// <summary>Gets the growth limiting factor due to soil N availability (0-1).</summary>
        [Description("Growth limiting factor due to soil N availability")]
        [Units("0-1")]
        public double GlfNSupply
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfNSupply * species.NetPotentialGrowthAfterWaterWt),
                    NetPotentialGrowthAfterWaterWt, 0.0);
            }
        }

        /// <summary>Gets the generic growth limiting factor due to soil fertility [0-1]: (0-1).</summary>
        [Description("Generic growth limiting factor due to soil fertility [0-1]:")]
        [Units("0-1")]
        public double GlfSoilFertility
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.GlfSoilFertility * species.NetPotentialGrowthAfterWaterWt),
                    NetPotentialGrowthAfterWaterWt, 0.0);
            }
        }

        ////- LAI and cover outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the leaf area index of green tissues (m^2/m^2).</summary>
        [Description("Leaf area index of green tissues")]
        [Units("m^2/m^2")]
        public double LAIGreen
        {
            get { return mySpecies.Sum(species => species.LAIGreen); }
        }

        /// <summary>Gets the leaf area index of dead tissues (m^2/m^2).</summary>
        [Description("Leaf area index of dead tissues")]
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get { return mySpecies.Sum(species => species.LAIDead); }
        }

        /// <summary>Gets the total leaf area index (m^2/m^2).</summary>
        [Description("Total leaf area index")]
        [Units("m^2/m^2")]
        public double LAITotal
        {
            get { return LAIGreen + LAIDead; }
        }

        /// <summary>Gets the fraction of soil covered by green tissues (0-1).</summary>
        [Description("Fraction of soil covered by green tissues")]
        [Units("%")]
        public double CoverGreen
        {
            get
            {
                if (LAIGreen < Epsilon) return 0.0;
                return 1.0 - Math.Exp(-LightExtinctionCoefficient * LAIGreen);
            }
        }

        /// <summary>Gets the fraction of soil covered by dead tissues (0-1).</summary>
        [Description("Fraction of soil covered by dead tissues")]
        [Units("%")]
        public double CoverDead
        {
            get
            {
                if (LAIDead < Epsilon) return 0.0;
                return 1.0 - Math.Exp(-LightExtinctionCoefficient * LAIDead);
            }
        }

        /// <summary>Gets the fraction of soil covered by plant tissues (0-1).</summary>
        [Description("Fraction of soil covered by plant tissues")]
        [Units("%")]
        public double CoverTotal
        {
            get
            {
                if (LAITotal < Epsilon) return 0.0;
                return 1.0 - Math.Exp(-LightExtinctionCoefficient * LAITotal);
            }
        }

        /// <summary>Gets the light extinction coefficient [0-1] (0-1).</summary>
        [Description("Light extinction coefficient [0-1]")]
        [Units("0-1")]
        public double LightExtinctionCoefficient
        {
            get
            {
                double result = mySpecies.Sum(species => species.LAITotal * species.LightExtinctionCoefficient);
                result /= mySpecies.Sum(species => species.LAITotal);

                return result;
            }
        }

        ////- Height, root depth and distribution >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the average canopy height (mm).</summary>
        [Description("Average canopy height")]
        [Units("mm")]
        public double Height
        {
            get
            {
                double result = 0.0;
                if (StandingHerbageWt > 0.0)
                {
                    for (int s = 0; s < numSpecies; s++)
                        result += mySpecies[s].Height * mySpecies[s].StandingHerbageWt;

                    result /= StandingHerbageWt;
                }

                return result;
            }
        }

        /// <summary>Gets the average depth of root zone (mm).</summary>
        [Description("Average depth of root zone")]
        [Units("mm")]
        public double RootZoneDepth
        {
            get { return mySpecies.Max(species => species.RootDepth); }
        }

        /// <summary>Gets the layer at bottom of root zone (-).</summary>
        [Description("Layer at bottom of root zone")]
        [Units("-")]
        public int RootZoneFrontier
        {
            get { return mySpecies.Max(species => species.RootFrontier); }
        }

        /// <summary>Gets the fraction of root dry matter for each soil layer (0-1).</summary>
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
                        result[layer] = mySpecies.Sum(species => species.RootWt * species.RootWtFraction[layer]) / RootWt;
                return result;
            }
        }

        /// <summary>Gets the root length density by volume (mm/mm^3).</summary>
        [Description("Root length density by volume")]
        [Units("mm/mm^3")]
        public double[] RootLengthDensity
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = mySpecies.Sum(species => species.RootLengthDensity[layer]);
                return result;
            }
        }

        ////- Harvest outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the dry matter weight available for harvesting (kgDM/ha).</summary>
        [Description("Dry matter weight available for harvesting")]
        [Units("kg/ha")]
        public double HarvestableWt
        {
            get { return mySpecies.Sum(species => species.HarvestableWt); }
        }

        /// <summary>Gets the amount of plant dry matter removed by harvest (kgDM/ha).</summary>
        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kg/ha")]
        public double HarvestedWt
        {
            get { return mySpecies.Sum(species => species.HarvestedWt); }
        }

        /// <summary>Gets the amount of plant N removed by harvest (kgN/ha).</summary>
        [Description("Amount of plant N removed by harvest")]
        [Units("kg/ha")]
        public double HarvestedN
        {
            get { return mySpecies.Sum(species => species.HarvestedN); }
        }

        /// <summary>Gets the average N concentration in harvested material (kgN/kgDM).</summary>
        [Description("Average N concentration in harvested material")]
        [Units("kg/kg")]
        public double HarvestedNConc
        {
            get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
        }

        /// <summary>Gets the average digestibility of harvested material (0-1).</summary>
        [Description("Average digestibility of harvested material")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get
            {
                return MathUtilities.Divide(mySpecies.Sum(species => species.HarvestedDigestibility * species.HarvestedWt), HarvestedWt, 0.0);
            }
        }

        /// <summary>Gets the average metabolisable energy concentration of harvested material (MJ/kgDM).</summary>
        [Description("Average metabolisable energy concentration of harvested material")]
        [Units("MJ/kg")]
        public double HarvestedME
        {
            get { return MathUtilities.Divide(mySpecies.Sum(species => species.HarvestedME * species.HarvestedWt), HarvestedWt, 0.0); }
        }

        /// <summary>Gets the average digestibility of standing herbage (0-1).</summary>
        [Description("Average digestibility of standing herbage")]
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get { return MathUtilities.Divide(mySpecies.Sum(species => species.HerbageDigestibility * species.StandingHerbageWt), StandingHerbageWt, 0.0); }
        }

        /// <summary>Gets the average metabolisable energy concentration of standing herbage (MJ/kgDM).</summary>
        [Description("Average metabolisable energy concentration of standing herbage")]
        [Units("MJ/kg")]
        public double HerbageME
        {
            get { return MathUtilities.Divide(mySpecies.Sum(species => species.HerbageME * species.StandingHerbageWt), StandingHerbageWt, 0.0); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Initialisation methods  ------------------------------------------------------------------------------------

        /// <summary>Called when model has been created.</summary>
        public override void OnCreated()
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
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // check whether uptake is controlled by the sward or by species
            myWaterUptakeSource = "species";
            myNUptakeSource = "species";

            double totalInitalDM = 0.0;
            foreach (PastureSpecies species in mySpecies)
            {
                species.isSwardControlled = isSwardControlled;
                species.MyWaterUptakeSource = myWaterUptakeSource;
                species.MyNitrogenUptakeSource = myNUptakeSource;
                totalInitalDM += species.InitialShootDM;
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

            // check that there are plants alive
            if (totalInitalDM > 0.0)
                swardIsAlive = true;
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Daily processes  -------------------------------------------------------------------------------------------

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/>instance containing the event data</param>
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
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/>instance containing the event data</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (swardIsAlive && isSwardControlled)
            {
                foreach (PastureSpecies species in mySpecies)
                {
                    if (species.Stage == 0)
                    {
                        // plant has not emerged yet, check germination progress
                        if (species.DailyGerminationProgress() >= 1.0)
                        {
                            // germination completed
                            species.SetEmergenceState();
                        }
                    }
                    else
                    {
                        // Evaluate tissue turnover and get remobilisation (C and N)
                        species.EvaluateTissueTurnoverRates();

                        // Get the potential gross growth
                        species.CalcDailyPotentialGrowth();

                        // Evaluate potential allocation of today's growth
                        species.GetAllocationFractions();

                        // Get the potential growth after water limitations
                        species.CalcGrowthAfterWaterLimitations();

                        // Get the N amount demanded for optimum growth and luxury uptake
                        species.EvaluateNitrogenDemand();
                    }
                }
            }
        }

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/>instance containing the event data</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
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
                    species.DoActualGrowthAndAllocation();
                }

                // Send detached tissues (litter and roots) to other modules
                DoAddDetachedShootToSurfaceOM(LitterDepositionWt, LitterDepositionN);
                DoAddDetachedRootToSoilFOM(RootDetachedWt, RootDetachedN);
            }
        }

        #region - Nitrogen uptake processes - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Performs the nitrogen uptake calculations.</summary>
        private void DoNitrogenCalculations()
        {
            if (myNUptakeSource.ToLower() == "sward")
            {
                throw new NotImplementedException();
            }
            else
            {
                //N uptake is controlled at species level, get sward totals
                for (int layer = 0; layer < RootZoneFrontier; layer++)
                {
                    foreach (PastureSpecies species in mySpecies)
                    {
                        swardSoilNH4Available[layer] += species.SoilNH4Available[layer];
                        swardSoilNO3Available[layer] += species.SoilNO3Available[layer];
                        swardSoilNH4Uptake[layer] += species.SoilNH4Uptake[layer];
                        swardSoilNO3Uptake[layer] += species.SoilNO3Uptake[layer];
                    }
                }
            }
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region - Organic matter processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Adds a given amount of detached plant material (DM and N) to the surface organic matter.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        private void DoAddDetachedShootToSurfaceOM(double amountDM, double amountN)
        {
            if (BiomassRemoved != null)
            {
                BiomassRemovedType biomassData = new BiomassRemovedType();
                string[] type = {"grass"}; // TODO: this should be "pasture" ??
                float[] dltdm = {(float) amountDM};
                float[] dltn = {(float) amountN};
                float[] dltp = {0f}; // P not considered here
                float[] fraction = {1f}; // fraction is always 1.0 here

                biomassData.crop_type = "grass"; //TODO: this could be the Name, what is the diff between name and type??
                biomassData.dm_type = type;
                biomassData.dlt_crop_dm = dltdm;
                biomassData.dlt_dm_n = dltn;
                biomassData.dlt_dm_p = dltp;
                biomassData.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(biomassData);
            }
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the soil's FOM pool.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        private void DoAddDetachedRootToSoilFOM(double amountDM, double amountN)
        {
            FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
            {
                FOMType fomData = new FOMType();
                fomData.amount = amountDM * RootWtFraction[layer];
                fomData.N = amountN * RootWtFraction[layer];
                fomData.C = amountDM * RootWtFraction[layer] * CarbonInDM;
                fomData.P = 0.0; // P not considered here
                fomData.AshAlk = 0.0; // Ash not considered here

                FOMLayerLayerType layerData = new FOMLayerLayerType();
                layerData.FOM = fomData;
                layerData.CNR = 0.0; // not used here
                layerData.LabileP = 0; // not used here

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

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Intermittent processes  ------------------------------------------------------------------------------------

        /// <summary>Kills a fraction of all plants in the sward.</summary>
        /// <param name="fractionToKill">The fraction of crop to kill (0-1)</param>
        public void KillCrop(double fractionToKill)
        {
            foreach (PastureSpecies species in mySpecies)
                species.KillCrop(fractionToKill);
        }

        /// <summary>Removes plant material simulating a graze event.</summary>
        /// <param name="amount">The DM amount (kg/ha)</param>
        /// <param name="type">How the amount is interpreted (SetResidueAmount or SetRemoveAmount)</param>
        public void Graze(double amount, string type)
        {
            double amountAvailable = HarvestableWt;
            if (swardIsAlive || (amountAvailable > Epsilon))
            {
                // Get the amount required to remove
                double amountRequired;
                if (type.ToLower() == "setresidueamount")
                {
                    // Remove all DM above given residual amount
                    amountRequired = Math.Max(0.0, StandingHerbageWt - amount);
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
                double amountToRemove = Math.Max(0.0, Math.Min(amountRequired, amountAvailable));

                // Get the amounts to remove by mySpecies:
                if (amountToRemove > Epsilon)
                {
                    double[] fractionToRemove = new double[numSpecies];
                    for (int s = 0; s < numSpecies; s++)
                    {
                        // get the fraction to required for each mySpecies, partition according to available DM to harvest
                        fractionToRemove[s] = mySpecies[s].HarvestableWt / amountAvailable;

                        // remove DM and N for each mySpecies (digestibility is also evaluated)
                        mySpecies[s].RemoveDM(amountToRemove * fractionToRemove[s]);
                    }
                }
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------
    }
}
