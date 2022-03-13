using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
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
    public class LabourActivityFeed : CLEMActivityBase, ICanHandleIdentifiableChildModels
    {
        private double feedRequired = 0;
        private Labour labour;

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
        public HumanFoodStoreType FeedType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeed()
        {
            this.SetDefaults();
            TransactionCategory = "Labour.Feed";
        }




        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate FeedType resource
            FeedType = Resources.FindResourceType<HumanFoodStore, HumanFoodStoreType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            // locate labour resource
            labour = Resources.FindResourceGroup<Labour>();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            feedRequired = 0;

            // get list from filters
            foreach (LabourFeedGroup child in FindAllChildren<LabourFeedGroup>())
            {
                double value = child.Value;
                
                foreach (LabourType ind in child.Filter(labour?.Items))
                {
                    // feed limited to the daily intake per ae set in HumanFoodStoreType
                    switch (FeedStyle)
                    {
                        case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                            feedRequired += value * 30.4;
                            break;
                        case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                            feedRequired += value * ind.AdultEquivalent * 30.4;
                            break;
                        default:
                            throw new Exception(String.Format("FeedStyle {0} is not supported in {1}", FeedStyle, this.Name));
                    }
                }
            }

            if (feedRequired > 0)
            {
                //FeedTypeName includes the ResourceGroup name eg. AnimalFoodStore.FeedItemName
                string feedItemName = FeedTypeName.Split('.').Last();
                return new List<ResourceRequest>()
                {
                    new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = feedRequired,
                        Resource = FeedType,
                        ResourceType = typeof(HumanFoodStore),
                        ResourceTypeName = feedItemName,
                        ActivityModel = this,
                        Category = TransactionCategory
                    }
                };
            }
            else
                return null;
        }

        ///// <inheritdoc/>
        //protected override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        //{
        //    IEnumerable<LabourType> labourers = labour?.Items.Where(a => a.Hired != true);
        //    int head = 0;
        //    double adultEquivalents = 0;
        //    foreach (var group in FindAllChildren<LabourFeedGroup>())
        //    {
        //        var subgroup = group.Filter(labourers);
        //        head += subgroup.Count();
        //        adultEquivalents += subgroup.Sum(a => a.AdultEquivalent);
        //    }

        //    double daysNeeded = 0;
        //    double numberUnits = 0;
        //    switch (requirement.UnitType)
        //    {
        //        case LabourUnitType.Fixed:
        //            daysNeeded = requirement.LabourPerUnit;
        //            break;
        //        case LabourUnitType.perHead:
        //            numberUnits = head / requirement.UnitSize;
        //            if (requirement.WholeUnitBlocks)
        //                numberUnits = Math.Ceiling(numberUnits);

        //            daysNeeded = numberUnits * requirement.LabourPerUnit;
        //            break;
        //        case LabourUnitType.perAE:
        //            numberUnits = adultEquivalents / requirement.UnitSize;
        //            if (requirement.WholeUnitBlocks)
        //                numberUnits = Math.Ceiling(numberUnits);

        //            daysNeeded = numberUnits * requirement.LabourPerUnit;
        //            break;
        //        case LabourUnitType.perKg:
        //            daysNeeded = feedRequired * requirement.LabourPerUnit;
        //            break;
        //        case LabourUnitType.perUnit:
        //            numberUnits = feedRequired / requirement.UnitSize;
        //            if (requirement.WholeUnitBlocks)
        //                numberUnits = Math.Ceiling(numberUnits);

        //            daysNeeded = numberUnits * requirement.LabourPerUnit;
        //            break;
        //        default:
        //            throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
        //    }
        //    return new LabourRequiredArgs(daysNeeded, TransactionCategory, null);
        //}

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            //add limit to amout collected based on labour shortfall
            double labourLimit = this.LabourLimitProportion;
            foreach (ResourceRequest item in ResourceRequestList)
            {
                if (item.ResourceType != typeof(LabourType))
                    item.Required *= labourLimit;
            }
            return;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            IEnumerable<LabourType> group = labour?.Items.Where(a => a.Hired != true);
            if (group != null && group.Any())
            {
                // calculate feed limit
                double feedLimit = 0.0;

                ResourceRequest feedRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(HumanFoodStore)).FirstOrDefault();
                if (feedRequest != null)
                    feedLimit = Math.Min(1.0, feedRequest.Provided / feedRequest.Required);

                if (feedRequest == null || (feedRequest.Required == 0 | feedRequest.Available == 0))
                {
                    Status = ActivityStatus.NotNeeded;
                    return;
                }

                foreach (LabourFeedGroup child in this.FindAllChildren<LabourFeedGroup>())
                {
                    double value = child.Value;

                    foreach (LabourType ind in child.Filter(labour?.Items))
                    {
                        switch (FeedStyle)
                        {
                            case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                                feedRequest.Provided = value * 30.4;
                                feedRequest.Provided *= feedLimit;
                                feedRequest.Provided *= (feedRequest.Resource as HumanFoodStoreType).EdibleProportion;
                                ind.AddIntake(new LabourDietComponent()
                                {
                                    AmountConsumed = feedRequest.Provided,
                                    FoodStore = feedRequest.Resource as HumanFoodStoreType
                                }
                                );
                                break;
                            case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                                feedRequest.Provided = value * ind.AdultEquivalent * 30.4;
                                feedRequest.Provided *= feedLimit;
                                feedRequest.Provided *= (feedRequest.Resource as HumanFoodStoreType).EdibleProportion;
                                ind.AddIntake(new LabourDietComponent()
                                {
                                    AmountConsumed = feedRequest.Provided,
                                    FoodStore = feedRequest.Resource as HumanFoodStoreType
                                }
                                );
                                break;
                            default:
                                throw new Exception(String.Format("FeedStyle {0} is not supported in {1}", FeedStyle, this.Name));
                        }
                    }
                }
                SetStatusSuccessOrPartial();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Feed people ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(FeedTypeName, "Feed type not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
