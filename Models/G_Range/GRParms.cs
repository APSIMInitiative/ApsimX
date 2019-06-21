using System;
using System.Linq;

namespace Models
{
    using Models.Core;
    using Models.Interfaces;
    using APSIM.Shared.Utilities;

    public partial class G_Range : Model, IPlant, ICanopy, IUptake
    {
        /// <summary>
        /// Obtain the parameters associated with a specific vegetation type 
        /// There are 15 types.
        /// These data are extracted from Land_Units.grg of the original Fortran version, and were read in by the unit Initialize_Model.f95
        /// Some values don't vary across vegetation type. These come from Initialize_Model.f95
        /// 
        /// This is being approached a bit differently than is was in the Fortan version. There most of these parameters 
        /// were stored in an external file - Land_Units.grg. Here I'm embedding them into the source. 
        /// 
        /// Also, the original maintained an array of Parms, one for each vegetation type. That makes sense in the context
        /// of a global model, where you will need the data for each type. Here, we're looking at a point model, so once we know
        /// our location, we just need to work with a single set of parameters.
        /// </summary>
        [Serializable]
        private class Parms
        {
            /// <summary>
            /// Melting temperature for snow, in C degrees
            /// </summary>
            public double meltingTemp = 0.0;

            /// <summary>
            /// Slope of melting equation(cm snow / degree C)
            /// </summary>
            public double meltingSlope = 0.002;

            /// <summary>
            /// Precipitation required, in cm, before runoff occurs (PRECRO)
            /// </summary>
            public double prcpThreshold;

            /// <summary>
            /// The fraction of precipitation that is runoff (FRACRO)
            /// </summary>
            public double prcpThresholdFraction;

            /// <summary>
            /// The fraction of water classed as base flow (BASEF)
            /// </summary>
            public double baseFlowFraction;

            /// <summary>
            /// The water transpired for each depth (AWTL)
            /// </summary>
            public double[] soilTranspirationFraction;

            /// <summary>
            /// Initial soil carbon to nitrogen ratio, from Potter and Klooster (1997) or similar source
            /// </summary>
            public double initSoilCNRatio;

            /// <summary>
            /// Initial lignin to nitrogen ratio in litter
            /// </summary>
            public double initLigninNRatio;

            /// <summary>
            /// Initial tree carbon g/m2(RLEAVC, RLVCIS, FBRCIS ... others?  in Century)  ALIVE and DEAD, with LEAVES and FINE ROOTS DEAD NOT USED (RLWCIS+)
            /// </summary>
            public double[,] treeCarbon;

            /// <summary>
            /// Initial tree carbon g/m2(RLEAVC, RLVCIS, FBRCIS ... others?  in Century)  ALIVE and DEAD, with LEAVES and FINE ROOTS DEAD NOT USED (RLWCIS+)
            /// </summary>
            public double[,] shrubCarbon;

            /// <summary>
            /// Dimension, in meters, of the length or width(i.e., square) of the root volume of plant
            /// </summary>
            public double[] plantDimension = new double[nFacets] { 0.5, 2.0, 8.0 };

            /// <summary>
            /// Effect of litter on soil temperature relative to live and standing dead biomass(ELITST in Century)
            /// </summary>
            public double litterEffectOnSoilTemp = 0.4;

            /// <summary>
            /// Effect of biomass on minimum soil surface temperature(PMNTMP in Century)
            /// </summary>
            public double biomassEffectOnMinSoilTemp = 0.004;

            /// <summary>
            /// Maximum biomass for soil temperature calculations(PMXBIO in Century)
            /// </summary>
            public double maximumBiomassSoilTemp = 600.0;

            /// <summary>
            /// Effect of biomass on maximum soil surface temperature(PMXTMP in Century)
            /// </summary>
            public double biomassEffectOnMaxSoilTemp = -0.0035;

            /// <summary>
            /// Controls on the shape of the regression line connecting available water to PET ratio and plant production.
            /// </summary>
            public double[] pptRegressionPoints = new double[3] { 0.0, 1.0, 0.8 };

            /// <summary>
            /// Parameters describing the effect of temperature on potential production.See the Century Parameterization Workbook for examples. (PPDF)
            /// </summary>
            public double[] temperatureProduction;

            /// <summary>
            /// Level of aboveground standing dead + 10% surface structural C that reduces production by half due to phyiscal obstruction(BIOK5 in CENTURY)
            /// </summary>
            public double standingDeadProductionHalved;

            /// <summary>
            /// Coefficient for calculating potential aboveground monthly production as a function of solar radiation.PRDX in CENTURY, with its meaning defined in the CENTURY code, and the online material outdated.
            /// </summary>
            public double radiationProductionCoefficient;

            /// <summary>
            /// Fraction of carbon production allocated to roots(likely from CENTURY 4.  CENTURY 4.5 uses a complex dynamic carbon allocation between roots and shoots) (FRTC+)
            /// </summary>
            public double[] fractionCarbonToRoots;

            /// <summary>
            /// A flag from 1 to 6, describing grazing effect responses (GRZEFF)_
            /// </summary>
            public int grazingEffect;

            /// <summary>
            /// A multiplier applied to grazing effects 4, 5, and 6
            /// </summary>
            public double grazingEffectMultiplier = 0.0;

            /// <summary>
            /// Four values, 1) [0 in C#] x location of inflection point, 2) [1 in C#] y location of inflection point, 3) [2 in C#] setp size(distance from the maximum point to the minimum point), 4) [3 in C#] slope of line at inflection point.Default values are:  15.40, 11.75, 29.70, 0.031 
            /// </summary>
            public double[] temperatureEffectDecomposition = new double[4] { 15.4, 11.75, 29.7, 0.031 };

            /// <summary>
            /// Three values, 1) [0 in C#] ratio below which there is no effect, 2) [1 in C#] ratio below which there is a maximum effect, 3) [2 in C#] minimum value of impact of precipitation on anerobic decomposition.
            /// </summary>
            public double[] anerobicEffectDecomposition = new double[3] { 1.5, 3.0, 0.5 };

            /// <summary>
            /// Effect of CO2 concentration on production, with 1 meaning no effect, per facet [THIS WILL REQUIRE SPECIAL CARE!]
            /// </summary>
            public double[] effectOfCo2OnProduction = new double[nFacets] { 0.8, 0.8, 0.8 };

            /// <summary>
            /// Decomposition rate of structural litter(per year)
            /// </summary>
            public double[] decompRateStructuralLitter = new double[2] { 3.9, 4.9 };

            /// <summary>
            /// Decomposition rate of metabolic litter(per year)
            /// </summary>
            public double[] decompRateMetabolicLitter = new double[2] { 14.8, 18.5 };

            /// <summary>
            /// Decomposition rate of the fast SOM(soil organic matter) pool(per year)
            /// </summary>
            public double[] decompRateFastSom = new double[2] { 6.0, 7.3 };

            /// <summary>
            /// Decomposition rate of the slow SOM(soil organic matter) pool(per year)
            /// </summary>
            public double decompRateSlowSom = 0.0045;

