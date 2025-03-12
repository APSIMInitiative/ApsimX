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
using static Models.Core.ScriptCompiler;
using Models.CLEM.Interfaces;

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
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral), typeof(RuminantParametersGrowPFCG), typeof(RuminantParametersGrowPFCI), typeof(RuminantParametersGrowPFCKCL) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType, ModelAssociationStyle.DescendentOfRuminantType },
        SingleInstance = true)]
    public class RuminantActivityGrowSCA07 : CLEMRuminantActivityBase, IValidatableObject, IRuminantActivityGrow
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private ProductStoreTypeManure manureStore;

        /// <inheritdoc/>
        public bool IncludeFatAndProtein { get => true; }
        /// <inheritdoc/>
        public bool IncludeVisceralProteinMass { get => false; }

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
            foreach (var ind in CurrentHerd(false).GroupBy(a => a.HerdName))
            {
                // condition-based intake reduction
                if (ind.First().Parameters.GrowPF_CI.RelativeConditionEffect_CI20 == 1.0)
                {
                    summary = FindInScope<Summary>();
                    summary.WriteMessage(this, $"Ruminant intake reduction based on high condition is disabled for [{ind.Key}].{Environment.NewLine}To allow this functionality set [Parameters].[GrowPF].[GrowPF CI].RelativeConditionEffect_CI20 to a value greater than [1] (default 1.5)", MessageType.Warning);
                }
                // intake reduced by quality of feed
                if (ind.First().Parameters.GrowPF_CI.IgnoreFeedQualityIntakeAdustment)
                {
                    summary ??= FindInScope<Summary>();
                    summary.WriteMessage(this, $"Ruminant intake reduction based on intake quality is disabled for [{ind.Key}].{Environment.NewLine}To allow this functionality set [Parameters].[GrowPF].[GrowPF CI].IgnoreFeedQualityIntakeAdustment to [False]", MessageType.Warning);
                }
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
                ind.SetCurrentDate(events.Clock.Today);

            // Natural weaning takes place here before animals eat or take milk from mother.
            foreach (var ind in CurrentHerd(false).Where(a => a.IsWeaned == false && MathUtilities.IsGreaterThan(a.AgeInDays, a.AgeToWeanNaturally)))
            {
                ind.Wean(true, "Natural", events.Clock.Today);
                // report wean. If mother has died create temp female with the mother's ID for reporting only
                ind.Parameters.Details.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother ?? new RuminantFemale(ind.MotherID), events.Clock.Today, ind));
            }
        }

        /// <summary>Function to determine all individuals potential intake and suckling intake after milk consumption from mother.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPotentialIntake")]
        private void OnCLEMPotentialIntake(object sender, EventArgs e)
        {
            RuminantActivityGrowPF.CalculateHerdPotentialIntake(CurrentHerd(false), events.Interval);
        }

        /// <summary>Function to calculate growth of herd for the time-step</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalWeightGain")]
        private void OnCLEMAnimalWeightGain(object sender, EventArgs e)
        {
            Status = ActivityStatus.NotNeeded;
            foreach (var ruminant in CurrentHerd(false).Where(a => a.IsSuckling == false))
            {
                Status = ActivityStatus.Success;
                if (ruminant is RuminantFemale female)
                {
                    CalculateEnergy(ruminant);
                    foreach (var suckling in female.SucklingOffspringList)
                    {
                        CalculateEnergy(ruminant);
                    }
                }
                else
                    CalculateEnergy(ruminant);
            }
            RuminantActivityGrowPF.ReportUnfedIndividualsWarning(CurrentHerd(false), Warnings, Summary, this, events);
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

            //prot <<- (d$MSTART + d$VSTART)
            //f <<- d$FSTART
            //M0 <<- d$MSTART
            //V0 <<- d$VSTART
            //F0 <<- f

            //double EBW = -(M0 / pPrpM / 23.6 + V0 / pPrpV / 23.6 + f / 39.3);
            //#EBW <<- (M0/pPrpM/23.6 + V0/pPrpV/23.6+ f/39.3)

            double MEI = ind.Intake.ME;
            double MD = ind.Intake.MDSolid;
            double kMaint = 0.02 * MD + 0.5;
            double kGain = 0.006 + (MD * 0.042);

            double W = 1.09 * (ind.Weight.EmptyBodyMass + 2.9);
            double Z = W / ind.Weight.StandardReferenceWeight;

            if (ind.Parameters.General.IncludeWool)
            {
                double pwInst = CalculateWool(ind);

                if (ind.Weight.Protein.ForWool == 0)
                {
                    ind.Weight.Protein.ForWool = pwInst;
                }
                else
                {
                    ind.Weight.Protein.ForWool = (ind.Weight.Protein.ForWool * 0.96) + (pwInst * 0.04);
                }

                // net energy into wool
                double nEWool = Math.Max(((24 * (ind.Weight.Protein.ForWool - (0.004 * Z))) / ind.Parameters.GrowPF_CW.CleanToGreasyCRatio_CW3), 0.0);

                // retained energy
                double RECleanWool = 0;
                if (nEWool > 0)
                {
                    RECleanWool = 24 * ind.Weight.Protein.ForWool;
                }

                double pwActual = (RECleanWool / 24) * events.Interval;
                ind.Energy.ForWool = nEWool / 0.18 * events.Interval ;

                ind.Weight.Wool.Adjust(pwActual / ind.Parameters.GrowPF_CW.CleanToGreasyCRatio_CW3);
                ind.Weight.WoolClean.Adjust(pwActual);
            }

            double sexFactor = 1;

            double MEMaint = ind.Parameters.GrowSCA07.MaintenanceFactor * sexFactor * 0.26 * Math.Pow(W, 0.75) * Math.Exp(-0.03 * ind.AgeInYears) / kMaint + 0.09 * MEI; // #units MJ/d SCAHP
            ind.Energy.ForBasalMetabolism = MEMaint;
            //double SCAFHP = MEMaint - (0.09 * MEI);

            //double XX = -XX + kGain;
            //double NN = -NN + 1;

            double NEG;
            if (MEI > MEMaint)
            {
                NEG = kGain * (MEI - MEMaint - ind.Energy.ForWool);
            }
            else
            {
                NEG = 0.8 * (MEI - MEMaint - ind.Energy.ForWool);
            }

            //if (t ==1) {
            //XX <<- XX + (MEI-NEG)/(EBW^0.75)
            //NN <<- NN+1
            //}

            // these energy in gain values apply to all cattle except large lean breeds of cattle like Charolais, Limousin, and Simmental etc

            double R = (MEI / MEMaint) - 2;
            double E = (6.7 + R + ((20.3 - R) / (1 + Math.Exp(-6 * (Z - 0.4)))));

            double dEBWdt = NEG / E;
            double dprotdt = dEBWdt * ((5 - 0.1 * R) - ((3.3 - 0.1 * R) / (1 + Math.Exp(-6 * (Z - 0.4)))));
            double dfdt = dEBWdt * (1.7 + 1.1 * R + ((23.6 - 1.1 * R) / (1 + Math.Exp(-6 * (Z - 0.4))))); //the 23.6 here for prt stays but otherwise will stick to 23.8
            // othewise biases bc of how we est IVs

            ind.Energy.Protein.Adjust(dprotdt * events.Interval); //mj
            ind.Energy.Fat.Adjust(dfdt * events.Interval); // mj
            ind.Weight.Protein.Adjust(dprotdt / 23.6 * events.Interval); 
            ind.Weight.Fat.Adjust(dfdt / 39.3 * events.Interval);

            // update weight, protein and fat
            ind.Weight.AdjustByEBMChange(dEBWdt * events.Interval, ind);
            //ind.Weight.AdjustByEBMChange(dEBWdt * events.Interval, ind); // kg

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
        /// The SCA 2007 equations to calculate wool growth based on GrassGro.
        /// </summary>
        /// <param name="ind">The individual being acted upon</param>
        /// <returns>Determine the energy required for wool growth.</returns>
        private static double CalculateWool(Ruminant ind)
        {
            double woolAgeFac = 0.25 + (0.75 * (1 - Math.Exp(-0.025 * ind.AgeInDays)));
            return 0.016 * (ind.Parameters.GrowPF_CW.StandardFleeceWeight / ind.Weight.StandardReferenceWeight) * woolAgeFac * 1 * ind.Intake.ME;
        }

        /// <inheritdoc/>
        public void SetProteinAndFatAtBirth(Ruminant newborn)
        {
            throw new NotImplementedException("Birth fat and protein not currently calculated!");
        }

        /// <inheritdoc/>
        public void SetInitialFatProtein(Ruminant individual, RuminantTypeCohort cohort, double initialWeight)
        {
            throw new NotImplementedException("Initial fat and protein not currently calculated!");
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
            RuminantActivityGrowPF.CalculateManure(manureStore, CurrentHerd(false));
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check parameters are available for all ruminants.
            foreach (var item in FindAllInScope<RuminantType>().Where(a => a.Parameters.GrowSCA07 is null))
            {
                yield return new ValidationResult($"No [RuminantParametersGrowSCA07] parameters are provided for [{item.NameWithParent}]", new string[] { "RuminantParametersGrowSCA" });
            }
        }

        #endregion

    }
}
