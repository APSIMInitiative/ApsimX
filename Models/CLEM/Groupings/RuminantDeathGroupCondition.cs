using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants for determining death by a currcent condition
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityDeath))]
    [Description("Manages the death of specified ruminants based on body condition.")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantDeathGroupCondition.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantDeathGroupCondition : RuminantDeathGroup, IRuminantDeathGroup
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        /// <summary>
        /// Metric for calculating condition-based mortality
        /// </summary>
        [Description("Condition metric to use")]
        [System.ComponentModel.DefaultValue(ConditionBasedCalculationStyle.RelativeCondition)]
        [Required]
        public ConditionBasedCalculationStyle ConditionMetric { get; set; }

        /// <summary>
        /// Cut-off for condition-based mortality
        /// </summary>
        [Description("Cut-off for condition-based mortality")]
        [Required, GreaterThanValue(0)]
        public double CutOff { get; set; }

        /// <summary>
        /// Probability of dying if less than condition-based mortality cut-off
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Probability of death if below condition-based cut-off")]
        [System.ComponentModel.DefaultValue(1)]
        [Required, GreaterThanValue(0), Proportion]
        public double ProbabilityOfDying { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantDeathGroupCondition()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <inheritdoc/>
        public override void DetermineDeaths(IEnumerable<Ruminant> individuals)
        {
            IEnumerable<Ruminant> died = new List<Ruminant>();
            switch (ConditionMetric)
            {
                case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                    died = individuals.Where(a => MathUtilities.IsLessThanOrEqual(a.Weight.Live, a.Weight.HighestAttained * CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying));
                    break;
                case ConditionBasedCalculationStyle.RelativeCondition:
                    died = individuals.Where(a => MathUtilities.IsLessThanOrEqual(a.Weight.RelativeCondition, CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying));
                    break;
                case ConditionBasedCalculationStyle.BodyConditionScore:
                    died = individuals.Where(a => MathUtilities.IsLessThanOrEqual(a.BodyConditionScore, CutOff) && MathUtilities.IsLessThanOrEqual(RandomNumberGenerator.Generator.NextDouble(), ProbabilityOfDying));
                    break;
                default:
                    break;
            }

            foreach (Ruminant ind in died)
            {
                ind.Died = true;
                ind.SaleFlag = HerdChangeReason.DiedUnderweight;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"\r\n<div class=\"activityentry\">Specified individuals with a ");
            switch (ConditionMetric)
            {
                case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                    htmlWriter.Write($"\r\n<div class=\"setvalue\">proportion of current weight to maximum weight attained</span>");
                    break;
                case ConditionBasedCalculationStyle.RelativeCondition:
                    htmlWriter.Write($"\r\n<div class=\"setvalue\">relative condition</span>");
                    break;
                case ConditionBasedCalculationStyle.BodyConditionScore:
                    htmlWriter.Write($"\r\n<div class=\"setvalue\">body condition score</span>");
                    break;
                default:
                    break;
            }
            htmlWriter.Write($" less than {DisplaySummaryValueSnippet(CutOff, warnZero: true)}");
            htmlWriter.Write($" have a probability of death of {DisplaySummaryValueSnippet(ProbabilityOfDying, warnZero: true)} for the time-step ({events.Interval} days)</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }

}

