using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;
using Models.CLEM.Groupings;
using APSIM.Numerics;
using APSIM.Core;

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
        public GrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RuminantActivityGrazePasture()
        {
        }

        /// <summary>
        /// Constructor using details from a GrazeAll activity
        /// </summary>
        public RuminantActivityGrazePasture(RuminantActivityGrazeAll grazeAll, GrazeFoodStoreType pastureType, string transactionCategory, Guid parentProvidedUid)
        {
            CLEMParentName = grazeAll.CLEMParentName;
            GrazeFoodStoreTypeName = pastureType.NameWithParent;
            HoursGrazed = grazeAll.HoursGrazed;
            TransactionCategory = transactionCategory;
            Name = "Graze_" + (pastureType).Name;
            OnPartialResourcesAvailableAction = grazeAll.OnPartialResourcesAvailableAction;
            Status = ActivityStatus.NoTask;
            UniqueID = parentProvidedUid;
            Resources = grazeAll.Resources;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GrazeFoodStoreModel = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            //Create list of children by breed
            //Guid currentUid = UniqueID;
            List<IModel> grazePastureList = new();

            bool buildTransactionFromTree = Structure.FindParent<ZoneCLEM>(recurse: true).BuildTransactionCategoryFromTree;
            string transCat = "";
            if (!buildTransactionFromTree)
            {
                transCat = TransactionCategory;
            }

            InitialiseHerd(true, true);
            Guid nextUID = ActivitiesHolder.AddToGuID(UniqueID, 2);
            foreach (RuminantType herdType in Structure.FindChildren<RuminantType>(relativeTo: HerdResource))
            {
                var newGrazePastureHerd = new RuminantActivityGrazePastureHerd(this, herdType, transCat, nextUID);
                Structure.AddChild(newGrazePastureHerd);
                var events = new Events(newGrazePastureHerd);
                // Publish Commencing event
                events.PublishToModelAndChildren("CLEMInitialiseActivity", new object[] { newGrazePastureHerd, new EventArgs() });

                nextUID = ActivitiesHolder.AddToGuID(nextUID, 2);
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            // This method does not take any resources but is used to arbitrate resources for all breed grazing activities it contains

            // check nested graze breed requirements for this pasture
            double totalNeeded = 0;
            IEnumerable<RuminantActivityGrazePastureHerd> grazeHerdChildren = Structure.FindChildren<RuminantActivityGrazePastureHerd>();
            double potentialIntakeLimiter = -1;
            foreach (RuminantActivityGrazePastureHerd item in grazeHerdChildren)
            {
                if (MathUtilities.IsNegative(potentialIntakeLimiter))
                {
                    potentialIntakeLimiter = item.CalculatePotentialIntakePastureQualityLimiter();
                }

                item.ResourceRequestList = null;
                item.PotentialIntakePastureQualityLimiter = potentialIntakeLimiter;
                var resourceRequest = item.RequestDetermineResources().Where(a => a.Resource is GrazeFoodStoreType).FirstOrDefault();
                if (resourceRequest != null)
                {
                    totalNeeded += resourceRequest.Required;
                }
            }

            // Check available resources
            // This determines the proportional amount available for competing breeds with different green diet proportions
            // It does not truly account for how the pasture is provided from pools but will suffice unless more detailed model required
            double available = GrazeFoodStoreModel.Amount;
            double limit = 1.0;
            if (MathUtilities.IsPositive(totalNeeded))
            {
                limit = Math.Min(1.0, available / totalNeeded);
            }

            // apply limits to children
            foreach (RuminantActivityGrazePastureHerd item in grazeHerdChildren)
            {
                item.SetupPoolsAndLimits(limit);
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
            if (GrazeFoodStoreTypeName.Contains("."))
            {
                ResourcesHolder resHolder = Structure.Find<ResourcesHolder>();
                if (resHolder is null || resHolder.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, GrazeFoodStoreTypeName) is null)
                {
                    yield return new ValidationResult($"The location defined for grazing [r={GrazeFoodStoreTypeName}] in [a={Name}] is not found.{Environment.NewLine}Ensure [r=GrazeFoodStore] is present and the [GrazeFoodStoreType] is present", new string[] { "Location is not valid" });
                }
            }
        }
        #endregion
    }
}
