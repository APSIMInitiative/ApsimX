using Models.Core;
using Models.CLEM.Interfaces;
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
    public class ActivitiesHolder: CLEMModel, IValidatableObject
    {
        private ActivityFolder timeStep = new ActivityFolder() { Name = "TimeStep", Status= ActivityStatus.NoTask };
        private int nextUniqueID = 1;

        /// <summary>
        /// Last resource request that was in defecit
        /// </summary>
        [JsonIgnore]
        public ResourceRequest LastShortfallResourceRequest { get; set; }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Details of the last activity performed
        /// </summary>
        [JsonIgnore]
        public ActivityPerformedEventArgs LastActivityPerformed { get; set; }

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Create a GuID string based on next unique ID
        /// </summary>
        /// <returns></returns>
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
        /// Provide next GUID based on level specified
        /// </summary>
        /// <param name="guid">GuID to add to</param>
        /// <param name="level">Level to add to</param>
        /// <returns>New GuID</returns>
        public static Guid AddToGuID(Guid guid, int level)
        {
            string guidString = guid.ToString();
            if (level > 0 & level <= 3)
            {
                List<string> parts = guidString.Split('-').ToList();
                int number = Convert.ToInt32(parts[level]);
                number++;
                parts[level] = number.ToString().PadLeft(4, '0');
                return Guid.Parse(string.Join('-', parts));
            }
            else
                throw new ArgumentException("Add to GuID only supports levels 1 to 3");
        }


        /// <summary>An method to perform core actions when simulation commences</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void SetUniqueActivityIDs(object sender, EventArgs e)
        {
            foreach (var activity in FindAllDescendants<CLEMModel>())
                activity.UniqueID = NextGuID;
        }

        /// <summary>A method to allow all activities to perform actions at the end of the time step</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void ReportActivityStatusAtEndOfTimestep(object sender, EventArgs e)
        {
            ReportAllActivityStatus();
        }

        /// <summary>A method to allow all activities to perform actions at the end of the time step</summary>
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
                child.ReportActivityStatus();

            // add timestep activity for reporting
            ActivityPerformedEventArgs ea = new ActivityPerformedEventArgs()
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
            // save 
            LastActivityPerformed = e;
            // call ActivityPerformedEventhandler
            OnActivityPerformed(e);
        }

        /// <summary>
        /// Report activity shortfall event
        /// </summary>
        /// <param name="e"></param>
        public void ReportActivityShortfall(ResourceRequestEventArgs e)
        {
            // save 
            LastShortfallResourceRequest = e.Request;
            // call ShortfallOccurredEventhandler
            OnShortfallOccurred(e);
        }

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
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
            return "\r\n<div class=\"activity\"style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "\r\n</div>";
        } 
        #endregion
    }
}
