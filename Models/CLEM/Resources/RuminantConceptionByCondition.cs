using Models.CLEM.Extras;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
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
        /// Calculate conception rate for a female based on condition score
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns>Conception rate (0-1)</returns>
        /// <remarks>A negative value for Condition index will use the Body Condition Score approach</remarks>
        public double ConceptionRate(RuminantFemale female)
        {
            return ConditionBasedConceptionStyle switch
            {
                ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive => (female.Weight.EmptyBodyMass >= female.Weight.EmptyBodyMassHighest * ConditionBasedConceptionCutOff) ? ConditionBasedConceptionProbability : 0,
                ConditionBasedCalculationStyle.RelativeCondition => (female.Weight.RelativeCondition >= ConditionBasedConceptionCutOff) ? ConditionBasedConceptionProbability : 0,
                ConditionBasedCalculationStyle.BodyConditionScore => (female.BodyConditionScore >= ConditionBasedConceptionCutOff) ? ConditionBasedConceptionProbability : 0,
                ConditionBasedCalculationStyle.None => 1,
                _ => throw new NotImplementedException($"No conception estimate available for style {ConditionBasedConceptionStyle}"),
            };
        }
    }
}