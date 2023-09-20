using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity</summary>
    /// <summary>This class represents the CLEM activity responsible for determining potential intake, tracking the quality of all food eaten, and providing energy for all needs (e.g. wool production, pregnancy, lactation and growth)</summary>
    /// <remarks>This activity controls mortality and tracks body condition, while the Breed activity is responsible for conception and births</remarks>
    /// <version>2.0</version>
    /// <updates>Version 2.0 is now consistent with SCA Feeding Standards of Domesticated Ruminants and is a major update to CLEM from the IAT/NABSA animal production, requiring changes to a number of other components and providing new parameters.</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs growth and aging of all ruminants based on Australian Feeding Standard. Only one instance of this activity is permitted")]
    [Version(2, 0, 1, "Updated to full SCA compliance")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrowSCA.htm")]
    public class RuminantActivityGrowSCA : CLEMActivityBase
    {
        [Link]
        private readonly Clock clock = null;

        private GreenhouseGasesType methaneEmissions;
        private ProductStoreTypeManure manureStore;
        private RuminantHerd ruminantHerd;
        private readonly FoodResourcePacket milkPacket;

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
            milkPacket = new FoodResourcePacket()
            {
                // ToDo: fill with correct values
                TypeOfFeed = FeedType.Milk,
                EnergyContent = 0,
                RumenDegradableProtein = 0,
                FatContent = 0,
                NitrogenToCrudeProteinFactor = 5.0,
                NitrogenContent = 0
            };
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
                    // Perform after potential intake calculation as it needs MEContent from previous month for Lactation energy
                    // After this these tallies can be reset ready for following intake and updating. 
                    ind.Intake.Reset();
                    ind.ResetEnergy();
                }
            }
        }

        private void CalculatePotentialIntake(Ruminant ind)
        {
            if (!ind.Weaned)
            {
                // all in units per day and multiplied at end of this section

                // potential milk intake/animal/day
                ind.Intake.Milk.Expected = ind.BreedParams.MilkIntakeIntercept + ind.BreedParams.MilkIntakeCoefficient * ind.Weight;

                // get estimated milk available
                // this will be updated to the corrected milk available in the calculate energy section.
                double actualMilk = Math.Min(ind.Intake.Milk.Expected, ind.MothersMilkProductionAvailable);

                // if milk supply low, suckling will subsitute forage up to a specified % of bodyweight (R_C60)
                if (MathUtilities.IsLessThan(actualMilk, ind.Weight * ind.BreedParams.MilkLWTFodderSubstitutionProportion))
                    ind.Intake.Feed.Expected = Math.Max(0.0, ind.Weight * ind.BreedParams.MaxJuvenileIntake - actualMilk * ind.BreedParams.ProportionalDiscountDueToMilk);
            }
            else
            {
                double liveWeightForIntake = ind.NormalisedAnimalWeight;
                // now performed at allocation of weight in Ruminant
                if (MathUtilities.IsLessThan(ind.HighWeight, ind.NormalisedAnimalWeight))
                    liveWeightForIntake = ind.HighWeight;

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
                        // move to half way through timestep is now in female.DaysLactating
                        // Reference: Intake multiplier for lactating cow (M.Freer)
                        // double intakeMilkMultiplier = 1 + 0.57 * Math.Pow((dayOfLactation / 81.0), 0.7) * Math.Exp(0.7 * (1 - (dayOfLactation / 81.0)));
                        // To make this flexible for sheep and goats, added three new Ruminant Coeffs
                        // Feeding standard values for Beef, Dairy suck, Dairy non-suck and sheep are:
                        // For 0.57 (A) use .42, .58, .85 and .69; for 0.7 (B) use 1.7, 0.7, 0.7 and 1.4, for 81 (C) use 62, 81, 81, 28
                        // added LactatingPotentialModifierConstantA, LactatingPotentialModifierConstantB and LactatingPotentialModifierConstantC
                        // replaces (A), (B) and (C) 
                        double intakeMilkMultiplier = 1 + ind.BreedParams.LactatingPotentialModifierConstantA * Math.Pow((femaleind.DaysLactating / ind.BreedParams.LactatingPotentialModifierConstantB), ind.BreedParams.LactatingPotentialModifierConstantC) * Math.Exp(ind.BreedParams.LactatingPotentialModifierConstantC * (1 - (femaleind.DaysLactating / ind.BreedParams.LactatingPotentialModifierConstantB)))*(1 - 0.5 + 0.5 * (ind.Weight/ind.NormalisedAnimalWeight));

                        ind.Intake.Feed.Expected *= intakeMilkMultiplier;

                        // calculate estimated milk production for time step here
                        // assuming average feed quality if no previous diet values
                        // This need to happen before suckling potential intake can be determined.
                        CalculateLactationEnergy(femaleind, false);
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

                //TODO: reduce intake based on high fat to protein concentration.
                //TODO: what actually stops an animal growing when feed better than maintenance energy.. just add fat until fat reduces hunger?
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
                    this.Status = ActivityStatus.Success;
                    if (ind.Weaned && ind.Intake.Feed.Actual == 0)
                        unfed++;
                    else if(!ind.Weaned && MathUtilities.IsLessThanOrEqual(ind.Intake.Milk.Actual + ind.Intake.Feed.Actual, 0))
                        unfedcalves++;

                    // Adjusting potential intake for digestability of fodder is now done in RuminantActivityGrazePasture.
                    // ToDo: check that the new protein requirements don't provide this functionality. 

                    CalculateEnergy(ind, out double methane);

                    // Sum and produce one event for breed at end of loop
                    totalMethane += methane;
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
            double intakeDaily = ind.Intake.Feed.Actual / 30.4;
            double feedingLevel = 0;
            double gainLossAdj = 0;

            // The feed quality measures are now provided in IFeedType and FoodResourcePackets
            // The individual tracks the quality of mixed feed types based on broad type (supplement or forage) in Intake
            // Energy metabolic - have DMD, fat content % CP% as inputs from ind as supplement and forage, do not need ether extract (fat) for forage
            // We can move these calculations to the RuminantIntake and calculate as they arrive or 

            // Sme 1 for females and castrates
            double sme = 1;
            // Sme 1.15 for all non-castrated males.
            if (ind.Weaned && ind.Sex == Sex.Male && (ind as RuminantMale).IsCastrated == false)
                sme = 1.15;

            // Wool production
            ind.EnergyForWool = CalculateWoolEnergy(ind);

            double conceptusProtein = 0;
            double conceptusFat = 0;

            // calculate here as is also needed in not weaned.. in case consumed feed and milk.
            double km = 0.02 * ind.Intake.ME + 0.5;
            double kg = 0.006 + ind.Intake.ME * 0.042;

            if (ind.Weaned)
            {
                // TODO: rename params.EMainIntercept ->  HPVisceraFL  HeatProduction FeedLevel
                // TODO: rename params.EMainCoefficient ->  FHPScalar

                CalculateMaintenanceEnergy(ind, km, ref feedingLevel, sme);

                if (ind is RuminantFemale indFemale)
                {
                    // Determine energy required for fetal development
                    ind.EnergyForFetus = CalculatePregnancyEnergy(indFemale, ref conceptusProtein, ref conceptusFat);

                    // calculate energy for lactation
                    // look for milk production calculated before offspring may have been weaned
                    // recalculate milk production based on DMD and MEContent of food provided
                    // MJ / time step
                    ind.EnergyForLactation = CalculateLactationEnergy(indFemale, true);
                }

                // we implemented this in the equation above, but this uses a parameter. Can we delete this parameter and assume fixed 6 years for all ruminants?
                // set maintenance age to maximum of 6 years (2190 days). Now uses EnergyMaintenanceMaximumAge (in years)
                //double maintenanceAge = Math.Min(ind.Age * 30.4, ind.BreedParams.EnergyMaintenanceMaximumAge * 365);

                // Reference: SCA p.24
                // Reference p19 (1.20). Does not include MEgraze or Ecold, also skips M,
                // 0.000082 is -0.03 Age in Years/365 for days 
                // TODO: delete ind.BreedParams.EMaintCoefficient
                // TODO: delete ind.BreedParams.EMaintExponent
                // TODO: delete ind.BreedParams.EMaintIntercept
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
                //double kgl = 1; overwirte kg in statement below so it can be used in common code outside weaned loop

                // ToDo: everything in following should be using milkIntakeDaily not MilkIntake for timestep right?

                if (MathUtilities.IsPositive(intakeDaily + milkIntakeDaily))
                {
                    // average energy efficiency for maintenance
                    kml = ((milkIntakeDaily * 0.85) + (intakeDaily * km)) / (milkIntakeDaily + intakeDaily);
                    // average energy efficiency for growth
                    // previously kgl, but we can override kg as it is next used in energyPredictedBodyMassChange outside the loop with the same equation as weaned except for the kg vs kgl term
                    kg = ((milkIntakeDaily * 0.7) + (intakeDaily * kg)) / (milkIntakeDaily + intakeDaily);
                }

                // ToDo: ensure MilkIntakeMaximum is daily and add units to summary description

                milkPacket.EnergyContent = ind.BreedParams.EnergyContentMilk;
                milkPacket.Amount = Math.Min(ind.BreedParams.MilkIntakeMaximum, milkIntakeDaily)*30.4;
                ind.Intake.AddFeed(milkPacket);

                CalculateMaintenanceEnergy(ind, kml, ref feedingLevel, sme);

                feedingLevel = ind.EnergyFromIntake / ind.EnergyForMaintenance;
                gainLossAdj = feedingLevel - 1;
            }

            //TODO: add draft individual energy requirement: does this also apply to unweaned individuals? If so move ouside loop
            //TODO: add mustering and movement to feed energy
            //TODO: allow zero feed or reduction on days when herd is moved.

            double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + gainLossAdj + (ind.BreedParams.GrowthEnergyIntercept2 - gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4)));

            double energyPredictedBodyMassChange;
            if (ind.EnergyAvailableForGain < 0)
                energyPredictedBodyMassChange = (0.8 * ind.EnergyAvailableForGain) / (ind.BreedParams.EB2LW * energyEmptyBodyGain);
            else
                energyPredictedBodyMassChange = (kg * ind.EnergyAvailableForGain) / (ind.BreedParams.EB2LW * energyEmptyBodyGain);

            // energy protein mass MJ day-1
            double energyPredictedProteinMassChange = energyPredictedBodyMassChange * (ind.BreedParams.ProteinGainIntercept1 - ind.BreedParams.ProteinGainSlope * gainLossAdj - (ind.BreedParams.ProteinGainIntercept2 - ind.BreedParams.ProteinGainSlope * gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));
            // energy fat mass MJ day-1
            double energyPredictedFatMassChange = energyPredictedBodyMassChange * (ind.BreedParams.FatGainIntercept1 + ind.BreedParams.FatGainSlope * gainLossAdj + (ind.BreedParams.FatGainIntercept2 - ind.BreedParams.FatGainSlope * gainLossAdj) / (1 + Math.Exp(-6 * (ind.RelativeSize - 0.4))));

            //TODO: complete Nin and Nout
            //double Nin = (ind.Intake.xxxx * UDPIsolid) + DPLSmcp)/ 6.25 + (DPLSmilk / 6.38);
            //double Nout = Nin - (dProtdt / 6.25) - (dMilkdt / 6.38) - (dfetalproteindt / 6.25) - (dwooldt / 6.25)

            // TODO: check.. this following is the same as adding an additional / 23.6 to the AdjustProteinMass calculation as both use energyPredictedProteinMassChange
            double kgProteinChange = energyPredictedProteinMassChange / 23.6;
            // protein mass on protein basis not mass of lean tissue mass. use conversvion XXXX for weight to perform checksum.
            ind.AdjustProteinMass(energyPredictedProteinMassChange * kgProteinChange * 30.4);

            // TODO: see above
            double kgFatChange = energyPredictedFatMassChange / 39.6;
            ind.AdjustFatMass(energyPredictedFatMassChange * kgFatChange * 30.4);

            // the mortality section below now checks for zero levels of fat and protein in the death decision which happens after this weight gain event.

            ind.PreviousWeight = ind.Weight;
            // update weight based on the time-step
            ind.Weight = Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange * 30.4);

            // Function to calculate approximate methane produced by animal, based on feed intake
            // Function based on Freer spreadsheet
            // methaneproduced is  0.02 * intakeDaily * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
            // methane is methaneProduced / 55.28 * 1000; // grams per day

            // Charmley et al 2016 can be substituted by intercept = 0 and coefficient = 20.7
            // per day at this point.
            methaneProduced = ind.BreedParams.MethaneProductionCoefficient * intakeDaily;
        }

        /// <summary>
        /// Calculate maintenance energy and reduce intake and recalculate based on rumen protein deficiency
        /// </summary>
        /// <param name="ind">The individual ruminant</param>
        /// <param name="km"></param>
        /// <param name="feedingLevel"></param>
        /// <param name="sme"></param>
        private void CalculateMaintenanceEnergy(Ruminant ind, double km, ref double feedingLevel, double sme)
        {
            // calculate maintenance energy
            // then determine the protein requirement of rumen bacteria
            // adjujst intake proportionally and recalulate maintenance energy with adjusted intake energy
            int recalculate = 0;
            do
            {
                // SCA assumes Age is double and in years    MainCoeff 0.26     EmainExp 0.03 (age in years)    the eqn changes to age in days
                // ToDo: Delete EMaintExponent as fixed at -0.03

                if (!ind.Weaned)
                    // ToDo: this maint doesn't include the intake energy or is that kml?
                    ind.EnergyForMaintenance = (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / km) * Math.Exp(-0.03 * ind.AgeInDays / 365);
                else
                    ind.EnergyForMaintenance = ind.BreedParams.Kme * sme * (ind.BreedParams.FHPScalar * Math.Pow(ind.Weight, 0.75) / km) * Math.Exp(-0.03 * Math.Min((ind.AgeInDays / 365), 6)) + (ind.BreedParams.HPVisceraFL * ind.EnergyFromIntake);

                feedingLevel = (ind.EnergyFromIntake / ind.EnergyForMaintenance) - 1;

                if (recalculate == 0)
                {
                    if(!ind.Intake.AdjustIntakeByRumenProteinRequired(CalculateCrudeProtein(ind, feedingLevel)))
                        break;
                }
                recalculate++;
            }
            while (recalculate < 2);
        }

        private double CalculateCrudeProtein(Ruminant ind, double feedingLevel)
        {
            // 1. calc RDP intake. Rumen Degradable Protein

            FoodResourceStore forage = ind.Intake.GetStore(FeedType.Forage);
            FoodResourceStore concentrate = ind.Intake.GetStore(FeedType.Concentrate);

            if (feedingLevel > 0)
            {
                if(forage != null)
                    ind.Intake.ReduceDegradableProtein(FeedType.Forage, (1 - (ind.BreedParams.RumenDegradabilityIntercept - ind.BreedParams.RumenDegradabilitySlope * forage.Details?.DryMatterDigestibility??0) * feedingLevel));
                if (concentrate != null)
                    ind.Intake.ReduceDegradableProtein(FeedType.Concentrate, (1 - ind.BreedParams.RumenDegradabilityConcentrateSlope * feedingLevel)); // Eq.(50)
            }
            double RDPIntake = forage?.DegradableCrudeProtein??0 + concentrate?.DegradableCrudeProtein??0; //Eq.(50)

            // 2. calc UDP intake by difference(CPI-RDPI)

            double UDPIntake = ind.Intake.CrudeProtein - RDPIntake;

            // 3.calculate RDP requirement

            // ignored GrassGro timeOfYearFactor 19/9/2023 as calculation showed it has very little effect compared with the error in parameterisation and tracking of feed quality and a monthly timestep
            // double timeOfYearFactorRDPR = 1 + rumenDegradableProteinTimeOfYear * latitude / 40 * Math.Sin(2 * Math.PI * dayOfYear / 365); //Eq.(52)
            double fermentableSupplementMEI = (13.3 * concentrate.Details?.DryMatterDigestibility??0 + 1.32) * concentrate?.Details.Amount??0; //Eq.(32) - (Ether extract set to zero)

            double RDPReq = (ind.BreedParams.RumenDegradableProteinIntercept + ind.BreedParams.RumenDegradableProteinSlope * (1 - Math.Exp(-ind.BreedParams.RumenDegradableProteinExponent *
                (feedingLevel + 1)))) * (forage?.Details.EnergyContent??0 + fermentableSupplementMEI);

            if(RDPReq > RDPIntake)
            {
                return (RDPIntake / RDPReq) * ind.BreedParams.RumenDegradableProteinShortfallScalar;
            }
            return 1;
        }

        /// <summary>
        /// Determine the energy required for wool growth
        /// </summary>
        /// <param name="ind">Ruminant individual</param>
        /// <returns>Daily energy required for wool production time step</returns>
        private double CalculateWoolEnergy(Ruminant ind)
        {
            // TODO: wool production energy here!
            // TODO: include wool production here.

            // grow wool and cashmere
            // TODO: move to the Calculate Wool Energy method
            //ind.Wool += ind.BreedParams.WoolCoefficient * ind.MetabolicIntake;
            //ind.Cashmere += ind.BreedParams.CashmereCoefficient * ind.MetabolicIntake;

            return 0;
        }

        /// <summary>
        /// Determine the energy required for lactation
        /// </summary>
        /// <param name="ind">Female individual</param>
        /// <returns>Daily energy required for lactation this time step</returns>
        private double CalculateLactationEnergy(RuminantFemale ind, bool updateValues)
        {
            if (ind.IsLactating | MathUtilities.IsPositive(ind.MilkProductionPotential))
            {
                ind.MilkMilkedThisTimeStep = 0;
                ind.MilkSuckledThisTimeStep = 0;

                // this is called in potential intake using last months energy available after pregnancy
                // and called again in CalculateEnergy of Weight Gain where it uses the energy available after ____ maint? from the current time step.

                // update old parameters in breed params to new approach based on energy and not L milk.
                // TODO: new intercept = 0.4 and coefficient = 0.02
                // TODO: update peak yield.
                double kl = ind.BreedParams.ELactationEfficiencyCoefficient * ind.Intake.ME + ind.BreedParams.ELactationEfficiencyIntercept;
                double milkTime = ind.DaysLactating; // assumes mid month

                // determine milk production curve to use
                double milkCurve = ind.BreedParams.MilkCurveSuckling;
                // if milking is taking place use the non-suckling curve for duration of lactation
                // otherwise use the suckling curve where there is a larger drop off in milk production
                if (ind.SucklingOffspringList.Any() == false)
                    milkCurve = ind.BreedParams.MilkCurveNonSuckling;

                // calculate milk production (eqns 66 thru 76)

                // DR (Eq. 74) => ind.ProportionMilkProductionAchieved, calculated after lactation energy determined in energy method.

                double LR = ind.BreedParams.PotentialYieldReduction * ind.ProportionMilkProductionAchieved + (1 - ind.BreedParams.PotentialYieldReduction) * ind.MilkLag; // Eq.(73) 
                double nutritionAfterPeakLactationFactor = 0;
                if (milkTime > 0.7 * ind.BreedParams.MilkPeakDay)
                    nutritionAfterPeakLactationFactor = ind.NutritionAfterPeakLactationFactor - ind.BreedParams.PotentialYieldReduction2 * (LR - ind.ProportionMilkProductionAchieved);  // Eq.(72) 
                else
                    nutritionAfterPeakLactationFactor = 1;

                double Mm = (milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay;

                double milkProductionMax = ind.BreedParams.PeakYieldScalar * Math.Pow(ind.StandardReferenceWeight, 0.75) * ind.RelativeSize * ind.RelativeConditionAtParturition * nutritionAfterPeakLactationFactor *
                    Math.Pow(Mm, milkCurve) * Math.Exp(milkCurve * (1 - Mm));

                if(updateValues)
                {
                    ind.ProportionMilkProductionAchieved = ind.MilkProducedThisTimeStep / ind.MilkProductionPotential;
                    ind.MilkLag = LR;
                    ind.NutritionAfterPeakLactationFactor = nutritionAfterPeakLactationFactor;
                }

                double ratioMilkProductionME = ((ind.EnergyAfterPregnancy) * 0.94 * kl)/milkProductionMax; // Eq. 68

                // LactationEnergyDeficit = 1.17
                // PotentialYieldParameter = 1.6
                // potentialYieldMEIEffect = 4
                // potentialYieldLactationEffect = 0.004 cattle / 0.008 sheep
                // potentialYieldLactationEffect2 = 0.006 cattle / 0.012 sheep
                // MilkConsumptionLimit1 = 0.42/0.3
                // MilkConsumptionLimit2 = 0.58/0.41
                // MilkConsumptionLimit3 = 0.036/0.071

                double ad = Math.Max(milkTime, ratioMilkProductionME / (2 * ind.BreedParams.PotentialYieldLactationEffect2));

                double MP1 = (ind.BreedParams.LactationEnergyDeficit * milkProductionMax) / (1 + Math.Exp(-(-ind.BreedParams.PotentialLactationYieldParameter + ind.BreedParams.PotentialYieldMEIEffect *
                    ratioMilkProductionME + ind.BreedParams.PotentialYieldLactationEffect * ad * (ratioMilkProductionME - ind.BreedParams.PotentialYieldLactationEffect2 * ad) - ind.BreedParams.PotentialYieldConditionEffect
                    * ind.RelativeCondition * (ratioMilkProductionME - ind.BreedParams.PotentialYieldConditionEffect2 * ind.RelativeCondition))));

                double MP2 = Math.Min(MP1, ind.SucklingOffspringList.Count() * ind.BreedParams.EnergyContentMilk * Math.Pow(ind.SucklingOffspringList.Average(a => a.Weight), 0.75) * (ind.BreedParams.MilkConsumptionLimit1 + ind.BreedParams.MilkConsumptionLimit2 * Math.Exp(-ind.BreedParams.MilkConsumptionLimit3 * milkTime)));

                // 0.032
                double proteinMilk = ind.BreedParams.ProteinContentMilk * MP2 / ind.BreedParams.EnergyContentMilk;

                ind.MilkCurrentlyAvailable = MP2 * 30.4;
                ind.MilkProducedThisTimeStep = ind.MilkCurrentlyAvailable;

                // returns the energy required for milk production
                return MP2 / 0.94 * kl;
            }
            return 0;
        }

        /// <summary>
        /// Determine the energy required for pregnancy
        /// </summary>
        /// <param name="ind">Female individual</param>
        /// <param name="conceptusProtein">Protein required by conceptus (kg)</param>
        /// <param name="conceptusFat">Fat required by conceptus (kg)</param>
        /// <returns>energy required for fetus this timestep</returns>
        private double CalculatePregnancyEnergy(RuminantFemale ind, ref double conceptusProtein, ref double conceptusFat)
        {
            if (!ind.IsPregnant)
            {
                conceptusProtein = 0;
                conceptusFat = 0;
                return 0;
            }

            double normalWeightFetus = ind.ScaledBirthWeight * Math.Exp(ind.BreedParams.FetalNormWeightParameter * (1 - Math.Exp(ind.BreedParams.FetalNormWeightParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength))));

            //ToDo: Fix fetus weight parameter
            double conceptusWeight = ind.NumberOfOffspring * (ind.BreedParams.ConceptusWeightRatio * ind.ScaledBirthWeight * Math.Exp(ind.BreedParams.ConceptusWeightParameter * (1 - Math.Exp(ind.BreedParams.ConceptusWeightParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength))))      );// + (fetusWeight - normalWeightFetus));
            double relativeConditionFoet = double.NaN;

            // MJ per day
            double conceptusME = (ind.BreedParams.ConceptusEnergyContent * (ind.NumberOfOffspring * ind.BreedParams.ConceptusWeightRatio * ind.ScaledBirthWeight) * relativeConditionFoet *
                (ind.BreedParams.ConceptusEnergyParameter * ind.BreedParams.ConceptusEnergyParameter2 / (ind.BreedParams.GestationLength * 30.4)) * 
                Math.Exp(ind.BreedParams.ConceptusEnergyParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength) + ind.BreedParams.ConceptusEnergyParameter * (1 - Math.Exp(ind.BreedParams.ConceptusEnergyParameter2 * (1 - ind.DaysPregnant/ ind.BreedParams.GestationLength))))) / 0.13;

            // kg protein per day
            double conceptusProteinReq = ind.BreedParams.ConceptusProteinContent * (ind.NumberOfOffspring * ind.BreedParams.ConceptusWeightRatio * ind.ScaledBirthWeight) * relativeConditionFoet * 
                (ind.BreedParams.ConceptusProteinParameter * ind.BreedParams.ConceptusProteinParameter2 / (ind.BreedParams.GestationLength * 30.4)) * Math.Exp(ind.BreedParams.ConceptusProteinParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength) + ind.BreedParams.ConceptusProteinParameter * (1 - Math.Exp(ind.BreedParams.ConceptusProteinParameter2 * (1 - ind.DaysPregnant / ind.BreedParams.GestationLength))));

            //ind.Weight / ind.HighWeightWhenNotPregnant

            conceptusProtein = conceptusProteinReq * conceptusWeight;
            conceptusFat = (conceptusME - (conceptusProtein * 23.6)) / 39.3;

            return conceptusME * 30.4;
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
                    manureStore.AddUncollectedManure(groupInds.Key ?? "", groupInds.Sum(a => a.Intake.Feed.Actual * (100 - a.Intake.DMD) / 100));
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
            IEnumerable<Ruminant> died = herd.Where(a => a.FatMass <= 0 || a.ProteinMass <= 0);
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
