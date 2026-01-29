using Models.Core;
using Models.Core.Attributes;
using System;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Holds a list of labour availability items
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyMultiModelView")]
    [PresenterName("UserInterface.Presenters.PropertyMultiModelPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("This represents a list of labour availability settings")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailability.htm")]
    public class LabourAvailabilityList : LabourSpecifications
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityList()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }
    }
}
