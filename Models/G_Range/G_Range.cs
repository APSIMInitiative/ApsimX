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

        [Link(IsOptional =true)]
        Soils.Soil Soil = null;

        [Link]
        Soils.Physical Analysis = null;

        [Link(IsOptional = true)]
        Soils.SoilCrop SoilCrop = null;

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
        public PMF.Biomass AboveGround
        {
            get
            {
                PMF.Biomass mass = new PMF.Biomass();
                // 2.5 is the factor G-Range uses to convert carbon to biomass for leaves and roots
                // but when converting from carbon to biomass for wood parts it uses 2.0, rather than 2.5
                // So what should be used for deadStanding? Looks like it uses 2.5, even though I suspect
                // for shrub and tree facets, the deadStandingCarbon will be mostly woody.
                mass.MetabolicWt = (leafCarbon[Facet.herb] + leafCarbon[Facet.shrub] + leafCarbon[Facet.tree]) * 2.5;
                mass.StructuralWt = (fineBranchCarbon[Facet.shrub] + fineBranchCarbon[Facet.tree]) * 2.0 + 
                                    (deadStandingCarbon[Facet.herb] + deadStandingCarbon[Facet.shrub] + deadStandingCarbon[Facet.tree]) * 2.5;
                mass.MetabolicN = leafNitrogen[Facet.herb] + leafNitrogen[Facet.shrub] + leafNitrogen[Facet.tree];
                mass.StructuralN = fineBranchNitrogen[Facet.shrub] + fineBranchNitrogen[Facet.tree] +
                                     deadStandingNitrogen[Facet.herb] + deadStandingNitrogen[Facet.shrub] + deadStandingNitrogen[Facet.tree];
                return mass;
            }
        }

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
        public double Albedo { get { return 0.15; } } // This is canopy albedo, not soil albedo

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax { get { return 0.01; } }

        /// <summary>Gets or sets the R50.</summary> // What is an R50?
        public double R50 { get { return 200; } }

        /// <summary>Gets the LAI (Leaf Area Index)</summary>
        [Units("m^2/m^2")]
        public double LAI { get { return 1.7; } set { } }

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

        /// <summary>
        ///  Stores parameters unique to each landscape unit
        /// </summary>
        private class UnitParm
        {
        }

        // I originally used enumerations for layers, facets, and woody parts. This was OK, but since enumerations can't
        // be used directly as array indices, it required having casts to int all over the place.
        // Using a class allows almost the same syntax to be used in most places, and avoids the need for casting.
        // It also maps to the original Fortran a bit more directly, if that matters...

        /// <summary>
        /// Vegetation layer constants
        /// </summary>
        public static class Layer
        {
            /// <summary>
            /// Herb layer index (H_LYR)
            /// </summary>
            public const int herb = 0; // Was 1 in Fortran!
            /// <summary>
            /// Herbs under shrubs layer index (H_S_LYR)
            /// </summary>
            public const int herbUnderShrub = 1;
            /// <summary>
            /// Herbs under tree layer index (H_T_LYR)
            /// </summary>
            public const int herbUnderTree = 2;
            /// <summary>
            /// Shrub layer index (S_LYR)
            /// </summary>
            public const int shrub = 3;
            /// <summary>
            /// Shrub under tree layer index (S_T_LYR)
            /// </summary>
            public const int shrubUnderTree = 4;
            /// <summary>
            /// Tree layer index (T_LYR)
            /// </summary>
            public const int tree = 5;
        };

        /// <summary>
        /// Total number of layers (V_LYRS)
        /// </summary>
        private const int nLayers = 6;

        /// <summary>
        /// Vegetation facet constants.  Facets are used for unit input, so that users provide 3 values, rather than 6
        /// </summary>
        public static class Facet
        {
            /// <summary>
            /// Herb layer index (H_FACET)
            /// </summary>
            public const int herb = 0;  // Was 1 in Fortran!
            /// <summary>
            /// Shrub facet index (S_FACET)
            /// </summary>
            public const int shrub = 1;
            /// <summary>
            /// Herbs under tree layer index (T_FACET)
            /// (Should be "Tree fact index (T_FACET)")
            /// </summary>
            public const int tree = 2;
        };
        /// <summary>
        /// Total number of facets (FACETS)
        /// </summary>
        private const int nFacets = 3;

        /// <summary>
        /// Woody part constants
        /// Woody parts, leaf, fine root, fine branch, large wood, and coarse root    
        /// </summary>
        public static class WoodyPart
        {
            /// <summary>
            /// LEAF_INDEX
            /// </summary>
            public const int leaf = 0; // Was 1 in Fortran!
            /// <summary>
            /// FINE_ROOT_INDEX
            /// </summary>
            public const int fineRoot = 1;
            /// <summary>
            /// FINE_BRANCH_INDEX
            /// </summary>
            public const int fineBranch = 2;
            /// <summary>
            /// COARSE_BRANCH_INDEX
            /// </summary>
            public const int coarseBranch = 3;
            /// <summary>
            /// COARSE_ROOT_INDEX
            /// </summary>
            public const int coarseRoot = 4;
        };

        /// <summary>
        /// Total number of woody parts (WOODY_PARTS)
        /// </summary>
        private const int nWoodyParts = 5;

        // Soil and layer constants
        private const int nDefSoilLayers = 4;                          // The number of soil layers (SOIL_LAYERS)
        private int nSoilLayers = nDefSoilLayers;

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
        /// In the "standard" G-Range configuration, values run from 1 to 720, corresponding with longitudes
        /// from 180 W (normally expressed as -180) to 180 E, using half-degree steps.
        /// </summary>
        [XmlIgnore]
        public int X;

        /// <summary>
        /// Y dimension of rangeland cell
        /// In the "standard" G-Range configuration, values run from 1 to 360, correpsonding with latitudes
        /// from 90 S (normally expressed as -90) to 90 N, using half-degree steps.
        /// </summary>
        [XmlIgnore]
        public int Y;

        /// <summary>
        /// Month of the year, expressed as a value in the range 1-12
        /// </summary>
        [XmlIgnore]
        private int month;

        /// <summary>
        /// Identifier storing the type of rangeland cell, used as a key to the Parms strcuture
        /// A value in the range 1-15, indicating the "biome" within a cell. Values are 1: tropical evergreen forest; 2: tropical deciduous forest;
        /// 3: temperate broadleaf evergreen forest; 4: temperate needleleaf evergreen forest; 5: temperate deciduous forest; 6: boreal evergreen forest;
        /// 7: boreal deciduous forest; 8: evergreen/deciduous mixed forest; 9: savanna; 10: grassland; 11: dense shrubland; 12: open shrubland;
        /// 13: tundra; 14: desert; 15: polar desert
        /// </summary>
        [XmlIgnore]
        public int rangeType { get; private set; }

        private double lastMonthDayLength;     // The day length of the previous month, to know when spring and fall come.
        private bool dayLengthIncreasing;      // Increasing or decreasing day length, comparing the current to previous day lengths.
        private bool inWinter = false;         // NOT ORIGINALLY PART OF THE CELL STRUCTURE - G-Range itself uses (inappropriately) a Fortran "SAVE" in Plant_Death.95

        /// <summary>
        /// Day length, calculated based on latitude and month
        /// </summary>
        [Units("hr")]
        [XmlIgnore]
        public double dayLength { get; private set; }

        /// <summary>
        /// Heat accumulation above a base temperature (e.g., 4.4 C in Boone (1999))
        /// </summary>
        [Units("<sup>o</sup>Cd")]
        [XmlIgnore]
        public double heatAccumulation { get; private set; }

        /// <summary>
        /// The proportion occupied by each facet
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double[] facetCover { get; private set; } = new double[nFacets];

        /// <summary>
        /// The total population of each vegetation layer
        /// This is the number of individuals in a 1 km^2 area
        /// </summary>
        [XmlIgnore]
        public double[] totalPopulation { get; private set; } = new double[nLayers]; 

        /// <summary>
        /// Bare cover stored, rather than continually summing the three facets. 
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double bareCover { get; private set; }

        /// <summary>
        /// Proportion of facet that is annual plants (H_FACET) or deciduous (S_FACET and T_FACET)
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double[] propAnnualDecid { get; private set; } = new double[nFacets];

        /// <summary>
        /// Precipitation total for the current month
        /// Exposed here for reporting purposes
        /// </summary>
        [Units("cm")]
        [XmlIgnore]
        public double precip { get { return globe.precip; } }

        /// <summary>
        /// Average maximum temperature for the current month 
        /// Exposed here for reporting purposes
        /// </summary>
        [Units("C")]
        [XmlIgnore]
        public double maxTemp { get { return globe.maxTemp; } }

        /// <summary>
        /// Average minimum temperature for the current month 
        /// Exposed here for reporting purposes
        /// </summary>
        [Units("C")]
        [XmlIgnore]
        public double minTemp { get { return globe.minTemp; } }

        /// <summary>
        /// Potential evapotranspiration for the cell(cm/month)
        /// </summary>
        [Units("cm/month")]
        [XmlIgnore]
        public double potEvap { get; private set; }

        /// <summary>
        /// Water evaporated from the soil and vegetation(cm/month)
        /// </summary>
        [Units("cm/month")]
        [XmlIgnore]
        public double evaporation { get; private set; }

        /// <summary>
        /// Snowpack, in cm
        /// </summary>
        [Units("cm")]
        [XmlIgnore]
        public double snow { get; private set; }

        /// <summary>
        /// Snowpack liquid water.
        /// </summary>
        [Units("cm")]
        [XmlIgnore]
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
        [XmlIgnore]
        public double melt { get; private set; }

        /// <summary>
        /// Potential evaporation decremented as steps are calculated.Appears to be a bookkeeping tool.
        /// </summary>
        [Units("cm")]
        [XmlIgnore]
        public double petRemaining { get; private set; }

        /// <summary>
        /// Precipitation adjusted for snow accumulation and melt, and available to infiltrate the soil (cm)
        /// </summary>
        [Units("cm")]
        [XmlIgnore]
        public double pptSoil { get; private set; }

        /// <summary>
        /// Runoff from the rangeland cell
        /// </summary>
        [XmlIgnore]
        public double runoff { get; private set; }

        /// <summary>
        /// Ratio of available water to potential evapotranspiration
        /// </summary>
        [XmlIgnore]
        public double ratioWaterPet { get; private set; }

        /// <summary>
        /// Potential evaporation from top soil (cm/day)
        /// </summary>
        [Units("cm/d")]
        [XmlIgnore]
        public double petTopSoil { get; private set; }

        /// <summary>
        /// Nitrogen leached from soil(AMTLEA in Century)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] nLeached { get; private set; } = new double[nDefSoilLayers]; 

        private double[] asmos = new double[nDefSoilLayers];     // Used in summing water
        private double[] amov = new double[nDefSoilLayers];      // Used in summing water movement
        private double stormFlow;              // Storm flow
        private double holdingTank;            // Stores water temporarily. Was asmos(layers+1) in H2OLos

        /// <summary>
        /// Transpiration water loss
        /// </summary>
        [Units("cm/month")]
        [XmlIgnore]
        public double transpiration { get; private set; }

        private double[] relativeWaterContent = new double[nDefSoilLayers]; // Used to initialize and during simulation in CENTURY.Here, only during simulation

        /// <summary>
        /// Water available to plants, available for growth =(1) [0 in C#], survival(2) [1 in C#], and in the two top layers(3) [2 in C#]
        /// </summary>
        [XmlIgnore]
        public double[] waterAvailable { get; private set; }  = new double[3]; 

        /// <summary>
        /// Annual actual evapotranspiration
        /// </summary>
        [Units("cm")]
        [XmlIgnore]
        public double annualEvapotranspiration { get; private set; }

        /// <summary>
        /// Total aboveground live biomass (g/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double totalAgroundLiveBiomass { get; private set; }

        /// <summary>
        /// Total belowground live biomass (g/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double totalBgroundLiveBiomass { get; private set; }

        /// <summary>
        /// Average monthly litter carbon(g/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] totalLitterCarbon { get; private set; } = new double[2];

        /// <summary>
        /// Average monthly litter carbon(g/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] totalLitterNitrogen { get; private set; } = new double[2];

        /// <summary>
        /// Root shoot ratio
        /// </summary>
        [XmlIgnore]
        public double[] rootShootRatio { get; private set; } = new double[nFacets];

        /// <summary>
        /// Basal area for trees
        /// </summary>
        [Units("m^2")]
        [XmlIgnore]
        public double treeBasalArea { get; private set; }

        /// <summary>
        /// Average soil surface temperature (C)
        /// </summary>
        [Units("C")]
        [XmlIgnore]
        public double soilSurfaceTemperature { get; private set; }

        // Soils as in Century 4.5 NLayer= 4, 0-15, 15-30, 30-45, 45-60 cm.
        // These will be initialized using approximations and weighted averages from HWSD soils database, which is 0-30 for TOP, 30-100 for SUB.
        /// <summary>
        /// The percent sand in the soil
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double[] sand { get; private set; } = new double[nDefSoilLayers];

        /// <summary>
        /// The percent silt in the soil
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double[] silt { get; private set; } = new double[nDefSoilLayers];

        /// <summary>
        /// The percent clay in the soil
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double[] clay { get; private set; } = new double[nDefSoilLayers];

        /// <summary>
        /// Mineral nitrogen content for layer (g/m2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] mineralNitrogen { get; private set; } = new double[nDefSoilLayers];

        private double[] soilDepth = new double[nDefSoilLayers] { 15.0, 15.0, 15.0, 15.0 }; // The depth of soils, in cm.Appears hardwired in some parts of CENTURY, flexible, and up to 9 layers, in other parts of CENTURY.Likely I noted some values from an early version, but this is a simplification, so...

        /// <summary>
        /// Field capacity for four soils layers shown above.
        /// </summary>
        [XmlIgnore]
        public double[] fieldCapacity { get; private set; } = new double[nDefSoilLayers];

        /// <summary>
        /// Wilting point for four soil layers shown above.
        /// </summary>
        [XmlIgnore]
        public double[] wiltingPoint { get; private set; } = new double[nDefSoilLayers];

        /// <summary>
        /// grams per square meter
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double soilTotalCarbon { get; private set; }

        /// <summary>
        /// Tree carbon in its components.These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        [XmlIgnore]
        public double[] treeCarbon { get; private set; } = new double[nWoodyParts];

        /// <summary>
        /// Tree nitrogen in its components.   These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        [XmlIgnore]
        public double[] treeNitrogen { get; private set; }  = new double[nWoodyParts];

        /// <summary>
        /// Shrub carbon in its components.   These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        [XmlIgnore]
        public double[] shrubCarbon { get; private set; } = new double[nWoodyParts];

        /// <summary>
        /// Shrub nitrogen in its components.   These must all be merged or otherwise crosswalked at some point.
        /// </summary>
        [XmlIgnore]
        public double[] shrubNitrogen { get; private set; } = new double[nWoodyParts];

        /// <summary>
        /// Carbon to nitrogen ratio, SURFACE, SOIL
        /// </summary>
        [XmlIgnore]
        public double[] carbonNitrogenRatio { get; private set; } = new double[2]; 

        /// <summary>
        /// Soil organic matter carbon, surface and soil  g/m2(SOM1C in Century)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] fastSoilCarbon { get; private set; } = new double[2];

        /// <summary>
        /// Intermediate soil carbon g/m2(SOMC2 in Century)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double intermediateSoilCarbon { get; private set; }

        /// <summary>
        /// Passive soil carbon g/m2(SOMC3 in Century)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double passiveSoilCarbon { get; private set; }

        /// <summary>
        /// Soil organic matter nitrogen, surface and soil  g/m2(SOM1E in Century and SSOM1E in Savanna)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] fastSoilNitrogen { get; private set; } = new double[2];

        /// <summary>
        /// Intermediate soil nitrogen g/m2(SOM2E in Century)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double intermediateSoilNitrogen { get; private set; }

        /// <summary>
        /// Passive soil nitrogen g/m2(SOM3E in Century)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double passiveSoilNitrogen { get; private set; }

        /// <summary>
        /// Calculated potential production for the cell, an index.Based on soil temperature, so not specific to facets.
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double potentialProduction { get; private set; }

        /// <summary>
        /// BIOMASS, Belowground potential production in g/m2
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] belowgroundPotProduction { get; private set; }  = new double[nLayers];

        /// <summary>
        /// BIOMASS, Aboveground potential production in g/m2
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] abovegroundPotProduction { get; private set; } = new double[nLayers];

        /// <summary>
        /// BIOMASS, Calculate total potential production, in g/m2 with all the corrections in place. 
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] totalPotProduction { get; private set; }  = new double[nLayers];

        /// <summary>
        /// Calculated effect of CO2 increasing from 350 to 700 ppm on grassland production, per facet
        /// </summary>
        [XmlIgnore]
        public double[] co2EffectOnProduction { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coefficient on total potential production reflecting limits due to nitrogen in place(EPRODL)
        /// </summary>
        [XmlIgnore]
        public double[] totalPotProdLimitedByN { get; private set; } = new double[nLayers];

        /// <summary>
        /// Monthly net primary production in g/m2, summed from total_pot_prod_limited_by_n
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double monthlyNetPrimaryProduction { get; private set; }

        /// <summary>
        /// Fraction of live forage removed by grazing  (FLGREM in CENTURY)
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double fractionLiveRemovedGrazing { get; private set; }

        /// <summary>
        /// Fraction of dead forage removed by grazing(FDGREM in CENTURY)
        /// </summary>
        [Units("0-1")]
        [XmlIgnore]
        public double fractionDeadRemovedGrazing { get; private set; }

        // Facets are used here.Facets are: 1 - Herb, 2 - Shrub, 3 - Tree
        // NOT USED RIGHT NOW:  The array index here is:  1 - Phenological death, 2 - Incremental death, 3 - Herbivory, 4 - Fire

        /// <summary>
        /// Temperature effect on decomposition (TFUNC in CENTURY Cycle.f)  (index)
        /// </summary>
        [XmlIgnore]
        public double tempEffectOnDecomp { get; private set; }

        /// <summary>
        /// Water effect on decomposition (index)  (Aboveground and belowground entries in CENTURY set to equal, so distinction not made here)
        /// </summary>
        [XmlIgnore]
        public double waterEffectOnDecomp { get; private set; }

        /// <summary>
        /// Anerobic effects on decomposition(index)  (EFFANT in Savanna)
        /// </summary>
        [XmlIgnore]
        public double anerobicEffectOnDecomp { get; private set; }

        /// <summary>
        /// Combined effects on decomposition, which in Savanna includes anerobic(CYCLE.F)  (index)
        /// </summary>
        [XmlIgnore]
        public double allEffectsOnDecomp { get; private set; }

        /// <summary>
        /// Dead fine root carbon of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadFineRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine root nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadFineRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Standing dead carbon of leaf and stem, of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadStandingCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Standing dead nitrogen of leaf and stem, of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadStandingNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead seed carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadSeedCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead seed nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadSeedNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead leaf carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadLeafCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead leaf nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadLeafNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine branch carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadFineBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine branch carbon, summed across facets
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double deadTotalFineBranchCarbon { get { return _deadTotalFineBranchCarbon; } }
        private double _deadTotalFineBranchCarbon;

        /// <summary>
        /// Dead fine branch nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadFineBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead fine branch nitrogen, summed across facets
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double deadTotalFineBranchNitrogen { get { return _deadTotalFineBranchNitrogen; } }
        private double _deadTotalFineBranchNitrogen;

        /// <summary>
        /// Dead coarse root carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadCoarseRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse root carbon, summed across facets
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double deadTotalCoarseRootCarbon { get { return _deadTotalCoarseRootCarbon; } }
        private double _deadTotalCoarseRootCarbon;

        /// <summary>
        /// Dead coarse root nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadCoarseRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse root nitrogen, summed across facets
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double deadTotalCoarseRootNitrogen { get { return _deadTotalCoarseRootNitrogen; } }
        private double _deadTotalCoarseRootNitrogen;

        /// <summary>
        /// Dead coarse wood carbon of the four types cited above.   (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadCoarseBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse wood carbon, summed across facets
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double deadTotalCoarseBranchCarbon { get { return _deadTotalCoarseBranchCarbon; } }
        private double _deadTotalCoarseBranchCarbon;

        /// <summary>
        /// Dead coarse wood nitrogen of the four types cited above.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] deadCoarseBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Dead total coarse wood nitrogen, summed across facets
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double deadTotalCoarseBranchNitrogen { get { return _deadTotalCoarseBranchNitrogen; } }
        private double _deadTotalCoarseBranchNitrogen;

        /// <summary>
        /// Fine root lignin concentration
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] ligninFineRoot { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse root lignin concentration
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] ligninCoarseRoot { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine branch lignin concentration
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] ligninFineBranch { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse branch lignin concentration
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] ligninCoarseBranch { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf lignin concentration
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] ligninLeaf { get; private set; } = new double[nFacets];

        /// <summary>
        /// Lignin in structural residue, at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (STRLIG)
        /// </summary>
        [XmlIgnore]
        public double[,] plantLigninFraction { get; private set; } = new double[nFacets, 2];

        /// <summary>
        /// Litter structural carbon at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (STRCIS, or in Savanna, SSTRCIS, with unlabeled and labeled merged)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] litterStructuralCarbon { get; private set; } = new double[2];   

        /// <summary>
        /// Litter metabolic carbon at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (METCIS, or in Savanna, SMETCIS)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] litterMetabolicCarbon { get; private set; } = new double[2];

        /// <summary>
        /// Litter structural nitrogen at the surface(1) and in the soil(2)  (STRUCE, or in Savanna, SSTRUCE, with STRUCE named for "elements"  I am only including nitrogen, as in Savanna, so dropping the name)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] litterStructuralNitrogen { get; private set; } = new double[2];

        /// <summary>
        /// Litter structural nitrogen at the surface(1) and in the soil(2)  (METABE, or in Savanna, SSTRUCE, with STRUCE named for "elements"  I am only including nitrogen, as in Savanna, so dropping the name)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
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
        [XmlIgnore]
        public double[] phenology { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine root carbon    (gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] fineRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine root nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] fineRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Seed carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] seedCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Seed nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] seedNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] leafCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] leafNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine branch carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] fineBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Fine branch nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] fineBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse root carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] coarseRootCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse root nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] coarseRootNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse branch carbon(gC/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] coarseBranchCarbon { get; private set; } = new double[nFacets];

        /// <summary>
        /// Coarse branch nitrogen(gN/m^2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] coarseBranchNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Stored nitrogen(CRPSTG in Century GROWTH, STORAGE in RESTRP).  I can't find where this is initialized, except for a gridded system input.  Assumed 0 for now, but here as a placeholder.
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] storedNitrogen { get; private set; } = new double[nFacets];

        /// <summary>
        /// Plant nitrogen fixed
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] plantNitrogenFixed { get; private set; } = new double[nFacets];

        /// <summary>
        /// Nitrogen fixed.  Not sure what components distinguish it, just yet.  (NFIX)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] nitrogenFixed { get; private set; } = new double[nFacets];

        /// <summary>
        /// Maintenance respiration flows to storage pool(MRSPSTG)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] respirationFlows { get; private set; } = new double[nFacets];

        /// <summary>
        /// Maintenance respiration flows for year(MRSPANN)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double[] respirationAnnual { get; private set; } = new double[nFacets];

        private double carbonSourceSink;                              // Carbon pool.  (g / m2)(CSRSNK)    I don't know the utility of this, but incorporating it.
        private double nitrogenSourceSink;                            // Nitrogen pool.  (g / m2)(ESRSNK)
        private double[,] carbonAllocation = new double[nFacets, nWoodyParts]; // Shrub carbon allocation, by proportion(TREE_CFAC in Century, except statis here)  Brought into this array, even though it requires more memory, in case I do incorporate dynamic allocation at some point.

        /// <summary>
        /// Optimum leaf area index
        /// </summary>
        [XmlIgnore]
        public double[] optimumLeafAreaIndex { get; private set; } = new double[nFacets];

        /// <summary>
        /// Leaf area index
        /// </summary>
        [XmlIgnore]
        public double[] leafAreaIndex { get; private set; } = new double[nFacets];

        /// <summary>
        /// Water function influencing mortality(AGWFUNC and BGWFUNC in Century, merged here since CYCLE assigns them equal and they start with the same value)
        /// </summary>
        [XmlIgnore]
        public double waterFunction { get; private set; }

        /// <summary>
        /// A score from 0 to 1 reflecting fire intensity
        /// </summary>
        [XmlIgnore]
        public double fireSeverity { get; private set; }

        /// <summary>
        /// The sum of carbon burned, only on the 1 m plots, not whole plant death
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double burnedCarbon { get; private set; }

        /// <summary>
        /// The sum of nitrogen burned, only on the 1 m plots, not whole plant death
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double burnedNitrogen { get; private set; }

        /// <summary>
        /// Total fertilized nitrogen added(g / m2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double fertilizedNitrogenAdded { get; private set; }

        /// <summary>
        /// Total fertilized carbon added(g / m2)
        /// </summary>
        [Units("g/m^2")]
        [XmlIgnore]
        public double fertilizedCarbonAdded { get; private set; }

        // private int largeErrorCount;  // The count of cells being reset because their values were very very large
        // private int negErrorCount;    // The count of cell being reset because values were below zero
       
        // Additional output variables added for convenience in the Apsim context - EJZ
        /// <summary>
        /// Herbaceous facet aboveground net primary production
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double aboveGroundHerbNPP { get; private set; }

        /// <summary>
        /// Herbaceous facet aboveground net primary production
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double aboveGroundShrubNPP { get; private set; }

        /// <summary>
        /// Herbaceous facet aboveground net primary production
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]

        public double aboveGroundTreeNPP { get; private set; }
        /// <summary>
        /// Indicate what we are. Dummied for use with CLEM for DARPA project
        /// </summary>

        public string CropType { get; private set; } = "NativePasture";

