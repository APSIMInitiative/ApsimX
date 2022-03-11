using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>The child of Manage crop to manage a particular product harvested</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityManageCrop))]
    [ValidParent(ParentType = typeof(CropActivityManageProduct))]
    [Description("Manage a crop product of the parent ManageCrop and obtain production values from the crop file")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Mixed cropping/multiple products implemented")]
    [Version(1, 0, 3, "Added ability to model multiple harvests from crop using Harvest Tags from input file")]
    [HelpUri(@"Content/Features/Activities/Crop/ManageCropProduct.htm")]
    public class CropActivityManageProduct: CLEMActivityBase, IValidatableObject, ICanHandleIdentifiableChildModels
    {
        [Link]
        private Clock clock = null;
        [Link]
        private Simulation simulation = null;

        private ActivityCutAndCarryLimiter limiter;
        private string addReason = "Harvest";
        private bool performedHarvest = false;
        private string previousTag = "";
        private double amountToDo;
        private double amountToSkip;

        /// <summary>
        /// Name of the model for the crop input file
        /// </summary>
        [Description("Crop file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of crop file required")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetNameOfModelsByType", ValuesArgs = new object[] { new Type[] { typeof(FileCrop), typeof(FileSQLiteCrop) } })]
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
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(AnimalFoodStore), typeof(GrazeFoodStore), typeof(HumanFoodStore), typeof(ProductStore) } })]
        [Required]
        public string StoreItemName { get; set; }

        /// <summary>
        /// Proportion of the crop harvest that is available
        /// </summary>
        [Description("Harvest achieved multiplier")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required]
        public double ProportionKept { get; set; }

        /// <summary>
        /// Proportion of the crop area (of parent ManageCrop) used
        /// </summary>
        [Description("Crop area multiplier")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Proportion]
        public double PlantedMultiplier { get; set; }

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
        [JsonIgnore]
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

        /// <summary>
        /// Constructor
        /// </summary>
        public CropActivityManageProduct()
        {
            this.SetDefaults();
            TransactionCategory = "Crop.[Product]";
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels(string type)
        {
            switch (type)
            {
                case "CropActivityFee":
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {

                        },
                        units: new List<string>() {
                            "fixed",
                            "per kg harvested",
                            "per ha",
                            "per ha harvested",
                        }
                        );
                default:
                    return new LabelsForIdentifiableChildren();
            }
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

            fileCrop = simulation.FindAllDescendants().Where(a => a.Name == ModelNameFileCrop).FirstOrDefault() as IFileCrop;
            if (fileCrop == null)
                throw new ApsimXException(this, $"Unable to locate crop data reader [x={this.ModelNameFileCrop ?? "Unknown"}] requested by [a={this.NameWithParent}]");

            LinkedResourceItem = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, StoreItemName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            if((LinkedResourceItem as Model).Parent is GrazeFoodStore)
            {
                // set manager of graze food store if linked
                (LinkedResourceItem as GrazeFoodStoreType).Manager = (Parent as IPastureManager);
                addReason = "Growth";
            }

            // look up tree until we find a parent to allow nested crop products for rotate vs mixed cropping/products
            parentManagementActivity = FindAncestor<CropActivityManageCrop>();

            // Retrieve harvest data from the forage file for the entire run. 
            // only get entries where a harvest happened (Amtkg > 0)
            HarvestData = fileCrop.GetCropDataForEntireRun(parentManagementActivity.LinkedLandItem.SoilType, CropName,
                                                               clock.StartDate, clock.EndDate).Where(a => a.AmtKg > 0).OrderBy(a => a.Year * 100 + a.Month).ToList<CropDataType>();
            if ((HarvestData == null) || (HarvestData.Count == 0))
                Summary.WriteMessage(this, $"Unable to locate any harvest data in [x={fileCrop.Name}] using [x={fileCrop.FileName}] for land id [{parentManagementActivity.LinkedLandItem.SoilType}] and crop name [{CropName}] between the dates [{clock.StartDate.ToShortDateString()}] and [{clock.EndDate.ToShortDateString()}]", MessageType.Warning);

            IsTreeCrop = (TreesPerHa == 0) ? false : true;  //using this boolean just makes things more readable.

            UnitsToHaConverter = (parentManagementActivity.LinkedLandItem.Parent as Land).UnitsOfAreaToHaConversion;

            // locate a cut and carry limiter associated with this event.
            limiter = LocateCutAndCarryLimiter(this);

            // check if harvest type tags have been provided
            HarvestTagsUsed = HarvestData.Where(a => a.HarvestType != "").Count() > 0;

            if (LinkedResourceItem is GrazeFoodStoreType)
            {
                double firstMonthsGrowth = 0;
                CropDataType cropData = HarvestData.Where(a => a.Year == clock.StartDate.Year && a.Month == clock.StartDate.Month).FirstOrDefault();
                if (cropData != null)
                    firstMonthsGrowth = cropData.AmtKg;

                (LinkedResourceItem as GrazeFoodStoreType).SetupStartingPasturePools(UnitsToHaConverter*(Parent as CropActivityManageCrop).Area * UnitsToHaConverter, firstMonthsGrowth);
                addReason = "Growth";
            }
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
                int clockYrMth = CalculateYearMonth(clock.Today);
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
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
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
                            else
                            {
                                NextHarvest = HarvestData.FirstOrDefault();
                                PreviousHarvest = null;
                            }
                            break;
                        case 1:
                            if (HarvestTagsUsed)
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
                            else
                                NextHarvest = HarvestData.FirstOrDefault();
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
            if ((this.ActivityEnabled & Status != ActivityStatus.Ignored) && HarvestData.Count() > 0 && clock.Today.Year * 100 + clock.Today.Month == HarvestData.First().Year * 100 + HarvestData.First().Month)
            {
                // don't rotate if no harvest tags or harvest type is not equal to "last"
                if (!HarvestTagsUsed || NextHarvest.HarvestType == "last")
                    parentManagementActivity.RotateCrop();
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
                    PerformTasksForActivity();
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
                ManageActivityResourcesAndTasks();
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
                if(model.Parent.GetType().IsSubclassOf(typeof(CLEMActivityBase)) || model.Parent.GetType() == typeof(ActivitiesHolder))
                    limiterFound = LocateCutAndCarryLimiter(model.Parent);
            return limiterFound;
        }


        /// <inheritdoc/>
        public override List<ResourceRequest> DetermineResourcesForActivity(double argument = 0)
        {
            int year = clock.Today.Year;
            int month = clock.Today.Month;
            AmountHarvested = 0;
            AmountAvailableForHarvest = 0;
            amountToDo = 0;
            amountToSkip = 0;

            // move pre placement tasks here
            if (NextHarvest != null)
            {
                //if this month is a harvest month for this crop
                if ((year == NextHarvest.HarvestDate.Year) && (month == NextHarvest.HarvestDate.Month))
                {
                    if (IsTreeCrop)
                        amountToDo = NextHarvest.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                    else
                        amountToDo = NextHarvest.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;

                }
            }

            // provide updated units of measure for identifiable children
            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForIdentifiableModels[valueToSupply.Key] = 1;
                        break;
                    case "per ha":
                        valuesForIdentifiableModels[valueToSupply.Key] = parentManagementActivity.Area;
                        break;
                    case "per ha harvested":
                        valuesForIdentifiableModels[valueToSupply.Key] = parentManagementActivity.Area;
                        break;
                    case "per kg harvested":
                        valuesForIdentifiableModels[valueToSupply.Key] = amountToDo;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }

            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForActivity()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var tagsShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "").FirstOrDefault();
                if (tagsShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * (1 - tagsShort.Required / tagsShort.Provided));

                this.Status = ActivityStatus.Partial;
            }

            AmountHarvested = amountToDo - amountToSkip;

            // reduce amount by limiter if present.
            if (limiter != null)
            {
                double canBeCarried = limiter.GetAmountAvailable(clock.Today.Month);
                AmountHarvested = Math.Max(AmountHarvested, canBeCarried);

                if (MathUtilities.IsLessThan(canBeCarried, AmountHarvested))
                    this.Status = ActivityStatus.Partial;

                limiter.AddWeightCarried(AmountHarvested);
            }

        }

        /// <inheritdoc/>
        public override void PerformTasksForActivity(double argument = 0)
        {
            if (MathUtilities.IsPositive(AmountHarvested))
            {
                AmountAvailableForHarvest = AmountHarvested;

                double percentN = 0;
                // if no nitrogen provided form file
                if (double.IsNaN(NextHarvest.Npct))
                {
                    if (LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType))
                        // grazed pasture with no N read assumes the green biomass N content
                        percentN = (LinkedResourceItem as GrazeFoodStoreType).GreenNitrogen;
                }
                else
                    percentN =  NextHarvest.Npct;

                if (MathUtilities.FloatsAreEqual(percentN, 0.0))
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
                        packet.DMD = (LinkedResourceItem as GrazeFoodStoreType).EstimateDMD(packet.PercentN);
                    LinkedResourceItem.Add(packet, this,"", addReason);
                }
                SetStatusSuccessOrPartial(MathUtilities.IsPositive(amountToSkip));
            }
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

            // ensure we don't try and change the crop area planeted when using unallocated land
            if (PlantedMultiplier != 1)
            {
                var parentManageCrop = this.FindAncestor<CropActivityManageCrop>();
                if (parentManageCrop != null && parentManageCrop.UseAreaAvailable)
                {
                    string[] memberNames = new string[] { "Invalid crop area" };
                    results.Add(new ValidationResult($"You cannot alter the crop area planted for product [a={this.Name}] when the crop [a={parentManageCrop.NameWithParent}] is set to use all available land", memberNames));
                }
                if(Parent is CropActivityManageProduct)
                {
                    string[] memberNames = new string[] { "Invalid crop area" };
                    results.Add(new ValidationResult($"You cannot alter the crop area planted for the mixed crop product (nested) [a={this.Name}] of the crop [a={parentManageCrop.NameWithParent}]", memberNames));
                }
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (TreesPerHa > 0)
                    htmlWriter.Write("\r\n<div class=\"activityentry\">This is a tree crop with a density of " + TreesPerHa.ToString() + " per hectare</div>");

                if (ProportionKept == 0)
                    htmlWriter.Write("\r\n<div class=\"activityentry\"><span class=\"errorlink\">" + ProportionKept.ToString("0.#%") + "</span> of this product is placed in ");
                else
                    htmlWriter.Write("\r\n<div class=\"activityentry\">" + ((ProportionKept == 1) ? "This " : "<span class=\"setvalue\">" + (ProportionKept).ToString("0.#%") + "</span> of this ") + "product is placed in ");

                if (StoreItemName == null || StoreItemName == "")
                    htmlWriter.Write("<span class=\"errorlink\">[STORE NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + StoreItemName + "</span>");

                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">Data is retrieved from ");
                if (ModelNameFileCrop == null || ModelNameFileCrop == "")
                    htmlWriter.Write("<span class=\"errorlink\">[CROP FILE NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"filelink\">" + ModelNameFileCrop + "</span>");

                htmlWriter.Write(" using crop named ");
                if (CropName == null || CropName == "")
                    htmlWriter.Write("<span class=\"errorlink\">[CROP NAME NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"filelink\">" + CropName + "</span>");

                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return base.ModelSummaryClosingTags(); 
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            string html = "";
            // if first child of mixed 
            if (this.Parent.GetType() == typeof(CropActivityManageProduct))
            {
                if (this.Parent.FindAllChildren<CropActivityManageProduct>().FirstOrDefault().Name == this.Name)
                    // close off the parent item so it displays
                    html += "\r\n</div>";
            }

            bool mixed = this.FindAllChildren<CropActivityManageProduct>().Count() >= 1;
            if (mixed)
            {
                html += "\r\n<div class=\"cropmixedlabel\">Mixed crop</div>";
                html += "\r\n<div class=\"cropmixedborder\">";
            }
            html += base.ModelSummaryOpeningTags();
            return html;
        } 
        #endregion
    }
}
