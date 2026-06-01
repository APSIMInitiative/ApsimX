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
    /// <summary>This activity determines how ruminants will graze a specified pasture</summary>
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
        private double[] totalNeededDaily;
        private double[] totalRequestedTimeStep;

        /// <summary>
        /// Number of hours grazed based on an 8 hour grazing days. Can be modfied later to account for rain/heat
        /// walking to water etc.
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
        /// Constructor using details from a parent GrazeAll activity
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
        /// Method to dynamically create all pasture-herd children for grazing
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
                    var newGrazePastureHerd = new RuminantActivityGrazePastureHerd(this, herdType, TransactionCategory, nextUID);
                    newGrazePastureHerd.OnPartialResourcesAvailableAction = this.OnPartialResourcesAvailableAction;
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

        /// <summary>
        /// Method to calculate the shortfall multiplier based on the total intake required and the amount available in
        /// the pools. This is used to reduce intake proportionally across all herds when there is insufficient pasture
        /// to meet requirements.
        /// </summary>
        /// <param name="pastureAvailable">Pasture available at start of time step</param>
        /// <param name="intakeRequired">Intake required by herd</param>
        /// <returns></returns>
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
            // generate pool gropus based on available pasture in the graze food store pools
            // For each pool group (i.e. based on DMD)
            // fill individual's feed requirements from first pool
            // Reduce based on shortfall of pasture for all herds. 

            //totalPastureAvailable = GrazeFoodStoreModel.AmountAvailable;
            int greenAge = (events.Clock.Today.Month <= 3) ? 2 : 1;
            var intakeGroups = GrazeFoodStoreModel.GenerateIntakeGroups(events.Interval, greenAge);

            int cnt = 0;
            foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
            {
                grazeHerd.DigestiblePasturePoolGroups = intakeGroups;
                grazeHerd.ResourceRequestList.Clear();
                totalNeededDaily[cnt] = grazeHerd.CalculateDailyFeedRequirement(greenAge);
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

            // looking at form of relationships for lowbiomass and greenpasture modifiers, I don't think there is much scope to take more intake if still not satisfied

            // determine if individuals can still feed by order of least overhead
            // if StrictFeedingLimits = false - individuals can eat beyond the green limit if needed
            // if green proportion < 1 - there was a green limit imposed
            // there is still feed in green pools
            // if individual unsatisfied - still demands feed

            /*            foreach (RuminantActivityGrazePastureHerd grazeHerd in grazeHerdChildren)
                        {
                            if (grazeHerd.RuminantTypeModel.Parameters.Grazing.StrictFeedingLimits)
                                continue;
                            if (grazeHerd.PotentialIntakeGreenPastureLimiter >= 1.0)
                                continue;

                            foreach (var poolGroup in poolGroups.Where(a => a.ProportionGreen > 0.9 && a.Pools.Sum(b => b.AmountAvailable) > 0))
                            {
                                if (grazeHerd.DailyPastureTaken >= grazeHerd.DailyPastureRequired)
                                    continue;

                                throw new NotImplementedException("Second take from green pools is not enabled in current version");
                            }
                        }*/

            return ResourceRequestList;
        }

        ///// <summary>
        ///// Method to create mixed pasture pool groups for feeding.
        ///// </summary>
        ///// <param name="grazeFoodStore">The graze food store type for the pasture.</param>
        ///// <param name="numberOfTimesteps">Number of timesteps to convert daily to total intake</param>
        ///// <param name="greenAge">
        ///// The age (in months) for pasture to be considered green. (-1 ignore green details)
        ///// </param>
        ///// <param name="dmdStep">The step size for Dry Matter Digestibility (DMD) categories (100 no groups).</param>
        //public static List<FoodResourceStore> GeneratePoolsGroups(IGrazeFoodStoreType grazeFoodStore, int numberOfTimesteps, int greenAge = -1, int dmdStep = 10)
        //{
        //    IEnumerable<GrazeFoodStorePool> pasturePools;
        //    if (grazeFoodStore is GrazeFoodStoreType grazeFoodStoreType)
        //    {
        //        pasturePools = grazeFoodStoreType.Pools;
        //    }
        //    else
        //    {
        //        // Handled in GrazeFoodStoreAPSIMLink. Could move code here out of resource
        //        return null;
        //    }

        //    // think about different approaches
        //    // 1. whole avearge pasture pool (DMD step = 100)
        //    // 2. select by DMD - current DMD step (e.g. 10)
        //    // 3. proportional with weighting toward green
        //    // 4. CLEM green biomass limit - implemented
        //    // 5. CLEM low biomass intake limited - implemented

        //    // individual selective ability proceedures can be actioned in GeneratePoolGroups and thus the list and order of pools the animals feed from.

        //    var nestedGroups = pasturePools
        //        .GroupBy(s => Convert.ToInt32(s.DryMatterDigestibility / dmdStep) * dmdStep)
        //        .Select(groups => new FoodResourceStore(
        //            groups.ToList(),
        //            greenAge,
        //            numberOfTimesteps
        //            )
        //        ).OrderByDescending(a => a.Details.DryMatterDigestibility);

        //    return nestedGroups.ToList();
        //}

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
