using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd management activity</summary>
    /// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyTreeView")]
    [PresenterName("UserInterface.Presenters.PropertyTreePresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs the management of ruminant numbers based upon the current herd filtering. It requires a RuminantActivityBuySell to undertake the purchases and sales.")]
    [Version(1, 0, 8, "Reworking of rules to better allow small herd management.")]
    [Version(1, 0, 7, "Added ability to turn on/off marking max age breeders and sires and age/weight males for sale and allow this action in other activities")]
    [Version(1, 0, 6, "Allow user to specify individuals that should be sold to reduce herd before young emales taken")]
    [Version(1, 0, 5, "Renamed all 'bulls' to 'sires' in properties. Requires resetting of values")]
    [Version(1, 0, 4, "Allow sires to be placed in different pasture to breeders")]
    [Version(1, 0, 3, "Allows herd to be adjusted to sires and max breeders kept at startup")]
    [Version(1, 0, 2, "Implements minimum breeders kept to define breeder purchase limits")]
    [Version(1, 0, 1, "First implementation of this activity using IAT/NABSA processes")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantManage.htm")]
    public class RuminantActivityManage : CLEMRuminantActivityBase, IValidatableObject
    {
        private int maxBreeders;
        private int minBreeders;

        /// <summary>
        /// Maximum number of breeders that can be kept
        /// </summary>
        [Category("Herd size", "Breeding females")]
        [Description("Maximum number of female breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumBreedersKept { get; set; } 

        /// <summary>
        /// Minimum number of breeders that can be kept
        /// </summary>
        [Category("Herd size", "Breeding females")]
        [Description("Minimum number of female breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MinimumBreedersKept { get; set; }

        /// <summary>
        /// Include the marking for sale of old breeders in this activity
        /// </summary>
        [Category("Destock", "Breeding females")]
        [Description("Mark old breeding females for sale")]
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool MarkOldBreedersForSale { get; set; }

        /// <summary>
        /// Maximum breeder age (months) for removal
        /// </summary>
        [Category("Destock", "Breeding females")]
        [Description("Maximum female breeder age (months) for removal")]
        [Required, GreaterThanEqualValue(0)]
        [System.ComponentModel.DefaultValueAttribute(120)]
        public double MaximumBreederAge { get; set; }

        /// <summary>
        /// Proportion of min breeders in single purchase
        /// </summary>
        [Category("Restock", "Breeding females")]
        [Description("Proportion of min female breeders in single purchase")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Proportion, GreaterThanEqualValue(0)]
        public double MaximumProportionBreedersPerPurchase { get; set; }

        /// <summary>
        /// The number of 12 month age classes to spread breeder purchases across
        /// </summary>
        [Category("Restock", "Breeding females")]
        [Description("Number of age classes to distribute female breeder purchases across")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Range(1, 4)]
        public int NumberOfBreederPurchaseAgeClasses { get; set; }

        /// <summary>
        /// Maximum number of breeding sires kept
        /// </summary>
        [Category("Herd size", "Breeding males")]
        [Description("Maximum number of male breeders kept")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumSiresKept { get; set; }

        /// <summary>
        /// Calculated sires kept
        /// </summary>
        [JsonIgnore]
        public int SiresKept { get; set; }

        /// <summary>
        /// Include the marking for sale of sires in this activity
        /// </summary>
        [Category("Destock", "Breeding males")]
        [Description("Mark old sires for sale")]
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool MarkOldSiresForSale { get; set; }

        /// <summary>
        /// Maximum sire age (months) for removal
        /// </summary>
        [Category("Destock", "Breeding males")]
        [Description("Maximum sire age (months) for removal")]
        [Required, GreaterThanEqualValue(0)]
        [System.ComponentModel.DefaultValueAttribute(120)]
        public double MaximumSireAge { get; set; }

        /// <summary>
        /// Sire age (months) at purchase
        /// </summary>
        [Category("Restock", "Breeding males")]
        [Description("Sire age (months) at purchase")]
        [System.ComponentModel.DefaultValueAttribute(48)]
        [Required, GreaterThanValue(0)]
        public double SireAgeAtPurchase { get; set; }

        /// <summary>
        /// Allow natural herd replacement of sires
        /// </summary>
        [Category("Restock", "Breeding males")]
        [Description("Allow male breeder replacement from herd")]
        [Required]
        public bool AllowSireReplacement { get; set; }

        /// <summary>
        /// Set sire herd to purchase relative to proportion of breeder herd present
        /// </summary>
        [Category("Restock", "Breeding males")]
        [Description("Restock sire numbers relative to proportion of breeders")]
        [Required]
        public bool RestockSiresRelativeToBreeders { get; set; }

        /// <summary>
        /// Maximum number of sires in a single purchase
        /// </summary>
        [Category("Restock", "Breeding males")]
        [Description("Maximum number of male breeders in a single purchase")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumSiresPerPurchase { get; set; }

        /// <summary>
        /// Identify males for sale every time step
        /// </summary>
        [Category("Grow out herd", "General")]
        [Description("Mark those reaching age/weight for sale every time step")]
        [Required]
        public bool ContinuousMaleSales { get; set; }

        /// <summary>
        /// Include the marking for sale of males reaching age or weight
        /// </summary>
        [Category("Grow out herd", "Males")]
        [Description("Perform growing out of young males")]
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool MarkAgeWeightMalesForSale { get; set; }

        /// <summary>
        /// Male selling age (months)
        /// </summary>
        [Category("Grow out herd", "Males")]
        [Description("Male selling age (months)")]
        [System.ComponentModel.DefaultValueAttribute(24)]
        [Required, GreaterThanEqualValue(0)]
        public double MaleSellingAge { get; set; }

        /// <summary>
        /// Male selling weight (kg)
        /// </summary>
        [Category("Grow out herd", "Males")]
        [Description("Male selling weight (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double MaleSellingWeight { get; set; }

        /// <summary>
        /// Perform selling of young females the same as males
        /// </summary>
        [Category("Grow out herd", "Females")]
        [Description("Perform growing out of young females")]
        [Required]
        public bool SellFemalesLikeMales { get; set; }

        /// <summary>
        /// Female selling age (months)
        /// </summary>
        [Category("Grow out herd", "Females")]
        [Description("Female grow out selling age (months)")]
        [System.ComponentModel.DefaultValueAttribute(24)]
        [Required, GreaterThanEqualValue(0)]
        public double FemaleSellingAge { get; set; }

        /// <summary>
        /// Female selling weight (kg)
        /// </summary>
        [Category("Grow out herd", "Females")]
        [Description("Female grow out selling weight (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double FemaleSellingWeight { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchased sires in for grazing
        /// </summary>
        [Category("Restock", "Pasture")]
        [Description("GrazeFoodStore (paddock) to place purchased sires in")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreNameSires { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchased breeders in for grazing
        /// </summary>
        [Category("Restock", "Pasture")]
        [Description("GrazeFoodStore (paddock) to place purchased breeders in")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreNameBreeders { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place grow out heifers in for grazing
        /// </summary>
        [Category("Grow out herd", "Pasture")]
        [Description("GrazeFoodStore (paddock) to place grow out females in")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreNameGrowOutFemales { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place grow out young males in for grazing
        /// </summary>
        [Category("Grow out herd", "Pasture")]
        [Description("GrazeFoodStore (paddock) to place grow out males in")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreNameGrowOutMales { get; set; }

        private string grazeStoreSires = "";
        private string grazeStoreBreeders = "";
        private string grazeStoreGrowOutFemales = "";
        private string grazeStoreGrowOutMales = "";

        /// <summary>
        /// Minimum pasture (kg/ha) before restocking if placed in paddock
        /// </summary>
        [Category("Restock", "Pasture")]
        [Description("Minimum pasture (kg/ha) before restocking if placed in paddock")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double MinimumPastureBeforeRestock { get; set; }

        /// <summary>
        /// Fill breeding males up to required amount at startup
        /// </summary>
        [Category("Start up", "Breeding females")]
        [Description("Adjust breeding females at start-up")]
        public bool AdjustBreedingFemalesAtStartup { get; set; }

        /// <summary>
        /// Fill breeding males up to required amount at startup
        /// </summary>
        [Category("Start up", "Breeding males")]
        [Description("Adjust breeding sires at start-up")]
        public bool AdjustBreedingMalesAtStartup { get; set; }


        /// <summary>
        /// Store graze for sires
        /// </summary>
        private GrazeFoodStoreType foodStoreSires;

        /// <summary>
        /// Store graze for breeders
        /// </summary>
        private GrazeFoodStoreType foodStoreBreeders;

        /// <summary>
        /// Store graze for sires
        /// </summary>
        private GrazeFoodStoreType foodStoreGrowOutFemales;

        /// <summary>
        /// Store graze for breeders
        /// </summary>
        private GrazeFoodStoreType foodStoreGrowOutMales;

        /// <summary>
        /// Breed params for this activity
        /// </summary>
        private RuminantType breedParams;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityManage()
        {
            this.SetDefaults();
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

            if (12 + (NumberOfBreederPurchaseAgeClasses - 1) * 12 >= MaximumBreederAge)
            {
                string[] memberNames = new string[] { "NumberOfBreederPurchaseAgeClasses" };
                results.Add(new ValidationResult("The number of age classes (12 months each) to spread breeder purchases across will exceed the maximum age of breeders. Reduce number of breeder age classes", memberNames));
            }
            return results;
        } 
        #endregion

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // reset min breeders if set greater than max breeders
            // this allows multi run versions to only consider maxbreeders
            // if value is changed it is updated with the experiment so use private
            minBreeders = (this.MinimumBreedersKept > this.MaximumBreedersKept) ? this.MaximumBreedersKept : this.MinimumBreedersKept;

            // create local version of max breeders so we can modify without affecting user set value
            maxBreeders = Math.Max(this.MaximumBreedersKept, minBreeders);

            this.InitialiseHerd(false, true);
            breedParams = Resources.GetResourceItem(this, typeof(RuminantHerd), this.PredictedHerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            decimal breederHerdSize = 0;

            if (AdjustBreedingFemalesAtStartup)
            {
                RuminantHerd herd = Resources.RuminantHerd();
                List<Ruminant> rumHerd = this.CurrentHerd(false);
                if (rumHerd != null && rumHerd.Count() > 0)
                {
                    int numberAdded = 0;
                    RuminantType breedParams = rumHerd.FirstOrDefault().BreedParams;
                    RuminantInitialCohorts cohorts = rumHerd.FirstOrDefault().BreedParams.FindAllChildren<RuminantInitialCohorts>().FirstOrDefault() as RuminantInitialCohorts;

                    if (cohorts != null)
                    {
                        int heifers = 0;// Convert.ToInt32(cohorts.FindAllChildren<RuminantTypeCohort>().Where(a => a.Gender == Sex.Female && (a.Age >= 12 & a.Age < breedParams.MinimumAge1stMating)).Sum(a => a.Number));
                        List<RuminantTypeCohort> cohortList = cohorts.FindAllChildren<RuminantTypeCohort>().Where(a => a.Gender == Sex.Female && (a.Age >= breedParams.MinimumAge1stMating & a.Age <= this.MaximumBreederAge)).ToList();
                        int initialBreeders = Convert.ToInt32(cohortList.Sum(a => a.Number));
                        if (initialBreeders < (minBreeders-heifers))
                        {
                            double scaleFactor = (minBreeders-heifers) / Convert.ToDouble(initialBreeders);
                            // add new individuals
                            foreach (var item in cohortList)
                            {
                                int numberToAdd = Convert.ToInt32(Math.Round(item.Number * scaleFactor) - item.Number);
                                foreach (var newind in item.CreateIndividuals(numberToAdd))
                                {
                                    newind.SaleFlag = HerdChangeReason.FillInitialHerd;
                                    herd.AddRuminant(newind, this);
                                    numberAdded++;
                                }
                            }
                            if (numberAdded == 0)
                            {
                                throw new ApsimXException(this, $"Unable to scale breeding female population up to the maximum breeders kept at startup\r\nNo cohorts representing breeders were found in the initial herd structure [r=InitialCohorts] for [r={breedParams.Name}]\r\nAdd at least one initial cohort that meets the breeder criteria of age at first mating and max age kept");
                            }
                            breederHerdSize = initialBreeders + numberAdded;
                        }
                        else if (initialBreeders > (maxBreeders - heifers))
                        {
                            int reduceBy = Math.Max(0, initialBreeders - maxBreeders - heifers);
                            // reduce initial herd size
                            // randomly select the individuals to remove form the breeder herd
                            List<Ruminant> breeders = rumHerd.Where(a => a.Gender == Sex.Female && a.Age > breedParams.MinimumAge1stMating && a.Age < this.MaximumBreederAge).OrderBy(x => Guid.NewGuid()).Take(reduceBy).ToList();
                            foreach (var item in breeders)
                            {
                                item.SaleFlag = HerdChangeReason.ReduceInitialHerd;
                                herd.RemoveRuminant(item, this);
                                reduceBy--;
                            }

                            if (reduceBy > 0)
                            {
                                // add warning
                                string warn = $"Unable to reduce breeders at the start of the simulation to number required [{maxBreeders}] using [a={this.Name}]";
                                if (!Warnings.Exists(warn))
                                {
                                    Summary.WriteWarning(this, warn);
                                    Warnings.Add(warn);
                                }
                            }
                            breederHerdSize = maxBreeders;
                        }
                    }
                    else
                    {
                        throw new ApsimXException(this, $"Unable to adjust breeding female population to the maximum breeders kept at startup\r\nNo initial herd structure [r=InitialCohorts] has been provided in [r={breedParams.Name}]");
                    }
                }
            }

            // max sires
            if (MaximumSiresKept < 1 & MaximumSiresKept > 0)
            {
                SiresKept = Convert.ToInt32(Math.Ceiling(maxBreeders * breederHerdSize), CultureInfo.InvariantCulture);
            }
            else
            {
                SiresKept = Convert.ToInt32(Math.Truncate(MaximumSiresKept), CultureInfo.InvariantCulture);
            }

            if(AdjustBreedingMalesAtStartup)
            {
                RuminantHerd herd = Resources.RuminantHerd();
                if (herd != null)
                {
                    // get number in herd
                    List<Ruminant> rumHerd = this.CurrentHerd(false);
                    int numberPresent = rumHerd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.IsSire).Count();
                    if (numberPresent < SiresKept)
                    {
                        // fill to number needed
                        for (int i = numberPresent; i < SiresKept; i++)
                        {
                            RuminantMale newSire = new RuminantMale(SireAgeAtPurchase, Sex.Male, 0, breedParams)
                            {
                                Breed = this.PredictedHerdBreed,
                                HerdName = this.PredictedHerdName,
                                Sire = true,
                                ID = herd.NextUniqueID,
                                SaleFlag = HerdChangeReason.FillInitialHerd
                            };
                            herd.AddRuminant(newSire, this);
                        }
                    }
                    else if(numberPresent > SiresKept)
                    {
                        // reduce initial herd.
                        int reduceBy = Math.Max(0,numberPresent - SiresKept);
                        // reduce initial sire herd size
                        // randomly select the individuals to remove form the breeder herd
                        List<RuminantMale> sires = rumHerd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.IsSire).OrderBy(x => Guid.NewGuid()).Take(reduceBy).ToList();
                        foreach (var item in sires)
                        {
                            item.SaleFlag = HerdChangeReason.ReduceInitialHerd;
                            herd.RemoveRuminant(item, this);
                            reduceBy--;
                        }

                        if (reduceBy > 0)
                        {
                            // add warning
                            string warn = $"Unable to reduce breeding sires at the start of the simulation to number required [{SiresKept}] using [a={this.Name}]";
                            if (!Warnings.Exists(warn))
                            {
                                Summary.WriteWarning(this, warn);
                                Warnings.Add(warn);
                            }
                        }
                    }
                }
            }

            // check GrazeFoodStoreExists for breeders
            grazeStoreBreeders = "";
            if(GrazeFoodStoreNameBreeders != null && !GrazeFoodStoreNameBreeders.StartsWith("Not specified"))
            {
                grazeStoreBreeders = GrazeFoodStoreNameBreeders.Split('.').Last();
                foodStoreBreeders = Resources.GetResourceItem(this, GrazeFoodStoreNameBreeders, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }

            // check for managed paddocks and warn if breeders placed in yards.
            if (grazeStoreBreeders == "" && this.MaximumProportionBreedersPerPurchase > 0)
            {
                var ah = this.FindInScope<ActivitiesHolder>();
                if(ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                {
                    Summary.WriteWarning(this, $"Breeders purchased by [a={this.Name}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]");
                }
            }

            // check GrazeFoodStoreExists for sires
            grazeStoreSires = "";
            if (GrazeFoodStoreNameSires != null && !GrazeFoodStoreNameSires.StartsWith("Not specified"))
            {
                grazeStoreSires = GrazeFoodStoreNameSires.Split('.').Last();
                foodStoreSires = Resources.GetResourceItem(this, GrazeFoodStoreNameSires, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }

            // check for managed paddocks and warn if sires placed in yards.
            if (grazeStoreSires == "" && this.SiresKept > 0)
            {
                var ah = this.FindInScope<ActivitiesHolder>();
                if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                {
                    Summary.WriteWarning(this, $"Sires purchased by [a={this.Name}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]");
                }
            }

            // check GrazeFoodStoreExists for grow out males
            if (MarkAgeWeightMalesForSale)
            {
                grazeStoreGrowOutMales = "";
                if (GrazeFoodStoreNameGrowOutMales != null && !GrazeFoodStoreNameGrowOutMales.StartsWith("Not specified"))
                {
                    grazeStoreGrowOutMales = GrazeFoodStoreNameGrowOutMales.Split('.').Last();
                    foodStoreGrowOutMales = Resources.GetResourceItem(this, GrazeFoodStoreNameGrowOutMales, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
                }

                // check for managed paddocks and warn if sires placed in yards.
                if (grazeStoreGrowOutMales == "")
                {
                    var ah = this.FindInScope<ActivitiesHolder>();
                    if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                    {
                        Summary.WriteWarning(this, $"Males grown out before sale by [a={this.Name}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]");
                    }
                }

                if (SellFemalesLikeMales)
                {
                    grazeStoreGrowOutFemales = "";
                    if (GrazeFoodStoreNameGrowOutFemales != null && !GrazeFoodStoreNameGrowOutFemales.StartsWith("Not specified"))
                    {
                        grazeStoreGrowOutFemales = GrazeFoodStoreNameGrowOutFemales.Split('.').Last();
                        foodStoreGrowOutFemales = Resources.GetResourceItem(this, GrazeFoodStoreNameGrowOutFemales, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
                    }

                    // check for managed paddocks and warn if sires placed in yards.
                    if (grazeStoreGrowOutFemales == "")
                    {
                        var ah = this.FindInScope<ActivitiesHolder>();
                        if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                        {
                            Summary.WriteWarning(this, $"Females grown out before sale by [a={this.Name}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]");
                        }
                    }
                }
            }
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalManage")]
        private void OnCLEMAnimalManage(object sender, EventArgs e)
        {
            this.Status = ActivityStatus.NoTask;
            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            // remove only the individuals that are affected by this activity.
            // these are old purchases that were not made. This list will be regenerated in this method.
            ruminantHerd.PurchaseIndividuals.RemoveAll(a => a.Breed == this.PredictedHerdBreed);

            List<Ruminant> herd = this.CurrentHerd(true).Where(a => !a.TagExists("GrowOut")).ToList();
            List<Ruminant> growOutHerd = new List<Ruminant>();

            // can sell off males any month as per NABSA
            // if we don't need this monthly, then it goes into next if statement with herd declaration
            // NABSA MALES - weaners, 1-2, 2-3 and 3-4 yo, we check for any male weaned and not a breeding sire.
            // check for sell age/weight of young males
            // if SellYoungFemalesLikeMales then all apply to both sexes else only males.
            // SellFemalesLikeMales will grow out excess heifers until age/weight rather than sell immediately.
            if (MarkAgeWeightMalesForSale && (this.TimingOK || ContinuousMaleSales))
            {
                this.Status = ActivityStatus.NotNeeded;
                // tag all grow out and move to pasture specified
                //var check = herd.Where(a => a.Weaned && (a.Gender == Sex.Female ? (SellFemalesLikeMales && (a as RuminantFemale).IsPreBreeder) : !(a as RuminantMale).IsSire) && !a.Tags.Contains("GrowOut"));
                foreach (var ind in herd.Where(a => (a.Gender == Sex.Female ? (SellFemalesLikeMales && (a as RuminantFemale).IsPreBreeder) : !(a as RuminantMale).IsSire) && !a.ReplacementBreeder && !a.TagExists("GrowOut")))
                {
                    ind.Location = ((ind is RuminantFemale) ? grazeStoreGrowOutFemales : grazeStoreGrowOutMales);
                    ind.TagAdd("GrowOut");
                }

                growOutHerd = this.CurrentHerd(true).Where(a => a.TagExists("GrowOut")).ToList();

                // identify those ready for sale
                foreach (var ind in growOutHerd.Where(a => (a.Age >= ((a is RuminantMale)? MaleSellingAge: FemaleSellingAge) || a.Weight >= ((a is RuminantMale) ? MaleSellingWeight : FemaleSellingWeight)) ))
                {
                    this.Status = ActivityStatus.Success;
                    ind.SaleFlag = HerdChangeReason.AgeWeightSale;
                    this.Status = ActivityStatus.Success;
                }
            }

            // if management month
            if (this.TimingOK)
            {
                this.Status = ActivityStatus.NotNeeded;
                // ensure pasture limits are ok before purchases
                bool sufficientFoodBreeders = true;
                bool sufficientFoodSires = true;
                if (foodStoreBreeders != null)
                {
                    sufficientFoodBreeders = (foodStoreBreeders.TonnesPerHectare * 1000) >= MinimumPastureBeforeRestock;
                }
                if (foodStoreSires != null)
                {
                    sufficientFoodSires = (foodStoreSires.TonnesPerHectare * 1000) >= MinimumPastureBeforeRestock;
                }

                // check for maximum age if permitted
                foreach (var ind in herd.Where(a => ((a.Gender == Sex.Female) ? MarkOldBreedersForSale : MarkOldSiresForSale) &&  a.Age >= ((a.Gender == Sex.Female) ? MaximumBreederAge : MaximumSireAge)) )
                {
                    ind.SaleFlag = HerdChangeReason.MaxAgeSale;
                    this.Status = ActivityStatus.Success;

                    // ensure females are not pregnant and add warning if pregnant old females found.
                    if (ind.Gender == Sex.Female && (ind as RuminantFemale).IsPregnant)
                    {
                        string warning = "Some females sold at maximum age in [a=" + this.Name + "] were pregnant.\r\nConsider changing the MaximumBreederAge in [a=RuminantActivityManage] or ensure [r=RuminantType.MaxAgeMating] is Gestation length less than the MaximumBreederAge to avoid selling pregnant individuals.";
                        if (!Warnings.Exists(warning))
                        {
                            Warnings.Add(warning);
                            Summary.WriteWarning(this, warning);
                        }
                    }
                }

                // MALES
                // check for sires after sale of old individuals and buy/sell
                int numberMaleSiresInHerd = herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.None).Cast<RuminantMale>().Where(a => a.IsSire).Count();

                // Number of females
                // weaned, >breeding age, female
                int numberFemaleBreedingInHerd = herd.Where(a => a.Gender == Sex.Female && a.SaleFlag == HerdChangeReason.None && a.Weaned && a.Age >= a.BreedParams.MinimumAge1stMating ).Count();
                int numberFemaleTotalInHerd = herd.Where(a => a.Gender == Sex.Female && a.SaleFlag == HerdChangeReason.None).Count();

                // these are the breeders already marked for sale
                // don't include those marked as max age sale as these can't be considered excess female
                int numberFemaleMarkedForSale = herd.Where(a => a.Gender == Sex.Female && (a as RuminantFemale).IsBreeder & a.SaleFlag != HerdChangeReason.None & a.SaleFlag != HerdChangeReason.MaxAgeSale).Count();

                // defined heifers here as weaned and will be a breeder in the next year
                // we should not include those individuals > 12 months before reaching breeder age
                List<RuminantFemale> preBreeders = herd.Where(a => a.Gender == Sex.Female && a.Weaned && (a.Age - a.BreedParams.MinimumAge1stMating > -11) && a.Age < a.BreedParams.MinimumAge1stMating && !a.TagExists("GrowOut")).Cast<RuminantFemale>().ToList();
                int numberFemaleHeifersInHerd = preBreeders.Count();

                // adjust males sires
                if (numberMaleSiresInHerd > SiresKept)
                {
                    // sell sires
                    // What rule? oldest first as they may be lost soonest?
                    int numberToRemove = numberMaleSiresInHerd - SiresKept;
                    if (numberToRemove > 0)
                    {
                        foreach (var male in herd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.IsSire).OrderByDescending(a => a.Age).Take(numberToRemove).Take(numberToRemove))
                        {
                            male.Location = grazeStoreSires;
                            male.SaleFlag = HerdChangeReason.ExcessSireSale;
                            this.Status = ActivityStatus.Success;
                            numberToRemove--;
                        }
                    }
                }
                else if(numberMaleSiresInHerd < SiresKept)
                {
                    if ((foodStoreSires == null) || (sufficientFoodSires))
                    {
                        // limit by breeders as proportion of max breeders so we don't spend alot on sires when building the herd and females more valuable
                        double propOfBreeders = 1;
                        if (RestockSiresRelativeToBreeders)
                        {
                            propOfBreeders = Math.Max(1, (double)numberFemaleBreedingInHerd / (double)maxBreeders);
                        }

                        int sires = Convert.ToInt32(Math.Ceiling(SiresKept * propOfBreeders), CultureInfo.InvariantCulture);
                        int numberToBuy = Math.Min(MaximumSiresPerPurchase, Math.Max(0, sires - numberMaleSiresInHerd));

                        if (AllowSireReplacement)
                        {
                            //TODO: Add selection of particular individuals based on quality

                            // remove young males from sale herd to replace breeding sires (not those sold because too old)
                            // only consider individuals that will mature in next 12 months
                            foreach (RuminantMale male in herd.Where(a => a.Gender == Sex.Male && a.Weaned && (a.Age - a.BreedParams.MinimumAge1stMating > -11) && (a.SaleFlag == HerdChangeReason.AgeWeightSale || a.SaleFlag == HerdChangeReason.None) && !(a as RuminantMale).IsCastrated).OrderByDescending(a => a.Age * a.Weight).Take(numberToBuy))
                            {
                                male.Location = grazeStoreSires;
                                male.SaleFlag = HerdChangeReason.None;
                                male.TagRemove("GrowOut");
                                male.ReplacementBreeder = true;
                                numberMaleSiresInHerd++;
                                numberToBuy--;
                            }
                            // if still insufficent, look into current gorwing out herd for replacement
                            // try get best male from grow out herd (not castrated)
                            // only consider individuals that will mature in next 12 months
                            foreach (RuminantMale male in growOutHerd.Where(a => a.Gender == Sex.Male && a.Weaned && (a.Age - a.BreedParams.MinimumAge1stMating > -11) && !(a as RuminantMale).IsCastrated).OrderByDescending(a => a.Age * a.Weight).Take(numberToBuy))
                            {
                                male.Location = grazeStoreSires;
                                male.SaleFlag = HerdChangeReason.None;
                                male.TagRemove("GrowOut");
                                male.ReplacementBreeder = true;
                                numberMaleSiresInHerd++;
                                numberToBuy--;
                            }

                            // we still don't have enough sires. 
                            // we can now move to buy or if purchasing is off we'll need to set aside a number of younger males and wait for them to grow

                            // remaining males assumed to be too small, so await next time-step
                        }

                        // if still insufficient buy sires.
                        if (numberToBuy > 0 && MaximumSiresPerPurchase>0)
                        {
                            for (int i = 0; i < numberToBuy; i++)
                            {
                                if (i < MaximumSiresPerPurchase)
                                {
                                    this.Status = ActivityStatus.Success;

                                    RuminantMale newSire = new RuminantMale(SireAgeAtPurchase, Sex.Male, 0, breedParams)
                                    {
                                        Location = grazeStoreSires,
                                        Breed = this.PredictedHerdBreed,
                                        HerdName = this.PredictedHerdName,
                                        Sire = true,
                                        Gender = Sex.Male,
                                        ID = 0, // Next unique id will be assigned when added
                                        SaleFlag = HerdChangeReason.SirePurchase
                                    };

                                    // add to purchase request list and await purchase in Buy/Sell
                                    ruminantHerd.PurchaseIndividuals.Add(newSire);
                                }
                            }
                        }
                    }
                }

                // FEMALES
                // Breeding herd sold as young (weaned to min age first mater) only, purchased as breeders (>= minAge1stMating)
                // TODO: allow purchase of pregtested females.
                // Feb2020 - Added ability to provide destocking groups to try and sell non heifer breeders before reverting to heifer sales.
                int excessBreeders = 0;

                // get the mortality rate for the herd if available or assume zero
                double mortalityRate = breedParams.MortalityBase;

                // shortfall between actual and desired numbers of breeders (-ve for shortfall)
                excessBreeders = numberFemaleBreedingInHerd - maxBreeders;
                
                // IAT-NABSA removes adjusts to account for the old animals that will be sold in the next year
                // This is not required in CLEM as they have been sold in this method, and it wont be until this method is called again that the next lot are sold.
                // Like IAT-NABSA we will account for mortality losses in the next year in our breeder purchases
                // Account for whole individuals only.

                excessBreeders -= numberFemaleMarkedForSale;

                // calculate the mortality of the remaining + purchases
                int numberDyingInNextYear = 0;
                numberDyingInNextYear +=  Convert.ToInt32(Math.Floor((Math.Max(0,numberFemaleBreedingInHerd + excessBreeders)) * mortalityRate), CultureInfo.InvariantCulture);
                //  include mortality of heifers added
                numberDyingInNextYear += Convert.ToInt32(Math.Floor(Math.Max(0,numberFemaleHeifersInHerd - ((excessBreeders>0)?-excessBreeders:0)) * mortalityRate), CultureInfo.InvariantCulture);

                // account for heifers already in the herd
                // These are the next cohort that will become breeders in the next 12 months (before this method is called again)
                excessBreeders += numberFemaleHeifersInHerd;

                // adjust for future mortality over 1 year
                excessBreeders -= numberDyingInNextYear;

                if (excessBreeders > 0) // surplus heifers to sell
                {
                    this.Status = ActivityStatus.Success;

                    // go through any ruminant filter groups and try and sell herd
                    // this allows the user to sell old females over young breeders and heifers if required. 
                    // must be female (warning raised here if males included)
                    // remove individuals to sale as specified by destock groups
                    foreach (RuminantGroup item in FindAllChildren<RuminantGroup>())
                    {
                        // works with current filtered herd to obey filtering.
                        List<Ruminant> herdToSell = herd.Filter(item);
                        int cnt = 0;
                        while (cnt < herdToSell.Count() && excessBreeders > 0)
                        {
                            if (herd[cnt] is RuminantFemale)
                            {
                                if ((herd[cnt] as RuminantFemale).IsBreeder)
                                {
                                    if (herd[cnt].SaleFlag != HerdChangeReason.ExcessBreederSale)
                                    {
                                        herd[cnt].SaleFlag = HerdChangeReason.ExcessBreederSale;
                                        excessBreeders--;
                                    }
                                }
                                else
                                {
                                    // warning trying to sell non-breeder to reduce breeder herd
                                    string warn = $"The [f={item.Name}] filter group used to reduce breeder numbers in [a={this.Name}] includes non-breeding females.\r\nThese individuals will be ignored.";
                                    if (!Warnings.Exists(warn))
                                    {
                                        Summary.WriteWarning(this, warn);
                                        Warnings.Add(warn);
                                    }
                                }
                            }
                            else
                            {
                                // warning trying to sell a male to reduce breeder herd
                                string warn = $"The [f={item.Name}] filter group used to reduce breeder numbers in [a={this.Name}] includes males.\r\nThese individuals will be ignored.";
                                if (!Warnings.Exists(warn))
                                {
                                    Summary.WriteWarning(this, warn);
                                    Warnings.Add(warn);
                                }
                            }
                            cnt++;
                        }
                    }

                    // we have now removed all the breeders allowed

                    // if still excess start reducing the young pre-breeder female pool
                    // some young will take multiple years before reaching breeding age

                    // remove any pre-breeder and becoming a breeder before next herd management (thus adding to breeder pool)
                    //foreach (var female in herd.Where(a => a.Gender == Sex.Female && (a as RuminantFemale).IsHeifer && a.Age >= a.BreedParams.MinimumAge1stMating & !a.Tags.Contains("GrowHeifer")).OrderByDescending(a => a.Age))
                    foreach (var female in preBreeders.Where(a => a.SaleFlag == HerdChangeReason.None).OrderByDescending(a => a.Age).Take(excessBreeders))
                    {
                        // tag for sale.
                        female.SaleFlag = HerdChangeReason.ExcessPreBreederSale;
                        excessBreeders--;
                    }

                    // any additional excess cannot be solved so herd will be larger than desired
                }
                else if (excessBreeders < 0) // shortfall breeders to buy
                {
                    double minBreedAge = breedParams.MinimumAge1stMating;
                    excessBreeders *= -1;
                    if ((foodStoreBreeders == null) || (sufficientFoodBreeders))
                    {
                        // remove females from sale herd to replace breeders (not those sold because too old)
                        if (excessBreeders > 0)
                        {
                            // can only remove those that will make breeder age in the next year before management
                            // includes suitable pre-breeders
                            foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female && (a.Age - a.BreedParams.MinimumAge1stMating > -11) && a.SaleFlag != HerdChangeReason.AgeWeightSale && a.SaleFlag != HerdChangeReason.None).OrderByDescending(a => a.Weight * a.Age).Take(excessBreeders))
                            {
                                // keep by removing any tag for sale.
                                female.SaleFlag = HerdChangeReason.None;
                                female.Location = grazeStoreBreeders;
                                excessBreeders--;
                                this.Status = ActivityStatus.Success;
                            }
                        }

                        // remove grow out heifers from grow out if of breeding in next year age
                        if (SellFemalesLikeMales & excessBreeders > 0)
                        {
                            foreach (Ruminant female in herd.Where(a => a.Gender == Sex.Female && (a.Age - a.BreedParams.MinimumAge1stMating > -11) && a.TagExists("GrowOut")).OrderByDescending(a => a.Weight * a.Age).Take(excessBreeders))
                            {
                                female.TagRemove("GrowOut");
                                female.SaleFlag = HerdChangeReason.None;
                                female.Location = grazeStoreBreeders;
                                excessBreeders--;
                                this.Status = ActivityStatus.Success;
                            }
                        }

                        // if still insufficient and permitted, buy breeders.
                        if (excessBreeders > 0 && (MaximumProportionBreedersPerPurchase > 0))
                        {
                            // recalculate based on minbreeders kept for purchases
                            // this limit is only applied to purchases, not herd replacement to max breeders kept
                            int limitedExcessBreeders = Math.Max(0, excessBreeders - (maxBreeders - minBreeders));
                            // adjust mortality for new level
                            if(limitedExcessBreeders < excessBreeders)
                            {
                                int notDead = Convert.ToInt32(Math.Floor((excessBreeders-limitedExcessBreeders) * mortalityRate), CultureInfo.InvariantCulture);
                                excessBreeders = Math.Max(0,limitedExcessBreeders - notDead);
                            }

                            int ageOfBreeder = 0;

                            // IAT-NABSA had buy mortality base% more to account for deaths before these individuals grow to breeding age
                            // These individuals are already of breeding age so we will ignore this in CLEM
                            // minimum of (max kept x prop in single purchase) and (the number needed + annual mortality)
                            int numberToBuy = Math.Min(excessBreeders,Convert.ToInt32(Math.Ceiling(MaximumProportionBreedersPerPurchase*minBreeders), CultureInfo.InvariantCulture));
                            int numberPerPurchaseCohort = Convert.ToInt32(Math.Ceiling(numberToBuy / Convert.ToDouble(NumberOfBreederPurchaseAgeClasses, CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);

                            int numberBought = 0;
                            while(numberBought < numberToBuy)
                            {
                                this.Status = ActivityStatus.Success;

                                int breederClass = Convert.ToInt32(numberBought / numberPerPurchaseCohort, CultureInfo.InvariantCulture);
                                ageOfBreeder = Convert.ToInt32(minBreedAge + (breederClass * 12), CultureInfo.InvariantCulture);

                                RuminantFemale newBreeder = new RuminantFemale(ageOfBreeder, Sex.Female, 0, breedParams)
                                {
                                    Location = grazeStoreBreeders,
                                    Breed = this.PredictedHerdBreed,
                                    HerdName = this.PredictedHerdName,
                                    BreedParams = breedParams,
                                    Gender = Sex.Female,
                                    ID = 0,
                                    SaleFlag = HerdChangeReason.BreederPurchase
                                };
                                // weight will be set to normalised weight as it was assigned 0 at initialisation
                                newBreeder.PreviousWeight = newBreeder.Weight;

                                // this individual must be weaned to be permitted to start breeding.
                                newBreeder.Wean(false, "Initial");
                                // add to purchase request list and await purchase in Buy/Sell
                                ruminantHerd.PurchaseIndividuals.Add(newBreeder);
                                numberBought++;
                                excessBreeders--;
                            }
                        }
                    }
                }

                // Breeders themselves don't get sold unless specified in destocking groups below this activity. Sales is with pre-breeders (e.g. Heifers)
                // Only excess pre-breeders either be marked as GrowOut like males or sold as an excess heifer if becoming a breeder within the next cycle of management
                // Breeders can be sold in seasonal and ENSO destocking.
                // The destocking groups will define the order individuals are sold
                // Pre-breeders are kept from oldest/heaviest, or most healthy (age x weight descending) so that any not sold are closest to being breeders
                // What rule? oldest first as they may be lost soonest
                // should keep pregnant females... and young...
                // this will currently remove pregnant females and females with suckling calf
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
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
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
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

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string skippedMarkForSale = (!MarkAgeWeightMalesForSale | !MarkOldBreedersForSale | !MarkOldSiresForSale) ? "*" : "";

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activitybannerlight\">Breeding females</div>");
                htmlWriter.Write("\r\n<div class=\"activitycontentlight\">");
                htmlWriter.Write("\r\n<div class=\"activityentry\">");

                double minimumBreedersKept = Math.Min(MinimumBreedersKept, MaximumBreedersKept);

                int maxBreed = Math.Max(MinimumBreedersKept, MaximumBreedersKept);
                htmlWriter.Write("The herd will be maintained");
                if (minimumBreedersKept == 0)
                {
                    htmlWriter.Write(" using only natural recruitment up to <span class=\"setvalue\">" + MaximumBreedersKept.ToString("#,###") + "</span> breeders");
                }
                else if (minimumBreedersKept == maxBreed)
                {
                    htmlWriter.Write(" with breeder purchases and natural recruitment up to <span class=\"setvalue\">" + minimumBreedersKept.ToString("#,###") + "</span > breeders");
                }
                else
                {
                    htmlWriter.Write(" with breeder purchases up to <span class=\"setvalue\">" + minimumBreedersKept.ToString("#,###") + "</span > and natural recruitment to <span class=\"setvalue\">" + maxBreed.ToString("#,###") + "</span> breeders");
                }
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (MarkOldBreedersForSale)
                {
                    htmlWriter.Write("Individuals will be sold when over <span class=\"setvalue\">" + MaximumBreederAge.ToString("###") + "</span> months old");
                }
                else
                {
                    htmlWriter.Write($"Old breeders will not be marked for sale{skippedMarkForSale}");
                }
                htmlWriter.Write("</div>");
                if (MaximumProportionBreedersPerPurchase < 1 & minimumBreedersKept > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("A maximum of <span class=\"setvalue\">" + MaximumProportionBreedersPerPurchase.ToString("#0.##%") + "</span> of the Minimum Breeders Kept can be purchased in a single transaction");
                    htmlWriter.Write("</div>");
                }
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activitybannerlight\">Breeding males (sires/rams etc)</div>");
                htmlWriter.Write("\r\n<div class=\"activitycontentlight\">");
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (MaximumSiresKept == 0)
                {
                    htmlWriter.Write("No breeding sires will be kept");
                }
                else if (MaximumSiresKept < 1)
                {
                    htmlWriter.Write("The number of breeding males will be determined as <span class=\"setvalue\">" + MaximumSiresKept.ToString("###%") + "</span> of the maximum female breeder herd. Currently <span class=\"setvalue\">" + (Convert.ToInt32(Math.Ceiling(MaximumBreedersKept * MaximumSiresKept), CultureInfo.InvariantCulture).ToString("#,##0")) + "</span> individuals");
                }
                else
                {
                    htmlWriter.Write("A maximum of <span class=\"setvalue\">" + MaximumSiresKept.ToString("#,###") + "</span> will be kept");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (MarkOldSiresForSale)
                {
                    htmlWriter.Write("Individuals will be sold when over <span class=\"setvalue\">" + MaximumSireAge.ToString("###") + "</span> months old");
                }
                else
                {
                    htmlWriter.Write($"Old sires will not be marked for sale{skippedMarkForSale}");
                }

                htmlWriter.Write("</div>");
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activitybannerlight\">General herd</div>");
                htmlWriter.Write("\r\n<div class=\"activitycontentlight\">");
                if (MarkAgeWeightMalesForSale || MaleSellingAge + MaleSellingWeight > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Males will be sold when <span class=\"setvalue\">" + MaleSellingAge.ToString("###") + "</span> months old or <span class=\"setvalue\">" + MaleSellingWeight.ToString("#,###") + "</span> kg");
                    htmlWriter.Write("</div>");
                    if (ContinuousMaleSales)
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">");
                        htmlWriter.Write("Young male age/weight sales will be performed in any month where conditions are met");
                        htmlWriter.Write("</div>");
                    }
                    else
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">");
                        htmlWriter.Write("Young male age/weight sales will only be performed when this activity is due");
                        htmlWriter.Write("</div>");
                    }
                    if (SellFemalesLikeMales)
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">");
                        htmlWriter.Write("Females will be sold the same as males");
                        htmlWriter.Write("</div>");
                    }
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"Individuals will not be marked for sale when reaching age or weight{skippedMarkForSale}");
                    htmlWriter.Write("</div>");
                }
                htmlWriter.Write("</div>");

                if (skippedMarkForSale.Length > 0)
                {
                    htmlWriter.Write("<br />* This activity is not marking all individuals for sale when conditions met. It is your responsibility to ensure old individuals and age or weight sales of young males are handled either by turning on the associated feature on in this activitiy or using a RuminantActivityMarkForSale activity.");
                }

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Purchased breeders will be placed in ");
                if (GrazeFoodStoreNameBreeders == null || GrazeFoodStoreNameBreeders == "")
                {
                    htmlWriter.Write("<span class=\"resourcelink\">General yards</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreNameBreeders + "</span>");
                    if (MinimumPastureBeforeRestock > 0)
                    {
                        htmlWriter.Write(" with no restocking while pasture is below <span class=\"setvalue\">" + MinimumPastureBeforeRestock.ToString() + "</span> kg/ha");
                    }
                }
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Purchased sires will be placed in ");
                if (GrazeFoodStoreNameSires == null || GrazeFoodStoreNameSires == "")
                {
                    htmlWriter.Write("<span class=\"resourcelink\">General yards</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreNameSires + "</span>");
                    if (MinimumPastureBeforeRestock > 0)
                    {
                        htmlWriter.Write(" with no restocking while pasture is below <span class=\"setvalue\">" + MinimumPastureBeforeRestock.ToString() + "</span> kg/ha");
                    }
                }
                htmlWriter.Write("</div>");

                if (FindAllChildren<RuminantGroup>().Any())
                {
                    htmlWriter.Write("\r\n<div style=\"margin-top:10px;\" class=\"activitygroupsborder\">");
                    htmlWriter.Write("<div class=\"labournote\">Any Ruminant Filter Group below will determine which breeders (and in which order) will be sold prior to selling heifers in order to reduce the breeder herd size. Note: You are responsible for ensuring these filter groups can be applied to a list of breeders.</div>");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            return "";
        } 
        #endregion

    }
}