#if !G_RANGE_BUG
        private double proportionCellBurned; // Here to correct inconsistent burining in original
        private bool doingSpinUp;    // Flags that we are doing a spinup
#endif

#endregion

        /// <summary>
        /// An enumeration of possible data source mechanisms for initialising soil properties
        /// </summary>
        public enum SoilDataSourceEnum {
            /// <summary>
            /// Take as much as possible from APSIM
            /// </summary>
            [Description("Use soil properties from APSIM (n layers)")]
            APSIM,
            /// <summary>
            /// Use APSIM values, but collapse to 4 layers
            /// </summary>
            [Description("Use soil properties from APSIM (4 layers)")]
            APSIM_4Layer,
            /// <summary>
            /// Use APSIM values, but collapse to 2 layers
            /// </summary>
            [Description("Use soil properties from APSIM (2 layers)")]
            APSIM_2Layer,
            /// <summary>
            /// Take physical properties from APSIM (sand, silt, clay), but use G-Range pedotransfer functions for wilting point (LL12) and field capacity (DUL)
            /// </summary>
            [Description("Use APSIM physical properities, and G-Range pedotransfer functions (n layers)")]
            APSIMPhysical,
            /// <summary>
            /// Take physical properties from APSIM (sand, silt, clay), but use G-Range pedotransfer functions for wilting point (LL12) and field capacity (DUL)
            /// </summary>
            [Description("Use APSIM physical properities, and G-Range pedotransfer functions (4 layers)")]
            APSIMPhysical_4Layer,
            /// <summary>
            /// Take physical properties from APSIM (sand, silt, clay), but use G-Range pedotransfer functions for wilting point (LL12) and field capacity (DUL)
            /// </summary>
            [Description("Use APSIM physical properities, and G-Range pedotransfer functions (2 layers)")]
            APSIMPhysical_2Layer,
            /// <summary>
            /// Take everything from the G-Range database
            /// </summary>
            [Description("Use soil properties from the G-Range database (2 layers)")]
            G_Range
        };

        /// <summary>
        /// Gets or sets the source to use for initialisation of soil properties
        /// </summary>
        [Summary]
        [Description("Source from which to obtain soil properties")]
        public SoilDataSourceEnum SoilDataSource { get; set; } = SoilDataSourceEnum.G_Range;

        /// <summary>
        /// An enumeration of biomes, to allow the user to select their preference (or fall back to G-Range's map)
        /// </summary>
        public enum BiomeEnum
        {
            /// <summary>
            /// Use the biome specified in the G-Range SAGE map
            /// </summary>
            [Description("Use biome from G-Range map")]
            Unspecified,
            /// <summary>
            /// TROPICAL EVERGREEN FOREST / WOODLAND
            /// </summary>
            [Description("Tropical evergreen forest and woodland")]
            TropicalEGreen,
            /// <summary>
            /// TROPICAL DECIDUOUS FOREST / WOODLAND
            /// </summary>
            [Description("Tropical deciduous forest and woodland")]
            TropicalDeciduous,
            /// <summary>
            /// TEMPERATE BROADLEAF EVERGREEN FOREST / WOODLAND
            /// </summary>
            [Description("Temperate broadleaf evergreen forest")]
            TemperateBroadEGreen,
            /// <summary>
            /// TEMPERATE NEEDLEAF EVERGREEN FOREST / WOODLAND
            /// </summary>
            [Description("Temperate needleleaf evergreen forest")]
            TemperateNeedleEGreen,
            /// <summary>
            /// TEMPERATE DECIDUOUS FOREST / WOODLAND
            /// </summary>
            [Description("Temperate deciduous forest and woodland")]
            TemperateDecid,
            /// <summary>
            /// BOREAL EVERGREEN FOREST / WOODLAND 
            /// </summary>
            [Description("Boreal evergreen forest and woodland")]
            BorealEGreen,
            /// <summary>
            /// BOREAL DECIDUOUS FOREST / WOODLAND 
            /// </summary>
            [Description("Boreal deciduous forest and woodland")]
            BorealDecid,
            /// <summary>
            /// EVERGREEN / DECIDUOUS MIXED FOREST / WOODLAND
            /// </summary>
            [Description("Evergreen / deciduous mixed forest")]
            TemperateMixed,
            /// <summary>
            /// SAVANNA
            /// </summary>
            [Description("Savanna")]
            Savanna,
            /// <summary>
            /// GRASSLAND / STEPPE
            /// </summary>
            [Description("Grassland steppe")]
            Steppe,
            /// <summary>
            /// DENSE SHRUBLAND
            /// </summary>
            [Description("Dense shrubland")]
            DenseShrub,
            /// <summary>
            /// OPEN SHRUBLAND
            /// </summary>
            [Description("Open shrubland")]
            OpenShrub,
            /// <summary>
            /// TUNDRA
            /// </summary>
            [Description("Tundra")]
            Tundra,
            /// <summary>
            /// DESERT
            /// </summary>
            [Description("Desert")]
            Desert,
            /// <summary>
            /// POLAR DESERT / ROCK / ICE 
            /// </summary>
            [Description("Polar desert")]
            PolarDesert
        };

        /// <summary>
        /// Gets or sets the biome to use for parameter selection
        /// </summary>
        [Summary]
        [Description("Biome type to use for parameter selection")]
        public BiomeEnum BiomeType { get; set; } = BiomeEnum.Unspecified;

        /// <summary>
        /// Gets or sets the latitude for the site being modelled. Should be in the range -90 to 90
        /// </summary>
        [Summary]
        [Description("Latitude (if NaN, take from Weather)")]
        public double Latitude { get; set; } = Double.NaN;

        /// <summary>
        /// Gets or sets the longitude for the site being modelled. Should be in the range -180 to 180
        /// </summary>
        [Summary]
        [Description("Longitude (if NaN, take from Weather")]
        public double Longitude { get; set; } = Double.NaN;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Display(Type = DisplayType.FileName)]
        [Description("G_Range database file name")]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Display(Type = DisplayType.FileName)]
        [Description("G_Range parameter file (blank for default values)")]
        public string ParameterFileName { get; set; } = String.Empty;
        /// <summary>
        /// Gets or sets the number of years to spinup the model
        /// </summary>
        [Summary]
        [Description("Spinup years")]
        public double spinupYears { get; set; } = 250;

        private double[] partBasedDeathRate = new double[nFacets];

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
            if (!globe.rangeland)
                throw new ApsimXException(this, "G-Range cannot treat the specified location as rangeland!");
            InitParms();    // Initialize_Rangelands
