using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    using System.Xml.Serialization;
    using Models.Core;
    using Models.Soils;
    using Models.Soils.Arbitrator;
    using Models.Interfaces;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Implements the plant growth model logic abstracted from G-Range
    /// Currently this is just an empty stub
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public partial class GRange : Model, IPlant, ICanopy, IUptake
    {
        #region Links

        //[Link]
        //private Clock Clock = null;

        //[Link]
        //private IWeather Weather = null;

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
        public double Legumosity { get { return 0; } }

        /// <summary>Gets a value indicating whether the biomass is from a c4 plant or not</summary>
        public bool IsC4 { get { return false; } }

        /// <summary> Is the plant alive?</summary>
        public bool IsAlive { get { return true; } }

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
        public double Gsmax { get { return 0.01; } }

        /// <summary>Gets or sets the R50.</summary>
        public double R50 { get { return 200; } }

        /// <summary>Gets the LAI</summary>
        [Description("Leaf Area Index (m^2/m^2)")]
        [Units("m^2/m^2")]
        public double LAI { get; set; }

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
        public GRange()
        {
            Name = "GRange";
        }

        #region Elements from Fortran code

        // I've pulled in all the members of the original G-Range RangeCell structure, but not all are used (yet)
        // These pragmas disable warnings related to the declaration of unused variables.

#pragma warning disable 0414

#pragma warning disable 0169

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
            treeLayer
        };

        /// <summary>
        /// Total number of layers (V_LYRS)
        /// </summary>
        private const int vLayers = 6;

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

        /// <summary>
        /// X dimension of rangeland cell
        /// </summary>
        public int X;

        /// <summary>
        /// Y dimension of rangeland cell
        /// </summary>
        public int Y;

        private int rangeType = 3;                 // Identifier storing the type of rangeland cell, used as a key to the Parms strcuture

        private double lastMonthDayLength;     // The day length of the previous month, to know when spring and fall come.
        private bool dayLengthIncreasing;      // Increasing or decreasing day length, comparing the current to previous day lengths.
        private double dayLength;              // Day length, calculated based on latitude and month

        private double heatAccumulation;       // Heat accumulation above a base temperature (e.g., 4.4 C in Boone (1999))

        private double[] facetCover = new double[nFacets];      // The proportion occupied by each facet
        private double[] totalPopulation = new double[vLayers]; // The total population of each vegetation layer

        private double bareCover;              // Bare cover stored, rather than continually summing the three facets.
        private double[] propAnnualDecid = new double[nFacets];  // Proportion of facet that is annual plants (H_FACET) or deciduous (S_FACET and T_FACET)

        private double potEvap;                // Potential evapotranspiration for the cell(cm/month)
        private double evaporation;            // Water evaporated from the soil and vegetation(cm/month)
        private double snow;                   // Snowpack, in cm
        private double snowLiquid;             // Snowpack liquid water.
        
        // Editing the model 01/02/2014 to prevent snow and snow liquid from skyrocketing.   Adding an field for OLD SNOW and ICE, prior to clearing out snow each year.
        private double oldSnow;                // Snow from past years, including glacial build up.   This will be an accumulator, but essentially outside of active process modeling
        private double oldSnowLiquid;          // Ditto
        // This won't be output until there is some need for it.
        // End of addition

        private double melt;                   // Snow that melts from snowpack (cm water)
        private double petRemaining;           // Potential evaporation decremented as steps are calculated.Appears to be a bookkeeping tool.

        private double pptSoil;                // Precipitation adjusted for snow accumulation and melt, and available to infiltrate the soil (cm)
        private double runoff;                 // Runoff from the rangeland cell
        private double ratioWaterPet;          // Ratio of available water to potential evapotranspiration
        private double petTopSoil;             // Potential evaporation from top soil (cm/day)
        private double[] nLeached = new double[nSoilLayers];  // Nitrogen leached from soil(AMTLEA in Century)
        private double[] asmos = new double[nSoilLayers];     // Used in summing water
        private double[] amov = new double[nSoilLayers];      // Used in summing water movement
        private double stormFlow;              // Storm flow
        private double holdingTank;            // Stores water temporarily.Was asmos(layers+1) in H2OLos
        private double transpiration;          // Transpiration water loss
        private double[] relativeWaterContent = new double[nSoilLayers]; // Used to initialize and during simulation in CENTURY.Here, only during simulation
        private double[] waterAvailable = new double[3];                 // Water available to plants, available for growth =(1) [0 in C#], survival(2) [1 in C#], and in the two top layers(3) [2 in C#]
        private double annualEvapotranspiration;                         // Annual actual evapotranspiration

        private double totalAgroundLiveBiomass;          // Total aboveground live biomass (g/m^2)
        private double totalBgroundLiveBiomass;          // Total belowground live biomass(g/m^2)

        private double[] totalLitterCarbon = new double[2];   // Average monthly litter carbon(g/m^2)
        private double[] totalLitterNitrogen = new double[2]; // Average monthly litter carbon(g/m^2)

        private double[] rootShootRatio = new double[nFacets]; // Root shoot ratio
        private double treeBasalArea;                  // Basal area for trees

        private double soilSurfaceTemperature;         // Average soil surface temperature (C)

        // Soils as in Century 4.5 NLayer= 4, 0-15, 15-30, 30-45, 45-60 cm.
        // These will be initialized using approximations and weighted averages from HWSD soils database, which is 0-30 for TOP, 30-100 for SUB.
        private double[] sand = new double[4];                 // The percent sand in the soil
        private double[] silt = new double[4];                 // The percent silt in the soil
        private double[] clay = new double[4];                 // The percent clay in the soil
        private double[] mineralNitrogen = new double[4];      // Mineral nitrogen content for layer(g/m2)
        private double[] soilDepth = new double[] { 15.0, 15.0, 15.0, 15.0 }; // The depth of soils, in cm.Appears hardwired in some parts of CENTURY, flexible, and up to 9 layers, in other parts of CENTURY.Likely I noted some values from an early version, but this is a simplification, so...

        private double[] fieldCapacity = new double[4];        // Field capacity for four soils layers shown above.
        private double[] wiltingPoint = new double[4];         // Wilting point for four soil layers shown above.
        private double soilTotalCarbon;                        // grams per square meter

        private double[] treeCarbon = new double[nWoodyParts];    // Tree carbon in its components.These must all be merged or otherwise crosswalked at some point.
        private double[] treeNitrogen = new double[nWoodyParts];  // Tree nitrogen in its components.   These must all be merged or otherwise crosswalked at some point.
        private double[] shrubCarbon = new double[nWoodyParts];   // Shrub carbon in its components.   These must all be merged or otherwise crosswalked at some point.
        private double[] shrubNitrogen = new double[nWoodyParts]; // Shrub nitrogen in its components.   These must all be merged or otherwise crosswalked at some point.

        private double[] carbonNitrogenRatio = new double[2];  // Carbon to nitrogen ratio, SURFACE, SOIL
        private double[] fastSoilCarbon = new double[2];       // Soil organic matter carbon, surface and soil  g/m2(SOM1C in Century)
        private double intermediateSoilCarbon;                 // Intermediate soil carbon g/m2(SOMC2 in Century)
        private double passiveSoilCarbon;                      // Passive soil carbon g/m2(SOMC3 in Century)
        private double[] fastSoilNitrogen = new double[2];     // Soil organic matter nitrogen, surface and soil  g/m2(SOM1E in Century and SSOM1E in Savanna)
        private double intermediateSoilNitrogen;               // Intermediate soil nitrogen g/m2(SOM2E in Century)
        private double passiveSoilNitrogen;                    // Passive soil nitrogen g/m2(SOM3E in Century)

        private double potentialProduction;                    // Calculated potential production for the cell, an index.Based on soil temperature, so not specific to facets.
        private double[] belowgroundPotProduction = new double[vLayers]; // BIOMASS, Belowground potential production in g/m2
        private double[] abovegroundPotProduction = new double[vLayers]; // BIOMASS, Abovegroudn potential production in g/m2
        private double[] totalPotProduction = new double[vLayers];       // BIOMASS, Calculate total potential production, in g/m2 with all the corrections in place.
        private double[] co2EffectOnProduction = new double[nFacets];    // Calculated effect of CO2 increasing from 350 to 700 ppm on grassland production, per facet

        private double[] totalPotProdLimitedByN = new double[vLayers];   // Coefficient on total potential production reflecting limits due to nitrogen in place(EPRODL)
        private double monthlyNetPrimaryProduction;            // Monthly net primary production in g/m2, summed from total_pot_prod_limited_by_n

        private double fractionLiveRemovedGrazing;             // Fraction of live forage removed by grazing  (FLGREM in CENTURY)
        private double fractionDeadRemovedGrazing;             // Fraction of dead forage removed by grazing(FDGREM in CENTURY)

        // Facets are used here.Facets are: 1 - Herb, 2 - Shrub, 3 - Tree
        // NOT USED RIGHT NOW:  The array index here is:  1 - Phenological death, 2 - Incremental death, 3 - Herbivory, 4 - Fire
        private double tempEffectOnDecomp;           // Temperature effect on decomposition (TFUNC in CENTURY Cycle.f)  (index)
        private double waterEffectOnDecomp;          // Water effect on decomposition (index)  (Aboveground and belowground entries in CENTURY set to equal, so distinction not made here)
        private double anerobicEffectOnDecomp;       // Anerobic effects on decomposition(index)  (EFFANT in Savanna)
        private double allEffectsOnDecomp;           // Combined effects on decomposition, which in Savanna includes anerobic(CYCLE.F)  (index)

        private double[] deadFineRootCarbon = new double[nFacets];     // Dead fine root carbon of the four types cited above.
        private double[] deadFineRootNitrogen = new double[nFacets];   // Dead fine root nitrogen of the four types cited above.
        private double[] deadStandingCarbon = new double[nFacets];     // Standing dead carbon of leaf and stem, of the four types cited above.
        private double[] deadStandingNitrogen = new double[nFacets];   // Standing dead nitrogen of leaf and stem, of the four types cited above.
        private double[] deadSeedCarbon = new double[nFacets];         // Dead seed carbon of the four types cited above.   (gC/m^2)
        private double[] deadSeedNitrogen = new double[nFacets];       // Dead seed nitrogen of the four types cited above.           (units?)
        private double[] deadLeafCarbon = new double[nFacets];         // Dead leaf carbon of the four types cited above.   (gC/m^2)
        private double[] deadLeafNitrogen = new double[nFacets];       // Dead leaf nitrogen of the four types cited above.
        private double[] deadFineBranchCarbon = new double[nFacets];   // Dead fine branch carbon of the four types cited above.   (gC/m^2)
        private double deadTotalFineBranchCarbon;                      // Dead fine branch carbon, summed across facets
        private double[] deadFineBranchNitrogen = new double[nFacets]; // Dead fine branch nitrogen of the four types cited above.
        private double deadTotalFineBranchNitrogen;                    // Dead fine branch nitrogen, summed across facets
        private double[] deadCoarseRootCarbon = new double[nFacets];   // Dead coarse root carbon of the four types cited above.   (gC/m^2)
        private double deadTotalCoarseRootCarbon;                      // Dead total coarse root carbon, summed across facets
        private double[] deadCoarseRootNitrogen = new double[nFacets]; // Dead coarse root nitrogen of the four types cited above.
        private double deadTotalCoarseRootNitrogen;                    // Dead total coarse root nitrogen, summed across facets
        private double[] deadCoarseBranchCarbon = new double[nFacets]; // Dead coarse wood carbon of the four types cited above.   (gC/m^2)
        private double deadTotalCoarseBranchCarbon;                    // Dead total coarse wood carbon, summed across facets
        private double[] deadCoarseBranchNitrogen = new double[nFacets]; // Dead coarse wood nitrogen of the four types cited above.
        private double deadTotalCoarseBranchNitrogen;                  // Dead total coarse wood nitrogen, summed across facets

        private double[] ligninFineRoot = new double[nFacets];         // Fine root lignin concentration
        private double[] ligninCoarseRoot = new double[nFacets];       // Coarse root lignin concentration
        private double[] ligninFineBranch = new double[nFacets];       // Fine branch lignin concentration
        private double[] ligninCoarseBranch = new double[nFacets];     // Coarse branch lignin concentration
        private double[] ligninLeaf = new double[nFacets];             // Leaf lignin concentration

        private double[,] plantLigninFraction = new double[nFacets, 2];   // Lignin in structural residue, at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (STRLIG)
        private double[] litteStructuralCarbon = new double[2];           // Litter structural carbon at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (STRCIS, or in Savanna, SSTRCIS, with unlabeled and labeled merged)
        private double[] litterMetabolicCarbon = new double[2];           // Litter metabolic carbon at the surface(1)[0 in C#] and in the soil(2)[1 in C#]  (METCIS, or in Savanna, SMETCIS)
        private double[] litterStructuralNitrogen = new double[2];        // Litter structural nitrogen at the surface(1) and in the soil(2)  (STRUCE, or in Savanna, SSTRUCE, with STRUCE named for "elements"  I am only including nitrogen, as in Savanna, so dropping the name)
        private double[] litterMetabolicNitrogen = new double[2];         // Litter structural nitrogen at the surface(1) and in the soil(2)  (METABE, or in Savanna, SSTRUCE, with STRUCE named for "elements"  I am only including nitrogen, as in Savanna, so dropping the name)

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

        private double[] phenology = new double[nFacets];             // Phenological stage, a continuous variable from 0 to 4.

        private double[] fineRootCarbon = new double[nFacets];        // Fine root carbon    (gC/m^2)
        private double[] fineRootNitrogen = new double[nFacets];      // Fine root nitrogen(gN/m^2)
        private double[] seedCarbon = new double[nFacets];            //  Seed carbon(gC/m^2)
        private double[] seedNitrogen = new double[nFacets];          // Seed nitrogen(gN/m^2)
        private double[] leafCarbon = new double[nFacets];            // Leaf carbon(gC/m^2)
        private double[] leafNitrogen = new double[nFacets];          // Leaf nitrogen(gN/m^2)
        private double[] fineBranchCarbon = new double[nFacets];      // Fine branch carbon(gC/m^2)
        private double[] fineBranchNitrogen = new double[nFacets];    // Fine branch nitrogen(gN/m^2)
        private double[] coarseRootCarbon = new double[nFacets];      // Coarse root carbon(gC/m^2)
        private double[] coarseRootNitrogen = new double[nFacets];    // Coarse root nitrogen(gN/m^2)
        private double[] coarseBranchCarbon = new double[nFacets];    // Coarse branch carbon(gC/m^2)
        private double[] coarseBranchNitrogen = new double[nFacets];  // Coarse branch nitrogen(gN/m^2)
        private double[] storedNitrogen = new double[nFacets];        // Stored nitrogen(CRPSTG in Century GROWTH, STORAGE in RESTRP).  I can't find where this is initialized, except for a gridded system input.  Assumed 0 for now, but here as a placeholder.
        private double[] plantNitrogenFixed = new double[nFacets];    // Plant nitrogen fixed
        private double[] nitrogenFixed = new double[nFacets];         // Nitrogen fixed.  Not sure what components distinguish it, just yet.  (NFIX)
        private double[] respirationFlows = new double[nFacets];      // Maintenance respiration flows to storage pool(MRSPSTG)
        private double[] respirationAnnual = new double[nFacets];     // Maintenance respiration flows for year(MRSPANN)

        private double carbonSourceSink;                              // Carbon pool.  (g / m2)(CSRSNK)    I don't know the utility of this, but incorporating it.
        private double nitrogenSourceSink;                            // Nitrogen pool.  (g / m2)(ESRSNK)
        private double[,] carbonAllocation = new double[nFacets, nWoodyParts]; // Shrub carbon allocation, by proportion(TREE_CFAC in Century, except statis here)  Brought into this array, even though it requires more memory, in case I do incorporate dynamic allocation at some point.

        private double[] optimumLeafAreaIndex = new double[nFacets];  // Optimum leaf area index
        private double[] leafAreaIndex = new double[nFacets];         // Leaf area index

        private double waterFunction;            // Water function influencing mortality(AGWFUNC and BGWFUNC in Century, merged here since CYCLE assigns them equal and they start with the same value)

        private double fireSeverity;             // A score from 0 to 1 reflecting fire intensity
        private double burnedCarbon;             // The sum of carbon burned, only on the 1 m plots, not whole plant death
        private double burnedNitrogen;           // The sum of nitrogen burned, only on the 1 m plots, not whole plant death

        private double fertilizedNitrogenAdded;  // Total fertilized nitrogen added(g / m2)
        private double fertilizedCarbonAdded;    // Total fertilized carbon added(g / m2)

        private int largeErrorCount;  // The count of cells being reset because their values were very very large
        private int negErrorCount;    // The count of cell being reset because values were below zero

        #endregion

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

            LoadParms();

            //// Work out where we are, what the vegetation type is, and load suitable params
            parms = parmArray[rangeType];
        }

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            UpdateWeather();
        }

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
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

        private void UpdateWeather()
        { }

        /// <summary>
        /// Processes that are required each year, prior to any process-based simulation steps.
        /// 
        /// Transcoded from Misc_Material.f95
        /// </summary>
        private void EachYear()
        {
            int iunit = rangeType;
            annualEvapotranspiration = 0.0;
            negErrorCount = 0;
            largeErrorCount = 0;

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
                deadTotalFineBranchCarbon = 0.0;
                deadTotalFineBranchNitrogen = 0.0;
                deadTotalCoarseBranchCarbon = 0.0;
                deadTotalCoarseBranchNitrogen = 0.0;
                deadTotalCoarseRootCarbon = 0.0;
                deadTotalCoarseRootNitrogen = 0.0;
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

                double precip_average = 100; // FIX THIS Globe(Rng(icell) % x, Rng(icell) % y) % precip_average
                // Recalculate the proportion of residue that is lignin, which follows from annual precipitation (CMPLIG.F in Century.No equilvalent in Savanna)
                plantLigninFraction[iFacet, surfaceIndex] = ligninLeaf[iFacet] +
                                       parms.ligninContentFractionAndPrecip[0, surfaceIndex] +
                                       (parms.ligninContentFractionAndPrecip[1, surfaceIndex] * precip_average) / 2.0;
            plantLigninFraction[iFacet, soilIndex] = ligninFineRoot[iFacet] +
                                    parms.ligninContentFractionAndPrecip[0, soilIndex] +
                                    (parms.ligninContentFractionAndPrecip[1, soilIndex] * precip_average) / 2.0;
            plantLigninFraction[iFacet, surfaceIndex] = Math.Max(0.02, plantLigninFraction[iFacet, surfaceIndex]);
            plantLigninFraction[iFacet, surfaceIndex] = Math.Min(0.50, plantLigninFraction[iFacet, surfaceIndex]);
            plantLigninFraction[iFacet, soilIndex] = Math.Max(0.02, plantLigninFraction[iFacet, soilIndex]);
            plantLigninFraction[iFacet, soilIndex] = Math.Min(0.50, plantLigninFraction[iFacet, soilIndex]);

            // Fire modeling
            burnedCarbon = 0.0;
            burnedNitrogen = 0.0;
            fireSeverity = 0.0;
        } // Facet loop
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

        }

    }
}