using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity</summary>
    /// <summary>This class represents the CLEM activity responsible for determining potential intake, tracking the quality of all food eaten and providing energy for all needs (e.g. wool production, pregnancy, lactation and growth)</summary>
    /// <remarks>This activity controls mortality and tracks body condition, while the Breed activity is responsible for conception and births</remarks>
    /// <version>2.0</version>
    /// <updates>Version 2.0 is now consistent with SCA Feeding Standards of Domesticated Ruminants and is a major update to CLEM from the IAT/NABSA animal production, requiring changes to a number of other components.</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs growth and aging of all ruminants. Only one instance of this activity is permitted")]
    [Version(2, 0, 1, "Updated to full SCA compliance")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrowSCA.htm")]
    public class RuminantActivityGrowSCA : CLEMActivityBase
    {
        [Link]
        private readonly Clock clock = null;

        private GreenhouseGasesType methaneEmissions;
        private ProductStoreTypeManure manureStore;
        private RuminantHerd ruminantHerd;

        /// <summary>
        /// Methane store for emissions
        /// </summary>
        [Description("Greenhouse gas store for methane emissions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store named Metane if present", typeof(GreenhouseGases) } })]
        [System.ComponentModel.DefaultValue("Use store named Methane if present")]
        public string MethaneStoreName { get; set; }

        /// <summary>
        /// Perform Activity with partial resources available
        /// </summary>
        [JsonIgnore]
        public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityGrowSCA()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if(MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                methaneEmissions = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
            else
                methaneEmissions = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, MethaneStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            manureStore = Resources.FindResourceType<ProductStore, ProductStoreTypeManure>(this, "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
            ruminantHerd = Resources.FindResourceGroup<RuminantHerd>();
        }

        /// <summary>Function to determine naturally wean individuals at start of timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // Natural weaning takes place here before animals eat or take milk from mother.
            foreach (var ind in ruminantHerd.Herd.Where(a => a.Weaned == false && MathUtilities.IsGreaterThan(a.Age, a.WeaningAge)))
            {
                ind.Wean(true, "Natural");

                // report wean. If mother has died create temp female with the mother's ID for reporting only
                ind.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.BreedParams, -1, 999) { ID = ind.MotherID }, clock.Today, ind));
            }
        }

        /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPotentialIntake")]
        private void OnCLEMPotentialIntake(object sender, EventArgs e)
        {
            // Calculate potential intake and reset stores
            // Order age descending so breeder females calculate milkproduction before suckings grow

            foreach (var groupInd in ruminantHerd.Herd.GroupBy(a => a.IsSucklingWithMother).OrderBy(a => a.Key))
            {
                foreach (var ind in groupInd)
                {
                    CalculatePotentialIntake(ind);
                    // reset tallies at start of the month.
                    // Perform after potential intake calculation as it needs MEContent from previous month for Lactation energy
                    // After this these can be reset ready for following intake and updating. 
                    ind.Intake.Reset();
                    ind.ResetEnergy();
                }
            }
        }

        private void CalculatePotentialIntake(Ruminant ind)
        {
            // calculate daily potential intake for the selected individual/cohort

            double liveWeightForIntake = ind.NormalisedAnimalWeight;
            // now performed at allocation of weight in Ruminant
            if (MathUtilities.IsLessThan(ind.HighWeight, ind.NormalisedAnimalWeight))
                liveWeightForIntake = ind.HighWeight;

            // Calculate potential intake based on current weight compared to SRW and previous highest weight

            // calculate milk intake shortfall for sucklings
            // all in units per day and multiplied at end of this section
            if (!ind.Weaned)
            {
                // potential milk intake/animal/day
                ind.Intake.Milk.Expected = ind.BreedParams.MilkIntakeIntercept + ind.BreedParams.MilkIntakeCoefficient * ind.Weight;

                // get estimated milk available
                // this will be updated to the corrected milk available in the calculate energy section.
                ind.Intake.Milk.Actual = Math.Min(ind.Intake.Milk.Expected, ind.MothersMilkProductionAvailable);

                // if milk supply low, suckling will subsitute forage up to a specified % of bodyweight (R_C60)
                if (MathUtilities.IsLessThan(ind.Intake.Milk.Actual, ind.Weight * ind.BreedParams.MilkLWTFodderSubstitutionProportion))
                    ind.Intake.Feed.Expected = Math.Max(0.0, ind.Weight * ind.BreedParams.MaxJuvenileIntake - ind.Intake.Milk.Actual * ind.BreedParams.ProportionalDiscountDueToMilk);

                // convert to timestep amount
                ind.Intake.Milk.Actual *= 30.4;
                ind.Intake.Milk.Expected *= 30.4;
            }
            else
            {
                if (ind.IsWeaner)
                {
                    // Reference: SCA Metabolic LWTs
                    // restored in v112 of NABSA for weaner animals
                    ind.Intake.Feed.Expected = ind.BreedParams.IntakeCoefficient * ind.StandardReferenceWeight * (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(ind.StandardReferenceWeight, 0.75)) * (ind.BreedParams.IntakeIntercept - (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(ind.StandardReferenceWeight, 0.75)));
                }
                else // 12month+ weaned individuals
                {
                    // Reference: SCA based actual LWTs
                    ind.Intake.Feed.Expected = ind.BreedParams.IntakeCoefficient * liveWeightForIntake * (ind.BreedParams.IntakeIntercept - liveWeightForIntake / ind.StandardReferenceWeight);
                }

                if (ind.Sex == Sex.Female)
                {
                    RuminantFemale femaleind = ind as RuminantFemale;
                    // Increase potential intake for lactating breeder
                    if (femaleind.IsLactating)
                    {
                        // move to half way through timestep
                        double dayOfLactation = femaleind.DaysLactating;
                        // Reference: Intake multiplier for lactating cow (M.Freer)
                        // double intakeMilkMultiplier = 1 + 0.57 * Math.Pow((dayOfLactation / 81.0), 0.7) * Math.Exp(0.7 * (1 - (dayOfLactation / 81.0)));
                        double intakeMilkMultiplier = 1 + ind.BreedParams.LactatingPotentialModifierConstantA * Math.Pow((dayOfLactation / ind.BreedParams.LactatingPotentialModifierConstantB), ind.BreedParams.LactatingPotentialModifierConstantC) * Math.Exp(ind.BreedParams.LactatingPotentialModifierConstantC * (1 - (dayOfLactation / ind.BreedParams.LactatingPotentialModifierConstantB)))*(1 - 0.5 + 0.5 * (ind.Weight/ind.NormalisedAnimalWeight));

                        // To make this flexible for sheep and goats, added three new Ruminant Coeffs
                        // Feeding standard values for Beef, Dairy suck, Dairy non-suck and sheep are:
                        // For 0.57 (A) use .42, .58, .85 and .69; for 0.7 (B) use 1.7, 0.7, 0.7 and 1.4, for 81 (C) use 62, 81, 81, 28
                        // added LactatingPotentialModifierConstantA, LactatingPotentialModifierConstantB and LactatingPotentialModifierConstantC
                        // replaces (A), (B) and (C) 
                        ind.Intake.Feed.Expected *= intakeMilkMultiplier;

                        // calculate estimated milk production for time step here
                        // assuming average feed quality if no previous diet values
                        // This need to happen before suckling potential intake can be determined.
                        CalculateLactationEnergy(femaleind);
                    }
                    else
                    {
                        femaleind.MilkProduction = 0;
                        femaleind.MilkProductionPotential = 0;
                        femaleind.MilkCurrentlyAvailable = 0;
                        femaleind.MilkMilkedThisTimeStep = 0;
                        femaleind.MilkSuckledThisTimeStep = 0;
                        femaleind.MilkProducedThisTimeStep = 0;
                    }
                }
                
                //TODO: option to restrict potential further due to stress (e.g. heat, cold, rain)

            }
            // set monthly potential intake
            ind.Intake.Feed.Expected *= 30.4;
        }

        /// <summary>Function to calculate growth of herd for the monthly timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            this.Status = ActivityStatus.NotNeeded;

            foreach (var breed in ruminantHerd.Herd.GroupBy(a => a.BreedParams.Name))
            {
                int unfed = 0;
                int unfedcalves = 0;
                double totalMethane = 0;
                foreach (Ruminant ind in breed.OrderByDescending(a => a.Age))
                {
                    ind.MetabolicIntake = ind.Intake.Feed.Actual;
                    this.Status = ActivityStatus.Success;
                    if (ind.Weaned)
                    {
                        // check that they had some food
                        if (ind.Intake.Feed.Actual == 0)
                            unfed++;

                        // calculate protein concentration

                        // Calculate diet dry matter digestibilty from the %N of the current diet intake.
                        // Reference: Ash and McIvor
                        // ind.DietDryMatterDigestibility = 36.7 + 9.36 * ind.PercentNOfIntake / 62.5;
                        // Now tracked via incoming food DMD values

                        // TODO: NABSA restricts Diet_DMD to 75% before supplements. Why?
                        // Our flow has already taken in supplements by this stage and cannot be distinguished
                        // Maybe this limit should be placed on some feed to limit DMD to 75% for non supp feeds
                        // A Ash stated that the 75% limit is no longer required and DMD above 75% is possible even if unlikely.

                        // Crude protein required generally 130g per kg of digestable feed.
                        double crudeProteinRequired = ind.BreedParams.ProteinCoefficient * ind.Intake.CombinedDetails.DryMatterDigestibility / 100;

                        // adjust for efficiency of use of protein, (default 90%) degradable. now user param.
                        double crudeProteinSupply = (ind.Intake.CombinedDetails.NitrogenContent * 62.5) * ind.BreedParams.ProteinDegradability;
                        // This was proteinconcentration * 0.9

                        // prevent future divide by zero issues.
                        if (MathUtilities.FloatsAreEqual(crudeProteinSupply, 0.0))
                            crudeProteinSupply = 0.001;

                        if (MathUtilities.IsLessThan(crudeProteinSupply, crudeProteinRequired))
                        {
                            double ratioSupplyRequired = (crudeProteinSupply + crudeProteinRequired) / (2 * crudeProteinRequired); // half-linear
                            //TODO: add min protein to parameters
                            ratioSupplyRequired = Math.Max(ratioSupplyRequired, 0.3);
                            ind.MetabolicIntake *= ratioSupplyRequired; // reduces intake proportionally as protein drops below CP required
                        }

                        // TODO: check if we still need to apply modification to only the non-supplemented component of intake
                        // Used to be 1.2 * Potential
                        ind.Intake.Feed.Actual = Math.Min(ind.Intake.Feed.Actual, ind.Intake.Feed.Expected);
                        // when discarding intake can we be specific and hang onto N?
                        ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake.Feed.Actual);
                    }
                    else
                    {
                        // for calves
                        // these individuals have access to milk or are separated from mother and must survive on calf calculated pasture intake

                        // if potential intake = 0 they have not needed to consume pasture and intake will be zero.
                        if (MathUtilities.IsGreaterThan(ind.Intake.Feed.Expected, 0.0))
                        {
                            // TODO: can these individuals be overfed?
                            ind.Intake.Feed.Actual = Math.Min(ind.Intake.Feed.Actual, ind.Intake.Feed.Expected);
                            ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake.Feed.Actual);
                        }

                        if (MathUtilities.IsLessThanOrEqual(ind.Intake.Milk.Actual + ind.Intake.Feed.Actual, 0))
                            unfedcalves++;
                    }

                    // TODO: nabsa adjusts potential intake for digestability of fodder here.
                    // This is now done in RuminantActivityGrazePasture

                    // calculate energy
                    CalculateEnergy(ind, out double methane);

                    // Sum and produce one event for breed at end of loop
                    totalMethane += methane;

                    // grow wool and cashmere
                    ind.Wool += ind.BreedParams.WoolCoefficient * ind.MetabolicIntake;
                    ind.Cashmere += ind.BreedParams.CashmereCoefficient * ind.MetabolicIntake;
                }

                ReportUnfedIndividualsWarning(breed, unfed, unfedcalves);

                // g per day -> total kg
                methaneEmissions?.Add(totalMethane * 30.4 / 1000, this, breed.Key, TransactionCategory);
            }
        }

        private void ReportUnfedIndividualsWarning(IGrouping<string, Ruminant> breed, int unfed, int unfedcalves)
        {
            // alert user to unfed animals in the month as this should not happen
            if (unfed > 0)
            {
                string warn = $"individuals of [r={breed.Key}] not fed";
                string warnfull = $"Some individuals of [r={breed.Key}] were not fed in some months (e.g. [{unfed}] individuals in [{clock.Today.Month}/{clock.Today.Year}])\r\nFix: Check feeding strategy and ensure animals are moved to pasture or fed in yards";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
            }
            if (unfedcalves > 0)
            {
                string warn = $"calves of [r={breed.Key}] not fed";
                string warnfull = $"Some calves of [r={breed.Key}] were not fed in some months (e.g. [{unfedcalves}] individuals in [{clock.Today.Month}/{clock.Today.Year}])\r\nFix: Check calves are are fed, or have access to pasture (moved with mothers or separately) when no milk is available from mother";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
            }
        }

        /// <summary>
        /// Function to calculate energy from intake and subsequent growth
        /// </summary>
        /// <remarks>
        /// All energy calculations are per day and multiplied at end to give weight gain for time step (monthly). 
        /// Energy and body mass change are based on SCA - Nutrient Requirements of Domesticated Ruminants, CSIRO
        /// </remarks>
        /// <param name="ind">Indivudal ruminant for calculation.</param>
        /// <param name="methaneProduced">Output parameter for methane produced.</param>
        private void CalculateEnergy(Ruminant ind, out double methaneProduced)
        {
            double km = 0;
            double kg = 0;
            double energyPredictedBodyMassChange = 0;
            double energyPredictedFatMassChange = 0;
            double energyPredictedProteinMassChange = 0;
            double intakeDaily = ind.Intake.Feed.Actual / 30.4;
            //double MEAvailableForGain = 0;

            // The following feed quality measures are provided in IFeedType and FoodResourcePackets
            // The individual tracks the quality of mixed feed types based on broad type (supplement or forage)
            // Energy metabolic - have DMD, fat content % CP% as inputs from ind as supplement and forage, do not need ether extract (fat) for forage
            // We can move these calculations to the RuminantIntake and calculate as they arrive or 

            // Sme 1 for females and castrates
            double sme = 1;
            // Sme 1.15 for all non-castrated males.
            if (ind.Weaned && ind.Sex == Sex.Male && (ind as RuminantMale).IsCastrated == false)
                sme = 1.15;

            // Wool production
            ind.EnergyForWool = CalculateWoolEnergy(ind);

            if (ind.Weaned)
            {
                km = (0.02 * ind.Intake.MEContent) + 0.5;
                kg = 0.006 + (ind.Intake.MEContent * 0.042);

                // rename params.EMainIntercept ->  HPVisceraFL  HeatProduction FeedLevel
                // rename params.EMainCoefficient ->  FHPScalar
                ind.EnergyForMaintenance = ind.BreedParams.Kme * sme * (ind.BreedParams.FHPScalar * Math.Pow(ind.Weight, 0.75) / km) * Math.Exp(-0.03 * Math.Min((ind.AgeInDays / 365), 6)) + (ind.BreedParams.HPVisceraFL * ind.EnergyAfterWool);

                if (ind.Sex == Sex.Female)
                {
                    RuminantFemale femaleind = ind as RuminantFemale;

                    // Determine energy required for fetal development
                    ind.EnergyForFetus = CalculatePregnancyEnergy(femaleind);

                    // calculate energy for lactation
                    // look for milk production calculated before offspring may have been weaned

                    // recalculate milk production based on DMD of food provided
                    ind.EnergyForLactation = CalculateLactationEnergy(femaleind);
                }

                //TODO: add draft individual energy requirement: does this also apply to unweaned individuals? If so move ouside loop

                //TODO: add mustering and movement to feed energy
                //TODO: allow zero feed or reduction on days when herd is moved.

                // we implemented this in the equation above, but this uses a parameter. Can we delete this parameter and assume fixed 6 years for all ruminants?
                // set maintenance age to maximum of 6 years (2190 days). Now uses EnergyMaintenanceMaximumAge (in years)
                //double maintenanceAge = Math.Min(ind.Age * 30.4, ind.BreedParams.EnergyMaintenanceMaximumAge * 365);

                // Reference: SCA p.24
                // Reference p19 (1.20). Does not include MEgraze or Ecold, also skips M,
                // 0.000082 is -0.03 Age in Years/365 for days 
                // TODO: delete ind.BreedParams.EMaintCoefficient
                // TODO: delete ind.BreedParams.EMaintExponent
                // TODO: delete ind.BreedParams.EMaintIntercept

                //double feedingValue;
                //MEAvailableForGain = ind.Intake.METotal - energyMaintenance;

                double feedingLevel = (ind.EnergyAfterLactation) / ind.EnergyForMaintenance;
                double gainLossAdj = feedingLevel - 2;

                double energyEmptyBodyGain = (ind.BreedParams.GrowthEnergyIntercept1 + gainLossAdj + (ind.BreedParams.GrowthEnergyIntercept2 - gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));

                if (ind.EnergyAvailableForGain < 0)
                {
                    energyPredictedBodyMassChange = (0.8 * ind.EnergyAvailableForGain) / (ind.BreedParams.EB2LW * energyEmptyBodyGain);
                }
                else
                {
                    energyPredictedBodyMassChange = (kg * ind.EnergyAvailableForGain) / (ind.BreedParams.EB2LW * energyEmptyBodyGain);
                }

                // energy protein mass MJ day-1
                energyPredictedProteinMassChange = energyPredictedBodyMassChange * ((ind.BreedParams.ProteinGainIntercept1 - ind.BreedParams.ProteinGainSlope * gainLossAdj) - (ind.BreedParams.ProteinGainIntercept2 - ind.BreedParams.ProteinGainSlope * gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));

                // energy fat mass MJ day-1
                energyPredictedFatMassChange = energyPredictedBodyMassChange * (ind.BreedParams.FatGainIntercept1 + ind.BreedParams.FatGainSlope * gainLossAdj + (ind.BreedParams.FatGainIntercept2 - ind.BreedParams.FatGainSlope * gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));
            }
            else // Unweaned
            {
                // unweaned individuals are assumed to be suckling as natural weaning rate set regardless of inclusion of a wean activity.
                // unweaned individuals without mother or milk from mother will need to try and survive on limited pasture until weaned.
                // CLEM has a rule for how much these individuals can eat which is defined in PotentialIntake()

                // recalculate milk intake based on mothers updated milk production for the time step using the previous monthly potential milk intake
                ind.Intake.Milk.Actual = Math.Min(ind.Intake.Milk.Expected, ind.MothersMilkProductionAvailable * 30.4);
                // remove consumed milk from mother.
                ind.Mother?.TakeMilk(ind.Intake.Milk.Actual, MilkUseReason.Suckling);
                double milkIntakeDaily = ind.Intake.Milk.Actual / 30.4;

                // Below now uses actual intake received rather than assume all potential intake is eaten
                double kml = 1;
                double kgl = 1;

                // ToDo: everything in  following should be using milkIntakeDaily not MilkIntake for timestep right?

                if (MathUtilities.IsPositive(intakeDaily + milkIntakeDaily))
                {
                    // average energy efficiency for maintenance
                    kml = ((milkIntakeDaily * 0.85) + (intakeDaily * km)) / (milkIntakeDaily + intakeDaily);
                    // average energy efficiency for growth
                    kgl = ((milkIntakeDaily * 0.7) + (intakeDaily * kg)) / (milkIntakeDaily + intakeDaily);
                }
                //double energyMilkConsumed = milkIntakeDaily * 3.2;
                // limit suckling intake of milk per day

                // ToDo: ensure MilkIntakeMaximum is daily and add units to summary description
                ind.Intake.AddMilk(Math.Min(ind.BreedParams.MilkIntakeMaximum, milkIntakeDaily), 3.2);
                    //.MEIntakeMilk = Math.Min(ind.BreedParams.MilkIntakeMaximum * 3.2, energyMilkConsumed);

                //MEtotal += MEmilk;

                //double Kmmilk = MEmilk / (ind.Intake.METotal + MEmilk) * 0.85;

                //double Kmdiet = ind.Intake.METotal / (ind.Intake.METotal + MEmilk) * ((0.02 * ind.Intake.MEContent) + 0.5);

                // ToDo: this value is not used...  I think the kml in energyMaintenance is meant to be km
                //double Km = Kmmilk + Kmdiet;

                // SCA assumes Age is double and in years    MainCoeff 0.26     EmainExp 0.03 (age in years)    the eqn changes to age in days
                // Delete EMaintExponent as fixed at -0.03
                ind.EnergyForMaintenance = (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / kml) * Math.Exp(-0.03 * ind.AgeInDays / 365);

                ind.EnergyForFetus = 0;
                ind.EnergyForLactation = 0;

                double feedingLevel = (ind.EnergyAfterWool) / ind.EnergyForMaintenance;
                double gainLossAdj = feedingLevel - 2;

                double energyEmptyBodyGain = (ind.BreedParams.GrowthEnergyIntercept1 + gainLossAdj + (ind.BreedParams.GrowthEnergyIntercept2 - gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));

                if(ind.EnergyAvailableForGain < 0)
                {
                    energyPredictedBodyMassChange = (0.8 * ind.EnergyAvailableForGain) / (ind.BreedParams.EB2LW * energyEmptyBodyGain);
                }
                else
                {
                    energyPredictedBodyMassChange = (kgl * ind.EnergyAvailableForGain) / (ind.BreedParams.EB2LW * energyEmptyBodyGain);
                }

                // energy protein mass MJ day-1
                energyPredictedProteinMassChange = energyPredictedBodyMassChange * ((ind.BreedParams.ProteinGainIntercept1 - ind.BreedParams.ProteinGainSlope * gainLossAdj) - (ind.BreedParams.ProteinGainIntercept2 - ind.BreedParams.ProteinGainSlope * gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));

                // energy fat mass MJ day-1
                energyPredictedFatMassChange = energyPredictedBodyMassChange * (ind.BreedParams.FatGainIntercept1 + ind.BreedParams.FatGainSlope * gainLossAdj + (ind.BreedParams.FatGainIntercept2 - ind.BreedParams.FatGainSlope * gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));
            }

            // TODO: Check that kgChange this isn't weaned/unweaned specific
            // TODO: check.. this following is the same as adding an additional / 23.6 to the AdjustProetinMass calculation as both use energyPredictedProteinMassChange
            double kgProteinChange = energyPredictedProteinMassChange / 23.6;
            // protein mass on protein basis not mass of lean tissue mass. use conversvion XXXX for weight to perform checksum.
            ind.AdjustProteinMass(energyPredictedProteinMassChange * kgProteinChange * 30.4);

            // TODO: see above
            double kgFatChange = energyPredictedFatMassChange / 39.6;
            ind.AdjustFatMass(energyPredictedFatMassChange * kgFatChange * 30.4);

            // if tried to take more protein than available dies...
            // if tried to take more fat than available dies...
            // fatal....
            // the mortality section below now checks for zero levels of fat and protein in the death decision which happens after this weight gain event.

            ind.PreviousWeight = ind.Weight;
            // update weight based on the time-step
            ind.Weight = Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange * 30.4);

            // ToDo: Precious code had upper weight checks. Delete once everyone has looked at this as correct SRW allows for exceeding and intake should reduce based on fat content!
            //ind.Weight = Math.Min(
            //    Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange),
            //    ind.StandardReferenceWeight * ind.BreedParams.MaximumSizeOfIndividual
            //    );

            // Function to calculate approximate methane produced by animal, based on feed intake
            // Function based on Freer spreadsheet
            // methaneproduced is  0.02 * intakeDaily * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
            // methane is methaneProduced / 55.28 * 1000; // grams per day

            // Charmley et al 2016 can be substituted by intercept = 0 and coefficient = 20.7
            // per day at this point.
            methaneProduced = ind.BreedParams.MethaneProductionCoefficient * intakeDaily;
        }

        /// <summary>
        /// Determine the energy required for wool growth
        /// </summary>
        /// <param name="ind">Ruminant individual</param>
        /// <returns>Daily energy required for wool production time step</returns>
        private double CalculateWoolEnergy(Ruminant ind)
        {
            // TODO: wool production energy here!
            return 0;
        }

        /// <summary>
        /// Determine the energy required for lactation
        /// </summary>
        /// <param name="ind">Female individual</param>
        /// <returns>Daily energy required for lactation this time step</returns>
        private double CalculateLactationEnergy(RuminantFemale ind)
        {
            if (ind.IsLactating | MathUtilities.IsPositive(ind.MilkProductionPotential))
            {
                ind.MilkMilkedThisTimeStep = 0;
                ind.MilkSuckledThisTimeStep = 0;

                //TODO: we need to ensure that the montly diet details are summarised by this point and these will be known.

                // this is called in potential intake using last months energy available after pregnancy
                // and called again in CalculateEnergy of Weight Gain where it uses the energy available after pregnancy from the current time step.

                // update old parameters in breed params to new approach based on energy and not L milk.
                // TODO: new intercept = 0.4 and coefficient = 0.02
                // TODO: update peak yield.
                double kl = ind.BreedParams.ELactationEfficiencyCoefficient * ind.Intake.MEContent + ind.BreedParams.ELactationEfficiencyIntercept;
                double milkTime = ind.DaysLactating; // assumes mid month

                // determine milk production curve to use
                double milkCurve = ind.BreedParams.MilkCurveSuckling;
                // if milking is taking place use the non-suckling curve for duration of lactation
                // otherwise use the suckling curve where there is a larger drop off in milk production
                if (ind.SucklingOffspringList.Any() == false)
                    milkCurve = ind.BreedParams.MilkCurveNonSuckling;

                // calculate
                // eq 66 thru 76



                ind.MilkProductionPotential = ind.BreedParams.MilkPeakYield * Math.Pow(ind.StandardReferenceWeight, y: 0.75) * ind.Weight / ind.NormalisedAnimalWeight * (Math.Pow(((milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay), milkCurve)) * Math.Exp(milkCurve * (1 - (milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay));
                ind.MilkProductionPotential = Math.Max(ind.MilkProductionPotential, 0.0);
                // Reference: Potential milk prodn, 3.2 MJ/kg milk - Jouven et al 2008
                double energyMilk = ind.MilkProductionPotential * 3.2 / kl;
                // adjust last time step's energy balance
                double adjustedEnergyBalance = ind.EnergyAfterPregnancy;
                if (adjustedEnergyBalance < (-0.5936 / 0.322 * energyMilk))
                    adjustedEnergyBalance = (-0.5936 / 0.322 * energyMilk);

                // set milk production in lactating females for consumption.
                ind.MilkProduction = Math.Max(0.0, ind.MilkProductionPotential * (0.5936 + 0.322 * adjustedEnergyBalance / energyMilk));
                
                ind.MilkCurrentlyAvailable = ind.MilkProduction * 30.4;
                ind.MilkProducedThisTimeStep = ind.MilkCurrentlyAvailable;

                // returns the energy required for milk production
                return ind.MilkProduction * 3.2 / kl;
            }
            return 0;
        }

        /// <summary>
        /// Determine the energy required for pregnancy
        /// </summary>
        /// <param name="ind">Female individual</param>
        /// <returns>energy required for fetus this timestep</returns>
        private double CalculatePregnancyEnergy(RuminantFemale ind)
        {
            if (!ind.IsPregnant) return 0;

            // Potential birth weight
            // Reference: Freer
            // SRWBirth -> BirthScalar

            // ScaledBirthWeight property of pregnant female
            // (1 - 0.33 + 0.33 * ind.RelativeSize) * ind.BreedParams.SRWBirth * ind.StandardReferenceWeight;

            double normalWeightFetus = ind.ScaledBirthWeight * Math.Exp(ind.BreedParams.FetalNormWeightParameter * (1 - Math.Exp(ind.BreedParams.FetalNormWeightParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength))));

            double fetusWeight = double.NaN;

            double conceptusWeight = ind.NumberOfOffspring * (ind.BreedParams.ConceptusWeightRatio * ind.ScaledBirthWeight * Math.Exp(ind.BreedParams.ConceptusWeightParameter * (1 - Math.Exp(ind.BreedParams.ConceptusWeightParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength)))) + (fetusWeight - normalWeightFetus));

            double relativeConditionFoet = double.NaN;

            // MJ per day
            double conceptusME = (ind.BreedParams.ConceptusEnergyContent * (ind.NumberOfOffspring * ind.BreedParams.ConceptusWeightRatio * ind.ScaledBirthWeight) * relativeConditionFoet *
                (ind.BreedParams.ConceptusEnergyParameter * ind.BreedParams.ConceptusEnergyParameter2 / (ind.BreedParams.GestationLength * 30.4)) * 
                Math.Exp(ind.BreedParams.ConceptusEnergyParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength) + ind.BreedParams.ConceptusEnergyParameter * (1 - Math.Exp(ind.BreedParams.ConceptusEnergyParameter2 * (1 - ind.DaysPregnant/ ind.BreedParams.GestationLength))))) / 0.13;

            // kg protein per day
            double conceptusProteinReq = ind.BreedParams.ConceptusProteinContent * (ind.NumberOfOffspring * ind.BreedParams.ConceptusWeightRatio * ind.ScaledBirthWeight) * relativeConditionFoet * 
                (ind.BreedParams.ConceptusProteinParameter * ind.BreedParams.ConceptusProteinParameter2 / (ind.BreedParams.GestationLength * 30.4)) * Math.Exp(ind.BreedParams.ConceptusProteinParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength) + ind.BreedParams.ConceptusProteinParameter * (1 - Math.Exp(ind.BreedParams.ConceptusProteinParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength))));

            //ind.Weight / ind.HighWeightWhenNotPregnant

            double conceptusProteinKG = conceptusProteinReq * conceptusWeight;

            //double WtFetus = 0;
            //double EnergyFetus = 0;
            //double FatFetus = 0;
            //double ProteinFetus = 0;

            // 0.13 means calculated in NE (Nett energy) but reported back in ME
            //return potentialBirthWeight * 349.16 * 0.000058 * Math.Exp(345.67 - 0.000058 * fetusAge - 349.16 * Math.Exp(-0.000058 * fetusAge)) / 0.13;
        }

        /// <summary>
        /// Function to calculate manure production and place in uncollected manure pools of the "manure" resource in ProductResources 
        /// This is called at the end of CLEMAnimalWeightGain so after intake determines and before deaths and sales.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCalculateManure")]
        private void OnCLEMCalculateManure(object sender, EventArgs e)
        {
            if (manureStore != null)
            {
                // sort by animal location
                foreach (var groupInds in ruminantHerd.Herd.GroupBy(a => a.Location))
                {
                    manureStore.AddUncollectedManure(groupInds.Key ?? "", groupInds.Sum(a => a.Intake.Feed.Actual * (100 - a.Intake.CombinedDetails.DryMatterDigestibility) / 100));
                }
            }
        }

        /// <summary>
        /// Function to age individuals and remove those that died in timestep
        /// This needs to be undertaken prior to herd management
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            // grow all individuals
            foreach (Ruminant ind in ruminantHerd.Herd)
                ind.IncrementAge();
        }

        /// <summary>Function to determine which animlas have died and remove from the population</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalDeath")]
        private void OnCLEMAnimalDeath(object sender, EventArgs e)
        {
            // remove individuals that died
            // currently performed in the month after weight has been adjusted
            // and before breeding, trading, culling etc (See Clock event order)

            // Calculated by
            // critical weight &&
            // juvenile (unweaned) death based on mothers weight &&
            // adult weight adjusted base mortality.
            // zero fat or protein mass in body

            List<Ruminant> herd = ruminantHerd.Herd;

            // zero fat or protein mass based mortality
            IEnumerable<Ruminant> died = herd.Where(a => a.FatMass == 0 || a.ProteinMass == 0);
            // set died flag
            foreach (Ruminant ind in died)
                ind.SaleFlag = HerdChangeReason.DiedUnderweight;
            
            //ToDo: remove once for loop checked.
            //died.Select(a => { a.SaleFlag = HerdChangeReason.DiedUnderweight; return a; });
            ruminantHerd.RemoveRuminant(died, this);

            // weight based mortality
            died = herd.Where(a => a.Weight < (a.HighWeight * a.BreedParams.ProportionOfMaxWeightToSurvive)).ToList();
            // set died flag
            foreach (Ruminant ind in died)
                ind.SaleFlag = HerdChangeReason.DiedUnderweight;
            //ToDo: remove once for loop checked.
            //died.Select(a => { a.SaleFlag = HerdChangeReason.DiedUnderweight; return a; }).ToList();
            ruminantHerd.RemoveRuminant(died, this);

            // base mortality adjusted for condition
            foreach (var ind in ruminantHerd.Herd)
            {
                double mortalityRate = 0;
                if (!ind.Weaned)
                {
                    mortalityRate = 0;
                    if(ind.Mother == null || MathUtilities.IsLessThan(ind.Mother.Weight, ind.BreedParams.CriticalCowWeight * ind.StandardReferenceWeight))
                        // if no mother assigned or mother's weight is < CriticalCowWeight * SFR
                        mortalityRate = ind.BreedParams.JuvenileMortalityMaximum;
                    else
                        // if mother's weight >= criticalCowWeight * SFR
                        mortalityRate = Math.Exp(-Math.Pow(ind.BreedParams.JuvenileMortalityCoefficient * (ind.Mother.Weight / ind.Mother.NormalisedAnimalWeight), ind.BreedParams.JuvenileMortalityExponent));

                    mortalityRate += ind.BreedParams.MortalityBase;
                    mortalityRate = Math.Min(mortalityRate, ind.BreedParams.JuvenileMortalityMaximum);
                }
                else
                    mortalityRate = 1 - (1 - ind.BreedParams.MortalityBase) * (1 - Math.Exp(Math.Pow(-(ind.BreedParams.MortalityCoefficient * (ind.Weight / ind.NormalisedAnimalWeight - ind.BreedParams.MortalityIntercept)), ind.BreedParams.MortalityExponent)));

                // convert mortality from annual (calculated) to monthly (applied).
                if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), mortalityRate/12))
                    ind.Died = true;
            }

            died = herd.Where(a => a.Died).ToList();
            foreach (Ruminant ind in died)
                ind.SaleFlag = HerdChangeReason.DiedUnderweight;
            //ToDo: remove once for loop checked.
            //died.Select(a => { a.SaleFlag = HerdChangeReason.DiedMortality; return a; }).ToList();

            //// TODO: separate foster from real mother for genetics
            //// check for death of mother with sucklings and try foster sucklings
            //IEnumerable<RuminantFemale> mothersWithSuckling = died.OfType<RuminantFemale>().Where(a => a.SucklingOffspringList.Any());
            //List<RuminantFemale> wetMothersAvailable = died.OfType<RuminantFemale>().Where(a => a.IsLactating & a.SucklingOffspringList.Count() == 0).OrderBy(a => a.DaysLactating).ToList();
            //int wetMothersAssigned = 0;
            //if (wetMothersAvailable.Any())
            //{
            //    if(mothersWithSuckling.Any())
            //    {
            //        foreach (var deadMother in mothersWithSuckling)
            //        {
            //            foreach (var suckling in deadMother.SucklingOffspringList)
            //            {
            //                if(wetMothersAssigned < wetMothersAvailable.Count)
            //                {
            //                    suckling.Mother = wetMothersAvailable[wetMothersAssigned];
            //                    wetMothersAssigned++;
            //                }
            //                else
            //                    break;
            //            }

            //        }
            //    }
            //}

            ruminantHerd.RemoveRuminant(died, this);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new StringWriter();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Methane emissions will be placed in ");
            if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
                htmlWriter.Write("<span class=\"resourcelink\">GreenhouseGases.Methane</span> if present");
            else
                htmlWriter.Write($"<span class=\"resourcelink\">{MethaneStoreName}</span>");
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        } 
        #endregion

    }
}
