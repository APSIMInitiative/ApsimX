using System;
using System.Collections.Generic;

namespace Models
{
    using System.Xml.Serialization;
    using System.Reflection;
    using Models.Core;
    using Models.Soils.Arbitrator;
    using Models.Interfaces;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Implements the plant growth model logic abstracted from G_Range
    /// Currently this is just an empty stub
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public partial class G_Range : Model, IPlant, ICanopy, IUptake
    {
        #region Links

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary summary = null;

        [Link]
        private Clock Clock = null;

        [Link]
        private IWeather Weather = null;

        //[Link]
        //private MicroClimate MicroClim; //added for fr_intc_radn_ , but don't know what the corresponding variable is in MicroClimate.

        //[Link]
        //private ISummary Summary = null;

        //[Link]
        //Soils.Soil Soil = null;

        //[ScopedLinkByName]
        //private ISolute NO3 = null;

        //[ScopedLinkByName]
        //private ISolute NH4 = null;

        #endregion

        #region IPlant interface

        /// <summary>Gets a value indicating how leguminous a plant is</summary>
        public double Legumosity { get { return parms.maxSymbioticNFixationRatio * 100.0; } }  // Very ad hod.

        /// <summary>Gets a value indicating whether the biomass is from a c4 plant or not</summary>
        public bool IsC4 { get { return Math.Abs(Latitude) < 30.0 ; } } // Treat low latitudes as C4

        /// <summary> Is the plant alive?</summary>
        public bool IsAlive { get { return totalAgroundLiveBiomass > 0.0; } }

        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames { get { return null; } }

        /// <summary>Get above ground biomass</summary>
        public PMF.Biomass AboveGround { get { return new PMF.Biomass(); } }

        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        /// <param name="rowConfig">The bud number.</param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1, double rowConfig = 1) { }

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        public bool IsReadyForHarvesting { get { return false; } }

        /// <summary>Harvest the crop</summary>
        public void Harvest() { }

        /// <summary>End the crop</summary>
        public void EndCrop() { }

        #endregion

        #region ICanopy interface

        /// <summary>Albedo.</summary>
        public double Albedo { get { return 0.15; } }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax { get { return 0.01; } }

        /// <summary>Gets or sets the R50.</summary>
        public double R50 { get { return 200; } }

        /// <summary>Gets the LAI</summary>
        [Description("Leaf Area Index (m^2/m^2)")]
        [Units("m^2/m^2")]
        public double LAI { get { return 1.7; } }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI; } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen { get { return Math.Min(1.0 - Math.Exp(-0.5 * LAI), 0.999999999); } }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal { get { return 1.0 - (1 - CoverGreen) * (1 - 0); } }

        /// <summary>Gets the canopy height (mm)</summary>
        [Units("mm")]
        public double Height { get; set; }

