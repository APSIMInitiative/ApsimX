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
    /// Defines the labour required for an Activity where details are provided by the Activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines the labour required for an Activity where details are provided by the Activity")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirementSimple.htm")]
    public class LabourRequirementSimple: LabourRequirement
    {
        /// <summary>
        /// Size of unit
        /// </summary>
        [XmlIgnore]
        public new double UnitSize { get; set; }

        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [XmlIgnore]
        public new double LabourPerUnit { get; set; }

        /// <summary>
        /// Labour unit type
        /// </summary>
        [XmlIgnore]
        public new LabourUnitType UnitType { get; set; }

    }
}