            /// <summary>
            /// Decomposition rate of the intermediate SOM(soil organic matter) pool(per year)
            /// </summary>
            public double decompRateInterSom = 0.2;

            /// <summary>
            /// Decomposition rate of fine branches(per year)
            /// </summary>
            public double decompRateFineBranch = 1.5;

            /// <summary>
            /// Decomposition rate of coarse branches(per year)
            /// </summary>
            public double decompRateCoarseBranch = 0.5;

            /// <summary>
            /// Decomposition rate of coarse roots(per year)
            /// </summary>
            public double decompRateCoarseRoot = 0.6;

            /// <summary>
            /// Decomposition rate of structural litter in layers 1 and 2 due to invertebrates (DECINV)
            /// </summary>
            public double[] decompRateStructuralLitterInverts;

            /// <summary>
            /// Drainage affecting the rate of anaerobic decomposition, spanning from 0 to 1. (DRAIN)
            /// </summary>
            public double drainageAffectingAnaerobicDecomp;                                      // 

            /// <summary>
            /// Feces lignin content (FECLIG)
            /// </summary>
            public double fecesLignin;

            /// <summary>
            /// 1,1 [0,0 in C#]= Intercept, aboveground, 1,2 [0,1 in C#] = Slope, aboveground, 2,1 [1,0 in C#] = Intercept, belowground, 2,2 [1,1 in C#] = Slope, belowground (FLIGNI)
            /// </summary>
            public double[,] ligninContentFractionAndPrecip;

            /// <summary>
            /// Fraction of urine volatilized (URINEVOL)
            /// </summary>
            public double fractionUrineVolatized;

            /// <summary>
            /// Fraction of gross N mineral volitalized
            /// </summary>
            public double fractionGrossNMineralVolatized = 0.05;

            /// <summary>
            /// Rate of volitalization of mineral N
            /// </summary>
            public double rateVolatizationMineralN = 0.02;

            /// <summary>
            /// Parameters relating precipitation to deposition N rate	(EPNFA)
            /// </summary>
            public double[] precipNDeposition;

            /// <summary>
            /// Degree of mixing of litter fall among facets(0 = none, 1 = complete) (FLITRMIX)
            /// </summary>
            public double decompLitterMixFacets;

            /// <summary>
            /// Degree days and the relationship to plant phenology, by FACET, by 10 values shaping the curve. 
            /// </summary>
            public double[,] degreeDaysPhen;

            /// <summary>
            /// Total degree days to reset phenology
            /// </summary>
            public double[] degreeDaysReset;

            /// <summary>
            /// Effect of root biomass on available nutrients used to determine growth(RICTRL)
            /// </summary>
            public double rootEffectOnNutrients = 0.015;

            /// <summary>
            /// Intercept of relationship of effect of root biomass on available nutrients(RIINT)
            /// </summary>
            public double rootInterceptOnNutrients = 0.8;

            /// <summary>
            /// Site potential for trees, which adjusts nitrogen availability in savannas (SITPOT)
            /// </summary>
            public double treeSitePotential;

            /// <summary>
            /// Correction relating tree basal area to grass nitrogen fraction
            /// </summary>
            public double treeBasalAreaToGrassNitrogen = 1.0;

            /// <summary>
            /// Correction relating tree basal area to grass nitrogen fraction
            /// </summary>
            public double treeBasalAreaToWoodBiomass = 400.0;

            /// <summary>
            /// Symbiotic nitrogen fixation maximum for grassland (g N fixed / g C new growth) (SNFXMX)
            /// </summary>
            public double maxSymbioticNFixationRatio;

            /// <summary>
            /// Fraction of nitrogen available to plants
            /// </summary>
            public double fractionNitrogenAvailable = 0.9;

            /// <summary>
            /// Minimum carbon / nitrogen ratio(Parts > 2 for grasses will be empty)(CERCRP set in FLTCE, BUT SET HERE, not doing dynamic carbon as in Century)
            /// </summary>
            public double[,] minimumCNRatio;

            /// <summary>
            /// Maximum carbon / nitrogen ratio(Parts > 2 for grasses will be empty)(CERCRP set in FLTCE, BUT SET HERE, not doing dynamic carbon as in Century)
            /// </summary>
            public double[,] maximumCNRatio;

            /// <summary>
            /// Fraction of net primary production that goes to maintenance respiration
            /// </summary>
            public double[] fractionNppToRespiration = new double[nFacets] { 1.0, 1.0, 1.0 };

            /// <summary>
            /// Grass maximum fraction of net primary production that goes to maintenance respiration
            /// </summary>
            public double[] herbMaxFractionNppToRespiration = new double[2] { 0.26, 0.26 };

            /// <summary>
            /// Woody maximum fraction of net primary production that goes to maintenance respiration
            /// </summary>
            public double[] woodyMaxFractionNppToRespiration = new double[5] { 0.4, 0.4, 0.4, 0.4, 0.4 };

            /// <summary>
            /// Maximum leaf area index for trees (MAXLAI)
            /// </summary>
            public double maximumLeafAreaIndex;

            /// <summary>
            /// I don't know what this is ... ASK.  Not documented on the web or in code. (KLAI)
            /// </summary>
            public double kLeafAreaIndex;

            /// <summary>
            /// Biomass to leaf area index factor (BTOLAI)
            /// </summary>
            public double biomassToLeafAreaIndexFactor;

            /// <summary>
            /// Annual fraction of nitrogen volatilized (VLOSSE)
            /// </summary>
            public double annualFractionVolatilizedN;

            /// <summary>
            /// Maximum herbaceous root death rate per month
            /// </summary>
            public double maxHerbRootDeathRate;

            /// <summary>
            /// Fraction of nitrogen absorbed by residue, for surface(1) and soil(2)
            /// </summary>
            public double[] fractionNAbsorbedByResidue = new double[2] { 0.0, 0.02 };

            /// <summary>
            /// Shoot death rate due to 1 [0]) water stress, 2 [1]) phenology, 3 [2]) shading, according to carbon centration in 4 [3].  (FSDETH)
            /// </summary>
            public double[] shootDeathRate;

            /// <summary>
            /// The proportion of annual plants in the herbaceous facet.
            /// </summary>
            public double propAnnuals;

            /// <summary>
            /// Month to remove annual plants, following their standing dead for some time, contributing to litter, etc.
            /// </summary>
            public double monthToRemoveAnnuals;

            /// <summary>
            /// Annual seed production, in relative number per year.Increase one group to favor it.Increase all groups to increase general establishment.
            /// </summary>
            public double[] relativeSeedProduction;     // FACTOR OF 10000!                                         

            /// <summary>
            /// Fraction of aboveground net primary productivity that goes to seeds, by facets.For woody plants, it is the proportion of carbon for leaf growth diverted to seeds.
            /// </summary>
            public double[] fractionAgroundNppToSeeds = new double[nFacets] { 0.05, 0.03, 0.01 };

