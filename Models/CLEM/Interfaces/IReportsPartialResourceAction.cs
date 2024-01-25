using System.ComponentModel;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for a class able to report on action performed when resources available are insufficient
    /// </summary>
    public interface IReportPartialResourceAction
    {
        /// <summary>
        /// Action to perform when Insufficient resources available action
        /// </summary>
        [Description("Insufficient resources available action")]
        [Models.Core.Display(Order = 1000)]
        public OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>
        /// Indicates if the activity supports partial resources actions
        /// </summary>
        public bool AllowsPartialResourcesAvailable { get; }

        /// <summary>
        /// Current status of this activity component
        /// </summary>
        public ActivityStatus Status { get; set; }

        /// <summary>
        /// Additional message relating to current status of this activity component
        /// </summary>
        public string StatusMessage { get; }
    }
}
