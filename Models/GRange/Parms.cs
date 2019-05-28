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

    public partial class GRange : Model, IPlant, ICanopy, IUptake
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
        public class Parms
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

        private Parms[] parmArray = null;

        /// <summary>
        /// 
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
                    throw new Exception("Error parsing G-Range parameters");
                parm.prcpThreshold = ReadDoubleVal(parmsStrings[iLine++]);
                parm.prcpThresholdFraction = ReadDoubleVal(parmsStrings[iLine++]);
                parm.baseFlowFraction = ReadDoubleVal(parmsStrings[iLine++]);
                ReadDoubleArray(parmsStrings[iLine++], out parm.soilTranspirationFraction);
                parm.initSoilCNRatio = ReadDoubleVal(parmsStrings[iLine++]);
                parm.initLigninNRatio = ReadDoubleVal(parmsStrings[iLine++]);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.shrubCarbon = new double[nWoodyParts, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.shrubCarbon[i % nWoodyParts, i / nWoodyParts] = tempArray[i];

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.treeCarbon = new double[nWoodyParts, 2];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.treeCarbon[i % nWoodyParts, i / nWoodyParts] = tempArray[i];

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
                // { { 0.0, 0.0, 200.0, 1.0, 300.0, 2.0, 400.0, 3.0, 700.0, 4.0 }, { 0.0, 0.0, 200.0, 1.0, 300.0, 2.0, 400.0, 3.0, 700.0, 4.0 }, { 0.0, 0.0, 200.0, 1.0, 300.0, 2.0, 400.0, 3.0, 700.0, 4.0 } };
                ReadDoubleArray(parmsStrings[iLine++], out parm.degreeDaysReset);
                parm.treeSitePotential = ReadDoubleVal(parmsStrings[iLine++]);
                parm.maxSymbioticNFixationRatio = ReadDoubleVal(parmsStrings[iLine++]);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.minimumCNRatio = new double[nFacets, nWoodyParts];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.minimumCNRatio[i / nWoodyParts, i % nWoodyParts] = tempArray[i];
                // { { 10.0, 13.0, 0.0, 0.0, 0.0 }, { 13.0, 20.0, 30.0, 50.0, 60.0 }, { 15.0, 21.0, 32.0, 52.0, 52.0 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.maximumCNRatio = new double[nFacets, nWoodyParts];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.maximumCNRatio[i / nWoodyParts, i % nWoodyParts] = tempArray[i];
                //{ { 30.0, 33.0, 0.0, 0.0, 0.0 }, { 33.0, 40.0, 50.0, 80.0, 90.0 }, { 35.0, 51.0, 62.0, 92.0, 95.0 } };

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
                //{ 0.4500, 0.2350, 0.2550 };  // FACTOR OF 10000!  

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.waterEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.waterEffectOnEstablish[i / 4, i % 4] = tempArray[i];
                // { { 0.43, 0.67, 3.0, 1.00 }, { 1.0, 0.66, 6.0, 1.00 }, { 1.0, 0.30, 4.0, 1.00 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.herbRootEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.herbRootEffectOnEstablish[i / 4, i % 4] = tempArray[i];
                // { { 50.0, 1.00, 300., 0.57 }, { 100.0, 1.00, 610.0, 0.10 }, { 150.0, 1.00, 550.0, 0.05 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.litterEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.litterEffectOnEstablish[i / 4, i % 4] = tempArray[i];
                // { { 300.0, 1.00, 1000.0, 0.49 }, { 340.0, 1.00, 1000.0, 0.50 }, { 400.0, 1.00, 1000.0, 0.20 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.woodyCoverEffectOnEstablish = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.woodyCoverEffectOnEstablish[i / 4, i % 4] = tempArray[i];
                // { { 0.0, 1.00, 0.3, 0.40 }, { 0.0, 1.00, 0.39, 0.00 }, { 0.0, 1.00, 0.65, 0.00 } };

                ReadDoubleArray(parmsStrings[iLine++], out parm.nominalPlantDeathRate);

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.waterEffectOnDeathRate = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.waterEffectOnDeathRate[i / 4, i % 4] = tempArray[i];
                // { { 0.0, 0.05, 2.5, 0.000 }, { 0.0, 0.0050, 2.5, 0.000 }, { 0.0, 0.005, 2.5, 0.000 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.grazingEffectOnDeathRate = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.grazingEffectOnDeathRate[i / 4, i % 4] = tempArray[i];
                //{ { 0.0, 0.0, 1.0, 0.040 }, { 0.0, 0.000, 1.0, 0.0050 }, { 0.0, 0.00, 1.0, 0.0050 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.shadingEffectOnDeathRate = new double[nFacets, 4];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.shadingEffectOnDeathRate[i / 4, i % 4] = tempArray[i];
                // { { 0.0, 0.0, 4.0, 0.020 }, { 0.0, 0.000, 4.0, 0.025 }, { 0.0, 0.00, 4.0, 0.025 } };

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
                // { { 0.0, 1.0 }, { 0.3, 0.7 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionShootsBurned = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionShootsBurned[i / fireSeverities, i % fireSeverities] = tempArray[i];
                // { { 0.1, 1.0 }, { 0.1, 0.2 }, { 0.1, 0.2 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionStandingDeadBurned = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionStandingDeadBurned[i / fireSeverities, i % fireSeverities] = tempArray[i];
                // { { 0.4, 1.0 }, { 0.3, 0.9 }, { 0.3, 0.9 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionPlantsBurnedDead = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionPlantsBurnedDead[i / fireSeverities, i % fireSeverities] = tempArray[i];
                // { { 0.2, 0.5 }, { 0.0, 0.15 }, { 0.0, 0.15 } };

                ReadDoubleArray(parmsStrings[iLine++], out tempArray);
                parm.fractionLitterBurned = new double[nFacets, fireSeverities];
                for (int i = 0; i < tempArray.Length; i++)
                    parm.fractionLitterBurned[i / fireSeverities, i % fireSeverities] = tempArray[i];
                // { { 0.1, 0.5 }, { 0.1, 0.5 }, { 0.1, 0.5 } };

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
    }
}
