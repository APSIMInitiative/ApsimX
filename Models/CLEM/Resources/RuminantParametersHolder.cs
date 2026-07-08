using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Holds all ruminant parameters sub-models
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Manages all ruminant parameters")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersHolder: CLEMModel
    {
        /// <summary>
        /// Switch to determine if summary ruminant parameters are displayed in the descriptive summary
        /// </summary>
        [Description("Display summary parameters in descriptive summary")]
        [Category("Summary", "General")]
        public bool DisplaySummaryParameters { get; set; } = true;

    }
}
