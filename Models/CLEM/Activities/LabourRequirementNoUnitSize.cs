using Models.Core;
using System;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Defines the labour required for an activity where unit size is provided by the parent activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [Description("Defines the labour required for an activity where unit size is defined by the parent activity")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirementNoUnitSize.htm")]
    public class LabourRequirementNoUnitSize: LabourRequirement
    {
        /// <summary>
        /// Size of unit
        /// </summary>
        [JsonIgnore]
        public new double UnitSize { get; set; }
    }
}
