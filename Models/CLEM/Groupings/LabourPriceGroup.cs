using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual labour in a set price group
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourPricing))]
    [Description("Set the pay rate for the selected group of individuals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/LabourPriceGroup.htm")]
    public class LabourPriceGroup : FilterGroup<LabourType>
    {
        /// <summary>
        /// Pay rate
        /// </summary>
        [Description("Daily pay rate")]
        [Required, GreaterThanEqualValue(0)]
        public double Value { get; set; }

    }
}
