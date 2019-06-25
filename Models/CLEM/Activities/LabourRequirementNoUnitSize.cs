using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Defines the labour required for an activity where unit size is provided by the parent activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [Description("Defines the labour required for an activity where unit size is provided by the parent activity")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirementNoUnitSize.htm")]
    public class LabourRequirementNoUnitSize: LabourRequirement
    {
        /// <summary>
        /// Size of unit
        /// </summary>
        [XmlIgnore]
        public new double UnitSize { get; set; }
    }
}