            /// <summary>
            /// Fraction of seeds that do not germinate.This is not used in population dynamics, but rather in decomposition
            /// </summary>
            public double[] fractionSeedsNotGerminated = new double[nFacets] { 0.5, 0.8, 0.9 };

            /// <summary>
            /// Available water:PET ratio effect on establishment, per facet, and with 2 pairs of values used in a regression.
            /// </summary>
            public double[,] waterEffectOnEstablish;

            /// <summary>
            /// Herbaceous root biomass effect on establishment, per facet, and with 2 pairs of values used in a regression.
            /// </summary>
            public double[,] herbRootEffectOnEstablish;

            /// <summary>
            /// Litter cover effect on establishment, per facet, and with 2 pairs of values used in a regression.
            /// </summary>
            public double[,] litterEffectOnEstablish;

            /// <summary>
            /// Woody cover effect on establishment, per facet, and with 2 pairs of values used in a regression.
            /// </summary>
            public double[,] woodyCoverEffectOnEstablish;

            /// <summary>
            /// Nominal plant death rate.This may be increased by various factors.
            /// </summary>
            public double[] nominalPlantDeathRate;

            /// <summary>
            /// Available water:PET ratio effect on plant death rate, per facet, and with 2 pairs per value used in regression.
            /// </summary>
            public double[,] waterEffectOnDeathRate;

            /// <summary>
            /// Grazing rate effect on plant death rate, per facet, and with 2 pairs per value used in regression.
            /// </summary>
            public double[,] grazingEffectOnDeathRate;

            /// <summary>
            /// Effect of shading, associated with LAI, on death rate.Suitable for younger age classes of trees as well (although not explicitly modeled here).
            /// </summary>
            public double[,] shadingEffectOnDeathRate;

            /// <summary>
            /// Rate per month of standing dead to fall to litter (FALR)
            /// </summary>
            public double[] fallRateOfStandingDead;

            /// <summary>
            /// The rate of death of leaves after fall has arrived.
            /// </summary>
            public double deathRateOfDeciduousLeaves;

            /// <summary>
            /// Temperature in C at leaf on(1 [0]) in spring and leaf fall(2 [1]) in fall
            /// </summary>
            public double[] temperatureLeafOutAndFall = new double[2] { 10.0, 7.0 };

            /// <summary>
            /// Whether the deciduous fraction of the plants are typical deciduous(FALSE) or drought deciduous(TRUE)
            /// </summary>
            public double[] droughtDeciduous;

            /// <summary>
            /// The fraction of nitrogen in dead leaves that is translocated into storage. (FORRTF)
            /// </summary>
            public double fractionWoodyLeafNTranslocated;

            /// <summary>
            /// Leaf death rate per month, per facet.
            /// </summary>
            public double[] leafDeathRate;

            /// <summary>
            /// Death rate of fine root in woody plants, per facet(herbs are a placeholder)
            /// </summary>
            public double[] fineRootDeathRate;

            /// <summary>
            /// Death rate of fine branches in woody plants, per facet(herbs are a placeholder)
            /// </summary>
            public double[] fineBranchDeathRate;

            /// <summary>
            /// Death rate of coarse wood in woody plants, per facet(herbs are a placeholder)
            /// </summary>
            public double[] coarseBranchDeathRate;

            /// <summary>
            /// Death rate of coarse root in woody plants, per facet(herbs are a placeholder)
            /// </summary>
            public double[] coarseRootDeathRate;

            /// <summary>
            /// Fraction of carbon in grazed material that is returned to the system(the rest is in carcasses or milk or the like) (GFCRET)
            /// </summary>
            public double fractionCarbonGrazedReturned;

            /// <summary>
            /// Fraction of nitrogen excreted that is in feces.The remainder is in urine. (FACESFR)
            /// </summary>
            public double fractionExcretedNitrogenInFeces;

            /// <summary>
            /// The fraction of grazing that comes from each facet.Sum to 100 %.
            /// </summary>
            public double[] fractionGrazedByFacet;

            /// <summary>
            /// The annual fraction of plant material that is removed.This includes both live and dead material. (FLGREM)
            /// </summary>
            public double fractionGrazed;

            /// <summary>
            /// The probability of fire per year for any given cell within the landscape unit(NOTE SCALE DEPENDENCE, USE DEPENDS ON fire_maps_used), set to 0 for no fire(unitless) (GFD)
            /// </summary>
            public double frequencyOfFire;

            /// <summary>
            /// The proportion of a landscape cell that burns, in the case of a fire event (NOTE SCALE DEPENDENCE.USE DEPENDS ON fire_maps_used.ALSO ONE FIRE PER YEAR MAX)  (unitless) (GFD)
            /// </summary>
            public double fractionBurned;

            /// <summary>
            /// The month in which patches will be burned, in the case of a fire event (ONE FIRE PER YEAR MAX, USE DEPENDS ON fire_maps_used)  (month) (GFD)
            /// </summary>
            public int burnMonth;

            /// <summary>
            /// The fuel load as related to low and high intensity fires(g biomass / m2) (fuello)
            /// </summary>
            public double[] fuelVsIntensity;

            /// <summary>
            /// The proportion of aboveground vegetation that is green versus fire intensity(unitless) (pgrnsev)
            /// </summary>
            public double[,] greenVsIntensity;

            /// <summary>
            /// The proportion of live leaves and shoots removed by a fire event, by facet, for low and high intensity fire  (unitless) (FLFREM)
            /// </summary>
            public double[,] fractionShootsBurned;

            /// <summary>
            /// The proportion of standing dead removed by a fire event, by facet, for low and high intensity fire  (unitless) (FDMREM)
            /// </summary>
            public double[,] fractionStandingDeadBurned;

            /// <summary>
            /// The proportion of plants that are burned that die, by facet, for low and high intensity fire(unitless) (FRDTH)
            /// </summary>
            public double[,] fractionPlantsBurnedDead;

            /// <summary>
            /// The proportion of litter removed by a fire event, by facet, for low and high intensity fire  (unitless) (FLTRCMB)
            /// </summary>
            public double[,] fractionLitterBurned;

            /// <summary>
            /// The proportion of carbon in burned aboveground material that is ash, going to structural litter  (unitless) (FCMBCASH)
            /// </summary>
            public double fractionBurnedCarbonAsAsh;

            /// <summary>
            /// The proportion of nitrogen in burned aboveground material that is ash, going to soil mineral nitrogen  (unitless) (FCMBNASH)
            /// </summary>
            public double fractionBurnedNitrogenAsAsh;

            /// <summary>
            /// The probability of fertlization per year in the landscape unit (USE DEPENDS ON fertilize_maps_used) (unitless)
            /// </summary>
            public double frequencyOfFertilization;

            /// <summary>
            /// The proportion of a landscape cell that is fertilized, in the case of a fertilization event (NOTE SCALE DEPENDENCE.USE DEPENDS ON fertilize_maps_used)  (unitless)
            /// </summary>
            public double fractionFertilized;

            /// <summary>
            /// The month in which fertilization occurs (one event per year per landscape unit)  (month)
            /// </summary>
            public int fertilizeMonth;

