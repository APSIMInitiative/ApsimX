using APSIM.Numerics;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using StdUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity (PF, Protein and Fat version)</summary>
    /// <summary>This class represents the CLEM activity responsible for determining potential intake from the quality of all food eaten, and providing energy and protein for all needs (e.g. wool production, pregnancy, lactation and growth).</summary>
    /// <summary>Full documentation and equations required for this component are available in Dougherty et al, 2025 (in prep).</summary>
    /// <remarks>Ruminant death activity controls mortality, while the Breed activity is responsible for conception and births.</remarks>
    /// <authors>Summary and implementation of best methods in predicting ruminant growth based on Frier 2012 (AusFarm) and latest research, James Dougherty, CSIRO</authors>
    /// <authors>CLEM upgrade and implementation, Adam Liedloff, CSIRO</authors>
    /// <acknowledgements>This animal production continues to develop upon the equations provided by Frier (2007, 2012), implemented in GRAZPLAN (Moore, CSIRO) and APSFARM (CSIRO)</acknowledgements>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs best available growth and aging of all ruminants based on Frier, 2012.")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrowPF.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: [typeof(RuminantParametersGrowPF)],
        associationStyles: [ModelAssociationStyle.DescendentOfRuminantType],
        SingleInstance = true)]
    public class RuminantActivityGrowPF : CLEMRuminantActivityBase, IValidatableObject, IRuminantActivityGrow
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private ProductStoreTypeManure manureStore;
        private readonly FoodResourcePacket milkPacket = new()
        {
            TypeOfFeed = FeedType.Milk,
            RumenDegradableProteinPercent = 0,
        };

        /// <summary>
        /// Switch to perform unfed test and reporting.
        /// </summary>
        [Description("Perform unfed individuals warning")]
        public bool ReportUnfed { get; set; } = false;

        /// <inheritdoc/>
        public bool IncludeFatAndProtein { get => true; }
        
        /// <inheritdoc/>
        public bool IncludeVisceralProteinMass { get => false; }

        // ========================================================================================================================
        // The equations used in this activity are provided in Dougherty et al, 2025 (in prep)
        // Links to equations and parameters in the paper and XXXX report are provided as comments in the code and parameter names.
        // e.g. Equation X, parameterName_CIX
        // ========================================================================================================================

        /// <summary>
        /// Perform Activity with partial resources available.
        /// </summary>
        [JsonIgnore]
        public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnInitialiseActivity(object sender, EventArgs e)
        {
            InitialiseHerd(true, true);
            manureStore = Resources.FindResourceType<ProductStore, ProductStoreTypeManure>(this, "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        }

        /// <summary>Function to handle last of pregnancy in the time step before births occur</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBeforeBreeding")]
        private void OnCLEMPregnancyEnergy(object sender, EventArgs e)
        {
            CalculateHerdPregnancyEnergy(CurrentHerd(false));
        }

        /// <summary>
        /// Method to calculate pregnancy energy for the time step to ensure done before births in CLEMAnimalBreeding.
        /// </summary>
        /// <param name="herd">Enumerable of individuals to consider</param>
        public void CalculateHerdPregnancyEnergy(IEnumerable<Ruminant> herd)
        {
            foreach (RuminantFemale female in herd.OfType<RuminantFemale>().Where(a => a.IsMature))
            {
                // 0 returned if not pregnant and Energy.Fetus is reset here rather than in Energy.Reset as this needs to be available for energy use later this time step after potential intake resets all stores.
                female.Energy.ForFetus = CalculatePregnancyEnergy(female) * ((double)female.DaysPregnantInTimeStep/ events.Interval);
            }
        }

        /// <summary>Function to naturally wean individuals on CLEMAnimalBreeding so will beat wean activity on CLEMAnimalBreeding.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBreeding")]
        private void OnCLEMNaturallyWean(object sender, EventArgs e)
        {
            // Natural weaning takes place here before animals eat or take milk from mother.
            // either the suckling has reached natural weaning age specified or default to gestation length, or the mother's milk production has stopped.
            foreach (var ind in CurrentHerd(false).Where(a => a.IsWeaned == false && (MathUtilities.IsGreaterThanOrEqual(a.AgeInDays, a.AgeToWeanNaturally) || ((a.Mother?.IsLactating??false) == false) )))
            {
                DateTime weanDate = default;
                if (ind.AgeInDays > ind.AgeToWeanNaturally)
                {
                    weanDate = ind.Parameters.Details.CurrentTimeStep.TimeStepStart.AddDays(ind.AgeInDays - ind.AgeToWeanNaturally);
                }
                else
                {
                    if (ind.Mother is null)
                    {
                        weanDate = ind.Parameters.Details.CurrentTimeStep.TimeStepStart;
                    }
                    else
                    {
                        weanDate = ind.Mother.DateOfLastConception.AddDays(ind.Parameters.Lactation.MilkingDays);
                    }
                }

                ind.Wean(true, "Natural", weanDate);
                // report wean. If mother has died create temp female with the mother's ID for reporting only
                ind.Parameters.Details.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.MotherID), weanDate, ind));
            }
        }

        /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPotentialIntake")]
        private void OnCLEMPotentialIntake(object sender, EventArgs e)
        {
            CalculateHerdPotentialIntake(CurrentHerd(false));
        }

        /// <summary>
        /// Method to calculate potential intake for the entire herd for the time step.
        /// </summary>
        /// <param name="herd">Enumerable of individuals to consider</param>
        public static void CalculateHerdPotentialIntake(IEnumerable<Ruminant> herd)
        {
            foreach (var ruminant in herd.Where(a => a.IsSuckling == false))
            {
                PotentialIntake(ruminant);
                if (ruminant is RuminantFemale female)
                {
                    foreach (var suckling in female.SucklingOffspringList)
                    {
                        PotentialIntake(suckling);
                    }
                }
            }
        }

        /// <summary>
        /// Method to calculate potential intake for an individual for the time step.
        /// </summary>
        /// <param name="ind">Individual ruminant to consider</param>
        public static void PotentialIntake(Ruminant ind)
        {
            ind.Intake.SolidsDaily.Reset();
            ind.Intake.MilkDaily.Reset(ind.IsSuckling);
            ind.Weight?.TimeStepReset();
            ind.Energy?.TimeStepReset();

            CalculatePotentialIntake(ind);

            ind.Intake.Reset();
            ind.Energy.Reset();
            ind.Output.Reset();
        }

        /// <summary>
        /// Method to calculate an individual's potential intake for the time step scaling for condition, young age, and lactation.
        /// </summary>
        /// <param name="ind">Individual for which potential intake is determined.</param>
        public static void CalculatePotentialIntake(Ruminant ind)
        {
            // Equation 3 ==================================================
            double cf = 1.0;
            if (ind.Parameters.GrowPF_CI.RelativeConditionEffect_CI20 > 1 && ind.Weight.RelativeCondition > 1)
            {
                cf = 0;
                if (ind.Weight.RelativeCondition < ind.Parameters.GrowPF_CI.RelativeConditionEffect_CI20)
                {
                    cf = Math.Min(1.0, ind.Weight.RelativeCondition * Math.Max(0, ind.Parameters.GrowPF_CI.RelativeConditionEffect_CI20 - ind.Weight.RelativeCondition) / (ind.Parameters.GrowPF_CI.RelativeConditionEffect_CI20 - 1));
                }
            }

            // Equation 4 ================================================== The proportion of solid intake sucklings have as function of age.
            double yf = 1.0;
            if (!ind.IsWeaned)
            {
                // expected milk and mother's milk production has been determined in CalculateLactationEnergy of the mother before getting here.
                double predictedIntake = Math.Min(ind.Intake.MilkDaily.Expected, ind.Mother.Milk.ProductionRate / ind.Mother.Milk.EnergyContent / ind.Mother.NumberOfSucklings);
                yf = (1 - (predictedIntake / ind.Intake.MilkDaily.Expected)) / (1 + Math.Exp(-ind.Parameters.GrowPF_CI.RumenDevelopmentCurvature_CI3 *(ind.AgeInDays + (ind.DaysInTimeStep / 2.0) - ind.Parameters.GrowPF_CI.RumenDevelopmentAge_CI4))); 
                // ToDo: reduce if only unweaned for proportion of time-step.
            }

            // Equations 5-7  ==================================================  Temperature factor. NOT INCLUDED
            double tf = 1.0;

            // Equations 8-13 ==================================================  Lactation factor. Increased intake for lactation demand.
            double lf = 1.0;
            if(ind is RuminantFemale female)
            {
                if (female.IsLactating)
                {
                    // ToDo: check turn off cf is appropriate. cf was reducing more than the lactation increase.
                    //cf = 1.0;

                    // age of young (Ay) is the same as female DaysLactating
                    // Equation 9  ==================================================
                    double dayOfLactation = female.DaysLactating(true);
                    double mi = dayOfLactation / ind.Parameters.GrowPF_CI.PeakLactationIntakeDay_CI8;
                    double lb = 1.0;
                    // Equation 12  ==================================================
                    double wl = ind.Weight.RelativeSize * ((female.RelativeConditionAtParturition - ind.Weight.RelativeCondition) / dayOfLactation);
                    // Equation 11  ==================================================
                    if (dayOfLactation >= ind.Parameters.Lactation.MilkPeakDay && wl > ind.Parameters.GrowPF_CI.LactationConditionLossThresholdDecay_CI14 * Math.Exp(-Math.Pow(ind.Parameters.GrowPF_CI.LactationConditionLossThreshold_CI13 * dayOfLactation, 2.0)))
                    {
                        lb = 1 - ((ind.Parameters.GrowPF_CI.LactationConditionLossAdjustment_CI12 * wl) / ind.Parameters.GrowPF_CI.LactationConditionLossThreshold_CI13);
                    }
                    if (female.NumberOfSucklings > 0)
                    {
                        // todo: trap for suckings not yet defined in month of birth but needed to calculate peak lactation. Assume 1 individual in the first few days if this is the case.
                        lf = 1 + ind.Parameters.GrowPF_CI.PeakLactationIntakeLevel_CI19[Math.Max(female.NumberOfSucklings - 1, 0)] * Math.Pow(mi, ind.Parameters.GrowPF_CI.LactationResponseCurvature_CI9) * Math.Exp(ind.Parameters.GrowPF_CI.LactationResponseCurvature_CI9 * (1 - mi)); // SCA Eq.8
                        // Equation 10 ==================================================
                        lf *= lb * (1 - ind.Parameters.GrowPF_CI.ConditionAtParturitionAdjustment_CI15 + ind.Parameters.GrowPF_CI.ConditionAtParturitionAdjustment_CI15 * female.RelativeConditionAtParturition);
                    }
                    else
                    {
                        // Equation 13  ================================================== non suckling - e.g. dairy.
                        lf *= lb * (1 + ind.Parameters.GrowPF_CI.EffectLevelsMilkProdOnIntake_CI10 * ((ind.Parameters.Lactation.MilkPeakYield - ind.Parameters.GrowPF_CI.BasalMilkRelSRW_CI11 * ind.Weight.StandardReferenceWeight) / (ind.Parameters.GrowPF_CI.BasalMilkRelSRW_CI11 * ind.Weight.StandardReferenceWeight)));
                    }

                    // decrease extra intake needed based on lactation for only a portion of the time-step
                    if (female.DaysLactatingInTimeStep < ind.Parameters.Details.CurrentTimeStep.Interval)
                    {
                        double increase_component = lf - 1;
                        lf = 1.0 + (female.DaysLactatingInTimeStep * 1.0 / ind.Parameters.Details.CurrentTimeStep.Interval)*increase_component;
                    }

                    // calculate estimated milk production for time step here so known before suckling potential intake determined.
                    _ = CalculateLactationEnergy(female, Math.Min(female.DaysLactatingInTimeStep, ind.Parameters.Details.CurrentTimeStep.Interval), false);
                }
                else
                    female.Milk.Reset();
            }

            // Equation 2     ==================================================
            ind.Intake.SolidsDaily.MaximumExpected = Math.Max(0.0, ind.Parameters.GrowPF_CI.RelativeSizeScalar_CI1 * ind.Weight.StandardReferenceWeight * ind.Weight.RelativeSize * (ind.Parameters.GrowPF_CI.RelativeSizeQuadratic_CI2 - ind.Weight.RelativeSize));
            ind.Intake.SolidsDaily.Expected = ind.Intake.SolidsDaily.MaximumExpected * cf * yf * tf * lf;
        }

        /// <summary>Function to calculate growth of herd for the time-step</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            Status = CalculateHerdWeightGain(CurrentHerd(false));
        }

        /// <summary>
        /// Method to calculate weight gain for the entire herd for the time step.
        /// </summary>
        /// <param name="herd">Enumerable of individuals to consider</param>
        public ActivityStatus CalculateHerdWeightGain(IEnumerable<Ruminant> herd)
        {
            ActivityStatus status = ActivityStatus.NotNeeded;
            foreach (var ruminant in herd.Where(a => a.IsSuckling == false))
            {
                status = ActivityStatus.Success;
                if (ruminant is RuminantFemale female)
                {
                    AnimalWeightGain(ruminant, female.IsLactating);

                    foreach (var suckling in female.SucklingOffspringList)
                    {
                        AnimalWeightGain(suckling);
                    }
                }
                else
                {
                    AnimalWeightGain(ruminant);
                }
            }
            if (ReportUnfed)
            {
                ReportUnfedIndividualsWarning(CurrentHerd(false), Warnings, Summary, this, events);
            }
            return status;
        }

        private void AnimalWeightGain(Ruminant ind, bool lactating = false)
        {
            // Adjusting potential intake for digestibility of fodder is now done in RuminantIntake along with concentrates and fodder.
            ind.Intake.AdjustIntakeBasedOnFeedQuality(lactating, ind);
            ind.Intake.UpdateGutFill();
            CalculateEnergy(ind);
        }

        /// <summary>
        /// Calculate growth efficiency based on lactation
        /// </summary>
        /// <param name="ind"></param>
        /// <returns></returns>
        private static void CalculateGrowthEfficiency(Ruminant ind)
        {
            // eq 36
            if (ind.Energy.ForLactation > 0)
            {
                if (ind.Energy.AfterWool >= 0)
                {
                    ind.Energy.Kg = 0.95 * ind.Energy.Kl;
                }
                else
                {
                    ind.Energy.Kg = ind.Energy.Kl / 0.84; // represents tissue mobilisation
                }
            }
            else
            {
                if (ind.Energy.AfterWool >= 0)
                {
                    ind.Energy.Kg = 0.042 * ind.Intake.MDSolid + 0.006;
                }
                else
                {
                    ind.Energy.Kg = ind.Energy.Km / 0.8; // represents tissue mobilisation
                }
            }
        }

        /// <summary>
        /// Function to calculate energy from intake and subsequent growth.
        /// </summary>
        /// <remarks>
        /// All energy calculations are per day and multiplied at end to give weight gain for time-step (e.g. monthly). 
        /// </remarks>
        /// <param name="ind">Individual ruminant for calculation.</param>
        public void CalculateEnergy(Ruminant ind)
        {
            // The feed quality measures are provided in IFeedType and FoodResourcePackets
            // The individual tracks the quality of mixed feed types based on broad type (concentrate, hay or silage, temperate pasture, tropical pasture, or milk) in Ruminant.Intake
            // Energy metabolic - have DMD, fat content, % CP as inputs from ind as supplement and forage, do not need ether extract (fat) for forage

            double sexEffectME = 1;
            // Sme 1.15 for all non-castrated males.
            if (ind.IsWeaned && ind.Sex == Sex.Male && !ind.IsSterilised)
            {
                sexEffectME = 1.15;
            }

            int daysInTimeStep = ind.DaysInTimeStep;

            // Equation 33    ==================================================
            ind.Energy.Km = 0.02 * ind.Intake.MDSolid + 0.5;

            if (ind.IsWeaned)
            {
                CalculateMaintenanceEnergy(ind, ind.Energy.Km, sexEffectME);

                if (ind is RuminantFemale female)
                {
                    // energy for fetus is now calculated in previous event to ensure it happens prior to time step births to account for energy;
                    ind.Energy.ForLactation = CalculateLactationEnergy(female, Math.Min(female.DaysLactatingInTimeStep, events.Interval)) * ((double)female.DaysLactatingInTimeStep / events.Interval);
                }
            }
            else // Unweaned
            {
                // Unweaned individuals are assumed to be suckling as natural weaning rate set regardless of inclusion of a wean activity.
                // Unweaned individuals without mother will automatically wean and survive on feed defined from PotentialIntake and the young factor (yf).

                // recalculate milk intake based on mothers updated milk production for the time step using the previous monthly potential milk intake
                if(ind.Mother is not null)
                {
                    double received = Math.Min(ind.Intake.MilkDaily.Expected, ind.Mother.Milk.ProductionRate / ind.Mother.Milk.EnergyContent / ind.Mother.SucklingOffspringList.Count);
                    milkPacket.MetabolisableEnergyContent = ind.Parameters.GrowPF_CKCL.EnergyContentMilk_CL6;
                    milkPacket.CrudeProteinPercent = ind.Parameters.GrowPF_CKCL.ProteinPercentMilk_CL15;
                    milkPacket.Amount = received;
                    ind.Mother.Milk.Take(received * Math.Min(ind.DaysSucklingInTimeStep, Math.Min(ind.AgeInDays, events.Interval)), MilkUseReason.Suckling);
                    ind.Intake.AddFeed(milkPacket);
                }

                double milkIntakeME = ind.Intake.MilkME;
                double solidsIntakeME = ind.Intake.SolidsME;
                double kml = 1;
                if (MathUtilities.IsPositive(milkIntakeME + solidsIntakeME))
                { 
                    ind.Energy.Kg = (0.006 + ind.Intake.MDSolid * 0.042) + 0.7 * (milkIntakeME / (milkIntakeME + solidsIntakeME)); // MJ milk/MJ total intake;
                }
                if (MathUtilities.IsPositive(ind.Intake.MilkDaily.Actual + ind.Intake.SolidsDaily.Actual))
                {
                    kml = ((milkIntakeME * 0.85) + (solidsIntakeME * ind.Energy.Km)) / (milkIntakeME + solidsIntakeME);
                    ind.Energy.Kg = ((milkIntakeME * 0.7) + (solidsIntakeME * ind.Energy.Kg)) / (milkIntakeME + solidsIntakeME);
                }

                CalculateMaintenanceEnergy(ind, kml, sexEffectME);
            }
            // Equation 45    ==================================================
            // adjusted feeding level is FeedingLevel (already includes a -1) - 1 as used in following equations.
            double adjustedFeedingLevel = (ind.Energy.FromIntake / ind.Energy.ForMaintenance) - 2;

            //TODO: add draught individual energy requirement: does this also apply to unweaned individuals? If so move outside loop

            // Equations 46-49   ==================================================
            var milkStore = ind.Intake.GetStore(FeedType.Milk);
            ind.Weight.Protein.ForEndogenousUrinary = ind.Parameters.GrowPF_CM.BreedEUPFactor1_CM12 * Math.Log(ind.Weight.Base.Amount) - ind.Parameters.GrowPF_CM.BreedEUPFactor2_CM13;
            ind.Weight.Protein.ForEndogenousFaecal = 0.0152 * ind.Intake.SolidIntake + (ind.Parameters.GrowPF_CM.EFPFromMilkDiet_CM11 * milkStore?.ME??0);
            ind.Weight.Protein.ForDermal = ind.Parameters.GrowPF_CM.DermalLoss_CM14 * Math.Pow(ind.Weight.Base.Amount,0.75);
            // digestible protein leaving stomach from milk
            double DPLSMilk = milkStore?.CrudeProtein??0 * 0.92;

            // Equation 103   ================================================== efficiency of using DPLS
            ind.Intake.kDPLS = (ind.IsWeaned)? ind.Parameters.GrowPF_CG.EfficiencyOfDPLSUseFromFeed_CG2: ind.Parameters.GrowPF_CG.EfficiencyOfDPLSUseFromFeed_CG2 / (1 + ((ind.Parameters.GrowPF_CG.EfficiencyOfDPLSUseFromFeed_CG2 / ind.Parameters.GrowPF_CG.EfficiencyOfDPLSUseFromMilk_CG3) -1)*(DPLSMilk / ind.Intake.DPLS) ); //EQn 103

            // Wool production
            CalculateWool(ind, daysInTimeStep);

            if (ind.IsWeaned)
            {
                CalculateGrowthEfficiency(ind);
            }

            double relativeSizeForWeightGainPurposes = Math.Min(1 - ((1 - (ind.Weight.AtBirth/ind.Weight.StandardReferenceWeight)) * Math.Exp(-(ind.Parameters.General.AgeGrowthRateCoefficient_CN1 * ind.AgeInDays) / Math.Pow(ind.Weight.StandardReferenceWeight, ind.Parameters.General.SRWGrowthScalar_CN2))), (ind.Weight.HighestBaseAttained / ind.Weight.StandardReferenceWeight));
            double sizeFactor1ForGain = 1 / (1 + Math.Exp(-ind.Parameters.GrowPF_CG.GainCurvature_CG4 * (relativeSizeForWeightGainPurposes - ind.Parameters.GrowPF_CG.GainMidpoint_CG5)));
            double sizeFactor2ForGain = Math.Max(0, Math.Min(((relativeSizeForWeightGainPurposes - ind.Parameters.GrowPF_CG.ConditionNoEffect_CG6) / (ind.Parameters.GrowPF_CG.ConditionMaxEffect_CG7 - ind.Parameters.GrowPF_CG.ConditionNoEffect_CG6)), 1));
            // Note: the use of ZF2 in proteinContentOfGain has been changed to -ve as opposed to the incorrect +ve in documentation (J.Dougherty, CSIRO, 21/1/2025)

            // determine if body protein is below expected for age to adjust protein content of gain for recovery of protein
            ind.Weight.Protein.Normal = ind.Weight.Protein.MassAtSRW * relativeSizeForWeightGainPurposes;
            ind.Weight.Protein.NormalShortfall = Math.Max(0, ind.Weight.Protein.Normal - ind.Weight.Protein.Amount);

            // Equation 102, 104 & 105   =======================================
            // Equation 102 - PG1 and PG2 protein available from diet after accounting for maintenance and conceptus and milk
            // rewritten 102 to be algebraically simple. UsableDPSL = (kdpls * dpls) and wool adjusted.  
            double proteinAvailableForGainFromIntake = ind.Intake.UsableDPLS - ind.Weight.Protein.BeforeGrowth - (ind.Intake.kDPLS * ind.Weight.Protein.ForWool / ind.Parameters.GrowPF_CG.EfficiencyOfDPLSUseForWool_CG1);
            // Equation 104  units = mj/kg gain
            double energyEmptyBodyGain = (ind.Parameters.GrowPF_CG.GrowthEnergyIntercept1_CG8b + adjustedFeedingLevel) + sizeFactor1ForGain * (ind.Parameters.GrowPF_CG.GrowthEnergyIntercept2_CG9 - adjustedFeedingLevel) + sizeFactor2ForGain * (13.8 * (ind.Weight.RelativeCondition - 1));
            
            // Equation 105  units = kg protein/kg gain
            double proteinContentOfGain = (ind.Parameters.GrowPF_CG.ProteinGainIntercept1_CG12b - (ind.Parameters.GrowPF_CG.ProteinGainSlope1_CG14b * adjustedFeedingLevel)) - sizeFactor1ForGain * (ind.Parameters.GrowPF_CG.ProteinGainIntercept2_CG13 - (ind.Parameters.GrowPF_CG.ProteinGainSlope1_CG14b * adjustedFeedingLevel)) + (sizeFactor2ForGain * ind.Parameters.GrowPF_CG.ProteinGainSlope2_CG15 * (ind.Weight.RelativeCondition - 1));

            // Equations 101 & 109  ===============================================
            // Equation 101 EG1 and EG2  units MJ tissue gain/kg ebg
            double energyAvailableForGain = ind.Energy.AvailableForGain * ind.Energy.Kg;
            if (MathUtilities.IsPositive(energyAvailableForGain))
            {
                energyAvailableForGain *= ind.Parameters.GrowPF_CG.BreedGrowthEfficiencyScalar;
            }
            // Equation 109  - the amount of protein required for the growth based on energy available

            // todo: ensure we are not double dipping here.
            double proteinNeededForGrowthAllowableByEnergy = Math.Max(0.0, proteinContentOfGain * (energyAvailableForGain / energyEmptyBodyGain));

            ind.Weight.Protein.ForGain = proteinNeededForGrowthAllowableByEnergy;
            ind.Weight.Protein.AvailableForGain = proteinAvailableForGainFromIntake;

            AdjustLactationForProteinDeficit(ind, ind.Weight.Protein.Normal, ref proteinAvailableForGainFromIntake, energyEmptyBodyGain, proteinContentOfGain, ref energyAvailableForGain, ref proteinNeededForGrowthAllowableByEnergy);

            // Fat and Protein change - Dougherty et al 2024 ========================================
            // Departure from Freer 2012 to allow for fat and protein change to be calculated separately to derive ebm change.
            // there is no max daily protein gain other than what will come from feed so protein from intake is the limiter and no accounting of extremely high CP content of diet.
            // Cannot use body fat to put on additional protein up to shortfall

            double kgProteinGrowth = 0;

            // 1. if protein from intake available AND insufficient energy to grow protein to normal limit AND lactating, mobilise fat to provide energy to grow protein from diet

            double proteinToMeetNormal = Math.Min(ind.Weight.Protein.NormalShortfall, Math.Max(0.0, proteinAvailableForGainFromIntake));
            double energyNeededToMeetNormal = proteinToMeetNormal * ind.Parameters.General.MJEnergyPerKgProtein;
            double energyShortfall = Math.Min(0, Math.Max(energyAvailableForGain - energyNeededToMeetNormal, energyNeededToMeetNormal * -1));
            double efficiencyToGetEnergy = Math.Min(1.0, ind.Energy.Km / 0.8);
            // previously only for lactating females.
            // if (ind is RuminantFemale indFemale && MathUtilities.IsNegative(energyShortfall) && MathUtilities.IsPositive(ind.Weight.Protein.ForLactation))
            if (MathUtilities.IsNegative(energyShortfall))
            {
                double fatEnergyRemaining = ind.Energy.Fat.Amount;
                if (MathUtilities.IsNegative(energyAvailableForGain))
                {
                    fatEnergyRemaining = Math.Max(0, ind.Energy.Fat.Amount + energyAvailableForGain);
                }

                energyShortfall = Math.Min(Math.Abs(energyShortfall), fatEnergyRemaining); // * indFemale.DaysLactatingInTimeStep / ind.Parameters.Details.CurrentTimeStep.Interval;
                ind.Weight.Fat.MobiliseAmount(energyShortfall / ind.Parameters.General.MJEnergyPerKgFat, efficiencyToGetEnergy, MobilisationReasonType.EnergyForProtein);
                double energyProvided = ind.Energy.Fat.MobiliseAmount(energyShortfall, efficiencyToGetEnergy, MobilisationReasonType.EnergyForProtein);
                energyAvailableForGain += energyProvided;
            }

            // 2. Grow protein up to normal shortfall first using available energy

            if (MathUtilities.IsPositive(energyAvailableForGain))
            {
                double energyToUse = Math.Max(0.0, Math.Min(energyAvailableForGain, energyNeededToMeetNormal));
                energyAvailableForGain = MathUtilities.RoundToZero(energyAvailableForGain - energyToUse, 1e-5);
                double proteinAdded = energyToUse / ind.Parameters.General.MJEnergyPerKgProtein;
                kgProteinGrowth += proteinAdded;
                //proteinToMeetNormal = MathUtilities.RoundToZero(proteinToMeetNormal - proteinAdded, 1e-5);
                proteinAvailableForGainFromIntake = MathUtilities.RoundToZero(proteinAvailableForGainFromIntake - proteinAdded, 1e-5);
            }

            ind.Energy.ForGain = energyAvailableForGain;

            // 3. handle protein deficit
            // allow protein to be taken from the body stores (efficiency similar to kdpls of 0.7) to handle deficit for maintenance, pregnancy and wool
            // any deficit should have been reduced by lactation reduction where possible
            if (MathUtilities.IsNegative(proteinAvailableForGainFromIntake))
            {
                ind.Weight.Protein.MobiliseAmountNeeded(Math.Abs(proteinAvailableForGainFromIntake), 0.7, MobilisationReasonType.ProteinShortfall);
                ind.Energy.Protein.MobiliseAmountNeeded(Math.Abs(proteinAvailableForGainFromIntake) * ind.Parameters.General.MJEnergyPerKgProtein, 0.7, MobilisationReasonType.ProteinShortfall);
                kgProteinGrowth = 0;
            }

            double MJFatChange = energyAvailableForGain;

            // 4. account for provision of energy in deficit from body protein and fat based on the percent empty body fat (slope and intercept)
            if (MathUtilities.IsNegative(energyAvailableForGain))
            {
                double propFatForEnergy = Math.Min(1.0, ind.Parameters.GrowPF_CG.FatPercentToProportionEDeficitTakenFromBodyFatIntercept + ind.Parameters.GrowPF_CG.FatPercentToProportionEDeficitTakenFromBodyFatSlope * ind.Weight.EBF);
                //todo: make sure this is actually needed... for lactating females or this may have been the very bad initial slope and intercept used in dev check
                double fatEnergyMobilised = Math.Abs(energyAvailableForGain * propFatForEnergy);
                double proteinEnergyMobilised = Math.Abs(energyAvailableForGain + fatEnergyMobilised);

                ind.Energy.Fat.MobiliseAmountNeeded(fatEnergyMobilised, efficiencyToGetEnergy, MobilisationReasonType.EnergyShortfall);
                ind.Energy.Protein.MobiliseAmount(proteinEnergyMobilised, efficiencyToGetEnergy, MobilisationReasonType.EnergyShortfall);
                ind.Weight.Fat.MobiliseAmountNeeded(fatEnergyMobilised / ind.Parameters.General.MJEnergyPerKgFat, efficiencyToGetEnergy, MobilisationReasonType.EnergyShortfall);
                ind.Weight.Protein.MobiliseAmountNeeded(proteinEnergyMobilised / ind.Parameters.General.MJEnergyPerKgProtein, efficiencyToGetEnergy, MobilisationReasonType.EnergyShortfall);
                MJFatChange = 0;
            }

            MJFatChange -= (ind.Energy.Fat.GetTotalMobilisedByReason());
            double MJProteinChange = (kgProteinGrowth * ind.Parameters.General.MJEnergyPerKgProtein) - ind.Energy.Protein.GetTotalMobilisedByReason();

            // protein mass on protein basis not mass of lean tissue mass. use conversion XXXX for weight to perform checksum.
            ind.Energy.Protein.Adjust(MJProteinChange * daysInTimeStep); // for time step
            ind.Weight.Protein.Adjust(ind.Energy.Protein.Change / ind.Parameters.General.MJEnergyPerKgProtein, ind); // for time step

            ind.Energy.Fat.Adjust(MJFatChange * daysInTimeStep); // for time step
            ind.Weight.Fat.Adjust(ind.Energy.Fat.Change / ind.Parameters.General.MJEnergyPerKgFat); // for time step

            ind.Weight.Adjust();

            // Equations 118-120   ==================================================
            ind.Output.NitrogenBalance =  ind.Intake.CrudeProtein/ FoodResourcePacket.FeedProteinToNitrogenFactor - (ind.Weight.Protein.ForLactationActual / FoodResourcePacket.MilkProteinToNitrogenFactor) - ((ind.Weight.Protein.ForPregnancy + ind.Weight.Protein.ForWool + ind.Weight.Protein.Change) / FoodResourcePacket.FeedProteinToNitrogenFactor);
            // Total fecal protein
            ind.Weight.Protein.ForFaecal = ind.Intake.IndigestibleUDP + ind.Parameters.GrowPF_CACRD.FaecalProteinFromMCP_CA8 * ind.Intake.RDPRequired + (1 - ind.Parameters.GrowPF_CACRD.MilkProteinDigestibility_CA5) * milkStore?.CrudeProtein??0 + ind.Weight.Protein.ForEndogenousFaecal;
            // Total urinary protein
            ind.Weight.Protein.ForUrinary = ind.Intake.CrudeProtein - (ind.Weight.Protein.ForPregnancy + ind.Weight.Protein.ForLactationActual + ind.Weight.Protein.Change + ind.Weight.Protein.ForWool) - ind.Weight.Protein.ForFaecal - ind.Weight.Protein.ForDermal;
            ind.Output.NitrogenUrine = ind.Weight.Protein.ForUrinary / 6.25 * daysInTimeStep;
            ind.Output.NitrogenFaecal = ind.Weight.Protein.ForFaecal / 6.25 * daysInTimeStep;

            // Do check against NBal gain to TFP and TUP
            if (Math.Abs(ind.Output.NitrogenBalance - ind.Weight.Protein.ForFaecal - ind.Weight.Protein.ForUrinary) / ind.Output.NitrogenBalance > 0.05)
            {
                string warn = $"Cross-check: Ruminant [{ind.Breed}] nitrogen balance differs from TFP plus TUP by more then 5%.{Environment.NewLine}[a={NameWithParent}]";
                string warningString = $"Cross-check: Ruminant [{ind.Breed}] nitrogen balance differs from TFP plus TUP by more then 5%.{Environment.NewLine}[a={NameWithParent}], TimeStep:[{events.IntervalIndex},{events.Clock.Today:yyyy-MM-dd}], Individual:[{ind.ID}].{Environment.NewLine}This advice is for advanced users and breed developers. Seek advice from CLEM developers.";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warningString);
            }

            // manure per time step
            ind.Output.Manure = ind.Intake.SolidsDaily.Actual * (100.0 - ind.Intake.DMD) / 100.0 * daysInTimeStep;
        }

        private void AdjustLactationForProteinDeficit(Ruminant ind, double proteinNormal, ref double proteinAvailableForGainFromIntake, double energyEmptyBodyGain, double proteinContentOfGain, ref double energyAvailableForGain, ref double proteinNeededForGrowthAllowableByEnergy)
        {
            // ignore if protein from intake 0 or positive
            // ignore if Protein.ForLactation is zero (non-lactating females and any males
            if (MathUtilities.IsNegative(proteinAvailableForGainFromIntake) == false || MathUtilities.IsPositive(ind.Weight.Protein.ForLactationActual) == false)
            {
                return;
            }

            var indFemale = ind as RuminantFemale;

            // Mobilise body protein to produce milk when CP shortfall - Dougherty et al 2024 ========================================
            // Departure from Freer 2012 to allow body protein above 75% of normalised protein to be provided to lactation when less than peak milk days.

            // if day of lactation (mid point of time step) < peak lactation 
            if (indFemale.DaysLactating(true) <= ind.Parameters.Lactation.MilkPeakDay)
            {
                // get lactation protein deficit
                double lactationProteinDeficit = Math.Min(indFemale.Weight.Protein.ForLactationActual, Math.Abs(proteinAvailableForGainFromIntake));

                double bodyProteinAvailable = Math.Max(0.0, ind.Weight.Protein.Amount - (proteinNormal * 0.75));

                // get protein required from body
                double bodyProteinTakenForLactation = Math.Min(lactationProteinDeficit / 0.8, bodyProteinAvailable);

                double proteinProvided = ind.Weight.Protein.MobiliseAmount(bodyProteinTakenForLactation, 0.8, MobilisationReasonType.LactationProtein);
                double proteinEnergyProvided = ind.Energy.Protein.MobiliseAmount(bodyProteinTakenForLactation * ind.Parameters.General.MJEnergyPerKgProtein, 0.8, MobilisationReasonType.LactationProtein);

                // reduce CP shortfall by the body protein provided for milk production. This will be removed from the body later in protein and fat updates.
                proteinAvailableForGainFromIntake += proteinProvided;
            }

            // reduce milk production based on any deficit
            // Equations 75-76   ==================================================  Freer et al. (2012) The GRAZPLAN animal biology model
            // Equation 110 Modified  ================

            // if deficit has been fully addressed by body protein mobilisation then no need to reduce lactation
            if (MathUtilities.IsNegative(proteinAvailableForGainFromIntake) == false)
            {
                return;
            }

            double MP = Math.Max(0.0, 1 + (proteinAvailableForGainFromIntake / indFemale.Weight.Protein.ForLactationActual)) * indFemale.Milk.ProductionRate;
            indFemale.Milk.Available = MP * Math.Min(indFemale.DaysLactatingInTimeStep, events.Interval) / indFemale.Milk.EnergyContent;
            indFemale.Milk.Produced = indFemale.Milk.Available;

            indFemale.Weight.Protein.LactationReduction = (indFemale.Milk.ProductionRate - MP) * ((ind.Parameters.GrowPF_CKCL.ProteinPercentMilk_CL15 / 100.0) / indFemale.Milk.EnergyContent);

            indFemale.Milk.ProductionRate = MP;
            indFemale.Milk.ProductionRatePrevious = MP;

            indFemale.Weight.Protein.ForLactationActual = (indFemale.Parameters.GrowPF_CKCL.ProteinPercentMilk_CL15 / 100.0) * (MP / indFemale.Milk.EnergyContent);

            // Equation 75  ================
            ind.Energy.ForLactation = MP / (0.94 * ind.Energy.Kl) * ind.Parameters.GrowPF_CG.BreedLactationEfficiencyScalar;

            // if lactation has been turned off due to protein deficit, then we need the other kg (efficiency of gain)
            CalculateGrowthEfficiency(ind);
            energyAvailableForGain = ind.Energy.AvailableForGain * ind.Energy.Kg; // AvailableForGain includes any energy used to mobilise any protein for lactation.
            if (MathUtilities.IsPositive(energyAvailableForGain))
            {
                energyAvailableForGain *= ind.Parameters.GrowPF_CG.BreedGrowthEfficiencyScalar;
            }

            // Equation 111  ================ Adjusted NEG1 based on the protein saved from reduced milk
            proteinNeededForGrowthAllowableByEnergy = proteinContentOfGain * (energyAvailableForGain / energyEmptyBodyGain); // this actually reduces the energy deficit as energyForGain is -ve or up to zero
            // Equation 112  ================
            // Here we adjust proteinAvailableForGain (PG1) rather than use PG2 from report as we can do these equations where Female object known in this if statement and Pg2 is set to PG1 if there are no lactation limits.
            proteinAvailableForGainFromIntake += indFemale.Weight.Protein.LactationReduction;
        }

        /// <summary>
        /// Calculate maintenance energy and reduce intake based on any rumen protein deficiency
        /// </summary>
        /// <param name="ind">The individual ruminant.</param>
        /// <param name="km">The maintenance efficiency to use</param>
        /// <param name="sexEffect">The sex effect to apply</param>
        private static void CalculateMaintenanceEnergy(Ruminant ind, double km, double sexEffect)
        {
            int maxReductionAllowed = 3;

            // Calculate maintenance energy then determine the protein requirement of rumen bacteria
            // Adjust intake proportionally and recalculate maintenance energy with adjusted intake energy
            km *= ind.Parameters.GrowPF_CG.BreedMainenanceEfficiencyScalar;

            // Note: Energy.ToMove and Energy.ToGraze are calculated in Grazing and Move activities.
            // Note: Energy.ToMove DOES NOT currently include terrain and slope as per AusFarm.
            ind.Energy.ToMove /= km;
            ind.Energy.ToGraze /= km;

            int reductionCount = 0;
            double rumenDegradableProteinIntake = 0;
            double rumenDegradableProteinRequirement = 1;
            while (rumenDegradableProteinIntake < rumenDegradableProteinRequirement && reductionCount < maxReductionAllowed)
            {
                //ToDo: include adjustment onto maintenance for activity level which should be an additional 5-25% depending on walking and terrain requirements.
                ind.Energy.ForBasalMetabolism = ((ind.Parameters.GrowPF_CM.FHPScalar_CM2 * sexEffect * Math.Pow(ind.Weight.Base.Amount, 0.75)) * Math.Max(Math.Exp(-ind.Parameters.GrowPF_CM.MainExponentForAge_CM3 * ind.AgeInDays), ind.Parameters.GrowPF_CM.AgeEffectMin_CM4) * (1.0 + ind.Parameters.GrowPF_CM.MilkScalar_CM5 * ind.Intake.ProportionMilk)) / km;
                ind.Energy.ForHPViscera = ind.Parameters.GrowPF_CM.HPVisceraFL_CM1 * ind.Energy.FromIntake;

                double adjustedFeedingLevel = -1;
                if (MathUtilities.GreaterThan(ind.Energy.FromIntake, 0, 2) & MathUtilities.GreaterThan(ind.Energy.ForMaintenance, 0, 2))
                {
                    adjustedFeedingLevel = (ind.Energy.FromIntake / ind.Energy.ForMaintenance) - 1;
                }
                rumenDegradableProteinIntake = CalculateRumenDegradableProteinIntake(ind, adjustedFeedingLevel);
                rumenDegradableProteinRequirement = (ind.Parameters.GrowPF_CACRD.RumenDegradableProteinIntercept_CRD4 + ind.Parameters.GrowPF_CACRD.RumenDegradableProteinSlope_CRD5 * (1 - Math.Exp(-ind.Parameters.GrowPF_CACRD.RumenDegradableProteinExponent_CRD6 *
                    (adjustedFeedingLevel + 1)))) * ind.Intake.FMEI;

                if (ind.Parameters.GrowPF_CI.IntakeReductionFromInsufficientRDPIntake == 0)
                {
                    // if intake reduction turned off we can break from the loop after the Energy.ForBasalMetabolism and ForHPViscera has been calculated for the time step
                    break;
                }

                // reduce intake based on rumen degradable protein limitations.
                
                // ToDo: JD? this next bit gets a proportion of 2.25 but handled down the chain to not cause any loss
                if (rumenDegradableProteinIntake < rumenDegradableProteinRequirement)
                {
                    ind.Intake.ReduceIntakeByProportion((1 - rumenDegradableProteinIntake / rumenDegradableProteinRequirement) * ind.Parameters.GrowPF_CI.IntakeReductionFromInsufficientRDPIntake);
                    reductionCount++;
                }
                else
                {
                    reductionCount = int.MaxValue;
                }
            }

            ind.Intake.CalculateDigestibleProteinLeavingStomach(rumenDegradableProteinRequirement, ind.Parameters.GrowPF_CACRD.MilkProteinDigestibility_CA5);
        }

        private static double CalculateRumenDegradableProteinIntake(Ruminant ind, double feedingLevel)
        {
            //// Ignored from GrassGro: timeOfYearFactor 19/9/2023 as calculation showed it has very little effect compared with the error in parameterisation and tracking of feed quality and a monthly time step
            //// Ignored from GrassGro latitude factor for now.
            //// double timeOfYearFactorRDPR = 1 + rumenDegradableProteinTimeOfYear * latitude / 40 * Math.Sin(2 * Math.PI * dayOfYear / 365); //Eq.(52)
            
            double rdpi = 0;
            if (feedingLevel <= 0)
            {
                foreach (var store in ind.Intake.GetAllStores.Where(a => a.Key != FeedType.Milk))
                {
                    rdpi += store.Value.DegradableCrudeProtein;
                }
            }
            else
            {
                foreach (var store in ind.Intake.GetAllStores.Where(a => a.Key != FeedType.Milk))
                {
                    switch (store.Key)
                    {
                        case FeedType.Concentrate:
                            rdpi += (1 - ind.Parameters.GrowPF_CACRD.RumenDegradabilityConcentrateSlope_CRD3 * feedingLevel) * store.Value.DegradableCrudeProtein;
                            break;
                        case FeedType.PastureTropical:
                        case FeedType.PastureTemperate:
                        case FeedType.HaySilage:
                            rdpi += (1 - (ind.Parameters.GrowPF_CACRD.RumenDegradabilityIntercept_CRD1 - ind.Parameters.GrowPF_CACRD.RumenDegradabilitySlope_CRD2 * (store.Value.Details?.DryMatterDigestibility ?? 0) / 100.0) * feedingLevel) * store.Value.DegradableCrudeProtein; 
                            break;
                        default:
                            break;
                    }
                }
            }
            return rdpi;
        }

        /// <summary>
        /// Determine the energy required for wool growth.
        /// </summary>
        /// <param name="ind">Ruminant individual.</param>
        /// <param name="daysGrowth">Number of days growth</param>
        /// <returns>Daily energy required for wool production time step.</returns>
        private static void CalculateWool(Ruminant ind, int daysGrowth)
        {
            if (!ind.Parameters.General.IncludeWool)
            {
                return;
            }

            // age factor for wool
            double ageFactorWool = ind.Parameters.GrowPF_CW.WoolGrowthProportionAtBirth_CW5 + ((1 - ind.Parameters.GrowPF_CW.WoolGrowthProportionAtBirth_CW5) * (1 - Math.Exp(-1 * ind.Parameters.GrowPF_CW.AgeFactorExponent_CW12 * ind.AgeInDays)));

            double dPLSAvailableForWool = Math.Max(0, ind.Intake.DPLS - (ind.Parameters.GrowPF_CW.PregLactationAdjustment_CW9 * (ind.Weight.Protein.ForPregnancy + ind.Weight.Protein.ForLactationActual)));

            double mEAvailableForWool = Math.Max(0, ind.Energy.AfterLactation); //  Intake.ME - (ind.Energy.ForFetus + ind.Energy.ForLactation));

            double prtWool = ind.Parameters.GrowPF_CW.DPLSLimitationForWoolGrowth_CW7 * (ind.Parameters.GrowPF_CW.StandardFleeceWeight / ind.Weight.StandardReferenceWeight) * ageFactorWool * dPLSAvailableForWool;
            double eWool = ind.Parameters.GrowPF_CW.MEILimitationOnWoolGrowth_CW8 * (ind.Parameters.GrowPF_CW.StandardFleeceWeight / ind.Weight.StandardReferenceWeight) * ageFactorWool * mEAvailableForWool;
            ind.Weight.Wool.ProteinLimited = prtWool - eWool;

            double pwInst = Math.Min(prtWool, eWool);
            // pwToday is either the calculation or 0.04 (CW2) * relative size
            double pwToday = Math.Max(ind.Parameters.GrowPF_CW.BasalCleanWoolGrowth_CW2 * ind.Weight.RelativeSize, (1 - ind.Parameters.GrowPF_CW.LagFactorForWool_CW4) * ind.Weight.WoolClean.Change) + (ind.Parameters.GrowPF_CW.LagFactorForWool_CW4 * pwInst);

            ind.Weight.Wool.Adjust(pwToday / ind.Parameters.GrowPF_CW.CleanToGreasyCRatio_CW3 * daysGrowth );
            ind.Weight.WoolClean.Adjust(pwToday * daysGrowth);
            ind.Weight.Protein.ForWool = pwToday * daysGrowth;

            ind.Energy.ForWool = (ind.Parameters.GrowPF_CW.EnergyContentCleanWool_CW1 * (pwToday - (ind.Parameters.GrowPF_CW.BasalCleanWoolGrowth_CW2 * ind.Weight.RelativeSize)) / ind.Parameters.GrowPF_CW.CleanToGreasyCRatio_CW3) / ind.Energy.Kw;
        }

        /// <summary>
        /// Determine the energy required for lactation.
        /// </summary>
        /// <param name="ind">Female individual.</param>
        /// <param name="timestep">The number of days in current time-step</param>
        /// <param name="updateValues">A flag to indicate whether tracking values should be updated in this calculation as call from PotentialIntake and CalculateEnergy.</param>
        /// <returns>Daily energy required for lactation this time step.</returns>
        private static double CalculateLactationEnergy(RuminantFemale ind, int timestep, bool updateValues = true)
        {
            // This is first called in potential intake using last month's energy available after pregnancy and updateValues = false
            // It is called again in CalculateEnergy of WeightGain where it uses the energy available after intake from the current time step.
            ind.Milk.Milked = 0;
            ind.Milk.Suckled = 0;

            if ((ind.IsLactating | MathUtilities.IsPositive(ind.Milk.PotentialRate)) == false)
            {
                ind.Milk.Reset();
                return 0;
            }

            ind.Energy.Kl = ind.Parameters.GrowPF_CKCL.ELactationEfficiencyCoefficient_CK6 * ind.Intake.MDSolid + ind.Parameters.GrowPF_CKCL.ELactationEfficiencyIntercept_CK5;
            double milkTime = ind.DaysLactating(true);

            // milk quality - will be dynamic with milk days etc when relationships provided
            ind.Milk.EnergyContent = ind.Parameters.GrowPF_CKCL.EnergyContentMilk_CL6;

            // Equations 66-76   ==================================================
            // Equation 74  ===================================================
            double DR = 1.0;
            if (MathUtilities.IsGreaterThan(ind.Milk.MaximumRate, 0.0))
            {
                DR = ind.Milk.ProductionRatePrevious / ind.Milk.MaximumRate; // Maximum rate will also be previous as it is not updated until 2nd calc of lactation.
            }

            // todo: MEForMilkPreviousDay += ind.Energy.ForFetus;

            // Milk lag . LR
            ind.Milk.Lag = (ind.Parameters.GrowPF_CKCL.PotentialYieldReduction2_CL18 * DR) * ((1 - ind.Parameters.GrowPF_CKCL.PotentialYieldReduction2_CL18) * DR);

            double nutritionAfterPeakLactationFactor = 1; // LB
            if (MathUtilities.IsGreaterThan(milkTime, 0.7 * ind.Parameters.Lactation.MilkPeakDay))
            {
                nutritionAfterPeakLactationFactor = ind.Milk.NutritionAfterPeakLactationFactor - (ind.Parameters.GrowPF_CKCL.PotentialYieldReduction_CL17 * (ind.Milk.Lag - DR));
            }

            double Mm = (milkTime + ind.Parameters.Lactation.MilkOffsetDay) / ind.Parameters.Lactation.MilkPeakDay;

            double milkProductionMax = 0;

            // TODO: need to trap for case where partial time-step has lactation and therefore no sucklings exist before birth so use number of fetuses as the daysLactatingInTimeStep have been determined greater than 0
            // this is a problem as it is this activity that sets the newborn expected milk intake, but they haven't been created yet.

            // This currently assumes either suckling offspring or breeder is being milked by an activity and therefore lactating but with no offspring.
            if (ind.NumberOfSucklings != 0)
            {
                milkProductionMax = ind.Parameters.GrowPF_CKCL.PeakYieldScalar_CL0[ind.NumberOfSucklings - 1] * Math.Pow(ind.Weight.StandardReferenceWeight, 0.75) * ind.Weight.RelativeSize * ind.RelativeConditionAtParturition * nutritionAfterPeakLactationFactor *
                Math.Pow(Mm, ind.Parameters.Lactation.MilkCurveSuckling_CL3) * Math.Exp(ind.Parameters.Lactation.MilkCurveSuckling_CL3 * (1 - Mm));
            }
            else
            {
                milkProductionMax = 0.94 * ind.Milk.EnergyContent * ind.Parameters.GrowPF_CKCL.ExpectedPeakYield * ind.RelativeConditionAtParturition * nutritionAfterPeakLactationFactor * Math.Pow(Mm, ind.Parameters.Lactation.MilkCurveNonSuckling_CL4) * Math.Exp(ind.Parameters.Lactation.MilkCurveNonSuckling_CL4 * (1 - Mm));
            }

            double ratioMilkProductionME = 1.0;

            // ToDo: JD? the first part of next if statement uses Energy.ForLactation but this value is being determined by this method so is currently 0 until final value is returned.
            // therefore ratio will always be 0, and ad will always equal milktime.

            if (updateValues)
            {
                // calculated after intake, maintenance and pregnancy
                ratioMilkProductionME = ind.Energy.AfterPregnancy * 0.94 * ind.Energy.Kl * ind.Parameters.GrowPF_CG.BreedLactationEfficiencyScalar / milkProductionMax;
            }
            else
            {   // calculated at start of time step to provide suckling with indication of milk production
                ratioMilkProductionME = (ind.Milk.EnergyForLactationPrevious * 0.94 * ind.Energy.Kl * ind.Parameters.GrowPF_CG.BreedLactationEfficiencyScalar) / milkProductionMax; // Eq. 69
            }
            
            double ad = Math.Max(milkTime, ratioMilkProductionME / (2 * ind.Parameters.GrowPF_CKCL.PotentialYieldLactationEffect2_CL22));

            double MP1 = (ind.Parameters.GrowPF_CKCL.LactationEnergyDeficit_CL7 * milkProductionMax) / 
                (1 + Math.Exp(-(-ind.Parameters.GrowPF_CKCL.PotentialLactationYieldParameter_CL19 + 
                ind.Parameters.GrowPF_CKCL.PotentialYieldMEIEffect_CL20 *
                ratioMilkProductionME + ind.Parameters.GrowPF_CKCL.PotentialYieldLactationEffect_CL21 * ad * (ratioMilkProductionME - ind.Parameters.GrowPF_CKCL.PotentialYieldLactationEffect2_CL22 * ad) 
                - ind.Parameters.GrowPF_CKCL.PotentialYieldConditionEffect_CL23
                * ind.Weight.RelativeCondition * (ratioMilkProductionME - ind.Parameters.GrowPF_CKCL.PotentialYieldConditionEffect2_CL24 * ind.Weight.RelativeCondition))));

            double sucklingExpected = ind.Milk.EnergyContent * Math.Pow(ind.SucklingOffspringList.Average(a => a.Weight.Live), 0.75) * (ind.Parameters.GrowPF_CKCL.MilkConsumptionLimit1_CL12 + (ind.Parameters.GrowPF_CKCL.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.GrowPF_CKCL.MilkConsumptionLimit3_CL14 * milkTime)));
            double MP2 = Math.Min(MP1, ind.NumberOfSucklings * sucklingExpected);

            foreach (var suckling in ind.SucklingOffspringList)
            {
                suckling.Intake.MilkDaily.Expected = sucklingExpected / ind.Milk.EnergyContent;
            }

            if (updateValues)
            {
                ind.Milk.EnergyForLactationPrevious = ind.Energy.AfterPregnancy;
                ind.Milk.NutritionAfterPeakLactationFactor = nutritionAfterPeakLactationFactor;
                ind.Milk.MaximumRate = milkProductionMax;
                ind.Milk.ProductionRatePrevious = MP2;
            }

            // Equation 76  ================================================== 0.032 -- All milk production is in MJ/day
            ind.Weight.Protein.ForLactationActual = (ind.Parameters.GrowPF_CKCL.ProteinPercentMilk_CL15 / 100.0) * MP2 / ind.Milk.EnergyContent;

            ind.Milk.Produced = MP2 * timestep / ind.Milk.EnergyContent;
            ind.Milk.PotentialRate = MP1;
            ind.Milk.ProductionRate = MP2;
            ind.Milk.Available = ind.Milk.Produced;

            // MP2 is MJ going to the suckling(s)
            // so convert to energy required to provide this energy to suckling(s)
            // returns the energy required for milk production (MJ/Day)
            return MP2 / (0.94 * ind.Energy.Kl) * ind.Parameters.GrowPF_CG.BreedLactationEfficiencyScalar;
        }

        /// <summary>
        /// Determine the energy required for pregnancy.
        /// </summary>
        /// <param name="ind">Female individua.l</param>
        /// <returns>Energy required per day for pregnancy</returns>
        private static double CalculatePregnancyEnergy(RuminantFemale ind)
        {
            if (!ind.IsPregnant)
            {
                if (ind.IsLactating)
                {
                    ind.Weight.Conceptus.Set(0);
                }
                return 0;
            }

            double totalMERequired = 0;
            double conceptusFat = 0;
            double conceptusProtein = 0;

            // smallest allowed interval is 7 days for representative calculations
            const int smallestInterval = 7;
            int step = Math.Min(ind.Parameters.Details.CurrentTimeStep.Interval, smallestInterval);
            int currentDays = 0;

            while (currentDays < ind.DaysPregnantInTimeStep)
            {
                double propOfPregnancy = ind.ProportionOfPregnancy(currentDays);
                // Equations 57-65   ==================================================
                double normalWeightFetus = ind.ScaledBirthWeight * Math.Exp(ind.Parameters.GrowPF_CP.FetalNormWeightParameter_CP2 * (1 - Math.Exp(ind.Parameters.GrowPF_CP.FetalNormWeightParameter2_CP3 * (1 - propOfPregnancy))));
                if (propOfPregnancy == 0 || ind.Weight.Fetus.Amount == 0) // either first day of pregnancy or setting up fetus at initialisation when the breeder has not been through previous pregnancy
                {
                    ind.Weight.Fetus.Set(normalWeightFetus);
                }

                if (propOfPregnancy >= 0.7 && ind.WeightAt70PctPregnant == 0)
                {
                    // todo: this is not correct for initially pregnant breeders as the 70% rate has been missed prior to start of the simulation
                    ind.WeightAt70PctPregnant = ind.Weight.Base.Amount;
                }

                // toxaemia mortality
                if (ind.NumberOfFetuses >= 2 && propOfPregnancy > 0.7)
                {
                    double toxaemiaRate = StdMath.SIG((ind.WeightAt70PctPregnant - ind.Weight.Base.Amount) / ind.Weight.NormalisedForAge,
                                                       ind.Parameters.GrowPF_CP.ToxaemiaCoefficients);
                    if (MathUtilities.IsLessThan(RandomNumberGenerator.Generator.NextDouble(), toxaemiaRate * ind.DaysPregnantInTimeStep))
                    {
                        ind.Died = true;
                        ind.SaleFlag = HerdChangeReason.DiedToxaemia;
                    }
                }

                // change in normal fetus weight across time step
                int daysToEnd = Math.Min(ind.DaysPregnantInTimeStep - currentDays, smallestInterval);
                double deltaChangeNormFetusWeight = ind.ScaledBirthWeight * Math.Exp(ind.Parameters.GrowPF_CP.FetalNormWeightParameter_CP2 * (1 - Math.Exp(ind.Parameters.GrowPF_CP.FetalNormWeightParameter2_CP3 * (1 - ind.ProportionOfPregnancy(currentDays + daysToEnd))))) - normalWeightFetus;

                // calculated after growth in time step to avoid issue of 0 in first step especially for larger time steps
                double relativeConditionFetus = ind.Weight.Fetus.Amount / normalWeightFetus;

                double CFPregnant = (ind.Weight.RelativeCondition - 1) * (normalWeightFetus / (ind.Parameters.General.BirthScalar[ind.NumberOfFetuses - 1] * ind.Weight.StandardReferenceWeight));

                if (MathUtilities.IsGreaterThanOrEqual(ind.Weight.RelativeCondition, 1.0))
                {
                    ind.Weight.Fetus.Adjust(Math.Max(0.0, deltaChangeNormFetusWeight * (CFPregnant + 1)));
                }
                else
                {
                    // include stunt factor in calculation
                    ind.Weight.Fetus.Adjust(Math.Max(0.0, deltaChangeNormFetusWeight * ((ind.Parameters.GrowPF_CP.FetalGrowthPoorCondition_CP14[ind.NumberOfFetuses - 1] * CFPregnant) + 1)));
                }

                ind.Weight.Conceptus.Set(ind.NumberOfFetuses * (ind.Parameters.GrowPF_CP.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight * Math.Exp(ind.Parameters.GrowPF_CP.ConceptusWeightParameter_CP6 * (1 - Math.Exp(ind.Parameters.GrowPF_CP.ConceptusWeightParameter2_CP7 * (1 - propOfPregnancy))))) + (ind.Weight.Fetus.Amount - normalWeightFetus));

                if (propOfPregnancy > 0 && ind.Weight.ConceptusProtein.Amount == 0) 
                {
                    // conceptus fat and protein have not been tracked for this breeder to date, so set as default ensuring the number of fetuses are accounted for by using the expected protein content of the conceptus weight just calculated.

                    //
                    //
                    //
                    //  !!!!!!!!!!!!!!!!  NEEDS TO BE FIXED
                    //
                    //
                    //
                    //
                    // ToDo: need to also set the amount of fat and protein of the conceptus at the current date as this is used to determine offspring fat and protein mass

                    ind.Weight.ConceptusProtein.Set(ind.Weight.Conceptus.Amount * ind.Parameters.GrowPF_CP.ConceptusProteinContent_CP11);
                    // ToDo: calculate conceptus energy minus the conceptus protein energy
                    // convert net energy to fat and update the conceptus fat.
                    //double fat = ((birthWeight * newborn.Parameters.GrowPF_CP.ConceptusEnergyContent_CP8) - (newborn.Parameters.General.MJEnergyPerKgProtein * newborn.Weight.Protein.Amount)) / newborn.Parameters.General.MJEnergyPerKgFat
                    ind.Weight.ConceptusFat.Set(0);
                }

                // MJ per day
                double conceptusME = (ind.Parameters.GrowPF_CP.ConceptusEnergyContent_CP8 * (ind.NumberOfFetuses * ind.Parameters.GrowPF_CP.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight) * relativeConditionFetus *
                (ind.Parameters.GrowPF_CP.ConceptusEnergyParameter_CP9 * ind.Parameters.GrowPF_CP.ConceptusEnergyParameter2_CP10 / (ind.Parameters.General.GestationLength.InDays)) *
                Math.Exp(ind.Parameters.GrowPF_CP.ConceptusEnergyParameter2_CP10 * (1 - ind.DaysPregnant / ind.Parameters.General.GestationLength.InDays) + ind.Parameters.GrowPF_CP.ConceptusEnergyParameter_CP9 * (1 - Math.Exp(ind.Parameters.GrowPF_CP.ConceptusEnergyParameter2_CP10 * (1 - propOfPregnancy))))) / 0.13;

                totalMERequired += conceptusME * daysToEnd;

                // kg protein per day
                double conceptusProteinReq = ind.Parameters.GrowPF_CP.ConceptusProteinContent_CP11 * (ind.NumberOfFetuses * ind.Parameters.GrowPF_CP.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight) * relativeConditionFetus *
                    (ind.Parameters.GrowPF_CP.ConceptusProteinParameter_CP12 * ind.Parameters.GrowPF_CP.ConceptusProteinParameter2_CP13 / (ind.Parameters.General.GestationLength.InDays)) * Math.Exp(ind.Parameters.GrowPF_CP.ConceptusProteinParameter2_CP13 * (1 - propOfPregnancy) + ind.Parameters.GrowPF_CP.ConceptusProteinParameter_CP12 * (1 - Math.Exp(ind.Parameters.GrowPF_CP.ConceptusProteinParameter2_CP13 * (1 - ind.ProportionOfPregnancy(currentDays)))));

                // protein for time-step (kg)
                conceptusProtein += conceptusProteinReq * daysToEnd;
                // fat for time-step (kg)
                conceptusFat += ((conceptusME * 0.13) - (conceptusProteinReq * 23.6)) / 39.3 * daysToEnd;

                currentDays += step;
            }

            //fetal fat is conceptus fat. per individual. Assumes minimal (0) fat in placenta

            ind.Weight.Protein.ForPregnancy = conceptusProtein;
            ind.Weight.ConceptusProtein.Adjust(conceptusProtein);
            ind.Weight.ConceptusFat.Adjust(conceptusFat);

            return totalMERequired/ind.DaysPregnantInTimeStep;
        }

        /// <inheritdoc/>
        public void SetProteinAndFatAtBirth(Ruminant newborn, double birthWeight = 0)
        {
            if ((newborn.Mother?.Weight?.Fetus?.Amount??0) > 0)
            {
                // mother has been through pregnancy and has conceptus fat and protein
                newborn.Weight.Fat = new(newborn.Mother.Weight.Fetus.Amount / newborn.Mother.Weight.Conceptus.Amount * newborn.Mother.Weight.ConceptusFat.Amount);
                newborn.Weight.Protein = new(newborn, newborn.Mother.Weight.Fetus.Amount / newborn.Mother.Weight.Conceptus.Amount * newborn.Mother.Weight.ConceptusProtein.Amount);
            }
            // set fat and protein energy based on initial amounts
            newborn.Energy.Fat = new(newborn.Weight.Fat.Amount * newborn.Parameters.General.MJEnergyPerKgFat);
            newborn.Energy.Protein = new(newborn.Weight.Protein.Amount * newborn.Parameters.General.MJEnergyPerKgProtein);
        }

        /// <inheritdoc/>
        public void SetInitialFatProtein(Ruminant individual, RuminantTypeCohort cohort, double initialWeight)
        {
            double pFat;
            double pProtein = 0;
            double vProtein = 0;

            if (cohort.InitialFatProteinStyle == InitialiseFatProteinAssignmentStyle.EstimateFromRelativeCondition)
            {
                double RC = individual.Weight.RelativeCondition;
                if (individual.Weight.IsStillGrowing)
                {
                    RC = 0.9;
                }

                double sexFactor = 1.0;
                if (individual.Sex == Sex.Male && individual.IsAbleToBreed)
                {
                    sexFactor = 0.85;
                }

                double propFat = individual.Parameters.General.ProportionEBWFatFemale;
                if (individual.Sex == Sex.Male && individual.IsSterilised == false)
                {
                    propFat = individual.Parameters.General.ProportionEBWFatMale;
                }

                double RCFatSlope = (individual.Parameters.General.ProportionEBWFatMax - propFat) / 0.5;
                pFat = (propFat + ((RC - 1) * RCFatSlope)) * sexFactor;
            }
            else
            {
                if (cohort.InitialFatProteinValues == null)
                {
                    throw new Exception($"Cannot set initial fat and protein for individual [{individual.ID}]. InitialFatProteinValues required for the selected ruminant growth activity were not provided for the cohort [{cohort.Name}].");
                }

                pFat = cohort.InitialFatProteinValues[0];
                pProtein = cohort.InitialFatProteinValues[1];
                if (cohort.InitialFatProteinValues.Length == 3)
                {
                    if (cohort.AssociatedHerd.RuminantGrowActivity.IncludeVisceralProteinMass)
                    {
                        vProtein = cohort.InitialFatProteinValues[2];
                    }
                    else
                    {
                        // need to convert this visceral protein to non-visceral such that the wet weight conversion is correct and EBM is ok from protein alone
                        pProtein += cohort.InitialFatProteinValues[2] * (individual.Parameters.GrowOddy.pPrpM / individual.Parameters.GrowOddy.pPrpV);
                    }
                }
            }

            switch (cohort.InitialFatProteinStyle)
            {
                case InitialiseFatProteinAssignmentStyle.ProvideMassKg:
                    break;
                case InitialiseFatProteinAssignmentStyle.ProportionOfEmptyBodyMass:
                    pFat *= individual.Weight.EmptyBodyMass;
                    pProtein *= individual.Weight.EmptyBodyMass;
                    vProtein *= individual.Weight.EmptyBodyMass;
                    break;
                case InitialiseFatProteinAssignmentStyle.ProvideEnergyMJ:
                    pFat /= individual.Parameters.General.MJEnergyPerKgFat;
                    pProtein /= individual.Parameters.General.MJEnergyPerKgProtein;
                    vProtein /= individual.Parameters.General.MJEnergyPerKgProtein;
                    break;
                case InitialiseFatProteinAssignmentStyle.EstimateFromRelativeCondition:
                    pFat *= individual.Weight.EmptyBodyMass;
                    pProtein = individual.Parameters.GrowPF_CG.ProteinContentOfFatFreeTissueGainWetBasis * (individual.Weight.EmptyBodyMass - pFat);
                    if (cohort.AssociatedHerd.RuminantGrowActivity.IncludeVisceralProteinMass)
                        {
                        throw new NotImplementedException("Cannot estimate required visceral protein mass using the RelativeCondition fat and protein mass assignment style.");
                    }
                    break;
                default:
                    break;
            }

            // pFat and pProtein are now in kg
            individual.Weight.Fat = new(pFat);
            individual.Energy.Fat = new(pFat * individual.Parameters.General.MJEnergyPerKgFat);

            if (cohort.AssociatedHerd.RuminantGrowActivity is RuminantActivityGrowOddy)
            {
                individual.Weight.Protein = new(individual, pProtein);
                individual.Weight.ProteinViscera = new(individual, vProtein);
                individual.Energy.ProteinViscera = new(vProtein * individual.Parameters.General.MJEnergyPerKgProtein);
            }
            else
            {
                individual.Weight.Protein = new(individual, pProtein);
            }
            individual.Energy.Protein = new(pProtein * individual.Parameters.General.MJEnergyPerKgProtein);
        }

        /// <summary>
        /// Function to calculate manure production and place in uncollected manure pools of the "manure" resource in ProductResources.
        /// This is called at the end of CLEMAnimalWeightGain so after intake determined and before deaths and sales.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCalculateManure")]
        private void OnCLEMCalculateManure(object sender, EventArgs e)
        {
            CalculateManure(manureStore, CurrentHerd(false));
        }

        /// <summary>
        /// Method to perform manure production for herd.
        /// </summary>
        /// <param name="manureStore">Store to place manure</param>
        /// <param name="herd">Individuals <see langword="for"/>production</param>
        public static void CalculateManure(ProductStoreTypeManure manureStore, IEnumerable<Ruminant> herd)
        {
            // nowhere to place manure
            if (manureStore is null)
            {
                return;
            }

            // sort by animal location to ensure correct deposit location.
            foreach (var groupIndividuals in herd.GroupBy(a => a.Location))
            {
                manureStore.AddUncollectedManure(groupIndividuals.Key ?? "", groupIndividuals.Sum(a => a.Output.Manure));
            }
        }

        /// <summary>
        /// Method to report unfed individuals to warning log
        /// </summary>
        public static void ReportUnfedIndividualsWarning(IEnumerable<Ruminant> herd, WarningLog warnings, ISummary summary, IModel sender, CLEMEvents events)
        {
            string fix = "\r\nFix: Check feeding strategy and ensure animals are moved to pasture or fed in yards";
            string styleOfIndividual = "individuals";

            var unfed = herd.Where(a => a.Intake.IsUnfed)
                .GroupBy(a => new { breed = a.Parameters.Details.Name, weaned = a.IsWeaned })
                .Select(a => new { group = a.Key, number = a.Count() });
            foreach (var unfedGroup in unfed)
            {
                if (unfedGroup.number > 0)
                {
                    if (!unfedGroup.group.weaned)
                    {
                        styleOfIndividual = "sucklings";
                        fix = "\\r\\nFix: Check sucklings are are fed, or have access to pasture (moved with mothers or separately) when no milk is available from mother";
                    }
                    string warn = $"{styleOfIndividual} of [r={unfedGroup.group.breed}] not fed";
                    string warningFull = $"Some {styleOfIndividual} of [r={unfedGroup.group.breed}] were not fed in some months (e.g. [{unfedGroup.number}] individuals in [{events.Clock.Today.Month}/{events.Clock.Today.Year}]){fix}";
                    warnings.CheckAndWrite(warn, summary, sender, MessageType.Warning, warningFull);
                }
            }
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check parameters are available for all ruminants.
            foreach (var item in Structure.FindAll<RuminantType>().Where(a => a.Parameters.GrowPF is null))
            {
                yield return new ValidationResult($"No [RuminantParametersGrowPF] parameters are provided for [{item.NameWithParent}]", new string[] { "RuminantParametersGrowPF" });
            }
        }

        #endregion

    }
}