        /// <summary>Gets the canopy depth (mm)</summary>
        [Units("mm")]
        public double Depth { get { return Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Gets the canopy depth (mm)</summary>
        [Units("mm")]
        public double Width { get { return 0.0; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [XmlIgnore]
        public double PotentialEP { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        [XmlIgnore]
        [Units("mm")]
        public double WaterDemand { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        #endregion

        #region IUptake

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            return null; // Needs to be implemented
        }

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public List<ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            return null; // Needs to be implemented
        }

        /// <summary>
        /// Set the sw uptake for today.
        /// </summary>
        public void SetActualWaterUptake(List<ZoneWaterAndN> info)
        {
            // Needs to be implemented
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetActualNitrogenUptakes(List<ZoneWaterAndN> info)
        {
            // Needs to be implemented
        }

        #endregion

        /// <summary>Constructor</summary>
        public G_Range()
        {
            Name = "G_Range";
        }

        #region Elements from Fortran code

        // I've pulled in all the members of the original G-Range RangeCell structure, but not all are used (yet)
        // These pragmas disable warnings related to the declaration of unused variables.

        // Once the entire model has been transcoded, these pragmas should be remove (and the associated warnings dealt with appropriately)

        //#pragma warning disable 0414

        //#pragma warning disable 0169

        //#pragma warning disable 0649

        /// <summary>
        ///  Stores parameters unique to each landscape unit
        /// </summary>
        private class UnitParm
        {
        }

        /// <summary>
        /// Vegetation layer constants
        /// </summary>
        public enum Layer
        {
            /// <summary>
            /// Herb layer index (H_LYR)
            /// </summary>
            herb = 0, // Was 1 in Fortran!
            /// <summary>
            /// Herbs under shrubs layer index (H_S_LYR)
            /// </summary>
            herbUnderShrub,
            /// <summary>
            /// Herbs under tree layer index (H_T_LYR)
            /// </summary>
            herbUnderTree,
            /// <summary>
            /// Shrub layer index (S_LYR)
            /// </summary>
            shrub,
            /// <summary>
            /// Shrub under tree layer index (S_T_LYR)
            /// </summary>
            shrubUnderTree,
            /// <summary>
            /// Tree layer index (T_LYR)
            /// </summary>
            tree
        };

        /// <summary>
        /// Total number of layers (V_LYRS)
        /// </summary>
        private const int nLayers = 6;

        /// <summary>
        /// Vegetation facet constants.  Facets are used for unit input, so that users provide 3 values, rather than 6
        /// </summary>
        public enum Facet
        {
            /// <summary>
            /// Herb layer index (H_FACET)
            /// </summary>
            herb = 0,  // Was 1 in Fortran!
            /// <summary>
            /// Shrub facet index (S_FACET)
            /// </summary>
            shrub,
            /// <summary>
            /// Herbs under tree layer index (T_FACET)
            /// </summary>
            tree
        };
        /// <summary>
        /// Total number of facets (FACETS)
        /// </summary>
        private const int nFacets = 3;

        /// <summary>
        /// Woody part constants
        /// Woody parts, leaf, fine root, fine branch, large wood, and coarse root    
        /// </summary>
        public enum WoodyPart
        {
            /// <summary>
            /// LEAF_INDEX
            /// </summary>
            leaf = 0, // Was 1 in Fortran!
            /// <summary>
            /// FINE_ROOT_INDEX
            /// </summary>
            fineRoot,
            /// <summary>
            /// FINE_BRANCH_INDEX
            /// </summary>
            fineBranch,
            /// <summary>
            /// COARSE_BRANCH_INDEX
            /// </summary>
            coarseBranch,
            /// <summary>
            /// COARSE_ROOT_INDEX
            /// </summary>
            coarseRoot
        };

        /// <summary>
        /// Total number of woody parts (WOODY_PARTS)
        /// </summary>
        private const int nWoodyParts = 5;

        // Soil and layer constants
        private const int nSoilLayers = 4;                             // The number of soil layers (SOIL_LAYERS)

        private const int surfaceIndex = 0;                            // Surface index in litter array and perhaps elsewhere (SURFACE_INDEX) [Was 1 in Fortran]
        private const int soilIndex = 1;                               // Soil index in litter array and perhaps (SOIL_INDEX) [Was 2 in Fortran]

        private const int fireSeverities = 2;                          // The number of fire severities used
        private const double refArea = 1000000.0;                      // A 1 km x 1 km square area, in meters.

        private const int ALIVE = 0;                                   // Plant material that is alive, index [Was 1 in Fortran]
        private const int DEAD = 1;                                    // Plant material that is dead, index [Was 2 in Fortran]

        private const int ABOVE = 0;                                   // Aboveground index [Was 1 in Fortran]
        private const int BELOW = 1;                                   // Belowground index [Was 2 in Fortran]

        private const int N_STORE = 0;                                 // Element to storage, using in Growth, etc.
        private const int N_SOIL = 1;                                  // Element to soil, used in Growth, etc.
        private const int N_FIX = 2;                                   // Element to fix, used in Growth, etc.

        private const double baseTemp = 4.4;
        private const double vLarge = 1000000000.0;
        static private readonly int[] julianDayMid = new int[] { 16, 46, 75, 106, 136, 167, 197, 228, 259, 289, 320, 350 };  // The Julian day at the middle of each month.
        static private readonly int[] julianDayStart = new int[] { 1, 32, 61, 92, 122, 153, 183, 214, 245, 275, 306, 337 };  // The Julian day at the start of each month.

        /// <summary>
        /// X dimension of rangeland cell
        /// </summary>
        public int X;

        /// <summary>
        /// Y dimension of rangeland cell
        /// </summary>
        public int Y;

        private int month;

        /// <summary>
        /// Identifier storing the type of rangeland cell, used as a key to the Parms strcuture
        /// </summary>
        public int rangeType { get; private set; }

        private double lastMonthDayLength;     // The day length of the previous month, to know when spring and fall come.
        private bool dayLengthIncreasing;      // Increasing or decreasing day length, comparing the current to previous day lengths.
        private bool inWinter = false;         // NOT ORIGINALLY PART OF THE CELL STRUCTURE - It used a Fortran "SAVE" in Plant_Death.95

        /// <summary>
        /// Day length, calculated based on latitude and month
        /// </summary>
        [Units("hr")]
        public double dayLength { get; private set; }

        /// <summary>
        /// Heat accumulation above a base temperature (e.g., 4.4 C in Boone (1999))
        /// </summary>
        public double heatAccumulation { get; private set; }

        /// <summary>
        /// The proportion occupied by each facet
        /// </summary>
        [Units("0-1")]
        public double[] facetCover { get; private set; } = new double[nFacets];

        /// <summary>
        /// The total population of each vegetation layer
        /// </summary>
        public double[] totalPopulation { get; private set; } = new double[nLayers]; 

        /// <summary>
        /// Bare cover stored, rather than continually summing the three facets. 
        /// </summary>
        [Units("0-1")]
        public double bareCover { get; private set; }

        /// <summary>
        /// Proportion of facet that is annual plants (H_FACET) or deciduous (S_FACET and T_FACET)
        /// </summary>
        [Units("0-1")]
        public double[] propAnnualDecid { get; private set; } = new double[nFacets];

        /// <summary>
        /// Precipitation total for the current month
        /// Exposed here for reporting purposes
        /// </summary>
        [Units("cm")]
        public double precip { get { return globe.precip; } }

        /// <summary>
        /// Average maximum temperature for the current month 
        /// Exposed here for reporting purposes
        /// </summary>
        [Units("C")]
        public double maxTemp { get { return globe.maxTemp; } }

        /// <summary>
        /// Average minimum temperature for the current month 
        /// Exposed here for reporting purposes
        /// </summary>
        [Units("C")]
        public double minTemp { get { return globe.minTemp; } }

        /// <summary>
        /// Potential evapotranspiration for the cell(cm/month)
        /// </summary>
        [Units("cm/month")]
        public double potEvap { get; private set; }

        /// <summary>
        /// Water evaporated from the soil and vegetation(cm/month)
        /// </summary>
        [Units("cm/month")]
        public double evaporation { get; private set; }

        /// <summary>
        /// Snowpack, in cm
        /// </summary>
        [Units("cm")]
        public double snow { get; private set; }

        /// <summary>
        /// Snowpack liquid water.
        /// </summary>
        [Units("cm")]
        public double snowLiquid { get; private set; }

        // Editing the model 01/02/2014 to prevent snow and snow liquid from skyrocketing.   Adding an field for OLD SNOW and ICE, prior to clearing out snow each year.
        private double oldSnow;                // Snow from past years, including glacial build up.   This will be an accumulator, but essentially outside of active process modeling
        private double oldSnowLiquid;          // Ditto
                                               // This won't be output until there is some need for it.
                                               // End of addition

        /// <summary>
        /// Snow that melts from snowpack (cm water)
        /// </summary>
        [Units("cm")]
        public double melt { get; private set; }

        /// <summary>
        /// Potential evaporation decremented as steps are calculated.Appears to be a bookkeeping tool.
        /// </summary>
        [Units("cm")]
        public double petRemaining { get; private set; }

        /// <summary>
        /// Precipitation adjusted for snow accumulation and melt, and available to infiltrate the soil (cm)
        /// </summary>
        [Units("cm")]
        public double pptSoil { get; private set; }

        /// <summary>
        /// Runoff from the rangeland cell
        /// </summary>
        public double runoff { get; private set; }

        /// <summary>
        /// Ratio of available water to potential evapotranspiration
        /// </summary>
        public double ratioWaterPet { get; private set; }

        /// <summary>
        /// Potential evaporation from top soil (cm/day)
        /// </summary>
        [Units("cm/d")]
        public double petTopSoil { get; private set; }

        /// <summary>
        /// Nitrogen leached from soil(AMTLEA in Century)
        /// </summary>
        [Units("g/m^2")]
        public double[] nLeached { get; private set; } = new double[nSoilLayers]; 

        private double[] asmos = new double[nSoilLayers];     // Used in summing water
        private double[] amov = new double[nSoilLayers];      // Used in summing water movement
        private double stormFlow;              // Storm flow
        private double holdingTank;            // Stores water temporarily. Was asmos(layers+1) in H2OLos

        /// <summary>
        /// Transpiration water loss
        /// </summary>
        public double transpiration { get; private set; }

        private double[] relativeWaterContent = new double[nSoilLayers]; // Used to initialize and during simulation in CENTURY.Here, only during simulation

        /// <summary>
        /// Water available to plants, available for growth =(1) [0 in C#], survival(2) [1 in C#], and in the two top layers(3) [2 in C#]
        /// </summary>
        public double[] waterAvailable { get; private set; }  = new double[3]; 

        /// <summary>
        /// Annual actual evapotranspiration
        /// </summary>
        [Units("cm")]
        public double annualEvapotranspiration { get; private set; }

        /// <summary>
        /// Total aboveground live biomass (g/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double totalAgroundLiveBiomass { get; private set; }

        /// <summary>
        /// Total belowground live biomass (g/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double totalBgroundLiveBiomass { get; private set; }

        /// <summary>
        /// Average monthly litter carbon(g/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] totalLitterCarbon { get; private set; } = new double[2];

        /// <summary>
        /// Average monthly litter carbon(g/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] totalLitterNitrogen { get; private set; } = new double[2];

        /// <summary>
        /// Root shoot ratio
        /// </summary>
        public double[] rootShootRatio { get; private set; } = new double[nFacets];

        /// <summary>
        /// Basal area for trees
        /// </summary>
        [Units("m^2")]
        public double treeBasalArea { get; private set; }

        /// <summary>
        /// Average soil surface temperature (C)
        /// </summary>
        [Units("C")]
        public double soilSurfaceTemperature { get; private set; }

        // Soils as in Century 4.5 NLayer= 4, 0-15, 15-30, 30-45, 45-60 cm.
        // These will be initialized using approximations and weighted averages from HWSD soils database, which is 0-30 for TOP, 30-100 for SUB.
        /// <summary>
        /// The percent sand in the soil
        /// </summary>
        [Units("0-1")]
        public double[] sand { get; private set; } = new double[nSoilLayers];

        /// <summary>
        /// The percent silt in the soil
        /// </summary>
        [Units("0-1")]
        public double[] silt { get; private set; } = new double[nSoilLayers];

        /// <summary>
        /// The percent clay in the soil
        /// </summary>
        [Units("0-1")]
        public double[] clay { get; private set; } = new double[nSoilLayers];

        /// <summary>
        /// Mineral nitrogen content for layer(g/m2)
        /// </summary>
        [Units("g/m^2")]
        public double[] mineralNitrogen { get; private set; } = new double[nSoilLayers];

        private double[] soilDepth = new double[nSoilLayers] { 15.0, 15.0, 15.0, 15.0 }; // The depth of soils, in cm.Appears hardwired in some parts of CENTURY, flexible, and up to 9 layers, in other parts of CENTURY.Likely I noted some values from an early version, but this is a simplification, so...

        /// <summary>
        /// Field capacity for four soils layers shown above.
        /// </summary>
        public double[] fieldCapacity { get; private set; } = new double[nSoilLayers];

        /// <summary>
        /// Wilting point for four soil layers shown above.
        /// </summary>
        public double[] wiltingPoint { get; private set; } = new double[nSoilLayers];

        /// <summary>
        /// grams per square meter
        /// </summary>
        [Units("g/m^2")]
        public double soilTotalCarbon { get; private set; }

        /// <summary>
        /// Tree carbon in its components.These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        public double[] treeCarbon { get; private set; } = new double[nWoodyParts];

        /// <summary>
        /// Tree nitrogen in its components.   These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        public double[] treeNitrogen { get; private set; }  = new double[nWoodyParts];

        /// <summary>
        /// Shrub carbon in its components.   These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        public double[] shrubCarbon { get; private set; } = new double[nWoodyParts];

        /// <summary>
        /// Shrub nitrogen in its components.   These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        public double[] shrubNitrogen { get; private set; } = new double[nWoodyParts];

        /// <summary>
        /// Carbon to nitrogen ratio, SURFACE, SOIL
        /// </summary>
        public double[] carbonNitrogenRatio { get; private set; } = new double[2]; 

        /// <summary>
        /// Soil organic matter carbon, surface and soil  g/m2(SOM1C in Century)
        /// </summary>
        [Units("g/m^2")]
        public double[] fastSoilCarbon { get; private set; } = new double[2];

        /// <summary>
        /// Intermediate soil carbon g/m2(SOMC2 in Century)
        /// </summary>
        [Units("g/m^2")]
        public double intermediateSoilCarbon { get; private set; }

        /// <summary>
        /// Passive soil carbon g/m2(SOMC3 in Century)
        /// </summary>
        [Units("g/m^2")]
        public double passiveSoilCarbon { get; private set; }

        /// <summary>
        /// Soil organic matter nitrogen, surface and soil  g/m2(SOM1E in Century and SSOM1E in Savanna)
        /// </summary>
        [Units("g/m^2")]
        public double[] fastSoilNitrogen { get; private set; } = new double[2];

        /// <summary>
        /// Intermediate soil nitrogen g/m2(SOM2E in Century)
        /// </summary>
        [Units("g/m^2")]
        public double intermediateSoilNitrogen { get; private set; }

        /// <summary>
        /// Passive soil nitrogen g/m2(SOM3E in Century)
        /// </summary>
        [Units("g/m^2")]
        public double passiveSoilNitrogen { get; private set; }

        /// <summary>
        /// Calculated potential production for the cell, an index.Based on soil temperature, so not specific to facets.
        /// </summary>
        [Units("0-1")]
        public double potentialProduction { get; private set; }

        /// <summary>
        /// BIOMASS, Belowground potential production in g/m2
        /// </summary>
        [Units("g/m^2")]
        public double[] belowgroundPotProduction { get; private set; }  = new double[nLayers];

        /// <summary>
        /// BIOMASS, Aboveground potential production in g/m2
        /// </summary>
        [Units("g/m^2")]
        public double[] abovegroundPotProduction { get; private set; } = new double[nLayers];

        /// <summary>
        /// BIOMASS, Calculate total potential production, in g/m2 with all the corrections in place. 
        /// </summary>
        [Units("g/m^2")]
        public double[] totalPotProduction { get; private set; }  = new double[nLayers];

        /// <summary>
        /// Calculated effect of CO2 increasing from 350 to 700 ppm on grassland production, per facet
        /// </summary>
        public double[] co2EffectOnProduction { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coefficient on total potential production reflecting limits due to nitrogen in place(EPRODL)
        /// </summary>
        [Units("g/m^2")]
        public double[] totalPotProdLimitedByN { get; private set; } = new double[nLayers];

        /// <summary>
        /// Monthly net primary production in g/m2, summed from total_pot_prod_limited_by_n
        /// </summary>
        [Units("g/m^2")]
        public double monthlyNetPrimaryProduction { get; private set; }

        /// <summary>
        /// Fraction of live forage removed by grazing  (FLGREM in CENTURY)
        /// </summary>
        [Units("0-1")]
        public double fractionLiveRemovedGrazing { get; private set; }

        /// <summary>
        /// Fraction of dead forage removed by grazing(FDGREM in CENTURY)
        /// </summary>
        [Units("0-1")]
        public double fractionDeadRemovedGrazing { get; private set; }

        // Facets are used here.Facets are: 1 - Herb, 2 - Shrub, 3 - Tree
        // NOT USED RIGHT NOW:  The array index here is:  1 - Phenological death, 2 - Incremental death, 3 - Herbivory, 4 - Fire

        /// <summary>
        /// Temperature effect on decomposition (TFUNC in CENTURY Cycle.f)  (index)
        /// </summary>
        public double tempEffectOnDecomp { get; private set; }

        /// <summary>
        /// Water effect on decomposition (index)  (Aboveground and belowground entries in CENTURY set to equal, so distinction not made here)
        /// </summary>
        public double waterEffectOnDecomp { get; private set; }

        /// <summary>
        /// Anerobic effects on decomposition(index)  (EFFANT in Savanna)
        /// </summary>
        public double anerobicEffectOnDecomp { get; private set; }

        /// <summary>
        /// Combined effects on decomposition, which in Savanna includes anerobic(CYCLE.F)  (index)
        /// </summary>
        public double allEffectsOnDecomp { get; private set; }

        /// <summary>
        /// Dead fine root carbon of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadFineRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine root nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadFineRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Standing dead carbon of leaf and stem, of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadStandingCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Standing dead nitrogen of leaf and stem, of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadStandingNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead seed carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] deadSeedCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead seed nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadSeedNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead leaf carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] deadLeafCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead leaf nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadLeafNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine branch carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] deadFineBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine branch carbon, summed across facets
        /// </summary>
        [Units("g/m^2")]
        public double deadTotalFineBranchCarbon { get { return _deadTotalFineBranchCarbon; } }
        private double _deadTotalFineBranchCarbon;

        /// <summary>
        /// Dead fine branch nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadFineBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine branch nitrogen, summed across facets
        /// </summary>
        [Units("g/m^2")]
        public double deadTotalFineBranchNitrogen { get { return _deadTotalFineBranchNitrogen; } }
        private double _deadTotalFineBranchNitrogen;

        /// <summary>
        /// Dead coarse root carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] deadCoarseRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse root carbon, summed across facets
        /// </summary>
        [Units("g/m^2")]
        public double deadTotalCoarseRootCarbon { get { return _deadTotalCoarseRootCarbon; } }
        private double _deadTotalCoarseRootCarbon;

        /// <summary>
        /// Dead coarse root nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadCoarseRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse root nitrogen, summed across facets
        /// </summary>
        [Units("g/m^2")]
        public double deadTotalCoarseRootNitrogen { get { return _deadTotalCoarseRootNitrogen; } }
        private double _deadTotalCoarseRootNitrogen;

        /// <summary>
        /// Dead coarse wood carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] deadCoarseBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse wood carbon, summed across facets
        /// </summary>
        [Units("g/m^2")]
        public double deadTotalCoarseBranchCarbon { get { return _deadTotalCoarseBranchCarbon; } }
        private double _deadTotalCoarseBranchCarbon;

        /// <summary>
        /// Dead coarse wood nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        public double[] deadCoarseBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse wood nitrogen, summed across facets
        /// </summary>
        [Units("g/m^2")]
        public double deadTotalCoarseBranchNitrogen { get { return _deadTotalCoarseBranchNitrogen; } }
        private double _deadTotalCoarseBranchNitrogen;

        /// <summary>
        /// Fine root lignin concentration
        /// </summary>
        [Units("g/m^2")]
        public double[] ligninFineRoot { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse root lignin concentration
        /// </summary>
        [Units("g/m^2")]
        public double[] ligninCoarseRoot { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine branch lignin concentration
        /// </summary>
        [Units("g/m^2")]
        public double[] ligninFineBranch { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse branch lignin concentration
        /// </summary>
        [Units("g/m^2")]
        public double[] ligninCoarseBranch { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf lignin concentration
        /// </summary>
        [Units("g/m^2")]
        public double[] ligninLeaf { get; private set; } = new double[nFacets];

        /// <summary>
        /// Lignin in structural residue, at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (STRLIG)
        /// </summary>
        public double[,] plantLigninFraction { get; private set; } = new double[nFacets, 2];

        /// <summary>
        /// Litter structural carbon at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (STRCIS, or in Savanna, SSTRCIS, with unlabeled and labeled merged)
        /// </summary>
        [Units("g/m^2")]
        public double[] litterStructuralCarbon { get; private set; } = new double[2];   

        /// <summary>
        /// Litter metabolic carbon at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (METCIS, or in Savanna, SMETCIS)
        /// </summary>
        [Units("g/m^2")]
        public double[] litterMetabolicCarbon { get; private set; } = new double[2];

        /// <summary>
        /// Litter structural nitrogen at the surface(1) and in the soil(2)  (STRUCE, or in Savanna, SSTRUCE, with STRUCE named for "elements"  I am only including nitrogen, as in Savanna, so dropping the name)
        /// </summary>
        [Units("g/m^2")]
        public double[] litterStructuralNitrogen { get; private set; } = new double[2];

        /// <summary>
        /// Litter structural nitrogen at the surface(1) and in the soil(2)  (METABE, or in Savanna, SSTRUCE, with STRUCE named for "elements"  I am only including nitrogen, as in Savanna, so dropping the name)
        /// </summary>
        [Units("g/m^2")]
        public double[] litterMetabolicNitrogen { get; private set; } = new double[2];

        // Temporary storage places, but used across swaths of DECOMP.If memory is limited, devise an alternative.
        private double[] tnetmin = new double[2];              // Temporary storage
        private double[] tminup = new double[2];               // Temporary storage
        private double[] grossmin = new double[2];             // Temporary storage
        private double[] volitn = new double[2];               // Temporary storage, volitized nitrogen
        private double fixNit;                                 // Temporary storage, total fixed nitrogen
        private double runoffN;                                // Temporary storage, runoff nitrogen

        private double[,] eUp = new double[nFacets, nWoodyParts];     // Temporary storage, eup().  Woody parts dimensions for that, but includes ABOVE and BELOW in 1 and 2 for herbaceous material

        private double volatizedN;                                    // Accumulator for monthy volatilization of N

        // Growth parameters and others

        private double[] maintainRespiration = new double[nFacets];   // Maintainence respiration

        /// <summary>
        /// Phenological stage, a continuous variable from 0 to 4.
        /// </summary>
        [Units("0-4")]
        public double[] phenology { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine root carbon    (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] fineRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine root nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] fineRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Seed carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] seedCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Seed nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] seedNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] leafCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] leafNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine branch carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] fineBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine branch nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] fineBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse root carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] coarseRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse root nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] coarseRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse branch carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] coarseBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse branch nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        public double[] coarseBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Stored nitrogen(CRPSTG in Century GROWTH, STORAGE in RESTRP).  I can't find where this is initialized, except for a gridded system input.  Assumed 0 for now, but here as a placeholder.
        /// </summary>
        [Units("g/m^2")]
        public double[] storedNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Plant nitrogen fixed
        /// </summary>
        [Units("g/m^2")]
        public double[] plantNitrogenFixed { get; private set; } = new double[nFacets];

        /// <summary>
        /// Nitrogen fixed.  Not sure what components distinguish it, just yet.  (NFIX)
        /// </summary>
        [Units("g/m^2")]
        public double[] nitrogenFixed { get; private set; } = new double[nFacets];

        /// <summary>
        /// Maintenance respiration flows to storage pool(MRSPSTG)
        /// </summary>
        [Units("g/m^2")]
        public double[] respirationFlows { get; private set; } = new double[nFacets];

        /// <summary>
        /// Maintenance respiration flows for year(MRSPANN)
        /// </summary>
        [Units("g/m^2")]
        public double[] respirationAnnual { get; private set; } = new double[nFacets];

        private double carbonSourceSink;                              // Carbon pool.  (g / m2)(CSRSNK)    I don't know the utility of this, but incorporating it.
        private double nitrogenSourceSink;                            // Nitrogen pool.  (g / m2)(ESRSNK)
        private double[,] carbonAllocation = new double[nFacets, nWoodyParts]; // Shrub carbon allocation, by proportion(TREE_CFAC in Century, except statis here)  Brought into this array, even though it requires more memory, in case I do incorporate dynamic allocation at some point.

        /// <summary>
        /// Optimum leaf area index
        /// </summary>
        public double[] optimumLeafAreaIndex { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf area index
        /// </summary>
        public double[] leafAreaIndex { get; private set; } = new double[nFacets];

        /// <summary>
        /// Water function influencing mortality(AGWFUNC and BGWFUNC in Century, merged here since CYCLE assigns them equal and they start with the same value)
        /// </summary>
        public double waterFunction { get; private set; }

        /// <summary>
        /// A score from 0 to 1 reflecting fire intensity
        /// </summary>
        public double fireSeverity { get; private set; }

        /// <summary>
        /// The sum of carbon burned, only on the 1 m plots, not whole plant death
        /// </summary>
        [Units("g/m^2")]
        public double burnedCarbon { get; private set; }

        /// <summary>
        /// The sum of nitrogen burned, only on the 1 m plots, not whole plant death
        /// </summary>
        [Units("g/m^2")]
        public double burnedNitrogen { get; private set; }

        /// <summary>
        /// Total fertilized nitrogen added(g / m2)
        /// </summary>
        [Units("g/m^2")]
        public double fertilizedNitrogenAdded { get; private set; }

        /// <summary>
        /// Total fertilized carbon added(g / m2)
        /// </summary>
        [Units("g/m^2")]
        public double fertilizedCarbonAdded { get; private set; }

        // private int largeErrorCount;  // The count of cells being reset because their values were very very large
        // private int negErrorCount;    // The count of cell being reset because values were below zero

        #endregion

        /// <summary>
        /// Gets or sets the latitude for the site being modelled. Should be in the range -90 to 90
        /// </summary>
        [Summary]
        [Description("Latitude")]
        public double Latitude { get; set; } = Double.NaN;

        /// <summary>
        /// Gets or sets the longitude for the site being modelled. Should be in the range -180 to 180
        /// </summary>
        [Summary]
        [Description("Longitude")]
        public double Longitude { get; set; } = Double.NaN;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("G_Range database file name")]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// </summary>
        [XmlIgnore]
        public string FullDatabaseName
        {
            get
            {
                Simulation simulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(this.DatabaseName, simulation.FileName);
                else
                    return this.DatabaseName;
            }
            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null)
                    this.DatabaseName = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this.DatabaseName = value;
            }
        }
        private Parms parms;

        //[EventHandler]
        /// <summary>
        /// Called when [start of simulation].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            LoadParms();    // Initialize_Landscape_Parms
            LoadGlobals();  // Initialize_Globe
            InitParms();    // Initialize_Rangelands
        }

        /// <summary>EventHandler - preparation before the main daily processes.
        /// For G_Range, this means we need to store and aggregate weather data
        /// into monthly values.
        /// </summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            ReadWeather();
        }

        /// <summary>EventHandler - G_Range model logic run at the end of each month.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            month = Clock.Today.Month; // 1 to 12
            UpdateVegetation();       // Update metrics for vegetation.
            UpdateWeather();          // Calculate snowfall, evapotranspiration, etc.  Also updates heat accumulation.
            PotentialProduction();    // Calculate potential production, and plant allometrics adjusted by grazing fraction
            HerbGrowth();             // Calculate herbaceous growth
            WoodyGrowth();            // Calculate woody plant growth
            Grazing();                // Remove material grazed by livestock
            PlantPartDeath();         // Plant part death
            WholePlantDeath();        // Whole plant death
            Management();             // Fertilization and other management
            PlantReproduction();      // Seed-based reproduction by plants
            UpdateVegetation();       // Update metrics for vegetation
            WaterLoss();              // Calculate water loss
            Decomposition();          // Decomposition
            NitrogenLosses();         // Leaching and volatilization of nitrogen
            EachMonth();              // Miscellaneous steps that need to be done each month
            //OutputSurfaces();         // Produce output surfaces
            ZeroAccumulators();       // Zero-out the accumulators storing dead materials
        }

        /// <summary>EventHandler - Tasks done at the start of each year.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("StartOfYear")]
        private void OnStartOfYear(object sender, EventArgs e)
        {
            EachYear();
        }

        /// <summary>Performs the calculations for potential growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        { }

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        { }

        /// <summary>
        /// A linear interpolation routine, from Savanna ALINT.
        /// Rewritten March 12, 2013 to ensure that values are going in the placed needed.
        /// Typo fixed that had do i = l versus do i = 1 prior to the exit call.
        /// </summary>
        /// <param name="x">The value that needs a Y</param>
        /// <param name="dataVal">Pairs of values that define the relationship, x1, y1, x2, y2, etc.</param>
        /// <param name="imx">The number of pairs given, often 2</param>
        /// <returns></returns>
        private double Linear(double x, double[] dataVal, int imx)
        {
            double[,] dataV = new double[2, imx];
            for (int m = 0; m < imx; m++)
            {
                int n = m * 2;
                dataV[0, m] = dataVal[n];
                dataV[1, m] = dataVal[n + 1];
            }

            if (x <= dataV[0, 0])
                return dataV[1, 0];

            if (x >= dataV[0, imx - 1])
                return dataV[1, imx - 1];

            int k = 0;
            for (int i = 0; i < imx - 1; i++)
            {
                if (x <= dataV[0, i + 1])
                {
                    k = i;
                    break;
                }
            }

            return dataV[1, k] + (dataV[1, k + 1] - dataV[1, k]) / (dataV[0, k + 1] - dataV[0, k]) * (x - dataV[0, k]);

        }

        /// <summary>
        /// Update metrics that summarize vegetation.  Adapted from Century or from scratch
        /// Transcoded from the subroutine in Misc_Material.f95
        /// </summary>
        private void UpdateVegetation()
        {
            // Update tree basal area
            double wdbmas = (fineBranchCarbon[(int)Facet.tree] + coarseBranchCarbon[(int)Facet.tree]) * 2.0;
            if (wdbmas <= 0.0)
                wdbmas = 50.0;                              // Adjusted to allow trees to grow from zero and avoid underflows.

            double basf = wdbmas / (0.88 * (Math.Pow(wdbmas * 0.01, 0.635)));   // Division by zero avoided above.
            if (basf < 250.0)
                basf = basf * parms.treeBasalAreaToWoodBiomass;
            treeBasalArea = wdbmas / basf;                  // Setting WDBMAS and the use of a parameter should avoid division by 0.

            waterFunction = 1.0 / (1.0 + 4.0 * Math.Exp(-6.0 * relativeWaterContent[0]));  // 1 + etc.should avoid division by zero error.
            totalLitterCarbon[surfaceIndex] = litterStructuralCarbon[surfaceIndex] + litterMetabolicCarbon[surfaceIndex];
            if (totalLitterCarbon[surfaceIndex] < 0.0)
                totalLitterCarbon[surfaceIndex] = 0.0;
            totalLitterCarbon[soilIndex] = litterStructuralCarbon[soilIndex] + litterMetabolicCarbon[soilIndex];
            if (totalLitterCarbon[soilIndex] < 0.0)
                totalLitterCarbon[soilIndex] = 0.0;
            totalLitterNitrogen[surfaceIndex] = litterStructuralNitrogen[surfaceIndex] + litterMetabolicNitrogen[surfaceIndex];
            if (totalLitterNitrogen[surfaceIndex] < 0.0)
                totalLitterNitrogen[surfaceIndex] = 0.0;
            totalLitterNitrogen[soilIndex] = litterStructuralNitrogen[soilIndex] + litterMetabolicNitrogen[soilIndex];
            if (totalLitterNitrogen[soilIndex] < 0.0)
                totalLitterNitrogen[soilIndex] = 0.0;

            double[] dataVal = new double[10];
            // Update the phenology of the plants
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                for (int i = 0; i < 10; i++)
                    dataVal[i] = parms.degreeDaysPhen[iFacet, i];
                phenology[iFacet] = Linear(heatAccumulation, dataVal, 5);
                if (heatAccumulation >= parms.degreeDaysReset[iFacet])
                    phenology[iFacet] = 0.0;
            }

            // The following is likely not very good for herbs, using the same values as trees, but still a helpful index.Could be expanded to include LAI to biomass relationship specific to herbs
            // The following only considers the tallest facet.  Could(should ?) include shrubs and herbs in tree facet, etc?   Separated out from a facet loop here incase that change is made.
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
                leafAreaIndex[iFacet] = leafCarbon[iFacet] * (2.5 * parms.biomassToLeafAreaIndexFactor);

            soilTotalCarbon = fastSoilCarbon[soilIndex] + intermediateSoilCarbon +
                                           passiveSoilCarbon + litterStructuralCarbon[soilIndex] +
                                           litterMetabolicCarbon[soilIndex];
            // CHECK
            // The Fortran code assigned to carbon_nitrogen_ratio, which is an array of 2 elements.
            // Is that an error, or should both elements of the array receive the new value?
            carbonNitrogenRatio[soilIndex] = (fastSoilCarbon[soilIndex] + intermediateSoilCarbon +
                                   passiveSoilCarbon + litterStructuralCarbon[soilIndex] +
                                   litterMetabolicCarbon[soilIndex]) /
                                   (fastSoilNitrogen[soilIndex] + intermediateSoilNitrogen +
                                    passiveSoilNitrogen + litterStructuralNitrogen[soilIndex] +
                                    litterMetabolicNitrogen[soilIndex]);

            // If the whole array was being set deliberately, then we also need
            carbonNitrogenRatio[surfaceIndex] = carbonNitrogenRatio[soilIndex];
            // CHECK

            // Moving the following from WATER_LOSS, since it was only being done with no snow present.
            double avgALiveBiomass = 0.0;
            double avgBLiveBiomass = 0.0;
            double[] biomassLivePerLayer = new double[nLayers];

            // ABOVEGROUND
            // Using method used in productivity.Does not use plant populations in layers, but uses the facets instead.Not at precise but less prone to vast swings.
            double totalCover = facetCover[(int)Facet.herb] + facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree];
            double fracCover;
            if (totalCover > 0.000001) {
                // HERBS
                fracCover = facetCover[(int)Facet.herb] / totalCover;
                biomassLivePerLayer[(int)Layer.herb] = (leafCarbon[(int)Facet.herb] + seedCarbon[(int)Facet.herb]) * 2.5 * fracCover;
                fracCover = facetCover[(int)Facet.shrub] / totalCover;
                biomassLivePerLayer[(int)Layer.herbUnderShrub] = (leafCarbon[(int)Facet.herb] + seedCarbon[(int)Facet.herb]) * 2.5 * fracCover;
                fracCover = facetCover[(int)Facet.tree] / totalCover;
                biomassLivePerLayer[(int)Layer.herbUnderTree] = (leafCarbon[(int)Facet.herb] + seedCarbon[(int)Facet.herb]) * 2.5 * fracCover;

                // SHRUBS
                if ((facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree]) > 0.000001)
                {
                    fracCover = facetCover[(int)Facet.shrub] / (facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree]);
                    biomassLivePerLayer[(int)Layer.shrub] = (leafCarbon[(int)Facet.shrub] + seedCarbon[(int)Facet.shrub] +
                          fineBranchCarbon[(int)Facet.shrub] + coarseBranchCarbon[(int)Facet.shrub]) * 2.5 * fracCover;
                    fracCover = facetCover[(int)Facet.tree] / (facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree]);
                    biomassLivePerLayer[(int)Layer.shrubUnderTree] = (leafCarbon[(int)Facet.shrub] + seedCarbon[(int)Facet.shrub] +
                          fineBranchCarbon[(int)Facet.shrub] + coarseBranchCarbon[(int)Facet.shrub]) * 2.5 * fracCover;
                }
                else
                {
                    biomassLivePerLayer[(int)Layer.shrub] = 0.0;
                    biomassLivePerLayer[(int)Layer.tree] = 0.0;
                }

                // TREES
                fracCover = facetCover[(int)Facet.tree];
                biomassLivePerLayer[(int)Layer.tree] = (leafCarbon[(int)Facet.tree] + seedCarbon[(int)Facet.tree] +
                          fineBranchCarbon[(int)Facet.tree] + coarseBranchCarbon[(int)Facet.tree]) * 2.5 * fracCover;

                for (int iLyr = 0; iLyr < nLayers; iLyr++)
                    avgALiveBiomass = avgALiveBiomass + biomassLivePerLayer[iLyr];
            }
            else
                avgALiveBiomass = 0.0;       // There is no cover on the cell

            // BELOWGROUND
            if (totalCover > 0.000001)
            {
                // HERBS
                fracCover = facetCover[(int)Facet.herb] / totalCover;
                biomassLivePerLayer[(int)Layer.herb] = fineRootCarbon[(int)Facet.herb] * 2.5 * fracCover;
                fracCover = facetCover[(int)Facet.shrub] / totalCover;
                biomassLivePerLayer[(int)Layer.herbUnderShrub] = fineRootCarbon[(int)Facet.herb] * 2.5 * fracCover;
                fracCover = facetCover[(int)Facet.tree] / totalCover;
                biomassLivePerLayer[(int)Layer.herbUnderTree] = fineRootCarbon[(int)Facet.herb] * 2.5 * fracCover;
                // SHRUBS
                if ((facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree]) > 0.000001)
                {
                    fracCover = facetCover[(int)Facet.shrub] / (facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree]);
                    biomassLivePerLayer[(int)Layer.shrub] =
                        (fineRootCarbon[(int)Facet.shrub] + coarseRootCarbon[(int)Facet.shrub]) * 2.5 * fracCover;
                    fracCover = facetCover[(int)Facet.tree] / (facetCover[(int)Facet.shrub] + facetCover[(int)Facet.tree]);
                    biomassLivePerLayer[(int)Layer.shrubUnderTree] =
                         (fineRootCarbon[(int)Facet.shrub] + coarseRootCarbon[(int)Facet.shrub]) * 2.5 * fracCover;
                }
                else
                {
                    biomassLivePerLayer[(int)Layer.shrub] = 0.0;
                    biomassLivePerLayer[(int)Layer.tree] = 0.0;
                }

                // TREES
                fracCover = facetCover[(int)Facet.tree];
                biomassLivePerLayer[(int)Layer.tree] =
                         (fineRootCarbon[(int)Facet.tree] + coarseRootCarbon[(int)Facet.tree]) * 2.5 * fracCover;


                for (int iLyr = 0; iLyr < nLayers; iLyr++)
                    avgBLiveBiomass = avgBLiveBiomass + biomassLivePerLayer[iLyr];
            }
            else
                avgBLiveBiomass = 0.0;   // There is no cover on the cell

            // The following is used in Decomposition, and perhaps elsewhere.
            totalAgroundLiveBiomass = avgALiveBiomass;
            totalBgroundLiveBiomass = avgBLiveBiomass;

            // Calculate monthly net primary productivity
            monthlyNetPrimaryProduction =
                (totalPotProdLimitedByN[(int)Layer.herb] * facetCover[(int)Facet.herb]) +
                (totalPotProdLimitedByN[(int)Layer.herbUnderShrub] * facetCover[(int)Facet.shrub]) +
                (totalPotProdLimitedByN[(int)Layer.herbUnderTree] * facetCover[(int)Facet.tree]) +
                (totalPotProdLimitedByN[(int)Layer.shrub] * facetCover[(int)Facet.shrub]) +
                (totalPotProdLimitedByN[(int)Layer.shrubUnderTree] * facetCover[(int)Facet.tree]) +
                (totalPotProdLimitedByN[(int)Layer.tree] * facetCover[(int)Facet.tree]);
        }

        /// <summary>
        /// Processes that are required each year, prior to any process-based simulation steps.
        /// 
        /// Transcoded from Misc_Material.f95
        /// </summary>
        private void EachYear()
        {
            annualEvapotranspiration = 0.0;
            // negErrorCount = 0;
            // largeErrorCount = 0;

            // Fill the tree carbon allocation array.  This is dynamic in Century 4.5, static in 4.0, and static here.  (Calculated each year, but no matter)
            // This will be approximate, as all five pieces are not specified(but could be).
            double shrubCSum = 0.0;
            double treeCSum = 0.0;
            for (int iPart = 0; iPart < nWoodyParts; iPart++)
            {
                shrubCSum += parms.shrubCarbon[iPart, ALIVE] + parms.shrubCarbon[iPart, DEAD];
                treeCSum += parms.treeCarbon[iPart, ALIVE] + parms.treeCarbon[iPart, DEAD];
            }
            if (shrubCSum > 0.0)
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[(int)Facet.shrub, iPart] = (parms.shrubCarbon[iPart, ALIVE] +
                                                                 parms.shrubCarbon[iPart, DEAD]) / shrubCSum;
            }
            else
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[(int)Facet.shrub, iPart] = 0.0;
            }
            if (treeCSum > 0.0)
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[(int)Facet.tree, iPart] = (parms.treeCarbon[iPart, ALIVE] +
                                                        parms.shrubCarbon[iPart, DEAD]) / treeCSum;
            }
            else
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[(int)Facet.tree, iPart] = 0.0;
            }


            // Clearing -out the shrub_carbon and tree_carbon variables, such that they represent the contribution of new carbon            
            // to woody parts for the year in question.
            for (int iPart = 0; iPart < nWoodyParts; iPart++)
            {
                shrubCarbon[iPart] = 0.0;
                treeCarbon[iPart] = 0.0;
                shrubNitrogen[iPart] = 0.0;
                treeNitrogen[iPart] = 0.0;
            }

            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                // Some of the dead plant materials are long-term storage(e.g., dead coarse roots, standing dead)
                // Others don't exist in Century, but are incorporated here for completeness.  They are storage holders for use in annual
                // model tracking.  They need to be cleared -out prior to each year's simulation.
                // (Note: Arrays can be zeroed out with a single call, but not as clear)
                // Totals summed across facets are to be reset each year
                _deadTotalFineBranchCarbon = 0.0;
                _deadTotalFineBranchNitrogen = 0.0;
                _deadTotalCoarseBranchCarbon = 0.0;
                _deadTotalCoarseBranchNitrogen = 0.0;
                _deadTotalCoarseRootCarbon = 0.0;
                _deadTotalCoarseRootNitrogen = 0.0;
                // Other dead placeholders may be zeroed-out as well.The only reason they would store anything after the
                // call to DECOMPOSITION would be from rounding errors.And DEAD_LEAF_CARBON and NITROGEN plus FINE_BRANCH_CARBON and NITROGEN
                // aren't used in modeling at all, they are only accumulators.   The death of those elements go to STANDING_DEAD_CARBON and NITROGEN
                // where they are partitioned.  STANDING_DEAD* SHOULD NOT be zeroed out, as that material can accumulate over more than one season.

                // Storage areas that serve as accumulators only should be reset to 0 each year, to avoid a continual accumulation of
                // values through an entire simulation.There may be a case or two where such accumulations are helpful, but most
                // will be reset to zero.   Some of these don't include facets and will be reset more than required, but no matter here.
                evaporation = 0.0;
                maintainRespiration[iFacet] = 0.0;
                respirationAnnual[iFacet] = 0.0;
                nitrogenFixed[iFacet] = 0.0;

                // Recalculate the proportion of residue that is lignin, which follows from annual precipitation (CMPLIG.F in Century.No equilvalent in Savanna)
                plantLigninFraction[iFacet, surfaceIndex] = ligninLeaf[iFacet] +
                                       parms.ligninContentFractionAndPrecip[0, surfaceIndex] +
                                       (parms.ligninContentFractionAndPrecip[1, surfaceIndex] *
                                       globe.precipAverage) / 2.0;
                plantLigninFraction[iFacet, soilIndex] = ligninFineRoot[iFacet] +
                                        parms.ligninContentFractionAndPrecip[0, soilIndex] +
                                        (parms.ligninContentFractionAndPrecip[1, soilIndex] *
                                        globe.precipAverage) / 2.0;
                plantLigninFraction[iFacet, surfaceIndex] = Math.Max(0.02, plantLigninFraction[iFacet, surfaceIndex]);
                plantLigninFraction[iFacet, surfaceIndex] = Math.Min(0.50, plantLigninFraction[iFacet, surfaceIndex]);
                plantLigninFraction[iFacet, soilIndex] = Math.Max(0.02, plantLigninFraction[iFacet, soilIndex]);
                plantLigninFraction[iFacet, soilIndex] = Math.Min(0.50, plantLigninFraction[iFacet, soilIndex]);

                // Fire modeling
                burnedCarbon = 0.0;
                burnedNitrogen = 0.0;
                fireSeverity = 0.0;
            } // Facet loop
              // Mostly eaving out CO2 effects for now...
              /*
                ! Opening the CO2 effects file each month, just for simplicity
                open(SHORT_USE_FILE, FILE = parm_path(1:len_trim(parm_path))//Sim_Parm%co2effect_file_name, ACTION='READ', IOSTAT=ioerr)
                if (ioerr == 0) then
                  in_year = -9999
                  read(SHORT_USE_FILE, *) unit_cnt! GRange never knows the number of landscape units, so required at the top of file or some other pathway
                  read(SHORT_USE_FILE, *)! Skip the header information
                  do while (in_year.ne.year)
                                  read(SHORT_USE_FILE, *) in_year, (Parms(unit_id) % effect_of_co2_on_production(H_FACET), &
                                    Parms(unit_id) % effect_of_co2_on_production(S_FACET), Parms(unit_id) % effect_of_co2_on_production(T_FACET), &
                                    unit_id = 1, unit_cnt)
                  end do
                              do icell = 1,range_cells! Process all of the cells classed as rangeland only
                 iunit = Rng(icell) % range_type
                    Rng(icell) % co2_effect_on_production(H_FACET) = Parms(iunit) % effect_of_co2_on_production(H_FACET)
                    Rng(icell) % co2_effect_on_production(S_FACET) = Parms(iunit) % effect_of_co2_on_production(S_FACET)
                    Rng(icell) % co2_effect_on_production(T_FACET) = Parms(iunit) % effect_of_co2_on_production(T_FACET)
                  end do
                              close(SHORT_USE_FILE)
                else
                              write(*, *) 'There is a problem updating the CO2 effect on production values'
                  stop
                end if

                if (check_nan_flag.eqv. .TRUE.) call check_for_nan(icell, 'EACH_YR')
                */
              // I prefer not to use the external files of G-Range, especially as the Weather component is already intended as
              // a provider of information on CO2 levels.

            // In G-Range, a value of 0.8 is the baseline value for co2EffectOnProduction, 
            // and is what G-Range uses for dates prior to 2007. They have the value going up to around 1.0 in 2066 under the rcp85 scenario,
            // which is around 800 ppm. Boone et al. 2017 reference equation 10 of Pan et al. (1998):
            // 1 + (1.25 - 1)/(log10(2)) * (log10(CO2 / 350))
            // But actually use something a bit different, indicating 2006 CO2 levels as a baseline, without giving details. 
            // I can get close to their values with the following:
            double co2 = Math.Max(Weather.CO2, 380.0);
            double co2Effect = 0.8 + (0.19 / Math.Log10(2.0)) * Math.Log10(co2 / 380.0);
            co2EffectOnProduction[(int)Facet.herb] = co2Effect;
            co2EffectOnProduction[(int)Facet.shrub] = co2Effect;
            co2EffectOnProduction[(int)Facet.tree] = co2Effect;
            // This won't matter for initial testing, as Weather.CO2 will default to 350, and so co2effect will always be 0.8, the G-Range "default"
        }

        /// <summary>
        /// Zero-out some accumulators each month.
        /// </summary>
        private void ZeroAccumulators()
        {
            // Zeroing out the dead plant stores each month, as in Savanna(ZFLOW.F)
            // These have all been partitioned by now, either into forms of litter or passed to standing dead, which is not zeroed -out.
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                deadFineRootCarbon[iFacet] = 0.0;
                deadFineRootNitrogen[iFacet] = 0.0;
                deadFineBranchCarbon[iFacet] = 0.0;
                deadFineBranchNitrogen[iFacet] = 0.0;
                deadSeedCarbon[iFacet] = 0.0;
                deadSeedNitrogen[iFacet] = 0.0;
                deadLeafCarbon[iFacet] = 0.0;
                deadLeafNitrogen[iFacet] = 0.0;
                deadCoarseBranchCarbon[iFacet] = 0.0;
                deadCoarseBranchNitrogen[iFacet] = 0.0;
                deadCoarseRootCarbon[iFacet] = 0.0;
                deadCoarseRootNitrogen[iFacet] = 0.0;
            }
        }

        /// <summary>
        /// Do processing steps that must be done each month.  The main steps are a long series of tests to ensure that
        /// values aren't exceeding a very large value, or moving negative.  Errors will cause tallying of counts of errors,
        /// both spatially and per entry.That said, they won't be stored spatially for each individual entry, as that
        /// would almost double memory.
        /// 
        /// This routine includes a simple assignment of grazing fraction.That logic is placed here to allow for it to
        /// be made more dynamic in the future.
        /// </summary>
        private void EachMonth()
        {
            // I'm not transcoding the bulk of this directly. There are around 1700 lines of code that test variables against bounds of 0
            // and vLarge. Here I use reflection to check (almost) all Double members against that range.

            Type myType = GetType();
            double var;
            // We should see Properties through their backing fields. This is adequate, provided the properties aren't making use of accessors.
            FieldInfo[] fields = myType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo fieldInfo in fields)
            {
                if (fieldInfo.Name.Contains("Latitude") || fieldInfo.Name.Contains("Longitude")) // These can be negative
                    continue;
                if (fieldInfo.FieldType == typeof(Double))
                {
                    var = TestDouble((double)fieldInfo.GetValue(this), fieldInfo.Name);
                    if (!Double.IsNaN(var))
                        fieldInfo.SetValue(this, var);
                }
                else if (fieldInfo.FieldType == typeof(Double[]))
                {
                    Double[] array = (Double[])fieldInfo.GetValue(this);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var = TestDouble(array[i], fieldInfo.Name + '[' + i.ToString() + ']');
                        if (!Double.IsNaN(var))
                            array[i] = var;
                    }
                }
                else if (fieldInfo.FieldType == typeof(Double[,]))
                {
                    Double[,] array = (Double[,])fieldInfo.GetValue(this);
                    for (int i = 0; i < array.GetLength(0); i++)
                    {
                        for (int j = 0; j < array.GetLength(1); j++)
                        {
                            var = TestDouble(array[i, j], fieldInfo.Name + '[' + i.ToString() + ',' + j.ToString() + ']');
                            if (!Double.IsNaN(var))
                                array[i, j] = var;
                        }
                    }
                }
            }

            // What it retained here from the Fortran original is the logic for checking the bounds on grazing.

            double live_carbon = leafCarbon[(int)Facet.herb] + leafCarbon[(int)Facet.shrub] + leafCarbon[(int)Facet.tree] +
                                 fineBranchCarbon[(int)Facet.shrub] + fineBranchCarbon[(int)Facet.tree];
            double dead_carbon = deadStandingCarbon[(int)Facet.herb] + deadStandingCarbon[(int)Facet.shrub] + deadStandingCarbon[(int)Facet.tree];
            if ((live_carbon + dead_carbon) > 0.0)
            {
                fractionLiveRemovedGrazing = (parms.fractionGrazed / 12.0) * (live_carbon / (live_carbon + dead_carbon));
                fractionDeadRemovedGrazing = (parms.fractionGrazed / 12.0) * (1.0 - (live_carbon / (live_carbon + dead_carbon)));
            }
            else
            {
                fractionLiveRemovedGrazing = 0.0;
                fractionDeadRemovedGrazing = 0.0;
            }
        }

        private double TestDouble(double var, string varName)
        {
            double min = 0.0;
            double max = vLarge;
            if (Double.IsNaN(var))
            {
                summary.WriteWarning(this, "Variable " + varName + " was NaN");
                return min;
            }
            else if (var < min)
            {
                if (!varName.Contains("carbonSourceSink") &&!varName.Contains("holdingTank"))
                    summary.WriteWarning(this, "The value " + var.ToString() + " for variable " + varName + " was below the minimum allowed value");
                return min;
            }
            else if (var > max)
            {
                summary.WriteWarning(this, "Variable " + varName + " was above the maximum allowed value");
                return max;
            }
            return Double.NaN;
        }
    }
}