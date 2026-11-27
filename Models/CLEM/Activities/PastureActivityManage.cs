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
using Models.CLEM.Extras;
using Models.CLEM.DataReaders;

namespace Models.CLEM.Activities
{
    /// <summary>This activity manages a pasture on an area of land linked to a pasture type</summary>
    /// <remarks>
    /// <para>Ruminant move activities place individuals in the paddock, after which they will graze pasture for the
    /// paddock stored in the Pasture Pools</para>
    /// </remarks>
    /// <updates>
    /// First implementation of this activity using NABSA grazing processes
    /// </updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages a pasture (GrazeFoodStoreType) by allocating land, tracking pasture state and ecological indicators, communicating with a pasture production database")]
    [Version(1, 0, 2, "Now supports generic pasture production data reader")]
    [Version(1, 0, 2, "Added ecological indicator calculations")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Pasture/ManagePasture.htm")]
    public class PastureActivityManage: CLEMActivityBase, IValidatableObject, IPastureManager, IHandlesActivityCompanionModels
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        [Link]
        private readonly ZoneCLEM zoneCLEM = null;

        private double unitsOfArea2Ha;
        private IFilePasture filePasture = null;
        private string soilIndex = "0"; // obtained from LandType used
        private double stockingRateSummed;  //summed since last Ecological Calculation.
        private readonly double ha2sqkm = 0.01; //convert ha to square km
        private List<PastureDataType> pastureDataList;
        private Relationship relationshipLC;
        private Relationship relationshipGBA;

        /// <summary>
        /// The name of the <c>LandType</c> where pasture is located
        /// </summary>
        /// <remarks>
        /// This is required. A list of available land types is provided in a dropdown list in the UI.
        /// </remarks>
        [Description("Land type to use")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Land type where pasture is located required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Land) } })]
        public string LandTypeNameToUse { get; set; }

        /// <summary>
        /// The area requested for the paddock
        /// </summary>
        [Description("Land area requested")]
        [Required, GreaterThanEqualValue(0)]
        public double AreaRequested { get; set; }

        /// <summary>
        /// A switch to indicate whether this pasture should use all unallocated land of the specified type
        /// </summary>
        [Description("Use Land type's unallocated land")]
        public bool UseAreaAvailable { get; set; }

        /// <summary>
        /// The name of the Pasture type (<c>GrazeFoodStoreType</c>) to track pasture
        /// </summary>
        /// <remarks>
        /// A list of available pasture types is provided in a dropdown list in the UI.
        /// </remarks>
        [Description("GrazeFoodStore type managed")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pasture required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(GrazeFoodStore) } })]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// The name of the data reader to use
        /// </summary>
        /// <remarks>
        /// This is required. A list of available pasture data readers is provided in a dropdown list in the UI.
        /// </remarks>
        [Description("Pasture data reader to use")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pasture production database reader required")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetNameOfModelsByType", ValuesArgs = new object[] { new Type[] { typeof(FileCrop), typeof(FileSQLitePasture) } })]
        public string PastureDataReader { get; set; }

        /// <summary>
        /// The starting stocking rate (Adult Equivalents/square km) 
        /// </summary>
        [Description("Starting stocking rate (AEs/sqkm)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingStockingRate { get; set; }

        /// <summary>
        /// The current area allocated to the pasture
        /// </summary>
        [JsonIgnore]
        public double Area { get; set; }

        /// <summary>
        /// The current land condition index
        /// </summary>
        [JsonIgnore]
        public RelationshipRunningValue LandConditionIndex { get; set; }

        /// <summary>
        /// The current grass basal area
        /// </summary>
        [JsonIgnore]
        public RelationshipRunningValue GrassBasalArea { get; set; }

        /// <summary>
        /// The GrazeFoodStoreType resource being managed
        /// </summary>
        [JsonIgnore]
        public GrazeFoodStoreType LinkedNativeFoodType { get; set; }

        /// <summary>
        /// The land type upon which this pasture is growing
        /// </summary>
        [JsonIgnore]
        public LandType LinkedLandItem { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PastureActivityManage()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            return type switch
            {
                "Relationship" => new LabelsForCompanionModels(
                                        identifiers: [
                                            "Utilisation % to change in Land condition index",
                                            "Utilisation % to change in Grass basal area"
                                        ],
                                        measures: []
                                        ),
                _ => new LabelsForCompanionModels(),
            };
        }

        /// <summary>An event handler allowing for the initialisation of this activity</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed during the <c>CLEMEvents.CLEMInitialiseResource</c> event before other activities are
        /// initialised
        /// </remarks>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // activity is performed in CLEMUpdatePasture not CLEMGetResources and has no labour
            AllocationStyle = ResourceAllocationStyle.Manual;

            // locate Land Type resource for this forage.
            LinkedLandItem = Resources.FindResourceType<Land, LandType>(this, LandTypeNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            relationshipLC = Structure.FindChildren<Relationship>().Where(a => a.Identifier == "Utilisation % to change in Land condition index").FirstOrDefault();
            if (relationshipLC != null)
            {
                LandConditionIndex = Structure.FindChild<RelationshipRunningValue>(relativeTo: relationshipLC);
            }

            relationshipGBA = Structure.FindChildren<Relationship>().Where(a => a.Identifier == "Utilisation % to change in Grass basal area").FirstOrDefault();
            if (relationshipGBA != null)
            {
                GrassBasalArea = Structure.FindChild<RelationshipRunningValue>(relativeTo: relationshipGBA);
            }

            ZoneCLEM clemModel = Structure.FindParent<ZoneCLEM>(recurse: true);
            filePasture = Structure.FindChildren<IFilePasture>(recurse: true, relativeTo: clemModel).Where(a => (a as CLEMModel).Name == PastureDataReader).FirstOrDefault();

            if (LandConditionIndex is null || GrassBasalArea is null || filePasture is null)
            {
                return;
            }

            LandType land = null;
            if (filePasture != null)
            {
                // check that database has region id and land id
                ZoneCLEM clem = Structure.FindParent<ZoneCLEM>(recurse: true);
                int recs = filePasture.RecordsFound((filePasture as FileSQLitePasture).RegionColumnName, clem.ClimateRegion);
                if (recs == 0)
                {
                    throw new ApsimXException(this, $"No pasture production records were located by [x={(filePasture as Model).Name}] for [a={Name}] given [Region id] = [{clem.ClimateRegion}] as specified in [{clem.Name}]");
                }

                land = Resources.FindResourceType<Land, LandType>(this, LandTypeNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
                if (land != null)
                {
                    recs = filePasture.RecordsFound((filePasture as FileSQLitePasture).LandIdColumnName, land.SoilType);
                    if (recs == 0)
                    {
                        throw new ApsimXException(this, $"No pasture production records were located by [x={(filePasture as Model).Name}] for [a={Name}] given [Land id] = [{land.SoilType}] as specified in [{land.Name}] used to manage the pasture");
                    }
                }
            }

            if (UseAreaAvailable)
            {
                LinkedLandItem.TransactionOccurred += LinkedLandItem_TransactionOccurred;
            }

            ResourceRequestList =
            [
                new()
                {
                    Resource = land,
                    AllowTransmutation = false,
                    Required = UseAreaAvailable ? LinkedLandItem.AmountAvailable : AreaRequested,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandTypeNameToUse.Split('.').Last(),
                    ActivityModel = this,
                    Category = UseAreaAvailable? "Assign unallocated": TransactionCategory,
                    FilterDetails = null
                }
            ];

            CheckResources(ResourceRequestList, Guid.NewGuid());
            TakeResources(ResourceRequestList);

            //Now the Land has been allocated we have an Area
            if (ResourceRequestList.FirstOrDefault().Provided > 0)
            {
                //get the units of area for this run from the Land resource parent.
                unitsOfArea2Ha = Resources.FindResourceGroup<Land>().UnitsOfAreaToHaConversion;
                // locate Pasture Type resource
                LinkedNativeFoodType = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
                //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                Area = ResourceRequestList.FirstOrDefault().Provided;
                // ensure no other activity has set the area of this GrazeFoodStore
                LinkedNativeFoodType.Manager = this as IPastureManager;
                soilIndex = ((LandType)ResourceRequestList.FirstOrDefault().Resource).SoilType;

                if (LandConditionIndex is not null)
                {
                    LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex = LandConditionIndex.StartingValue;
                }

                if (GrassBasalArea is not null)
                {
                    LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea = GrassBasalArea.StartingValue;
                }

                LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate = StartingStockingRate;
                stockingRateSummed = StartingStockingRate;

                //Now we have a stocking rate and we have starting values for Land Condition and Grass Basal Area
                //get the starting pasture data list from Pasture reader
                if (filePasture != null & LinkedNativeFoodType != null)
                {
                    GetPastureDataList_TodayToNextEcologicalCalculation();

                    double firstMonthsGrowth = 0;
                    if (pastureDataList != null)
                    {
                        PastureDataType pastureData = pastureDataList.Where(a => a.Year == events.Clock.StartDate.Year && a.Month == events.Clock.StartDate.Month).FirstOrDefault();
                        firstMonthsGrowth = pastureData.Growth;
                    }

                    LinkedNativeFoodType.SetupStartingPasturePools(Area * unitsOfArea2Ha, firstMonthsGrowth);
                }
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (LinkedLandItem != null && UseAreaAvailable)
            {
                LinkedLandItem.TransactionOccurred -= LinkedLandItem_TransactionOccurred;
            }
        }

        /// <summary>An event handler to allow us to get next supply of pasture</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMUpdatePasture")]
        private void OnCLEMUpdatePasture(object sender, EventArgs e)
        {
            Status = ActivityStatus.Ignored;
            if (pastureDataList is not null)
            {
                Status = ActivityStatus.NotNeeded;
                double growth = 0;

                FoodResourcePacket trackQuality = new();

                // get all entries in the current time step
                // entries are combined to occur at the start of the time step
                IEnumerable<PastureDataType> dataEntries = pastureDataList.Where(a => events.IsDateInTimeStep(a.CutDate));

                if (dataEntries.Any())
                {
                    growth = dataEntries.Sum(a => a.Growth);
                    growth *= unitsOfArea2Ha;

                    LinkedNativeFoodType.CurrentEcologicalIndicators.Rainfall += dataEntries.Sum(a => a.Rainfall);
                    LinkedNativeFoodType.CurrentEcologicalIndicators.Erosion += dataEntries.Sum(a => a.SoilLoss);
                    LinkedNativeFoodType.CurrentEcologicalIndicators.Runoff += dataEntries.Sum(a => a.Runoff);
                    LinkedNativeFoodType.CurrentEcologicalIndicators.Cover = dataEntries?.Last().Cover ?? LinkedNativeFoodType.CurrentEcologicalIndicators.Cover;
                    LinkedNativeFoodType.CurrentEcologicalIndicators.TreeBasalArea = dataEntries?.Last().TreeBA ?? LinkedNativeFoodType.CurrentEcologicalIndicators.TreeBasalArea;

                    if (growth > 0)
                    {
                        Status = ActivityStatus.Success;
                        GrazeFoodStorePool newPasture = new(growth * Area, LinkedNativeFoodType, events.TimeStepStart, events.TimeStepStart)
                        {
                            NitrogenPercent = LinkedNativeFoodType.GreenNitrogenPercent
                        };
                        if (LinkedNativeFoodType.DMDStyle == DryMatterDigestibilityStyle.SpecifyNewGrowthDMD)
                        {
                            newPasture.DryMatterDigestibility = LinkedNativeFoodType.GreenDMD;
                        }
                        else
                        {
                            newPasture.DryMatterDigestibility = newPasture.NitrogenPercent * LinkedNativeFoodType.NToDMDCoefficient + LinkedNativeFoodType.NToDMDIntercept;
                        }
                        newPasture.DryMatterDigestibility = Math.Min(100, Math.Max(LinkedNativeFoodType.MinimumDMD, newPasture.DryMatterDigestibility));
                        newPasture.RumenDegradableProteinPercent = LinkedNativeFoodType.RumenDegradableProteinPercent;
                        LinkedNativeFoodType.AddToResource(newPasture, this, LinkedNativeFoodType.Name, "Growth");
                    }
                }
            }

            // report activity performed.
            ActivityPerformedEventArgs activitye = new()
            {
                Name = Name,
                Status = events.IsEcologicalIndicatorsCalculationDue() ? ActivityStatus.Calculation : ActivityStatus.Success,
                Id = UniqueID.ToString(),
            };
        }

        /// <summary>
        /// An event handler to allow for the calculation of ecological indicators.
        /// </summary>
        /// <remarks>
        ///
        /// <para>By summing the monthly stocking rates so when you do yearly ecological calculation you can get average
        /// monthly stocking rate for the whole year.</para>
        /// <para>This is performed in the CLEMEvents.CLEMCalculateEcologicalState event.</para>
        /// </remarks>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCalculateEcologicalState")]
        private void OnCLEMCalculateEcologicalState(object sender, EventArgs e)
        {
            // This event happens after growth and pasture consumption and animal death but before any management, buying and selling of animals.

            // add this months stocking rate to running total
            stockingRateSummed += CalculateStockingRateRightNow(Resources.FindResourceGroup<RuminantHerd>(), FeedTypeName, Area * unitsOfArea2Ha * ha2sqkm);

            //If it is time to do yearly calculation
            if (LinkedNativeFoodType is not null && events.IsEcologicalIndicatorsCalculationDue())
            {
                CalculateEcologicalIndicators(LinkedNativeFoodType, LandConditionIndex, GrassBasalArea, stockingRateSummed, events.EcologicalIndicatorsCalculationInterval, events.Clock.StartDate, events.EcologicalIndicatorsNextDueDate);

                // Reset running total for stocking rate
                stockingRateSummed = 0;

                GetPastureDataList_TodayToNextEcologicalCalculation();
            }
        }

        /// <summary>
        /// Calculate the current stocking rate
        /// </summary>
        /// <param name="herd">Herd to consider</param>
        /// <param name="paddockName">Name of paddock with animals</param>
        /// <param name="areaSqKm">Area of paddock in Square Km</param>
        /// <returns>Stocking rate (AE/sq km)</returns>
        public static double CalculateStockingRateRightNow(RuminantHerd herd, string paddockName, double areaSqKm)
        {
            if (herd != null)
            {
                if (paddockName.Contains('.'))
                    paddockName = paddockName[(paddockName.IndexOf('.') + 1)..];

                return herd.Herd.Where(a => a.Location == paddockName).Sum(a => a.Weight.AdultEquivalent) / areaSqKm;
            }
            else
                return 0;
        }

        /// <summary>
        /// Method to perform calculation of all Ecological indicators.
        /// </summary>
        /// <param name="grazeFoodStore">Graze food store to consider</param>
        /// <param name="landConIndex">Land condition value</param>
        /// <param name="grassBasalAreaIndex">Grass basal area value</param>
        /// <param name="summedStockRate">Running sum of stocking rate</param>
        /// <param name="interval">Ecological indicator interval</param>
        /// <param name="NextDueDate">Next ecological indicator calculation date</param>
        /// <param name="startDate">Simulation start date</param>
        public static void CalculateEcologicalIndicators(GrazeFoodStoreType grazeFoodStore, RelationshipRunningValue landConIndex, RelationshipRunningValue grassBasalAreaIndex, double summedStockRate, int interval, DateTime startDate, DateTime NextDueDate )
        {
            if (grazeFoodStore != null)
            {
                // Calculate change in Land Condition index and Grass basal area
                double utilisation = grazeFoodStore.PercentUtilisation;

                if (landConIndex != null)
                {
                    landConIndex.Modify(utilisation);
                    grazeFoodStore.CurrentEcologicalIndicators.LandConditionIndex = landConIndex.Value;
                }

                if (grassBasalAreaIndex != null)
                {
                    grassBasalAreaIndex.Modify(utilisation);
                    grazeFoodStore.CurrentEcologicalIndicators.GrassBasalArea = grassBasalAreaIndex.Value;
                }

                // Calculate average monthly stocking rate
                // Check number of months to use
                int monthDifference = ((NextDueDate.Year - startDate.Year) * 12) + NextDueDate.Month - startDate.Month + 1;
                if (monthDifference >= interval)
                    monthDifference = interval;

                grazeFoodStore.CurrentEcologicalIndicators.StockingRate = summedStockRate / monthDifference;

                //perennials
                if (landConIndex != null)
                {
                    grazeFoodStore.CurrentEcologicalIndicators.Perennials = 92.2 * (1 - Math.Pow(landConIndex.Value, 3.35) / Math.Pow(landConIndex.Value, 3.35 + 137.7)) - 2.2;
                }

                //%utilisation
                grazeFoodStore.CurrentEcologicalIndicators.Utilisation = utilisation;

                // calculate averages
                grazeFoodStore.CurrentEcologicalIndicators.Cover /= monthDifference;
                grazeFoodStore.CurrentEcologicalIndicators.TreeBasalArea /= monthDifference;

                //TreeC
                // I didn't include the / area as tba is already per ha. I think NABSA has this wrong
                grazeFoodStore.CurrentEcologicalIndicators.TreeCarbon = grazeFoodStore.CurrentEcologicalIndicators.TreeBasalArea * 6286 * 0.46;

                //methane
                //soilC
                //Burn kg
                //methaneFire
                //N2OFire
            }
        }

        /// <summary>
        /// A method to get all the Pasture Data from today to the next Ecological Calculation from the Pasture File
        /// </summary>
        private void GetPastureDataList_TodayToNextEcologicalCalculation()
        {
            // In IAT it only updates the GrassBA, LandCon and StockingRate (Ecological Indicators)
            // every so many months (specified by user, not every month).
            // And the month they are updated on each year is whatever the starting month was for the run.

            pastureDataList = filePasture.GetIntervalsPastureData(zoneCLEM.ClimateRegion, soilIndex,
               LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea, LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex, LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate, events.Clock.Today.AddDays(1), events.EcologicalIndicatorsCalculationInterval);
        }

        /// <summary>
        /// An event hander to listen for changes in land area
        /// </summary>
        /// <remarks>
        /// This allows this activity to dynamically respond to changes in land available when use available area is
        /// selected
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LinkedLandItem_TransactionOccurred(object sender, EventArgs e)
        {
            Area = LinkedLandItem.AmountAvailable;
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (relationshipLC == null)
            {
                string[] memberNames = ["Relationship for LandConditionIndex"];
                yield return new ValidationResult($"[a={NameWithParent}] requires a [Relationship] as a child component with the identifier [Utilisation % to change in Land condition index]", memberNames);
            }
            else
            {
                if (LandConditionIndex == null)
                {
                    string[] memberNames = ["RelationshipRunningValue for LandConditionIndex"];
                    yield return new ValidationResult("Unable to locate a [o=RelationshipRunningValue] with the Land Condition Index [a=Relationship] for this pasture.\r\nAdd a [o=RelationshipRunningValue] below the [a=Relationsip] with identifier [Utilisation % to change in Land condition index]", memberNames);
                }
            }
            if (relationshipGBA == null)
            {
                string[] memberNames = ["Relationship for Grass Basal Area"];
                yield return new ValidationResult($"[a={NameWithParent}] requires a [Relationship] as a child component with the identifier [Utilisation % to change in Grass basal area]", memberNames);
            }
            else
            {
                if (GrassBasalArea == null)
                {
                    string[] memberNames = ["RelationshipRunningValue for GrassBasalArea"];
                    yield return new ValidationResult("Unable to locate a [o=RelationshipRunningValue] with the Grass Basal Area [a=Relationship] for this pasture.\r\nAdd a [o=RelationshipRunningValue] below the [a=Relationsip] with identifier [Utilisation % to change in Grass basal area]", memberNames);
                }
            }
            if (filePasture == null)
            {
                string[] memberNames = ["FilePastureReader"];
                yield return new ValidationResult("Unable to locate pasture database file. Add a FilePasture or FileSQLitePasture reader model component to the simulation tree.", memberNames);
            }
        }

        #endregion
    }
}
