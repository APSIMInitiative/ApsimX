using Models.CLEM.Reporting;
using Models.CLEM.Resources;
using Models.CLEM.Extras;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Parent component used to manage all activities available to CLEM
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(Market))]
    [Description("This holds all activities used in the CLEM simulation")]
    [HelpUri(@"Content/Features/Activities/ActivitiesHolder.htm")]
    [Version(1, 0, 1, "")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(singleInstance: true)]
    public class ActivitiesHolder: CLEMModel, IValidatableObject
    {
        private readonly ActivityFolder timeStep = new() { Name = "TimeStep", Status = ActivityStatus.NoTask };
        private int nextUniqueID = 1;
        private bool foundReportActivitiesPerformed = false;

        /// <summary>
        /// The last resource request that was in deficit
        /// </summary>
        [JsonIgnore]
        public ResourceRequest LastShortfallResourceRequest { get; set; }

        /// <summary>
        /// Raised when a resource shortfall is detected
        /// </summary>
        public event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Raised when the activity is performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Method to raise the resource shortfall occurred event
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Arguments used by the previous activity performed event call
        /// </summary>
        [JsonIgnore]
        public ActivityPerformedEventArgs LastActivityPerformed { get; set; }

        /// <summary>
        /// Method to raise the activity performed event
        /// </summary>
        /// <param name="e">Default event args object for this event.</param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// The <c>NextGuID</c> property determines the next available GuID.
        /// </summary>
        /// <returns>CLEM formatted GuID object tracking nested activities</returns>
        public Guid NextGuID
        {
            get
            {
                int current = nextUniqueID;
                nextUniqueID++;
                return Guid.Parse($"{current.ToString().PadLeft(8, '0')}-0000-0000-0000-000000000000");
            }
        }

        /// <summary>
        /// The <c>AddToGuid</c> method increments a CLEM GuID by 1 for a specified level
        /// </summary>
        /// <param name="guid">GuID object to be incremented by 1</param>
        /// <param name="level">Zero bound level index to be incremented (permits 1 to 3)</param>
        /// <returns>The resulting GuID object with updated level</returns>
        public static Guid AddToGuID(Guid guid, int level)
        {
            if (level > 0 & level <= 3)
            {
                string[] parts = guid.ToString().Split('-');
                int number = Convert.ToInt32(parts[level]) + 1;
                parts[level] = number.ToString().PadLeft(4, '0');
                return Guid.Parse(string.Join('-', parts));
            }
            else
            {
                throw new ArgumentException("Add to GuID only supports levels 1 to 3");
            }
        }


        /// <summary>
        /// An event handler to assign CLEM GuID and reset status of all activities
        /// </summary>
        /// <remarks>
        /// This method is called when the APSIM simulation is Commencing.
        /// </remarks>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void SetUniqueActivityIDs(object sender, EventArgs e)
        {
            foundReportActivitiesPerformed = base.Structure.FindAll<ReportActivitiesPerformed>().Any();

            foreach (var activity in Structure.FindChildren<CLEMActivityBase>(recurse: true))
            {
                activity.UniqueID = NextGuID;
                activity.Status = ActivityStatus.Ignored;
            }
        }

        /// <summary>
        /// An event handler to call the <c>ReportAllActivityStatus</c> method at the end of a CLEM time step
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void ReportActivityStatusAtEndOfTimestep(object sender, EventArgs e)
        {
            if (foundReportActivitiesPerformed)
            {
                ReportAllActivityStatus();
            }
        }

        /// <summary>
        /// An event handler to call the <c>ReportAllActivityStatus</c> method after CLEM has completed its initialisation
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("CLEMValidate")]
        private void ReportActivityStatusAfterInitialisation(object sender, EventArgs e)
        {
            // call after FinalInitialise during CLEMEvents.CLEMValidate
            if (foundReportActivitiesPerformed)
            {
                ReportAllActivityStatus(true);
            }
        }

        /// <summary>
        /// Method to manage reporting of activity status and details for ActivityPerformed event
        /// </summary>
        /// <param name="fromSetup">A flag to indicate that this is being called from the setup phase</param>
        private void ReportAllActivityStatus(bool fromSetup = false)
        {
            // fire all activity performed triggers when needeed
            foreach (CLEMActivityBase child in Structure.FindChildren<CLEMActivityBase>())
            {
                child.ReportActivityStatus(0, fromSetup);
            }

            // add time step activity for reporting
            ActivityPerformedEventArgs ea = new()
            {
                Name = timeStep.Name,
                Status = timeStep.Status,
                Id = timeStep.UniqueID.ToString(),
                ModelType = (int)ActivityPerformedType.Timer,
            };
            LastActivityPerformed = ea;
            if (fromSetup || ea.Status != ActivityStatus.NoTask)
            {
                OnActivityPerformed(ea);
            }
        }

        /// <summary>
        /// Report activity performed event
        /// </summary>
        /// <param name="e">The <see cref="ActivityPerformedEventArgs"/> instance containing the event data</param>
        public void ReportActivityPerformed(ActivityPerformedEventArgs e)
        {
            LastActivityPerformed = e;
            OnActivityPerformed(e);
        }

        /// <summary>
        /// Report activity shortfall event
        /// </summary>
        /// <param name="e">The <see cref="ResourceRequestEventArgs"/> instance containing the event data</param>
        public void ReportActivityShortfall(ResourceRequestEventArgs e)
        {
            LastShortfallResourceRequest = e.Request;
            OnShortfallOccurred(e);
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ensure all folders are not APSIM folders
            if (Structure.FindChildren<Folder>(recurse: true).Any())
            {
                yield return new ValidationResult("Only CLEMFolders should be used in the Activity holder. This type of folder provides functionality for working with Activities in CLEM. At least one APSIM Folder was used in the Activities section.", ["ActivityHolder"]);
            }
        }

        #endregion
    }
}
