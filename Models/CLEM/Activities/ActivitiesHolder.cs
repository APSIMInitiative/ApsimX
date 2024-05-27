using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Manager for all activities available to the model
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
        private ActivityFolder timeStep = new() { Name = "TimeStep", Status = ActivityStatus.NoTask };
        private int nextUniqueID = 1;

        /// <summary>
        /// Last resource request that was in defecit
        /// </summary>
        [JsonIgnore]
        public ResourceRequest LastShortfallResourceRequest { get; set; }

        /// <summary>
        /// Resource shortfall occurred event handler
        /// </summary>
        public event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Resource shortfall occurred event handler
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Method to raise the Shortfall occurred event handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Holds the event arguments for the activity performed event
        /// </summary>
        [JsonIgnore]
        public ActivityPerformedEventArgs LastActivityPerformed { get; set; }

        /// <summary>
        /// Shortfall occurred Event Handler
        /// </summary>
        /// <param name="e">Default event args object for this event.</param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Create a GuID object based on next unique ID available.
        /// </summary>
        /// <returns>CLEM formated GuID object.</returns>
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
        /// Provide the next GUID based on level specified when using GUID for identifying nested activities.
        /// </summary>
        /// <param name="guid">GuID object which needs one level incremented by 1.</param>
        /// <param name="level">Zero bound level index to be incremented (permits 1 to 3).</param>
        /// <returns>New GuID object with updated level.</returns>
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
                throw new ArgumentException("Add to GuID only supports levels 1 to 3");
        }


        /// <summary>An method to perform core actions when simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void SetUniqueActivityIDs(object sender, EventArgs e)
        {
            foreach (var activity in FindAllDescendants<CLEMModel>())
                activity.UniqueID = NextGuID;
        }

        /// <summary>A method to allow all activities to perform actions at the end of the time step.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void ReportActivityStatusAtEndOfTimestep(object sender, EventArgs e)
        {
            ReportAllActivityStatus();
        }

        /// <summary>A method to allow all activities to perform actions during the last stage of initialisation.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("FinalInitialise")]
        private void ReportActivityStatusAfterInitialisation(object sender, EventArgs e)
        {
            ReportAllActivityStatus(true);
        }

        private void ReportAllActivityStatus(bool fromSetup = false)
        {
            // fire all activity performed triggers at end of time step
            foreach (CLEMActivityBase child in FindAllChildren<CLEMActivityBase>())
                child.ReportActivityStatus(0, fromSetup);

            // add timestep activity for reporting
            ActivityPerformedEventArgs ea = new()
            {
                Name = timeStep.Name,
                Status = timeStep.Status,
                Id = timeStep.UniqueID.ToString(),
                ModelType = (int)ActivityPerformedType.Timer,
            };
            LastActivityPerformed = ea;
            OnActivityPerformed(ea);
        }

        /// <summary>
        /// Report activity performed event
        /// </summary>
        /// <param name="e"></param>
        public void ReportActivityPerformed(ActivityPerformedEventArgs e)
        {
            LastActivityPerformed = e;
            OnActivityPerformed(e);
        }

        /// <summary>
        /// Report activity shortfall event
        /// </summary>
        /// <param name="e"></param>
        public void ReportActivityShortfall(ResourceRequestEventArgs e)
        {
            LastShortfallResourceRequest = e.Request;
            OnShortfallOccurred(e);
        }

        #region validation

        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection that holds failed-validation information.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // ensure all folders are not APSIM folders
            if (FindAllDescendants<Folder>().Any())
            {
                string[] memberNames = new string[] { "ActivityHolder" };
                results.Add(new ValidationResult("Only CLEMFolders should be used in the Activity holder. This type of folder provides functionality for working with Activities in CLEM. At least one APSIM Folder was used in the Activities section.", memberNames));
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return "\r\n<h1>Activities summary</h1>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return $"\r\n<div class=\"activity\"style=\"opacity: {SummaryOpacity(FormatForParentControl)}\">";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "\r\n</div>";
        } 
        #endregion
    }
}
