using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// CLEM Activity base model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Activity Base Class and should not be used directly.")]
    [Version(1, 0, 1, "")]
    public abstract class CLEMActivityBase: CLEMModel
    {
        /// <summary>
        /// A protected link to the CLEM resource holder
        /// </summary>
        [Link]
        protected ResourcesHolder Resources = null;

        /// <summary>
        /// Link to Activity holder
        /// </summary>
        [Link]
        [NonSerialized]
        public ActivitiesHolder ActivitiesHolder = null;

        private bool enabled = true;
        private ZoneCLEM parentZone = null;

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Category for transactions required")]
        [Models.Core.Display(Order = 500)]
        virtual public string TransactionCategory { get; set; }

        /// <summary>
        /// Insufficient resources available action
        /// </summary>
        [Description("Insufficient resources available action")]
        [Models.Core.Display(Order = 1000)]
        public OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>
        /// Current list of resources requested by this activity
        /// </summary>
        [JsonIgnore]
        public List<ResourceRequest> ResourceRequestList { get; set; }

        /// <summary>
        /// Current status of this activity
        /// </summary>
        [JsonIgnore]
        public ActivityStatus Status { get; set; }

        /// <summary>
        /// Resource allocation style
        /// </summary>
        [JsonIgnore]
        public ResourceAllocationStyle AllocationStyle { get; set; }

        /// <summary>
        /// Current status of this activity
        /// </summary>
        [JsonIgnore]
        public bool ActivityEnabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if(value!=enabled)
                    foreach (var child in FindAllChildren<CLEMActivityBase>())
                        child.ActivityEnabled = value;
                    enabled = value;
            }
        }

        /// <summary>
        /// Multiplier for farms in this zone
        /// </summary>
        public double FarmMultiplier 
        {
            get
            {
                if(parentZone is null)
                    parentZone = FindAncestor<ZoneCLEM>();

                if(parentZone is null)
                    return 1;
                else
                    return parentZone.FarmMultiplier;
            }
        }

        /// <summary>
        /// Property to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree
        /// </summary>
        /// <returns>T/F</returns>
        public virtual new bool TimingOK
        {
            get
            {
                // use timing to not perform activity based on Enabled state
                if (!ActivityEnabled)
                    return false;

                // sum all where true=0 and false=1 so that all must be zero to get a sum total of zero or there are no timers
                int result = 0;
                IModel current = this as IModel;
                while (current.GetType() != typeof(ZoneCLEM) & current.GetType() != typeof(Market))
                {
                    if(current is CLEMModel)
                        result += (current as CLEMModel).ActivityTimers.Sum(a => a.ActivityDue ? 0 : 1);
                    current = current.Parent as IModel;
                }
                return (result == 0);
            }
        }

        /// <summary>
        /// Method to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree and a specified date
        /// </summary>
        /// <returns>T/F</returns>
        public bool TimingCheck(DateTime date)
        {
            // use timing to not perform activity based on Enabled state
            if (!ActivityEnabled)
                return false;

            // sum all where true=0 and false=1 so that all must be zero to get a sum total of zero or there are no timers
            int result = 0;
            IModel current = this as IModel;
            while (current.GetType() != typeof(ZoneCLEM))
            {
                if (current is CLEMModel)
                    result += (current as CLEMModel).ActivityTimers.Sum(a => a.Check(date) ? 0 : 1);
                current = current.Parent as IModel;
            }
            return (result == 0);
        }

        /// <summary>
        /// Property to check if timing of this activity is ok based on child and parent ActivityTimers in UI tree
        /// </summary>
        /// <returns>T/F</returns>
        public bool TimingExists
        {
            get
            {
                // sum all where true=0 and false=1 so that all must be zero to get a sum total of zero or there are no timers
                int result = 0;
                IModel current = this as IModel;
                while (current.GetType() != typeof(ZoneCLEM))
                {
                    if (current is CLEMModel)
                        result += (current as CLEMModel).ActivityTimers.Count();
                    current = current.Parent as IModel;
                }
                return (result != 0);
            }
        }

        /// <summary>
        /// A method to allow the resource holder to be set when [Link] not possible for dynamically created model
        /// </summary>
        /// <param name="resourceHolder">The resource holder to provide</param>
        public void SetLinkedModels(ResourcesHolder resourceHolder)
        {
            Resources = resourceHolder;
        }

        /// <summary>
        /// Sets the status of the activity to success if the activity was in fact needed this time step
        /// </summary>
        public void SetStatusSuccess()
        {
            if(Status== ActivityStatus.NotNeeded)
                Status = ActivityStatus.Success;
        }

        /// <summary>
        /// Get a list of model names given specified types as array
        /// </summary>
        /// <param name="typesToFind">the list of types to include</param>
        /// <returns>A list of model names</returns>
        public IEnumerable<string> GetNameOfModelsByType(Type[] typesToFind)
        {
            Simulation simulation = this.FindAncestor<Simulation>();
            if (simulation is null)
                return new List<string>().AsEnumerable();
            else
            {
                List<Type> types =  new List<Type>();
                return simulation.FindAllDescendants().Where(a => typesToFind.ToList().Contains(a.GetType())).Select(a => a.Name);
            }
        }

        /// <summary>An method to perform core actions when simulation commences</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        protected virtual void OnSimulationCommencing(object sender, EventArgs e)
        {
            Debug.WriteLine($"StartOfSimulation for {Name}");
        }

        /// <summary>A method to arrange clearing status on CLEMStartOfTimeStep event</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        protected virtual void ResetActivityStatus(object sender, EventArgs e)
        {
            // clear Resources Required list
            ResourceRequestList = new List<ResourceRequest>();

            Status = ActivityStatus.Ignored;
        }

        /// <summary>
        /// Protected method to cascade calls for activities performed for all dynamically created activities
        /// </summary>
        public void ReportActivityStatus()
        {
            this.TriggerOnActivityPerformed();

            // report all timers that were due this time step
            foreach (IActivityTimer timer in this.FindAllChildren<IActivityTimer>())
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
                    ActivitiesHolder?.ReportActivityPerformed(timerActivity);
                }
            }
            // call activity performed for all children of type CLEMActivityBase
            foreach (CLEMActivityBase activity in FindAllChildren<CLEMActivityBase>())
                activity.ReportActivityStatus();
        }

        /// <summary>A method to arrange the activity to be performed on the specified clock event</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMGetResourcesRequired")]
        protected virtual void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
            if (AllocationStyle != ResourceAllocationStyle.Manual)
            {
                ManageActivityResourcesAndTasks();
            }
        }

        /// <summary>
        /// The main method to manage an activity based on resources available 
        /// </summary>
        protected virtual void ManageActivityResourcesAndTasks()
        {
            if (Enabled)
            {
                if (TimingOK)
                {
                    // add any labour resources requirements based on method supplied by activity
                    ResourceRequestList.AddRange(GetLabourRequiredForActivity());

                    // add any non-labour resources needed based on method supplied by activity
                    var requests = DetermineResourcesForActivity();
                    if (requests != null)
                        ResourceRequestList.AddRange(requests);

                    // check availability
                    CheckResources(ResourceRequestList, Guid.NewGuid());

                    // adjust if needed based on method supplied by activity
                    AdjustResourcesForActivity();

                    // take resources
                    bool tookRequestedResources = TakeResources(ResourceRequestList, false);

                    // if no resources required perform Activity if code is present.
                    // if resources are returned (all available or UseResourcesAvailable action) perform Activity
                    if (tookRequestedResources || (ResourceRequestList.Count == 0))
                        PerformTasksForActivity(); //based on method supplied by activity

                }
                else
                {
                    this.Status = ActivityStatus.Ignored;
                }
            }
            else
            {
                Status = ActivityStatus.Ignored;
            }
        }

        /// <summary>
        /// Base method to determine the number of days labour required based on Activity requirements and labour settings.
        /// Functionality provided in derived classes
        /// </summary>
        protected virtual LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            return null;
        }

        /// <summary>
        /// Method to determine the list of resources and amounts needed. 
        /// Functionality provided in derived classes
        /// </summary>
        protected virtual List<ResourceRequest> DetermineResourcesForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method to adjust activities needed based on shortfalls before they are taken from resource pools. 
        /// Functionality provided in derived classes
        /// </summary>
        protected virtual void AdjustResourcesForActivity()
        {
            return;
        }

        /// <summary>
        /// Method to perform activity tasks if expected as soon as resources are available
        /// Functionality provided in derived classes
        /// </summary>
        protected virtual void PerformTasksForActivity()
        {
            return;
        }

        /// <summary>
        /// A common method to get the labour resource requests for the activity.
        /// </summary>
        /// <returns></returns>
        protected List<ResourceRequest> GetLabourRequiredForActivity()
        {
            List<ResourceRequest> labourResourceRequestList = new List<ResourceRequest>();
            foreach (LabourRequirement item in FindAllChildren<LabourRequirement>())
            {
                LabourRequiredArgs daysResult = GetDaysLabourRequired(item);
                if (daysResult?.DaysNeeded > 0)
                {
                    foreach (LabourFilterGroup fg in item.FindAllChildren<LabourFilterGroup>())
                    {
                        int numberOfPpl = 1;
                        if (item.ApplyToAll)
                            // how many matches
                            numberOfPpl = fg.Filter(Resources.FindResourceGroup<Labour>().Items).Count();
                        for (int i = 0; i < numberOfPpl; i++)
                        {
                            labourResourceRequestList.Add(new ResourceRequest()
                            {
                                AllowTransmutation = true,
                                Required = daysResult.DaysNeeded,
                                ResourceType = typeof(Labour),
                                ResourceTypeName = "",
                                ActivityModel = this,
                                FilterDetails = new List<object>() { fg },
                                Category = daysResult.Category,
                                RelatesToResource = daysResult.RelatesToResource
                            }
                            ); ;
                        }
                    }
                }
            }
            return labourResourceRequestList;
        }

        /// <summary>
        /// Method to provide the proportional limit based on labour shortfall
        /// A proportion less than 1 will only be returned if LabourShortfallAffectsActivity is true in the LabourRequirement
        /// </summary>
        /// <returns></returns>
        public double LabourLimitProportion
        {
            get
            {
                double proportion = 1.0;
                if (ResourceRequestList == null)
                    return proportion;

                double totalNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(LabourType)).Sum(a => a.Required);

                foreach (ResourceRequest item in ResourceRequestList.Where(a => a.ResourceType == typeof(LabourType)))
                    if (item.FilterDetails != null && ((item.FilterDetails.First() as LabourFilterGroup).Parent as LabourRequirement).LabourShortfallAffectsActivity)
                        proportion *= item.Provided / item.Required;
                return proportion;
            }
        }

        /// <summary>
        /// Method to provide the proportional limit based on specified resource type
        /// A proportion less than 1 will only be returned if LabourShortfallAffectsActivity is true in the LabourRequirement
        /// </summary>
        /// <returns></returns>
        public double LimitProportion(Type resourceType)
        {
            double proportion = 1.0;
            if (ResourceRequestList == null)
                return proportion;

            if (resourceType == typeof(LabourType))
                return LabourLimitProportion;

            double totalNeeded = ResourceRequestList.Where(a => a.ResourceType == resourceType).Sum(a => a.Required);

            foreach (ResourceRequest item in ResourceRequestList.Where(a => a.ResourceType == resourceType))
            {
                if (resourceType == typeof(LabourType))
                {
                    if (item.FilterDetails != null && ((item.FilterDetails.First() as LabourFilterGroup).Parent as LabourRequirement).LabourShortfallAffectsActivity)
                        proportion *= item.Provided / item.Required;
                }
                else // all other types
                    proportion *= item.Provided / item.Required;
            }
            return proportion;
        }

        /// <summary>
        /// Method to determine if activity limited based on labour shortfall has been set
        /// </summary>
        /// <returns></returns>
        public bool IsLabourLimitSet
        {
            get
            {
                foreach (LabourRequirement item in FindAllChildren<LabourRequirement>())
                    if (item.LabourShortfallAffectsActivity)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Method to determine available labour based on filters and take it if requested.
        /// </summary>
        /// <param name="request">Resource request details</param>
        /// <param name="removeFromResource">Determines if only calculating available labour or labour removed</param>
        /// <param name="callingModel">Model calling this method</param>
        /// <param name="resourceHolder">Location of resource holder</param>
        /// <param name="partialAction">Action on partial resources available</param>
        /// <returns></returns>
        public static double TakeLabour(ResourceRequest request, bool removeFromResource, IModel callingModel, ResourcesHolder resourceHolder, OnPartialResourcesAvailableActionTypes partialAction )
        {
            double amountProvided = 0;
            double amountNeeded = request.Required;
            LabourFilterGroup current = request.FilterDetails.OfType<LabourFilterGroup>().FirstOrDefault();

            LabourRequirement lr;
            if (current!=null)
            {
                if (current.Parent is LabourRequirement)
                    lr = current.Parent as LabourRequirement;
                else
                    // coming from Transmutation request
                    lr = new LabourRequirement()
                    {
                        LimitStyle = LabourLimitType.AsDaysRequired,
                        ApplyToAll = false,
                        MaximumPerGroup = 10000,
                        MaximumPerPerson = 1000,
                        MinimumPerPerson = 0
                    };
            }
            else
                lr = callingModel.FindAllChildren<LabourRequirement>().FirstOrDefault();

            lr.CalculateLimits(amountNeeded);
            amountNeeded = Math.Min(amountNeeded, lr.MaximumDaysPerGroup);
            request.Required = amountNeeded;
            // may need to reduce request here or shortfalls will be triggered

            int currentIndex = 0;
            if (current==null)
                // no filtergroup provided so assume any labour
                current = new LabourFilterGroup();

            request.ResourceTypeName = "Labour";
            ResourceRequest removeRequest = new ResourceRequest()
            {
                ActivityID = request.ActivityID,
                ActivityModel = request.ActivityModel,
                AdditionalDetails = request.AdditionalDetails,
                AllowTransmutation = request.AllowTransmutation,
                Available = request.Available,
                FilterDetails = request.FilterDetails,
                Provided = request.Provided,
                Category = request.Category,
                RelatesToResource = request.RelatesToResource,
                Required = request.Required,
                Resource = request.Resource,
                ResourceType = request.ResourceType,
                ResourceTypeName = (request.Resource is null? "":(request.Resource as CLEMModel).NameWithParent)
            };

            // start with top most LabourFilterGroup
            while (current != null && amountProvided < amountNeeded)
            {
                IEnumerable<LabourType> items = resourceHolder.FindResource<Labour>().Items;
                items = items.Where(a => (a.LastActivityRequestID != request.ActivityID) || (a.LastActivityRequestID == request.ActivityID && a.LastActivityRequestAmount < lr.MaximumDaysPerPerson));
                items = current.Filter(items);

                // search for people who can do whole task first
                while (amountProvided < amountNeeded && items.Where(a => a.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson) >= request.Required).Any())
                {
                    // get labour least available but with the amount needed
                    LabourType lt = items.Where(a => a.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson) >= request.Required).OrderBy(a => a.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson)).FirstOrDefault();

                    double amount = Math.Min(amountNeeded - amountProvided, lt.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson));

                    // limit to max allowed per person
                    amount = Math.Min(amount, lr.MaximumDaysPerPerson);
                    // limit to min per person to do activity
                    if (amount < lr.MinimumPerPerson)
                    {
                        request.Category = "Min labour limit";
                        return amountProvided;
                    }

                    amountProvided += amount;
                    removeRequest.Required = amount;
                    if (removeFromResource)
                    {
                        lt.LastActivityRequestID = request.ActivityID;
                        lt.LastActivityRequestAmount = amount;
                        lt.Remove(removeRequest);
                        request.Provided += removeRequest.Provided;
                        request.Value += request.Provided * lt.PayRate();
                    }
                }

                // if still needed and allow partial resource use.
                if (partialAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                {
                    if (amountProvided < amountNeeded)
                    {
                        // then search for those that meet criteria and can do part of task
                        foreach (LabourType item in items.Where(a => a.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson) >= 0).OrderByDescending(a => a.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson))) 
                        {
                            if (amountProvided >= amountNeeded)
                                break;

                            double amount = Math.Min(amountNeeded - amountProvided, item.LabourCurrentlyAvailableForActivity(request.ActivityID, lr.MaximumDaysPerPerson));

                            // limit to max allowed per person
                            amount = Math.Min(amount, lr.MaximumDaysPerPerson);

                            // limit to min per person to do activity
                            if (amount >= lr.MinimumDaysPerPerson)
                            {
                                amountProvided += amount;
                                removeRequest.Required = amount;
                                if (removeFromResource)
                                {
                                    if(item.LastActivityRequestID != request.ActivityID)
                                        item.LastActivityRequestAmount = 0;
                                    item.LastActivityRequestID = request.ActivityID;
                                    item.LastActivityRequestAmount += amount;
                                    item.Remove(removeRequest);
                                    request.Provided += removeRequest.Provided;
                                    request.Value += request.Provided * item.PayRate();
                                }
                            }
                            else
                                currentIndex = request.FilterDetails.Count;
                        }
                    }
                }
                currentIndex++;
                var currentFilterGroups = current.FindAllChildren<LabourFilterGroup>();
                if (currentFilterGroups.Any())
                    current = currentFilterGroups.FirstOrDefault();
                else
                    current = null;
            }
            // report amount gained.
            return amountProvided;
        }

        /// <summary>
        /// Method to determine available non-labour resources and take if requested.
        /// </summary>
        /// <param name="request">Resource request details</param>
        /// <param name="removeFromResource">Determines if only calculating available labour or labour removed</param>
        /// <returns></returns>
        private double TakeNonLabour(ResourceRequest request, bool removeFromResource)
        {
            // get available resource
            if (request.Resource == null)
                //If it hasn't been assigned try and find it now.
                request.Resource = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(request, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);

            if (request.Resource != null)
                // get amount available
                request.Available = Math.Min(request.Resource.Amount, request.Required);

            if(removeFromResource && request.Resource != null)
                request.Resource.Remove(request);

            return request.Available;
        }

        /// <summary>
        /// Determine resources available and perform transmutation if needed.
        /// </summary>
        /// <param name="resourceRequests">List of requests</param>
        /// <param name="uniqueActivityID">Unique id for the activity</param>
        public void CheckResources(IEnumerable<ResourceRequest> resourceRequests, Guid uniqueActivityID)
        {
            if (resourceRequests is null || !resourceRequests.Any())
            {
                this.Status = ActivityStatus.Success;
                return;
            }

            foreach (ResourceRequest request in resourceRequests)
            {
                request.ActivityID = uniqueActivityID;
                request.Available = 0;

                // If resource group does not exist then provide required.
                // This means when resource is not added to model it will not limit simulations
                if (request.ResourceType == null || Resources.FindResource(request.ResourceType) == null)
                {
                    request.Available = request.Required;
                    request.Provided = request.Required;
                }
                else
                {
                    if (request.ResourceType == typeof(Labour))
                        // get available labour based on rules.
                        request.Available = TakeLabour(request, false, this, Resources, this.OnPartialResourcesAvailableAction);
                    else
                        request.Available = TakeNonLabour(request, false);
                }
            }

            // are all resources available
            IEnumerable<ResourceRequest> shortfallRequests = resourceRequests.Where(a => a.Required > a.Available);
            if (shortfallRequests.Any())
                // check what transmutations can occur
                Resources.TransmutateShortfall(shortfallRequests);

            // check if need to do transmutations
            int countTransmutationsSuccessful = shortfallRequests.Where(a => a.TransmutationPossible == true && a.AllowTransmutation).Count();
            bool allTransmutationsSuccessful = (shortfallRequests.Where(a => a.TransmutationPossible == false && a.AllowTransmutation).Count() == 0);

            // OR at least one transmutation successful and PerformWithPartialResources
            if ((shortfallRequests.Any() && (shortfallRequests.Count() == countTransmutationsSuccessful)) || (countTransmutationsSuccessful > 0 && OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable))
            {
                // do transmutations.
                // this uses the current zone resources, but will find markets if needed in the process
                Resources.TransmutateShortfall(shortfallRequests, false);

                // recheck resource amounts now that resources have been topped up
                foreach (ResourceRequest request in resourceRequests)
                {
                    // get resource
                    request.Available = 0;
                    if (request.Resource != null)
                        // get amount available
                        request.Available = Math.Min(request.Resource.Amount, request.Required);
                }
            }

            bool deficitFound = false;
            // report any resource defecits here
            foreach (var item in resourceRequests.Where(a => (a.Required - a.Available) > 0.000001))
            {
                ResourceRequestEventArgs rrEventArgs = new ResourceRequestEventArgs() { Request = item };

                if (item.Resource != null && (item.Resource as Model).FindAncestor<Market>() != null)
                {
                    ActivitiesHolder marketActivities = Resources.FoundMarket.FindChild<ActivitiesHolder>();
                    if(marketActivities != null)
                        marketActivities.ReportActivityShortfall(rrEventArgs);
                }
                else
                    ActivitiesHolder.ReportActivityShortfall(rrEventArgs);
                Status = ActivityStatus.Partial;
                deficitFound = true;
            }
            if(!deficitFound)
                this.Status = ActivityStatus.Success;
        }

        /// <summary>
        /// Try to take the Resources based on Resource Request List provided.
        /// Returns true if it was able to take the resources it needed.
        /// Returns false if it was unable to take the resources it needed.
        /// </summary>
        /// <param name="resourceRequestList"></param>
        /// <param name="triggerActivityPerformed"></param>
        public bool TakeResources(List<ResourceRequest> resourceRequestList, bool triggerActivityPerformed)
        {
            // no resources required or this is an Activity folder.
            if ((resourceRequestList == null)||(!resourceRequestList.Any()))
                return false;

            // remove activity resources 
            // check if deficit and performWithPartial
            if ((resourceRequestList.Where(a => a.Required > a.Available).Count() == 0) || OnPartialResourcesAvailableAction != OnPartialResourcesAvailableActionTypes.SkipActivity)
            {
                if(OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    string resourcelist = string.Join("][r=", resourceRequestList.Where(a => a.Required > a.Available).Select(a => a.ResourceType.Name));
                    if (resourcelist.Length > 0)
                    {
                        string errorMessage = $"Insufficient [r={resourcelist}] for [a={this.NameWithParent}]{Environment.NewLine}[Report error and stop] is selected as action when shortfall of resources. Ensure sufficient resources are available or change OnPartialResourcesAvailableAction setting";
                        Status = ActivityStatus.Critical;
                        throw new ApsimXException(this, errorMessage);
                    }
                }

                foreach (ResourceRequest request in resourceRequestList)
                {
                    // get resource
                    request.Provided = 0;
                    // do not take if the resource does not exist
                    if (request.ResourceType != null && Resources.FindResource(request.ResourceType) != null)
                    { 
                        if (request.ResourceType == typeof(Labour))
                            // get available labour based on rules.
                            request.Available = TakeLabour(request, true, this, Resources, this.OnPartialResourcesAvailableAction);
                        else
                            request.Available = TakeNonLabour(request, true);
                    }
                }
            }
            else
                Status = ActivityStatus.Ignored;
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
            ActivitiesHolder?.ReportActivityPerformed(activitye);
        }

        /// <summary>
        /// Method to trigger an Activity Performed event 
        /// </summary>
        /// <param name="status">The status of this activity to be reported</param>
        public void TriggerOnActivityPerformed(ActivityStatus status)
        {
            this.Status = status;
            ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
            {
                Activity = new ActivityFolder() { Name = this.Name, Status = status }
            };
            ActivitiesHolder?.ReportActivityPerformed(activitye);
        }

    }
}
