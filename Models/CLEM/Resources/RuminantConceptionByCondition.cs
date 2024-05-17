using Models.Core;
using Models.Core.Attributes;
using Models.GrazPlan;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant conception based on body condition: current weight as prop or high weight
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Specify ruminant conception based on individual's condition")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantConceptionCondition.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantType) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class RuminantConceptionByCondition : CLEMModel, IConceptionModel
    {
        /// <summary>
        /// Style of calculating condition-based conception
        /// </summary>
        [Description("Style of calculating condition-based conception")]
        [System.ComponentModel.DefaultValue(ConditionBasedCalculationStyle.None)]
        [Required]
        public ConditionBasedCalculationStyle ConditionBasedConceptionStyle { get; set; }
        /// <summary>
        /// Cut-off for condition-based conception
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Cut-off for condition-based conception")]
        [Required, GreaterThanEqualValue(0)]
        public double ConditionBasedConceptionCutOff { get; set; }
        /// <summary>
        /// Probability of dying if less than condition-based mortality cut-off
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Probability of conception when above cut-off")]
        [System.ComponentModel.DefaultValue(1)]
        [Required, Proportion, GreaterThanValue(0)]
        public double ConditionBasedConceptionProbability { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionByCondition()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Calculate conception rate for a female based on condition score
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns>Conception rate (0-1)</returns>
        /// <remarks>A negative value for Condition index will use the Body Condition Score approach</remarks>
        public double ConceptionRate(RuminantFemale female)
        {
            switch(ConditionBasedConceptionStyle)
            {
                case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                    return (female.Weight.Live >= female.Weight.HighestAttained * ConditionBasedConceptionCutOff) ? ConditionBasedConceptionProbability : 0;
                case ConditionBasedCalculationStyle.RelativeCondition:
                    return (female.Weight.RelativeCondition >= ConditionBasedConceptionCutOff) ? ConditionBasedConceptionProbability : 0;
                case ConditionBasedCalculationStyle.BodyConditionScore:
                    return (female.BodyConditionScore >= ConditionBasedConceptionCutOff) ? ConditionBasedConceptionProbability : 0;
                case ConditionBasedCalculationStyle.None:
                    return 1;
                default:
                    throw new NotImplementedException($"No conception estimate available for style {ConditionBasedConceptionStyle}");
            }
        }

        #region descriptive summary 

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("<div class=\"activityentry\">");
            htmlWriter.Write("Females ");
            switch (ConditionBasedConceptionStyle)
            {
                case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                    htmlWriter.Write("with a ratio of live weight to highest weight achieved greater than or equal to ");
                    break;
                case ConditionBasedCalculationStyle.RelativeCondition:
                    htmlWriter.Write("with a relative condition (live weight over normalised weight) greater than or equal to ");
                    break;
                case ConditionBasedCalculationStyle.BodyConditionScore:
                    htmlWriter.Write("with a Body Condition Score greater than or equal to ");
                    break;
                case ConditionBasedCalculationStyle.None:
                    htmlWriter.Write("");
                    break;
                default:
                    htmlWriter.Write("with <span class=\"errorlink\">Undefined style selected</span> ");
                    break;
            }
            if (ConditionBasedConceptionStyle != ConditionBasedCalculationStyle.None)
            {
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ConditionBasedConceptionCutOff, warnZero: true)}");
            }
            htmlWriter.Write($" will have a {CLEMModel.DisplaySummaryValueSnippet(ConditionBasedConceptionProbability, warnZero: true)} probability of conceiving.");
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }
}