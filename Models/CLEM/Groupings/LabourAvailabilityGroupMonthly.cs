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
    /// An individual labour availability item with monthly days available
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourAvailabilityList))]
    [Description("Set the labour availability of specified individuals with monthly days available")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailabilityItemMonthly.htm")]
    public class LabourAvailabilityGroupMonthly : FilterGroup<LabourType>, ILabourSpecificationItem
    {
        /// <summary>
        /// Monthly values.
        /// </summary>
        [System.ComponentModel.DefaultValue(new double[] { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 })]
        [Description("Availability each month of the year")]
        [Required, ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <summary>
        /// Provide the monthly labour availability
        /// </summary>
        /// <param name="month">Month for labour</param>
        /// <returns></returns>
        public double GetAvailability(int month)
        {
            if (month <= 12 && month > 0 && month <= MonthlyValues.Count())
            {
                return MonthlyValues[month - 1];
            }
            else
            {
                return 0;
            }
        }
    }
}
