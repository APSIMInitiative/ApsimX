
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
using Models.Utilities;

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
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow), typeof(RuminantParametersGeneral), typeof(RuminantParametersLactation) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType },
        SingleInstance = true)]
    public class RuminantActivityGrow : CLEMActivityBase, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
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
            foreach (var ind in herd.Where(a => a.IsWeaned == false))
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
            if (!ind.IsWeaned)
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

            IEnumerable<string> breeds = herd.Select(a => a.Breed).Distinct();
            Status = ActivityStatus.NotNeeded;

            foreach (string breed in breeds)
            {
                int unfed = 0;
                int unfedcalves = 0;
                foreach (Ruminant ind in herd.Where(a => a.Breed == breed).OrderByDescending(a => a.AgeInDays))
                {
                    ind.MetabolicIntake = ind.Intake.SolidsDaily.Actual;
                    Status = ActivityStatus.Success;
                    if (ind.IsWeaned)
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
                        // WRONG! this is actually the CP provided by the rumen per kg digested. 

                        // JD - discussion
                        // Assume energy is limiting intake before protein
                        // in most cases it is
                        // Recommend this is turned off - parameter = 0;
                        // Better to flag a warning if protein is limiting intake
                        
                        //double crudeProteinRequired = 0.0;
                        // //double crudeProteinRequired = ind.Parameters.Grow.ProteinCoefficient / 100.0 * ind.Intake.DMD / 100 * ind.Intake.SolidsDaily.ActualForTimeStep(events.Interval);
                        // //double crudeProteinRequired = ind.Parameters.Grow.ProteinCoefficient * ind.Intake.DMD / 100;

                        // // adjust for efficiency of use of protein, (default 90%) degradable. now user param.
                        //double crudeProteinSupply = (ind.Intake.CrudeProtein) * ind.Parameters.Grow.ProteinDegradability; //  PercentNOfIntake * 62.5
                        //// This was proteinconcentration * 0.9

                        //// prevent future divide by zero issues.
                        //if (MathUtilities.FloatsAreEqual(crudeProteinSupply, 0.0))
                        //    crudeProteinSupply = 0.001;

                        //if (MathUtilities.IsLessThan(crudeProteinSupply, crudeProteinRequired))
                        //{
                        //    double ratioSupplyRequired = (crudeProteinSupply + crudeProteinRequired) / (2 * crudeProteinRequired); // half-linear
                        //    //TODO: add min protein to parameters
                        //    ratioSupplyRequired = Math.Max(ratioSupplyRequired, 0.3);
                        //    ind.MetabolicIntake *= ratioSupplyRequired; // reduces intake proportionally as protein drops below CP required
                        //}

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

                    // TODO: nabsa adjusts potential intake for digestibility of fodder here.
                    // This is now done in RuminantActivityGrazePasture

                    // calculate energy
                    CalculateEnergy(ind);

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
            }
        }

        /// <summary>
        /// Function to calculate energy from intake and subsequent growth
        /// </summary>
        /// <param name="ind">Ruminant individual class</param>
        /// <returns></returns>
        private void CalculateEnergy(Ruminant ind)
        {
            // all energy calculations are per day and multiplied at end to give monthly weight gain

            double intakeDaily = ind.MetabolicIntake / events.Interval;

            // Sme 1 for females and castrates
            double sme = 1;
            // Sme 1.15 for all non-castrated males.
            if (ind.IsWeaned && ind.Sex == Sex.Male && (ind as RuminantMale).IsCastrated == false)
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
            if (!ind.IsWeaned)
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

                //
                // These original equations seems to be incorrect.
                // See adult section below!
                //


                // REMOVED!
                // if (MathUtilities.IsPositive(EnergyBalance))
                //     feedingValue = 2 * 0.7 * EnergyBalance / (kgl * energyMaintenance) - 1;
                // else
                //     // (from Hirata model)
                //     feedingValue = 2 * EnergyBalance / (0.85 * energyMaintenance) - 1;

                feedingValue = ((energyMetabolicFromIntake / energyMaintenance) - 1);

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

                //TODO: add draught individual energy requirement

                double feedingValue;
                //ind.EnergyFromIntake = energyMetabolicFromIntake;
                ind.Energy.ForFetus = energyFetus;
                ind.Energy.ForMaintenance = energyMaintenance;
                ind.Energy.ForLactation = energyMilk;

                //
                // These original equations seems to be incorrect.
                // Based on assessment with J.D. and the form of parameter needed in energyEmptyBodyGain 
                // The correct measure of feeding value is (metabolisable energy from intake / energy for maintenance) - 1
                // Will need to be updated in all versions and documentation.

                // REMOVED!
                // Reference: Feeding_value = Adjustment for rate of loss or gain (SCA p.43, ? different from Hirata model)
                // if (MathUtilities.IsPositive(energyBalance))
                //     feedingValue = 2 * ((kg * energyBalance) / (km * energyMaintenance) - 1);
                // else
                //     feedingValue = 2 * (energyBalance / (0.8 * energyMaintenance) - 1);  //(from Hirata model)

                feedingValue = ((energyMetabolicFromIntake / energyMaintenance) - 1) - 1;

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

            ind.Weight.AdjustByWeightChange(energyPredictedBodyMassChange, ind);
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

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check parameters are available for all ruminants.
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.Grow is null))
            {
                yield return new ValidationResult($"No [RuminantParametersGrow] parameters are provided for [{item.NameWithParent}]", new string[] { "RuminantParametersGrow" });
            }
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

            return htmlWriter.ToString();
        }
        #endregion

    }
}
