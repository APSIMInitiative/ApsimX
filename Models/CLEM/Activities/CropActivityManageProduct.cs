using Models.Core;
using Models.CLEM.Limiters;
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
    public class CropActivityManageProduct: CLEMActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        [Link]
        private IClock clock = null;
        [Link]
        private Simulation simulation = null;
        [Link]
        private ZoneCLEM zoneCLEM = null;

        private IFileCrop fileCrop;
        private CropActivityManageCrop parentManagementActivity;
        private ActivityCarryLimiter limiter;
        private bool rotationReady = false;
        private string addReason = "Harvest";
        private double amountToDo;
        private double amountToSkip;
        private (int? previous, int? first, int? current, int? last) harvestOffset;
        private (CropDataType previous, CropDataType first, CropDataType current, CropDataType next, CropDataType last) harvests = (null, null, null, null, null);
        private double stockingRateSummed = 0;

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
        public bool CurrentlyManaged { get; set; } = true;

        /// <summary>
        /// Constructor
        /// </summary>
        public CropActivityManageProduct()
        {
            this.SetDefaults();
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {

                        },
                        measures: new List<string>() {
                            "fixed",
                            "per kg harvested",
                            "per land unit of crop",
                            "per hectare of crop",
                            "per land unit harvested",
                            "per hectare harvested",
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
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
            limiter = ActivityCarryLimiter.Locate(this);

            // check if harvest type tags have been provided
            HarvestTagsUsed = HarvestData.Where(a => a.HarvestType != "").Count() > 0;
        }

        /// <summary>An event handler to allow us to make checks after resources and activities initialised.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("FinalInitialise")]
        private void OnFinalInitialiseForProduct(object sender, EventArgs e)
        {
            // parent crop doesn't know pasture area until FinalInitialise activity
            // We must initialise biomass after the crop/pasture area is known
            if (LinkedResourceItem is GrazeFoodStoreType && (Parent as CropActivityManageCrop).FindAllChildren<CropActivityManageProduct>().Where(a => a.StoreItemName == this.StoreItemName).FirstOrDefault() == this)
            {
                double firstMonthsGrowth = 0;
                CropDataType cropData = HarvestData.Where(a => a.Year == clock.StartDate.Year && a.Month == clock.StartDate.Month).FirstOrDefault();
                if (cropData != null)
                    firstMonthsGrowth = cropData.AmtKg;

                (LinkedResourceItem as GrazeFoodStoreType).SetupStartingPasturePools((Parent as CropActivityManageCrop).Area * UnitsToHaConverter, firstMonthsGrowth);
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
            if (rotationReady)
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
                        if (harvests.last != null)
                        {
                            if (harvests.last.HarvestDate > HarvestData.Skip(1).FirstOrDefault().HarvestDate)
                                harvests.next = HarvestData.Skip(1).FirstOrDefault();
                            else
                                harvests.next = harvests.last;
                        }
                        else
                            harvests.next = HarvestData.Skip(1).FirstOrDefault();

                    }
                    else
                    {
                        // if the "last" tag has not been found keep checking as new data may have been loaded
                        // otherwise this crop will run until end of simulation.
                        if(harvests.last is null)
                            harvests.last = HarvestData.Where(a => a.HarvestType == "last").FirstOrDefault();
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
                ManageActivityResourcesAndTasks();
                //PerformTasksForTimestep();
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
        /// Function to calculate ecological indicators.
        /// By summing the monthly stocking rates so when you do yearly ecological calculation
        /// you can get average monthly stocking rate for the whole year.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCalculateEcologicalState")]
        private void OnCLEMCalculateEcologicalState(object sender, EventArgs e)
        {
            // This event happens after growth and pasture consumption and animal death
            // But before any management, buying and selling of animals.

            if (LinkedResourceItem is GrazeFoodStoreType)
            {
                double area = (Parent as CropActivityManageCrop).Area * UnitsToHaConverter * 0.01;

                // add this months stocking rate to running total
                stockingRateSummed += PastureActivityManage.CalculateStockingRateRightNow(Resources.FindResourceGroup<RuminantHerd>(), StoreItemName, area);

                //If it is time to do yearly calculation
                if (zoneCLEM.IsEcologicalIndicatorsCalculationMonth())
                {
                    PastureActivityManage.CalculateEcologicalIndicators(LinkedResourceItem as GrazeFoodStoreType, null, null, stockingRateSummed, zoneCLEM.EcologicalIndicatorsCalculationInterval, clock.StartDate, zoneCLEM.EcologicalIndicatorsNextDueDate);

                    // Reset running total for stocking rate
                    stockingRateSummed = 0;
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            AmountHarvested = 0;
            AmountAvailableForHarvest = 0;
            amountToDo = 0;
            amountToSkip = 0;
            if (CurrentlyManaged && this.TimingOK)
            {
                //if this month is a harvest month for this crop
                if (harvests.current != null)
                {
                    if (IsTreeCrop)
                        amountToDo = harvests.current.AmtKg * TreesPerHa * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;
                    else
                        amountToDo = harvests.current.AmtKg * parentManagementActivity.Area * UnitsToHaConverter * ProportionKept;

                    if (limiter != null)
                    {
                        double canBeCarried = limiter.GetAmountAvailable(clock.Today.Month);
                        amountToDo = Math.Max(amountToDo, canBeCarried);
                    }
                }
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per kg harvested":
                        valuesForCompanionModels[valueToSupply.Key] = amountToDo;
                        break;
                    case "per land unit of crop":
                        valuesForCompanionModels[valueToSupply.Key] = parentManagementActivity.Area;
                        break;
                    case "per hectare of crop":
                        valuesForCompanionModels[valueToSupply.Key] = parentManagementActivity.Area * UnitsToHaConverter;
                        break;
                    case "per land unit harvested":
                        if (amountToDo > 0)
                            valuesForCompanionModels[valueToSupply.Key] = parentManagementActivity.Area;
                        else
                            valuesForCompanionModels[valueToSupply.Key] = 0;
                        break;
                    case "per hectare harvested":
                        if (amountToDo > 0)
                            valuesForCompanionModels[valueToSupply.Key] = parentManagementActivity.Area * UnitsToHaConverter;
                        else
                            valuesForCompanionModels[valueToSupply.Key] = 0;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }

            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var tagsShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "").FirstOrDefault();
                if (tagsShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * (1 - tagsShort.Available / tagsShort.Required));
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
        public override void PerformTasksForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NoTask;
            if (CurrentlyManaged)
            {
                if (this.TimingOK) // && NextHarvest != null)
                {
                    Status = ActivityStatus.NotNeeded;
                    if (MathUtilities.IsPositive(AmountHarvested))
                    {
                        AmountAvailableForHarvest = AmountHarvested;

                        double percentN = 0;
                        // if no nitrogen provided from file
                        if (double.IsNaN(harvests.current.Npct))
                        {
                            if (LinkedResourceItem.GetType() == typeof(GrazeFoodStoreType))
                                // grazed pasture with no N read assumes the green biomass N content
                                percentN = (LinkedResourceItem as GrazeFoodStoreType).GreenNitrogen;
                        }
                        else
                            percentN = harvests.current.Npct;

                        if (MathUtilities.FloatsAreEqual(percentN, 0.0))
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
                            LinkedResourceItem.Add(packet, this, null, addReason);
                        }
                        SetStatusSuccessOrPartial(MathUtilities.IsPositive(amountToSkip));
                    }
                }
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
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            string intro = (FindAllChildren<CropActivityManageProduct>().Count() >= 1) ? "Mixed crop" : "";
            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<CropActivityManageProduct>(), true, "childgrouprotationborder", intro, ""),
            };
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (TreesPerHa > 0)
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">This is a tree crop with a density of {CLEMModel.DisplaySummaryValueSnippet(TreesPerHa)} per hectare</div>");

                htmlWriter.Write($"\r\n<div class=\"activityentry\">" + ((ProportionKept == 1) ? "This " : $"{CLEMModel.DisplaySummaryValueSnippet(ProportionKept, warnZero: true)} of this ") + "product is placed in ");
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(StoreItemName, "Resource not set", HTMLSummaryStyle.Resource)}");
                htmlWriter.Write("</div>");

                htmlWriter.Write($"\r\n<div class=\"activityentry\">Data is retrieved from {CLEMModel.DisplaySummaryValueSnippet(ModelNameFileCrop, "Resource not set", HTMLSummaryStyle.FileReader)}");
                htmlWriter.Write($" using crop named {CLEMModel.DisplaySummaryValueSnippet(CropName)}");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
