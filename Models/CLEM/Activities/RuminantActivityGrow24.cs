using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity (2024 version)</summary>
    /// <summary>This class represents the CLEM activity responsible for determining potential intake from the quality of all food eaten, and providing energy and protein for all needs (e.g. wool production, pregnancy, lactation and growth).</summary>
    /// <remarks>Rumiant death activity controls mortality, while the Breed activity is responsible for conception and births.</remarks>
    /// <authors>Summary and implementation of best methods in predicting ruminant growth based on Frier 2012 (AusFarm), James Dougherty, CSIRO</authors>
    /// <authors>CLEM upgrade and implementation, Adam Liedloff, CSIRO</authors>
    /// <acknowledgements>This animal production is based on the equations developed by Frier (2012), implemented in GRAZPLAN (Moore, CSIRO) and APSFARM (CSIRO)</acknowledgements>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs best available growth and aging of all ruminants based on Frier, 2012.")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrow2024.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow24) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType },
        SingleInstance = true)]
    public class RuminantActivityGrow24 : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private ProductStoreTypeManure manureStore;
        private double kl = 0;
        private readonly FoodResourcePacket milkPacket = new()
        {
            TypeOfFeed = FeedType.Milk,
            RumenDegradableProteinContent = 0,
        };

        /// <summary>
        /// Perform Activity with partial resources available.
        /// </summary>
        [JsonIgnore]
        public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // Because this activity inherits from CLEMRuminantActivityBase we have access to herd details.
            InitialiseHerd(true, true);

            manureStore = Resources.FindResourceType<ProductStore, ProductStoreTypeManure>(this, "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        }

        /// <summary>Function to naturally wean individuals at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // Age all individuals
            foreach (Ruminant ind in CurrentHerd())
                ind.SetCurrentDate(events.Clock.Today);

            // Natural weaning takes place here before animals eat or take milk from mother.
            foreach (var ind in CurrentHerd(false).Where(a => a.Weaned == false && MathUtilities.IsGreaterThan(a.AgeInDays, a.AgeToWeanNaturally)))
            {
                ind.Wean(true, "Natural", events.Clock.Today);
                // report wean. If mother has died create temp female with the mother's ID for reporting only
                ind.BreedDetails.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.Parameters, events.Clock.Today, -1, ind.Parameters.General.BirthScalar[0], 999) { ID = ind.MotherID }, events.Clock.Today, ind));
            }
        }

        /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPotentialIntake")]
        private void OnCLEMPotentialIntake(object sender, EventArgs e)
        {
            // Calculate potential intake and reset stores
            // Order age descending so breeder females calculate milk production before suckings require it for growth.
            foreach (var groupInd in CurrentHerd(false).GroupBy(a => a.IsSucklingWithMother).OrderBy(a => a.Key))
            {
                foreach (var ind in groupInd)
                {
                    // reset actual expected containers as expected determined during CalculatePotentialIntake
                    ind.Intake.SolidsDaily.Reset();
                    ind.Intake.MilkDaily.Reset();

                    CalculatePotentialIntake(ind);
                    // Perform after potential intake calculation as it needs MEContent from previous month for Lactation energy calculation.
                    // After this these tallies can be reset ready for following intake and updating. 
                    ind.Intake.Reset();
                    ind.Energy.Reset();
                    ind.Output.Reset();
                }
            }
        }

        /// <summary>
        /// Method to calculate an individual's potential intake for the time step scaling for condition, young age, and lactation.
        /// </summary>
        /// <param name="ind">Individual for which potential intake is determined.</param>
        private void CalculatePotentialIntake(Ruminant ind)
        {
            // CF - Condition factor SCA Eq.3
            double cf = 1.0;
            if(ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 > 1 && ind.Weight.BodyCondition > 1)
            {
                if (ind.Weight.BodyCondition >= ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20)
                    cf = 0;
                else
                    cf = Math.Min(1.0, ind.Weight.BodyCondition * (ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 - ind.Weight.BodyCondition) / (ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 - 1));
            }

            // YF - Young factor SCA Eq.4, the proportion of solid intake sucklings have when low milk supply as function of age.
            double yf = 1.0;
            if (!ind.Weaned)
            {
                // calculate expected milk intake, part B of SCA Eq.70 with one individual (y=1)
                ind.Intake.MilkDaily.Expected = ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6 * Math.Pow(ind.AgeInDays+(events.Interval/2.0), 0.75) * (ind.Parameters.Grow24_CKCL.MilkConsumptionLimit1_CL12 + ind.Parameters.Grow24_CKCL.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.Grow24_CKCL.MilkCurveSuckling_CL3 * (ind.AgeInDays + (events.Interval / 2.0))));  // changed CL4 -> CL3 as sure it should be the suckling curve used here. 
                double milkactual = Math.Min(ind.Intake.MilkDaily.Expected, ind.Mother.Milk.PotentialRate / ind.Mother.SucklingOffspringList.Count());
                // calculate YF
                // ToDo check that this is the potential milk calculation needed.
                yf = (1 - (milkactual / ind.Intake.MilkDaily.Expected)) / (1 + Math.Exp(-ind.Parameters.Grow24_CI.RumenDevelopmentCurvature_CI3 *(ind.AgeInDays + (events.Interval / 2.0) - ind.Parameters.Grow24_CI.RumenDevelopmentAge_CI4))); 
            }

            // TF - Temperature factor. SCA Eq.5
            // NOT INCLUDED
            double tf = 1.0;

            // LF - Lactation factor SCA Eq.8
            double lf = 1.0;
            if(ind is RuminantFemale female)
            {
                if (female.IsLactating)
                {
                    // age of young (Ay) is the same as female DaysLactating
                    double mi = female.DaysLactating(events.Interval / 2.0) / ind.Parameters.Grow24_CI.PeakLactationIntakeDay_CI8; // SCA Eq.9
                    lf = 1 + ind.Parameters.Grow24_CI.PeakLactationIntakeLevel_CI19[female.NumberOfSucklings-1] * Math.Pow(mi, ind.Parameters.Grow24_CI.LactationResponseCurvature_CI9) * Math.Exp(ind.Parameters.Grow24_CI.LactationResponseCurvature_CI9 * (1 - mi)); // SCA Eq.8
                    double lb = 1.0;
                    double wl = ind.Weight.RelativeSize * ((female.BodyConditionParturition - ind.Weight.BodyCondition) / female.DaysLactating(events.Interval / 2.0)); // SCA Eq.12
                    if (female.DaysLactating(events.Interval / 2.0) >= ind.Parameters.Lactation.MilkPeakDay && wl > ind.Parameters.Grow24_CI.LactationConditionLossThresholdDecay_CI14 * Math.Exp(-Math.Pow(ind.Parameters.Grow24_CI.LactationConditionLossThreshold_CI13 * female.DaysLactating(events.Interval / 2.0), 2.0)))
                    {
                        lb = 1 - ((ind.Parameters.Grow24_CI.LactationConditionLossAdjustment_CI12 * wl) / ind.Parameters.Grow24_CI.LactationConditionLossThreshold_CI13); // (Eq.11)
                    }
                    if (female.SucklingOffspringList.Any())
                    {
                        // lf * lb * la (Eq.10)
                        lf *= lb * (1 - ind.Parameters.Grow24_CI.ConditionAtParturitionAdjustment_CI15 + ind.Parameters.Grow24_CI.ConditionAtParturitionAdjustment_CI15 * female.BodyConditionParturition);
                    }
                    else
                    {
                        // non suckling - e.g. dairy.
                        // lf * lb * lc (Eq.13)
                        lf *= lb * (1 + ind.Parameters.Grow24_CI.EffectLevelsMilkProdOnIntake_CI10 * ((ind.Parameters.Lactation.MilkPeakYield - ind.Parameters.Grow24_CI.BasalMilkRelSRW_CI11 * ind.Weight.StandardReferenceWeight) / (ind.Parameters.Grow24_CI.BasalMilkRelSRW_CI11 * ind.Weight.StandardReferenceWeight)));
                    }

                    // calculate estimated milk production for time step here
                    // assuming average feed quality if no previous diet values
                    // This needs to happen before suckling potential intake can be determined.
                    double tempMilkProtein = 0;
                    _ = CalculateLactationEnergy(female, false, ref tempMilkProtein);
                }
                else
                    female.Milk.Reset();
            }

            // Intake max SCA Eq.2
            // Restricted here to Expected (potential) time OverFeedPotentialIntakeModifier
            ind.Intake.SolidsDaily.MaximumExpected = Math.Max(0.0, ind.Parameters.Grow24_CI.RelativeSizeScalar_CI1 * ind.Weight.StandardReferenceWeight * ind.Weight.RelativeSize * (ind.Parameters.Grow24_CI.RelativeSizeQuadratic_CI2 - ind.Weight.RelativeSize));
            ind.Intake.SolidsDaily.Expected = ind.Intake.SolidsDaily.MaximumExpected * cf * yf * tf * lf;
        }

        /// <summary>Function to calculate growth of herd for the time-step</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            Status = ActivityStatus.NotNeeded;

            foreach (var breed in CurrentHerd(false).GroupBy(a => a.BreedDetails.Name))
            {
                int unfed = 0;
                int unfedcalves = 0;
                // work on herd sorted descending age to ensure mothers are processed before sucklings.
                foreach (Ruminant ind in breed.OrderByDescending(a => a.AgeInDays))
                {
                    Status = ActivityStatus.Success;

                    // Adjusting potential intake for digestability of fodder is now done in RuminantIntake along with concentrates and fodder.
                    if(ind is RuminantFemale rumFemale)
                        ind.Intake.AdjustIntakeBasedOnFeedQuality(rumFemale.IsLactating, ind);
                    else
                        ind.Intake.AdjustIntakeBasedOnFeedQuality(false, ind);

                    CalculateEnergy(ind);

                    if (ind.Weaned && ind.Intake.SolidsDaily.Actual == 0 && ind.Intake.SolidsDaily.Expected > 0)
                        unfed++;
                    else if (!ind.Weaned && MathUtilities.IsLessThanOrEqual(ind.Intake.MilkDaily.Actual + ind.Intake.SolidsDaily.Actual, 0))
                        unfedcalves++;
                }
                ReportUnfedIndividualsWarning(breed, unfed, unfedcalves);
            }
        }

        /// <summary>
        /// Function to calculate energy from intake and subsequent growth.
        /// </summary>
        /// <remarks>
        /// All energy calculations are per day and multiplied at end to give weight gain for time step (monthly). 
        /// Energy and body mass change are based on SCA - Nutrient Requirements of Domesticated Ruminants, CSIRO
        /// </remarks>
        /// <param name="ind">Indivudal ruminant for calculation.</param>
        public void CalculateEnergy(Ruminant ind)
        {
            kl = 0;

            // The feed quality measures are now provided in IFeedType and FoodResourcePackets
            // The individual tracks the quality of mixed feed types based on broad type (concentrate, hay or sillage, temperate pasture, tropical pasture, or milk) in RuminantIntake
            // Energy metabolic - have DMD, fat content % CP% as inputs from ind as supplement and forage, do not need ether extract (fat) for forage

            // Sme 1 for females and castrates
            double sexEffectME = 1;
            // Sme 1.15 for all non-castrated males.
            if (ind.Weaned && ind.Sex == Sex.Male && !ind.IsSterilised)
                sexEffectME = 1.15;

            double conceptusProtein = 0;
            double conceptusFat = 0;
            double milkProtein = 0;

            // calculate here as is also needed in not weaned.. in case consumed feed and milk.
            ind.Energy.Km = 0.02 * ind.Intake.MDSolid + 0.5;

            if (ind.Weaned)
            {
                CalculateMaintenanceEnergy(ind, ind.Energy.Km, sexEffectME);

                if (ind is RuminantFemale indFemale)
                {
                    // Determine energy required for fetal development
                    ind.Energy.ForFetus = CalculatePregnancyEnergy(indFemale, ref conceptusProtein, ref conceptusFat);

                    // calculate energy for lactation
                    // look for milk production calculated before offspring may have been weaned
                    // recalculate milk production based on DMD and MEContent of food provided
                    // MJ / day
                    ind.Energy.ForLactation = CalculateLactationEnergy(indFemale, true, ref milkProtein);
                }

                if(ind.Energy.ForLactation > 0)
                {
                    if(ind.Energy.AfterLactation >= 0)
                    {
                        ind.Energy.Kg = 0.95 * kl;
                    }
                    else
                    {
                        ind.Energy.Kg = kl / 0.84; // represents tissue mobilisation
                    }
                }
                else
                {
                    if (ind.Energy.AfterLactation >= 0)
                    {
                        ind.Energy.Kg = 0.042 * ind.Intake.MDSolid + 0.006;
                    }
                    else
                    {
                        ind.Energy.Kg = ind.Energy.Km / 0.8; // represents tissue mobilisation
                    }
                }
            }
            else // Unweaned
            {
                // Unweaned individuals are assumed to be suckling as natural weaning rate set regardless of inclusion of a wean activity.
                // Unweaned individuals without mother or milk from mother will need to try and survive on limited pasture until weaned.
                // YoungFactor in CalculatePotentialIntake determines how much these individuals can eat when milk is in shortfall

                // recalculate milk intake based on mothers updated milk production for the time step using the previous monthly potential milk intake
                double received = 0;
                if(ind.Mother is not null)
                    received = Math.Min(ind.Intake.MilkDaily.Expected, ind.Mother.Milk.PotentialRate/ ind.Mother.SucklingOffspringList.Count);
                // remove consumed milk from mother.
                ind.Mother?.Milk.Take(received*events.Interval, MilkUseReason.Suckling);

                milkPacket.MetabolisableEnergyContent = ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6;
                milkPacket.CrudeProteinContent = ind.Parameters.Grow24_CKCL.ProteinContentMilk_CL15;
                milkPacket.Amount = received;
                ind.Intake.AddFeed(milkPacket);

                double milkIntakeME = ind.Intake.MilkME;
                double solidsIntakeME = ind.Intake.SolidsME;

                // Below now uses actual intake received rather than assume all potential intake is eaten
                double kml = 1;
                ind.Energy.Kg = (0.006 + ind.Intake.MDSolid * 0.042) + 0.7 * (milkIntakeME / (milkIntakeME + solidsIntakeME)); // MJ milk/MJ total intake;

                if (MathUtilities.IsPositive(ind.Intake.MilkDaily.Actual + ind.Intake.SolidsDaily.Actual))
                {
                    // average energy efficiency for maintenance
                    kml = ((milkIntakeME * 0.85) + (solidsIntakeME * ind.Energy.Km)) / (milkIntakeME + solidsIntakeME);
                    // average energy efficiency for growth
                    ind.Energy.Kg = ((milkIntakeME * 0.7) + (solidsIntakeME * ind.Energy.Kg)) / (milkIntakeME + solidsIntakeME);
                }

                CalculateMaintenanceEnergy(ind, kml, sexEffectME);
            }
            double adjustedFeedingLevel = ind.Energy.FromIntake / ind.Energy.ForMaintenance - 2;

            // Wool production
            ind.Energy.ForWool = CalculateWoolEnergy(ind);

            //TODO: add draft individual energy requirement: does this also apply to unweaned individuals? If so move outside loop

            // protein use for maintenance
            var milkStore = ind.Intake.GetStore(FeedType.Milk);
            double EndogenousUrinaryProtein = ind.Parameters.Grow24_CM.BreedEUPFactor1_CM12 * Math.Log(ind.Weight.Live) - ind.Parameters.Grow24_CM.BreedEUPFactor2_CM13;
            double EndogenousFecalProtein = 0.0152 * ind.Intake.SolidIntake + ((5.26 * (10 ^ -4)) * milkStore?.ME??0);
            double DermalProtein = ind.Parameters.Grow24_CM.DermalLoss_CM14 * Math.Pow(ind.Weight.Live,0.75);
            // digestible protein leaving stomach from milk
            double DPLSmilk = milkStore?.CrudeProtein??0 * 0.92;
            // efficiency of using DPLS
            double kDPLS = (ind.Weaned)? ind.Parameters.Grow24_CG.EfficiencyOfDPLSUseFromFeed_CG2: ind.Parameters.Grow24_CG.EfficiencyOfDPLSUseFromFeed_CG2 / (1 + ((ind.Parameters.Grow24_CG.EfficiencyOfDPLSUseFromFeed_CG2 / ind.Parameters.Grow24_CG.EfficiencyOfDPLSUseFromMilk_CG3) -1)*(DPLSmilk / ind.Intake.DPLS) ); //EQn 103
            double proteinForMaintenance = EndogenousUrinaryProtein + EndogenousFecalProtein + DermalProtein;

            // ToDo: Do these use SRW of Female or does it include the 1.2x factor for males?
            double relativeSizeForWeightGainPurposes = Math.Min(1 - ((1 - (ind.Weight.AtBirth/ind.Weight.StandardReferenceWeight)) * Math.Exp(-(ind.Parameters.General.AgeGrowthRateCoefficient_CN1 * ind.AgeInDays) / Math.Pow(ind.Weight.StandardReferenceWeight, ind.Parameters.General.SRWGrowthScalar_CN2))), (ind.Weight.HighestAttained / ind.Weight.StandardReferenceWeight));
            double sizeFactor1ForGain = 1 / (1 + Math.Exp(-ind.Parameters.Grow24_CG.GainCurvature_CG4 * (relativeSizeForWeightGainPurposes - ind.Parameters.Grow24_CG.GainMidpoint_CG5)));
            double sizeFactor2ForGain = Math.Max(0, Math.Min(((relativeSizeForWeightGainPurposes - ind.Parameters.Grow24_CG.ConditionNoEffect_CG6) / (ind.Parameters.Grow24_CG.ConditionMaxEffect_CG7 - ind.Parameters.Grow24_CG.ConditionNoEffect_CG6)), 1));

            double proteinGain1 = kDPLS * (ind.Intake.DPLS - ((proteinForMaintenance + conceptusProtein + milkProtein) / kDPLS));

            // mj/kg gain
            double energyEmptyBodyGain = ind.Parameters.Grow24_CG.GrowthEnergyIntercept1_CG8 - sizeFactor1ForGain * (ind.Parameters.Grow24_CG.GrowthEnergyIntercept2_CG9 - (ind.Parameters.Grow24_CG.GrowthEnergySlope1_CG10 * adjustedFeedingLevel)) + sizeFactor2ForGain * (ind.Parameters.Grow24_CG.GrowthEnergySlope2_CG11 * (ind.Weight.RelativeCondition - 1));
            // units = kg protein/kg gain
            double proteinContentOfGain = ind.Parameters.Grow24_CG.ProteinGainIntercept1_CG12 + sizeFactor1ForGain * (ind.Parameters.Grow24_CG.ProteinGainIntercept2_CG13 - ind.Parameters.Grow24_CG.ProteinGainSlope1_CG14 * adjustedFeedingLevel) + sizeFactor2ForGain * ind.Parameters.Grow24_CG.ProteinGainSlope2_CG15 * (ind.Weight.RelativeCondition - 1);
            // units MJ tissue gain/kg ebg

            double netEnergyForGain = ind.Energy.Kg * (ind.Intake.ME - (ind.Energy.ForMaintenance + ind.Energy.ForFetus + ind.Energy.ForLactation));
            if (netEnergyForGain > 0)
                netEnergyForGain *= ind.Parameters.Grow24_CG.BreedGrowthEfficiencyScalar;

            double proteinNet1 = proteinGain1 - (proteinContentOfGain * (netEnergyForGain / energyEmptyBodyGain));

            if (milkProtein > 0 && proteinNet1 < milkProtein)
            {
                // MilkProteinLimit replaces MP2 in equations 75 and 76
                // ie it recalculates ME for lactation and protein for lactation
                RuminantFemale checkFemale = ind as RuminantFemale;

                // recalculate MP to replace the MP2 and recalculate milk production and Energy for lactation
                double MP = (1 + Math.Min(0, (proteinNet1 / milkProtein))) * checkFemale.Milk.PotentialRate2;

                milkProtein = ind.Parameters.Grow24_CKCL.ProteinContentMilk_CL15 * MP / ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6;

                checkFemale.Milk.Available = MP * events.Interval;
                checkFemale.Milk.Produced = checkFemale.Milk.Available;

                ind.Energy.ForLactation = MP / 0.94 * kl;

                // adjusted NEG1/ 
                double NEG2 = netEnergyForGain + ind.Parameters.Grow24_CKCL.MetabolisabilityOfMilk_CL5 * (checkFemale.Milk.PotentialRate2 - MP);
                double PG2 = proteinGain1 + (checkFemale.Milk.PotentialRate2 - MP) * (ind.Parameters.Grow24_CKCL.MetabolisabilityOfMilk_CL5 / ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6);
                double ProteinNet2 = PG2 - proteinContentOfGain * (NEG2 / energyEmptyBodyGain);
                ind.Energy.NetForGain = NEG2 + ind.Parameters.Grow24_CG.ProteinGainIntercept1_CG12 * energyEmptyBodyGain * ((Math.Min(0, ProteinNet2) / proteinContentOfGain));
            }
            else
            {
                ind.Energy.NetForGain = netEnergyForGain + ind.BreedDetails.Parameters.Grow24_CG.ProteinGainIntercept1_CG12 * energyEmptyBodyGain * (Math.Min(0, proteinGain1) / proteinContentOfGain);
            }
            double emptyBodyGainkg = ind.Energy.NetForGain / energyEmptyBodyGain;

            // update weight based on the time-step
            ind.Weight.Adjust(emptyBodyGainkg * events.Interval, ind.Parameters.Grow24_CG.EBW2LW_CG18 * emptyBodyGainkg * events.Interval, ind);

            double MJFatChange = 0;
            double MJProteinChange = proteinGain1 / 1000.0 * 23.6;
            if (proteinGain1 < 0 & emptyBodyGainkg < 0)
            {
                MJFatChange = (ind.Energy.NetForGain - MJProteinChange);
            }
            if (proteinGain1 > 0 & emptyBodyGainkg < 0)
            {
                // Assumes protein increase as suggested and all energy loss is Fat
                MJFatChange = ind.Energy.NetForGain;
            }

            // protein mass on protein basis not mass of lean tissue mass. use conversvion XXXX for weight to perform checksum.
            ind.Weight.Protein.Adjust(MJProteinChange / 23.6 * events.Interval); // for time step
            ind.Energy.Protein.Adjust(MJProteinChange * events.Interval); // for time step

            ind.Weight.Fat.Adjust(MJFatChange / 39.3 * events.Interval); // for time step
            ind.Energy.Fat.Adjust(MJFatChange * events.Interval); // for time step

            // N balance = 
            // ToDo: not currently used
            ind.Output.NitrogenBalance =  ind.Intake.CrudeProtein/ FoodResourcePacket.FeedProteinToNitrogenFactor - (milkProtein / FoodResourcePacket.MilkProteinToNitrogenFactor) - ((conceptusProtein + MJProteinChange / 23.6) / FoodResourcePacket.FeedProteinToNitrogenFactor);

            // Total fecal protein
            double TFP = ind.Intake.IndigestibleUDP + ind.Parameters.Grow24_CACRD.MicrobialProteinDigestibility_CA7 * ind.Parameters.Grow24_CACRD.FaecalProteinFromMCP_CA8 * ind.Intake.RDPRequired + (1 - ind.Parameters.Grow24_CACRD.MilkProteinDigestability_CA5) * milkStore?.CrudeProtein??0 + EndogenousFecalProtein;

            // Total urinary protein
            double TUP = ind.Intake.CrudeProtein - (conceptusProtein + milkProtein + MJProteinChange / 23.6) - TFP - DermalProtein;
            // ToDo: not currently used
            ind.Output.NitrogenExcreted = (TFP + TUP) * events.Interval;

            ind.Output.NitrogenUrine = TUP / 6.25 * events.Interval;
            ind.Output.NitrogenFaecal = TFP / 6.25 * events.Interval;

            // Nbal should be close ish to TFP + TUP

            // manure per time step
            ind.Output.Manure = ind.Intake.SolidsDaily.Actual * (100 - ind.Intake.DMD) / 100 * events.Interval;
        }

        /// <summary>
        /// Calculate maintenance energy and reduce intake based on any rumen protein deficiency
        /// </summary>
        /// <param name="ind">The individual ruminant.</param>
        /// <param name="km">The maintenance efficiency to use</param>
        /// <param name="sexEffect">The sex effect to apply</param>
        private static void CalculateMaintenanceEnergy(Ruminant ind, double km, double sexEffect)
        {
            // Calculate maintenance energy then determine the protein requirement of rumen bacteria
            // Adjust intake proportionally and recalulate maintenance energy with adjusted intake energy
            km *= ind.Parameters.Grow24_CG.BreedMainenanceEfficiencyScalar;

            // Note: Energy.ToMove and Energy.ToGraze are calulated in Grazing Activity.
            ind.Energy.ToMove /= km;
            ind.Energy.ToGraze /= km;

            double rdpReq;
            // todo: check ind.Params.EMaintExponent as in the params is actually CM3 maintenanceExponentForAge
            ind.Energy.ForBasalMetabolism = ((ind.Parameters.Grow24_CM.FHPScalar_CM2 * sexEffect * Math.Pow(ind.Weight.Live, 0.75)) * Math.Max(Math.Exp(-ind.Parameters.Grow24_CM.MainExponentForAge_CM3 * ind.AgeInDays), ind.Parameters.Grow24_CM.AgeEffectMin_CM4) * (1 + ind.Parameters.Grow24_CM.MilkScalar_CM5 * ind.Intake.ProportionMilk)) / km;
            ind.Energy.ForHPViscera = ind.Parameters.Grow24_CM.HPVisceraFL_CM1 * ind.Energy.FromIntake;
            ind.Energy.ForMaintenance = ind.Energy.ForBasalMetabolism + ind.Energy.ForGrazing + ind.Energy.ForHPViscera;

            double adjustedFeedingLevel = -1;
            if(MathUtilities.GreaterThan(ind.Energy.FromIntake,0,2) & MathUtilities.GreaterThan(ind.Energy.ForMaintenance, 0,2 ))
            {
                adjustedFeedingLevel = (ind.Energy.FromIntake / ind.Energy.ForMaintenance) - 1;
            }
            rdpReq = CalculateCrudeProtein(ind, adjustedFeedingLevel);

            ind.Intake.CalculateDigestibleProteinLeavingStomach(rdpReq, ind.Parameters.Grow24_CACRD.MilkProteinDigestability_CA5);
        }

        private static double CalculateCrudeProtein(Ruminant ind, double feedingLevel)
        {
            // 1. calc RDP intake. Rumen Degradable Protein

            // ToDo: check what happens with RDPReq if feedingvalue <- 0... what FMIE term will be used in equation.
            double FMEIterm = 1;

            if (feedingLevel > 0)
            {
                FMEIterm = 0;
                double FMEIrf = 0;
                double DPrf = 0;
                foreach (var store in ind.Intake.GetAllStores)
                {
                    switch (store.Key)
                    {
                        case FeedType.Concentrate:
                            DPrf = 1 - ind.Parameters.Grow24_CACRD.RumenDegradabilityConcentrateSlope_CRD3 * feedingLevel; //Eq50
                            FMEIrf = 1; // set as 1 and there is no reduction for concentrates.
                            break;
                        case FeedType.HaySilage:
                            DPrf = 1 - (ind.Parameters.Grow24_CACRD.RumenDegradabilityIntercept_CRD1 - ind.Parameters.Grow24_CACRD.RumenDegradabilitySlope_CRD2 * store.Value.Details?.DryMatterDigestibility ?? 0) * feedingLevel; // DMD in proportion
                            FMEIrf = 1; // add later depending on feed type, need to add new types PastureTrop, PastureTemp, HaySilage (non-grazed forage). ?? Lucena
                            break;
                        case FeedType.Milk:
                            FMEIrf = 0; // ignore milk. solids only
                            break;
                        case FeedType.PastureTemperate:
                            DPrf = 1 - (ind.Parameters.Grow24_CACRD.RumenDegradabilityIntercept_CRD1 - ind.Parameters.Grow24_CACRD.RumenDegradabilitySlope_CRD2 * store.Value.Details?.DryMatterDigestibility ?? 0) * feedingLevel; // DMD in proportion
                            FMEIrf = 1;
                            break;
                        case FeedType.PastureTropical:
                            DPrf = 1 - (ind.Parameters.Grow24_CACRD.RumenDegradabilityIntercept_CRD1 - ind.Parameters.Grow24_CACRD.RumenDegradabilitySlope_CRD2 * store.Value.Details?.DryMatterDigestibility ?? 0) * feedingLevel; // DMD in proportion
                            FMEIrf = 1;
                            break;
                        default:
                            break;
                    }
                    if(store.Key != FeedType.Milk && DPrf < 1.0)
                        store.Value.ReduceDegradableProtein(DPrf);
                    FMEIterm += (FMEIrf * store.Value.FME);
                }
            }

            // 2. calc UDP intake by difference(CPI-RDPI)
            // now a property of intake

            // 3.calculate RDP requirement

            // ignored GrassGro timeOfYearFactor 19/9/2023 as calculation showed it has very little effect compared with the error in parameterisation and tracking of feed quality and a monthly timestep
            // ignored GrassGro latitude factor for now.
            // double timeOfYearFactorRDPR = 1 + rumenDegradableProteinTimeOfYear * latitude / 40 * Math.Sin(2 * Math.PI * dayOfYear / 365); //Eq.(52)

            return (ind.Parameters.Grow24_CACRD.RumenDegradableProteinIntercept_CRD4 + ind.Parameters.Grow24_CACRD.RumenDegradableProteinSlope_CRD5 * (1 - Math.Exp(-ind.Parameters.Grow24_CACRD.RumenDegradableProteinExponent_CRD6 *
                (feedingLevel + 1)))) * FMEIterm;
        }

        /// <summary>
        /// Determine the energy required for wool growth.
        /// </summary>
        /// <param name="ind">Ruminant individual.</param>
        /// <returns>Daily energy required for wool production time step.</returns>
        private double CalculateWoolEnergy(Ruminant ind)
        {
            // TODO: wool production energy here!
            // TODO: include wool production here.

            // grow wool and cashmere from CLEM based on IAT/NABSA
            //ind.Wool += ind.Parameters.General.WoolCoefficient * ind.MetabolicIntake;
            //ind.Cashmere += ind.Parameters.General.CashmereCoefficient * ind.MetabolicIntake;

            return 0;
        }

        /// <summary>
        /// Determine the energy required for lactation.
        /// </summary>
        /// <param name="ind">Female individual.</param>
        /// <param name="updateValues">A flag to indicate whether tracking values should be updated in this calculation as call from PotenitalIntake and CalculateEnergy.</param>
        /// <param name="milkProtein">Protein required for milk production.</param>
        /// <returns>Daily energy required for lactation this time step.</returns>
        private double CalculateLactationEnergy(RuminantFemale ind, bool updateValues, ref double milkProtein)
        {
            if (ind.IsLactating | MathUtilities.IsPositive(ind.Milk.PotentialRate))
            {
                ind.Milk.Milked = 0;
                ind.Milk.Suckled = 0;

                // this is called in potential intake using last months energy available after pregnancy
                // and called again in CalculateEnergy of Weight Gain where it uses the energy available after ____ maint? from the current time step.

                // update old parameters in breed params to new approach based on energy and not L milk.
                // TODO: new intercept = 0.4 and coefficient = 0.02
                // TODO: update peak yield.
                kl = ind.Parameters.Grow24_CKCL.ELactationEfficiencyCoefficient_CK6 * ind.Intake.MDSolid + ind.Parameters.Grow24_CKCL.ELactationEfficiencyIntercept_CK5;
                double milkTime = ind.DaysLactating(events.Interval / 2.0); // assumes mid month

                // determine milk production curve to use
                double milkCurve = ind.Parameters.Lactation.MilkCurveSuckling;
                // if milking is taking place use the non-suckling curve for duration of lactation
                // otherwise use the suckling curve where there is a larger drop off in milk production
                if (ind.SucklingOffspringList.Any() == false)
                    milkCurve = ind.Parameters.Lactation.MilkCurveNonSuckling;

                // calculate milk production (eqns 66 thru 76)

                // DR (Eq. 74) => ind.ProportionMilkProductionAchieved, calculated after lactation energy determined in energy method.
                double DR = 1.0;
                if (MathUtilities.IsGreaterThanOrEqual(ind.Milk.MaximumRate, 0.0))
                    DR = ind.Milk.PotentialRate2 / ind.Milk.MaximumRate;

                double LR = (ind.Parameters.Grow24_CKCL.PotentialYieldReduction2_CL18 * DR) * ((1 - ind.Parameters.Grow24_CKCL.PotentialYieldReduction2_CL18) * DR);

                double nutritionAfterPeakLactationFactor = 1; // LB
                if (MathUtilities.IsGreaterThan(milkTime, 0.7 * ind.Parameters.Lactation.MilkPeakDay))
                    nutritionAfterPeakLactationFactor = ind.Milk.NutritionAfterPeakLactationFactor - ind.Parameters.Grow24_CKCL.PotentialYieldReduction_CL17 * (LR - DR);

                double Mm = (milkTime + ind.Parameters.Lactation.MilkOffsetDay) / ind.Parameters.Lactation.MilkPeakDay;

                double milkProductionMax = ind.Parameters.Grow24_CKCL.PeakYieldScalar_CL0[ind.NumberOfSucklings - 1] * Math.Pow(ind.Weight.StandardReferenceWeight, 0.75) * ind.Weight.RelativeSize * ind.RelativeConditionAtParturition * nutritionAfterPeakLactationFactor *
                    Math.Pow(Mm, milkCurve) * Math.Exp(milkCurve * (1 - Mm));

                double ratioMilkProductionME = (ind.Energy.AfterPregnancy * 0.94 * kl * ind.Parameters.Grow24_CG.BreedLactationEfficiencyScalar)/milkProductionMax; // Eq. 69

                double ad = Math.Max(milkTime, ratioMilkProductionME / (2 * ind.Parameters.Grow24_CKCL.PotentialYieldLactationEffect2_CL22));

                double MP1 = (ind.Parameters.Grow24_CKCL.LactationEnergyDeficit_CL7 * milkProductionMax) / (1 + Math.Exp(-(-ind.Parameters.Grow24_CKCL.PotentialLactationYieldParameter_CL19 + ind.Parameters.Grow24_CKCL.PotentialYieldMEIEffect_CL20 *
                    ratioMilkProductionME + ind.Parameters.Grow24_CKCL.PotentialYieldLactationEffect_CL21 * ad * (ratioMilkProductionME - ind.Parameters.Grow24_CKCL.PotentialYieldLactationEffect2_CL22 * ad) - ind.Parameters.Grow24_CKCL.PotentialYieldConditionEffect_CL23
                    * ind.Weight.RelativeCondition * (ratioMilkProductionME - ind.Parameters.Grow24_CKCL.PotentialYieldConditionEffect2_CL24 * ind.Weight.RelativeCondition))));

                double MP2 = Math.Min(MP1, ind.NumberOfSucklings * ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6 * Math.Pow(ind.SucklingOffspringList.Average(a => a.Weight.Live), 0.75) * (ind.Parameters.Grow24_CKCL.MilkConsumptionLimit1_CL12 + ind.Parameters.Grow24_CKCL.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.Grow24_CKCL.MilkConsumptionLimit3_CL14 * milkTime)));

                if (updateValues)
                {
                    ind.Milk.Lag = LR;
                    ind.Milk.NutritionAfterPeakLactationFactor = nutritionAfterPeakLactationFactor;
                    ind.Milk.MaximumRate = milkProductionMax;
                    ind.Milk.PotentialRate2 = MP2;
                }

                // 0.032
                ind.Milk.Protein = ind.Parameters.Grow24_CKCL.ProteinContentMilk_CL15 * MP2 / ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6;

                ind.Milk.PotentialRate = MP2;
                ind.Milk.Available = MP2 * events.Interval;
                ind.Milk.Produced = ind.Milk.Available;

                // returns the energy required for milk production
                return MP2 / 0.94 * kl * ind.Parameters.Grow24_CG.BreedLactationEfficiencyScalar;
            }
            return 0;
        }

        /// <summary>
        /// Determine the energy required for pregnancy.
        /// </summary>
        /// <param name="ind">Female individua.l</param>
        /// <param name="conceptusProtein">Protein required by conceptus for time-step(kg).</param>
        /// <param name="conceptusFat">Fat required by conceptus for time-step (kg).</param>
        /// <returns>Energy required per day for pregnancy</returns>
        private double CalculatePregnancyEnergy(RuminantFemale ind, ref double conceptusProtein, ref double conceptusFat)
        {
            if (!ind.IsPregnant)
                return 0;

            // ToDo: maybe age of conceptus should be calculated half way, or 2/3 etc through large time-steps.

            double normalWeightFetus = ind.ScaledBirthWeight * Math.Exp(ind.Parameters.Grow24_CP.FetalNormWeightParameter_CP2 * (1 - Math.Exp(ind.Parameters.Grow24_CP.FetalNormWeightParameter2_CP3 * (1 - ind.ProportionOfPregnancy()))));
            if (ind.ProportionOfPregnancy() == 0)
                ind.Weight.Fetus.Set(normalWeightFetus);

            // change in normal fetus weight across time-step
            double deltaChangeNormFetusWeight = ind.ScaledBirthWeight * Math.Exp(ind.Parameters.Grow24_CP.FetalNormWeightParameter_CP2 * (1 - Math.Exp(ind.Parameters.Grow24_CP.FetalNormWeightParameter2_CP3 * (1 - ind.ProportionOfPregnancy(events.Interval))))) - normalWeightFetus;

            // calculated after growth in time-step to avoild issue of 0 in first step especially for larger time-steps
            double relativeConditionFetus = ind.Weight.Fetus.Amount / normalWeightFetus;

            // get stunting factor
            double stunt = 1;
            if(MathUtilities.IsLessThan(ind.Weight.RelativeCondition, 1.0))
            {
                stunt = ind.Parameters.Grow24_CP.FetalGrowthPoorCondition_CP14[ind.NumberOfFetuses-1];
            }
            double CFPreg = (ind.Weight.RelativeCondition - 1) * (normalWeightFetus / (ind.Parameters.General.BirthScalar[ind.NumberOfFetuses - 1] * ind.Weight.StandardReferenceWeight));

            if(MathUtilities.IsGreaterThanOrEqual(ind.Weight.RelativeCondition, 1.0))
                ind.Weight.Fetus.Adjust(Math.Max(0.0, deltaChangeNormFetusWeight * (CFPreg+1)));
            else
                ind.Weight.Fetus.Adjust(Math.Max(0.0, deltaChangeNormFetusWeight * ((stunt * CFPreg) + 1)));

            ind.Weight.Conceptus.Set(ind.NumberOfFetuses * (ind.Parameters.Grow24_CP.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight * Math.Exp(ind.Parameters.Grow24_CP.ConceptusWeightParameter_CP6 * (1 - Math.Exp(ind.Parameters.Grow24_CP.ConceptusWeightParameter2_CP7 * (1 - ind.ProportionOfPregnancy()))))) + (ind.Weight.Fetus.Amount - normalWeightFetus));

            //ToDo: check that these next lines are truly per day!!

            // MJ per day
            double conceptusME = (ind.Parameters.Grow24_CP.ConceptusEnergyContent_CP8 * (ind.NumberOfFetuses * ind.Parameters.Grow24_CP.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight) * relativeConditionFetus *
                (ind.Parameters.Grow24_CP.ConceptusEnergyParameter_CP9 * ind.Parameters.Grow24_CP.ConceptusEnergyParameter2_CP10 / (ind.Parameters.General.GestationLength.InDays)) * 
                Math.Exp(ind.Parameters.Grow24_CP.ConceptusEnergyParameter2_CP10 * (1 - ind.DaysPregnant / ind.Parameters.General.GestationLength.InDays) + ind.Parameters.Grow24_CP.ConceptusEnergyParameter_CP9 * (1 - Math.Exp(ind.Parameters.Grow24_CP.ConceptusEnergyParameter2_CP10 * (1 - ind.ProportionOfPregnancy()))))) / 0.13;

            // kg protein per day
            double conceptusProteinReq = ind.Parameters.Grow24_CP.ConceptusProteinContent_CP11 * (ind.NumberOfFetuses * ind.Parameters.Grow24_CP.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight) * relativeConditionFetus * 
                (ind.Parameters.Grow24_CP.ConceptusProteinParameter_CP12 * ind.Parameters.Grow24_CP.ConceptusProteinParameter2_CP13 / (ind.Parameters.General.GestationLength.InDays)) * Math.Exp(ind.Parameters.Grow24_CP.ConceptusProteinParameter2_CP13 * (1 - ind.ProportionOfPregnancy()) + ind.Parameters.Grow24_CP.ConceptusProteinParameter_CP12 * (1 - Math.Exp(ind.Parameters.Grow24_CP.ConceptusProteinParameter2_CP13 * (1 - ind.ProportionOfPregnancy()))));

            conceptusProtein = conceptusProteinReq * ind.Weight.Conceptus.Amount;
            // fat (kg)
            conceptusFat = ((conceptusME * 0.13) - (conceptusProtein * 23.6)) / 39.3;

            //fetal fat and protein
            //fetal fat is conceptus fat. per individual. Assumes minimal (0) fat in placenta
            ind.Weight.FetusFat.Adjust(conceptusFat/ind.NumberOfFetuses);

            return conceptusME/events.Interval;
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
            if (manureStore is not null)
            {
                // sort by animal location to ensure correct deposit location.
                foreach (var groupInds in CurrentHerd(false).GroupBy(a => a.Location))
                {
                    manureStore.AddUncollectedManure(groupInds.Key ?? "", groupInds.Sum(a => a.Output.Manure));
                }
            }
        }

        private void ReportUnfedIndividualsWarning(IGrouping<string, Ruminant> breed, int unfed, int unfedcalves)
        {
            // alert user to unfed animals in the month as this should not happen
            if (unfed > 0)
            {
                string warn = $"individuals of [r={breed.Key}] not fed";
                string warnfull = $"Some individuals of [r={breed.Key}] were not fed in some months (e.g. [{unfed}] individuals in [{events.Clock.Today.Month}/{events.Clock.Today.Year}])\r\nFix: Check feeding strategy and ensure animals are moved to pasture or fed in yards";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
            }
            if (unfedcalves > 0)
            {
                string warn = $"calves of [r={breed.Key}] not fed";
                string warnfull = $"Some calves of [r={breed.Key}] were not fed in some months (e.g. [{unfedcalves}] individuals in [{events.Clock.Today.Month}/{events.Clock.Today.Year}])\r\nFix: Check calves are are fed, or have access to pasture (moved with mothers or separately) when no milk is available from mother";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning, warnfull);
            }
        }

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
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.Grow24 is null))
            {
                string[] memberNames = new string[] { "RuminantParametersGrowSCA" };
                results.Add(new ValidationResult($"No [RuminantParametersGrowSCA] parameters are provided for [{item.NameWithParent}]", memberNames));
            }
            return results;
        }

        #endregion

    }
}