            /// <summary>
            /// Amount of inorganic nitrogen added during a fertilization event (g / m2) (FERAMT)
            /// </summary>
            public double fertilizeNitrogenAdded;

            /// <summary>
            /// Amount of carbon added as part of organic matter fertilizer (g / m2) (ASTGC)
            /// </summary>
            public double fertilizeCarbonAdded;

            // NOT ACTUALLY USED - double fertilizeCarbonNitrogenRatio;                                          // Ratio of carbon to nitrogen in organic matter fertilizer added(unitless)
            // The following variables are at the landscape unit level, but are calculated, rather than read in.

            /// <summary>
            /// The number of plants that can be supported on 1 x 1 km of land
            /// </summary>
            public int[] potPopulation = new int[nFacets];

            /// <summary>
            /// For brevity, going to store the area of plants
            /// </summary>
            public double[] indivPlantArea = new double[nFacets];
        }

#pragma warning disable 0649

        [Serializable]
        private struct Globals
        {
            public int zone;                    // A unique ID, from 1 to N, for each cell.ZONE is a name that comes from ARC GRID.
            public int elev;                    // Elevation, in meters ... commented out, not used.

            public bool land;                   // Defining land versus ocean

            public int coverClass;              // Land cover / land use class
            public int landscapeType;           // Landscape type identifying the units for which parameters are given
            public bool rangeland;              // A flag showing whether land is rangeland or not
            public double latitude;             // The latitude of the center of the cell

            public double precip;               // The total precipitation in the month, in mm
            public double maxTemp;              // The average maximum temperature in the month, in C
            public double minTemp;              // The average minimum temperature in the month, in C
            public double precipAverage;        // The average annual precipitation, in mm / yr
            public double temperatureAverage;   // The average annual temperature, in C
            public double topSand;              // The percent sand in the top-soil
            public double topSilt;              // The percent silt in the top-soil
            public double topClay;              // The percent clay in the top-soil
            public double topGravel;            // The percent rock in the top-soil
            public double topBulkDensity;       // The bulk density of the top-soil
            public double topOrganicCarbon;     // The percent organic matter carbon content in the top-soil
            public double subSand;              // The percent sand in the sub-soil
            public double subSilt;              // The percent silt in the sub-soil
            public double subClay;              // The percent clay in the sub-soil
            public double subGravel;            // The percent rock in the sub-soil
            public double subBulkDensity;       // The bulk density of the sub-soil
            public double subOrganicCarbon;     // The percent organic matter carbon content in the sub-soil
            public double decidTreeCover;       // Deciduous tree cover, in percent, perhaps from DeFries, for example, which is from data from 1993-94, but only trees.
            public double egreenTreeCover;      // Evergreen tree cover, in percent, perhaps from DeFries, for example, which is from data from 1993-94, but only trees.
            public double shrubCover;           // DERIVED from other layers using MODIS 44B products and other layers.VERY poorly known.
            public double herbCover;            // Herbaceous cover, from MODIS 44B product
            public double propBurned;           // The proportion of the cell burned, as shown in maps(NOTE SCALE DEPENDENCE)
            public double propFertilized;       // The proportion of the cell fertilized, as shown in maps(NOTE SCALE DEPENDENCE)
            public double temporary;            // A temporary map location, used to read-in values
            public int fireMapsUsed;            // [From SimParm] Whether fire maps will be used (1) or fire will be based on frequencies in the land unit
            public int fertilizeMapsUsed;       // [From SimParm] Whether fertilize maps will be used (1) or fertilizing will be based on frequencies in the
        };

        private Globals globe = new Globals();
        private Parms[] parmArray = null;

        /// <summary>
        /// Transcoded from Initialize_Rangelands subroutine in Initialize_Model.f95
        /// 
        /// !***** Take additional steps to initialize rangelands.  For example, wilting point and field capacity must
        /// !***** be determined for the rangeland sites.
        /// !***** (Facets were confirmed being reasonably populated through echoed statements)
        /// 
        /// Much of this shouldn't really be necessary once we start getting soil information from Apsim, but I'm porting it over now
        /// as it let's us get started, and will make it easier to cross-check ApsimX and G_Range outputs.
        /// </summary>
        private void InitParms()
        {
            //// Work out where we are, what the vegetation type is, and load suitable params
            parms = parmArray[globe.landscapeType - 1];

            sand[0] = globe.topSand;  // The top and bottom layers get their values directly from the two HWSD layers
            sand[3] = globe.subSand;  // The top and bottom layers get their values directly from the two HWSD layers
            sand[1] = (globe.topSand * 0.6667) + (globe.subSand * 0.3333);  // The other layers get weighted values.
            sand[2] = (globe.topSand * 0.3333) + (globe.subSand * 0.6667);  // The other layers get weighted values.
            silt[0] = globe.topSilt;   // The top and bottom layers get their values directly from the two HWSD layers
            silt[3] = globe.subSilt;   // The top and bottom layers get their values directly from the two HWSD layers
            silt[1] = (globe.topSilt * 0.6667) + (globe.subSilt * 0.3333);  // The other layers get weighted values.
            silt[2] = (globe.topSilt * 0.3333) + (globe.subSilt * 0.6667);  // The other layers get weighted values.
            clay[0] = globe.topClay;   // The top and bottom layers get their values directly from the two HWSD layers
            clay[3] = globe.subClay;   // The top and bottom layers get their values directly from the two HWSD layers
            clay[1] = (globe.topClay * 0.6667) + (globe.subClay * 0.3333);  // The other layers get weighted values.
            clay[2] = (globe.topClay * 0.3333) + (globe.subClay * 0.6667);  // The other layers get weighted values.
            // The following are only used a few lines down, so not storing in Rng, for space - saving purposes.
            double[] gravel = new double[4];
            gravel[0] = globe.topGravel;   // The top and bottom layers get their values directly from the two HWSD layers
            gravel[3] = globe.subGravel;   // The top and bottom layers get their values directly from the two HWSD layers
            gravel[1] = (globe.topGravel * 0.6667) + (globe.subGravel * 0.3333);   // The other layers get weighted values.
            gravel[2] = (globe.topGravel * 0.3333) + (globe.subGravel * 0.6667);   // The other layers get weighted values.
            double[] bulkDensity = new double[4];
            bulkDensity[0] = globe.topBulkDensity;   // The top and bottom layers get their values directly from the two HWSD layers
            bulkDensity[3] = globe.subBulkDensity;   // The top and bottom layers get their values directly from the two HWSD layers
            bulkDensity[1] = (globe.topBulkDensity * 0.6667) + (globe.subBulkDensity * 0.3333);  // The other layers get weighted values.
            bulkDensity[2] = (globe.topBulkDensity * 0.3333) + (globe.subBulkDensity * 0.6667);  // The other layers get weighted values.
            double[] organicCarbon = new double[4];
            organicCarbon[0] = globe.topOrganicCarbon;  // The top and bottom layers get their values directly from the two HWSD layers
            organicCarbon[3] = globe.subOrganicCarbon;  // The top and bottom layers get their values directly from the two HWSD layers
#if G_RANGE_BUG
            // EJZ - THIS BIT IS DELIBERATELY BROKEN, TO CORRESPOND TO AN ERROR IN GRANGE ITSELF
            organicCarbon[1] = (globe.topSand * 0.6667) + (globe.subOrganicCarbon * 0.3333);   // The other layers get weighted values.
            organicCarbon[2] = (globe.topSand * 0.3333) + (globe.subOrganicCarbon * 0.6667);   // The other layers get weighted values.
            // EJZ - END OF BROKEN CODE
#else
            organicCarbon[1] = (globe.topOrganicCarbon * 0.6667) + (globe.subOrganicCarbon * 0.3333);   // The other layers get weighted values.
            organicCarbon[2] = (globe.topOrganicCarbon * 0.3333) + (globe.subOrganicCarbon * 0.6667);   // The other layers get weighted values.
#endif

            // Century uses these soil parameters from 0 - 1, so...
            for (int iLayer = 0; iLayer < nSoilLayers; iLayer++)
            {
                sand[iLayer] /= 100.0;
                silt[iLayer] /= 100.0;
                clay[iLayer] /= 100.0;
                gravel[iLayer] /= 100.0;
            }

            // Calculate the field capacity and wilting point for the rangeland cells.
            // This process comes from Century 4.5, where they cite Gupta and Larson(1979).          
            // NB: The kg / dm3 for bulk density in the soils database is equal to g / cm3 in Gupta and Larson.            
            // Field capacity is done at a Matric potential of - 0.33, as in Century, and includes only option SWFLAG = 1, where both wilting point and field capacity are calculated.            
            // Wilting point is done at Matric potential of - 15.0.            
            // (Century includes extra components, but the coefficients on those for SWFLAG = 1 are 0, so following Gupta and Larson(1979) is correct.)

            rangeType = globe.landscapeType;
            if (rangeType < 1)
            {
                summary.WriteError(this, "A range cell has a landscape type 0.  Make sure GIS layers agree for X and Y: " + X.ToString() + ", " + Y.ToString());
                rangeType = 1;
                parms = parmArray[0];
            }
            // Calculating initial plant populations.  These are based on a 1 km ^ 2 area, and the coverage maps.
            // Three facets, plus bare ground.
            // The potential populations of the plants are higher than the aerial coverage of the facets, at least
            // for herbs and shrubs.Tree cover and population are the same.This is due to herbs being in the understory
            // of shrubs and trees, and shrubs being in the understory of trees.

            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                // The following is the total population possible for the facet, if entirely dominated by that facet.
                parms.indivPlantArea[iFacet] = parms.plantDimension[iFacet] * parms.plantDimension[iFacet];  // m x m = m ^ 2
                parms.potPopulation[iFacet] = (int)(refArea / parms.indivPlantArea[iFacet]);  // (m x m) / m ^ 2 = #
            }

