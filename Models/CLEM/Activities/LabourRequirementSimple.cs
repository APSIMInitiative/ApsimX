using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Defines the labour required for an Activity where details are provided by the Activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines the labour required for an Activity where details are provided by the Activity")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirementSimple.htm")]
    public class LabourRequirementSimple: LabourRequirement
    {
        /// <summary>
        /// Size of unit
        /// </summary>
        [JsonIgnore]
        public new double UnitSize { get; set; }

        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [JsonIgnore]
        public new double LabourPerUnit { get; set; }

        /// <summary>
        /// Labour unit type
        /// </summary>
        [JsonIgnore]
        public new LabourUnitType UnitType { get; set; }

    }
}
