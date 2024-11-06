using Models.CLEM.Resources;
using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Ruminant growth activity (Oddy model phase ver RAAN)
    /// 
    /// This class represents the CLEM activity responsible for determining potential intake, determining the quality of all food eaten, and providing energy and protein for all needs (e.g. wool production, pregnancy, lactation and growth).
    /// </summary>
    /// <remarks>Rumiant death activity controls mortality, while the Breed activity is responsible for conception and births.</remarks>
    /// <authors>Animal physiology and equations for this methodology, Based on Oddy V.H., Dougherty, J.C.H., Evered, M., Clayton, E.H. and Oltjen, J.W. (2024) A revised model of energy transactions and body composition in sheep. Journal of Animal Science, Volume 102, https://doi.org/10.1093/jas/skad403</authors>
    /// <authors>Implementation of R script based equations, Adam Liedloff, CSIRO</authors>
    /// <authors>Quality control, Thomas Keogh, CSIRO</authors>
    /// <acknowledgements>This animal production component is based upon the equations developed by V.H. Oddy and J.W. Oltjen</acknowledgements>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs growth and aging of all ruminants based on Oddy et al (2024) ruminant energetics model.")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrowOddy.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral), typeof(RuminantParametersGrow24CG), typeof(RuminantParametersGrow24CI), typeof(RuminantParametersGrow24CKCL) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType },
        SingleInstance = true)]
    public class RuminantActivityGrowOddy : CLEMRuminantActivityBase, IValidatableObject, IRuminantActivityGrow
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private ProductStoreTypeManure manureStore;

        /// <inheritdoc/>
        public bool IncludeFatAndProtein { get => true; }
        /// <inheritdoc/>
        public bool IncludeVisceralProteinMass { get => true; }

        #region Common ruminant grow templating

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

            // warn about a couple settings that may have been missed and have an influence of outcomes. These are designed for expert use such as feedlot validation.
            ISummary summary = null;
            foreach (var indgrp in CurrentHerd(false).GroupBy(a => a.HerdName))
            {
                // condition-based intake reduction
                if (indgrp.First().Parameters.Grow24_CI.RelativeConditionEffect_CI20 == 1.0)
                {
                    summary = FindInScope<Summary>();
                    summary.WriteMessage(this, $"Ruminant intake reduction based on high condition is disabled for [{indgrp.Key}].{Environment.NewLine}To allow this functionality set [Parameters].[Grow24].[Grow24 CI].RelativeConditionEffect_CI20 to a value greater than [1] (default 1.5)", MessageType.Warning);
                }
                // intake reduced by quality of feed
                if (indgrp.First().Parameters.Grow24_CI.IgnoreFeedQualityIntakeAdustment)
                {
                    summary ??= FindInScope<Summary>();
                    summary.WriteMessage(this, $"Ruminant intake reduction based on intake quality is disabled for [{indgrp.Key}].{Environment.NewLine}To allow this functionality set [Parameters].[Grow24].[Grow24 CI].IgnoreFeedQualityIntakeAdustment to [False]", MessageType.Warning);
                }

                //// additional energy and weight property needed for Oddy method to track visceral protein.
                //foreach (var ind in indgrp)
                //{
                //    ind.Energy.ProteinV = new();
                //    ind.Weight.ProteinV = new();
                //}
            }
        }

        /// <summary>Function to naturally wean individuals at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // Age all individuals
            foreach (Ruminant ind in CurrentHerd())
            {
                ind.SetCurrentDate(events.Clock.Today);
            }

            // Natural weaning takes place here before animals eat or take milk from mother.
            foreach (var ind in CurrentHerd(false).Where(a => a.IsWeaned == false && MathUtilities.IsGreaterThan(a.AgeInDays, a.AgeToWeanNaturally)))
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
            if (ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 > 1 && ind.Weight.RelativeCondition > 1)
            {
                if (ind.Weight.RelativeCondition >= ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20)
                    cf = 0;
                else
                    cf = Math.Min(1.0, ind.Weight.RelativeCondition * (ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 - ind.Weight.RelativeCondition) / (ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 - 1));
            }

            // YF - Young factor SCA Eq.4, the proportion of solid intake sucklings have when low milk supply as function of age.
            double yf = 1.0;
            if (!ind.IsWeaned)
            {
                // calculate expected milk intake, part B of SCA Eq.70 with one individual (y=1)
                ind.Intake.MilkDaily.Expected = ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6 * Math.Pow(ind.AgeInDays + (events.Interval / 2.0), 0.75) * (ind.Parameters.Grow24_CKCL.MilkConsumptionLimit1_CL12 + ind.Parameters.Grow24_CKCL.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.Grow24_CKCL.MilkCurveSuckling_CL3 * (ind.AgeInDays + (events.Interval / 2.0))));  // changed CL4 -> CL3 as sure it should be the suckling curve used here. 
                double milkactual = Math.Min(ind.Intake.MilkDaily.Expected, ind.Mother.Milk.PotentialRate / ind.Mother.SucklingOffspringList.Count);
                // calculate YF
                // ToDo check that this is the potential milk calculation needed.
                yf = (1 - (milkactual / ind.Intake.MilkDaily.Expected)) / (1 + Math.Exp(-ind.Parameters.Grow24_CI.RumenDevelopmentCurvature_CI3 * (ind.AgeInDays + (events.Interval / 2.0) - ind.Parameters.Grow24_CI.RumenDevelopmentAge_CI4)));
            }

            // TF - Temperature factor. SCA Eq.5
            // NOT INCLUDED
            double tf = 1.0;

            // LF - Lactation factor SCA Eq.8
            // lactation is not supported in the SCA 2007 implementation
            double lf = 1.0;

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
                foreach (Ruminant ind in breed)
                {
                    Status = ActivityStatus.Success;

                    if (ind is RuminantFemale female && (female.IsPregnant | female.IsLactating))
                        throw new NotImplementedException("[a=RuminantActivityGrowSCA2007] does not support pregnancy or lactation.");

                    ind.Intake.AdjustIntakeBasedOnFeedQuality(false, ind);

                    CalculateEnergy(ind);

                    if (ind.IsWeaned && ind.Intake.SolidsDaily.Actual == 0 && ind.Intake.SolidsDaily.Expected > 0)
                        unfed++;
                    else if (!ind.IsWeaned && MathUtilities.IsLessThanOrEqual(ind.Intake.MilkDaily.Actual + ind.Intake.SolidsDaily.Actual, 0))
                        unfedcalves++;
                }
                ReportUnfedIndividualsWarning(breed, unfed, unfedcalves);
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

        #endregion

        /// <summary>
        /// Function to calculate energy from intake and subsequent growth using Oddy ruminant energy, fat and protein model.
        /// </summary>
        /// <remarks>
        /// B model with combined data file now used for MODNUT and ISEP phase simulations
        /// Based on version as of Dec 1, new kp and kf, new v*, new pv, new lp and lf
        /// </remarks>
        /// <param name="ind">Indivudal ruminant for calculation.</param>
        public virtual void CalculateEnergy(Ruminant ind)
        {
            // eulerStep function in R code.

            double alphaM = ind.Weight.StandardReferenceWeight * ind.Parameters.GrowOddy.shrink * ind.Parameters.GrowOddy.leanM * ind.Parameters.GrowOddy.pPrpM * ind.Parameters.GrowOddy.pMusc * ind.Parameters.General.MJEnergyPerKgProtein;
            double alphaV = ind.Parameters.GrowOddy.cs1 * ind.Intake.ME + ind.Parameters.GrowOddy.cs2 * Math.Pow(ind.Energy.Protein.Amount, 0.41) - ind.Parameters.GrowOddy.cs3 * ind.Intake.MDSolid;
            double NEG = CalculateNEG(ind, out double dwdt);

            // Pool energy change by time-step
            double dmdt = (NEG * ind.Parameters.GrowOddy.pm + ind.Parameters.GrowOddy.e0) * (1 - ind.Energy.Protein.Amount / alphaM);
            double dvdt = ind.Parameters.GrowOddy.pv * (alphaV - ind.Energy.ProteinViscera.Amount);
            double dfdt = NEG - dmdt - dvdt - dwdt;

            // Oddy tracks total energy in pools which includes the energy used to lay down the other structures (bone etc)
            // ToDo: but can this energy be lost from the individual like fat and protein?

            // Other models are recording the total energy of the protein in the pool so we move the conversion factor to saving energy, not calculation of mass
            // Note: the energy used for other compoonents is not preported anywhere if not included in the protein pool so there will be a difference in balance.

            // ToDo: This doesn't work for losses in protein as the loss energy is adjusted by PrpM and PrpV.
            // ToDo: A loss in protein energy has an equal loss in the other body components associated with it (e.g the bone etc of pPrpM and pPrpV)
            // Is this correct. I assume you lose bone structure etc in same way gained?


            // ToDo: the paper states energy is kJ, where is this converted to MJ?

            ind.Energy.Protein.Adjust(dmdt * events.Interval); // total energy in the non-visceral tissues and structures 
            ind.Energy.ProteinViscera.Adjust(dvdt * events.Interval); // total energy in the visceral tissues and structures 
            ind.Energy.Fat.Adjust(dfdt * events.Interval);

            // ToDO: where do we account for the other structures hoof, head, bone etc so it is available in our EMB reported by model?
            // Currently these are stored in the weight pools, but may not match values expected in validation datasets as different to other CLEM models.
            ind.Weight.Protein.Adjust(dmdt / ind.Parameters.GrowOddy.pPrpM / ind.Parameters.General.MJEnergyPerKgProtein);
            ind.Weight.ProteinViscera.Adjust(dvdt / ind.Parameters.GrowOddy.pPrpV / ind.Parameters.General.MJEnergyPerKgProtein);
            ind.Weight.Fat.Adjust(dfdt / ind.Parameters.General.MJEnergyPerKgFat);

            ind.Weight.UpdateEBM(ind);

            // manure per time step
            ind.Output.Manure = ind.Intake.SolidsDaily.Actual * (100 - ind.Intake.DMD) / 100 * events.Interval;
        }

        /// <summary>
        /// The Oddy Ruminant model method to calculate net t energy gain.
        /// </summary>
        /// <param name="ind">The individual being acted upon</param>
        /// <param name="dwdt">Change in wool for time step</param>
        /// <returns>Net energy gain</returns>
        private double CalculateNEG(Ruminant ind, out double dwdt)
        {
            // conceptus when added
            // double bc = 1 / ind.Parameters.GrowOddy.kc - 1;
            // lactation when added
            // double bl = 1 / ind.Parameters.GrowOddy.kl - 1;

            dwdt = 0;
            if (ind.Parameters.GrowOddy.IncludeWool)
            {
                double WInst = CalculateWool(ind);
                if (ind.Energy.ForWool == 0)
                    dwdt = WInst; //no previous woolchange so just use this step, units MJ/d
                else
                    dwdt = ind.Energy.ForWool * 0.96 + WInst * 0.04; //units MJ/d

                ind.Weight.WoolClean.Adjust(dwdt * 1000 / ind.Parameters.General.MJEnergyPerKgProtein); //gets total cwg (cumulative) in g
                ind.Weight.Wool.Adjust(ind.Weight.WoolClean.Change * ind.Parameters.Grow24_CW.CleanToGreasyCRatio_CW3);
            }

            double bf = 1 / ind.Parameters.GrowOddy.kf - 1;
            double bpm = 1 / ind.Parameters.GrowOddy.kp - 1;
            double bpv = bpm;
            double bpw = bpm;

            if (ind.Energy.Fat.Change < 0) bf = ind.Parameters.GrowOddy.lf;
            if (ind.Energy.Protein.Change < 0) bpm = ind.Parameters.GrowOddy.lp;
            if (ind.Energy.ProteinViscera.Change < 0) bpv = ind.Parameters.GrowOddy.lp;

            // change in energy is from previous timestep and will be updated in parent function
            ind.Energy.ForBasalMetabolism = ind.Parameters.GrowOddy.bm * ind.Energy.Protein.Amount + ind.Parameters.GrowOddy.bv * ind.Energy.ProteinViscera.Amount;
            ind.Energy.ForHPViscera = (1 - ind.Parameters.GrowOddy.km) * ind.Intake.ME;
            ind.Energy.ForProductFormation = bpm * ind.Energy.Protein.Change + bpv * ind.Energy.ProteinViscera.Change + bf * ind.Energy.Protein.Change + bpw * dwdt; //+ bc*dcdt + bl*dldt
            ind.Energy.UpdateProductFormationAverage();
            ind.Intake.UpdateMEAverage();
            return ind.Intake.MEAverage - ind.Energy.ForHeatProduction;
        }

        /// <summary>
        /// The Oddy Ruminant model method to calculate empty body weight.
        /// </summary>
        /// <param name="ind">The individual being acted upon</param>
        /// <returns>EmptyBodyWeight</returns>
        private double CalculateEBW(Ruminant ind)
        {
            // Total mass of protein and associated structures is stored in Protein, ProteinViscera and Fat stores (ind.Weight) as per Oddy.
            // therefore EBM is the sum of protein and fat pools.

            return ind.Weight.Protein.Amount + ind.Weight.ProteinViscera.Amount + ind.Weight.Fat.Amount;

            //ToDO: Logical issue here. If an animal has lost protein the the lost energy does not contain the production factor. Better to track MJ pf protein deposited.
            //ToDo: this can be omitted as we are tracking the EBM of individuals separately.

            // the proportion of the energy going to protein (pPrpM and pPrpV) is handled at the adjusting of energy not mass
            // double mkg = ind.Energy.Protein.Amount / ind.Parameters.General.MJEnergyPerKgProtein; // / ind.Parameters.GrowOddy.pPrpM
            // double vkg = ind.Energy.ProteinViscera.Amount / ind.Parameters.General.MJEnergyPerKgProtein; // / ind.Parameters.GrowOddy.pPrpV
            // double fkg = ind.Energy.Fat.Amount / ind.Parameters.General.MJEnergyPerKgFat;
            // return (mkg + vkg + fkg);
        }

        /// <summary>
        /// The Oddy Ruminant model method to calculate wool.
        /// </summary>
        /// <param name="ind">The individual being acted upon</param>
        /// <returns>Determine the energy required for wool growth.</returns>
        private double CalculateWool(Ruminant ind)
        {
            double shr = 1 - (0.35 * Math.Pow(ind.Intake.MDSolid, 2) - 9 * ind.Intake.MDSolid + 70) / 100;
            double Z = CalculateEBW(ind) / shr / ind.Weight.StandardReferenceWeight;
            double WZ = Z * 0.82 + 0.18;
            double WBr = (ind.Parameters.Grow24_CW.StandardFleeceWeight * 1000 / 365) / (0.26 * Math.Pow(ind.Weight.StandardReferenceWeight, 0.75) / 0.7 * 1.3);
            double PWout = WBr * WZ * ind.Intake.ME * ind.Parameters.General.MJEnergyPerKgProtein / 1000; //units here are MJ/d internally, converted from kJ
            return PWout;
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check parameters are available for all ruminants.
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.Grow24 is null))
            {
                yield return new ValidationResult($"No [RuminantParametersGrowSCA] parameters are provided for [{item.NameWithParent}]", new string[] { "RuminantParametersGrowSCA" });
            }
        }

        #endregion


    }
}
