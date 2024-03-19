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
using Models.Aqua;
using Models.PMF.Interfaces;
using Models.Interfaces;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity (SCA version)</summary>
    /// <summary>This class represents the CLEM activity responsible for determining potential intake, determining the quality of all food eaten, and providing energy and protein for all needs (e.g. wool production, pregnancy, lactation and growth).</summary>
    /// <remarks>This activity controls mortality and tracks body condition, while the Breed activity is responsible for conception and births.</remarks>
    /// <authors>Science and methodology, James Dougherty, CSIRO</authors>
    /// <authors>Code and implementation Adam Liedloff, CSIRO</authors>
    /// <authors>Quality control, Thomas Keogh, CSIRO</authors>
    /// <acknowledgements>This animal production is based upon the equations developed for SCA, Feedng Standards and implemented in GRAZPLAN (Moore, CSIRO) and APSFARM ()</acknowledgements>
    /// <version>1.0</version>
    /// <updates>Version 1.0 is consistent with SCA Feeding Standards of Domesticated Ruminants and is a major update to CLEM from the IAT/NABSA animal production, requiring changes to a number of other components and providing new parameters.</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs growth and aging of all ruminants based on Australian Feeding Standard. Only one instance of this activity is permitted")]
    [Version(2, 0, 1, "Updated to full SCA compliance")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrowSCA.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantActivityGrowSCA : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        private readonly CLEMEvents events = null;
        private ProductStoreTypeManure manureStore;
        private double kl = 0;
        private double MP2 = 0;
        private readonly FoodResourcePacket milkPacket;

        /// <summary>
        /// Perform Activity with partial resources available.
        /// </summary>
        [JsonIgnore]
        public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public RuminantActivityGrowSCA()
        {
            this.SetDefaults();
            // create milk packet to use repeatedly
            milkPacket = new FoodResourcePacket()
            {
                TypeOfFeed = FeedType.Milk,
                RumenDegradableProteinContent = 0,
            };
        }

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
                ind.BreedDetails.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.BreedDetails, events.Clock.Today, -1, ind.Parameters.General.BirthScalar[0], 999) { ID = ind.MotherID }, events.Clock.Today, ind));
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
                    ind.Intake.SolidsDaily.Received = 0;
                    ind.Intake.MilkDaily.Received = 0;
                    ind.Intake.SolidsDaily.Unneeded = 0;
                    ind.Intake.MilkDaily.Unneeded = 0;

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
            if(ind.Parameters.GrowSCA.RelativeConditionEffect_CI20 > 1 && ind.Weight.BodyCondition > 1)
            {
                if (ind.Weight.BodyCondition >= ind.Parameters.GrowSCA.RelativeConditionEffect_CI20)
                    cf = 0;
                else
                    cf = Math.Min(1.0, ind.Weight.BodyCondition * (ind.Parameters.GrowSCA.RelativeConditionEffect_CI20 - ind.Weight.BodyCondition) / (ind.Parameters.GrowSCA.RelativeConditionEffect_CI20 - 1));
            }

            // YF - Young factor SCA Eq.4, the proportion of solid intake sucklings have when low milk supply as function of age.
            double yf = 1.0;
            if (!ind.Weaned)
            {
                // calculate expected milk intake, part B of SCA Eq.70 with one individual (y=1)
                ind.Intake.MilkDaily.Expected = ind.Parameters.GrowSCA.EnergyContentMilk_CL6 * Math.Pow(ind.AgeInDays, 0.75) * (ind.Parameters.GrowSCA.MilkConsumptionLimit1_CL12 + ind.Parameters.GrowSCA.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.GrowSCA.MilkCurveSuckling_CL3 * ind.AgeInDays));  // changed CL4 -> CL3 as sure it should be the suckling curve used here. 
                double milkactual = ind.Mother.MilkProductionPotential / ind.Mother.SucklingOffspringList.Count();
                // calculate YF
                // ToDo check that this is the potential milk calculation needed.
                yf = (1 - (milkactual / ind.Intake.MilkDaily.Expected)) / (1 + Math.Exp(-ind.Parameters.GrowSCA.RumenDevelopmentCurvature_CI3 *(ind.AgeInDays - ind.Parameters.GrowSCA.RumenDevelopmentAge_CI4))); 
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
                    double mi = female.DaysLactating() / ind.Parameters.GrowSCA.PeakLactationIntakeDay_CI8; // SCA Eq.9
                    lf = 1 + ind.Parameters.GrowSCA.PeakLactationIntakeLevel_CI19[female.SucklingOffspringList.Count()] * Math.Pow(mi, ind.Parameters.GrowSCA.LactationResponseCurvature_CI9) * Math.Exp(ind.Parameters.GrowSCA.LactationResponseCurvature_CI9 * (1 - mi)); // SCA Eq.8
                    double lb = 1.0;
                    double wl = ind.Weight.RelativeSize * ((female.BodyConditionParturition - ind.Weight.BodyCondition) / female.DaysLactating()); // SCA Eq.12
                    if (female.DaysLactating() >= ind.Parameters.GrowSCA.MilkPeakDay_CL2 && wl > ind.Parameters.GrowSCA.LactationConditionLossThresholdDecay_CI14 * Math.Exp(-Math.Pow(ind.Parameters.GrowSCA.LactationConditionLossThreshold_CI13 * female.DaysLactating(), 2.0)))
                    {
                        lb = 1 - ((ind.Parameters.GrowSCA.LactationConditionLossAdjustment_CI12 * wl) / ind.Parameters.GrowSCA.LactationConditionLossThreshold_CI13); // (Eq.11)
                    }
                    if (female.SucklingOffspringList.Any())
                    {
                        // lf * lb * la (Eq.10)
                        lf *= lb * (1 - ind.Parameters.GrowSCA.ConditionAtParturitionAdjustment_CI15 + ind.Parameters.GrowSCA.ConditionAtParturitionAdjustment_CI15 * female.BodyConditionParturition);
                    }
                    else
                    {
                        // non suckling - e.g. dairy.
                        // lf * lb * lc (Eq.13)
                        lf *= lb * (1 + ind.Parameters.GrowSCA.EffectLevelsMilkProdOnIntake_CI10 * ((ind.Parameters.General.MilkPeakYield - ind.Parameters.GrowSCA.BasalMilkRelSRW_CI11 * ind.Weight.StandardReferenceWeight) / (ind.Parameters.GrowSCA.BasalMilkRelSRW_CI11 * ind.Weight.StandardReferenceWeight)));
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
            ind.Intake.SolidsDaily.MaximumExpected = Math.Max(0.0, ind.Parameters.GrowSCA.RelativeSizeScalar_CI1 * ind.Weight.StandardReferenceWeight * ind.Weight.RelativeSize * (ind.Parameters.GrowSCA.RelativeSizeQuadratic_CI2 - ind.Weight.RelativeSize));
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
                    if (ind.Weaned && ind.Intake.SolidsDaily.Actual == 0 && ind.Intake.SolidsDaily.Expected > 0)
                        unfed++;
                    else if(!ind.Weaned && MathUtilities.IsLessThanOrEqual(ind.Intake.MilkDaily.Actual + ind.Intake.SolidsDaily.Actual, 0))
                        unfedcalves++;

                    // Adjusting potential intake for digestability of fodder is now done in RuminantIntake along with concentrates and fodder.
                    // Now performed in Intake
                    if(ind is RuminantFemale rumFemale)
                        ind.Intake.AdjustIntakeBasedOnFeedQuality(rumFemale.IsLactating, ind);
                    else
                        ind.Intake.AdjustIntakeBasedOnFeedQuality(false, ind);

                    CalculateEnergy(ind);
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
        private void CalculateEnergy(Ruminant ind)
        {
            kl = 0;
            MP2 = 0;

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
                    // MJ / time step
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
                // unweaned individuals are assumed to be suckling as natural weaning rate set regardless of inclusion of a wean activity.
                // unweaned individuals without mother or milk from mother will need to try and survive on limited pasture until weaned.
                // YoungFactor in CalculatePotentialIntake determines how much these individuals can eat when milk is in shortfall

                // recalculate milk intake based on mothers updated milk production for the time step using the previous monthly potential milk intake
                ind.Intake.MilkDaily.Received = Math.Min(ind.Intake.MilkDaily.Expected, ind.MothersMilkProductionAvailable/ ind.Mother.SucklingOffspringList.Count);
                // remove consumed milk from mother.
                ind.Mother?.TakeMilk(ind.Intake.MilkDaily.Actual, MilkUseReason.Suckling);
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

                milkPacket.MetabolisableEnergyContent = ind.Parameters.GrowSCA.EnergyContentMilk_CL6;
                milkPacket.CrudeProteinContent = ind.Parameters.GrowSCA.ProteinContentMilk_CL15;
                milkPacket.Amount = ind.Intake.MilkDaily.Actual;
                ind.Intake.AddFeed(milkPacket);

                CalculateMaintenanceEnergy(ind, kml, sexEffectME);
            }
            double adjustedFeedingLevel = ind.Energy.FromIntake / ind.Energy.ForMaintenance -1;

            // Wool production
            ind.Energy.ForWool = CalculateWoolEnergy(ind);

            //TODO: add draft individual energy requirement: does this also apply to unweaned individuals? If so move outside loop

            // protein use for maintenance
            var milkStore = ind.Intake.GetStore(FeedType.Milk);
            double EndogenousUrinaryProtein = ind.Parameters.GrowSCA.BreedEUPFactor1_CM12 * Math.Log(ind.Weight.Live) - ind.Parameters.GrowSCA.BreedEUPFactor2_CM13;
            double EndogenousFecalProtein = 0.0152 * ind.Intake.SolidIntake + ((5.26 * (10 ^ -4)) * milkStore?.ME??0);
            double DermalProtein = ind.Parameters.GrowSCA.DermalLoss_CM14 * Math.Pow(ind.Weight.Live,0.75);
            // digestible protein leaving stomach from milk
            double DPLSmilk = milkStore?.CrudeProtein??0 * 0.92;
            // efficiency of using DPLS
            double kDPLS = (ind.Weaned)? ind.Parameters.GrowSCA.EfficiencyOfDPLSUseFromFeed_CG2: ind.Parameters.GrowSCA.EfficiencyOfDPLSUseFromFeed_CG2 / (1 + ((ind.Parameters.GrowSCA.EfficiencyOfDPLSUseFromFeed_CG2 / ind.Parameters.GrowSCA.EfficiencyOfDPLSUseFromMilk_CG3) -1)*(DPLSmilk / ind.Intake.DPLS) ); //EQn 103
            double proteinForMaintenance = EndogenousUrinaryProtein + EndogenousFecalProtein + DermalProtein;

            // ToDo: Do these use SRW of Female or does it include the 1.2x factor for males?
            double relativeSizeForWeightGainPurposes = Math.Min(1 - ((1 - (ind.Weight.AtBirth/ind.Weight.StandardReferenceWeight)) * Math.Exp(-(ind.Parameters.General.AgeGrowthRateCoefficient_CN1 * ind.AgeInDays) / Math.Pow(ind.Weight.StandardReferenceWeight, ind.Parameters.General.SRWGrowthScalar_CN2))), (ind.Weight.HighestAttained / ind.Weight.StandardReferenceWeight));
            double sizeFactor1ForGain = 1 / (1 + Math.Exp(-ind.Parameters.GrowSCA.GainCurvature_CG4 * (relativeSizeForWeightGainPurposes - ind.Parameters.GrowSCA.GainMidpoint_CG5)));
            double sizeFactor2ForGain = Math.Max(0, Math.Min(((relativeSizeForWeightGainPurposes - ind.Parameters.GrowSCA.ConditionNoEffect_CG6) / (ind.Parameters.GrowSCA.ConditionMaxEffect_CG7 - ind.Parameters.GrowSCA.ConditionNoEffect_CG6)), 1));

            double proteinGain1 = kDPLS * (ind.Intake.DPLS - ((proteinForMaintenance + conceptusProtein + milkProtein) / kDPLS));

            // mj/kg gain
            double energyEmptyBodyGain = ind.Parameters.GrowSCA.GrowthEnergyIntercept1_CG8 - sizeFactor1ForGain * (ind.Parameters.GrowSCA.GrowthEnergyIntercept2_CG9 - (ind.Parameters.GrowSCA.GrowthEnergySlope1_CG10 * adjustedFeedingLevel)) + sizeFactor2ForGain * (ind.Parameters.GrowSCA.GrowthEnergySlope2_CG11 * (ind.Weight.RelativeCondition - 1));
            // units = kg protein/kg gain
            double proteinContentOfGain = ind.Parameters.GrowSCA.ProteinGainIntercept1_CG12 + sizeFactor1ForGain * (ind.Parameters.GrowSCA.ProteinGainIntercept2_CG13 - ind.Parameters.GrowSCA.ProteinGainSlope1_CG14 * adjustedFeedingLevel) + sizeFactor2ForGain * ind.Parameters.GrowSCA.ProteinGainSlope2_CG15 * (ind.Weight.RelativeCondition - 1);
            // units MJ tissue gain/kg ebg
            //double proteinGainMJ = 23.6 * proteinContentOfGain;
            //double fatGainMJ = energyEmptyBodyGain - proteinGainMJ;

            double netEnergyForGain = ind.Energy.Kg * (ind.Intake.ME - (ind.Energy.ForMaintenance + ind.Energy.ForFetus + ind.Energy.ForLactation));
            if (netEnergyForGain > 0)
                netEnergyForGain *= ind.Parameters.GrowSCA.BreedGrowthEfficiencyScalar;

            double ProteinNet1 = proteinGain1 - (proteinContentOfGain * (netEnergyForGain / energyEmptyBodyGain));

            if (milkProtein > 0 && ProteinNet1 < milkProtein)
            {
                // MilkProteinLimit replaces MP2 in equations 75 and 76
                // ie it recalculates ME for lactation and protein for lactation

                // recalculate MP to replace the MP2 and recalculate milk production and Energy for lactation
                double MP = (1 + Math.Min(0, (ProteinNet1 / milkProtein))) * MP2;

                milkProtein = ind.Parameters.GrowSCA.ProteinContentMilk_CL15 * MP / ind.Parameters.GrowSCA.EnergyContentMilk_CL6;

                RuminantFemale checkFemale = ind as RuminantFemale;
                checkFemale.MilkCurrentlyAvailable = MP * events.Interval;
                checkFemale.MilkProducedThisTimeStep = checkFemale.MilkCurrentlyAvailable;

                ind.Energy.ForLactation = MP / 0.94 * kl;

                // adjusted NEG1/ 
                double NEG2 = netEnergyForGain + ind.Parameters.GrowSCA.MetabolisabilityOfMilk_CL5 * (MP2 - MP);
                double PG2 = proteinGain1 + (MP2 - MP) * (ind.Parameters.GrowSCA.MetabolisabilityOfMilk_CL5 / ind.Parameters.GrowSCA.EnergyContentMilk_CL6);
                double ProteinNet2 = PG2 - proteinContentOfGain * (NEG2 / energyEmptyBodyGain);
                ind.Energy.NetForGain = NEG2 + ind.Parameters.GrowSCA.ProteinGainIntercept1_CG12 * energyEmptyBodyGain * ((Math.Min(0, ProteinNet2) / proteinContentOfGain));
            }
            else
            {
                ind.Energy.NetForGain = netEnergyForGain + ind.BreedDetails.Parameters.GrowSCA.ProteinGainIntercept1_CG12 * energyEmptyBodyGain * (Math.Min(0, proteinGain1) / proteinContentOfGain);
            }
            double emptyBodyGainkg = ind.Energy.NetForGain / energyEmptyBodyGain;
            
            // update weight based on the time-step
            ind.Weight.Adjust(ind.Parameters.General.EBW2LW_CG18 * emptyBodyGainkg * events.Interval, ind);

            double kgProteinChange = Math.Min(proteinGain1, proteinContentOfGain * emptyBodyGainkg);
            double MJProteinChange = 23.6 * kgProteinChange;

            // protein mass on protein basis not mass of lean tissue mass. use conversvion XXXX for weight to perform checksum.
            ind.Weight.Protein.Adjust(kgProteinChange * events.Interval); // for time step
            ind.Energy.Protein.Adjust(MJProteinChange * events.Interval); // for time step

            double MJFatChange = ind.Energy.NetForGain - MJProteinChange;
            double kgFatChange = MJFatChange / 39.3;
            ind.Weight.Fat.Adjust(kgFatChange * events.Interval); // for time step
            ind.Energy.Fat.Adjust(MJFatChange * events.Interval); // for time step

            // N balance = 
            // ToDo: not currently used
            ind.Output.NitrogenBalance =  ind.Intake.CrudeProtein/ FoodResourcePacket.FeedProteinToNitrogenFactor - (milkProtein / FoodResourcePacket.MilkProteinToNitrogenFactor) - ((conceptusProtein + kgProteinChange) / FoodResourcePacket.FeedProteinToNitrogenFactor);

            // Total fecal protein
            double TFP = ind.Intake.IndigestibleUDP + ind.Parameters.GrowSCA.MicrobialProteinDigestibility_CA7 * ind.Parameters.GrowSCA.FaecalProteinFromMCP_CA8 * ind.Intake.RDPRequired + (1 - ind.Parameters.GrowSCA.MilkProteinDigestability_CA5) * milkStore?.CrudeProtein??0 + EndogenousFecalProtein;

            // Total urinary protein
            double TUP = ind.Intake.CrudeProtein - (conceptusProtein + milkProtein + kgProteinChange) - TFP - DermalProtein;
            // ToDo: not currently used
            ind.Output.NitrogenExcreted = (TFP + TUP) * events.Interval;

            ind.Output.NitrogenUrine = TUP / 6.25 * events.Interval;
            ind.Output.NitrogenFaecal = TFP / 6.25 * events.Interval;

            // Nbal should be close ish to TFP + TUP

            // increase pools from daily to timestep
            //ind.Intake.AdjustAmounts(events.Interval);
            
            // ToDo: increase energy stores to reflect time-step or make are reported as per day

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
            km *= ind.Parameters.GrowSCA.BreedMainenanceEfficiencyScalar;

            // Note: Energy.ToMove and Energy.ToGraze are calulated in Grazing Activity.
            ind.Energy.ToMove /= km;
            ind.Energy.ToGraze /= km;

            double rdpReq;
            // todo: check ind.BreedParams.EMaintExponent as in the params is actually CM3 maintenanceExponentForAge
            ind.Energy.ForBasalMetabolism = ((ind.Parameters.GrowSCA.FHPScalar_CM2 * sexEffect * Math.Pow(ind.Weight.Live, 0.75)) * Math.Max(Math.Exp(-ind.Parameters.GrowSCA.MainExponentForAge_CM3 * ind.AgeInDays), ind.Parameters.GrowSCA.AgeEffectMin_CM4) * (1 + ind.Parameters.GrowSCA.MilkScalar_CM5 * ind.Intake.ProportionMilk)) / km;
            ind.Energy.ForHPViscera = ind.Parameters.GrowSCA.HPVisceraFL_CM1 * ind.Energy.FromIntake;
            ind.Energy.ForMaintenance = ind.Energy.ForBasalMetabolism + ind.Energy.ForGrazing + ind.Energy.ForHPViscera;

            double adjustedFeedingLevel = -1;
            if(MathUtilities.GreaterThan(ind.Energy.FromIntake,0,2) & MathUtilities.GreaterThan(ind.Energy.ForMaintenance, 0,2 ))
            {
                adjustedFeedingLevel = (ind.Energy.FromIntake / ind.Energy.ForMaintenance) - 1;
            }
            rdpReq = CalculateCrudeProtein(ind, adjustedFeedingLevel);

            ind.Intake.CalculateDigestibleProteinLeavingStomach(rdpReq, ind.Parameters.GrowSCA.MilkProteinDigestability_CA5);
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
                            DPrf = 1 - ind.Parameters.GrowSCA.RumenDegradabilityConcentrateSlope_CRD3 * feedingLevel; //Eq50
                            FMEIrf = 1; // set as 1 and there is no reduction for concentrates.
                            break;
                        case FeedType.HaySilage:
                            DPrf = 1 - (ind.Parameters.GrowSCA.RumenDegradabilityIntercept_CRD1 - ind.Parameters.GrowSCA.RumenDegradabilitySlope_CRD2 * store.Value.Details?.DryMatterDigestibility ?? 0) * feedingLevel; // DMD in proportion
                            FMEIrf = 1; // add later depending on feed type, need to add new types PastureTrop, PastureTemp, HaySilage (non-grazed forage). ?? Lucena
                            break;
                        case FeedType.Milk:
                            FMEIrf = 0; // ignore milk. solids only
                            break;
                        case FeedType.PastureTemperate:
                            DPrf = 1 - (ind.Parameters.GrowSCA.RumenDegradabilityIntercept_CRD1 - ind.Parameters.GrowSCA.RumenDegradabilitySlope_CRD2 * store.Value.Details?.DryMatterDigestibility ?? 0) * feedingLevel; // DMD in proportion
                            FMEIrf = 1;
                            break;
                        case FeedType.PastureTropical:
                            DPrf = 1 - (ind.Parameters.GrowSCA.RumenDegradabilityIntercept_CRD1 - ind.Parameters.GrowSCA.RumenDegradabilitySlope_CRD2 * store.Value.Details?.DryMatterDigestibility ?? 0) * feedingLevel; // DMD in proportion
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

            return (ind.Parameters.GrowSCA.RumenDegradableProteinIntercept_CRD4 + ind.Parameters.GrowSCA.RumenDegradableProteinSlope_CRD5 * (1 - Math.Exp(-ind.Parameters.GrowSCA.RumenDegradableProteinExponent_CRD6 *
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

            // grow wool and cashmere
            // TODO: move to the Calculate Wool Energy method
            //ind.Wool += ind.BreedParams.WoolCoefficient * ind.MetabolicIntake;
            //ind.Cashmere += ind.BreedParams.CashmereCoefficient * ind.MetabolicIntake;

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
            if (ind.IsLactating | MathUtilities.IsPositive(ind.MilkProductionPotential))
            {
                ind.Milk.Milked = 0;
                ind.Milk.Suckled = 0;

                // this is called in potential intake using last months energy available after pregnancy
                // and called again in CalculateEnergy of Weight Gain where it uses the energy available after ____ maint? from the current time step.

                // update old parameters in breed params to new approach based on energy and not L milk.
                // TODO: new intercept = 0.4 and coefficient = 0.02
                // TODO: update peak yield.
                kl = ind.Parameters.GrowSCA.ELactationEfficiencyCoefficient_CK6 * ind.Intake.ME + ind.Parameters.GrowSCA.ELactationEfficiencyIntercept_CK5;
                double milkTime = ind.DaysLactating(events.Interval / 2.0); // assumes mid month

                // determine milk production curve to use
                double milkCurve = ind.Parameters.GrowSCA.MilkCurveSuckling_CL3;
                // if milking is taking place use the non-suckling curve for duration of lactation
                // otherwise use the suckling curve where there is a larger drop off in milk production
                if (ind.SucklingOffspringList.Any() == false)
                    milkCurve = ind.Parameters.GrowSCA.MilkCurveNonSuckling_CL4;

                // calculate milk production (eqns 66 thru 76)

                // DR (Eq. 74) => ind.ProportionMilkProductionAchieved, calculated after lactation energy determined in energy method.

                double LR = ind.Parameters.GrowSCA.PotentialYieldReduction_CL17 * ind.ProportionMilkProductionAchieved + (1 - ind.Parameters.GrowSCA.PotentialYieldReduction_CL17) * ind.MilkLag; // Eq.(73) 
                double nutritionAfterPeakLactationFactor = 0;
                if (milkTime > ind.Parameters.GrowSCA.AdjustmentOfPotentialYieldReduction_CL16 * ind.Parameters.GrowSCA.MilkPeakDay_CL2)
                    nutritionAfterPeakLactationFactor = ind.NutritionAfterPeakLactationFactor - ind.Parameters.GrowSCA.PotentialYieldReduction2_CL18 * (LR - ind.ProportionMilkProductionAchieved);  // Eq.(72) 
                else
                    nutritionAfterPeakLactationFactor = 1;

                double Mm = (milkTime + ind.Parameters.GrowSCA.MilkOffsetDay_CL1) / ind.Parameters.GrowSCA.MilkPeakDay_CL2;

                double milkProductionMax = ind.Parameters.GrowSCA.PeakYieldScalar_CL0[ind.NumberOfFetuses-1] * Math.Pow(ind.Weight.StandardReferenceWeight, 0.75) * ind.Weight.RelativeSize * ind.RelativeConditionAtParturition * nutritionAfterPeakLactationFactor *
                    Math.Pow(Mm, milkCurve) * Math.Exp(milkCurve * (1 - Mm));

                if(updateValues)
                {
                    ind.ProportionMilkProductionAchieved = ind.MilkProducedThisTimeStep / ind.MilkProductionPotential;
                    ind.MilkLag = LR;
                    ind.NutritionAfterPeakLactationFactor = nutritionAfterPeakLactationFactor;
                }

                double ratioMilkProductionME = (ind.Energy.AfterPregnancy * 0.94 * kl * ind.Parameters.GrowSCA.BreedLactationEfficiencyScalar)/milkProductionMax; // Eq. 68

                double ad = Math.Max(milkTime, ratioMilkProductionME / (2 * ind.Parameters.GrowSCA.PotentialYieldLactationEffect2_CL22));

                double MP1 = (ind.Parameters.GrowSCA.LactationEnergyDeficit_CL7 * milkProductionMax) / (1 + Math.Exp(-(-ind.Parameters.GrowSCA.PotentialLactationYieldParameter_CL19 + ind.Parameters.GrowSCA.PotentialYieldMEIEffect_CL20 *
                    ratioMilkProductionME + ind.Parameters.GrowSCA.PotentialYieldLactationEffect_CL21 * ad * (ratioMilkProductionME - ind.Parameters.GrowSCA.PotentialYieldLactationEffect2_CL22 * ad) - ind.Parameters.GrowSCA.PotentialYieldConditionEffect_CL23
                    * ind.Weight.RelativeCondition * (ratioMilkProductionME - ind.Parameters.GrowSCA.PotentialYieldConditionEffect2_CL24 * ind.Weight.RelativeCondition))));

                MP2 = Math.Min(MP1, ind.SucklingOffspringList.Count * ind.Parameters.GrowSCA.EnergyContentMilk_CL6 * Math.Pow(ind.SucklingOffspringList.Average(a => a.Weight.Live), 0.75) * (ind.Parameters.GrowSCA.MilkConsumptionLimit1_CL12 + ind.Parameters.GrowSCA.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.GrowSCA.MilkConsumptionLimit3_CL14 * milkTime)));

                // 0.032
                ind.Milk.Protein = ind.Parameters.GrowSCA.ProteinContentMilk_CL15 * MP2 / ind.Parameters.GrowSCA.EnergyContentMilk_CL6;

                ind.Milk.Available = MP2 * events.Interval;
                ind.MilkProducedThisTimeStep = ind.MilkCurrentlyAvailable;

                // returns the energy required for milk production
                return MP2 / 0.94 * kl * ind.Parameters.GrowSCA.BreedLactationEfficiencyScalar;
            }
            return 0;
        }

        /// <summary>
        /// Determine the energy required for pregnancy.
        /// </summary>
        /// <param name="ind">Female individua.l</param>
        /// <param name="conceptusProtein">Protein required by conceptus (kg).</param>
        /// <param name="conceptusFat">Fat required by conceptus (kg).</param>
        /// <returns>Energy required for fetus this time step.</returns>
        private double CalculatePregnancyEnergy(RuminantFemale ind, ref double conceptusProtein, ref double conceptusFat)
        {
            if (!ind.IsPregnant)
            {
                conceptusProtein = 0;
                conceptusFat = 0;
                return 0;
            }

            // ToDo: maybe age of conceptus should be calculated half way, or 2/3 etc through large time-steps.

            // Todo: this is not used and was commented out at the end of conceptus Weight
            double normalWeightFetus = ind.ScaledBirthWeight * Math.Exp(ind.Parameters.GrowSCA.FetalNormWeightParameter_CP2 * (1 - Math.Exp(ind.Parameters.GrowSCA.FetalNormWeightParameter2_CP3 * (1 - ind.ProportionOfPregnancy))));

            //ToDo: Fix fetus weight parameter
            double conceptusWeight = ind.NumberOfOffspring * (ind.Parameters.GrowSCA.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight * Math.Exp(ind.Parameters.GrowSCA.ConceptusWeightParameter_CP6 * (1 - Math.Exp(ind.Parameters.GrowSCA.ConceptusWeightParameter2_CP7 * (1 - ind.ProportionOfPregnancy))))      );// + (fetusWeight - normalWeightFetus));
            double relativeConditionFoet = double.NaN;

            // MJ per day
            double conceptusME = (ind.Parameters.GrowSCA.ConceptusEnergyContent_CP8 * (ind.NumberOfOffspring * ind.Parameters.GrowSCA.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight) * relativeConditionFoet *
                (ind.Parameters.GrowSCA.ConceptusEnergyParameter_CP9 * ind.Parameters.GrowSCA.ConceptusEnergyParameter2_CP10 / (ind.Parameters.General.GestationLength.InDays)) * 
                Math.Exp(ind.Parameters.GrowSCA.ConceptusEnergyParameter2_CP10 * (1 - ind.DaysPregnant / ind.Parameters.General.GestationLength.InDays) + ind.Parameters.GrowSCA.ConceptusEnergyParameter_CP9 * (1 - Math.Exp(ind.Parameters.GrowSCA.ConceptusEnergyParameter2_CP10 * (1 - ind.ProportionOfPregnancy))))) / 0.13;

            // kg protein per day
            double conceptusProteinReq = ind.Parameters.GrowSCA.ConceptusProteinContent_CP11 * (ind.NumberOfOffspring * ind.Parameters.GrowSCA.ConceptusWeightRatio_CP5 * ind.ScaledBirthWeight) * relativeConditionFoet * 
                (ind.Parameters.GrowSCA.ConceptusProteinParameter_CP12 * ind.Parameters.GrowSCA.ConceptusProteinParameter2_CP13 / (ind.Parameters.General.GestationLength.InDays)) * Math.Exp(ind.Parameters.GrowSCA.ConceptusProteinParameter2_CP13 * (1 - ind.ProportionOfPregnancy) + ind.Parameters.GrowSCA.ConceptusProteinParameter_CP12 * (1 - Math.Exp(ind.Parameters.GrowSCA.ConceptusProteinParameter2_CP13 * (1 - ind.ProportionOfPregnancy))));

            conceptusProtein = conceptusProteinReq * conceptusWeight;
            conceptusFat = (conceptusME - (conceptusProtein * 23.6)) / 39.3;

            return conceptusME; // ToDo: daily? * events.Interval;
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

        /// <summary>Function to determine which animals have died and remove from the population.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalDeath")]
        private void OnCLEMAnimalDeath(object sender, EventArgs e)
        {
            //foreach (Ruminant ind in CurrentHerd())
            //{
            //    double mr = ind.Parameters.GrowSCA.BasalMortalityRate_CD1;
            //    // work out weaner proportion age.. x weaned to y 12 months.
            //    if (ind.IsWeaner)
            //        mr += (ind.Parameters.GrowSCA.UpperLimitForMortalityInWeaners_CD13 - ind.Parameters.GrowSCA.BasalMortalityRate_CD1) * (ind.DaysSinceWeaned / (365 - (ind.AgeInDays - ind.DaysSinceWeaned)));
                
            //    // ToDo: Check that the normalised weight difference is in fact the normalised weight calculated at the end of the time step - current normalised weight (last calculated start of time step)
            //    if (0.1 * ind.EmptyBodyMassChange < 0.2 * (ind.CalculateNormalisedWeight(ind.AgeInDays + events.Interval) - ind.NormalisedAnimalWeight) / events.Interval)
            //        mr += ind.Parameters.GrowSCA.EffectBCOnMortality1_CD2 * Math.Max(0, (ind.Parameters.GrowSCA.EffectBCOnMortality2_CD3 * ind.BodyCondition));

            //    ind.Died = (RandomNumberGenerator.Generator.NextDouble() <= mr*events.Interval);
            //}

            //List<Ruminant> died = HerdResource.Herd.Where(a => a.Died).ToList();
            //foreach (Ruminant ind in died)
            //    ind.SaleFlag = HerdChangeReason.DiedUnderweight;

            //// TODO: separate foster from real mother for genetics
            //// to be placed before individuals are removed and will need a rethink of whole section.
            ////// check for death of mother with sucklings and try foster sucklings
            ////IEnumerable<RuminantFemale> mothersWithSuckling = died.OfType<RuminantFemale>().Where(a => a.SucklingOffspringList.Any());
            ////List<RuminantFemale> wetMothersAvailable = died.OfType<RuminantFemale>().Where(a => a.IsLactating & a.SucklingOffspringList.Count() == 0).OrderBy(a => a.DaysLactating).ToList();
            ////int wetMothersAssigned = 0;
            ////if (wetMothersAvailable.Any())
            ////{
            ////    if(mothersWithSuckling.Any())
            ////    {
            ////        foreach (var deadMother in mothersWithSuckling)
            ////        {
            ////            foreach (var suckling in deadMother.SucklingOffspringList)
            ////            {
            ////                if(wetMothersAssigned < wetMothersAvailable.Count)
            ////                {
            ////                    suckling.Mother = wetMothersAvailable[wetMothersAssigned];
            ////                    wetMothersAssigned++;
            ////                }
            ////                else
            ////                    break;
            ////            }
            ////        }
            ////    }
            ////}

            //HerdResource.RemoveRuminant(died, this);
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
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.GrowSCA is null))
            {
                string[] memberNames = new string[] { "RuminantParametersGrowSCA" };
                results.Add(new ValidationResult($"No [RuminantParametersGrowSCA] parameters are provided for [{item.NameWithParent}]", memberNames));
            }
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.Feeding is null))
            {
                string[] memberNames = new string[] { "RuminantParametersGrowSCA" };
                results.Add(new ValidationResult($"No [RuminantParametersFeeding] parameters are provided for [{item.NameWithParent}]", memberNames));
            }
            return results;
        }

        #endregion

    }
}
