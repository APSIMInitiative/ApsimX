using Models.Core;
using Models.CLEM.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity</summary>
    /// <summary>This activity determines potential intake for the Feeding activities and feeding arbitrator for all ruminants</summary>
    /// <summary>This activity includes deaths</summary>
    /// <summary>See Breed activity for births, calf mortality etc</summary>
    /// <version>1.1</version>
    /// <updates>First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs the growth and aging of all ruminants. Only one instance of this activity is permitted.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrow.htm")]
    public class RuminantActivityGrow : CLEMActivityBase
    {
        [Link]
        private readonly Clock Clock = null;

        private GreenhouseGasesType methaneEmissions;
        private ProductStoreTypeManure manureStore;

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
        [XmlIgnore]
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
            methaneEmissions = Resources.GetResourceItem(this, typeof(GreenhouseGases), "Methane", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GreenhouseGasesType;
            manureStore = Resources.GetResourceItem(this, typeof(ProductStore), "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as ProductStoreTypeManure;
        }

        /// <summary>Function to determine naturally wean individuals at start of timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> herd = ruminantHerd.Herd;

            // Natural weaning takes place here before animals eat or take milk from mother.
            foreach (var ind in herd.Where(a => a.Weaned == false))
            {
                double weaningAge = ind.BreedParams.NaturalWeaningAge;
                if(weaningAge == 0)
                {
                    weaningAge = ind.BreedParams.GestationLength;
                }

                if (ind.Age >= weaningAge)
                {
                    ind.Wean(true, "Natural");
                    if (ind.Mother != null)
                    {
                        // report conception status changed when last multiple birth dies.
                        ind.Mother.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother, Clock.Today));
                    }
                }
            }
        }

        /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPotentialIntake")]
        private void OnCLEMPotentialIntake(object sender, EventArgs e)
        {
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> herd = ruminantHerd.Herd;

            // Calculate potential intake and reset stores
            // Order age descending so breeder females calculate milkproduction before suckings grow

            DateTime start = DateTime.Now;

            foreach (var ind in herd.GroupBy(a => a.Weaned).OrderByDescending(a => a.Key))
            {
                foreach (var indi in ind)
                {
                    // reset tallies at start of the month
                    indi.DietDryMatterDigestibility = 0;
                    indi.PercentNOfIntake = 0;
                    indi.Intake = 0;
                    indi.MilkIntake = 0;
                    CalculatePotentialIntake(indi);
                }
            }

            var diff = DateTime.Now - start;
        }

        private void CalculatePotentialIntake(Ruminant ind)
        {
            // calculate daily potential intake for the selected individual/cohort
            double standardReferenceWeight = ind.StandardReferenceWeight;

            // now calculated in Ruminant
            // ind.NormalisedAnimalWeight = standardReferenceWeight - ((1 - ind.BreedParams.SRWBirth) * standardReferenceWeight) * Math.Exp(-(ind.BreedParams.AgeGrowthRateCoefficient * (ind.Age * 30.4)) / (Math.Pow(standardReferenceWeight, ind.BreedParams.SRWGrowthScalar)));
            double liveWeightForIntake = ind.NormalisedAnimalWeight;
            // now performed at allocation of weight in Ruminant
            if (ind.HighWeight < ind.NormalisedAnimalWeight)
            {
                liveWeightForIntake = ind.HighWeight;
            }

            // Calculate potential intake based on current weight compared to SRW and previous highest weight
            double potentialIntake = 0;
            ind.MilkIntakePotential = 0;

            // calculate milk intake shortfall for sucklings
            // all in units per day and multiplied at end of this section
            if (!ind.Weaned)
            {
                // potential milk intake/animal/day
                ind.MilkIntakePotential = ind.BreedParams.MilkIntakeIntercept + ind.BreedParams.MilkIntakeCoefficient * ind.Weight;

                // get estimated milk available
                // this will be updated to the corrected milk available in the calculate energy section.
                ind.MilkIntake = Math.Min(ind.MilkIntakePotential, ind.MothersMilkProductionAvailable);

                // if milk supply low, calf will subsitute forage up to a specified % of bodyweight (R_C60)
                if (ind.MilkIntake < ind.Weight * ind.BreedParams.MilkLWTFodderSubstitutionProportion)
                {
                    potentialIntake = Math.Max(0.0, ind.Weight * ind.BreedParams.MaxJuvenileIntake - ind.MilkIntake * ind.BreedParams.ProportionalDiscountDueToMilk);
                }

                ind.MilkIntake *= 30.4;
            }
            else
            {
                if (ind.Weaner)
                {
                    // Reference: SCA Metabolic LWTs
                    // restored in v112 of NABSA for weaner animals
                    potentialIntake = ind.BreedParams.IntakeCoefficient * standardReferenceWeight * (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)) * (ind.BreedParams.IntakeIntercept - (Math.Pow(liveWeightForIntake, 0.75) / Math.Pow(standardReferenceWeight, 0.75)));
                    // older individual check. previous method before adding calulation for weaners after discussions with Cam McD
                    //double prevint = ind.BreedParams.IntakeCoefficient * liveWeightForIntake * (ind.BreedParams.IntakeIntercept - liveWeightForIntake / standardReferenceWeight);
                }
                else // 12month+ individuals
                {
                    // Reference: SCA based actual LWTs
                    potentialIntake = ind.BreedParams.IntakeCoefficient * liveWeightForIntake * (ind.BreedParams.IntakeIntercept - liveWeightForIntake / standardReferenceWeight);
                }

                if (ind.Gender == Sex.Female)
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
                        potentialIntake *= intakeMilkMultiplier;

                        // calculate estimated milk production for time step here
                        // assuming average feed quality if no previous diet values
                        // This need to happen before suckling potential intake can be determined.
                        CalculateMilkProduction(femaleind);
                        femaleind.MilkProducedThisTimeStep = femaleind.MilkProduction * 30.4;
                    }
                    else
                    {
                        femaleind.MilkProduction = 0;
                        femaleind.MilkProductionPotential = 0;
                        femaleind.MilkCurrentlyAvailable = 0;
                    }
                }
                
                //TODO: option to restrict potential further due to stress (e.g. heat, cold, rain)

            }
            // get monthly intake
            potentialIntake *= 30.4;
            ind.PotentialIntake = potentialIntake;
        }

        /// <summary>
        /// Set the milk production of the selected female given diet drymatter digesibility
        /// </summary>
        /// <param name="ind">Female individual</param>
        /// <returns>energy of milk</returns>
        private double CalculateMilkProduction(RuminantFemale ind)
        {
            ind.MilkMilkedThisTimeStep = 0;
            ind.MilkSuckledThisTimeStep = 0;

            double energyMetabolic = EnergyGross * (((ind.DietDryMatterDigestibility==0)?50:ind.DietDryMatterDigestibility)/100.0) * 0.81;
            // Reference: SCA p.
            double kl = ind.BreedParams.ELactationEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.ELactationEfficiencyIntercept;
            double milkTime = ind.DaysLactating;
            double milkCurve;
            // determine milk production curve to use
            // if milking is taking place use the non-suckling curve for duration of lactation
            // otherwise use the suckling curve where there is a larger drop off in milk production
            if (ind.MilkingPerformed)
            {
                milkCurve = ind.BreedParams.MilkCurveNonSuckling;
            }
            else // no milking
            {
                milkCurve = ind.BreedParams.MilkCurveSuckling;
            }
            ind.MilkProductionPotential = ind.BreedParams.MilkPeakYield * ind.Weight / ind.NormalisedAnimalWeight * (Math.Pow(((milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay), milkCurve)) * Math.Exp(milkCurve * (1 - (milkTime + ind.BreedParams.MilkOffsetDay) / ind.BreedParams.MilkPeakDay));
            ind.MilkProductionPotential = Math.Max(ind.MilkProductionPotential, 0.0);
            // Reference: Potential milk prodn, 3.2 MJ/kg milk - Jouven et al 2008
            double energyMilk = ind.MilkProductionPotential * 3.2 / kl;
            // adjust last time step's energy balance
            double adjustedEnergyBalance = ind.EnergyBalance;
            if (adjustedEnergyBalance < (-0.5936 / 0.322 * energyMilk))
            {
                adjustedEnergyBalance = (-0.5936 / 0.322 * energyMilk);
            }

            // set milk production in lactating females for consumption.
            ind.MilkProduction = Math.Max(0.0, ind.MilkProductionPotential * (0.5936 + 0.322 * adjustedEnergyBalance / energyMilk));
            ind.MilkCurrentlyAvailable = ind.MilkProduction * 30.4;

            // returns the energy required for milk production

            return ind.MilkProduction * 3.2 / kl;
        }

        /// <summary>Function to calculate growth of herd for the monthly timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> herd = ruminantHerd.Herd;

            int cmonth = Clock.Today.Month;

            // grow individuals

            List<string> breeds = herd.Select(a => a.BreedParams.Name).Distinct().ToList();
            this.Status = ActivityStatus.NotNeeded;

            foreach (string breed in breeds)
            {
                double totalMethane = 0;
                foreach (Ruminant ind in herd.Where(a => a.BreedParams.Name == breed).OrderByDescending(a => a.Age))
                {
                    ind.MetabolicIntake = ind.Intake;
                    this.Status = ActivityStatus.Success;
                    if (ind.Weaned)
                    {
                        // calculate protein concentration

                        // Calculate diet dry matter digestibilty from the %N of the current diet intake.
                        // Reference: Ash and McIvor
                        // ind.DietDryMatterDigestibility = 36.7 + 9.36 * ind.PercentNOfIntake / 62.5;
                        // Now tracked via incoming food DMD values

                        // TODO: NABSA restricts Diet_DMD to 75% before supplements. Why?
                        // Our flow has already taken in supplements by this stage and cannot be distinguished
                        // Maybe this limit should be placed on some feed to limit DMD to 75% for non supp feeds
                        // A Ash stated that the 75% limit is no longer required and DMD above 75% is possible even if unlikely.

                        // TODO: Check equation. NABSA doesn't include the 0.9
                        // Crude protein required generally 130g per kg of digestable feed.
                        double crudeProteinRequired = ind.BreedParams.ProteinCoefficient * ind.DietDryMatterDigestibility / 100;

                        // adjust for efficiency of use of protein, (default 90%) degradable. now user param.
                        double crudeProteinSupply = (ind.PercentNOfIntake * 62.5) * ind.BreedParams.ProteinDegradability;
                        // This was proteinconcentration * 0.9

                        // prevent future divide by zero issues.
                        if (crudeProteinSupply == 0.0)
                        {
                            crudeProteinSupply = 0.001;
                        }

                        if (crudeProteinSupply < crudeProteinRequired)
                        {
                            double ratioSupplyRequired = (crudeProteinSupply + crudeProteinRequired) / (2 * crudeProteinRequired);
                            //TODO: add min protein to parameters
                            ratioSupplyRequired = Math.Max(ratioSupplyRequired, 0.3);
                            ind.MetabolicIntake *= ratioSupplyRequired;
                        }

                        // old. I think IAT
                        //double ratioSupplyRequired = Math.Max(0.3, Math.Min(1.3, crudeProteinSupply / crudeProteinRequired));

                        // TODO: check if we still need to apply modification to only the non-supplemented component of intake
                        // Used to be 1.2 * Potential
                        ind.Intake = Math.Min(ind.Intake, ind.PotentialIntake);
                        ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake);
                    }
                    else
                    {
                        // no potential * 1.2 as potential has been fixed based on suckling individuals.
                        ind.Intake = Math.Min(ind.Intake, ind.PotentialIntake);
                        ind.MetabolicIntake = Math.Min(ind.MetabolicIntake, ind.Intake);
                    }

                    // TODO: nabsa adjusts potential intake for digestability of fodder here.
                    // This is now done in RuminantActivityGrazePasture

                    // calculate energy
                    CalculateEnergy(ind, out double methane);

                    // Sum and produce one event for breed at end of loop
                    totalMethane += methane;

                    // grow wool and cashmere
                    ind.Wool += ind.BreedParams.WoolCoefficient * ind.Intake;
                    ind.Cashmere += ind.BreedParams.CashmereCoefficient * ind.Intake;
                }

                if (methaneEmissions != null)
                {
                    // g per day -> total kg
                    methaneEmissions.Add(totalMethane * 30.4 / 1000, this, breed);
                }
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
            if(manureStore!=null)
            {
                // sort by animal location
                foreach (var item in Resources.RuminantHerd().Herd.GroupBy(a => a.Location))
                {
                    double manureProduced = item.Sum(a => a.Intake * ((100 - a.DietDryMatterDigestibility) / 100));
                    manureStore.AddUncollectedManure(item.Key??"", manureProduced);
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
            double intakeDaily = ind.MetabolicIntake / 30.4;

            // Sme 1 for females and castrates
            // TODO: castrates not implemented
            double sme = 1;
            // Sme 1.15 for all males.
            if (ind.Gender == Sex.Male)
            {
                sme = 1.15;
            }

            double energyDiet = EnergyGross * ind.DietDryMatterDigestibility / 100.0;
            // Reference: Nutrient Requirements of domesticated ruminants (p7)
            double energyMetabolic = energyDiet * 0.81;
            double energyMetablicFromIntake = energyMetabolic * intakeDaily;

            double km = ind.BreedParams.EMaintEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.EMaintEfficiencyIntercept;
            // Reference: SCA p.49
            double kg = ind.BreedParams.EGrowthEfficiencyCoefficient * energyMetabolic / EnergyGross + ind.BreedParams.EGrowthEfficiencyIntercept;
            double energyPredictedBodyMassChange;
            double energyMaintenance;
            if (!ind.Weaned)
            {
                // calculate engergy and growth from milk intake

                // recalculate milk intake based on mothers updated milk production for the time step
                double potentialMilkIntake = ind.BreedParams.MilkIntakeIntercept + ind.BreedParams.MilkIntakeCoefficient * ind.Weight;
                ind.MilkIntake = Math.Min(potentialMilkIntake, ind.MothersMilkProductionAvailable);

                if (ind.Mother != null)
                {
                    ind.Mother.TakeMilk(ind.MilkIntake * 30.4, MilkUseReason.Suckling);
                }

                // Below now uses actual intake received rather than assume all potential intake is eaten
                double kml = 1;
                double kgl = 1;
                if ((ind.MetabolicIntake + ind.MilkIntake) > 0)
                {
                    // average energy efficiency for maintenance
                    kml = ((ind.MilkIntake * 0.7) + (intakeDaily * km)) / (ind.MilkIntake + intakeDaily);
                    // average energy efficiency for growth
                    kgl = ((ind.MilkIntake * 0.7) + (intakeDaily * kg)) / (ind.MilkIntake + intakeDaily);
                }
                double energyMilkConsumed = ind.MilkIntake * 3.2;
                // limit calf intake of milk per day
                energyMilkConsumed = Math.Min(ind.BreedParams.MilkIntakeMaximum * 3.2, energyMilkConsumed);

                energyMaintenance = (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / kml) * Math.Exp(-ind.BreedParams.EMaintExponent * ind.AgeZeroCorrected);
                ind.EnergyBalance = energyMilkConsumed - energyMaintenance + energyMetablicFromIntake;
                ind.EnergyIntake = energyMilkConsumed + energyMetablicFromIntake;
                ind.EnergyFoetus = 0;
                ind.EnergyMaintenance = energyMaintenance;
                ind.EnergyMilk = 0;

                double feedingValue;
                if (ind.EnergyBalance > 0)
                {
                    feedingValue = 2 * 0.7 * ind.EnergyBalance / (kgl * energyMaintenance) - 1;
                }
                else
                {
                    //(from Hirata model)
                    feedingValue = 2 * ind.EnergyBalance / (0.85 * energyMaintenance) - 1;
                }
                double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + feedingValue + (ind.BreedParams.GrowthEnergyIntercept2 - feedingValue) / (1 + Math.Exp(-6 * (ind.Weight / ind.NormalisedAnimalWeight - 0.4)));

                energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * 0.7 * ind.EnergyBalance / energyEmptyBodyGain;
            }
            else
            {
                double energyMilk = 0;
                double energyFoetus = 0;

                if (ind.Gender == Sex.Female)
                {
                    RuminantFemale femaleind = ind as RuminantFemale;

                    // calculate energy for lactation
                    // look for milk production calculated before offspring may have been weaned

                    if (femaleind.IsLactating | femaleind.MilkProductionPotential > 0)
                    {
                        // recalculate milk production based on DMD of food provided
                        energyMilk = CalculateMilkProduction(femaleind);
                    }

                    // Determine energy required for foetal development
                    if (femaleind.IsPregnant)
                    {
                        double standardReferenceWeight = ind.StandardReferenceWeight;
                        // Potential birth weight
                        // Reference: Freer
                        double potentialBirthWeight = ind.BreedParams.SRWBirth * standardReferenceWeight * (1 - 0.33 * (1 - ind.Weight / standardReferenceWeight));
                        double foetusAge = (femaleind.Age - femaleind.AgeAtLastConception) * 30.4;
                        //TODO: Check foetus age correct
                        energyFoetus = potentialBirthWeight * 349.16 * 0.000058 * Math.Exp(345.67 - 0.000058 * foetusAge - 349.16 * Math.Exp(-0.000058 * foetusAge)) / 0.13;
                    }
                }

                //TODO: add draft individual energy requirement

                // set maintenance age to maximum of 6 years (2190 days). Now uses EnergeyMaintenanceMaximumAge
                double maintenanceAge = Math.Min(ind.Age * 30.4, ind.BreedParams.EnergyMaintenanceMaximumAge * 365);

                // Reference: SCA p.24
                // Reference p19 (1.20). Does not include MEgraze or Ecold, also skips M,
                // 0.000082 is -0.03 Age in Years/365 for days 
                energyMaintenance = ind.BreedParams.Kme * sme * (ind.BreedParams.EMaintCoefficient * Math.Pow(ind.Weight, 0.75) / km) * Math.Exp(-ind.BreedParams.EMaintExponent * maintenanceAge) + (ind.BreedParams.EMaintIntercept * energyMetablicFromIntake);
                ind.EnergyBalance = energyMetablicFromIntake - energyMaintenance - energyMilk - energyFoetus; // milk will be zero for non lactating individuals.
                double feedingValue;
                ind.EnergyIntake = energyMetablicFromIntake;
                ind.EnergyFoetus = energyFoetus;
                ind.EnergyMaintenance = energyMaintenance;
                ind.EnergyMilk = energyMilk;

                // Reference: Feeding_value = Ajustment for rate of loss or gain (SCA p.43, ? different from Hirata model)
                if (ind.EnergyBalance > 0)
                {
                    feedingValue = 2 * ((kg * ind.EnergyBalance) / (km * energyMaintenance) - 1);
                }
                else
                {
                    feedingValue = 2 * (ind.EnergyBalance / (0.8 * energyMaintenance) - 1);  //(from Hirata model)
                }
                double weightToReferenceRatio = Math.Min(1.0, ind.Weight / ind.StandardReferenceWeight);

                // Reference:  MJ of Energy required per kg Empty body gain (SCA p.43)
                double energyEmptyBodyGain = ind.BreedParams.GrowthEnergyIntercept1 + feedingValue + (ind.BreedParams.GrowthEnergyIntercept1 - feedingValue) / (1 + Math.Exp(-6 * (weightToReferenceRatio - 0.4)));
                // Determine Empty body change from Eebg and Ebal, and increase by 9% for LW change
                if (ind.EnergyBalance > 0)
                {
                    energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * kg * ind.EnergyBalance / energyEmptyBodyGain;
                }
                else
                {
                    // Reference: from Hirata model
                    energyPredictedBodyMassChange = ind.BreedParams.GrowthEfficiency * km * ind.EnergyBalance / (0.8 * energyEmptyBodyGain);
                }
            }
            energyPredictedBodyMassChange *= 30.4;  // Convert to monthly

            ind.PreviousWeight = ind.Weight;
            if(ind.Gender == Sex.Female && (ind as RuminantFemale).BirthDue)
            {
                ind.Weight -= (ind as RuminantFemale).WeightLossDueToCalf;
            }
            ind.Weight += energyPredictedBodyMassChange;
            ind.Weight = Math.Max(0.0, ind.Weight);
            ind.Weight = Math.Min(ind.Weight, ind.StandardReferenceWeight * ind.BreedParams.MaximumSizeOfIndividual);

            // Function to calculate approximate methane produced by animal, based on feed intake
            // Function based on Freer spreadsheet
            // methane is  0.02 * intakeDaily * ((13 + 7.52 * energyMetabolic) + energyMetablicFromIntake / energyMaintenance * (23.7 - 3.36 * energyMetabolic)); // MJ per day
            // methane is methaneProduced / 55.28 * 1000; // grams per day
            
            // Charmely et al 2016 can be substituted by intercept = 0 and coefficient = 20.7
            methaneProduced = ind.BreedParams.MethaneProductionCoefficient * intakeDaily;
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
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            // grow all individuals
            foreach (Ruminant ind in ruminantHerd.Herd)
            {
                ind.IncrementAge();
            }
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

            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> herd = ruminantHerd.Herd;

            // weight based mortality
            List<Ruminant> died = herd.Where(a => a.Weight < (a.HighWeight * a.BreedParams.ProportionOfMaxWeightToSurvive)).ToList();
            // set died flag
            died.Select(a => { a.SaleFlag = HerdChangeReason.DiedUnderweight; return a; }).ToList();
            ruminantHerd.RemoveRuminant(died, this);

            // base mortality adjusted for condition
            foreach (var ind in ruminantHerd.Herd)
            {
                double mortalityRate = 0;
                if (!ind.Weaned)
                {
                    mortalityRate = 0;
                    if((ind.Mother == null) || (ind.Mother.Weight < ind.BreedParams.CriticalCowWeight * ind.StandardReferenceWeight))
                    {
                        // if no mother assigned or mother's weight is < CriticalCowWeight * SFR
                        mortalityRate = ind.BreedParams.JuvenileMortalityMaximum;
                    }
                    else
                    {
                        // if mother's weight >= criticalCowWeight * SFR
                        mortalityRate = Math.Exp(-Math.Pow(ind.BreedParams.JuvenileMortalityCoefficient * (ind.Mother.Weight / ind.Mother.NormalisedAnimalWeight), ind.BreedParams.JuvenileMortalityExponent));
                    }
                    mortalityRate += ind.BreedParams.MortalityBase;
                    mortalityRate = Math.Min(mortalityRate, ind.BreedParams.JuvenileMortalityMaximum);
                }
                else
                {
                    mortalityRate = 1 - (1 - ind.BreedParams.MortalityBase) * (1 - Math.Exp(Math.Pow(-(ind.BreedParams.MortalityCoefficient * (ind.Weight / ind.NormalisedAnimalWeight - ind.BreedParams.MortalityIntercept)), ind.BreedParams.MortalityExponent)));
                }
                // convert mortality from annual (calculated) to monthly (applied).
                if (RandomNumberGenerator.Generator.NextDouble() <= (mortalityRate/12))
                {
                    ind.Died = true;
                }
            }

            died = herd.Where(a => a.Died).ToList();
            died.Select(a => { a.SaleFlag = HerdChangeReason.DiedMortality; return a; }).ToList();

            // TODO: separate foster from real mother for genetics
            // check for death of mother with sucklings and try foster sucklings
            List<RuminantFemale> mothersWithCalf = died.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.SucklingOffspringList.Count() > 0).ToList();
            List<RuminantFemale> wetMothersAvailable = died.Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsLactating & a.SucklingOffspringList.Count() == 0).OrderBy(a => a.DaysLactating).ToList();
            int wetMothersAssigned = 0;
            if (wetMothersAvailable.Count() > 0)
            {
                if(mothersWithCalf.Count() > 0)
                {
                    foreach (var deadMother in mothersWithCalf)
                    {
                        foreach (var calf in deadMother.SucklingOffspringList)
                        {
                            if(wetMothersAssigned < wetMothersAvailable.Count())
                            {
                                calf.Mother = wetMothersAvailable[wetMothersAssigned];
                                wetMothersAssigned++;
                            }
                            else
                            {
                                break;
                            }
                        }

                    }
                }
            }

            ruminantHerd.RemoveRuminant(died, this);
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return; ;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">The gross energy content of forage is ";

            if (EnergyGross == 0)
            {
                html += "<span class=\"errorlink\">[NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"setvalue\">" + EnergyGross.ToString() + "</span>";
            }
            html += " MJ/kg dry matter</div>";
            return html;

        }

    }
}
