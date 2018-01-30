using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// CLEM Activity base model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Activity Base Class and should not be used directly.")]
    public abstract class CLEMActivityBase: CLEMModel
    {
        /// <summary>
        /// 
        /// </summary>
        [Link]
        public ResourcesHolder Resources = null;
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Current list of resources requested by this activity
        /// </summary>
        [XmlIgnore]
        public List<ResourceRequest> ResourceRequestList { get; set; }

        /// <summary>
        /// Current list of activities under this activity
        /// </summary>
        [XmlIgnore]
        public List<CLEMActivityBase> ActivityList { get; set; }

        /// <summary>
        /// Current status of this activity
        /// </summary>
        [XmlIgnore]
        public ActivityStatus Status { get; set; }

        /// <summary>
        /// Property to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree
        /// </summary>
        /// <returns>T/F</returns>
        public bool TimingOK
        {
            get
            {
                int result = 0;
                IModel current = this as IModel;
                while (current.GetType() != typeof(ZoneCLEM))
                {
                    result += current.Children.Where(a => a is IActivityTimer).Cast<IActivityTimer>().Sum(a => a.ActivityDue ? 0 : 1);
                    current = current.Parent as IModel;
                }
                return (result == 0);

                // sum all where true=0 and false=1 so that all must be zero to get a sum total of zero or there are no timers
                // return this.Children.Where(a => a is IActivityTimer).Cast<IActivityTimer>().Sum(a => a.ActivityDue() ? 0 : 1) == 0;
            }
        }

        /// <summary>
        /// Property to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree
        /// </summary>
        /// <returns>T/F</returns>
        public void SetStatusSuccess()
        {
            if(Status== ActivityStatus.NotNeeded)
            {
                Status = ActivityStatus.Success;
            }
        }

        /// <summary>
        /// Method to cascade calls for calling activites performed for all activities in the UI tree. 
        /// </summary>
        public virtual void ClearAllAllActivitiesPerformedStatus()
        {
            ClearActivitiesPerformedStatus();
        }

        /// <summary>
        /// Protected method to cascade calls for activities performed for all activities in the UI tree. 
        /// </summary>
        protected void ClearActivitiesPerformedStatus()
        {
            // clear status of all dynamically created CLEMActivityBase activities
            if (ActivityList != null)
            {
                foreach (CLEMActivityBase activity in ActivityList)
                {
                    activity.Status = ActivityStatus.NotNeeded;
                    activity.ClearAllAllActivitiesPerformedStatus();
                }
            }
            // clear status for all children of type CLEMActivityBase
            foreach (CLEMActivityBase activity in this.Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))).ToList())
            {
                activity.Status = ActivityStatus.NotNeeded;
                activity.ClearAllAllActivitiesPerformedStatus();
            }
        }

        /// <summary>
        /// Method to cascade calls for calling activites performed for all activities in the UI tree. 
        /// </summary>
        public virtual void ReportAllAllActivitiesPerformed()
        {
            ReportActivitiesPerformed();
        }

        /// <summary>
        /// Protected method to cascade calls for activities performed for all activities in the UI tree. 
        /// </summary>
        protected void ReportActivitiesPerformed()
        {
            // call activity performed for all dynamically created CLEMActivityBase activities
            if (ActivityList != null)
            {
                foreach (CLEMActivityBase activity in ActivityList)
                {
                    activity.TriggerOnActivityPerformed();
                    activity.ReportAllAllActivitiesPerformed();
                }
            }
            // call acticity performed  for all children of type CLEMActivityBase
            foreach (CLEMActivityBase activity in this.Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))).ToList())
            {
                activity.TriggerOnActivityPerformed();
                activity.ReportAllAllActivitiesPerformed();
            }
        }

        /// <summary>
        /// Method to cascade calls for resources for all activities in the UI tree. 
        /// Responds to CLEMInitialiseActivity in the Activity model holing top level list of activities
        /// </summary>
        public virtual void GetResourcesForAllActivityInitialisation()
        {
            ResourcesForAllActivityInitialisation();
        }

        /// <summary>
        /// Protected method to cascade calls for resources for all activities in the UI tree. 
        /// </summary>
        protected void ResourcesForAllActivityInitialisation()
        {
            // Get resources needed and use substitution if needed and provided, then move through children getting their resources.
            GetResourcesRequiredForInitialisation();

            // get resources required for all dynamically created CLEMActivityBase activities
            if (ActivityList != null)
            {
                foreach (CLEMActivityBase activity in ActivityList)
                {
                    activity.GetResourcesForAllActivityInitialisation();
                }
            }
            // get resources required for all children of type CLEMActivityBase
            foreach (CLEMActivityBase activity in this.Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))).ToList())
            {
                activity.GetResourcesForAllActivityInitialisation();
            }
        }

        /// <summary>
        /// Method to cascade calls for resources for all activities in the UI tree. 
        /// Responds to CLEMGetResourcesRequired in the Activity model holing top level list of activities
        /// </summary>
        public virtual void GetResourcesForAllActivities()
        {
            if (this.TimingOK)
            {
                ResourcesForAllActivities();
            }
        }

        /// <summary>
        /// protected method to cascade calls for resources for all activities in the UI tree. 
        /// </summary>
        protected void ResourcesForAllActivities()
        {
            // Get resources needed and use substitution if needed and provided, then move through children getting their resources.
            GetResourcesRequiredForActivity();

            // get resources required for all dynamically created CLEMActivityBase activities
            if (ActivityList != null)
            {
                foreach (CLEMActivityBase activity in ActivityList)
                {
                    activity.GetResourcesForAllActivities();
                }
            }
            // get resources required for all children of type CLEMActivityBase
            foreach (CLEMActivityBase activity in this.Children.Where(a => a.GetType().IsSubclassOf(typeof(CLEMActivityBase))).ToList())
            {
                activity.GetResourcesForAllActivities();
            }
        }

        /// <summary>
        /// Method to get required resources for initialisation of this activity. 
        /// </summary>
        public virtual void GetResourcesRequiredForInitialisation()
        {
            ResourcesRequiredForInitialisation();
        }

        /// <summary>
        /// Protected method to get required resources for initialisation of this activity. 
        /// </summary>
        protected void ResourcesRequiredForInitialisation()
        {
            // determine what resources are needed for initialisation
            ResourceRequestList = GetResourcesNeededForinitialisation();

            bool tookRequestedResources = TakeResources(ResourceRequestList, false);

            // no resources required perform Activity if code is present.
            // if resources are returned (all available or UseResourcesAvailable action) perform Activity
            // if reportErrorAndStop do not perform Activity
            // if SkipActivity still attempt to perform initialisation but no resources will show in Supplied fields of ResourceRequestItems
            //if (tookRequestedResources || (ResourceRequestList == null) || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.SkipActivity)
            //{
            //    DoInitialisation();
            //}
            ResourceRequestList = null;
        }

        /// <summary>
        /// Method to get this time steps current required resources for this activity. 
        /// </summary>
        public virtual void GetResourcesRequiredForActivity()
        {
            ResourcesRequiredForActivity();
        }

        /// <summary>
        /// Protected method to get this time steps current required resources for this activity. 
        /// </summary>
        protected void ResourcesRequiredForActivity()
        {
            // determine what resources are needed
            ResourceRequestList = GetResourcesNeededForActivity();

            bool tookRequestedResources = TakeResources(ResourceRequestList, false);

            // if no resources required perform Activity if code is present.
            // if resources are returned (all available or UseResourcesAvailable action) perform Activity
            // if reportErrorAndStop or SkipActivity do not perform Activity
            if (tookRequestedResources || (ResourceRequestList == null))
            {
                DoActivity();
            }
        }

        /// <summary>
        /// Try to take the Resources based on Resource Request List provided.
        /// Returns true if it was able to take the resources it needed.
        /// Returns false if it was unable to take the resources it needed.
        /// </summary>
        /// <param name="ResourceRequestList"></param>
        /// <param name="TriggerActivityPerformed"></param>
        public bool TakeResources(List<ResourceRequest> ResourceRequestList, bool TriggerActivityPerformed)
        {
            bool resourceAvailable = false;

            // no resources required or this is an Activity folder.
            if ((ResourceRequestList == null)||(ResourceRequestList.Count() ==0)) return false;

            Guid uniqueRequestID = Guid.NewGuid();
            // check resource amounts available
            foreach (ResourceRequest request in ResourceRequestList)
            {
                request.ActivityID = uniqueRequestID;
                request.Available = 0;
                // get resource
                if (request.Resource == null)
                {
                    //If it hasn't been assigned try and find it now.
                    request.Resource = Resources.GetResourceItem(request, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as IResourceType;
                }
                if (request.Resource != null)
                {
                    // get amount available
                    request.Available = Math.Min(request.Resource.Amount, request.Required);
                }
                else
                {
                    if (!resourceAvailable)
                    {
                        // if resource does not exist in simulation assume unlimited resource available
                        // otherwise 0 will be assigned to available when no resouces match request
                        request.Available = request.Required;
                    }
                }
            }

            // are all resources available
            List<ResourceRequest> shortfallRequests = ResourceRequestList.Where(a => a.Required > a.Available).ToList();
            int countShortfallRequests = shortfallRequests.Count();
            if (countShortfallRequests > 0)
            {
                // check what transmutations can occur
                Resources.TransmutateShortfall(shortfallRequests, true);
            }

            // check if need to do transmutations
            int countTransmutationsSuccessful = shortfallRequests.Where(a => a.TransmutationPossible == true & a.AllowTransmutation).Count();
            bool allTransmutationsSuccessful = (shortfallRequests.Where(a => a.TransmutationPossible == false & a.AllowTransmutation).Count() == 0);

            // OR at least one transmutation successful and PerformWithPartialResources
            if (((countShortfallRequests > 0) & (countShortfallRequests == countTransmutationsSuccessful)) || (countTransmutationsSuccessful > 0 & OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable))
            {
                // do transmutations.
                Resources.TransmutateShortfall(shortfallRequests, false);

                // recheck resource amounts now that resources have been topped up
                foreach (ResourceRequest request in ResourceRequestList)
                {
                    // get resource
                    request.Available = 0;
                    if (request.Resource != null)
                    {
                        // get amount available
                        request.Available = Math.Min(request.Resource.Amount, request.Required);
                    }
                }
            }

            // report any resource defecits here
            foreach (var item in ResourceRequestList.Where(a => a.Required > a.Available))
            {
                ResourceRequestEventArgs rrEventArgs = new ResourceRequestEventArgs() { Request = item };
                OnShortfallOccurred(rrEventArgs);
                Status = ActivityStatus.Partial;
            }

            // remove activity resources 
            // check if deficit and performWithPartial
            if ((ResourceRequestList.Where(a => a.Required > a.Available).Count() == 0) || OnPartialResourcesAvailableAction != OnPartialResourcesAvailableActionTypes.SkipActivity)
            {
                if(OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    string resourcelist = "";
                    foreach (var item in ResourceRequestList.Where(a => a.Required > a.Available))
                    {
                        Summary.WriteWarning(this, String.Format("Insufficient ({0}) resource of type ({1}) for activity ({2})", item.ResourceType, item.ResourceTypeName, this.Name));
                        resourcelist += ((resourcelist.Length >0)?",":"")+item.ResourceType.Name;
                    }
                    if (resourcelist.Length > 0)
                    {
                        Summary.WriteWarning(this, String.Format("Ensure resources are available or change OnPartialResourcesAvailableAction setting for activity ({0}) to handle previous error", this.Name));
                        Status = ActivityStatus.Critical;
                        throw new Exception(String.Format("Insufficient resources ({0}) for activity ({1}) (see Summary for details)", resourcelist, this.Name));
                    }
                }

                foreach (ResourceRequest request in ResourceRequestList)
                {
                    // get resource
                    request.Provided = 0;
                    if (request.Resource != null)
                    {
                        // remove resource
                        request.Resource.Remove(request);
                    }
                }
                //return true; //could take all the resources it needed or is able to do with partial amounts
            }
            else
            {
                Status = ActivityStatus.Ignored;
                //return false;  //could not take all the resources it needed.
            }

            return Status != ActivityStatus.Ignored;
        }

        /// <summary>
        /// Method to trigger an Activity Performed event 
        /// </summary>
        public void TriggerOnActivityPerformed()
        {
            ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
            {
                Activity = this
            };
            this.OnActivityPerformed(activitye);
        }

        /// <summary>
        /// Method to trigger an Activity Performed event 
        /// </summary>
        public void TriggerOnActivityPerformed(ActivityStatus status)
        {
            this.Status = status;
            ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
            {
                Activity = new ActivityFolder() { Name = this.Name, Status = status }
            };
            this.OnActivityPerformed(activitye);
        }


        /// <summary>
        /// Insufficient resources available action
        /// </summary>
        [Description("Insufficient resources available action")]
        public OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>
        /// Abstract method to determine list of resources and amounts needed. 
        /// </summary>
        public abstract List<ResourceRequest> GetResourcesNeededForActivity();

        /// <summary>
        /// Abstract method to determine list of resources and amounts needed for initilaisation. 
        /// </summary>
        public abstract List<ResourceRequest> GetResourcesNeededForinitialisation();

        /// <summary>
        /// Method to perform activity tasks if expected as soon as resources are available
        /// </summary>
        public abstract void DoActivity();

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public virtual event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Activity performed event handler
        /// </summary>
        public virtual event EventHandler ActivityPerformed;

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }

    /// <summary>
    /// Status of activity
    /// </summary>
    public enum ActivityStatus
    {
        /// <summary>
        /// Performed with all resources available
        /// </summary>
        Success,
        /// <summary>
        /// Performed with partial resources available
        /// </summary>
        Partial,
        /// <summary>
        /// Insufficient resources so activity ignored
        /// </summary>
        Ignored,
        /// <summary>
        /// Insufficient resources so simulation stopped
        /// </summary>
        Critical,
        /// <summary>
        /// Indicates a timer occurred successfully
        /// </summary>
        Timer,
        /// <summary>
        /// Indicates a calculation event occurred
        /// </summary>
        Calculation,
        /// <summary>
        /// Indicated activity occurred but was not needed
        /// </summary>
        NotNeeded
    }

}
