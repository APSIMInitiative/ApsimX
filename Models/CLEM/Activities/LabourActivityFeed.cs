using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Labour (Human) feed activity</summary>
    /// <summary>This activity provides food to specified people based on a feeding style</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Feed people (labour) as selected with a specified feeding style.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeed.htm")]
    public class LabourActivityFeed : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        private int numberToDo;
        private double amountToDo;
        private IEnumerable<LabourFeedGroup> filterGroups;
        private IEnumerable<LabourType> population;
        private IEnumerable<LabourType> uniqueIndividuals;
        private List<(LabourType, double)> indFed;
        private ResourceRequest resourceRequest;

        /// <summary>
        /// Name of Human Food to use (with Resource Group name appended to the front [separated with a '.'])
        /// </summary>
        [Description("Food to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(HumanFoodStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Food type required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(LabourFeedActivityTypes.SpecifiedDailyAmountPerAE)]
        [Description("Feeding style to use")]
        [Required]
        public LabourFeedActivityTypes FeedStyle { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [JsonIgnore]
        public HumanFoodStoreType FeedType { get; private set; }

        /// <summary>
        /// The list of individuals remaining to be fed in the current timestep
        /// </summary>
        [JsonIgnore]
        public IEnumerable<LabourType> IndividualsToBeFed { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeed()
        {
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "LabourFeedGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>()
                        {
                            "SpecifiedDailyAmountPerIndividual",
                            "SpecifiedDailyAmountPerAE"
                        }
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate FeedType resource
            FeedType = Resources.FindResourceType<HumanFoodStore, HumanFoodStoreType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            filterGroups = GetCompanionModelsByIdentifier<LabourFeedGroup>(true, false);

            ResourcesHolder resourcesHolder = FindInScope<ResourcesHolder>();
            Labour labour = resourcesHolder.FindResource<Labour>();
            if (labour != null)
                population = labour.Items;
        }

        /// <summary>
        /// A method to return the unique individuals from a list and multiple potentially overlapping filter groups
        /// </summary>
        /// <param name="filters">The filter groups to include</param>
        /// <param name="population">the individuals to filter</param>
        /// <returns>A list of unique individuals</returns>
        public static IEnumerable<T> GetUniqueIndividuals<T>(IEnumerable<LabourGroup> filters, IEnumerable<T> population) where T : LabourType
        {
            // no filters provided
            if (!filters.Any())
            {
                return population;
            }
            // check that no filters will filter all groups otherwise return all 
            // account for any sorting or reduced takes
            var emptyfilters = filters.Where(a => a.FindAllChildren<Filter>().Any() == false);
            if (emptyfilters.Any())
            {
                foreach (var empty in emptyfilters.Where(a => a.FindAllChildren<ISort>().Any() || a.FindAllChildren<TakeFromFiltered>().Any()))
                    population = empty.Filter(population);
                return population;
            }
            else
            {
                // get unique individuals across all filters
                if (filters.Count() > 1)
                {
                    IEnumerable<T> unique = new List<T>();
                    foreach (var selectFilter in filters)
                        unique = unique.Union(selectFilter.Filter(population)).DistinctBy(a => a.Name);
                    return unique;
                }
                else
                {
                    return filters.FirstOrDefault().Filter(population);
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> resourceRequests = new List<ResourceRequest>();
            numberToDo = 0;
            amountToDo = 0;
            uniqueIndividuals = GetUniqueIndividuals(filterGroups.Cast<LabourGroup>(), population);
            numberToDo = uniqueIndividuals?.Count() ?? 0;
            IndividualsToBeFed = uniqueIndividuals;

            List<LabourType> inds = uniqueIndividuals.ToList();
            indFed = new List<(LabourType, double)>();

            foreach (LabourFeedGroup child in filterGroups)
            {
                var filteredInd = child.Filter(inds);
                // get list from filters
                foreach (LabourType ind in filteredInd)
                {
                    numberToDo++;
                    // feed limited to the daily intake per ae set in HumanFoodStoreType
                    switch (FeedStyle)
                    {
                        case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                            amountToDo += child.Value * events.Interval;
                            indFed.Add((ind, child.Value * events.Interval));
                            break;
                        case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                            amountToDo += child.Value * ind.AdultEquivalent * events.Interval;
                            indFed.Add((ind, child.Value * ind.AdultEquivalent * events.Interval));
                            break;
                        default:
                            throw new Exception(String.Format("FeedStyle {0} is not supported in {1}", FeedStyle, this.Name));
                    }
                }
                inds.RemoveAll(a => filteredInd.Contains(a));
            }

            foreach (var valueToSupply in valuesForCompanionModels)
            {
                int number = numberToDo;

                switch (valueToSupply.Key.type)
                {
                    case "LabourFeedGroup":
                        valuesForCompanionModels[valueToSupply.Key] = 0;
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
                                        valuesForCompanionModels[valueToSupply.Key] = amountToDo;
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

            if (amountToDo > 0)
            {
                //FeedTypeName includes the ResourceGroup name eg. AnimalFoodStore.FeedItemName
                string feedItemName = FeedTypeName.Split('.').Last();
                resourceRequest = new ResourceRequest()
                {
                    AllowTransmutation = true,
                    Required = amountToDo,
                    Resource = FeedType,
                    ResourceType = typeof(HumanFoodStore),
                    ResourceTypeName = feedItemName,
                    ActivityModel = this,
                    Category = TransactionCategory
                };
                return new List<ResourceRequest>()
                {
                    resourceRequest  
                };
            }
            return null;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            // check for shortfall in request to apply to feeding
            if (resourceRequest.Provided > 0)
            {
                double propFed = resourceRequest.Required / resourceRequest.Provided;

                // feed with any modification
                // walk througth the indfed list

                foreach (var item in indFed)
                {
                    item.Item1.AddIntake(new LabourDietComponent()
                    {
                        AmountConsumed = item.Item2 * propFed,
                        FoodStore = resourceRequest.Resource as HumanFoodStoreType
                    });
                };

                SetStatusSuccessOrPartial(propFed < 1);
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<LabourFeedGroup>(), true, "childgroupactivityborder", "The following groups will be fed:", "No LabourFeedGroup was provided"),
            };
        }


        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Feed people ");
            htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(FeedTypeName, "Feed type not set", HTMLSummaryStyle.Resource));
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        } 
        #endregion
    }
}
