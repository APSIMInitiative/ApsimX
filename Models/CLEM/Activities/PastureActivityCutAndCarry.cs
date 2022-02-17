using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to perform manual cut and carry from a pasture</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform cut and carry from a specified graze food store (i.e. native pasture paddock)")]
    [Version(1, 0, 1, "Included new ProportionOfAvailable option for moving pasture")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Pasture/CutAndCarry.htm")]
    public class PastureActivityCutAndCarry : CLEMRuminantActivityBase
    {
        [Link]
        private Clock clock = null;

        private GrazeFoodStoreType pasture;
        private AnimalFoodStoreType foodstore;
        private ActivityCutAndCarryLimiter limiter;

        /// <summary>
        /// Name of graze food store/paddock to cut and carry from
        /// </summary>
        [Description("Graze food store/paddock")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze food store where pasture is located required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        public string PaddockName { get; set; }

        /// <summary>
        /// Animal food store to receive pasture
        /// </summary>
        [Description("Animal food store to receive pasture")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Animal food store to receive pasture is required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore) } })]
        public string AnimalFoodStoreName { get; set; }

        /// <summary>
        /// Cut and carry amount type
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(RuminantFeedActivityTypes.SpecifiedDailyAmount)]
        [Description("Cut and carry amount type")]
        [Required]
        public RuminantFeedActivityTypes CutStyle { get; set; }

        /// <summary>
        /// Value to supply
        /// </summary>
        [Description("Daily value to supply")]
        [GreaterThanValue("0")]
        public double Supply { get; set; }

        /// <summary>
        /// Amount harvested this timestep after limiter accounted for
        /// </summary>
        [JsonIgnore]
        public double AmountHarvested { get; set; }

        /// <summary>
        /// Amount available for harvest from crop file
        /// </summary>
        [JsonIgnore]
        public double AmountAvailableForHarvest { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PastureActivityCutAndCarry()
        {
            TransactionCategory = "Pasture.Collect";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // activity is performed in CLEMDoCutAndCarry not CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;
            
            // get pasture
            pasture = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, PaddockName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            // get food store
            foodstore = Resources.FindResourceType<AnimalFoodStore, AnimalFoodStoreType>(this, AnimalFoodStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            // locate a cut and carry limiter associarted with this event.
            limiter = LocateCutAndCarryLimiter(this);

            switch (CutStyle)
            {
                case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                case RuminantFeedActivityTypes.ProportionOfWeight:
                case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                    InitialiseHerd(true, true);
                    break;
                default:
                    break;
            }
        }

        /// <summary>An event handler for a Cut and Carry</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDoCutAndCarry")]
        private void OnCLEMDoCutAndCarry(object sender, EventArgs e)
        {
            AmountHarvested = 0;
            AmountAvailableForHarvest = 0;

            if (this.TimingOK)
            {
                switch (CutStyle)
                {
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        AmountHarvested += pasture.Amount * Supply;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        AmountHarvested += Supply * 30.4;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        foreach (Ruminant ind in CurrentHerd(false))
                        {
                            AmountHarvested += Supply * ind.Weight * 30.4;
                        }
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        foreach (Ruminant ind in CurrentHerd(false))
                        {
                            AmountHarvested += Supply * ind.PotentialIntake;
                        }
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        foreach (Ruminant ind in CurrentHerd(false))
                        {
                            AmountHarvested += Supply * (ind.PotentialIntake - ind.Intake);
                        }
                        break;
                    default:
                        throw new Exception(String.Format("FeedActivityType {0} is not supported in {1}", CutStyle, this.Name));
                }

                AmountAvailableForHarvest = AmountHarvested;
                // reduce amount by limiter if present.
                if (limiter != null)
                {
                    double canBeCarried = limiter.GetAmountAvailable(clock.Today.Month);
                    AmountHarvested = Math.Max(AmountHarvested, canBeCarried);
                    limiter.AddWeightCarried(AmountHarvested);
                }
            }

            // get resources
            GetResourcesRequiredForActivity();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> requestList = null;
            if (AmountHarvested > 0)
            {
                requestList = new List<ResourceRequest>()
                {
                    new ResourceRequest()
                    {
                        ActivityModel = this,
                        AdditionalDetails = this,
                        Category = TransactionCategory,
                        Required = AmountHarvested,
                        Resource = pasture
                    }
                };
            }
            return requestList;
        }

        /// <inheritdoc/>
        public override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded;
            // TODO add labour multiplier if pasture below given amount and difficult to cut
            // as per IAT rules below 500kg/ha

            switch (requirement.UnitType)
            {
                case LabourUnitType.perKg:
                    daysNeeded = requirement.LabourPerUnit * AmountHarvested;
                    break;
                case LabourUnitType.perUnit:
                    double numberUnits = AmountHarvested / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = requirement.LabourPerUnit * numberUnits;
                    break;
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new LabourRequiredArgs(daysNeeded, TransactionCategory, pasture.NameWithParent);
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            // labour limiter
            var labourRequests = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour));
            if(labourRequests.Any())
            {
                double required = labourRequests.Sum(a => a.Required);
                double provided = labourRequests.Sum(a => a.Provided);
                double limiter = Math.Min(1.0, provided / required);

                // TODO add ability to turn off labour influence
                if(limiter<1)
                {
                    // find pasture and reduce required based on labour limit.
                    ResourceRequest pastureRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(GrazeFoodStoreType)).FirstOrDefault();
                    if (pastureRequest != null)
                    {
                        AmountHarvested *= limiter;
                        pastureRequest.Required = AmountHarvested;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            FoodResourcePacket packet = new FoodResourcePacket()
            {
                Amount = AmountHarvested,
                PercentN = pasture.Nitrogen,
                DMD = pasture.EstimateDMD(pasture.Nitrogen)
            };

            foodstore.Add(packet, this,"", TransactionCategory);
        }

        /// <summary>
        /// Method to locate a ActivityCutAndCarryLimiter
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private ActivityCutAndCarryLimiter LocateCutAndCarryLimiter(IModel model)
        {
            // search children
            ActivityCutAndCarryLimiter limiterFound = model.FindAllChildren<ActivityCutAndCarryLimiter>().Cast<ActivityCutAndCarryLimiter>().FirstOrDefault();
            if (limiterFound == null)
            {
                if (model.Parent.GetType().IsSubclassOf(typeof(CLEMActivityBase)) || model.Parent.GetType() == typeof(ActivitiesHolder))
                {
                    limiterFound = LocateCutAndCarryLimiter(model.Parent);
                }
            }
            return limiterFound;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Cut ");
                switch (CutStyle)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        htmlWriter.Write("<span class=\"setvalue\">" + Supply.ToString("#,##0.##") + "</span> kg ");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        htmlWriter.Write("<span class=\"setvalue\">" + Supply.ToString("#0.##%") + "</span> of herd <span class=\"setvalue\">live weight</span> ");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        htmlWriter.Write("<span class=\"setvalue\">" + Supply.ToString("#0.##%") + "</span> of herd <span class=\"setvalue\">potential intake</span> ");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        htmlWriter.Write("<span class=\"setvalue\">" + Supply.ToString("#0.##%") + "</span> of herd <span class=\"setvalue\">remaining intake required</span> ");
                        break;
                    default:
                        break;
                }

                htmlWriter.Write("from ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(PaddockName, "Pasture not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write(" and carry to ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(AnimalFoodStoreName, "Fodd store not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
