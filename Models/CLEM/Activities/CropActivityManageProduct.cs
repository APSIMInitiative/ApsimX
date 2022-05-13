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
using System.Xml.Serialization;
using System.Diagnostics;

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
    public class CropActivityManageProduct: CLEMActivityBase, IValidatableObject
    {
        [Link]
        private Clock clock = null;
        [Link]
        private Simulation simulation = null;

        private IFileCrop fileCrop;
        private CropActivityManageCrop parentManagementActivity;
        private ActivityCutAndCarryLimiter limiter;
        private bool rotationReady = false;
        private string addReason = "Harvest";
        private (int? previous, int? first, int? current, int? last) harvestOffset;
        private (CropDataType previous, CropDataType first, CropDataType current, CropDataType next, CropDataType last) harvests = (null, null, null, null, null);

        /// <summary>
        /// Name of the model for the crop input file
        /// </summary>
        [Description("Crop file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of crop file required")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetReadersAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(FileCrop), typeof(FileSQLiteCrop) } })]
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
        /// Flag for determining if this crop is currently being managed in cropping system e.g. rotation
        /// </summary>
        [JsonIgnore]
        public bool CurrentlyManaged { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CropActivityManageProduct()
        {
            this.SetDefaults();
            TransactionCategory = "Crop.[Product]";
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
                (LinkedResourceItem as GrazeFoodStoreType).Manager = Parent as IPastureManager;
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

            IsTreeCrop = TreesPerHa != 0;  //using this boolean just makes things more readable.

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
        /// The offset from previous, current and last harvests in months
        /// </summary>
        public (int? previous, int? first, int? current, int? last) HarvestOffset { get { return harvestOffset; } }

        /// <summary>
        /// The various harvests in sequence in which to make decisions
        /// </summary>
        [XmlIgnore]
        public (CropDataType previous, CropDataType first, CropDataType current, CropDataType last) Harvests { get; set; }

        /// <summary>
        /// Method to arrange any outstanding rotation before we start time step
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void OnCLEMStartOfTimeStepDoRotations(object sender, EventArgs e)
        {
            // rotate harvest if needed
            if (rotationReady) // && this.ActivityEnabled && HarvestData.Any())
            {
                parentManagementActivity.RotateCrop();
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
            harvests.current = null;
            rotationReady = false;  
            harvestOffset = (null, null, null, null);
            if (harvests.first == null)
            {
                harvests.first = HarvestData.FirstOrDefault();
                harvests.next = harvests.first;
            }

            int clockYrMth = CalculateYearMonth(clock.Today);
            if (harvests.previous != null)
                harvestOffset.previous = clockYrMth - CalculateYearMonth(harvests.previous.HarvestDate) as int?;
            if (harvests.first != null)
                harvestOffset.first = clockYrMth - CalculateYearMonth(harvests.first.HarvestDate) as int?;
            if (harvests.next != null)
                harvestOffset.current = clockYrMth - CalculateYearMonth(harvests.next.HarvestDate) as int?;
            if (harvests.last != null)
                harvestOffset.last = clockYrMth - CalculateYearMonth(harvests.last.HarvestDate) as int?;

            // change setting at time of harvest
            if (harvestOffset.current == 0)
            {
                if (HarvestTagsUsed)
                {
                    if ((harvests.next?.HarvestType??"") == "last")
                    {
                        InsideMultiHarvestSequence = false;
                        harvests.previous = harvests.next;
                        harvests.current = harvests.previous??harvests.first;
                        harvests.next = HarvestData.Skip(1).FirstOrDefault();
                        harvests.first = harvests.next;
                        harvests.last = null;
                        rotationReady = true;
                    }
                    else if ((harvests.next?.HarvestType ?? "") == "first")
                    {
                        harvests.current = harvests.first;
                        InsideMultiHarvestSequence = true;
                        harvests.previous = null;
                        harvests.last = HarvestData.Where(a => a.HarvestType == "last").FirstOrDefault();
                        if(harvests.last.HarvestDate > HarvestData.Skip(1).FirstOrDefault().HarvestDate)
                            harvests.next = HarvestData.Skip(1).FirstOrDefault();
                        else
                            harvests.next = harvests.last;
                    }
                    else
                    {
                        harvests.current = harvests.next;
                        harvests.next = HarvestData.Skip(1).FirstOrDefault();
                    }
                }
                else
                {
                    if(CurrentlyManaged)
                    {
                        rotationReady = true;
                        harvests.current = harvests.next;
                    }
                    harvests.previous = harvests.next;
                    harvests.next = HarvestData.Skip(1).FirstOrDefault();
                    harvests.first = harvests.next;
                    harvests.last = null;
                }

                // remove the record
                HarvestData.RemoveAt(0);
            }
            return;
        }

        /// <summary>
        /// Method to calulate the year month integer for this date
        /// </summary>
        /// <param name="date">date to process</param>
        /// <returns>year month integer</returns>
        public int CalculateYearMonth(DateTime date)
        {
            return date.Year * 12 + date.Month;
        }

        /// <summary>An event handler to allow us to get next supply of pasture</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMUpdatePasture")]
        private void OnCLEMUpdatePasture(object sender, EventArgs e)
        {
            if(parentManagementActivity.ActivityEnabled && LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType) && this.TimingOK)
            {
                Status = ActivityStatus.NotNeeded;
                DoActivity();
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
                GetResourcesRequiredForActivity();
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
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            if (CurrentlyManaged && this.TimingOK)
            {
                double amount = 0;
                //if this month is a harvest month for this crop
                if (harvests.current != null)
                {
                    if (IsTreeCrop)
                        amount = harvests.current.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                    else
                        amount = harvests.current.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;

                    if (limiter != null)
                    {
                        double canBeCarried = limiter.GetAmountAvailable(clock.Today.Month);
                        amount = Math.Max(amount, canBeCarried);
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
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perUnitOfLand:
                        numberUnits = parentManagementActivity.Area / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perHa:
                        numberUnits = parentManagementActivity.Area * UnitsToHaConverter / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
                }

                if (amount <= 0)
                    daysNeeded = 0;
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, (LinkedResourceItem as CLEMModel).NameWithParent);
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            AmountHarvested = 0;
            AmountAvailableForHarvest = 0;

            Status = ActivityStatus.NoTask;
            if (CurrentlyManaged)
            {
                if (this.TimingOK) // && NextHarvest != null)
                {
                    Status = ActivityStatus.NotNeeded;

                    //if this month is a harvest month for this crop
                    if (harvests.current != null)
                    {
                        if (IsTreeCrop)
                            AmountHarvested = harvests.current.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                        else
                            AmountHarvested = harvests.current.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;

                        AmountAvailableForHarvest = AmountHarvested;
                        // reduce amount by limiter if present.
                        if (limiter != null)
                        {
                            double canBeCarried = limiter.GetAmountAvailable(clock.Today.Month);
                            AmountHarvested = Math.Max(AmountHarvested, canBeCarried);

                            // now modify by labour limits as this is the amount labour was calculated for.
                            double labourLimit = this.LabourLimitProportion;
                            AmountHarvested *= labourLimit;

                            if (labourLimit < 1)
                                this.Status = ActivityStatus.Partial;

                            // TODO: now limit further by fees not paid
                            double financeLimit = this.LimitProportion(typeof(Finance));

                            limiter.AddWeightCarried(AmountHarvested);
                        }

                        if (AmountHarvested > 0)
                        {
                            double percentN = 0;
                            // if no nitrogen provided form file
                            if (double.IsNaN(harvests.current.Npct))
                            {
                                if (LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType))
                                    // grazed pasture with no N read assumes the green biomass N content
                                    percentN = (LinkedResourceItem as GrazeFoodStoreType).GreenNitrogen;
                            }
                            else
                                percentN = harvests.current.Npct;

                            if (percentN == 0)
                            {
                                //Add without adding any new nitrogen.
                                //The nitrogen value for this feed item in the store remains the same.
                                LinkedResourceItem.Add(AmountHarvested, this, "", addReason);
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
                                LinkedResourceItem.Add(packet, this, "", addReason);
                            }
                            if(Status != ActivityStatus.Partial)
                                Status = ActivityStatus.Success;
                        }
                    }
                }
            }
        }

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
    }
}