            for (int iLayer = 0; iLayer < nSoilLayers; iLayer++)
            {
                if (sand[iLayer] + silt[iLayer] + clay[iLayer] + gravel[iLayer] > 0.01)
                {
                    fieldCapacity[iLayer] = (sand[iLayer] * 0.3075) + (silt[iLayer] * 0.5886) +
                                                      (clay[iLayer] * 0.8039) + (organicCarbon[iLayer] * 0.002208) +
                                                      (bulkDensity[iLayer] * (-0.14340));
                    wiltingPoint[iLayer] = (sand[iLayer] * (-0.0059)) + (silt[iLayer] * 0.1142) +
                                           (clay[iLayer] * 0.5766) + (organicCarbon[iLayer] * 0.002228) +
                                           (bulkDensity[iLayer] * 0.02671);
                }
                else
                {
                    fieldCapacity[iLayer] = 0.03;
                    wiltingPoint[iLayer] = 0.01;
                    summary.WriteWarning(this, "Warning, check GIS: soil information is not defined for layer: " + iLayer.ToString());
                    // The following is commented out, to avoid distracting warnings with minor effects on outcomes.But the error to ECHO.GOF is retained.
                    // write(*, *) 'Warning, check GIS: soil information is not defined for cell: ',icell,' and layer: ',ilayer
                }

                // Correcting field capacity and wilting point based on gravel volume.
                fieldCapacity[iLayer] = fieldCapacity[iLayer] * (1.0 - gravel[iLayer]);
                wiltingPoint[iLayer] = wiltingPoint[iLayer] * (1.0 - gravel[iLayer]);

                relativeWaterContent[iLayer] = 0.50;
                // Initialize asmos to the range between capacity and wilting, plus the bottom value, wilting.
                // Then multiply that by the relative water content, and finally soil depth.  The other measures are for 1 cm deep soil, essentially.
                asmos[iLayer] = ((fieldCapacity[iLayer] - wiltingPoint[iLayer]) *
                                relativeWaterContent[iLayer] + wiltingPoint[iLayer]) * soilDepth[iLayer];
            }

            // Calculate total carbon in the soil.  This uses average temperature and precipitation, which Century
            // truncates to fairly low values, and so here we do the same. NOTE:  How incorrect it is to initialize
            // forested soils using the grassland initialization is a question.  But we won't be simulating forests per sey.
            double temper = Math.Min(23.0, globe.temperatureAverage);
            double precip = Math.Min(120.0, globe.precipAverage);
            double avgSilt = (silt[0] + silt[1] + silt[2] + silt[3]) / 4.0;
            double avgClay = (clay[0] + clay[1] + clay[2] + clay[3]) / 4.0;

            // Initialize total soil carbon in grams using the formula in Century, which combines som1c, som2c, and som3c
            soilTotalCarbon = (-8.27E-01 * temper + 2.24E-02 * temper * temper + precip * 1.27E-01 - 9.38E-04
                            * precip * precip + precip * avgSilt * 8.99E-02 + precip * avgClay * 6.00E-02 + 4.09) * 1000.0;
            // Truncated as in Century.Not allowed to go below 500 g / m ^ 2
            if (soilTotalCarbon < 500.0)
                soilTotalCarbon = 500.0;
            carbonNitrogenRatio[surfaceIndex] = parms.initSoilCNRatio;
            carbonNitrogenRatio[soilIndex] = parms.initSoilCNRatio;

