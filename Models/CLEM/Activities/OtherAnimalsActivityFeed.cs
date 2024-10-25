using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Models.Core.Attributes;
using DocumentFormat.OpenXml.Office.CustomUI;
using System.Linq;
using APSIM.Shared.Utilities;
using Docker.DotNet.Models;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals feed activity</summary>
    /// <summary>This activity provides food to specified other animals based on a feeding style</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the feeding of a specified type of other animal based on a feeding style")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityFeed.htm")]
    public class OtherAnimalsActivityFeed : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private IEnumerable<OtherAnimalsGroup> filterGroups;
        private OtherAnimals otherAnimals;
        int numberToDo = 0;
        double amountToDo = 0;
        double feedEstimated = 0;

        /// <summary>
        /// Name of Feed to use
        /// </summary>
        [Description("Feed store to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Feed type to use required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [Description("Feeding style to use")]
        [System.ComponentModel.DefaultValueAttribute(OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount)]
        [Required]
        public OtherAnimalsFeedActivityTypes FeedStyle { get; set; } = OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount;

        /// <summary>
        /// Feed type
        /// </summary>
        [JsonIgnore]
        public IFeedType FeedType { get; set; }

        /// <summary>
        /// Provides the redicted other animal name based on filtering 
        /// </summary>
        public string PredictedAnimalName { get; set; } = "NA";

        /// <summary>
        /// The list of cohorts remaining to be fed in the current timestep
        /// </summary>
        [JsonIgnore]
        public List<OtherAnimalsTypeCohort> CohortsToBeFed { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            otherAnimals = Resources.FindResourceGroup<OtherAnimals>();
            filterGroups = GetCompanionModelsByIdentifier<OtherAnimalsFeedGroup>(true, false);

            // locate FeedType resource
            FeedType = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "OtherAnimalsFeedGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() { "Feed provided" }
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number fed",
                            "Feed provided"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "per kg feed"
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
            CohortsToBeFed  = otherAnimals.GetCohorts(filterGroups, false).ToList();
            foreach (var cohort in CohortsToBeFed)
            {
                cohort.Considered = false;
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
