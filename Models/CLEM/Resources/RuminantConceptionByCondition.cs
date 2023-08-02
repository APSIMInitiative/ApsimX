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
    public class RuminantConceptionByCondition : CLEMModel, IConceptionModel
    {

        /// <summary>
        /// Condition cutoff for conception
        /// </summary>
        [Description("Condition index (wt/normalised wt for age) below which no conception")]
        [Required, GreaterThanValue(0)]
        public double ConditionCutOff { get; set; }

        /// <summary>
        /// Maximum probability of conceiving given condition satisfied
        /// </summary>
        [Description("Maximum probability of conceiving")]
        [Required, Proportion, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValueAttribute(1)]
        public double MaximumConceptionProbability { get; set; }

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
        /// <returns></returns>
        public double ConceptionRate(RuminantFemale female)
        {
            return (female.RelativeCondition >= ConditionCutOff) ? MaximumConceptionProbability : 0;
        }

        #region descriptive summary 

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write("Conception is determined by animal condition measured as the ratio of live weight to normalised weight for age.\r\nNo breeding females will concieve if this ratio is below ");
                if (ConditionCutOff == 0)
                    htmlWriter.Write("<span class=\"errorlink\">No set</span>");
                else
                    htmlWriter.Write("<span class=\"setvalue\">" + ConditionCutOff.ToString("0.0##") + "</span>");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}