using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

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
    public class PastureActivityManage: CLEMActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;
        [Link]
        IFileGRASP FileGRASP = null;
        [Link]
        ZoneCLEM ZoneCLEM = null;
 
        /// <summary>
        /// Name of land type where pasture is located
        /// </summary>
        [Description("Land type where pasture is located")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of land type where pasture is located required")]
        public string LandTypeNameToUse { get; set; }

        /// <summary>
        /// Name of the pasture type to use
        /// </summary>
        [Description("Name of pasture")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of pasture required")]
        public string FeedTypeName { get; set; }

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
        public Relationship LandConditionIndex { get; set; }
        private int pkLandCon = 0; //rounded integer value used as primary key in GRASP file.

        /// <summary>
        /// Grass basal area
        /// </summary>
        [XmlIgnore]
        public Relationship GrassBasalArea { get; set; }

        /// <summary>
        /// Perennials
        /// </summary>
        [XmlIgnore]
        public double Perennials { get; set; }

        /// <summary>
        /// Area requested
        /// </summary>
        [Description("Area requested")]
        [Required, GreaterThanEqualValue(0)]
        public double AreaRequested { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
        public GrazeFoodStoreType LinkedNativeFoodType { get; set; }

        /// <summary>
        /// Conversion of simulation units of area to hectares
        /// </summary>
        private double unitsOfArea2Ha;

        // private properties
        private int pkGrassBA = 0; //rounded integer value used as primary key in GRASP file.
        private int soilIndex = 0; // obtained from LandType used
        private double StockingRateSummed;  //summed since last Ecological Calculation.
        private int pkStkRate = 0; //rounded integer value used as primary key in GRASP file.

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
            // Get Land condition relationship from children
            LandConditionIndex = Apsim.Children(this, typeof(Relationship)).Where(a => a.Name == "LandConditionIndex").FirstOrDefault() as Relationship;
            if (LandConditionIndex == null)
            {
                string[] memberNames = new string[] { "LandConditionIndexRelationship" };
                results.Add(new ValidationResult("Unable to locate Land Condition Index relationship in user interface", memberNames));
            }
            // Get Grass basal area relationship from children
            GrassBasalArea = Apsim.Children(this, typeof(Relationship)).Where(a => a.Name == "GrassBasalArea").FirstOrDefault() as Relationship;
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
            if (Area == 0 & AreaRequested > 0)
            {
                ResourceRequestList = new List<ResourceRequest>();
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = AreaRequested,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandTypeNameToUse,
                    ActivityModel = this,
                    Reason = "Assign",
                    FilterDetails = null
                }
                );
            }
            // if we get here we assume some land has been supplied
            if (ResourceRequestList != null || ResourceRequestList.Count() > 0)
            {
                gotLandRequested = TakeResources(ResourceRequestList, false);
            }

            //Now the Land has been allocated we have an Area 
            if (gotLandRequested)
            {            
                //get the units of area for this run from the Land resource parent.
                unitsOfArea2Ha = Resources.Land().UnitsOfAreaToHaConversion;

                // locate Pasture Type resource
                LinkedNativeFoodType = Resources.GetResourceItem(this, typeof(GrazeFoodStore), FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;

                //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                Area = ResourceRequestList.FirstOrDefault().Provided;

                LinkedNativeFoodType.Area = Area;

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

        /// <summary>An event handler to allow us to get next supply of pasture</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMUpdatePasture")]
        private void OnCLEMUpdatePasture(object sender, EventArgs e)
        {
            if (PastureDataList != null)
            {            
                double growth = 0;

                // method is performed on last day of month but needs to work with next month's details
                int year = Clock.Today.AddDays(1).Year;
                int month = Clock.Today.AddDays(1).Month;

                //Get this months pasture data from the pasture data list
                PastureDataType pasturedata = PastureDataList.Where(a => a.Year == year && a.Month == month).FirstOrDefault();

                growth = pasturedata.Growth;
                //TODO: check units from input files.
                // convert from kg/ha to kg/area unit
                growth = growth * unitsOfArea2Ha;

                if (growth > 0)
                {
                    GrazeFoodStorePool newPasture = new GrazeFoodStorePool();
                    newPasture.Age = 0;
                    newPasture.Set(growth * Area);  
                    newPasture.Nitrogen = this.LinkedNativeFoodType.GreenNitrogen; 
                    newPasture.DMD = newPasture.Nitrogen * LinkedNativeFoodType.NToDMDCoefficient + LinkedNativeFoodType.NToDMDIntercept;
                    newPasture.DMD = Math.Min(100,Math.Max(LinkedNativeFoodType.MinimumDMD, newPasture.DMD));
                    this.LinkedNativeFoodType.Add(newPasture,this.Name,"Growth");
                }
            }
            else
            {
                throw new Exception("No pasture data");
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

        /// <summary>An event handler to allow us to clear pools.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // decay N and DMD of pools and age by 1 month
            foreach (var pool in LinkedNativeFoodType.Pools)
            {
                pool.Reset();
            }
        }

        /// <summary>
        /// Function to age resource pools
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            // decay N and DMD of pools and age by 1 month
            foreach (var pool in LinkedNativeFoodType.Pools)
            {
                // N is a loss of N% (x = x -loss)
                pool.Nitrogen = Math.Max(pool.Nitrogen - LinkedNativeFoodType.DecayNitrogen, LinkedNativeFoodType.MinimumNitrogen);
                // DMD is a proportional loss (x = x*(1-proploss))
                pool.DMD = Math.Max(pool.DMD * (1 - LinkedNativeFoodType.DecayDMD), LinkedNativeFoodType.MinimumDMD);

                double detach = LinkedNativeFoodType.CarryoverDetachRate;
                if (pool.Age < 12)
                {
                    detach = LinkedNativeFoodType.DetachRate;
                    pool.Age++;
                }
                double detachedAmount = pool.Amount * (1 - detach);
                pool.Set(detachedAmount);
                pool.Detached = detachedAmount;
            }

            //// combine all pools >= 12 months old.
            //var pools12 = LinkedNativeFoodType.Pools.Where(a => a.Age >= 12);
            //if(pools12.Count() > 1)
            //{
            //    while(pools12.Count() > 1)
            //    {


            //    }

            //}

            // remove all pools with less than 10g of food
            LinkedNativeFoodType.Pools.RemoveAll(a => a.Amount < 0.01);
        }

        private void SetupStartingPasturePools(double StartingGrowth)
        {
            // Initial biomass
            double amountToAdd = Area * StartingGrowth;
            if (amountToAdd <= 0) return;

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
                if (month == 0) month = 12;
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
                pool.Growth = amountToAdd * (pool.StartingAmount / total);
            }

            // Previously: remove this months growth from pool age 0 to keep biomass at approximately setup.
            // But as updates happen at the end of the month, the fist months biomass is never added so stay with 0 or delete following section
            // Get this months growth
            //Get this months pasture data from the pasture data list
            PastureDataType pasturedata = PastureDataList.Where(a => a.Year == Clock.StartDate.Year && a.Month == Clock.StartDate.Month).FirstOrDefault();
            //double growth = ; // GRASPFile.Get(xxxxxxxxx)

            double thisMonthsGrowth = pasturedata.Growth;
            if (thisMonthsGrowth > 0)
            {
                GrazeFoodStorePool thisMonth = newPools.Where(a => a.Age == 0).FirstOrDefault() as GrazeFoodStorePool;
                if (thisMonth != null)
                {
                    thisMonth.Set(Math.Max(0, thisMonth.Amount - thisMonthsGrowth));
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
                LinkedNativeFoodType.Add(pool, this.Name, reason);
            }
        }

        private double CalculateStockingRateRightNow()
        {
            if (Resources.RuminantHerd() != null)
                return Resources.RuminantHerd().Herd.Where(a => a.Location == FeedTypeName).Sum(a => a.AdultEquivalent) / (Area * unitsOfArea2Ha * ha2sqkm);
            else
                return 0;
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
                int monthdiff = ((ZoneCLEM.EcologicalIndicatorsNextDueDate.Year - Clock.StartDate.Year) * 12) + ZoneCLEM.EcologicalIndicatorsNextDueDate.Month - Clock.StartDate.Month;
                if (monthdiff >= ZoneCLEM.EcologicalIndicatorsCalculationInterval)
                {
                    monthdiff = ZoneCLEM.EcologicalIndicatorsCalculationInterval;
                }
                LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate = StockingRateSummed / monthdiff;

                //erosion
                //tree basal area
                //perennials
                Perennials = 92.2 * (1 - Math.Pow(LandConditionIndex.Value, 3.35) / Math.Pow(LandConditionIndex.Value, 3.35 + 137.7)) - 2.2;
                //%runoff
                //methane
                //soilC
                //TreeC
                //Burnkg
                //methaneFire
                //NOxFire
                //%utilisation
                LinkedNativeFoodType.CurrentEcologicalIndicators.Utilisation = utilisation;

                // Reset running total for stocking rate
                StockingRateSummed = 0;

                //Get the new Pasture Data using the new Ecological Indicators (ie. GrassBA, LandCon, StRate)
                GetPastureDataList_TodayToNextEcolCalculation();

            }
        }


        /// <summary>
        /// From GRASP File get all the Pasture Data from today to the next Ecological Calculation
        /// </summary>
        private void GetPastureDataList_TodayToNextEcolCalculation()
        {
            //In IAT it only updates the GrassBA, LandCon and StockingRate (Ecological Indicators) 
            // every so many months (specified by  not every month.
            //And the month they are updated on each year is whatever the starting month was for the run.

            //round the doubles to nearest integers so can be used as primary key
            double landConditionIndex = LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex;
            double grassBasalArea = LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea;

            // Shaun's code. back to front from NABSA
            //pkGrassBA = (int)(Math.Round(grassBasalArea / 2, 0) * 2); //weird way but this is how NABSA does it.
            //pkLandCon = (int)(Math.Round((landConditionIndex - 1.1) / 2, 0) * 2 + 1);
            //
            // No reason for this grouping so just round.
            //
            // NABSA
            //pkLandCon = (int)(Math.Round(landConditionIndex / 2, 0) * 2); //weird way but this is how NABSA does it.
            //pkGrassBA = (int)(Math.Round((grassBasalArea - 1.1) / 2, 0) * 2 + 1);

            pkLandCon = (int)(Math.Round(landConditionIndex, 0));
            pkLandCon = Math.Min(10, Math.Max(0, pkLandCon));
            pkGrassBA = (int)(Math.Round(grassBasalArea, 0));
            pkGrassBA = Math.Min(6, Math.Max(1, pkGrassBA));

            double stockingRate = LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate;
            pkStkRate = (int)Math.Round(stockingRate);
            pkStkRate = Math.Min(70, Math.Max(1, pkStkRate));

            PastureDataList = FileGRASP.GetIntervalsPastureData(ZoneCLEM.ClimateRegion, soilIndex,
               pkGrassBA, pkLandCon, pkStkRate, Clock.Today.AddDays(1), ZoneCLEM.EcologicalIndicatorsCalculationInterval);
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }

}
