using Models.CLEM.Interfaces;
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
    /// <summary>This activity manages grazing of all ruminants in a specified pasture</summary>
    /// <remarks>
    /// <para>This activity will manage grazing in a paddock by dynamically creating the range of <c>
    /// RuminantActivityGrazePastureHerd</c> activities needed to represent the specified paddock</para>
    /// <para>Individuals will graze based on their location defined by other activities such as <c>
    /// RuminantActivityMove</c></para>
    /// <para>Use <c>RuminantActivityGrazeAll</c> to automatically manage grazing across all paddocks</para>
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform grazing of all herds within a specified pasture (paddock)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGraze.htm")]
    public class RuminantActivityGrazePasture : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        private readonly CLEMEvents events = null;
        private IEnumerable<RuminantActivityGrazePastureHerd> grazeHerdChildren;
        private double[] totalNeededDaily;
        private double[] totalRequestedTimeStep;

        /// <summary>
        /// Number of hours grazed
        /// </summary>
        /// <remarks>
        /// <para>Number of hours grazed based on 8 hour grazing days.</para>
        /// <para>This could be modified to account for rain/heat walking to water etc. in future</para>
        /// </remarks>
        [Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; } = 8;

        /// <summary>
        /// Name of pasture to graze
        /// </summary>
        /// <remarks>
        /// A required field. A list of available paddocks (<c>GrazeFoodStore</c>) is provided in a dropdown
        /// list in the UI.
        /// </remarks>
        [Description("GrazeFoodStore/pasture to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze Food Store/pasture required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// The model representing the paddock or pasture to graze
        /// </summary>
        [JsonIgnore]
        public IGrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RuminantActivityGrazePasture()
        {
        }

        /// <summary>
        /// Constructor using details provided by a parent <c>RuminantActivityGrazeAll</c> activity
        /// </summary>
        public RuminantActivityGrazePasture(RuminantActivityGrazeAll grazeAll, IGrazeFoodStoreType pastureType, string transactionCategory, Guid parentProvidedUid)
        {
            CLEMParentName = grazeAll.CLEMParentName;
            GrazeFoodStoreTypeName = (pastureType as CLEMModel).NameWithParent;
            HoursGrazed = grazeAll.HoursGrazed;
            TransactionCategory = transactionCategory;
            Name = "Graze_" + (pastureType).Name;
            OnPartialResourcesAvailableAction = grazeAll.OnPartialResourcesAvailableAction;
            Status = ActivityStatus.NoTask;
            UniqueID = parentProvidedUid;
            Parent = grazeAll;
            Resources = grazeAll.Resources;
        }

        /// <summary>
        /// An event handler allowing for the dynamic creation of herd grazing activity components
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed early in the model initialisation process during the <c>Clock.Commencing</c> event to
        /// ensure components are created beofre needing to be initialised. 
        /// </remarks>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            SetupDynamicChildren();

            if (Parent is not RuminantActivityGrazeAll)
            {
                var activities = Structure.FindParent<Simulation>(recurse: true);
                var apsimEvents = new Events(activities);
                apsimEvents.ReconnectEvents("CLEMEvents", "CLEMGetResourcesRequired");
            }
        }

        /// <summary>
        /// A method to dynamically create all pasture-herd <c>RuminantActivityGrazePastureHerd</c> children for grazing
        /// </summary>
        public void SetupDynamicChildren()
        {
            HerdResource = Structure.Find<RuminantHerd>();
            if (HerdResource is null)
                return;

            GrazeFoodStoreModel = Resources.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            Guid nextUID = ActivitiesHolder.AddToGuID(UniqueID, 2);
            grazeHerdChildren = Structure.FindChildren<RuminantActivityGrazePastureHerd>();
            totalNeededDaily = [grazeHerdChildren.Count()];
            totalRequestedTimeStep = [grazeHerdChildren.Count()];
            foreach (RuminantType herdType in Structure.FindChildren<RuminantType>(relativeTo: HerdResource))
            {
                if (grazeHerdChildren.Where(a => a.RuminantTypeModel != herdType).Any() == false)
                {
                    var newGrazePastureHerd = new RuminantActivityGrazePastureHerd(this, herdType, TransactionCategory, nextUID)
                    {
                        OnPartialResourcesAvailableAction = this.OnPartialResourcesAvailableAction
                    };
                    Structure.AddChild(newGrazePastureHerd);
                    Links links = new();
                    links.Resolve(newGrazePastureHerd as IModel, true, recurse: false);

                    var events = new Events(newGrazePastureHerd);
                    events.ConnectEvents();
                    events.PublishToModelAndChildren("Commencing", [newGrazePastureHerd, new EventArgs()]);

                    nextUID = ActivitiesHolder.AddToGuID(nextUID, 2);
                }
            }
        }

        /// <summary>
        /// Method to calculate the shortfall multiplier based on the total intake required and the amount available in
        /// the pools. This is used to reduce intake proportionally across all herds when there is insufficient pasture
        /// to meet requirements.
        /// </summary>
        /// <param name="pastureAvailable">Pasture available at start of time step</param>
        /// <param name="intakeRequired">Intake required by herd</param>
        /// <returns>Shortfall multiplier</returns>
        public static double CalculateShortfallMultiplier(double pastureAvailable, double[] intakeRequired)
        {
            double competitionMultiplier = 1.0;
            double totalIntake = intakeRequired.Sum();
            if (totalIntake <= pastureAvailable)
            {
                return competitionMultiplier;
            }
           
            return 1.0 - (totalIntake - pastureAvailable) / totalIntake;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            // generate pool groups based on available pasture in the graze food store pools
            // For each pool group (i.e. based on DMD)
            // fill individual's feed requirements from first pool
            // Reduce based on shortfall of pasture for all herds. 

            int greenAge = (events.Clock.Today.Month <= 3) ? 2 : 1;
            var intakeGroups = GrazeFoodStoreModel.GenerateIntakeGroups(events.Interval, greenAge);

            int cnt = 0;
            foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
            {
                grazeHerd.DigestiblePasturePoolGroups = intakeGroups;
                grazeHerd.ResourceRequestList.Clear();
                totalNeededDaily[cnt] = grazeHerd.CalculateDailyFeedRequirement();
                cnt++;
            }

            // fill all animals by calculating relative fill for each pool group 
            for (int i = 0; i < intakeGroups.Count; i++)
            {
                // fill pool by desired by each herd
                cnt = 0;
                double intakeSum = 0;
                foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
                {
                    totalRequestedTimeStep[cnt] = grazeHerd.PrepareTakeFromGrazingPoolGroup(intakeGroups[i], i) * totalNeededDaily[cnt] * events.Interval;
                    intakeSum += totalRequestedTimeStep[cnt];
                    cnt++;
                }

                // shortfall limiter
                double shortfallMultiplier = CalculateShortfallMultiplier(intakeGroups[i].Pools.Sum(a => a.AmountAvailable), totalRequestedTimeStep);

                foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
                {
                    grazeHerd.TakeFromGrazingPoolGroup(intakeGroups[i], i, shortfallMultiplier);
                }
            }

            foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
            {
                grazeHerd.CreateResourceRequest();
                grazeHerd.PotentialIntakeShortfallLimiter = grazeHerd.CalculatePotentialShortfallLimiter();
            }
            return ResourceRequestList;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (Status != ActivityStatus.Partial && Status != ActivityStatus.Critical)
            {
                Status = ActivityStatus.NoTask;
            }
            return;
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (GrazeFoodStoreTypeName.Contains('.'))
            {
                ResourcesHolder resHolder = Structure.Find<ResourcesHolder>();
                if (resHolder is null || resHolder.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName) is null)
                {
                    yield return new ValidationResult($"The location defined for grazing [r={GrazeFoodStoreTypeName}] in [a={Name}] is not found.{Environment.NewLine}Ensure [r=GrazeFoodStore] is present and the [GrazeFoodStoreType] is present", ["Location is not valid"]);
                }
            }
        }
        #endregion
    }
}
