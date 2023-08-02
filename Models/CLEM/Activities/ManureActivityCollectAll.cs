using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Limiters;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core.Attributes;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant manure collection activity</summary>
    /// <summary>This activity performs the collection of all manure</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Undertake the collection of manure from all paddocks and yards in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Manure/CollectManureAll.htm")]
    public class ManureActivityCollectAll : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        [Link]
        private IClock clock = null;

        private ProductStoreTypeManure manureStore;
        private ActivityCarryLimiter limiter;
        private double amountToDo;
        private double amountToSkip;

        /// <summary>
        /// Constructor
        /// </summary>
        public ManureActivityCollectAll()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // activity is performed in CLEMCollectManure not CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            manureStore = Resources.FindResourceType<ProductStore, ProductStoreTypeManure>(this, "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);

            // locate a cut and carry limiter associarted with this event.
            limiter = ActivityCarryLimiter.Locate(this);
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per kg collected",
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to perform manure collection</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCollectManure")]
        private void OnCLEMCollectManure(object sender, EventArgs e)
        {
                ManageActivityResourcesAndTasks();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToSkip = 0;

            amountToDo = manureStore?.UncollectedStores.Sum(a => a.Pools.Sum(b => b.WetWeight))??0;

            // reduce amount by limiter if present.
            if (limiter != null)
            {
                double canBeCarried = limiter.GetAmountAvailable(clock.Today.Month);
                Status = ActivityStatus.Warning;
                AddStatusMessage("CutCarry limit enforced");
                amountToDo = Math.Max(amountToDo, canBeCarried);
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels.ToList())
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per kg collected":
                        valuesForCompanionModels[valueToSupply.Key] = amountToDo;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var tagsShort = shortfalls.FirstOrDefault();
                amountToSkip = Convert.ToInt32(amountToDo * (1 - tagsShort.Available / tagsShort.Required));
                if (amountToSkip < 0)
                {
                    Status = ActivityStatus.Warning;
                    AddStatusMessage("Resource shortfall prevented any action");
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            double amountTaken = 0;
            if(amountToDo > 0)
            {
                if (manureStore != null)
                {
                    foreach (ManureStoreUncollected msu in manureStore.UncollectedStores)
                    {
                        double propCollected = 1;
                        double uncollected = msu.Pools.Sum(a => a.WetWeight);
                        if(MathUtilities.IsGreaterThanOrEqual(amountTaken + uncollected, amountToDo - amountToSkip))
                        {
                            propCollected = Math.Max(0, (amountToDo - amountToSkip) - amountTaken) / uncollected;
                        }
                        manureStore.Collect(msu.Name, propCollected, this);
                    }
                }
                limiter.AddWeightCarried(amountToDo - amountToSkip);
            }
            SetStatusSuccessOrPartial(amountToSkip > 0);
        }

        ///<inheritdoc/>
        public override string ModelSummary()
        {
            return "\r\n<div class=\"activityentry\">Collect manure from all pasture</div>";
        }

    }
}
