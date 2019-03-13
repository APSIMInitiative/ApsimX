using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

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
    public class RuminantConceptionAdvanced: CLEMModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionAdvanced()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Conception rate coefficient of breeder
        /// </summary>
        [Description("Conception rate coefficient of breeder PW (<12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateCoefficent { get; set; }
        /// <summary>
        /// Conception rate intercept of breeder
        /// </summary>
        [Description("Conception rate intercept of breeder PW (<12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateIntercept { get; set; }
        /// <summary>
        /// Conception rate assymtote of breeder
        /// </summary>
        [Description("Conception rate assymtote (<12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateAsymptote { get; set; }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "<div class=\"activityentry\">";
            html += "Conception rates are being calculated for first pregnancy before 12 months, between 12-24 months and after 24 months as well as 2nd calf and 3rd or later calf.";
            html += "</div>";
            return html;
        }

    }
}
