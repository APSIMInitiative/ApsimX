using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// The simplest ruminant conception using a single curve
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Advanced ruminant conception for first pregnancy less than 12 months, 12-24 months, 24 months, 2nd calf and 3+ calf")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantConceptionCurve.htm")]
    public class RuminantConceptionCurve: CLEMModel, IConceptionModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionCurve()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Conception rate coefficient of breeder
        /// </summary>
        [Description("Conception rate coefficient of breeder PW")]
        [Required]
        public double ConceptionRateCoefficent { get; set; }

        /// <summary>
        /// Conception rate intercept of breeder
        /// </summary>
        [Description("Conception rate intercept of breeder PW")]
        [Required]
        public double ConceptionRateIntercept { get; set; }

        /// <summary>
        /// Conception rate assymtote of breeder
        /// </summary>
        [Description("Conception rate assymtote")]
        [Required]
        public double ConceptionRateAsymptote { get; set; }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "<div class=\"activityentry\">";
            html += "Conception rates are being calculated for all females using the same curve.";
            html += "</div>";
            return html;
        }

        /// <summary>
        /// Calculate conception rate for a female
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns></returns>
        public double ConceptionRate(RuminantFemale female)
        {
            double rate = 0;
            if (female.StandardReferenceWeight > 0)
            {
                rate = ConceptionRateAsymptote / (1 + Math.Exp(ConceptionRateCoefficent * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept));
            }
            rate = Math.Max(0,Math.Min(rate, 100));
            return rate / 100;
        }
    }
}
