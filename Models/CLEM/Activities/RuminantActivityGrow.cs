
using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;
using Models.CLEM.Reporting;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity</summary>
    /// <summary>This activity determines potential intake for the Feeding activities and feeding arbitrator for all ruminants</summary>
    /// <summary>See Breed activity for births, suckling mortality etc</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs growth and aging of all ruminants. Only one instance of this activity is permitted")]
    [Version(1, 0, 4, "Mortality moved from all grow activities")]
    [Version(1, 0, 3, "Allows selection of methane store for emissions")]
    [Version(1, 0, 2, "Improved reporting of milk status")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrow.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow), typeof(RuminantParametersGeneral), typeof(RuminantParametersLactation), typeof(RuminantParametersMethaneCharmley) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType },
        SingleInstance = true)]
    public class RuminantActivityGrow : CLEMActivityBase, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private GreenhouseGasesType methaneEmissions;
        private ProductStoreTypeManure manureStore;
        private RuminantHerd ruminantHerd;
        private ConceptionStatusChangedEventArgs conceptionArgs = new ();

        /// <summary>
        /// Gross energy content of forage (MJ/kg DM)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(18.4)]
        [Description("Gross energy content of forage (MJ/kg digestible DM)")]
        [Required]
        [Units("MJ/kg DM")]
        public double EnergyGross { get; set; }

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
        public RuminantActivityGrow()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
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
            // grow all individuals
            foreach (Ruminant ind in ruminantHerd.Herd)
                ind.SetCurrentDate(events.Clock.Today);

            List<Ruminant> herd = ruminantHerd.Herd;

            // Natural weaning takes place here before animals eat or take milk from mother.
            foreach (var ind in herd.Where(a => a.Weaned == false))
            {
                int weaningAge = ind.Parameters.General.NaturalWeaningAge.InDays;
                if (weaningAge == 0)
                    weaningAge = ind.Parameters.General.GestationLength.InDays;

                if (ind.AgeInDays >= weaningAge)
                {
                    ind.Wean(true, "Natural", events.Clock.Today);

                    // report wean. If mother has died create temp female with the mother's ID for reporting only
                    conceptionArgs.Update(ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.Parameters, events.Clock.Today, -1, ind.Parameters.General.BirthScalar[0], 999) { ID = ind.MotherID }, events.Clock.Today, ind);
                    ind.BreedDetails.OnConceptionStatusChanged(conceptionArgs);
                }
            }
        }

        /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPotentialIntake")]
        private void OnCLEMPotentialIntake(object sender, EventArgs e)
        {
            List<Ruminant> herd = ruminantHerd.Herd;

            // Calculate potential intake and reset stores
            // Order age descending so breeder females calculate milkproduction before suckings grow

            foreach (var ind in herd.GroupBy(a => a.IsSucklingWithMother).OrderBy(a => a.Key))
            {
                foreach (var indi in ind)
                {
                    // reset tallies at start of the month
                    indi.Intake.Reset();
                    indi.Energy.Reset();
                    CalculatePotentialIntake(indi);
                }
            }
        }

        private void CalculatePotentialIntake(Ruminant ind)
        {
            // calculate daily potential intake for the selected individual/cohort
            double standardReferenceWeight = ind.Weight.StandardReferenceWeight;

            double liveWeightForIntake = ind.Weight.NormalisedForAge;
            // now performed at allocation of weight in Ruminant
            if (MathUtilities.IsLessThan(ind.Weight.HighestAttained, ind.Weight.NormalisedForAge))
                liveWeightForIntake = ind.Weight.HighestAttained;

            // Calculate potential intake based on current weight compared to SRW and previous highest weight
            double potentialIntake = 0;
            ind.Intake.MilkDaily.Expected = 0;

            // calculate milk intake shortfall for sucklings
            // all in units per day and multiplied at end of this section
            if (!ind.Weaned)
            {
                // potential milk intake/animal/day
                ind.Intake.MilkDaily.Expected = ind.Parameters.Grow.MilkIntakeIntercept + ind.Parameters.Grow.MilkIntakeCoefficient * ind.Weight.Live;

                // get estimated milk available
                // this will be updated to the corrected milk available in the calculate energy section.
                ind.Intake.MilkDaily.Received = Math.Min(ind.Intake.MilkDaily.Expected, ind.MothersMilkProductionAvailable);

                // if milk supply low, suckling will subsitute forage up to a specified % of bodyweight (R_C60)
                if (MathUtilities.IsLessThan(ind.Intake.MilkDaily.Received, ind.Weight.Live * ind.Parameters.Grow.MilkLWTFodderSubstitutionProportion))
                    potentialIntake = Math.Max(0.0, ind.Weight.Live * ind.Parameters.Grow.MaxJuvenileIntake - ind.Intake.MilkDaily.Received * ind.Parameters.Grow.ProportionalDiscountDueToMilk);

                ind.Intake.MilkDaily.Received *= events.Interval;
                //ind.Intake.Milk.Expected *= events.Interval;
            }
            else
            {
                if (ind.IsWeaner)
                {
                    // Reference: SCA Metabolic LWTs
                    // restored in v112 of NABSA for weaner animals
                    potentialIntake = ind.Parameters.Grow.IntakeCoefficient * standardReferenceWeight * (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)) * (ind.Parameters.Grow.IntakeIntercept - (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)));
                }
                else // 12month+ weaned individuals
                {
                    // Reference: SCA based actual LWTs
                    potentialIntake = ind.Parameters.Grow.IntakeCoefficient * liveWeightForIntake * (ind.Parameters.Grow.IntakeIntercept - liveWeightForIntake / standardReferenceWeight);
                }

                if (ind.Sex == Sex.Female)
                {
                    RuminantFemale femaleind = ind as RuminantFemale;
                    // Increase potential intake for lactating breeder
                    if (femaleind.IsLactating)
                    {
                        // move to half way through timestep
                        double dayOfLactation = femaleind.DaysLactating(events.Interval/2.0);
                        // Reference: Intake multiplier for lactating cow (M.Freer)
                        // double intakeMilkMultiplier = 1 + 0.57 * Math.Pow((dayOfLactation / 81.0), 0.7) * Math.Exp(0.7 * (1 - (dayOfLactation / 81.0)));
                        double intakeMilkMultiplier = 1 + ind.Parameters.Grow.LactatingPotentialModifierConstantA * Math.Pow((dayOfLactation / ind.Parameters.Grow.LactatingPotentialModifierConstantB), ind.Parameters.Grow.LactatingPotentialModifierConstantC) * Math.Exp(ind.Parameters.Grow.LactatingPotentialModifierConstantC * (1 - (dayOfLactation / ind.Parameters.Grow.LactatingPotentialModifierConstantB))) * (1 - 0.5 + 0.5 * (ind.Weight.Live / ind.Weight.NormalisedForAge));

                        // To make this flexible for sheep and goats, added three new Ruminant Coeffs
                        // Feeding standard values for Beef, Dairy suck, Dairy non-suck and sheep are:
                        // For 0.57 (A) use .42, .58, .85 and .69; for 0.7 (B) use 1.7, 0.7, 0.7 and 1.4, for 81 (C) use 62, 81, 81, 28
                        // added LactatingPotentialModifierConstantA, LactatingPotentialModifierConstantB and LactatingPotentialModifierConstantC
                        // replaces (A), (B) and (C)
                        potentialIntake *= intakeMilkMultiplier;

                        // calculate estimated milk production for time step here
                        // assuming average feed quality if no previous diet values
                        // This need to happen before suckling potential intake can be determined.
                        CalculateMilkProduction(femaleind);
                        femaleind.Milk.Produced = femaleind.Milk.Available;
                    }
                    else
                    {
                        femaleind.Milk.ProductionRate = 0;
                        femaleind.Milk.PotentialRate = 0;
                        femaleind.Milk.Available = 0;
                        femaleind.Milk.Milked = 0;
                        femaleind.Milk.Suckled = 0;
                        femaleind.Milk.Produced = 0;
                    }
                }

                //TODO: option to restrict potential further due to stress (e.g. heat, cold, rain)

            }
            // get monthly intake
            potentialIntake *= events.Interval;
            ind.Intake.SolidsDaily.MaximumExpected = potentialIntake;
            ind.Intake.SolidsDaily.Expected = potentialIntake;
        }

        /// <summary>
        /// Set the milk production of the selected female given diet drymatter digesibility
        /// </summary>
        /// <param name="ind">Female individual</param>
        /// <returns>energy of milk</returns>
        private double CalculateMilkProduction(RuminantFemale ind)
        {
            ind.Milk.Milked = 0;
            ind.Milk.Suckled = 0;

            double energyMetabolic = EnergyGross * (((ind.Intake.DMD == 0) ? 50 : ind.Intake.DMD) / 100.0) * 0.81;
            // Reference: SCA p.
            double kl = ind.Parameters.Grow.ELactationEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.Parameters.Grow.ELactationEfficiencyIntercept;
            double milkTime = ind.DaysLactating(events.Interval / 2.0);
            double milkCurve;
            // determine milk production curve to use
            // if milking is taking place use the non-suckling curve for duration of lactation
            // otherwise use the suckling curve where there is a larger drop off in milk production
            if (ind.SucklingOffspringList.Count == 0)
                milkCurve = ind.Parameters.Lactation.MilkCurveNonSuckling;
            else // no milking
                milkCurve = ind.Parameters.Lactation.MilkCurveSuckling;
            ind.Milk.PotentialRate = ind.Parameters.Lactation.MilkPeakYield * ind.Weight.Live / ind.Weight.NormalisedForAge * (Math.Pow(((milkTime + ind.Parameters.Lactation.MilkOffsetDay) / ind.Parameters.Lactation.MilkPeakDay), milkCurve)) * Math.Exp(milkCurve * (1 - (milkTime + ind.Parameters.Lactation.MilkOffsetDay) / ind.Parameters.Lactation.MilkPeakDay));
            ind.Milk.PotentialRate = Math.Max(ind.Milk.PotentialRate, 0.0);
            // Reference: Potential milk prodn, 3.2 MJ/kg milk - Jouven et al 2008
            double energyMilk = ind.Milk.PotentialRate * 3.2 / kl;
            // adjust last time step's energy balance
            double adjustedEnergyBalance = ind.Energy.AfterLactation;
            if (adjustedEnergyBalance < (-0.5936 / 0.322 * energyMilk))
                adjustedEnergyBalance = (-0.5936 / 0.322 * energyMilk);

            // set milk production in lactating females for consumption.
            // ToDo: can th adjustment be >1 or doe this need to be constrained to the previouisl cal of milk production.
            // Math.Min(ind.MilkProductionPotential, Math.Max(0.0, ind.MilkProductionPotential * (0.5936 + 0.322 * adjustedEnergyBalance / energyMilk)));
            ind.Milk.ProductionRate = Math.Min(ind.Milk.PotentialRate, Math.Max(0.0, ind.Milk.PotentialRate * (0.5936 + 0.322 * adjustedEnergyBalance / energyMilk)));
            ind.Milk.Available = ind.Milk.ProductionRate * events.Interval;

            // returns the energy required for milk production

            return ind.Milk.ProductionRate * 3.2 / kl;
        }

        /// <summary>Function to calculate growth of herd for the monthly timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            List<Ruminant> herd = ruminantHerd.Herd;

            int cmonth = events.Clock.Today.Month;

            // grow individuals

            IEnumerable<string> breeds = herd.Select(a => a.Breed).Distinct();
            Status = ActivityStatus.NotNeeded;

            foreach (string breed in breeds)
            {
                int unfed = 0;
                int unfedcalves = 0;
                double totalMethane = 0;
                foreach (Ruminant ind in herd.Where(a => a.Breed == breed).OrderByDescending(a => a.AgeInDays))
                {
                    ind.MetabolicIntake = ind.Intake.SolidsDaily.Actual;
                    Status = ActivityStatus.Success;
                    if (ind.Weaned)
                    {
                        // check that they had some food
                        if (ind.Intake.SolidsDaily.Actual == 0)
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
                        
                        double crudeProteinRequired = 0.0;
                        //double crudeProteinRequired = ind.Parameters.Grow.ProteinCoefficient / 100.0 * ind.Intake.DMD / 100 * ind.Intake.SolidsDaily.ActualForTimeStep(events.Interval);
                        //double crudeProteinRequired = ind.Parameters.Grow.ProteinCoefficient * ind.Intake.DMD / 100;

                        // adjust for efficiency of use of protein, (default 90%) degradable. now user param.
                        double crudeProteinSupply = (ind.Intake.CrudeProtein) * ind.Parameters.Grow.ProteinDegradability; //  PercentNOfIntake * 62.5
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
                        ind.Intake.SolidsDaily.Received = Math.Min(ind.Intake.SolidsDaily.Received, ind.Intake.SolidsDaily.Expected);
                        // when discarding intake can we be specific and hang onto N?
                        ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake.SolidsDaily.Received);
                    }
                    else
                    {
                        // for calves
                        // these individuals have access to milk or are separated from mother and must survive on calf calculated pasture intake

                        // if potential intake = 0 they have not needed to consume pasture and intake will be zero.
                        if (MathUtilities.IsGreaterThanOrEqual(ind.Intake.SolidsDaily.Expected, 0.0))
                        {
                            ind.Intake.SolidsDaily.Received = Math.Min(ind.Intake.SolidsDaily.Received, ind.Intake.SolidsDaily.Expected);
                            ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake.SolidsDaily.Received);
                        }

                        // no potential * 1.2 as potential has been fixed based on suckling individuals.

                        if (MathUtilities.IsLessThanOrEqual(ind.Intake.MilkDaily.Actual + ind.Intake.SolidsDaily.Received, 0))
                            unfedcalves++;
                    }

                    // TODO: nabsa adjusts potential intake for digestability of fodder here.
                    // This is now done in RuminantActivityGrazePasture

                    // calculate energy
                    CalculateEnergy(ind, out double methane);

                    // Sum and produce one event for breed at end of loop
                    totalMethane += methane;

                    // grow wool and cashmere
                    ind.Weight.Wool.Adjust(ind.Parameters.Grow.WoolCoefficient * ind.MetabolicIntake);
                    //ind.CashmereWeight += ind.Parameters.Grow.CashmereCoefficient * ind.MetabolicIntake;
                }

                // alert user to unfed animals in the month as this should not happen
                if (unfed > 0)
                {
                    string warn = $"individuals of [r={breed}] not fed";
                    string warnfull = $"Some individuals of [r={breed}] were not fed in some months (e.g. [{unfed}] individuals in [{events.Clock.Today.Month}/{events.Clock.Today.Year}])\r\nFix: Check feeding strategy and ensure animals are moved to pasture or fed in yards";
                    Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
                }
                if (unfedcalves > 0)
                {
                    string warn = $"calves of [r={breed}] not fed";
                    string warnfull = $"Some calves of [r={breed}] were not fed in some months (e.g. [{unfedcalves}] individuals in [{events.Clock.Today.Month}/{events.Clock.Today.Year}])\r\nFix: Check calves are are fed, or have access to pasture (moved with mothers or separately) when no milk is available from mother";
                    Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
                }

                // g per day -> total kg
                methaneEmissions?.Add(totalMethane * events.Interval / 1000, this, breed, TransactionCategory);
            }
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
                foreach (var item in ruminantHerd.Herd.GroupBy(a => a.Location))
                {
                    double manureProduced = item.Sum(a => a.Intake.SolidsDaily.Actual * ((100 - a.Intake.DMD) / 100));
                    manureStore.AddUncollectedManure(item.Key ?? "", manureProduced);
                }
            }
        }

        /// <summary>
        /// Function to calculate energy from intake and subsequent growth
        /// </summary>
        /// <param name="ind">Ruminant individual class</param>
        /// <param name="methaneProduced">Sets output variable to value of methane produced</param>
        /// <returns></returns>
        private void CalculateEnergy(Ruminant ind, out double methaneProduced)
        {
            // all energy calculations are per day and multiplied at end to give monthly weight gain

            // ind.MetabolicIntake is the inake received adjusted by any crude protein shortfall in AnimalWeightGain()
            double intakeDaily = ind.MetabolicIntake / events.Interval;

            // Sme 1 for females and castrates
            double sme = 1;
            // Sme 1.15 for all non-castrated males.
            if (ind.Weaned && ind.Sex == Sex.Male && (ind as RuminantMale).IsCastrated == false)
                sme = 1.15;

            double energyDiet = EnergyGross * ind.Intake.DMD / 100.0;
            // Reference: Nutrient Requirements of domesticated ruminants (p7)
            double energyMetabolic = energyDiet * 0.81;
            double energyMetabolicFromIntake = energyMetabolic * intakeDaily;

            double km = ind.Parameters.Grow.EMaintEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.Parameters.Grow.EMaintEfficiencyIntercept;
            ind.Energy.Km = km;
            // Reference: SCA p.49
            double kg = ind.Parameters.Grow.EGrowthEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.Parameters.Grow.EGrowthEfficiencyIntercept;
            ind.Energy.Kg = kg;
            double energyPredictedBodyMassChange;
            double energyMaintenance;
            if (!ind.Weaned)
            {
                // unweaned individuals are assumed to be suckling as natural weaning rate set regardless of inclusion of wean activity
                // unweaned individuals without mother or milk from mother will need to try and survive on limited pasture until weaned.

                // calculate engergy and growth from milk intake
                // recalculate milk intake based on mothers updated milk production for the time step using the previous monthly potential milk intake
                ind.Intake.MilkDaily.Received = Math.Min(ind.Intake.MilkDaily.Expected, ind.MothersMilkProductionAvailable * events.Interval);

                ind.Mother?.Milk.Take(ind.Intake.MilkDaily.Received, MilkUseReason.Suckling);
                double milkIntakeDaily = ind.Intake.MilkDaily.Received / events.Interval;

                // Below now uses actual intake received rather than assume all potential intake is eaten
                double kml = 1;
                double kgl = 1;
                if (MathUtilities.IsPositive(ind.MetabolicIntake + ind.Intake.MilkDaily.Received))
                {
                    // average energy efficiency for maintenance
                    kml = ((milkIntakeDaily * 0.7) + (intakeDaily * km)) / (milkIntakeDaily + intakeDaily);
                    // average energy efficiency for growth
                    kgl = ((milkIntakeDaily * 0.7) + (intakeDaily * kg)) / (milkIntakeDaily + intakeDaily);
                }
                double energyMilkConsumed = milkIntakeDaily * 3.2;
                // limit suckling intake of milk per day
                energyMilkConsumed = Math.Min(ind.Parameters.Grow.MilkIntakeMaximum * 3.2, energyMilkConsumed);

                energyMaintenance = (ind.Parameters.Grow.EMaintCoefficient * Math.Pow(ind.Weight.Live, 0.75) / kml) * Math.Exp(-ind.Parameters.Grow.EMaintExponent * (((ind.AgeInDays == 0) ? 0.03 : ind.AgeInDays / 30.4)));
                double EnergyBalance = energyMilkConsumed + energyMetabolicFromIntake - energyMaintenance;
                //ind.Energy.FromIntake = energyMilkConsumed + energyMetabolicFromIntake;
                ind.Energy.ForFetus = 0;
                ind.Energy.ForMaintenance = energyMaintenance;
                ind.Energy.ForLactation = 0;

                double feedingValue;
                if (MathUtilities.IsPositive(EnergyBalance))
                    feedingValue = 2 * 0.7 * EnergyBalance / (kgl * energyMaintenance) - 1;
                else
                    //(from Hirata model)
                    feedingValue = 2 * EnergyBalance / (0.85 * energyMaintenance) - 1;

                double energyEmptyBodyGain = ind.Parameters.Grow.GrowthEnergyIntercept1 + feedingValue + (ind.Parameters.Grow.GrowthEnergyIntercept2 - feedingValue) / (1 + Math.Exp(-6 * (ind.Weight.Live / ind.Weight.NormalisedForAge - 0.4)));

                energyPredictedBodyMassChange = ind.Parameters.Grow.GrowthEfficiency * 0.7 * EnergyBalance / energyEmptyBodyGain;
            }
            else
            {
                double energyMilk = 0;
                double energyFetus = 0;

                // set maintenance age to maximum of 6 years (2190 days). Now uses EnergeyMaintenanceMaximumAge
                double maintenanceAge = Math.Min(ind.AgeInDays, ind.Parameters.Grow.EnergyMaintenanceMaximumAge.InDays);
                // Reference: SCA p.24
                // Reference p19 (1.20). Does not include MEgraze or Ecold, also skips M,
                // 0.000082 is -0.03 Age in Years/365 for days
                energyMaintenance = ind.Parameters.Grow.Kme * sme * (ind.Parameters.Grow.EMaintCoefficient * Math.Pow(ind.Weight.Live, 0.75) / km) * Math.Exp(-ind.Parameters.Grow.EMaintExponent * maintenanceAge) + (ind.Parameters.Grow.EMaintIntercept * energyMetabolicFromIntake);
                //ind.EnergyMaintenance = energyMaintenance;
                double energyBalance = energyMetabolicFromIntake - energyMaintenance;

                if (ind.Sex == Sex.Female)
                {
                    RuminantFemale femaleind = ind as RuminantFemale;

                    // Determine energy required for fetal development
                    if (femaleind.IsPregnant)
                    {
                        double standardReferenceWeight = ind.Weight.StandardReferenceWeight;
                        // Potential birth weight
                        // Reference: Freer
                        double potentialBirthWeight = femaleind.CurrentBirthScalar * standardReferenceWeight * (1 - 0.33 * (1 - ind.Weight.Live / standardReferenceWeight));
                        double fetusAge = femaleind.DaysSince(RuminantTimeSpanTypes.Conceived, 0.0);

                        //TODO: Check fetus age correct
                        energyFetus = potentialBirthWeight * 349.16 * 0.000058 * Math.Exp(345.67 - 0.000058 * fetusAge - 349.16 * Math.Exp(-0.000058 * fetusAge)) / 0.13;
                        energyBalance -= energyFetus;
                    }

                    // calculate energy for lactation
                    // look for milk production calculated before offspring may have been weaned

                    if (femaleind.IsLactating | MathUtilities.IsPositive(femaleind.Milk.PotentialRate))
                    {
                        // recalculate milk production based on DMD of food provided
                        energyMilk = CalculateMilkProduction(femaleind);
                        energyBalance -= energyMilk;
                        // reset this. It was previously determined in potential intake as a measure of milk available. This is now the correct calculation
                        femaleind.Milk.Produced = femaleind.Milk.Available;
                    }
                }

                //TODO: add draft individual energy requirement

                double feedingValue;
                //ind.EnergyFromIntake = energyMetabolicFromIntake;
                ind.Energy.ForFetus = energyFetus;
                ind.Energy.ForMaintenance = energyMaintenance;
                ind.Energy.ForLactation = energyMilk;

                // Reference: Feeding_value = Adjustment for rate of loss or gain (SCA p.43, ? different from Hirata model)
                if (MathUtilities.IsPositive(energyBalance))
                    feedingValue = 2 * ((kg * energyBalance) / (km * energyMaintenance) - 1);
                else
                    feedingValue = 2 * (energyBalance / (0.8 * energyMaintenance) - 1);  //(from Hirata model)

                double weightToReferenceRatio = Math.Min(1.0, ind.Weight.Live / ind.Weight.StandardReferenceWeight);

                // Reference:  MJ of Energy required per kg Empty body gain (SCA p.43)
                double energyEmptyBodyGain = ind.Parameters.Grow.GrowthEnergyIntercept1 + feedingValue + (ind.Parameters.Grow.GrowthEnergyIntercept2 - feedingValue) / (1 + Math.Exp(-6 * (weightToReferenceRatio - 0.4)));
                // Determine Empty body change from Eebg and Ebal, and increase by 9% for LW change
                if (MathUtilities.IsPositive(energyBalance))
                    energyPredictedBodyMassChange = ind.Parameters.Grow.GrowthEfficiency * kg * energyBalance / energyEmptyBodyGain;
                else
                    // Reference: from Hirata model
                    energyPredictedBodyMassChange = ind.Parameters.Grow.GrowthEfficiency * km * energyBalance / (0.8 * energyEmptyBodyGain);
            }
            energyPredictedBodyMassChange *= events.Interval;  // Convert to monthly

            //ind.PreviousWeight = ind.Weight;

            //double newWt = Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange);
            //double maxwt = ind.Weight.StandardReferenceWeight * ind.BreedParams.MaximumSizeOfIndividual;
            //newWt = Math.Min(newWt, maxwt);
            
            //ind.Weight = Math.Min(
            //    Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange),
            //    ind.Weight.StandardReferenceWeight * ind.Parameters.Grow.MaximumSizeOfIndividual
            //    );

            ind.Weight.Adjust(energyPredictedBodyMassChange / ind.Parameters.Grow.GrowthEfficiency, energyPredictedBodyMassChange, ind);

            // Function to calculate approximate methane produced by animal, based on feed intake
            // Function based on Freer spreadsheet
            // methane is  0.02 * intakeDaily * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
            // methane is methaneProduced / 55.28 * 1000; // grams per day

            // Charmley et al 2016 can be substituted by intercept = 0 and coefficient = 20.7
            methaneProduced = ind.Parameters.EntericMethaneCharmley.MethaneProductionCoefficient * intakeDaily;
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
        }

        ///// <summary>Function to determine which animlas have died and remove from the population</summary>
        ///// <param name="sender">The sender.</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //[EventSubscribe("CLEMAnimalDeath")]
        //private void OnCLEMAnimalDeath(object sender, EventArgs e)
        //{
        //    // remove individuals that died
        //    // currently performed in the month after weight has been adjusted
        //    // and before breeding, trading, culling etc (See Clock event order)

        //    // Calculated by
        //    // critical weight OR
        //    // relative condition
        //    // Body condition score and additional mortality rate &&
        //    // juvenile (unweaned) death based on mothers weight &&
        //    // adult weight adjusted base mortality.

        //    List<Ruminant> herd = ruminantHerd.Herd;

        //    // weight based mortality
        //    List<Ruminant> died = new();

        //    died = herd.Where(a => a.Weight.Live == 0).ToList();
        //    if (died.Any())
        //    {
        //        foreach (Ruminant ind in died)
        //        {
        //            ind.Died = true;
        //            ind.SaleFlag = HerdChangeReason.DiedUnderweight;
        //        }
        //    }

        //    died = new();
        //    if (herd.Any())
        //    {
        //        switch (herd.FirstOrDefault().Parameters.Grow.ConditionBasedMortalityStyle)
        //        {
        //            case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
        //                died = herd.Where(a => MathUtilities.IsLessThanOrEqual(a.Weight.Live, a.Weight.HighestAttained * a.Parameters.Grow.ConditionBasedMortalityCutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), a.Parameters.Grow.ConditionBasedMortalityProbability)).ToList();
        //                break;
        //            case ConditionBasedCalculationStyle.RelativeCondition:
        //                died = herd.Where(a => MathUtilities.IsLessThanOrEqual(a.Weight.RelativeCondition, a.Parameters.Grow.ConditionBasedMortalityCutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), a.Parameters.Grow.ConditionBasedMortalityProbability)).ToList();
        //                break;
        //            case ConditionBasedCalculationStyle.BodyConditionScore:
        //                died = herd.Where(a => MathUtilities.IsLessThanOrEqual(a.BodyConditionScore, a.Parameters.Grow.ConditionBasedMortalityCutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), a.Parameters.Grow.ConditionBasedMortalityProbability)).ToList();
        //                break;
        //            case ConditionBasedCalculationStyle.None:
        //                break;
        //            default:
        //                break;
        //        }

        //        if (died.Any())
        //        {
        //            foreach (Ruminant ind in died)
        //            {
        //                ind.Died = true;
        //                ind.SaleFlag = HerdChangeReason.DiedUnderweight;
        //            }
        //        }
        //    }

        //    // base mortality adjusted for condition
        //    foreach (var ind in ruminantHerd.Herd.Where(a => !a.Died))
        //    {
        //        double mortalityRate = 0;
        //        if (!ind.Weaned)
        //        {
        //            mortalityRate = 0;
        //            // ToDo: see if we can remove the breed parameter from grow below
        //            if (ind.Mother == null || MathUtilities.IsLessThan(ind.Mother.Weight.Live, ind.Parameters.Breeding.CriticalCowWeight * ind.Weight.StandardReferenceWeight))
        //                // if no mother assigned or mother's weight is < CriticalCowWeight * SFR
        //                mortalityRate = ind.Parameters.Grow.JuvenileMortalityMaximum;
        //            else
        //                // if mother's weight >= criticalCowWeight * SFR
        //                mortalityRate = Math.Exp(-Math.Pow(ind.Parameters.Grow.JuvenileMortalityCoefficient * (ind.Mother.Weight.Live / ind.Mother.Weight.NormalisedForAge), ind.Parameters.Grow.JuvenileMortalityExponent));

        //            mortalityRate += ind.Parameters.Grow.MortalityBase;
        //            mortalityRate = Math.Min(mortalityRate, ind.Parameters.Grow.JuvenileMortalityMaximum);
        //        }
        //        else
        //            mortalityRate = 1 - (1 - ind.Parameters.Grow.MortalityBase) * (1 - Math.Exp(Math.Pow(-(ind.Parameters.Grow.MortalityCoefficient * (ind.Weight.Live / ind.Weight.NormalisedForAge - ind.Parameters.Grow.MortalityIntercept)), ind.Parameters.Grow.MortalityExponent)));

        //        // convert mortality from annual (calculated) to monthly (applied).
        //        if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), mortalityRate / 12))
        //        { 
        //            ind.Died = true;
        //            ind.SaleFlag = HerdChangeReason.DiedMortality;
        //        }
        //    }

        //    //died = herd.Where(a => a.Died).Select(a => { a.SaleFlag = HerdChangeReason.DiedMortality; return a; }).ToList();


        //    //            Original before CLEM events


        //    //using Models.Core;
        //    //            using Models.CLEM.Resources;
        //    //            using System;
        //    //            using System.Collections.Generic;
        //    //            using System.Linq;
        //    //            using Newtonsoft.Json;
        //    //            using Models.Core.Attributes;
        //    //            using System.IO;
        //    //            using APSIM.Shared.Utilities;

        //    //namespace Models.CLEM.Activities
        //    //    {
        //    //        /// <summary>Ruminant growth activity</summary>
        //    //        /// <summary>This activity determines potential intake for the Feeding activities and feeding arbitrator for all ruminants</summary>
        //    //        /// <summary>This activity includes deaths</summary>
        //    //        /// <summary>See Breed activity for births, suckling mortality etc</summary>
        //    //        /// <version>1.1</version>
        //    //        /// <updates>First implementation of this activity using IAT/NABSA processes</updates>
        //    //        [Serializable]
        //    //        [ViewName("UserInterface.Views.PropertyView")]
        //    //        [PresenterName("UserInterface.Presenters.PropertyPresenter")]
        //    //        [ValidParent(ParentType = typeof(CLEMActivityBase))]
        //    //        [ValidParent(ParentType = typeof(ActivitiesHolder))]
        //    //        [ValidParent(ParentType = typeof(ActivityFolder))]
        //    //        [Description("Performs growth and aging of all ruminants. Only one instance of this activity is permitted")]
        //    //        [Version(1, 0, 3, "Allows selection of methane store for emissions")]
        //    //        [Version(1, 0, 2, "Improved reporting of milk status")]
        //    //        [Version(1, 0, 1, "")]
        //    //        [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrow.htm")]
        //    //        public class RuminantActivityGrow : CLEMActivityBase
        //    //        {
        //    //            [Link]
        //    //            private Clock clock = null;

        //    //            private GreenhouseGasesType methaneEmissions;
        //    //            private ProductStoreTypeManure manureStore;
        //    //            private RuminantHerd ruminantHerd;

        //    //            /// <summary>
        //    //            /// Methane store for emissions
        //    //            /// </summary>
        //    //            [Description("Greenhouse gas store for methane emissions")]
        //    //            [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Use store named Metane if present", typeof(GreenhouseGases) } })]
        //    //            [System.ComponentModel.DefaultValue("Use store named Methane if present")]
        //    //            public string MethaneStoreName { get; set; }

        //    //            /// <summary>
        //    //            /// Gross Energy of feed (Style used in IAT/NABSA) hrowth section only
        //    //            /// </summary>
        //    //            public double EnergyGross { get; set; } = 19.6;

        //    //            /// <summary>
        //    //            /// Perform Activity with partial resources available
        //    //            /// </summary>
        //    //            [JsonIgnore]
        //    //            public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        //    //            /// <summary>
        //    //            /// Constructor
        //    //            /// </summary>
        //    //            public RuminantActivityGrow()
        //    //            {
        //    //                this.SetDefaults();
        //    //            }

        //    //            /// <summary>An event handler to allow us to initialise ourselves.</summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMInitialiseActivity")]
        //    //            private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        //    //            {
        //    //                if (MethaneStoreName is null || MethaneStoreName == "Use store named Methane if present")
        //    //                    methaneEmissions = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        //    //                else
        //    //                    methaneEmissions = Resources.FindResourceType<GreenhouseGases, GreenhouseGasesType>(this, MethaneStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        //    //                manureStore = Resources.FindResourceType<ProductStore, ProductStoreTypeManure>(this, "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        //    //                ruminantHerd = Resources.FindResourceGroup<RuminantHerd>();
        //    //            }

        //    //            /// <summary>Function to determine naturally wean individuals at start of timestep</summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMStartOfTimeStep")]
        //    //            private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        //    //            {
        //    //                List<Ruminant> herd = ruminantHerd.Herd;

        //    //                // Natural weaning takes place here before animals eat or take milk from mother.
        //    //                foreach (var ind in herd.Where(a => a.Weaned == false))
        //    //                {
        //    //                    double weaningAge = ind.BreedParams.NaturalWeaningAge;
        //    //                    if (MathUtilities.FloatsAreEqual(weaningAge, 0))
        //    //                        weaningAge = ind.BreedParams.GestationLength;

        //    //                    if (MathUtilities.IsGreaterThan(ind.Age, weaningAge))
        //    //                    {
        //    //                        ind.Wean(true, "Natural");

        //    //                        // report wean. If mother has died create temp female with the mother's ID for reporting only
        //    //                        ind.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.BreedParams, -1, 999) { ID = ind.MotherID }, clock.Today, ind));
        //    //                    }
        //    //                }
        //    //            }

        //    //            /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother</summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMPotentialIntake")]
        //    //            private void OnCLEMPotentialIntake(object sender, EventArgs e)
        //    //            {
        //    //                List<Ruminant> herd = ruminantHerd.Herd;

        //    //                // Calculate potential intake and reset stores
        //    //                // Order age descending so breeder females calculate milkproduction before suckings grow

        //    //                foreach (var ind in herd.GroupBy(a => a.IsSucklingWithMother).OrderBy(a => a.Key))
        //    //                {
        //    //                    foreach (var indi in ind)
        //    //                    {
        //    //                        // reset tallies at start of the month
        //    //                        //indi.DietDryMatterDigestibility = 0;
        //    //                        //indi.PercentNOfIntake = 0;
        //    //                        //indi.Intake = 0;
        //    //                        //indi.MilkIntake = 0;
        //    //                        CalculatePotentialIntake(indi);
        //    //                    }
        //    //                }
        //    //            }

        //    //            private void CalculatePotentialIntake(Ruminant ind)
        //    //            {
        //    //                // calculate daily potential intake for the selected individual/cohort
        //    //                double standardReferenceWeight = ind.StandardReferenceWeight;

        //    //                double liveWeightForIntake = ind.NormalisedAnimalWeight;
        //    //                // now performed at allocation of weight in Ruminant
        //    //                if (MathUtilities.IsLessThan(ind.HighWeight, ind.NormalisedAnimalWeight))
        //    //                    liveWeightForIntake = ind.HighWeight;

        //    //                // Calculate potential intake based on current weight compared to SRW and previous highest weight
        //    //                double potentialIntake = 0;
        //    //                ind.MilkPotentialIntake = 0;

        //    //                // calculate milk intake shortfall for sucklings
        //    //                // all in units per day and multiplied at end of this section
        //    //                if (!ind.Weaned)
        //    //                {
        //    //                    // potential milk intake/animal/day
        //    //                    ind.MilkPotentialIntake = ind.BreedParams.MilkIntakeIntercept + ind.BreedParams.MilkIntakeCoefficient * ind.Weight;

        //    //                    // get estimated milk available
        //    //                    // this will be updated to the corrected milk available in the calculate energy section.
        //    //                    ind.MilkIntake = Math.Min(ind.MilkPotentialIntake, ind.MothersMilkProductionAvailable);

        //    //                    // if milk supply low, suckling will subsitute forage up to a specified % of bodyweight (R_C60)
        //    //                    if (MathUtilities.IsLessThan(ind.MilkIntake, ind.Weight * ind.BreedParams.MilkLWTFodderSubstitutionProportion))
        //    //                        potentialIntake = Math.Max(0.0, ind.Weight * ind.BreedParams.MaxJuvenileIntake - ind.MilkIntake * ind.BreedParams.ProportionalDiscountDueToMilk);

        //    //                    ind.MilkIntake *= 30.4;
        //    //                    ind.MilkPotentialIntake *= 30.4;
        //    //                }
        //    //                else
        //    //                {
        //    //                    if (ind.IsWeaner)
        //    //                    {
        //    //                        // Reference: SCA Metabolic LWTs
        //    //                        // restored in v112 of NABSA for weaner animals
        //    //                        potentialIntake = ind.BreedParams.IntakeCoefficient * standardReferenceWeight * (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)) * (ind.BreedParams.IntakeIntercept - (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)));
        //    //                    }
        //    //                    else // 12month+ weaned individuals
        //    //                    {
        //    //                        // Reference: SCA based actual LWTs
        //    //                        potentialIntake = ind.BreedParams.IntakeCoefficient * liveWeightForIntake * (ind.BreedParams.IntakeIntercept - liveWeightForIntake / standardReferenceWeight);
        //    //                    }

        //    //                    if (ind.Sex == Sex.Female)
        //    //                    {
        //    //                        RuminantFemale femaleind = ind as RuminantFemale;
        //    //                        // Increase potential intake for lactating breeder
        //    //                        if (femaleind.IsLactating)
        //    //                        {
        //    //                            // move to half way through timestep
        //    //                            double dayOfLactation = femaleind.DaysLactating;
        //    //                            // Reference: Intake multiplier for lactating cow (M.Freer)
        //    //                            // double intakeMilkMultiplier = 1 + 0.57 * Math.Pow((dayOfLactation / 81.0), 0.7) * Math.Exp(0.7 * (1 - (dayOfLactation / 81.0)));
        //    //                            double intakeMilkMultiplier = 1 + ind.BreedParams.LactatingPotentialModifierConstantA * Math.Pow((dayOfLactation / ind.BreedParams.LactatingPotentialModifierConstantB), ind.BreedParams.LactatingPotentialModifierConstantC) * Math.Exp(ind.BreedParams.LactatingPotentialModifierConstantC * (1 - (dayOfLactation / ind.BreedParams.LactatingPotentialModifierConstantB))) * (1 - 0.5 + 0.5 * (ind.Weight / ind.NormalisedAnimalWeight));

        //    //                            // To make this flexible for sheep and goats, added three new Ruminant Coeffs
        //    //                            // Feeding standard values for Beef, Dairy suck, Dairy non-suck and sheep are:
        //    //                            // For 0.57 (A) use .42, .58, .85 and .69; for 0.7 (B) use 1.7, 0.7, 0.7 and 1.4, for 81 (C) use 62, 81, 81, 28
        //    //                            // added LactatingPotentialModifierConstantA, LactatingPotentialModifierConstantB and LactatingPotentialModifierConstantC
        //    //                            // replaces (A), (B) and (C) 
        //    //                            potentialIntake *= intakeMilkMultiplier;

        //    //                            // calculate estimated milk production for time step here
        //    //                            // assuming average feed quality if no previous diet values
        //    //                            // This need to happen before suckling potential intake can be determined.
        //    //                            CalculateMilkProduction(femaleind);
        //    //                            femaleind.MilkProducedThisTimeStep = femaleind.MilkCurrentlyAvailable;
        //    //                        }
        //    //                        else
        //    //                        {
        //    //                            femaleind.MilkProduction = 0;
        //    //                            femaleind.MilkProductionPotential = 0;
        //    //                            femaleind.MilkCurrentlyAvailable = 0;
        //    //                            femaleind.MilkMilkedThisTimeStep = 0;
        //    //                            femaleind.MilkSuckledThisTimeStep = 0;
        //    //                            femaleind.MilkProducedThisTimeStep = 0;
        //    //                        }
        //    //                    }

        //    //                    //TODO: option to restrict potential further due to stress (e.g. heat, cold, rain)

        //    //                }
        //    //                // get monthly intake
        //    //                potentialIntake *= 30.4;
        //    //                ind.PotentialIntake = potentialIntake;
        //    //            }

        //    //            /// <summary>
        //    //            /// Set the milk production of the selected female given diet drymatter digesibility
        //    //            /// </summary>
        //    //            /// <param name="ind">Female individual</param>
        //    //            /// <returns>energy of milk</returns>
        //    //            private double CalculateMilkProduction(RuminantFemale ind)
        //    //            {
        //    //                ind.MilkMilkedThisTimeStep = 0;
        //    //                ind.MilkSuckledThisTimeStep = 0;

        //    //                double energyMetabolic = EnergyGross * (((ind.DietDryMatterDigestibility == 0) ? 50 : ind.DietDryMatterDigestibility) / 100.0) * 0.81;
        //    //                // Reference: SCA p.
        //    //                double kl = ind.BreedParams.ELactationEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.ELactationEfficiencyIntercept;
        //    //                double milkTime = ind.DaysLactating;
        //    //                double milkCurve;
        //    //                // determine milk production curve to use
        //    //                // if milking is taking place use the non-suckling curve for duration of lactation
        //    //                // otherwise use the suckling curve where there is a larger drop off in milk production
        //    //                if (ind.SucklingOffspringList.Count() == 0)
        //    //                    milkCurve = ind.BreedParams.MilkCurveNonSuckling;
        //    //                else // no milking
        //    //                    milkCurve = ind.BreedParams.MilkCurveSuckling;
        //    //                ind.MilkProductionPotential = ind.BreedParams.MilkPeakYield * ind.Weight / ind.NormalisedAnimalWeight * (Math.Pow(((milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay), milkCurve)) * Math.Exp(milkCurve * (1 - (milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay));
        //    //                ind.MilkProductionPotential = Math.Max(ind.MilkProductionPotential, 0.0);
        //    //                // Reference: Potential milk prodn, 3.2 MJ/kg milk - Jouven et al 2008
        //    //                double energyMilk = ind.MilkProductionPotential * 3.2 / kl;
        //    //                // adjust last time step's energy balance
        //    //                double adjustedEnergyBalance = ind.EnergyBalance;
        //    //                if (adjustedEnergyBalance < (-0.5936 / 0.322 * energyMilk))
        //    //                    adjustedEnergyBalance = (-0.5936 / 0.322 * energyMilk);

        //    //                // set milk production in lactating females for consumption.
        //    //                ind.MilkProduction = Math.Max(0.0, ind.MilkProductionPotential * (0.5936 + 0.322 * adjustedEnergyBalance / energyMilk));
        //    //                ind.MilkCurrentlyAvailable = ind.MilkProduction * 30.4;

        //    //                // returns the energy required for milk production

        //    //                return ind.MilkProduction * 3.2 / kl;
        //    //            }

        //    //            /// <summary>Function to calculate growth of herd for the monthly timestep</summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMAnimalWeightGain")]
        //    //            private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        //    //            {
        //    //                List<Ruminant> herd = ruminantHerd.Herd;

        //    //                int cmonth = clock.Today.Month;

        //    //                // grow individuals

        //    //                IEnumerable<string> breeds = herd.Select(a => a.BreedParams.Name).Distinct();
        //    //                this.Status = ActivityStatus.NotNeeded;

        //    //                foreach (string breed in breeds)
        //    //                {
        //    //                    int unfed = 0;
        //    //                    int unfedcalves = 0;
        //    //                    double totalMethane = 0;
        //    //                    foreach (Ruminant ind in herd.Where(a => a.BreedParams.Name == breed).OrderByDescending(a => a.Age))
        //    //                    {
        //    //                        ind.MetabolicIntake = ind.Intake;
        //    //                        this.Status = ActivityStatus.Success;
        //    //                        if (ind.Weaned)
        //    //                        {
        //    //                            // check that they had some food
        //    //                            if (ind.Intake == 0)
        //    //                                unfed++;

        //    //                            // calculate protein concentration

        //    //                            // Calculate diet dry matter digestibilty from the %N of the current diet intake.
        //    //                            // Reference: Ash and McIvor
        //    //                            // ind.DietDryMatterDigestibility = 36.7 + 9.36 * ind.PercentNOfIntake / 62.5;
        //    //                            // Now tracked via incoming food DMD values

        //    //                            // TODO: NABSA restricts Diet_DMD to 75% before supplements. Why?
        //    //                            // Our flow has already taken in supplements by this stage and cannot be distinguished
        //    //                            // Maybe this limit should be placed on some feed to limit DMD to 75% for non supp feeds
        //    //                            // A Ash stated that the 75% limit is no longer required and DMD above 75% is possible even if unlikely.

        //    //                            // Crude protein required generally 130g per kg of digestable feed.
        //    //                            double crudeProteinRequired = ind.BreedParams.ProteinCoefficient * ind.DietDryMatterDigestibility / 100;

        //    //                            // adjust for efficiency of use of protein, (default 90%) degradable. now user param.
        //    //                            double crudeProteinSupply = (ind.PercentNOfIntake * 62.5) * ind.BreedParams.ProteinDegradability;
        //    //                            // This was proteinconcentration * 0.9

        //    //                            // prevent future divide by zero issues.
        //    //                            if (MathUtilities.FloatsAreEqual(crudeProteinSupply, 0.0))
        //    //                                crudeProteinSupply = 0.001;

        //    //                            if (MathUtilities.IsLessThan(crudeProteinSupply, crudeProteinRequired))
        //    //                            {
        //    //                                double ratioSupplyRequired = (crudeProteinSupply + crudeProteinRequired) / (2 * crudeProteinRequired); // half-linear
        //    //                                                                                                                                       //TODO: add min protein to parameters
        //    //                                ratioSupplyRequired = Math.Max(ratioSupplyRequired, 0.3);
        //    //                                ind.MetabolicIntake *= ratioSupplyRequired; // reduces intake proportionally as protein drops below CP required
        //    //                            }

        //    //                            // TODO: check if we still need to apply modification to only the non-supplemented component of intake
        //    //                            // Used to be 1.2 * Potential
        //    //                            ind.Intake = Math.Min(ind.Intake, ind.PotentialIntake);
        //    //                            // when discarding intake can we be specific and hang onto N?
        //    //                            ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake);
        //    //                        }
        //    //                        else
        //    //                        {
        //    //                            // for calves
        //    //                            // these individuals have access to milk or are separated from mother and must survive on calf calculated pasture intake

        //    //                            // if potential intake = 0 they have not needed to consume pasture and intake will be zero.
        //    //                            if (MathUtilities.IsGreaterThanOrEqual(ind.PotentialIntake, 0.0))
        //    //                            {
        //    //                                ind.Intake = Math.Min(ind.Intake, ind.PotentialIntake);
        //    //                                ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake);
        //    //                            }

        //    //                            // no potential * 1.2 as potential has been fixed based on suckling individuals.

        //    //                            if (MathUtilities.IsLessThanOrEqual(ind.MilkIntake + ind.Intake, 0))
        //    //                                unfedcalves++;
        //    //                        }

        //    //                        // TODO: nabsa adjusts potential intake for digestability of fodder here.
        //    //                        // This is now done in RuminantActivityGrazePasture

        //    //                        // calculate energy
        //    //                        CalculateEnergy(ind, out double methane);

        //    //                        // Sum and produce one event for breed at end of loop
        //    //                        totalMethane += methane;

        //    //                        // grow wool and cashmere
        //    //                        ind.Wool += ind.BreedParams.WoolCoefficient * ind.MetabolicIntake;
        //    //                        ind.Cashmere += ind.BreedParams.CashmereCoefficient * ind.MetabolicIntake;
        //    //                    }

        //    //                    // alert user to unfed animals in the month as this should not happen
        //    //                    if (unfed > 0)
        //    //                    {
        //    //                        string warn = $"individuals of [r={breed}] not fed";
        //    //                        string warnfull = $"Some individuals of [r={breed}] were not fed in some months (e.g. [{unfed}] individuals in [{clock.Today.Month}/{clock.Today.Year}])\r\nFix: Check feeding strategy and ensure animals are moved to pasture or fed in yards";
        //    //                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
        //    //                    }
        //    //                    if (unfedcalves > 0)
        //    //                    {
        //    //                        string warn = $"calves of [r={breed}] not fed";
        //    //                        string warnfull = $"Some calves of [r={breed}] were not fed in some months (e.g. [{unfedcalves}] individuals in [{clock.Today.Month}/{clock.Today.Year}])\r\nFix: Check calves are are fed, or have access to pasture (moved with mothers or separately) when no milk is available from mother";
        //    //                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
        //    //                    }

        //    //                    if (methaneEmissions != null)
        //    //                    {
        //    //                        // g per day -> total kg
        //    //                        methaneEmissions.Add(totalMethane * 30.4 / 1000, this, breed, TransactionCategory);
        //    //                    }
        //    //                }
        //    //            }

        //    //            /// <summary>
        //    //            /// Function to calculate manure production and place in uncollected manure pools of the "manure" resource in ProductResources 
        //    //            /// This is called at the end of CLEMAnimalWeightGain so after intake determines and before deaths and sales.
        //    //            /// </summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMCalculateManure")]
        //    //            private void OnCLEMCalculateManure(object sender, EventArgs e)
        //    //            {
        //    //                if (manureStore != null)
        //    //                {
        //    //                    // sort by animal location
        //    //                    foreach (var item in ruminantHerd.Herd.GroupBy(a => a.Location))
        //    //                    {
        //    //                        double manureProduced = item.Sum(a => a.Intake * ((100 - a.DietDryMatterDigestibility) / 100));
        //    //                        manureStore.AddUncollectedManure(item.Key ?? "", manureProduced);
        //    //                    }
        //    //                }
        //    //            }

        //    //            /// <summary>
        //    //            /// Function to calculate energy from intake and subsequent growth
        //    //            /// </summary>
        //    //            /// <param name="ind">Ruminant individual class</param>
        //    //            /// <param name="methaneProduced">Sets output variable to value of methane produced</param>
        //    //            /// <returns></returns>
        //    //            private void CalculateEnergy(Ruminant ind, out double methaneProduced)
        //    //            {
        //    //                // all energy calculations are per day and multiplied at end to give monthly weight gain

        //    //                // ind.MetabolicIntake is the inake received adjusted by any crude protein shortfall in AnimalWeightGain()
        //    //                double intakeDaily = ind.MetabolicIntake / 30.4;

        //    //                // Sme 1 for females and castrates
        //    //                double sme = 1;
        //    //                // Sme 1.15 for all non-castrated males.
        //    //                if (ind.Weaned && ind.Sex == Sex.Male && (ind as RuminantMale).IsCastrated == false)
        //    //                    sme = 1.15;

        //    //                double energyDiet = EnergyGross * ind.DietDryMatterDigestibility / 100.0;
        //    //                // Reference: Nutrient Requirements of domesticated ruminants (p7)
        //    //                double energyMetabolic = energyDiet * 0.81;
        //    //                double energyMetabolicFromIntake = energyMetabolic * intakeDaily;

        //    //                double km = ind.BreedParams.EMaintEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.EMaintEfficiencyIntercept;
        //    //                // Reference: SCA p.49
        //    //                double kg = ind.BreedParams.EGrowthEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.EGrowthEfficiencyIntercept;
        //    //                double energyPredictedBodyMassChange;
        //    //                double energyMaintenance;
        //    //                if (!ind.Weaned)
        //    //                {
        //    //                    // unweaned individuals are assumed to be suckling as natural weaning rate set regardless of inclusion of wean activity
        //    //                    // unweaned individuals without mother or milk from mother will need to try and survive on limited pasture until weaned.

        //    //                    // calculate engergy and growth from milk intake
        //    //                    // recalculate milk intake based on mothers updated milk production for the time step using the previous monthly potential milk intake
        //    //                    ind.MilkIntake = Math.Min(ind.MilkPotentialIntake, ind.MothersMilkProductionAvailable * 30.4);

        //    //                    if (ind.Mother != null)
        //    //                        ind.Mother.TakeMilk(ind.MilkIntake, MilkUseReason.Suckling);
        //    //                    double milkIntakeDaily = ind.MilkIntake / 30.4;

        //    //                    // Below now uses actual intake received rather than assume all potential intake is eaten
        //    //                    double kml = 1;
        //    //                    double kgl = 1;
        //    //                    if (MathUtilities.IsPositive(ind.MetabolicIntake + ind.MilkIntake))
        //    //                    {
        //    //                        // average energy efficiency for maintenance
        //    //                        kml = ((ind.MilkIntake * 0.7) + (intakeDaily * km)) / (ind.MilkIntake + intakeDaily);
        //    //                        // average energy efficiency for growth
        //    //                        kgl = ((ind.MilkIntake * 0.7) + (intakeDaily * kg)) / (ind.MilkIntake + intakeDaily);
        //    //                    }
        //    //                    double energyMilkConsumed = milkIntakeDaily * 3.2;
        //    //                    // limit suckling intake of milk per day
        //    //                    energyMilkConsumed = Math.Min(ind.BreedParams.MilkIntakeMaximum * 3.2, energyMilkConsumed);

        //    //                    energyMaintenance = (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / kml) * Math.Exp(-ind.BreedParams.EMaintExponent * (((ind.Age == 0) ? 0.1 : ind.Age)));
        //    //                    ind.EnergyBalance = energyMilkConsumed + energyMetabolicFromIntake - energyMaintenance;
        //    //                    ind.EnergyIntake = energyMilkConsumed + energyMetabolicFromIntake;
        //    //                    ind.EnergyFetus = 0;
        //    //                    ind.EnergyMaintenance = energyMaintenance;
        //    //                    ind.EnergyMilk = 0;

        //    //                    double feedingValue;
        //    //                    if (MathUtilities.IsPositive(ind.EnergyBalance))
        //    //                        feedingValue = 2 * 0.7 * ind.EnergyBalance / (kgl * energyMaintenance) - 1;
        //    //                    else
        //    //                        //(from Hirata model)
        //    //                        feedingValue = 2 * ind.EnergyBalance / (0.85 * energyMaintenance) - 1;

        //    //                    double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + feedingValue + (ind.BreedParams.GrowthEnergyIntercept2 - feedingValue) / (1 + Math.Exp(-6 * (ind.Weight / ind.NormalisedAnimalWeight - 0.4)));

        //    //                    energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * 0.7 * ind.EnergyBalance / energyEmptyBodyGain;
        //    //                }
        //    //                else
        //    //                {
        //    //                    double energyMilk = 0;
        //    //                    double energyFetus = 0;

        //    //                    if (ind.Sex == Sex.Female)
        //    //                    {
        //    //                        RuminantFemale femaleind = ind as RuminantFemale;

        //    //                        // calculate energy for lactation
        //    //                        // look for milk production calculated before offspring may have been weaned

        //    //                        if (femaleind.IsLactating | MathUtilities.IsPositive(femaleind.MilkProductionPotential))
        //    //                        {
        //    //                            // recalculate milk production based on DMD of food provided
        //    //                            energyMilk = CalculateMilkProduction(femaleind);
        //    //                            // reset this. It was previously determined in potential intake as a measure of milk available. This is now the correct calculation
        //    //                            femaleind.MilkProducedThisTimeStep = femaleind.MilkCurrentlyAvailable;
        //    //                        }

        //    //                        // Determine energy required for foetal development
        //    //                        if (femaleind.IsPregnant)
        //    //                        {
        //    //                            double standardReferenceWeight = ind.StandardReferenceWeight;
        //    //                            // Potential birth weight
        //    //                            // Reference: Freer
        //    //                            double potentialBirthWeight = ind.BreedParams.SRWBirth * standardReferenceWeight * (1 - 0.33 * (1 - ind.Weight / standardReferenceWeight));
        //    //                            double fetusAge = (femaleind.Age - femaleind.AgeAtLastConception) * 30.4;
        //    //                            //TODO: Check fetus age correct
        //    //                            energyFetus = potentialBirthWeight * 349.16 * 0.000058 * Math.Exp(345.67 - 0.000058 * fetusAge - 349.16 * Math.Exp(-0.000058 * fetusAge)) / 0.13;
        //    //                        }
        //    //                    }

        //    //                    //TODO: add draft individual energy requirement

        //    //                    // set maintenance age to maximum of 6 years (2190 days). Now uses EnergeyMaintenanceMaximumAge
        //    //                    double maintenanceAge = Math.Min(ind.Age * 30.4, ind.BreedParams.EnergyMaintenanceMaximumAge * 365);

        //    //                    // Reference: SCA p.24
        //    //                    // Reference p19 (1.20). Does not include MEgraze or Ecold, also skips M,
        //    //                    // 0.000082 is -0.03 Age in Years/365 for days 
        //    //                    energyMaintenance = ind.BreedParams.Kme * sme * (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / km) * Math.Exp(-ind.BreedParams.EMaintExponent * maintenanceAge) + (ind.BreedParams.EMaintIntercept * energyMetabolicFromIntake);
        //    //                    ind.EnergyBalance = energyMetabolicFromIntake - energyMaintenance - energyMilk - energyFetus; // milk will be zero for non lactating individuals.
        //    //                    double feedingValue;
        //    //                    ind.EnergyIntake = energyMetabolicFromIntake;
        //    //                    ind.EnergyFetus = energyFetus;
        //    //                    ind.EnergyMaintenance = energyMaintenance;
        //    //                    ind.EnergyMilk = energyMilk;

        //    //                    // Reference: Feeding_value = Adjustment for rate of loss or gain (SCA p.43, ? different from Hirata model)
        //    //                    if (MathUtilities.IsPositive(ind.EnergyBalance))
        //    //                        feedingValue = 2 * ((kg * ind.EnergyBalance) / (km * energyMaintenance) - 1);
        //    //                    else
        //    //                        feedingValue = 2 * (ind.EnergyBalance / (0.8 * energyMaintenance) - 1);  //(from Hirata model)

        //    //                    double weightToReferenceRatio = Math.Min(1.0, ind.Weight / ind.StandardReferenceWeight);

        //    //                    // Reference:  MJ of Energy required per kg Empty body gain (SCA p.43)
        //    //                    double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + feedingValue + (ind.BreedParams.GrowthEnergyIntercept2 - feedingValue) / (1 + Math.Exp(-6 * (weightToReferenceRatio - 0.4)));
        //    //                    // Determine Empty body change from Eebg and Ebal, and increase by 9% for LW change
        //    //                    if (MathUtilities.IsPositive(ind.EnergyBalance))
        //    //                        energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * kg * ind.EnergyBalance / energyEmptyBodyGain;
        //    //                    else
        //    //                        // Reference: from Hirata model
        //    //                        energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * km * ind.EnergyBalance / (0.8 * energyEmptyBodyGain);
        //    //                }
        //    //                energyPredictedBodyMassChange *= 30.4;  // Convert to monthly

        //    //                ind.PreviousWeight = ind.Weight;

        //    //                //double newWt = Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange);
        //    //                //double maxwt = ind.StandardReferenceWeight * ind.BreedParams.MaximumSizeOfIndividual;
        //    //                //newWt = Math.Min(newWt, maxwt);
        //    //                ind.Weight = Math.Min(
        //    //                    Math.Max(0.0, ind.Weight + energyPredictedBodyMassChange),
        //    //                    ind.StandardReferenceWeight * ind.BreedParams.MaximumSizeOfIndividual
        //    //                    );

        //    //                // Function to calculate approximate methane produced by animal, based on feed intake
        //    //                // Function based on Freer spreadsheet
        //    //                // methane is  0.02 * intakeDaily * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
        //    //                // methane is methaneProduced / 55.28 * 1000; // grams per day

        //    //                // Charmley et al 2016 can be substituted by intercept = 0 and coefficient = 20.7
        //    //                methaneProduced = ind.BreedParams.MethaneProductionCoefficient * intakeDaily;
        //    //            }

        //    //            /// <summary>
        //    //            /// Function to age individuals and remove those that died in timestep
        //    //            /// This needs to be undertaken prior to herd management
        //    //            /// </summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMAgeResources")]
        //    //            private void OnCLEMAgeResources(object sender, EventArgs e)
        //    //            {
        //    //                // grow all individuals
        //    //                foreach (Ruminant ind in ruminantHerd.Herd)
        //    //                    ind.IncrementAge();
        //    //            }

        //    //            /// <summary>Function to determine which animlas have died and remove from the population</summary>
        //    //            /// <param name="sender">The sender.</param>
        //    //            /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //    //            [EventSubscribe("CLEMAnimalDeath")]
        //    //            private void OnCLEMAnimalDeath(object sender, EventArgs e)
        //    //            {
        //    //                // remove individuals that died
        //    //                // currently performed in the month after weight has been adjusted
        //    //                // and before breeding, trading, culling etc (See Clock event order)

        //    //                // Calculated by
        //    //                // critical weight &&
        //    //                // juvenile (unweaned) death based on mothers weight &&
        //    //                // adult weight adjusted base mortality.

        //    //                List<Ruminant> herd = ruminantHerd.Herd;

        //    //                // weight based mortality
        //    //                List<Ruminant> died = new List<Ruminant>();
        //    //                if (herd.Any())
        //    //                {
        //    //                    switch (herd.FirstOrDefault().BreedParams.ConditionBasedMortalityStyle)
        //    //                    {
        //    //                        case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
        //    //                            died = herd.Where(a => MathUtilities.IsLessThanOrEqual(a.Weight, a.HighWeight * a.BreedParams.ConditionBasedMortalityCutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), a.BreedParams.BodyConditionScoreMortalityRate)).ToList();
        //    //                            break;
        //    //                        case ConditionBasedCalculationStyle.RelativeCondition:
        //    //                            died = herd.Where(a => MathUtilities.IsLessThanOrEqual(a.RelativeCondition, a.BreedParams.ConditionBasedMortalityCutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), a.BreedParams.BodyConditionScoreMortalityRate)).ToList();
        //    //                            break;
        //    //                        case ConditionBasedCalculationStyle.BodyConditionScore:
        //    //                            died = herd.Where(a => MathUtilities.IsLessThanOrEqual(a.BodyConditionScore, a.BreedParams.ConditionBasedMortalityCutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), a.BreedParams.BodyConditionScoreMortalityRate)).ToList();
        //    //                            break;
        //    //                        case ConditionBasedCalculationStyle.None:
        //    //                            break;
        //    //                        default:
        //    //                            break;
        //    //                    }

        //    //                    if (died.Any())
        //    //                    {
        //    //                        foreach (Ruminant ind in died)
        //    //                        {
        //    //                            ind.Died = true;
        //    //                            ind.SaleFlag = HerdChangeReason.DiedUnderweight;
        //    //                        }
        //    //                        ruminantHerd.RemoveRuminant(died, this);
        //    //                    }
        //    //                }

        //    //                // base mortality adjusted for condition
        //    //                foreach (var ind in ruminantHerd.Herd)
        //    //                {
        //    //                    double mortalityRate = 0;
        //    //                    if (!ind.Weaned)
        //    //                    {
        //    //                        mortalityRate = 0;
        //    //                        if (ind.Mother == null || MathUtilities.IsLessThan(ind.Mother.Weight, ind.BreedParams.CriticalCowWeight * ind.StandardReferenceWeight))
        //    //                            // if no mother assigned or mother's weight is < CriticalCowWeight * SFR
        //    //                            mortalityRate = ind.BreedParams.JuvenileMortalityMaximum;
        //    //                        else
        //    //                            // if mother's weight >= criticalCowWeight * SFR
        //    //                            mortalityRate = Math.Exp(-Math.Pow(ind.BreedParams.JuvenileMortalityCoefficient * (ind.Mother.Weight / ind.Mother.NormalisedAnimalWeight), ind.BreedParams.JuvenileMortalityExponent));

        //    //                        mortalityRate += ind.BreedParams.MortalityBase;
        //    //                        mortalityRate = Math.Min(mortalityRate, ind.BreedParams.JuvenileMortalityMaximum);
        //    //                    }
        //    //                    else
        //    //                        mortalityRate = 1 - (1 - ind.BreedParams.MortalityBase) * (1 - Math.Exp(Math.Pow(-(ind.BreedParams.MortalityCoefficient * (ind.Weight / ind.NormalisedAnimalWeight - ind.BreedParams.MortalityIntercept)), ind.BreedParams.MortalityExponent)));

        //    //                    // convert mortality from annual (calculated) to monthly (applied).
        //    //                    if (MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), mortalityRate / 12))
        //    //                        ind.Died = true;
        //    //                }

        //    //                died = herd.Where(a => a.Died).Select(a => { a.SaleFlag = HerdChangeReason.DiedMortality; return a; }).ToList();




        //    //died.Select(a => { a.SaleFlag = HerdChangeReason.DiedMortality; return a; }).ToList();
        //    died = herd.Where(a => a.Died).ToList();
        //    //died.Select(a => { a.SaleFlag = HerdChangeReason.DiedMortality; return a; }).ToList();

        //    // TODO: separate foster from real mother for genetics
        //    // check for death of mother with sucklings and try foster sucklings
        //    //IEnumerable<RuminantFemale> mothersWithSuckling = died.OfType<RuminantFemale>().Where(a => a.SucklingOffspringList.Any());
        //    //List<RuminantFemale> wetMothersAvailable = died.OfType<RuminantFemale>().Where(a => a.IsLactating & a.SucklingOffspringList.Count == 0).OrderBy(a => a.DaysLactating(events.Interval/2.0)).ToList();
        //    //int wetMothersAssigned = 0;
        //    //if (wetMothersAvailable.Any())
        //    //{
        //    //    if (mothersWithSuckling.Any())
        //    //    {
        //    //        foreach (var deadMother in mothersWithSuckling)
        //    //        {
        //    //            foreach (var suckling in deadMother.SucklingOffspringList)
        //    //            {
        //    //                if(wetMothersAssigned < wetMothersAvailable.Count && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), suckling.Parameters.Breeding.ProportionAcceptingSurrogate))
        //    //                {
        //    //                    suckling.Mother = wetMothersAvailable[wetMothersAssigned];
        //    //                    wetMothersAssigned++;
        //    //                }
        //    //                else
        //    //                    break;
        //    //            }

        //    //        }
        //    //    }
        //    //}

        //    ruminantHerd.RemoveRuminant(died, this);
        //}

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // check parameters are available for all ruminants.
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.Grow is null))
            {
                string[] memberNames = new string[] { "RuminantParametersGrow" };
                results.Add(new ValidationResult($"No [RuminantParametersGrow] parameters are provided for [{item.NameWithParent}]", memberNames));
            }
            return results;
        }

        #endregion


        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">The gross energy content of forage is ");

            if (MathUtilities.FloatsAreEqual(EnergyGross, 0))
                htmlWriter.Write("<span class=\"errorlink\">[NOT SET]</span>");
            else
                htmlWriter.Write("<span class=\"setvalue\">" + EnergyGross.ToString() + "</span>");
            htmlWriter.Write(" MJ/kg dry matter</div>");

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
