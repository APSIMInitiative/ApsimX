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
        private Dictionary<string, object> identifiableModelsPresent = new Dictionary<string, object>();
        private protected Dictionary<(string type, string identifier, string unit), double?> valuesForIdentifiableModels = new Dictionary<(string type, string identifier, string unit), double?>();
        private Dictionary<string, LabelsForIdentifiableChildren> identifiableModelLabels = new Dictionary<string, LabelsForIdentifiableChildren>();

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

        #region Identifiable child model handling

        /// <summary>
        /// A method to return the list of labels provided by the parent activity for the given identifiable child model type
        /// </summary>
        /// <param name="labelType">The type of labels to provide</param>
        /// <typeparam name="T">Identifiable child model type</typeparam>
        /// <returns>List of labels for the selected style</returns>
        public List<string> IdentifiableChildModelLabels<T>(IdentifiableChildModelLabelType labelType) where T : IIdentifiableChildModel
        {
            if (this is ICanHandleIdentifiableChildModels)
            {
                LabelsForIdentifiableChildren labels;
                if (identifiableModelLabels.ContainsKey(typeof(T).Name))
                {
                    labels = identifiableModelLabels[typeof(T).Name];
                }
                else
                {
                    labels = DefineIdentifiableChildModelLabels<T>();
                    identifiableModelLabels.Add(typeof(T).Name, labels);
                }
                switch (labelType)
                {
                    case IdentifiableChildModelLabelType.Identifiers:
                        return labels.Identifiers;
                    case IdentifiableChildModelLabelType.Units:
                        return labels.Units;
                    default:
                        break;
                }
                return new List<string>();
            }
            else
                throw new NotImplementedException($"[a={NameWithParent}] does not support Identifiable child models to perform custom tasks with resource provision.");
        }

        /// <summary>
        /// A method to get a list of activity specified labels for a generic type T 
        /// </summary>
        /// <typeparam name="T">Identifiable child model type</typeparam>
        /// <returns>A LabelsForIdentifiableChildren containing all labels</returns>
        public virtual LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels<T>() where T : IIdentifiableChildModel
        {
            return new LabelsForIdentifiableChildren();
        }

        /// <summary>An event handler to allow us to make checks after resources and activities initialised.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        protected virtual void OnStartOfSimulationGetIdentifiableChildModels(object sender, EventArgs e)
        {
            // if this activity supports identifiable child models for controlling resource requirements
            if (this is ICanHandleIdentifiableChildModels)
            {
                // for each IIdentifiableChildMlode type in direct children 
                foreach (Type componentType in FindAllChildren<IIdentifiableChildModel>().Select(a => a.GetType()).Distinct())
                {
                    switch (componentType.Name)
                    {
                        case "RuminantGroup":
                            identifiableModelsPresent.Add(componentType.Name, LocateIdentifiableChildren<RuminantGroup>());
                            break;
                        case "LabourRequirement":
                            identifiableModelsPresent.Add(componentType.Name, LocateIdentifiableChildren<LabourRequirement>());
                            break;
                        case "RuminantActivityFee":
                            identifiableModelsPresent.Add(componentType.Name, LocateIdentifiableChildren<RuminantActivityFee>());
                            break;
                        default:
                            throw new NotSupportedException($"{componentType.Name} not currently supported as IdentifiableComponent");
                    }
                } 
            }
        }

        /// <summary>
        /// Get the IEnumerable(T) of all activity specified identifiable child models by type and identifer
        /// </summary>
        /// <typeparam name="T">The Identifiable child model type</typeparam>
        /// <param name="identifier">Identifer label</param>
        /// <param name="mustBeProvidedByUser">Determines if the parent requesting assumes the user will provide an instance of this child</param>
        /// <param name="addNewIfEmpty">Create IENumuerable with a new() instance of T</param>
        /// <returns>IEnumerable of T found</returns>
        protected private IEnumerable<T> GetIdentifiableChildrenByIdentifier<T>(string identifier, bool mustBeProvidedByUser, bool addNewIfEmpty) where T : IIdentifiableChildModel, new()
        {
            if (identifiableModelsPresent.ContainsKey(typeof(T).Name))
            {
                if (identifiableModelsPresent[typeof(T).Name] is Dictionary<string, IEnumerable<T>> foundTypeDictionary)
                {
                    if (foundTypeDictionary.ContainsKey(identifier))
                    {
                        return foundTypeDictionary[identifier];
                    }
                    else
                    {
                        if(IdentifiableChildModelLabels<T>(IdentifiableChildModelLabelType.Identifiers).Contains(identifier) == false)
                            throw new NotSupportedException($"[{GetType().Name}] does not support the identifier [{identifier}]{Environment.NewLine}Internal error during request for Identifiable child models: request support from developers.");
                    }
                }
            }
            if (mustBeProvidedByUser)
            {
                throw new ApsimXException(this, $"[a={NameWithParent}] requires at least one [{typeof(T).Name}] as a child component with the Identifier set as [{identifier}]");
            }
            else
            {
                if (addNewIfEmpty)
                    return new List<T>() { new T() };
            }
            return null;
        }

        /// <summary>
        /// Create a dictionary of groups of components by identifier provided by the parent model
        /// </summary>
        /// <typeparam name="T">Type of component to consider</typeparam>
        /// <returns></returns>
        protected private Dictionary<string, IEnumerable<T>> LocateIdentifiableChildren<T>() where T : IIdentifiableChildModel, new()
        {
            Dictionary<string, IEnumerable<T>> filters = new Dictionary<string, IEnumerable<T>>();

            foreach (var id in IdentifiableChildModelLabels<T>(IdentifiableChildModelLabelType.Identifiers))
            {
                var iChildren = FindAllChildren<T>().Where(a => a.Identifier == id && a.Enabled);
                if (iChildren.Any())
                {
                    filters.Add(id, iChildren);
                    // if this type provides units for use by children add them
                    if (IdentifiableChildModelLabels<T>(IdentifiableChildModelLabelType.Units).Any())
                    {
                        foreach (var item in iChildren)
                        {
                            valuesForIdentifiableModels.Add((typeof(T).Name, id, item.Units), 0);
                        }
                    }
                }
                //else
                //{
                //    if (addBlankEntryIfNoneFound)
                //    {
                //        var newEntry = new List<T>() { new T() { Identifier = id } };
                //        filters.Add(id, newEntry);
                //    }
                //}
            }
            return filters;
        }

        #endregion

        /// <summary>A method to arrange clearing status on CLEMStartOfTimeStep event</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        protected virtual void ResetActivityStatus(object sender, EventArgs e)
        {
            // clear Resources Required list
            ResourceRequestList = new List<ResourceRequest>();
            foreach (var key in valuesForIdentifiableModels.Keys.ToList())
            {
                valuesForIdentifiableModels[key] = null;
            }
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
                    //ResourceRequestList.AddRange(GetLabourRequiredForActivity());
                    // labour will be added when linked up as IIdentifiable

                    // add any non-labour resources needed based on method supplied by activity
                    var requests = DetermineResourcesForActivity();
                    if (requests != null)
                        ResourceRequestList.AddRange(requests);

                    // get all identifiable child related expense requests
                    if (this is ICanHandleIdentifiableChildModels)
                    {
                        // get all identifiable children except filter groups
                        foreach (IIdentifiableChildModel identifiableChild in FindAllChildren<IIdentifiableChildModel>().Where(a => (a is IFilterGroup) == false))
                        {
                            var unitsProvided = valuesForIdentifiableModels[(identifiableChild.GetType().Name, identifiableChild.Identifier??"", identifiableChild.Units??"")];
                            if (unitsProvided is null)
                                throw new ApsimXException(this, $"Units for [{identifiableChild.GetType().Name}]-[{identifiableChild.Identifier}]-[{identifiableChild.Units}] have not been calculated by [a={NameWithParent}] before use.{Environment.NewLine}Code issue. See Developers");
                            else
                            {
                                if ((unitsProvided??-1) > 0)
                                {
                                    foreach (ResourceRequest request in identifiableChild.GetResourceRequests(unitsProvided ?? 0))
                                    {
                                        request.ActivityModel = this;
                                        request.IdentifiableChildDetails = (identifiableChild.GetType().Name, identifiableChild.Identifier, identifiableChild.Units);
                                        ResourceRequestList.Add(request);
                                    } 
                                }
                            }
                        }
                    }

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

                    return;
                }
            }
            else
            {
                Status = ActivityStatus.Ignored;
            }
        }

        /// <summary>
        /// Determine the min proportion shortfall for the current resource request
        /// Only considers those coming from the IIdentifiable childen
        /// </summary>
        /// <param name="affectsActivityOnly">Only uses identifiable chilren flags as affetcs Activity in calculations if True</param>
        /// <param name="reduceAllIdentifableShortfalls"></param>
        /// <returns>Minimum proportion found</returns>
        public IEnumerable<ResourceRequest> MinimumShortfallProportion(bool affectsActivityOnly = true, bool reduceAllIdentifableShortfalls = true)
        {
            double min = 1;
            if (ResourceRequestList != null && ResourceRequestList.Any())
            {
                var shortfallRequests =  ResourceRequestList.Where(a => Math.Round(Math.Max(0, a.Provided - a.Required), 4) > 0 && a.AdditionalDetails is IIdentifiableChildModel && (!affectsActivityOnly || (a.AdditionalDetails as IIdentifiableChildModel).ShortfallAffectsActivity)).ToList();
                if (shortfallRequests.Any())
                {
                    min = shortfallRequests.Select(a => a.Provided / a.Required).Min();
                    foreach (var request in ResourceRequestList.Where(a => a.Provided / a.Required != min && a.AdditionalDetails is IIdentifiableChildModel && (!affectsActivityOnly || (a.AdditionalDetails as IIdentifiableChildModel).ShortfallAffectsActivity)))
                        request.Required = Math.Min(request.Provided, request.Required * min);

                    return shortfallRequests.OrderBy(a => a.Provided / a.Required);
                } 
            }
            return new List<ResourceRequest>();
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
                    if (item.FilterDetails != null && ((item.FilterDetails.First() as LabourFilterGroup).Parent as LabourRequirement).ShortfallAffectsActivity)
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
                    if (item.FilterDetails != null && ((item.FilterDetails.First() as LabourFilterGroup).Parent as LabourRequirement).ShortfallAffectsActivity)
                        proportion *= item.Provided / item.Required;
                }
                else // all other types
                    proportion = item.Provided / item.Required;
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
                    if (item.ShortfallAffectsActivity)
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
                this.Status = ActivityStatus.NotNeeded;
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
            // if no shortfalls or not skip activity if they are present
            if (resourceRequestList.Where(a => Math.Round(a.Available - a.Required, 4)>=0).Any() == false || OnPartialResourcesAvailableAction != OnPartialResourcesAvailableActionTypes.SkipActivity)
            {
                // check if deficit and performWithPartial
                if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    string resourcelist = string.Join("][r=", resourceRequestList.Where(a => Math.Round(a.Available - a.Required, 4) < 0).Select(a => a.ResourceType.Name));
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
