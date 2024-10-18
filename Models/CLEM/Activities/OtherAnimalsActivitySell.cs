using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Interfaces;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Microsoft.VisualBasic.FileIO;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to price and sell other animals
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the sale of specified other animals")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/SellOtherAnimals.htm")]
    public class OtherAnimalsActivitySell : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private IEnumerable<OtherAnimalsGroup> filterGroups;

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "No finance required", typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        [System.ComponentModel.DefaultValueAttribute("No finance required")]
        public string AccountName { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            filterGroups = GetCompanionModelsByIdentifier<OtherAnimalsGroup>(true, false);
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "OtherAnimalsGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() { "Individuals" }
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number sold"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            amountToDo = 0;
            feedEstimated = 0;
            CohortsToBeFed = new List<OtherAnimalsTypeCohort>();
            List<string> animalsIncluded = new();

            foreach (var filter in filterGroups)
            {
                if (!animalsIncluded.Contains(filter.SelectedOtherAnimalsType.Name))
                    animalsIncluded.Add(filter.SelectedOtherAnimalsType.Name);
                CohortsToBeFed = CohortsToBeFed.Union(filter.Filter(filter.SelectedOtherAnimalsType.Cohorts));
            }

            numberToDo = CohortsToBeFed.Sum(a => a.Number);

            if (animalsIncluded.Any())
            {
                if (animalsIncluded.Count == 1)
                    PredictedAnimalName = animalsIncluded[0];
                else
                    PredictedAnimalName = "Multiple animals";
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            feedEstimated = filterGroups.OfType<OtherAnimalsFeedGroup>().Sum(a => a.CurrentResourceRequest.Required);

            foreach (var valueToSupply in valuesForCompanionModels)
            {
                int number = numberToDo;

                switch (valueToSupply.Key.type)
                {
                    case "OtherAnimalsFeedGroup":
                        valuesForCompanionModels[valueToSupply.Key] = feedEstimated;
                        break;
                    case "LabourRequirement":
                    case "ActivityFee":
                        switch (valueToSupply.Key.identifier)
                        {
                            case "Number fed":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "per head":
                                        valuesForCompanionModels[valueToSupply.Key] = number;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            case "Feed provided":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "per kg fed":
                                        amountToDo = feedEstimated;
                                        valuesForCompanionModels[valueToSupply.Key] = feedEstimated;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            default:
                                throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                        }
                        break;
                    default:
                        throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (feedEstimated > 0)
            {
                if (feedEstimated - filterGroups.OfType<OtherAnimalsFeedGroup>().Sum(a => a.CurrentResourceRequest.Required) > 0)
                {
                    Status = ActivityStatus.Partial;
                    return;
                }
                Status = ActivityStatus.Success;
            }
        }

    }
}
