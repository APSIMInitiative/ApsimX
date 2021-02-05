using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Advanced ruminant conception for first conception less than 12 months, 12-24 months, 2nd calf and 3+ calf
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Advanced ruminant conception for first pregnancy less than 12 months, 12-24 months, 24 months, 2nd calf and 3+ calf")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantConceptionCondition.htm")]
    public class RuminantConceptionByCondition : CLEMModel, IConceptionModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionByCondition()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Condition cutoff for conception
        /// </summary>
        [Description("Condition index (wt/normalised wt) below which no conception")]
        [Required, GreaterThanValue(0)]
        public double ConditionCutOff { get; set; }

        /// <summary>
        /// Calculate conception rate for a female based on condition score
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns></returns>
        public double ConceptionRate(RuminantFemale female)
        {
            return (female.RelativeCondition >= ConditionCutOff) ? 1 : 0;
        }

        #region descriptive summary 

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write("Conception is determined by animal condition measured as the ratio of live weight to normalised weight for age.\r\nNo breeding females will concieve if this ratio is below ");
                if (ConditionCutOff == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">No set</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + ConditionCutOff.ToString("0.0##") + "</span>");
                }
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}