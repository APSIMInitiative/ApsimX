using Models.Core;
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
    /// <summary>Pasture management activity</summary>
    /// <summary>This activity provides a pasture based on land unit, area and pasture type</summary>
    /// <summary>Ruminant mustering activities place individuals in the paddack after which they will graze pasture for the paddock stored in the PastureP Pools</summary>
    /// <version>1.0</version>
    /// <updates>First implementation of this activity using NABSA grazing processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages a pasture by allocating land, tracking pasture state and ecological indicators and communicating with the GRASP data file.")]
    [Version(1, 0, 2, "Added ecological indicator calculations")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Pasture/ManagePasture.htm")]
    public class PastureActivityManage: CLEMActivityBase, IValidatableObject, IPastureManager
    {
        [Link]
        Clock Clock = null;
        [Link]
        ZoneCLEM ZoneCLEM = null;
 
        /// <summary>
        /// Land type where pasture is located
        /// </summary>
        [Description("Land type where pasture is located")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Land type where pasture is located required")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Land) })]
        public string LandTypeNameToUse { get; set; }

        /// <summary>
        /// Pasture type to use
        /// </summary>
        [Description("Pasture to manage")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pasture required")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) })]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Name of the model for the GRASP pasture input file
        /// </summary>
        [Description("Name of model for GRASP pasture growth file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of GRASP pasture growth file model required")]
        [Models.Core.Display(Type = DisplayType.CLEMGraspFileName)]
        public string ModelNameFileGRASP { get; set; }

        /// <summary>
        /// Starting amount (kg)
        /// </summary>
        [Description("Starting Amount (kg/ha)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Starting stocking rate (Adult Equivalents/square km)
        /// </summary>
        [Description("Starting stocking rate (Adult Equivalents/sqkm)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingStockingRate { get; set; }

        /// <summary>
        /// Area of pasture
        /// </summary>
        [XmlIgnore]
        public double Area { get; set; }

        /// <summary>
        /// Current land condition index
        /// </summary>
        [XmlIgnore]
        public RelationshipTracker LandConditionIndex { get; set; }

        /// <summary>
        /// Grass basal area
        /// </summary>
        [XmlIgnore]
        public RelationshipTracker GrassBasalArea { get; set; }

        /// <summary>
        /// Area requested
        /// </summary>
        [Description("Area of pasture")]
        [Required, GreaterThanEqualValue(0)]
        public double AreaRequested { get; set; }

        /// <summary>
        /// Use unallocated available
        /// </summary>
        [Description("Use unallocated land")]
        public bool UseAreaAvailable { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
        public GrazeFoodStoreType LinkedNativeFoodType { get; set; }

        /// <summary>
        /// Land item
        /// </summary>
        [XmlIgnore]
        public LandType LinkedLandItem { get; set; }

        // private properties
        private double unitsOfArea2Ha;
        private IFileGRASP FileGRASP = null;
        //private double pkGrassBA = 0; //rounded (1 decimal place) integer value used as primary key in GRASP file.
        //private double pkLandCon = 0; //rounded (1decimal place) double value used as primary key in GRASP file.
        private string soilIndex = "0"; // obtained from LandType used
        private double StockingRateSummed;  //summed since last Ecological Calculation.
        //private double pkStkRate = 0; //rounded integer value used as primary key in GRASP file.
        private double ha2sqkm = 0.01; //convert ha to square km
        private bool gotLandRequested = false; //was this pasture able to get the land it requested ?
        //EcologicalCalculationIntervals worth of data read from GRASP file 
        private List<PastureDataType> PastureDataList;

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (LandConditionIndex == null)
            {
                string[] memberNames = new string[] { "LandConditionIndexRelationship" };
                results.Add(new ValidationResult("Unable to locate Land Condition Index relationship in user interface", memberNames));
            }
            if (GrassBasalArea == null)
            {
                string[] memberNames = new string[] { "GrassBasalAreaRelationship" };
                results.Add(new ValidationResult("Unable to locate grass Basal Area relationship in user interface", memberNames));
            }
            if (FileGRASP == null)
            {
                string[] memberNames = new string[] { "FileGRASP" };
                results.Add(new ValidationResult("Unable to locate GRASP file. Add a FileGRASP model component to the user interface tree.", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to intitalise this activity just once at start of simulation</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // activity is performed in CLEMUpdatePasture not CLEMGetResources and has no labour
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            // locate Land Type resource for this forage.
            LinkedLandItem = Resources.GetResourceItem(this, LandTypeNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as LandType;
            LandConditionIndex = Apsim.Children(this, typeof(RelationshipTracker)).Where(a => a.Name == "LandConditionIndex").FirstOrDefault() as RelationshipTracker;
            GrassBasalArea = Apsim.Children(this, typeof(RelationshipTracker)).Where(a => a.Name == "GrassBasalArea").FirstOrDefault() as RelationshipTracker;
            FileGRASP = Apsim.ChildrenRecursively(ZoneCLEM.Parent).Where(a => a.Name == ModelNameFileGRASP).FirstOrDefault() as IFileGRASP;

            if (UseAreaAvailable)
            {
                LinkedLandItem.TransactionOccurred += LinkedLandItem_TransactionOccurred;
            }

            ResourceRequestList = new List<ResourceRequest>
                {
                new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = UseAreaAvailable ? LinkedLandItem.AreaAvailable : AreaRequested,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandTypeNameToUse.Split('.').Last(),
                    ActivityModel = this,
                    Reason = UseAreaAvailable ?"Assign unallocated":"Assign",
                    FilterDetails = null
                }
                };

            CheckResources(ResourceRequestList, Guid.NewGuid());
            gotLandRequested = TakeResources(ResourceRequestList, false);

            //Now the Land has been allocated we have an Area 
            if (gotLandRequested)
            {            
                //get the units of area for this run from the Land resource parent.
                unitsOfArea2Ha = Resources.Land().UnitsOfAreaToHaConversion;

                // locate Pasture Type resource
                LinkedNativeFoodType = Resources.GetResourceItem(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;

                //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                Area = ResourceRequestList.FirstOrDefault().Provided;

                // ensure no other activity has set the area of this GrazeFoodStore
                LinkedNativeFoodType.Manager = this as IPastureManager;

                soilIndex = ((LandType)ResourceRequestList.FirstOrDefault().Resource).SoilType;
    
                LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex = LandConditionIndex.StartingValue;
                LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea = GrassBasalArea.StartingValue;
                LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate = StartingStockingRate;
                StockingRateSummed = StartingStockingRate;

                //Now we have a stocking rate and we have starting values for Land Condition and Grass Basal Area
                //get the starting pasture data list from GRASP
                GetPastureDataList_TodayToNextEcolCalculation();

                SetupStartingPasturePools(StartingAmount);
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
            this.Status = ActivityStatus.Ignored;
            if (PastureDataList != null)
            {
                this.Status = ActivityStatus.NotNeeded;
                double growth = 0;

                //// method is performed on last day of month but needs to work with next month's details
                //int year = Clock.Today.AddDays(1).Year;
                //int month = Clock.Today.AddDays(1).Month;

                int year = Clock.Today.Year;
                int month = Clock.Today.Month;

                //Get this months pasture data from the pasture data list
                PastureDataType pasturedata = PastureDataList.Where(a => a.Year == year && a.Month == month).FirstOrDefault();

                growth = pasturedata.Growth;
                //TODO: check units from input files.
                // convert from kg/ha to kg/area unit
                growth *= unitsOfArea2Ha;

                LinkedNativeFoodType.CurrentEcologicalIndicators.Rainfall += pasturedata.Rainfall;
                LinkedNativeFoodType.CurrentEcologicalIndicators.Erosion += pasturedata.SoilLoss;
                LinkedNativeFoodType.CurrentEcologicalIndicators.Runoff += pasturedata.Runoff;
                LinkedNativeFoodType.CurrentEcologicalIndicators.Cover += pasturedata.Cover;
                LinkedNativeFoodType.CurrentEcologicalIndicators.TreeBasalArea += pasturedata.TreeBA;

                if (growth > 0)
                {
                    this.Status = ActivityStatus.Success;
                    GrazeFoodStorePool newPasture = new GrazeFoodStorePool
                    {
                        Age = 0
                    };
                    newPasture.Set(growth * Area);
                    newPasture.Nitrogen = this.LinkedNativeFoodType.GreenNitrogen; 
                    newPasture.DMD = newPasture.Nitrogen * LinkedNativeFoodType.NToDMDCoefficient + LinkedNativeFoodType.NToDMDIntercept;
                    newPasture.DMD = Math.Min(100,Math.Max(LinkedNativeFoodType.MinimumDMD, newPasture.DMD));
                    newPasture.Growth = newPasture.Amount;
                    this.LinkedNativeFoodType.Add(newPasture,this,"Growth");
                }
            }
            else
            {
                this.Status = ActivityStatus.Critical;
                throw new Exception("No pasture data is available for [a="+this.Name+"]\nCheck that data is available for specified land id and climate region id etc. ");
            }

            // report activity performed.
            ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
            {
                Activity = new BlankActivity()
                {
                    Status = ZoneCLEM.IsEcologicalIndicatorsCalculationMonth()? ActivityStatus.Calculation: ActivityStatus.Success,
                    Name = this.Name
                }
            };
            activitye.Activity.SetGuID(this.UniqueID);
            this.OnActivityPerformed(activitye);
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

            // add this months stocking rate to running total 
            StockingRateSummed += CalculateStockingRateRightNow();

            CalculateEcologicalIndicators();
        }

        private void SetupStartingPasturePools(double startingGrowth)
        {
            // Initial biomass
            double amountToAdd = Area * startingGrowth;
            if (amountToAdd <= 0)
            {
                return;
            }

            // Set up pasture pools to start run based on month and user defined pasture properties
            // Locates the previous five months where growth occurred (Nov-Mar) and applies decomposition to current month
            // This months growth will not be included.

            int month = Clock.Today.Month;
            int monthCount = 0;
            int includedMonthCount = 0;
            double propBiomass = 1.0;
            double currentN = LinkedNativeFoodType.GreenNitrogen;
            // NABSA changes N by 0.8 for particular months. Not needed here as decay included.
            double currentDMD = currentN * LinkedNativeFoodType.NToDMDCoefficient + LinkedNativeFoodType.NToDMDIntercept;
            currentDMD = Math.Max(LinkedNativeFoodType.MinimumDMD, currentDMD);
            LinkedNativeFoodType.Pools.Clear();

            List<GrazeFoodStorePool> newPools = new List<GrazeFoodStorePool>();

            while (includedMonthCount < 5)
            {
                if (month == 0)
                {
                    month = 12;
                }

                if (month <= 3 | month >= 11)
                {
                    // add new pool
                    newPools.Add(new GrazeFoodStorePool()
                    {
                        Age = monthCount,
                        Nitrogen = currentN,
                        DMD = currentDMD,
                        StartingAmount = propBiomass
                    });
                    includedMonthCount++;
                }
                propBiomass *= 1 - LinkedNativeFoodType.DetachRate;
                currentN -= LinkedNativeFoodType.DecayNitrogen;
                currentN = Math.Max(currentN, LinkedNativeFoodType.MinimumNitrogen);
                currentDMD *= 1 - LinkedNativeFoodType.DecayDMD;
                currentDMD = Math.Max(currentDMD, LinkedNativeFoodType.MinimumDMD);
                monthCount++;
                month--;
            }

            // assign pasture biomass to pools based on proportion of total
            double total = newPools.Sum(a => a.StartingAmount);
            foreach (var pool in newPools)
            {
                pool.Set(amountToAdd * (pool.StartingAmount / total));
            }

            // Previously: remove this months growth from pool age 0 to keep biomass at approximately setup.
            // But as updates happen at the end of the month, the fist months biomass is never added so stay with 0 or delete following section
            // Get this months growth
            // Get this months pasture data from the pasture data list
            if (PastureDataList != null)
            {
                PastureDataType pasturedata = PastureDataList.Where(a => a.Year == Clock.StartDate.Year && a.Month == Clock.StartDate.Month).FirstOrDefault();

                double thisMonthsGrowth = pasturedata.Growth * Area;
                if (thisMonthsGrowth > 0)
                {
                    GrazeFoodStorePool thisMonth = newPools.Where(a => a.Age == 0).FirstOrDefault() as GrazeFoodStorePool;
                    if (thisMonth != null)
                    {
                        thisMonth.Set(Math.Max(0, thisMonth.Amount - thisMonthsGrowth));
                    }
                }
            }

            // Add to pasture. This will add pool to pasture available store.
            foreach (var pool in newPools)
            {
                string reason = "Initialise";
                if(newPools.Count()>1)
                {
                    reason = "Initialise pool " + pool.Age.ToString();
                }
                LinkedNativeFoodType.Add(pool, this, reason);
            }
        }

        private double CalculateStockingRateRightNow()
        {
            if (Resources.RuminantHerd() != null)
            {
                string paddock = FeedTypeName;
                if(paddock.Contains("."))
                {
                    paddock = paddock.Substring(paddock.IndexOf(".")+1);
                }
                return Resources.RuminantHerd().Herd.Where(a => a.Location == paddock).Sum(a => a.AdultEquivalent) / (Area * unitsOfArea2Ha * ha2sqkm);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Method to perform calculation of all ecological indicators.
        /// </summary>
        private void CalculateEcologicalIndicators()
        {

            //If it is time to do yearly calculation
            if (ZoneCLEM.IsEcologicalIndicatorsCalculationMonth())
            {
                // Calculate change in Land Condition index and Grass basal area
                double utilisation = LinkedNativeFoodType.PercentUtilisation;

                LandConditionIndex.Modify(utilisation);
                LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex = LandConditionIndex.Value;
                GrassBasalArea.Modify(utilisation);
                LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea = GrassBasalArea.Value;

                // Calculate average monthly stocking rate
                // Check number of months to use
                int monthdiff = ((ZoneCLEM.EcologicalIndicatorsNextDueDate.Year - Clock.StartDate.Year) * 12) + ZoneCLEM.EcologicalIndicatorsNextDueDate.Month - Clock.StartDate.Month+1;
                if (monthdiff >= ZoneCLEM.EcologicalIndicatorsCalculationInterval)
                {
                    monthdiff = ZoneCLEM.EcologicalIndicatorsCalculationInterval;
                }
                LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate = StockingRateSummed / monthdiff;

                //perennials
                LinkedNativeFoodType.CurrentEcologicalIndicators.Perennials = 92.2 * (1 - Math.Pow(LandConditionIndex.Value, 3.35) / Math.Pow(LandConditionIndex.Value, 3.35 + 137.7)) - 2.2;

                //%utilisation
                LinkedNativeFoodType.CurrentEcologicalIndicators.Utilisation = utilisation;

                // Reset running total for stocking rate
                StockingRateSummed = 0;

                // calculate averages
                LinkedNativeFoodType.CurrentEcologicalIndicators.Cover /= monthdiff;
                LinkedNativeFoodType.CurrentEcologicalIndicators.TreeBasalArea /= monthdiff;

                //TreeC
                // I didn't include the / area as tba is already per ha. I think NABSA has this wrong
                LinkedNativeFoodType.CurrentEcologicalIndicators.TreeCarbon = LinkedNativeFoodType.CurrentEcologicalIndicators.TreeBasalArea * 6286 * 0.46;

                //methane
                //soilC
                //Burnkg
                //methaneFire
                //N2OFire

                //Get the new Pasture Data using the new Ecological Indicators (ie. GrassBA, LandCon, StRate)
                GetPastureDataList_TodayToNextEcolCalculation();

            }
        }

        /// <summary>
        /// From GRASP File get all the Pasture Data from today to the next Ecological Calculation
        /// </summary>
        private void GetPastureDataList_TodayToNextEcolCalculation()
        {
            // In IAT it only updates the GrassBA, LandCon and StockingRate (Ecological Indicators) 
            // every so many months (specified by  not every month.
            // And the month they are updated on each year is whatever the starting month was for the run.

            // Shaun's code. back to front from NABSA
            //pkGrassBA = (int)(Math.Round(grassBasalArea / 2, 0) * 2); //weird way but this is how NABSA does it.
            //pkLandCon = (int)(Math.Round((landConditionIndex - 1.1) / 2, 0) * 2 + 1);
            //
            // No reason for this grouping so just round.
            //
            // NABSA
            //pkLandCon = (int)(Math.Round(landConditionIndex / 2, 0) * 2); //weird way but this is how NABSA does it.
            //pkGrassBA = (int)(Math.Round((grassBasalArea - 1.1) / 2, 0) * 2 + 1);

            PastureDataList = FileGRASP.GetIntervalsPastureData(ZoneCLEM.ClimateRegion, soilIndex,
               LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea, LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex, LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate, Clock.Today.AddDays(1), ZoneCLEM.EcologicalIndicatorsCalculationInterval);
        }

        // Method to listen for land use transactions 
        // This allows this activity to dynamically respond when use available area is selected
        private void LinkedLandItem_TransactionOccurred(object sender, EventArgs e)
        {
            Area = LinkedLandItem.AreaAvailable;
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
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
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
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">";
            if (FeedTypeName == null || FeedTypeName == "")
            {
                html += "<span class=\"errorlink\">[PASTURE TYPE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + FeedTypeName + "</span>";
            }
            html += " occupies ";
            Land parentLand = null;
            if (LandTypeNameToUse != null && LandTypeNameToUse != "")
            {
                parentLand = Apsim.Find(this, LandTypeNameToUse.Split('.')[0]) as Land;
            }

            if (UseAreaAvailable)
            {
                html += "the unallocated portion of ";
            }
            else
            {
                if (parentLand == null)
                {
                    html += "<span class=\"setvalue\">" + AreaRequested.ToString("#,##0.###") + "</span> <span class=\"errorlink\">[UNITS NOT SET]</span> of ";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + AreaRequested.ToString("#,##0.###") + "</span> " + parentLand.UnitsOfArea + " of ";
                }
            }
            if (LandTypeNameToUse == null || LandTypeNameToUse == "")
            {
                html += "<span class=\"errorlink\">[LAND NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + LandTypeNameToUse + "</span>";
            }
            html += "</div>";

            html += "\n<div class=\"activityentry\">";
            html += "The simulation starts with <span class=\"setvalue\">" + StartingAmount.ToString("#,##0.##") + "</span> kg/ha";
            html += "</div>";

            return html;
        }
    }
}
