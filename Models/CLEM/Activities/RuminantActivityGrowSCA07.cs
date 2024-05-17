using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;
using APSIM.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using Models.PMF.Phen;
using Models.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant growth activity (SCA 2007 version)</summary>
    /// <summary>This class represents the CLEM activity responsible for determining potential intake, determining the quality of all food eaten, and providing energy and protein for all needs (e.g. wool production, pregnancy, lactation and growth).</summary>
    /// <remarks>Rumiant death activity controls mortality, while the Breed activity is responsible for conception and births.</remarks>
    /// <authors>Animal physiology and SCA 2007 equations for this methodology, James Dougherty, CSIRO</authors>
    /// <authors>Implementation of R script based equations, Adam Liedloff, CSIRO</authors>
    /// <authors>Quality control, Thomas Keogh, CSIRO</authors>
    /// <acknowledgements>This animal production is based upon the equations developed for SCA, Feeding Standards and published in 2007</acknowledgements>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs growth and aging of all ruminants based on Australian Feeding Standard (2007).")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGrowSCA07.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral), typeof(RuminantParametersGrow24CG), typeof(RuminantParametersGrow24CI), typeof(RuminantParametersGrow24CKCL) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType },
        SingleInstance = true)]
    public class RuminantActivityGrowSCA07 : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private ProductStoreTypeManure manureStore;

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
            if (ind.Parameters.Grow24_CI.RelativeConditionEffect_CI20 > 1 && ind.Weight.BodyCondition > 1)
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
                ind.Intake.MilkDaily.Expected = ind.Parameters.Grow24_CKCL.EnergyContentMilk_CL6 * Math.Pow(ind.AgeInDays + (events.Interval / 2.0), 0.75) * (ind.Parameters.Grow24_CKCL.MilkConsumptionLimit1_CL12 + ind.Parameters.Grow24_CKCL.MilkConsumptionLimit2_CL13 * Math.Exp(-ind.Parameters.Grow24_CKCL.MilkCurveSuckling_CL3 * (ind.AgeInDays + (events.Interval / 2.0))));  // changed CL4 -> CL3 as sure it should be the suckling curve used here. 
                double milkactual = Math.Min(ind.Intake.MilkDaily.Expected, ind.Mother.Milk.PotentialRate / ind.Mother.SucklingOffspringList.Count());
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
        /// Energy and body mass change are based on SCA 2007 - Nutrient Requirements of Domesticated Ruminants, CSIRO
        /// </remarks>
        /// <param name="ind">Indivudal ruminant for calculation.</param>
        public virtual void CalculateEnergy(Ruminant ind)
        {
            // SCA 2007 equations
            // From R Script "beef sca claem test march jd.R"
            // Created by James Dougherty

            //double pPrp = 0.216;
            //double pPrpM = 0.21;
            //double pPrpV = 0.157;
            //double kwoolSCA = 0.18;

            //prot << -(d$MSTART + d$VSTART)
            //f << -d$FSTART
            //M0 << -d$MSTART
            //V0 << -d$VSTART
            //F0 << -f

            //double EBW = -(M0 / pPrpM / 23.6 + V0 / pPrpV / 23.6 + f / 39.3);
            //#EBW <<- (M0/pPrpM/23.6 + V0/pPrpV/23.6+ f/39.3)
            
            double MEI = ind.Intake.ME;
            double MD = ind.Intake.MDSolid;
            double kMaint = 0.02 * MD + 0.5;
            double kGain = 0.006 + (MD * 0.042);

            double W = 1.09 * (ind.Weight.EmptyBodyMass + 2.9);
            double Z = W / ind.Weight.StandardReferenceWeight;

            double sexFactor = 1;

            double MEMaint = 1.4 * sexFactor * 0.26 * Math.Pow(W, 0.75) * Math.Exp(-0.03 * ind.AgeInYears) / kMaint + 0.09 * MEI; // #units MJ/d SCAHP
            //double SCAFHP = MEMaint - (0.09 * MEI);

            //double XX = -XX + kGain;
            //double NN = -NN + 1;

            double NEG;
            if (MEI > MEMaint)
            {
                NEG = kGain * (MEI - MEMaint);
            }
            else
            {
                NEG = 0.8 * (MEI - MEMaint);
            }

            //if (t ==1) {
            //XX <<- XX + (MEI-NEG)/(EBW^0.75)
            //NN <<- NN+1
            //}

            // these energy in gain values apply to all cattle except large lean breeds of cattle like Charolais, Limousin, and Simmental etc

            double R = (MEI / MEMaint) - 2;
            double E = (6.7 + R + (20.3 - R) / (1 + Math.Exp(-6 * (Z - 0.4))));

            double dEBWdt = NEG / E;
            double dprotdt = dEBWdt * ((5 - 0.1 * R) - (3.3 - 0.1 * R) / (1 + Math.Exp(-6 * (Z - 0.4))));
            double dfdt = dEBWdt * (1.7 + 1.1 * R + (23.6 - 1.1 * R) / (1 + Math.Exp(-6 * (Z - 0.4)))); //the 23.6 here for prt stays but otherwise will stick to 23.8
            // othewise biases bc of how we est IVs

            // update weight, protein and fat
            ind.Weight.Adjust(dEBWdt * events.Interval, ind); // kg
            ind.Energy.Protein.Adjust(dprotdt * events.Interval); //mj
            ind.Energy.Fat.Adjust(dfdt * events.Interval); // mj
    
            //age << -age + dt / 365
            //double SCAHP = -MEI - (dprotdt + dfdt); // units MJ/d

            //if (t == finalt)
            //{
            //    finalProt << -prot
            //    finalF << -f
            //    finalEBW << -EBW
            //}

            // manure per time step
            ind.Output.Manure = ind.Intake.SolidsDaily.Actual * (100 - ind.Intake.DMD) / 100 * events.Interval;
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
