using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Manage crop product activity</summary>
    /// <summary>This activity sets aside land for the crop</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageCrop))]
    [ValidParent(ParentType = typeof(CropActivityManageProduct))]
    [Description("This activity is used within a crop management activity to obtain production values from the crop file.")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Mixed cropping/multiple products implemented")]
    [Version(1, 0, 3, "Added ability to model multiple harvests from crop using Harvest Tags from input file")]
    [HelpUri(@"Content/Features/Activities/Crop/ManageCropProduct.htm")]
    public class CropActivityManageProduct: CLEMActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        [Link]
        Simulation Simulation = null;

        /// <summary>
        /// Name of the model for the crop input file
        /// </summary>
        [Description("Crop file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of crop file required")]
        [Models.Core.Display(Type = DisplayType.CLEMCropFileReader)]
        public string ModelNameFileCrop { get; set; }

        /// <summary>
        /// Name of crop in file
        /// </summary>
        [Description("Name of crop in file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of crop in file required")]
        public string CropName { get; set; }

        /// <summary>
        /// Store to put crop growth into
        /// </summary>
        [Description("Store for crop product")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(GrazeFoodStore), typeof(HumanFoodStore), typeof(ProductStore) })]
        [Required]
        public string StoreItemName { get; set; }

        /// <summary>
        /// Percentage of the crop growth that is kept
        /// </summary>
        [Description("Proportion of product kept")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Proportion]
        public double ProportionKept { get; set; }

        /// <summary>
        /// Number of Trees per Hectare 
        /// </summary>
        [Description("Number of Trees (perHa) [0 if not a tree crop]")]
        [Required]
        public double TreesPerHa { get; set; }

        /// <summary>
        /// Is this a tree crop.
        /// </summary>
        [JsonIgnore]
        public bool IsTreeCrop;

        /// <summary>
        /// resource item
        /// </summary>
        [JsonIgnore]
        public IResourceType LinkedResourceItem { get; set; }

        /// <summary>
        /// Harvest Data retrieved from the Forage File.
        /// </summary>
        [JsonIgnore]
        public List<CropDataType> HarvestData { get; set; }

        /// <summary>
        /// Stores the start of current crop harvest sequence
        /// Using first harvest if HarvestType provided
        /// </summary>
        [JsonIgnore]
        public CropDataType StartCurrentSequenceHarvest { get; set; }

        /// <summary>
        /// Stores the end of current crop harvest sequence
        /// Using last harvest if HarvestType provided
        /// </summary>
        [JsonIgnore]
        public CropDataType EndCurrentSequenceHarvest { get; set; }

        /// <summary>
        /// Stores the next harvest details
        /// </summary>
        [JsonIgnore]
        public CropDataType NextHarvest { get; set; }

        /// <summary>
        /// Stores the previous harvest details
        /// Using last harvest if HarvestType provided
        /// </summary>
        [JsonIgnore]
        public CropDataType PreviousHarvest { get; set; }

        /// <summary>
        /// Model for the crop input file
        /// </summary>
        private IFileCrop fileCrop;

        /// <summary>
        /// Parent of this Model that gets the land for growing this crop.
        /// </summary>
        private CropActivityManageCrop parentManagementActivity;

        /// <summary>
        /// Units to Hectares converter from Land type
        /// </summary>
        public double UnitsToHaConverter { get; set; }

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
        /// Flag for first timestep in a rotation for checks
        /// </summary>
        [JsonIgnore]
        public int FirstTimeStepOfRotation { get; set; }

        /// <summary>
        /// Flag for determining if flagged harvest types have been provided
        /// </summary>
        [JsonIgnore]
        public bool HarvestTagsUsed { get; set; }

        /// <summary>
        /// Flag for determining if flagged harvest types have been provided
        /// </summary>
        [JsonIgnore]
        public bool InsideMultiHarvestSequence { get; set; }

        private ActivityCutAndCarryLimiter limiter;
        private string addReason = "Harvest";
        private bool performedHarvest = false;
        private string previousTag = "";

        /// <summary>
        /// Constructor
        /// </summary>
        public CropActivityManageProduct()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            InsideMultiHarvestSequence = false;

            // activity is performed in CLEMDoCutAndCarry not CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            fileCrop = Simulation.FindAllDescendants().Where(a => a.Name == ModelNameFileCrop).FirstOrDefault() as IFileCrop;
            if (fileCrop == null)
            {
                throw new ApsimXException(this, String.Format("Unable to locate model for crop input file [x={0}] referred to in [a={1}]", this.ModelNameFileCrop??"Unknown", this.Name));
            }

            LinkedResourceItem = Resources.GetResourceItem(this, StoreItemName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
            if((LinkedResourceItem as Model).Parent.GetType() == typeof(GrazeFoodStore))
            {
                (LinkedResourceItem as GrazeFoodStoreType).Manager = (Parent as IPastureManager);
                addReason = "Growth";
            }

            // look up tree until we find a parent to allow nested crop products for rotate vs mixed cropping/products
            parentManagementActivity = FindAncestor<CropActivityManageCrop>();

            // Retrieve harvest data from the forage file for the entire run. 
            // only get entries where a harvest happened (Amtkg > 0)
            HarvestData = fileCrop.GetCropDataForEntireRun(parentManagementActivity.LinkedLandItem.SoilType, CropName,
                                                               Clock.StartDate, Clock.EndDate).Where(a => a.AmtKg > 0).OrderBy(a => a.Year * 100 + a.Month).ToList<CropDataType>();
            if ((HarvestData == null) || (HarvestData.Count == 0))
            {
                Summary.WriteWarning(this, $"Unable to locate any harvest data in [x={fileCrop.Name}] using [x={fileCrop.FileName}] for soil type [{parentManagementActivity.LinkedLandItem.SoilType}] and crop name [{CropName}] between the dates [{Clock.StartDate.ToShortDateString()}] and [{Clock.EndDate.ToShortDateString()}]");
            }

            IsTreeCrop = (TreesPerHa == 0) ? false : true;  //using this boolean just makes things more readable.

            UnitsToHaConverter = (parentManagementActivity.LinkedLandItem.Parent as Land).UnitsOfAreaToHaConversion;

            // locate a cut and carry limiter associated with this event.
            limiter = LocateCutAndCarryLimiter(this);

            // check if harvest type tags have been provided
            HarvestTagsUsed = HarvestData.Where(a => a.HarvestType != "").Count() > 0;

            // set manager of graze food store if linked
        }

        /// <summary>
        /// Function to get the next harvest date from data
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // if harvest tags provided for this crop then they will be used to define previous, next etc

            // while date month > harvest record look for previous and delete past events

            if (HarvestData.Count() > 0)
            {
                int clockYrMth = CalculateYearMonth(Clock.Today);
                int position; // passed -1, current 0, future 1
                do
                {
                    int harvestYrMth = CalculateYearMonth(HarvestData.First().HarvestDate);
                    position = (clockYrMth > harvestYrMth) ? -1 : ((clockYrMth == harvestYrMth) ? 0 : 1);

                    // check for valid sequence
                    if(HarvestTagsUsed && HarvestData.FirstOrDefault().HarvestType != "")
                    {
                        if (previousTag == HarvestData.FirstOrDefault().HarvestType)
                        {
                            string warn = $"Invalid sequence of HarvetTags detected in [a={this.Name}]\r\nEnsure tags are ordered first, last in sequence.";
                            if (!Warnings.Exists(warn))
                            {
                                Summary.WriteWarning(this, warn);
                                Warnings.Add(warn);
                            }
                        }
                        previousTag = HarvestData.FirstOrDefault().HarvestType;
                    }


                    switch (position)
                    {
                        case -1:
                            if (HarvestTagsUsed)
                            {
                                switch (HarvestData.FirstOrDefault().HarvestType)
                                {
                                    case "first":
                                        if (!performedHarvest)
                                        {
                                            InsideMultiHarvestSequence = true;
                                            StartCurrentSequenceHarvest = HarvestData.FirstOrDefault();
                                            EndCurrentSequenceHarvest = HarvestData.Where(a => a.HarvestType == "last").FirstOrDefault(); 
                                        }
                                        break;
                                    case "last":
                                        // hit tagged last to delete as we've passed this date so out of multi harvest sequence
                                        InsideMultiHarvestSequence = false;
                                        PreviousHarvest = HarvestData.FirstOrDefault();
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                PreviousHarvest = HarvestData.FirstOrDefault();
                            }
                            HarvestData.RemoveAt(0);
                            break;
                        case 0:
                            performedHarvest = true;
                            if (HarvestTagsUsed)
                            {
                                switch (HarvestData.FirstOrDefault().HarvestType)
                                {
                                    case "first":
                                        // hit tagged first for current time step
                                        InsideMultiHarvestSequence = true;
                                        StartCurrentSequenceHarvest = HarvestData.FirstOrDefault();
                                        PreviousHarvest = null;
                                        EndCurrentSequenceHarvest = HarvestData.Where(a => a.HarvestType == "last").FirstOrDefault();
                                        break;
                                    default:
                                        NextHarvest = HarvestData.FirstOrDefault();
                                        PreviousHarvest = null;
                                        break;
                                }
                            }
                            else
                            {
                                NextHarvest = HarvestData.FirstOrDefault();
                                PreviousHarvest = null;
                            }
                            break;
                        case 1:
                            if (HarvestTagsUsed)
                            {
                                switch (HarvestData.FirstOrDefault().HarvestType)
                                {
                                    case "first":
                                        // hit tagged first for next harvest
                                        NextHarvest = HarvestData.FirstOrDefault();
                                        break;
                                    default:
                                        NextHarvest = HarvestData.FirstOrDefault();
                                        break;
                                }

                            }
                            else
                            {
                                NextHarvest = HarvestData.FirstOrDefault();
                            }
                            break;
                        default:
                            break;
                    }
                } while (HarvestData.Count > 0 && position == -1);
            }
        }

        /// <summary>
        /// Method to calulate the year month integer for this date
        /// </summary>
        /// <param name="date">date to process</param>
        /// <returns>year month integer</returns>
        public int CalculateYearMonth(DateTime date)
        {
            return date.Year * 100 + date.Month;
        }

        /// <summary>
        /// Function to get the next harvest date from data
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void OnCLEMEndOfTimeStep(object sender, EventArgs e)
        {
            // rotate harvest if needed
            if ((this.ActivityEnabled & Status != ActivityStatus.Ignored) && HarvestData.Count() > 0 && Clock.Today.Year * 100 + Clock.Today.Month == HarvestData.First().Year * 100 + HarvestData.First().Month)
            {
                // don't rotate if no harvest tags or harvest type is not equal to "last"
                if (!HarvestTagsUsed || NextHarvest.HarvestType == "last")
                {
                    // don't rotate activities that may have just had their enabled status changed in this timestep
                    //if(this.ActivityEnabled & Status != ActivityStatus.Ignored)
                    //{
                        parentManagementActivity.RotateCrop();
                    //}
                }
            }
        }

        /// <summary>An event handler to allow us to get next supply of pasture</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMUpdatePasture")]
        private void OnCLEMUpdatePasture(object sender, EventArgs e)
        {
            if(parentManagementActivity.ActivityEnabled && LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType))
            {
                if (this.TimingOK)
                {
                    Status = ActivityStatus.NotNeeded;
                    DoActivity();
                }
            }
        }

        /// <summary>An event handler for a Cut and Carry</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDoCutAndCarry")]
        private void OnCLEMDoCutAndCarry(object sender, EventArgs e)
        {
            // get resources needed such as labour before DoActivity
            // when not going to a GrazeFoodStore that uses CLEMUpdatePasture
            if (parentManagementActivity.ActivityEnabled && LinkedResourceItem.GetType() != typeof(GrazeFoodStoreType))
            {
                GetResourcesRequiredForActivity();
            }
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
                if(model.Parent.GetType().IsSubclassOf(typeof(CLEMActivityBase)) || model.Parent.GetType() == typeof(ActivitiesHolder))
                {
                    limiterFound = LocateCutAndCarryLimiter(model.Parent);
                }
            }
            return limiterFound;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>A list of resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            if (this.TimingOK)
            {
                int year = Clock.Today.Year;
                int month = Clock.Today.Month;
                double amount = 0;
                if (NextHarvest != null)
                {
                    //if this month is a harvest month for this crop
                    if ((year == NextHarvest.HarvestDate.Year) && (month == NextHarvest.HarvestDate.Month))
                    {
                        if (this.TimingOK)
                        {
                            if (IsTreeCrop)
                            {
                                amount = NextHarvest.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                            }
                            else
                            {
                                amount = NextHarvest.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                            }

                            if (limiter != null)
                            {
                                double canBeCarried = limiter.GetAmountAvailable(Clock.Today.Month);
                                amount = Math.Max(amount, canBeCarried);
                            }
                        }
                    }
                }

                double numberUnits;
                switch (requirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perKg:
                        daysNeeded = amount * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perUnit:
                        numberUnits = amount / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                        {
                            numberUnits = Math.Ceiling(numberUnits);
                        }

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perUnitOfLand:
                        numberUnits = parentManagementActivity.Area / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                        {
                            numberUnits = Math.Ceiling(numberUnits);
                        }
                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perHa:
                        numberUnits = parentManagementActivity.Area * UnitsToHaConverter / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                        {
                            numberUnits = Math.Ceiling(numberUnits);
                        }
                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
                }

                if (amount <= 0)
                {
                    daysNeeded = 0;
                }
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Harvest", (LinkedResourceItem as CLEMModel).NameWithParent);
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            int year = Clock.Today.Year;
            int month = Clock.Today.Month;
            AmountHarvested = 0;
            AmountAvailableForHarvest = 0;

            if (this.TimingOK && NextHarvest != null)
            {
                //if this month is a harvest month for this crop
                if ((year == NextHarvest.HarvestDate.Year) && (month == NextHarvest.HarvestDate.Month))
                {
                    if (IsTreeCrop)
                    {
                        AmountHarvested = NextHarvest.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                    }
                    else
                    {
                        AmountHarvested = NextHarvest.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                    }

                    AmountAvailableForHarvest = AmountHarvested;
                    // reduce amount by limiter if present.
                    if (limiter != null)
                    {
                        double canBeCarried = limiter.GetAmountAvailable(Clock.Today.Month);
                        AmountHarvested = Math.Max(AmountHarvested, canBeCarried);

                        // now modify by labour limits as this is the amount labour was calculated for.
                        double labourLimit = this.LabourLimitProportion;
                        AmountHarvested *= labourLimit;

                        if(labourLimit < 1)
                        {
                            this.Status = ActivityStatus.Partial;
                        }

                        // now limit further by fees not paid
                        double financeLimit = this.LimitProportion(typeof(Finance));

                        limiter.AddWeightCarried(AmountHarvested);
                    }

                    if (AmountHarvested > 0)
                    {
                        double percentN = 0;
                        // if no nitrogen provided form file
                        if (double.IsNaN(NextHarvest.Npct))
                        {
                            if (LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType))
                            {
                                // grazed pasture with no N read assumes the green biomass N content
                                percentN = (LinkedResourceItem as GrazeFoodStoreType).GreenNitrogen;
                            }
                        }
                        else
                        {
                            percentN =  NextHarvest.Npct;
                        }

                        if (percentN == 0)
                        {
                            //Add without adding any new nitrogen.
                            //The nitrogen value for this feed item in the store remains the same.
                            LinkedResourceItem.Add(AmountHarvested, this,"", addReason);
                        }
                        else
                        {
                            FoodResourcePacket packet = new FoodResourcePacket()
                            {
                                Amount = AmountHarvested,
                                PercentN = percentN
                            };
                            if (LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType))
                            {
                                packet.DMD = (LinkedResourceItem as GrazeFoodStoreType).EstimateDMD(packet.PercentN);
                            }
                            LinkedResourceItem.Add(packet, this,"", addReason);
                        }
                        SetStatusSuccess();
                    }
                    else
                    {
                        if (Status == ActivityStatus.Success)
                        {
                            Status = ActivityStatus.NotNeeded;
                        }
                    }
                }
                else
                {
                    this.Status = ActivityStatus.NotNeeded;
                }
            }
            else
            {
                this.Status = ActivityStatus.NotNeeded;
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
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (this.Parent.GetType() != typeof(CropActivityManageCrop) && this.Parent.GetType() != typeof(CropActivityManageProduct))
            {
                string[] memberNames = new string[] { "Parent model" };
                results.Add(new ValidationResult("A crop activity manage product must be placed immediately below a CropActivityManageCrop model component", memberNames));
            }

            // check that parent or grandparent is a CropActivityManageCrop to ensure correct nesting
            if (!((this.Parent.GetType() == typeof(CropActivityManageCrop) || (this.Parent.GetType() == typeof(CropActivityManageProduct) && this.Parent.Parent.GetType() == typeof(CropActivityManageCrop)))))
            {
                string[] memberNames = new string[] { "Invalid nesting" };
                results.Add(new ValidationResult("A crop activity manage product must be placed immediately below a CropActivityManageCrop model component (see rotational cropping) or below the CropActivityManageProduct immediately below the CropActivityManageCrop (see mixed cropping)", memberNames));
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (TreesPerHa > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">This is a tree crop with a density of " + TreesPerHa.ToString() + " per hectare</div>");
                }
                if (ProportionKept == 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\"><span class=\"errorlink\">" + ProportionKept.ToString("0.#%") + "</span> of this product is placed in ");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">" + ((ProportionKept == 1) ? "This " : "<span class=\"setvalue\">" + (ProportionKept).ToString("0.#%") + "</span> of this ") + "product is placed in ");
                }
                if (StoreItemName == null || StoreItemName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[STORE NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + StoreItemName + "</span>");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">Data is retrieved from ");
                if (ModelNameFileCrop == null || ModelNameFileCrop == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[CROP FILE NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"filelink\">" + ModelNameFileCrop + "</span>");
                }
                htmlWriter.Write(" using crop named ");
                if (CropName == null || CropName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[CROP NAME NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"filelink\">" + CropName + "</span>");
                }
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return base.ModelSummaryClosingTags(formatForParentControl); 
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string html = "";
            // if first child of mixed 
            if (this.Parent.GetType() == typeof(CropActivityManageProduct))
            {
                if (this.Parent.FindAllChildren<CropActivityManageProduct>().FirstOrDefault().Name == this.Name)
                {
                    // close off the parent item so it displays
                    html += "\r\n</div>";
                }
            }

            bool mixed = this.FindAllChildren<CropActivityManageProduct>().Count() >= 1;
            if (mixed)
            {
                html += "\r\n<div class=\"cropmixedlabel\">Mixed crop</div>";
                html += "\r\n<div class=\"cropmixedborder\">";
            }
            html += base.ModelSummaryOpeningTags(formatForParentControl);
            return html;
        } 
        #endregion
    }
}