#if !G_RANGE_BUG
            // I need to find a way to spin up the simulation. It takes a couple of decades for things to come to a near-equilibrium
            // The orignal G-Range uses a file to store state from a previous run to avoid this problem.
            SpinUp();
#endif
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
            RunMonthlyModel();
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

        private void RunMonthlyModel()
        {
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

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        { }

#if !G_RANGE_BUG
        private void SpinUp()
        {
            doingSpinUp = true;
            // Run for 250 years
            for (int iYear = 0; iYear < spinupYears; iYear++)
            {
                EachYear();
                for (month = 1; month <= 12; month++)
                {
                    RunMonthlyModel();
                }
            }
            //Check Clock.StartDate and be sure we spin up to the day (month) before the start of the simulation.
            if (Clock.StartDate.Month > 1)
            {
                EachYear();
                for (month = 1; month < Clock.StartDate.Month; month++)
                    RunMonthlyModel();
            }
            doingSpinUp = false;
        }
#endif

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
            double wdbmas = (fineBranchCarbon[Facet.tree] + coarseBranchCarbon[Facet.tree]) * 2.0;
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
            // CHECK - EJZ

            // Moving the following from WATER_LOSS, since it was only being done with no snow present.
            double avgALiveBiomass = 0.0;
            double avgBLiveBiomass = 0.0;
            double[] biomassLivePerLayer = new double[nLayers];

            // ABOVEGROUND
            // Using method used in productivity.Does not use plant populations in layers, but uses the facets instead.Not at precise but less prone to vast swings.
            double totalCover = facetCover[Facet.herb] + facetCover[Facet.shrub] + facetCover[Facet.tree];
            double fracCover;
            if (totalCover > 0.000001) {
                // HERBS
                fracCover = facetCover[Facet.herb] / totalCover;
                biomassLivePerLayer[Layer.herb] = (leafCarbon[Facet.herb] + seedCarbon[Facet.herb]) * 2.5 * fracCover;
                fracCover = facetCover[Facet.shrub] / totalCover;
                biomassLivePerLayer[Layer.herbUnderShrub] = (leafCarbon[Facet.herb] + seedCarbon[Facet.herb]) * 2.5 * fracCover;
                fracCover = facetCover[Facet.tree] / totalCover;
                biomassLivePerLayer[Layer.herbUnderTree] = (leafCarbon[Facet.herb] + seedCarbon[Facet.herb]) * 2.5 * fracCover;

                // SHRUBS
                if ((facetCover[Facet.shrub] + facetCover[Facet.tree]) > 0.000001)
                {
                    fracCover = facetCover[Facet.shrub] / (facetCover[Facet.shrub] + facetCover[Facet.tree]);
                    biomassLivePerLayer[Layer.shrub] = (leafCarbon[Facet.shrub] + seedCarbon[Facet.shrub] +
                          fineBranchCarbon[Facet.shrub] + coarseBranchCarbon[Facet.shrub]) * 2.5 * fracCover;
                    fracCover = facetCover[Facet.tree] / (facetCover[Facet.shrub] + facetCover[Facet.tree]);
                    biomassLivePerLayer[Layer.shrubUnderTree] = (leafCarbon[Facet.shrub] + seedCarbon[Facet.shrub] +
                          fineBranchCarbon[Facet.shrub] + coarseBranchCarbon[Facet.shrub]) * 2.5 * fracCover;
                }
                else
                {
                    biomassLivePerLayer[Layer.shrub] = 0.0;
                    biomassLivePerLayer[Layer.tree] = 0.0;
                }

                // TREES
                fracCover = facetCover[Facet.tree];
                biomassLivePerLayer[Layer.tree] = (leafCarbon[Facet.tree] + seedCarbon[Facet.tree] +
                          fineBranchCarbon[Facet.tree] + coarseBranchCarbon[Facet.tree]) * 2.5 * fracCover;

                for (int iLyr = 0; iLyr < nLayers; iLyr++)
                    avgALiveBiomass = avgALiveBiomass + biomassLivePerLayer[iLyr];
            }
            else
                avgALiveBiomass = 0.0;       // There is no cover on the cell

            // BELOWGROUND
            if (totalCover > 0.000001)
            {
                // HERBS
                fracCover = facetCover[Facet.herb] / totalCover;
                biomassLivePerLayer[Layer.herb] = fineRootCarbon[Facet.herb] * 2.5 * fracCover;
                fracCover = facetCover[Facet.shrub] / totalCover;
                biomassLivePerLayer[Layer.herbUnderShrub] = fineRootCarbon[Facet.herb] * 2.5 * fracCover;
                fracCover = facetCover[Facet.tree] / totalCover;
                biomassLivePerLayer[Layer.herbUnderTree] = fineRootCarbon[Facet.herb] * 2.5 * fracCover;
                // SHRUBS
                if ((facetCover[Facet.shrub] + facetCover[Facet.tree]) > 0.000001)
                {
                    fracCover = facetCover[Facet.shrub] / (facetCover[Facet.shrub] + facetCover[Facet.tree]);
                    biomassLivePerLayer[Layer.shrub] =
                        (fineRootCarbon[Facet.shrub] + coarseRootCarbon[Facet.shrub]) * 2.5 * fracCover;
                    fracCover = facetCover[Facet.tree] / (facetCover[Facet.shrub] + facetCover[Facet.tree]);
                    biomassLivePerLayer[Layer.shrubUnderTree] =
                         (fineRootCarbon[Facet.shrub] + coarseRootCarbon[Facet.shrub]) * 2.5 * fracCover;
                }
                else
                {
                    biomassLivePerLayer[Layer.shrub] = 0.0;
                    biomassLivePerLayer[Layer.tree] = 0.0;
                }

                // TREES
                fracCover = facetCover[Facet.tree];
                biomassLivePerLayer[Layer.tree] =
                         (fineRootCarbon[Facet.tree] + coarseRootCarbon[Facet.tree]) * 2.5 * fracCover;


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
                (totalPotProdLimitedByN[Layer.herb] * facetCover[Facet.herb]) +
                (totalPotProdLimitedByN[Layer.herbUnderShrub] * facetCover[Facet.shrub]) +
                (totalPotProdLimitedByN[Layer.herbUnderTree] * facetCover[Facet.tree]) +
                (totalPotProdLimitedByN[Layer.shrub] * facetCover[Facet.shrub]) +
                (totalPotProdLimitedByN[Layer.shrubUnderTree] * facetCover[Facet.tree]) +
                (totalPotProdLimitedByN[Layer.tree] * facetCover[Facet.tree]);

            // APSIM: Calculate above ground NPP for the herbaceous facet
            if (totalPotProduction[Layer.herb] > 0.0)
                aboveGroundHerbNPP = totalPotProdLimitedByN[Layer.herb] * abovegroundPotProduction[Layer.herb] / totalPotProduction[Layer.herb] * facetCover[Facet.herb];
            else
                aboveGroundHerbNPP = 0.0;
            if (totalPotProduction[Layer.herbUnderShrub] > 0.0)
                aboveGroundHerbNPP += totalPotProdLimitedByN[Layer.herbUnderShrub] * abovegroundPotProduction[Layer.herbUnderShrub] / totalPotProduction[Layer.herbUnderShrub] * facetCover[Facet.shrub];
            if (totalPotProduction[Layer.herbUnderTree] > 0.0)
                aboveGroundHerbNPP += totalPotProdLimitedByN[Layer.herbUnderTree] * abovegroundPotProduction[Layer.herbUnderTree] / totalPotProduction[Layer.herbUnderTree] * facetCover[Facet.tree];
            aboveGroundHerbNPP *= 10.0; // Convert units

            // APSIM: Calculate above ground NPP for the shrub facet
            if (totalPotProduction[Layer.shrub] > 0.0)
                aboveGroundShrubNPP = totalPotProdLimitedByN[Layer.shrub] * abovegroundPotProduction[Layer.shrub] / totalPotProduction[Layer.shrub] * facetCover[Facet.shrub];
            else
                aboveGroundShrubNPP = 0.0;
            if (totalPotProduction[Layer.shrubUnderTree] > 0.0)
                aboveGroundShrubNPP += totalPotProdLimitedByN[Layer.shrubUnderTree] * abovegroundPotProduction[Layer.shrubUnderTree] / totalPotProduction[Layer.shrubUnderTree] * facetCover[Facet.tree];
            aboveGroundShrubNPP *= 10.0; // Convert units

            // APSIM: Calculate above ground NPP for the tree facet
            if (totalPotProduction[Layer.tree] > 0.0)
                aboveGroundTreeNPP = totalPotProdLimitedByN[Layer.tree] * abovegroundPotProduction[Layer.tree] / totalPotProduction[Layer.tree] * facetCover[Facet.tree];
            else
                aboveGroundTreeNPP = 0.0;
            aboveGroundTreeNPP *= 10.0; // Convert units

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
                    carbonAllocation[Facet.shrub, iPart] = (parms.shrubCarbon[iPart, ALIVE] +
                                                                 parms.shrubCarbon[iPart, DEAD]) / shrubCSum;
            }
            else
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[Facet.shrub, iPart] = 0.0;
            }
            if (treeCSum > 0.0)
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[Facet.tree, iPart] = (parms.treeCarbon[iPart, ALIVE] +
                                                        parms.shrubCarbon[iPart, DEAD]) / treeCSum;
            }
            else
            {
                for (int iPart = 0; iPart < nWoodyParts; iPart++)
                    carbonAllocation[Facet.tree, iPart] = 0.0;
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
            co2EffectOnProduction[Facet.herb] = co2Effect;
            co2EffectOnProduction[Facet.shrub] = co2Effect;
            co2EffectOnProduction[Facet.tree] = co2Effect;
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
            // Note that I'm calling only GetFields, not GetProperties. However, we should be able to
            // see Properties through their backing fields. This is adequate, provided the properties aren't making use of accessors.
            FieldInfo[] fields = myType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo fieldInfo in fields)
            {
                string fieldName = fieldInfo.Name;
                if (fieldName.EndsWith(">k__BackingField"))
                    fieldName = fieldName.Substring(1, fieldName.Length - 17);
                if (fieldName.Equals("Latitude", StringComparison.OrdinalIgnoreCase) || fieldName.Equals("Longitude", StringComparison.OrdinalIgnoreCase)) // These can be negative
                    continue;
                if (fieldInfo.FieldType == typeof(Double))
                {
                    var = TestDouble((double)fieldInfo.GetValue(this), fieldName);
                    if (!Double.IsNaN(var))
                        fieldInfo.SetValue(this, var);
                }
                else if (fieldInfo.FieldType == typeof(Double[]))
                {
                    Double[] array = (Double[])fieldInfo.GetValue(this);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var = TestDouble(array[i], fieldName + '[' + i.ToString() + ']');
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
                            var = TestDouble(array[i, j], fieldName + '[' + i.ToString() + ',' + j.ToString() + ']');
                            if (!Double.IsNaN(var))
                                array[i, j] = var;
                        }
                    }
                }
            }

            // What it retained here from the Fortran original is the logic for checking the bounds on grazing.

            double live_carbon = leafCarbon[Facet.herb] + leafCarbon[Facet.shrub] + leafCarbon[Facet.tree] +
                                 fineBranchCarbon[Facet.shrub] + fineBranchCarbon[Facet.tree];
            double dead_carbon = deadStandingCarbon[Facet.herb] + deadStandingCarbon[Facet.shrub] + deadStandingCarbon[Facet.tree];
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