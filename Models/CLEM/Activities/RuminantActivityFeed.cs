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
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to specified ruminants based on a feeding style</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs ruminant feeding based upon the current herd filtering and a feeding style.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFeed.htm")]
    public class RuminantActivityFeed : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Name of Feed to use (with Resource Group name appended to the front [separated with a '.'])
        /// eg. AnimalFoodStore.RiceStraw
        /// </summary>
        [Description("Feed to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] {typeof(AnimalFoodStore), typeof(HumanFoodStore)} )]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Feed type required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Proportion wastage through trampling (feed trough = 0)
        /// </summary>
        [Description("Proportion wastage through trampling (feed trough = 0)")]
        [Required, Proportion]
        public double ProportionTramplingWastage { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
        public IFeedType FeedType { get; set; }

        private double feedRequired = 0;

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(RuminantFeedActivityTypes.SpecifiedDailyAmount)]
        [Description("Feeding style to use")]
        [Required]
        public RuminantFeedActivityTypes FeedStyle { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityFeed()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Apsim.Children(this, typeof(RuminantFeedGroup)).Count + Apsim.Children(this, typeof(RuminantFeedGroupMonthly)).Count == 0)
            {
                string[] memberNames = new string[] { "Ruminant feed group" };
                results.Add(new ValidationResult("At least one [f=RuminantFeedGroup] or [f=RuminantFeedGroupMonthly] filter group must be present", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);

            // locate FeedType resource
            FeedType = Resources.GetResourceItem(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<Ruminant> herd = CurrentHerd(false);

            // get zero limited month from clock
            int month = Clock.Today.Month - 1;

            feedRequired = 0;

            // get list from filters
            foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
            {
                double value = 0;
                if(child is RuminantFeedGroup)
                {
                    value = (child as RuminantFeedGroup).Value;
                }
                else
                {
                    value = (child as RuminantFeedGroupMonthly).MonthlyValues[month];
                }

                if (FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmount)
                {
                    feedRequired += value * 30.4;
                }
                else
                {
                    foreach (Ruminant ind in herd.Filter(child))
                    {
                        switch (FeedStyle)
                        {
                            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                                feedRequired += value * 30.4;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfWeight:
                                feedRequired += value * ind.Weight * 30.4;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                                feedRequired += value * ind.PotentialIntake;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                                feedRequired += value * (ind.PotentialIntake - ind.Intake);
                                break;
                            default:
                                throw new Exception(String.Format("FeedStyle {0} is not supported in {1}", FeedStyle, this.Name));
                        }
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
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = feedItemName,
                        ActivityModel = this
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
            List<Ruminant> herd = CurrentHerd(false);
            int head = 0;
            double adultEquivalents = 0;
            foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
            {
                var subherd = herd.Filter(child).ToList();
                head += subherd.Count();
                adultEquivalents += subherd.Sum(a => a.AdultEquivalent);
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
                if(item.ResourceType != typeof(LabourType))
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
            List<Ruminant> herd = CurrentHerd(false);
            if (herd != null && herd.Count > 0)
            {
                // calculate feed limit
                double feedLimit = 0.0;
                double wastage = 1.0 - this.ProportionTramplingWastage;
                double dailyAmountShortfall = 1.0;

                ResourceRequest feedRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(AnimalFoodStore)).FirstOrDefault();
                FoodResourcePacket details = new FoodResourcePacket();
                if (feedRequest != null)
                {
                    details = feedRequest.AdditionalDetails as FoodResourcePacket;
                    feedLimit = Math.Min(1.0, feedRequest.Provided / feedRequest.Required);
                }

                // feed animals
                int month = Clock.Today.Month - 1;

                if(feedRequest == null || (feedRequest.Required == 0 | feedRequest.Available == 0))
                {
                    Status = ActivityStatus.NotNeeded;
                    return;
                }

                // if feed style is fixed daily amount compare amount received against herd requirement.
                // this produces a reduction from potential intake for each individual.
                if (FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmount)
                {
                    double herdRequirement = 0;
                    foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
                    {
                        herdRequirement += herd.Filter(child).Sum(a => a.PotentialIntake - a.Intake);
                    }
                    dailyAmountShortfall = Math.Min(1.0, (feedRequest.Provided*wastage) / herdRequirement);
                }

                // get list from filters
                foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
                {
                    double value = 0;
                    if (child is RuminantFeedGroup)
                    {
                        value = (child as RuminantFeedGroup).Value;
                    }
                    else
                    {
                        value = (child as RuminantFeedGroupMonthly).MonthlyValues[month];
                    }

                    foreach (Ruminant ind in herd.Filter(child))
                    {
                        switch (FeedStyle)
                        {
                            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                                details.Amount = (ind.PotentialIntake - ind.Intake);
                                details.Amount *= dailyAmountShortfall;
                                ind.AddIntake(details);
                                break;
                            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                                details.Amount = value * 30.4; // * ind.Number;
                                details.Amount *= feedLimit * wastage;
                                ind.AddIntake(details);
                                break;
                            case RuminantFeedActivityTypes.ProportionOfWeight:
                                details.Amount = value * ind.Weight * 30.4; // * ind.Number;
                                details.Amount *= feedLimit * wastage;
                                ind.AddIntake(details);
                                break;
                            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                                details.Amount = value * ind.PotentialIntake; // * ind.Number;
                                details.Amount *= feedLimit * wastage;
                                ind.AddIntake(details);
                                break;
                            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                                details.Amount = value * (ind.PotentialIntake - ind.Intake); // * ind.Number;
                                details.Amount *= feedLimit * wastage;
                                ind.AddIntake(details);
                                break;
                            default:
                                throw new Exception("Feed style used [" + FeedStyle + "] not implemented in [" + this.Name + "]");
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
            html += "\n<div class=\"activityentry\">Feed ruminants ";

            if (FeedTypeName == null || FeedTypeName == "")
            {
                html += "<span class=\"errorlink\">[Feed TYPE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + FeedTypeName + "</span>";
            }
            html += "</div>";

            if(ProportionTramplingWastage>0)
            {
                html += "\n<div class=\"activityentry\"> <span class=\"setvalue\">" + (ProportionTramplingWastage).ToString("0.##%")+"</span> is lost through trampling</div>";
            }
            return html;
        }
    }
}