            // Century cites equations by Burke to initialize carbon pool compartments
            // Rng(icell) % fast_soil_carbon(SURFACE_INDEX) = &
            // Rng(icell) % soil_total_carbon * 0.02 + ((Rng(icell) % soil_total_carbon * 0.02) * 0.011)
            fastSoilCarbon[surfaceIndex] = 10.0 + (10.0 * 0.011);
            fastSoilCarbon[soilIndex] = (soilTotalCarbon * 0.02) +
                                               ((soilTotalCarbon * 0.02) * 0.011);
            fastSoilNitrogen[surfaceIndex] = fastSoilCarbon[surfaceIndex] *
                                                    (1.0 / carbonNitrogenRatio[surfaceIndex]);
            fastSoilNitrogen[soilIndex] = fastSoilCarbon[soilIndex] *
                                                    (1.0 / carbonNitrogenRatio[soilIndex]);
            intermediateSoilCarbon = soilTotalCarbon * 0.64 +
                                           ((soilTotalCarbon * 0.64) * 0.011);
            passiveSoilCarbon = soilTotalCarbon * 0.34 + ((soilTotalCarbon * 0.34) * 0.011);
            intermediateSoilNitrogen = intermediateSoilCarbon * (1.0 / parms.initSoilCNRatio);
            passiveSoilNitrogen = passiveSoilCarbon * (1.0 / parms.initSoilCNRatio);

            // Surface fast soil carbon was removed from the following, since it is assigned 10.11 by default.
            double remainingC = soilTotalCarbon - fastSoilCarbon[soilIndex] -
                    intermediateSoilCarbon - passiveSoilCarbon;
            // See multiple cites for the following, including Parten et al. (1993)
            double fractionMetab = 0.85 - (0.018 * parms.initLigninNRatio);
            fractionMetab = Math.Max(0.2, fractionMetab);
            // Assigning initial carbon and nitrogen concentrations
            for (int iLayer = surfaceIndex; iLayer <= soilIndex; iLayer++)
            {
                // Values differ in Century parameter files.   100 appears typical, and spin - up should customize responses
                litterStructuralCarbon[iLayer] = 100.0;
                litterMetabolicCarbon[iLayer] = 100.0 * fractionMetab;
                litterStructuralNitrogen[iLayer] = litterStructuralCarbon[iLayer] *
                                                       parms.initLigninNRatio;
                litterMetabolicNitrogen[iLayer] = litterStructuralNitrogen[iLayer] * fractionMetab;
            }

            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                leafCarbon[iFacet] = 200.0 + (200.0 * 0.011);
                leafNitrogen[iFacet] = 3.0;
                deadStandingCarbon[iFacet] = 80.0 + (80.0 * 0.011);
                deadStandingNitrogen[iFacet] = 1.6;
                fineRootCarbon[iFacet] = 200.0 + (200.0 * 0.011);
                fineRootNitrogen[iFacet] = 3.0;
                deadFineBranchCarbon[iFacet] = 0.0;   // Setting to 0, but really just for herbs.
                deadCoarseBranchCarbon[iFacet] = 0.0;
                deadCoarseRootCarbon[iFacet] = 0.0;
                rootShootRatio[iFacet] = leafCarbon[iFacet] / fineRootCarbon[iFacet];
            }

            // Standardize the three surfaces in case they sum to greater than 1.0(they are allowed to be less than 1.0, with the remainder being bare ground)
            double tempSum = globe.herbCover + globe.shrubCover + globe.decidTreeCover + globe.egreenTreeCover;
            if (tempSum > 100.0) {
                globe.herbCover = globe.herbCover * (100.0 / tempSum);
                globe.shrubCover = globe.shrubCover * (100.0 / tempSum);
                globe.decidTreeCover = globe.decidTreeCover * (100.0 / tempSum);
                globe.egreenTreeCover = globe.egreenTreeCover * (100.0 / tempSum);
            }
            // Facet_cover is the straight proportion of each facet on the 1 km ^ 2.Facet_population includes understory plants.
            facetCover[(int)Facet.tree] = (globe.decidTreeCover + globe.egreenTreeCover) / 100.0;
            if (facetCover[(int)Facet.tree] > 0.0001)
                propAnnualDecid[(int)Facet.tree] = globe.decidTreeCover / (globe.decidTreeCover + globe.egreenTreeCover);
            else
                propAnnualDecid[(int)Facet.tree] = 0.0;

            // Shrub cover, which has no good surface to define it(confirmed by Dr.Hansen himself)
            facetCover[(int)Facet.shrub] = globe.shrubCover / 100.0;
            if (facetCover[(int)Facet.shrub] > 0.99)
                facetCover[(int)Facet.shrub] = 0.99;   // Trim any cell that is 100 % shrubs to allow some herbs
            // THE FOLLOWING COULD BE A PARAMETER.For now, setting shrub deciduous proporation equal to tree deciduous portion, which should capture large - scale biome variation.
            propAnnualDecid[(int)Facet.shrub] = propAnnualDecid[(int)Facet.tree];
            // NOTE: Putting 1 % cover into each cell, as an initial value only.
            facetCover[(int)Facet.herb] = globe.herbCover / 100.0;
            if (facetCover[(int)Facet.herb] < 0.01)
                facetCover[(int)Facet.herb] = 0.01;
            // The following is a parameter...
            propAnnualDecid[(int)Facet.herb] = parms.propAnnuals;
            // Shrubs are the largest unknown, so if there is a problem, subtract from shrubs
            if ((facetCover[(int)Facet.tree] + facetCover[(int)Facet.shrub] + facetCover[(int)Facet.herb]) > 1.0)
                facetCover[(int)Facet.shrub] = 1.0 - (facetCover[(int)Facet.tree] + facetCover[(int)Facet.herb]);
            bareCover = (1.0 - (facetCover[(int)Facet.tree] + facetCover[(int)Facet.shrub] + facetCover[(int)Facet.herb]));
            // !    write(ECHO_FILE, '(A10,I6,5(F7.4,2X))') 'FACETS: ',icell,Rng(icell) % facet_cover(T_FACET),Rng(icell) % facet_cover(S_FACET), &
            // !Rng(icell) % facet_cover(H_FACET), Rng(icell) % bare_cover, (Rng(icell) % facet_cover(T_FACET) + &
            // !Rng(icell) % facet_cover(S_FACET) + Rng(icell) % facet_cover(H_FACET) + Rng(icell) % bare_cover)

            // Calculate facet populations.   * *Initializing herbs in 1 / 3 understory of shrubs.Shrubs in 1 / 3 understory of trees, and
            //                                   herbs in 1 / 6 understory of trees and shrubs** CONSIDER THIS AND ITS REPERCUSSIONS
            // Ok for initialization, but in the simulation, woody cover.
            // TREES
            totalPopulation[(int)Layer.tree] = facetCover[(int)Facet.tree] * parms.potPopulation[(int)Facet.tree]; // Tree population and cover are directly related.
            // SHRUBS
            // Calculate number of shrubs under trees, if 1 / 3 what could be fitted with full packing(trunks etc.are ignored here)
            double plantCount = (totalPopulation[(int)Layer.tree] * parms.indivPlantArea[(int)Facet.tree]) /
                                 parms.indivPlantArea[(int)Facet.shrub];
            totalPopulation[(int)Layer.shrubUnderTree] = plantCount * 0.3334;
            // Calculate total shrubs on shrub facet
            totalPopulation[(int)Layer.shrub] = facetCover[(int)Facet.shrub] * parms.potPopulation[(int)Facet.shrub];
            // HERBS
            // Calculate number of herbs under trees, if 1 / 6 what could be fitted with full packing(trunks etc.are ignored here)
            plantCount = (totalPopulation[(int)Layer.tree] * parms.indivPlantArea[(int)Facet.tree]) /
                     parms.indivPlantArea[(int)Facet.herb];
            totalPopulation[(int)Layer.herbUnderTree] = plantCount * 0.16667;
            // Calculate number of herbs under shrubs, if 1 / 3 what could be fitted with full packing(trunks etc.are ignored here)
            double plant_count2 = (totalPopulation[(int)Layer.shrub] * parms.indivPlantArea[(int)Facet.shrub]) /
                      parms.indivPlantArea[(int)Facet.herb];
            totalPopulation[(int)Layer.herbUnderShrub] = plant_count2 * 0.3334;
            // Calculate total herbs on herb facet, and sum those on tree and shrub facet to yield the total number of herbs
            totalPopulation[(int)Layer.herb] = facetCover[(int)Facet.herb] * parms.potPopulation[(int)Facet.herb];

