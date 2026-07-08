using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individuals able to undertake labour
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourRequirement))]
    [ValidParent(ParentType = typeof(LabourRequirementNoUnitSize))]
    [ValidParent(ParentType = typeof(LabourGroup))]
    [ValidParent(ParentType = typeof(TransmuteLabour))]
    [ValidParent(ParentType = typeof(LabourActivityTask))]
    [Description("Defines specific individuals from the labour pool to undertake labour")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/LabourFilterGroup.htm")]
    public class LabourGroup : FilterGroup<LabourType>
    {
    }
}
