using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private void BindEvents(IEnumerable<IModel> root)
        {
            foreach (var item in root.OfType<CLEMActivityBase>())
            {
                if (item.GetType() != typeof(ActivityFolder))
                {
                    (item as CLEMActivityBase).ResourceShortfallOccurred += ActivitiesHolder_ResourceShortfallOccurred;
                    (item as CLEMActivityBase).ActivityPerformed += ActivitiesHolder_ActivityPerformed;
                }
                BindEvents(item.FindAllChildren<IModel>());
            }
            // add link to all timers as children so they can fire activity performed
            foreach (var timer in root.OfType<IActivityPerformedNotifier>())
            {
                timer.ActivityPerformed += ActivitiesHolder_ActivityPerformed;
            }
        }

        private void UnBindEvents(IEnumerable<IModel> root)
        {
            if (root.Any())
            {
                foreach (var item in root.OfType<CLEMActivityBase>())
                {
                    if (item.GetType() != typeof(ActivityFolder))
                    {
                        (item as CLEMActivityBase).ResourceShortfallOccurred -= ActivitiesHolder_ResourceShortfallOccurred;
                        (item as CLEMActivityBase).ActivityPerformed -= ActivitiesHolder_ActivityPerformed;
                    }
                    UnBindEvents(item.FindAllChildren<IModel>());
                }
                // remove link to all timers as children
                foreach (var timer in root.OfType<IActivityPerformedNotifier>())
                {
                    timer.ActivityPerformed -= ActivitiesHolder_ActivityPerformed;
                }
            }
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
                results.Add(new ValidationResult("Only CLEMFolders shoud be used in the Activity holder. This type of folder provides functionality for working with Activities in CLEM. At least one APSIM Folder was used in the Activities section.", memberNames));
            }
            return results;
        } 
        #endregion

        /// <summary>
        /// Last resource request that was in defecit
        /// </summary>
        public ResourceRequest LastShortfallResourceRequest { get; set; }

        /// <summary>
        /// Hander for shortfall
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ActivitiesHolder_ResourceShortfallOccurred(object sender, EventArgs e)
        {
            // save resource request
            LastShortfallResourceRequest = (e as ResourceRequestEventArgs).Request;
            // call resourceShortfallEventhandler
            OnShortfallOccurred(e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public event EventHandler ResourceShortfallOccurred;

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
        public CLEMActivityBase LastActivityPerformed { get; set; }
        
        private void ActivitiesHolder_ActivityPerformed(object sender, EventArgs e)
        {
            // save 
            LastActivityPerformed = (e as ActivityPerformedEventArgs).Activity;
            // call ActivityPerformedEventhandler
            OnActivityPerformed(e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            BindEvents(FindAllChildren<IModel>());
            int index = 0;
            foreach (var activity in FindAllDescendants<CLEMActivityBase>())
            {
                activity.SetGuID($"{index.ToString().PadLeft(8,'0')}-0000-0000-0000-000000000000");
                index++;
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            UnBindEvents(FindAllChildren<IModel>());
        }

        /// <summary>An event handler to allow to call all Activities in tree to request their resources in order.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMGetResourcesRequired")]
        private void OnGetResourcesRequired(object sender, EventArgs e)
        {
            foreach (CLEMActivityBase child in FindAllChildren<CLEMActivityBase>())
                child.GetResourcesForAllActivities(this);
        }

        /// <summary>An event handler to allow to call all Activities in tree to request their resources in order.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            foreach (CLEMActivityBase child in FindAllChildren<CLEMActivityBase>())
                child.GetResourcesForAllActivityInitialisation();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void OnCLEMEndOfTimeStep(object sender, EventArgs e)
        {
            // fire all activity performed triggers at end of time step
            foreach (CLEMActivityBase child in FindAllChildren<CLEMActivityBase>())
                child.ReportAllAllActivitiesPerformed();

            // report all timers that were due this time step
            foreach (IActivityTimer timer in this.FindAllDescendants<IActivityTimer>())
            {
                if (timer.ActivityDue)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs timerActivity = new ActivityPerformedEventArgs
                    {
                        Activity = new BlankActivity()
                        {
                            Status = ActivityStatus.Timer,
                            Name = (timer as IModel).Name
                        }
                    };
                    timerActivity.Activity.SetGuID((timer as CLEMModel).UniqueID);
                    timer.OnActivityPerformed(timerActivity);
                }
            }

            // add timestep activity for reporting
            ActivityPerformedEventArgs ea = new ActivityPerformedEventArgs()
            {
                Activity = timeStep
            };
            LastActivityPerformed = timeStep;
            OnActivityPerformed(ea);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // clear the activity performed status at start of time step
            foreach (CLEMActivityBase child in FindAllChildren<CLEMActivityBase>())
                child.ClearAllAllActivitiesPerformedStatus();
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            return "\r\n<h1>Activities summary</h1>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "\r\n<div class=\"activity\"style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        } 
        #endregion
    }
}
