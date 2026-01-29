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
    /// <summary>
    /// An individual labour availability item with the same days available every month
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourAvailabilityList))]
    [Description("Set the labour availability of specified individuals with the same days available every month")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailabilityItem.htm")]
    public class LabourAvailabilityGroup : FilterGroup<LabourType>, ILabourSpecificationItem
    {
        /// <summary>
        /// Single values
        /// </summary>
        [System.ComponentModel.DefaultValue(20)]
        [Description("Availability")]
        [Required, GreaterThanValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Provide the labour availability
        /// </summary>
        /// <param name="month">Month for labour</param>
        /// <returns></returns>
        public double GetAvailability(int month)
        {
            return Value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }
    }
}
