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
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(Market))]
    [Description("This holds all activities used in the CLEM simulation")]
    [HelpUri(@"Content/Features/Activities/ActivitiesHolder.htm")]
    [Version(1, 0, 1, "")]
    public class ActivitiesHolder: CLEMModel, IValidatableObject
    {
        private ActivityFolder timeStep = new ActivityFolder() { Name = "TimeStep", Status= ActivityStatus.NoTask };

        /// <summary>
        /// List of the all the Activities.
        /// </summary>
        [JsonIgnore]
        private List<IModel> activities;

        private void BindEvents(List<IModel> root)
        {
            foreach (var item in root.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))))
            {
                if (item.GetType() != typeof(ActivityFolder))
                {
                    (item as CLEMActivityBase).ResourceShortfallOccurred += ActivitiesHolder_ResourceShortfallOccurred;
                    (item as CLEMActivityBase).ActivityPerformed += ActivitiesHolder_ActivityPerformed;
                }
                BindEvents(item.Children.Cast<IModel>().ToList());
            }
            // add link to all timers as children so they can fire activity performed
            foreach (var timer in root.Where(a => typeof(IActivityPerformedNotifier).IsAssignableFrom(a.GetType())))
            {
                (timer as IActivityPerformedNotifier).ActivityPerformed += ActivitiesHolder_ActivityPerformed;
            }
        }

        private void UnBindEvents(List<IModel> root)
        {
            if (root != null)
            {
                foreach (var item in root.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))))
                {
                    if (item.GetType() != typeof(ActivityFolder))
                    {
                        (item as CLEMActivityBase).ResourceShortfallOccurred -= ActivitiesHolder_ResourceShortfallOccurred;
                        (item as CLEMActivityBase).ActivityPerformed -= ActivitiesHolder_ActivityPerformed;
                    }
                    UnBindEvents(item.Children.Cast<IModel>().ToList());
                }
                // remove link to all timers as children
                foreach (var timer in root.Where(a => typeof(IActivityPerformedNotifier).IsAssignableFrom(a.GetType())))
                {
                    (timer as IActivityPerformedNotifier).ActivityPerformed -= ActivitiesHolder_ActivityPerformed;
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

        /// <summary>
        /// Function to return an activity from the list of available activities.
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private IModel SearchForNameInActivity(IModel activity, string name)
        {
            IModel found = activity.Children.Find(x => x.Name == name);
            if (found != null)
            {
                return found;
            }

            foreach (var child in activity.Children)
            {
                found = SearchForNameInActivity(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Function to return an activity from the list of available activities.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IModel SearchForNameInActivities(string name)
        {
            IModel found = Children.Find(x => x.Name == name);
            if (found != null)
            {
                return found;
            }

            foreach (var child in Children)
            {
                found = SearchForNameInActivity(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            activities = FindAllChildren<IModel>().ToList(); // = Children;
            BindEvents(activities);
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            UnBindEvents(activities);
        }

        /// <summary>An event handler to allow to call all Activities in tree to request their resources in order.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMGetResourcesRequired")]
        private void OnGetResourcesRequired(object sender, EventArgs e)
        {
            foreach (CLEMActivityBase child in Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))))
            {
                child.GetResourcesForAllActivities(this);
            }
        }

        /// <summary>An event handler to allow to call all Activities in tree to request their resources in order.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            foreach (CLEMActivityBase child in Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))))
            {
                child.GetResourcesForAllActivityInitialisation();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void OnCLEMEndOfTimeStep(object sender, EventArgs e)
        {
            // fire all activity performed triggers at end of time step
            foreach (CLEMActivityBase child in Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))))
            {
                if (child.Enabled)
                {
                    child.ReportAllAllActivitiesPerformed();
                }
            }

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
            // fire all activity performed triggers at end of time step
            foreach (CLEMActivityBase child in Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))))
            {
                if (child.Enabled)
                {
                    child.ClearAllAllActivitiesPerformedStatus();
                }
            }
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            return "\r\n<h1>Activities summary</h1>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "\r\n<div class=\"activity\"style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        } 
        #endregion
    }
}
