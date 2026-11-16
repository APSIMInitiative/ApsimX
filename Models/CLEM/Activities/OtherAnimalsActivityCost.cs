using Models.CLEM.Interfaces;
using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Resources;
using System.IO;
using Models.CLEM.Groupings;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals task activity</summary>
    /// <summary>This activity allows labour and costs to be applied to selected other animals</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Allows labour and costs to be applied to specified other animals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityCost.htm")]
    public class OtherAnimalsActivityCost : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private int numberToDo = 0;
        private IEnumerable<OtherAnimalsGroup> filterGroups;
        private OtherAnimals otherAnimals;

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            return type switch
            {
                "OtherAnimalsGroup" => new LabelsForCompanionModels(
                                        identifiers: [],
                                        measures: []
                                        ),
                "LabourRequirement" or "ActivityFee" => new LabelsForCompanionModels(
                                        identifiers: [
                                            "Number to perform"
                                        ],
                                        measures: [
                                            "fixed",
                            "per head"
                                        ]
                                        ),
                _ => new LabelsForCompanionModels(),
            };
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            filterGroups = GetCompanionModelsByIdentifier<OtherAnimalsGroup>(false, true);
            otherAnimals = Resources.FindResourceGroup<OtherAnimals>();
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            numberToDo = 0;
            IEnumerable<OtherAnimalsTypeCohort> CohortsToReport = otherAnimals.GetCohorts(filterGroups, true);
            numberToDo = CohortsToReport.Sum(a => a.Number);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                valuesForCompanionModels[valueToSupply.Key] = valueToSupply.Key.type switch
                {
                    "OtherAnimalsGroup" => numberToDo,
                    "LabourRequirement" or "ActivityFee" => valueToSupply.Key.identifier switch
                    {
                        "Number to buy" => valueToSupply.Key.unit switch
                        {
                            "fixed" => 1,
                            "per head" => (double?)numberToDo,
                            _ => throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key)),
                        },
                        _ => throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key)),
                    },
                    _ => throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key)),
                };
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // get greatest shortfall by proportion
                var buyShort = shortfalls.OrderBy(a => a.Provided / a.Required).FirstOrDefault();
                int reduce = Convert.ToInt32(numberToDo * buyShort.Provided / buyShort.Required);
                numberToDo -= reduce;
                this.Status = ActivityStatus.Partial;
            }
            else
            {
                this.Status = ActivityStatus.Success;
            }
        }
    }
}
