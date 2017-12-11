using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Advanced ruminant conception for first conception less than 12 months, 12-24 months, 2nd calf and 3+ calf
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Advanced ruminant conception for first conception less than 12 months, 12-24 months, 2nd calf and 3+ calf")]
    public class RuminantConceptionAdvanced: CLEMModel
    {
        /// <summary>
        /// Conception rate coefficient of breeder
        /// </summary>
        [Description("Conception rate coefficient of breeder PW (<12 mnth, 12-24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateCoefficent { get; set; }
        /// <summary>
        /// Conception rate intercept of breeder
        /// </summary>
        [Description("Conception rate intercept of breeder PW (<12 mnth, 12-24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateIntercept { get; set; }
        /// <summary>
        /// Conception rate assymtote of breeder
        /// </summary>
        [Description("Conception rate assymtote (<12 mnth, 12-24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateAsymptote { get; set; }
    }
}
