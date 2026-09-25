using System;
using Models.Core;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>A folder to manage activity grouping</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity folder helps arrange activities and apply timers to the group")]
    [HelpUri(@"Content/Features/Activities/ActivitiesFolder.htm")]
    [Version(1, 0, 1, "")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class ActivityFolder : CLEMActivityBase
    {
        /// <inheritdoc/>
        [JsonIgnore]
        public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NoTask;
        }
    }
}