            // Initialize lignin structural residue
            for (int iFacet = 0; iFacet < nFacets; iFacet++)
            {
                plantLigninFraction[iFacet, surfaceIndex] = 0.25;
                plantLigninFraction[iFacet, soilIndex] = 0.25;    // Alter what these should be initialized to, as needed
            }
        }

        /// <summary>
        /// Corresponds to subroutine Initialize_Globe in Initialize_Models.f95, but is implemented quite differently.
        /// We read from an SQLite database, rather than a series of files, and we focus only on a single location
        /// rather than reading in the entire globe.
        /// </summary>
        private void LoadGlobals()
        {
            double latitude = Latitude;
            double longitude = Longitude;

            // How should we handle knowing our location? We want to know early on - but the Weather component does start reading until weather is needed,
            // and it doesn't always provide longitude in any case.
            // For now, I'm usually going to rely on the user explicitly entering a latitude and longitude.
            if (Double.IsNaN(latitude))
                latitude = Weather.Latitude;
            if (Double.IsNaN(longitude) && Weather is Weather)
                longitude = (Weather as Weather).Longitude;

            // We need to work out the "cell" that corresponds to our latitude and longitude
            // The code below is roughly correct, but it might need a bit of additional thought about boundary cases.
            X = Math.Max(1, Math.Min(720, (int)Math.Round(360 + longitude * 2.0)));
            Y = Math.Max(1, Math.Min(360, (int)Math.Round(180.0 - latitude * 2.0)));
            globe.zone = (Y - 1) * 720 + X;

            SQLite sqlite = new SQLite();
            sqlite.OpenDatabase(FullDatabaseName, true);
            System.Data.DataTable dataTable = sqlite.ExecuteQuery("SELECT * FROM HalfDegree INNER JOIN SystemTypes ON HalfDegree.GoGe = SystemTypes.TypeId WHERE ZONE=" + globe.zone.ToString());
            if (dataTable.Rows.Count != 1)
                throw new ApsimXException(this, "Error reading G_Range database!");
            System.Data.DataRow dataRow = dataTable.Rows[0];

            rangeType = (int)dataRow["Sage"] - 1;
            if (rangeType < 0)
                throw new ApsimXException(this, "The specified location is not considered to be rangeland.");

            globe.land = (int)dataRow["Land"] == 1;
            globe.coverClass = (int)dataRow["Goge"];

            globe.latitude = (double)dataRow["Lats"];
            globe.topSand = (double)dataRow["Top_sand"];
            globe.topSilt = (double)dataRow["Top_silt"];
            globe.topClay = (double)dataRow["Top_clay"];
            globe.topGravel = (double)dataRow["Top_gravel"];
            globe.topBulkDensity = (double)dataRow["Top_bulk"];
            globe.topOrganicCarbon = (double)dataRow["Top_carbon"];
            globe.subSand = (double)dataRow["Sub_sand"];
            globe.subSilt = (double)dataRow["Sub_silt"];
            globe.subClay = (double)dataRow["Sub_clay"];
            globe.subGravel = (double)dataRow["Sub_gravel"];
            globe.subBulkDensity = (double)dataRow["Sub_bulk"];
            globe.subOrganicCarbon = (double)dataRow["Sub_carbon"];
            globe.landscapeType = (int)dataRow["Sage"];
            globe.precipAverage = (double)dataRow["Prcp_avg"];
            globe.temperatureAverage = (double)dataRow["Temp_avg"];
            globe.decidTreeCover = (double)dataRow["Decid"];
            globe.egreenTreeCover = (double)dataRow["Egreen"];
            globe.shrubCover = (double)dataRow["Shrub"];
            globe.herbCover = (double)dataRow["Herb"];
        }

        /// <summary>
        /// Based on subroutine Initialize_Landscape_Parms in Initialize_Model.f95,
        /// but reads from a resource rather than an external file, and does not 
        /// echo the values.
        /// </summary>
        private void LoadParms()
        {
            parmArray = new Parms[15];
            // Read in the whole parameter file as one big string
            string allParmData = Properties.Resources.ResourceManager.GetString("GRangeLandUnits");
            // Split the string into lines
            string[] longParmStrings = allParmData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            // Extract only the first part of each line
            string[] parmsStrings = longParmStrings
                .Select(s => s.Split(new string[] { "//" }, StringSplitOptions.None)[0].Trim())
                .ToArray();
            // Now work through them all and extract the data
            int iLine = 0;
            double[] tempArray;
            for (int iParm = 0; iParm < 15; iParm++)
            {
                parmArray[iParm] = new Parms();
                Parms parm = parmArray[iParm];
                int iType = ReadIntVal(parmsStrings[iLine++]);
                if (iType != iParm + 1)
                    throw new Exception("Error parsing G_Range parameters");
                parm.prcpThreshold = ReadDoubleVal(parmsStrings[iLine++]);
                parm.prcpThresholdFraction = ReadDoubleVal(parmsStrings[iLine++]);
                parm.baseFlowFraction = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.soilTranspirationFraction);
                parm.initSoilCNRatio = ReadDoubleVal(parmsStrings[iLine++]);
                parm.initLigninNRatio = ReadDoubleVal(parmsStrings[iLine++]);

#if G_RANGE_BUG
                // DOING THIS WRONG DELIBERATELY, TO CORRESPOND WITH A BUG IN GRANGE
                // EJZ - STARTING BAD CODE HERE
                // Two things are wrong with this - first, the order in which shrub and tree are read
                // Secondly, the order of array indices
                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.treeCarbon = new double[nWoodyParts, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.treeCarbon[i / 2, i % 2] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.shrubCarbon = new double[nWoodyParts, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.shrubCarbon[i / 2, i % 2] = tempArray[i];
                // END BAD CODE SECTION
#else
                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.shrubCarbon = new double[nWoodyParts, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.shrubCarbon[i % nWoodyParts, i / nWoodyParts] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.treeCarbon = new double[nWoodyParts, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.treeCarbon[i % nWoodyParts, i / nWoodyParts] = tempArray[i];
#endif                
                ReadDoubleArray(parmsStrings[iLine++], out parm.temperatureProduction);
                parm.standingDeadProductionHalved = ReadDoubleVal(parmsStrings[iLine++]);
                parm.radiationProductionCoefficient = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.fractionCarbonToRoots);
                parm.grazingEffect = ReadIntVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.decompRateStructuralLitterInverts);
                parm.drainageAffectingAnaerobicDecomp = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fecesLignin = ReadDoubleVal(parmsStrings[iLine++]);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.ligninContentFractionAndPrecip = new double[2, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.ligninContentFractionAndPrecip[i / 2, i % 2] = tempArray[i];

                parm.fractionUrineVolatized = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.precipNDeposition);
                parm.decompLitterMixFacets = ReadDoubleVal(parmsStrings[iLine++]);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.degreeDaysPhen = new double[nFacets, 10];
                for (int i = 0; i < nFacets * 10; i++)
                {
                    int iFacet = i / 10;
                    int iPoint = i % 10;
                    switch (iPoint)
                    {
                        case 0: parm.degreeDaysPhen[iFacet, iPoint] = 0.0; break;
                        case 1: parm.degreeDaysPhen[iFacet, iPoint] = 0.0; break;
                        case 2: parm.degreeDaysPhen[iFacet, iPoint] = tempArray[iFacet * 4]; break;
                        case 3: parm.degreeDaysPhen[iFacet, iPoint] = 1.0; break;
                        case 4: parm.degreeDaysPhen[iFacet, iPoint] = tempArray[iFacet * 4 + 1]; break;
                        case 5: parm.degreeDaysPhen[iFacet, iPoint] = 2.0; break;
                        case 6: parm.degreeDaysPhen[iFacet, iPoint] = tempArray[iFacet * 4 + 2]; break;
                        case 7: parm.degreeDaysPhen[iFacet, iPoint] = 3.0; break;
                        case 8: parm.degreeDaysPhen[iFacet, iPoint] = tempArray[iFacet * 4 + 3]; break;
                        case 9: parm.degreeDaysPhen[iFacet, iPoint] = 4.0; break;
                    }
                }
                ReadDoubleArray(parmsStrings[iLine++], out parm.degreeDaysReset);
                parm.treeSitePotential = ReadDoubleVal(parmsStrings[iLine++]);
                parm.maxSymbioticNFixationRatio = ReadDoubleVal(parmsStrings[iLine++]);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.minimumCNRatio = new double[nFacets, nWoodyParts];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.minimumCNRatio[i / nWoodyParts, i % nWoodyParts] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.maximumCNRatio = new double[nFacets, nWoodyParts];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.maximumCNRatio[i / nWoodyParts, i % nWoodyParts] = tempArray[i];

                parm.maximumLeafAreaIndex = ReadDoubleVal(parmsStrings[iLine++]);
                parm.kLeafAreaIndex = ReadDoubleVal(parmsStrings[iLine++]);
                parm.biomassToLeafAreaIndexFactor = ReadDoubleVal(parmsStrings[iLine++]);
                parm.annualFractionVolatilizedN = ReadDoubleVal(parmsStrings[iLine++]);
                parm.maxHerbRootDeathRate = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.shootDeathRate);
                parm.propAnnuals = ReadDoubleVal(parmsStrings[iLine++]);
                parm.monthToRemoveAnnuals = ReadDoubleVal(parmsStrings[iLine++]);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.relativeSeedProduction = new double[nFacets];
                for (int i = 0; i < nFacets; i++)
                    parm.relativeSeedProduction[i] = tempArray[i] / 10000.0;

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.waterEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.waterEffectOnEstablish[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.herbRootEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.herbRootEffectOnEstablish[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.litterEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.litterEffectOnEstablish[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.woodyCoverEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.woodyCoverEffectOnEstablish[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out parm.nominalPlantDeathRate);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.waterEffectOnDeathRate = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.waterEffectOnDeathRate[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.grazingEffectOnDeathRate = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.grazingEffectOnDeathRate[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.shadingEffectOnDeathRate = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.shadingEffectOnDeathRate[i / 4, i % 4] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out parm.fallRateOfStandingDead);
                parm.deathRateOfDeciduousLeaves = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.droughtDeciduous);
                parm.fractionWoodyLeafNTranslocated = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.leafDeathRate);
                ReadDoubleArray(parmsStrings[iLine++], out parm.fineRootDeathRate);
                ReadDoubleArray(parmsStrings[iLine++], out parm.fineBranchDeathRate);
                ReadDoubleArray(parmsStrings[iLine++], out parm.coarseBranchDeathRate);
                ReadDoubleArray(parmsStrings[iLine++], out parm.coarseRootDeathRate);
                parm.fractionCarbonGrazedReturned = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fractionExcretedNitrogenInFeces = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.fractionGrazedByFacet);
                parm.fractionGrazed = ReadDoubleVal(parmsStrings[iLine++]);
                parm.frequencyOfFire = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fractionBurned = ReadDoubleVal(parmsStrings[iLine++]);
                parm.burnMonth = ReadIntVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.fuelVsIntensity);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.greenVsIntensity = new double[2, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.greenVsIntensity[i / fireSeverities, i % fireSeverities] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionShootsBurned = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionShootsBurned[i / fireSeverities, i % fireSeverities] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionStandingDeadBurned = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionStandingDeadBurned[i / fireSeverities, i % fireSeverities] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionPlantsBurnedDead = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionPlantsBurnedDead[i / fireSeverities, i % fireSeverities] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionLitterBurned = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionLitterBurned[i / fireSeverities, i % fireSeverities] = tempArray[i];

                parm.fractionBurnedCarbonAsAsh = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fractionBurnedNitrogenAsAsh = ReadDoubleVal(parmsStrings[iLine++]);
                parm.frequencyOfFertilization = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fractionFertilized = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fertilizeMonth = ReadIntVal(parmsStrings[iLine++]);
                parm.fertilizeNitrogenAdded = ReadDoubleVal(parmsStrings[iLine++]);
                parm.fertilizeCarbonAdded = ReadDoubleVal(parmsStrings[iLine++]);
            }
        }

        private double ReadDoubleVal(string s)
        {
            double result;
            if (Double.TryParse(s, out result))
                return result;
            return Double.NaN;
        }

        private int ReadIntVal(string s)
        {
            int result;
            if (Int32.TryParse(s, out result))
                return result;
            return Int32.MinValue;
        }

        private void ReadDoubleArray(string s, out double[] dest)
        {
            string[] parts = s.Split(',');
            dest = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!Double.TryParse(parts[i], out dest[i]))
                    dest[i] = Double.NaN;
            }
        }

        /// <summary>
        /// The function for a line, yielding a Y value for a given X and the parameters for a line.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        private double Line(double x, double x1, double y1, double x2, double y2)
        {
#if G_RANGE_BUG
            return (double)((int)((y2 - y1) / (x2 - x1) * (x - x2) + y2));
#else
            return (y2 - y1) / (x2 - x1) * (x - x2) + y2;
#endif
        }
    }
}
