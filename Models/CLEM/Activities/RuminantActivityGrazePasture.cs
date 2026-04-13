using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Spreadsheet;
using Mapsui.Manipulations;
using MathNet.Numerics.Distributions;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.ServiceModel.Security;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant graze activity</summary>
    /// <summary>This activity determines how a ruminant group will graze</summary>
    /// <summary>It is designed to request food via a food store arbitrator</summary>
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
        private CLEMEvents events = null;
        private IEnumerable<RuminantActivityGrazePastureHerd> grazeHerdChildren;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; } = 8;

        /// <summary>
        /// Paddock or pasture to graze
        /// </summary>
        [Description("GrazeFoodStore/pasture to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze Food Store/pasture required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// paddock or pasture to graze
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
        /// Constructor using details from a GrazeAll activity
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
        /// Method to define dynamic breed grazing children
        /// </summary>
        public void SetupDynamicChildren()
        {
            HerdResource = Structure.Find<RuminantHerd>();
            if (HerdResource is null)
                return;

            GrazeFoodStoreModel = Resources.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            Guid nextUID = ActivitiesHolder.AddToGuID(UniqueID, 2);
            grazeHerdChildren = Structure.FindChildren<RuminantActivityGrazePastureHerd>();
            foreach (RuminantType herdType in Structure.FindChildren<RuminantType>(relativeTo: HerdResource))
            {
                if (grazeHerdChildren.Where(a => a.RuminantTypeModel != herdType).Any() == false)
                {
                    var newGrazePastureHerd = new RuminantActivityGrazePastureHerd(this, herdType, TransactionCategory, nextUID);
                    Structure.AddChild(newGrazePastureHerd);
                    Links links = new();
                    links.Resolve(newGrazePastureHerd as IModel, true, recurse: false);

                    var events = new Events(newGrazePastureHerd);
                    events.ConnectEvents();
                    events.PublishToModelAndChildren("Commencing", new object[] { newGrazePastureHerd, new EventArgs() });

                    nextUID = ActivitiesHolder.AddToGuID(nextUID, 2);
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            // This method does not take any resources but is used to arbitrate resources for all breed grazing activities it contains
            // if calling the getResources from child PastureHerd components they will only run once.

            double totalNeeded = 0;
            foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
            {
                grazeHerd.ResourceRequestList = null;
                totalNeeded += grazeHerd.CalculateFeedRequirement();
                //item.PotentialIntakePastureQualityLimiter = item.CalculatePotentialIntakePastureQualityLimiter();
                //var resourceRequest = item.RequestDetermineResources().Where(a => a.Resource is IGrazeFoodStoreType).FirstOrDefault();
                //totalNeeded += resourceRequest?.Required??0;
            }

            int greenAge = (events.Clock.Today.Month <= 3) ? 2 : 1;
            GeneratePoolsGroups(GrazeFoodStoreModel, grazeHerdChildren, greenAge);



            // Check available resources and only provide if there is truly competition between two or more breeds/herds
            double available = GrazeFoodStoreModel.Amount;
            double limit = 1.0;
            if (MathUtilities.IsPositive(totalNeeded) & grazeHerdChildren.Count() > 1)
            {
                limit = Math.Min(1.0, available / totalNeeded);
            }

            // apply limits to children
            foreach (RuminantActivityGrazePastureHerd item in grazeHerdChildren)
            {
                if (item.GrazeFoodStoreModel is GrazeFoodStoreAPSIMLink)
                    item.GrazingCompetitionLimiter = limit;
                else
                    item.SetupPoolsAndLimits(limit);
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method to create mixed pasture pool groups for feeding.
        /// </summary>
        /// <param name="grazeFoodStore">The graze food store type for the pasture.</param>
        /// <param name="grazeHerdModels">The collection of graze herd models to include in calculations.</param>
        /// <param name="greenAge">The age (in months) for pasture to be considered green. (-1 ignore green details)</param>
        /// <param name="dmdStep">The step size for Dry Matter Digestibility (DMD) categories (100 no groups).</param>
        public static void GeneratePoolsGroups(IGrazeFoodStoreType grazeFoodStore, IEnumerable<RuminantActivityGrazePastureHerd> grazeHerdModels, int greenAge = -1, int dmdStep = 10)
        {
            List<GrazeFoodStorePool> pasturePools;
            if (grazeFoodStore is GrazeFoodStoreType grazeFoodStoreType)
                pasturePools = grazeFoodStoreType.Pools;
            else
                // Handled in GrazeFoodStoreAPSIMLink. Could move code here out of resource
                return;

            // organise groups of pasture pools based on whether green and DMD categories.
            double green = grazeFoodStoreType.Pools.Where(a => (a.Age <= greenAge)).Sum(b => b.Amount);

            var nestedGroups = pasturePools
            .GroupBy(a => a.Age <= greenAge)
            .Select(dmdGroup => new
            {
                Green = dmdGroup.Key,
                DMDGroups = dmdGroup
                    .GroupBy(s => Convert.ToInt32(s.DryMatterDigestibility / dmdStep) * dmdStep)
                    .Select(groups => new GrazePasturePoolGroup(
                        $"{grazeFoodStoreType.Name}_{(dmdGroup.Key ? "alive" : "dead")}_DMD{groups.Key}",
                        groups.Select(a => a)
                        )
                    )
            });

            // clear relative intake details from previous time step
            foreach (RuminantActivityGrazePastureHerd item in grazeHerdModels)
            {
                // item.RelativePoolIntake = new [nestedGroups.Count(), nestedGroups.Max(a => a.DMDGroups.Count())];
            }

            // group pools by green and then by DMD 
            foreach (var greenGroup in nestedGroups)
            {
                foreach (var dmdGroup in greenGroup.DMDGroups)
                {
                    double amountNeeded = 0;

                    // calculate limits for each breed/herd based on requirements and pool quality
                    foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdModels)
                    {
                        // calculate green pool limits to apply
                        foreach (var ind in grazeHerd.CurrentHerd())
                        {
                            // work out relative intake
                            // start with green prop
                            relIntake

                            amountNeeded += ind.Intake * limits * relIntake[]

                        }

                        // reduce intake if exceeded amount available.

                    }

            }
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
            if (GrazeFoodStoreTypeName.Contains("."))
            {
                ResourcesHolder resHolder = Structure.Find<ResourcesHolder>();
                if (resHolder is null || resHolder.FindResourceType<GrazeFoodStore, IGrazeFoodStoreType>(this, GrazeFoodStoreTypeName) is null)
                {
                    yield return new ValidationResult($"The location defined for grazing [r={GrazeFoodStoreTypeName}] in [a={Name}] is not found.{Environment.NewLine}Ensure [r=GrazeFoodStore] is present and the [GrazeFoodStoreType] is present", new string[] { "Location is not valid" });
                }
            }
        }
        #endregion
    }
}
