using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Labour (Human) feed activity</summary>
    /// <summary>This activity provides food to specified people based on a feeding style</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs human feeding based upon the current labour filtering and a feeding style.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeed.htm")]
    public class LabourActivityFeed : CLEMActivityBase
    {
        private double feedRequired = 0;

        /// <summary>
        /// Name of Human Food to use (with Resource Group name appended to the front [separated with a '.'])
        /// </summary>
        [Description("Food to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] {typeof(HumanFoodStore)} )]
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
        [XmlIgnore]
        public HumanFoodStoreType FeedType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeed()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate FeedType resource
            FeedType = Resources.GetResourceItem(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as HumanFoodStoreType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            feedRequired = 0;

            // get list from filters
            foreach (Model child in Apsim.Children(this, typeof(LabourFeedGroup)))
            {
                double value = (child as LabourFeedGroup).Value;

                foreach (LabourType ind in Resources.Labour().Items.Filter(child))
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
                        ResourceType = typeof(HumanFoodStore),
                        ResourceTypeName = feedItemName,
                        ActivityModel = this,
                        Reason = "Consumption"
                    }
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<LabourType> group = Resources.Labour().Items.Where(a => a.Hired != true).ToList();
            int head = 0;
            double adultEquivalents = 0;
            foreach (Model child in Apsim.Children(this, typeof(LabourFeedGroup)))
            {
                var subgroup = group.Filter(child).ToList();
                head += subgroup.Count();
                adultEquivalents += subgroup.Sum(a => a.AdultEquivalent);
            }

            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perKg:
                    daysNeeded = feedRequired * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perUnit:
                    numberUnits = feedRequired / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            //add limit to amout collected based on labour shortfall
            double labourLimit = this.LabourLimitProportion;
            foreach (ResourceRequest item in ResourceRequestList)
            {
                if (item.ResourceType != typeof(LabourType))
                {
                    item.Required *= labourLimit;
                }
            }
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            List<LabourType> group = Resources.Labour().Items.Where(a => a.Hired != true).ToList();
            if (group != null && group.Count > 0)
            {
                // calculate feed limit
                double feedLimit = 0.0;

                ResourceRequest feedRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(HumanFoodStore)).FirstOrDefault();
                if (feedRequest != null)
                {
                    feedLimit = Math.Min(1.0, feedRequest.Provided / feedRequest.Required);
                }

                if (feedRequest == null || (feedRequest.Required == 0 | feedRequest.Available == 0))
                {
                    Status = ActivityStatus.NotNeeded;
                    return;
                }

                foreach (Model child in Apsim.Children(this, typeof(LabourFeedGroup)))
                {
                    double value = (child as LabourFeedGroup).Value;

                    foreach (LabourType ind in Resources.Labour().Items.Filter(child))
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
                SetStatusSuccess();
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Feed people ";
            if (FeedTypeName == null || FeedTypeName == "")
            {
                html += "<span class=\"errorlink\">[Feed TYPE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + FeedTypeName + "</span>";
            }
            html += "</div>";

            return html;
        }
    }
}
