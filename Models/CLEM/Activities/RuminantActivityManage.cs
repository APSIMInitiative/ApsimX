using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.Xml.Serialization;
using System.Globalization;

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
    [Version(1, 0, 5, "Renamed all 'bulls' to 'sires' in properties. Requires resetting of values")]
    [Version(1, 0, 4, "Allow sires to be placed in different pasture to breeders")]
    [Version(1, 0, 3, "Allows herd to be adjusted to sires and max breeders kept at startup")]
    [Version(1, 0, 2, "Implements minimum breeders kept to define breeder purchase limits")]
    [Version(1, 0, 1, "First implementation of this activity using IAT/NABSA processes")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantManage.htm")]
    public class RuminantActivityManage : CLEMRuminantActivityBase, IValidatableObject
    {
        private int maxBreeders;

        /// <summary>
        /// Maximum number of breeders that can be kept
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Maximum number of female breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumBreedersKept { get; set; } 

        /// <summary>
        /// Minimum number of breeders that can be kept
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Minimum number of female breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MinimumBreedersKept { get; set; }

        /// <summary>
        /// Maximum breeder age (months) for culling
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Maximum female breeder age (months) for culling")]
        [Required, GreaterThanEqualValue(0)]
        [System.ComponentModel.DefaultValueAttribute(120)]
        public double MaximumBreederAge { get; set; }

        /// <summary>
        /// Proportion of min breeders in single purchase
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Proportion of min female breeders in single purchase")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Proportion, GreaterThanEqualValue(0)]
        public double MaximumProportionBreedersPerPurchase { get; set; }

        /// <summary>
        /// The number of 12 month age classes to spread breeder purchases across
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Number of age classes to distribute female breeder purchases across")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Range(1, 4)]
        public int NumberOfBreederPurchaseAgeClasses { get; set; }

        /// <summary>
        /// Fill breeding males up to required amount at startup
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Adjust breeding females at start-up")]
        public bool AdjustBreedingFemalesAtStartup { get; set; }

        /// <summary>
        /// Maximum number of breeding sires kept
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Maximum number of male breeders kept")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumSiresKept { get; set; }

        /// <summary>
        /// Calculated sires kept
        /// </summary>
        [XmlIgnore]
        public int SiresKept { get; set; }

        /// <summary>
        /// Maximum sire age (months) for culling
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Maximum sire age (months) for culling")]
        [Required, GreaterThanEqualValue(0)]
        [System.ComponentModel.DefaultValueAttribute(120)]
        public double MaximumSireAge { get; set; }

        /// <summary>
        /// Sire age (months) at purchase
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Sire age (months) at purchase")]
        [System.ComponentModel.DefaultValueAttribute(48)]
        [Required, GreaterThanValue(0)]
        public double SireAgeAtPurchase { get; set; }

        /// <summary>
        /// Allow natural herd replacement of sires
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Allow male breeder replacement from herd")]
        [Required]
        public bool AllowSireReplacement { get; set; }

        /// <summary>
        /// Maximum number of sires in a single purchase
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Maximum number of male breeders in a single purchase")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumSiresPerPurchase { get; set; }

        /// <summary>
        /// Fill breeding males up to required amount at startup
        /// </summary>
        [Category("General", "Breeding males")]
        [Description("Adjust breeding sires to number kept at start-up")]
        public bool AdjustBreedingMalesAtStartup { get; set; }

        /// <summary>
        /// Male selling age (months)
        /// </summary>
        [Category("General", "Males")]
        [Description("Male selling age (months)")]
        [System.ComponentModel.DefaultValueAttribute(24)]
        [Required, GreaterThanEqualValue(0)]
        public double MaleSellingAge { get; set; }

        /// <summary>
        /// Male selling weight (kg)
        /// </summary>
        [Category("General", "Males")]
        [Description("Male selling weight (kg)")]
        [Required]
        public double MaleSellingWeight { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchased sires in for grazing
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("GrazeFoodStore (paddock) to place purchased sires in")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreNameSires { get; set; }

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchased breeders in for grazing
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("GrazeFoodStore (paddock) to place purchased breeders in")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreNameBreeders { get; set; }

        private string grazeStoreSires = "";
        private string grazeStoreBreeders = "";

        /// <summary>
        /// Minimum pasture (kg/ha) before restocking if placed in paddock
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("Minimum pasture (kg/ha) before restocking if placed in paddock")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double MinimumPastureBeforeRestock { get; set; }

        /// <summary>
        /// Perform selling of young females the same as males
        /// </summary>
        [Category("General", "Breeding females")]
        [Description("Perform selling of young females the same as males")]
        [Required]
        public bool SellFemalesLikeMales { get; set; }

        /// <summary>
        /// Identify males for sale every time step
        /// </summary>
        [Category("General", "Males")]
        [Description("Identify males for sale every time step")]
        [Required]
        public bool ContinuousMaleSales { get; set; }

        /// <summary>
        /// Store graze for sires
        /// </summary>
        private GrazeFoodStoreType foodStoreSires;

        /// <summary>
        /// Store graze for breeders
        /// </summary>
        private GrazeFoodStoreType foodStoreBreeders;

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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // create local version of max breeders so we can modify without affecting user set value
            maxBreeders = Math.Max(this.MaximumBreedersKept, this.MinimumBreedersKept);

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
                    RuminantInitialCohorts cohorts = Apsim.Children(rumHerd.FirstOrDefault().BreedParams, typeof(RuminantInitialCohorts)).FirstOrDefault() as RuminantInitialCohorts;

                    if (cohorts != null)
                    {
                        List<RuminantTypeCohort> cohortList = Apsim.Children(cohorts, typeof(RuminantTypeCohort)).Cast<RuminantTypeCohort>().Where(a => a.Gender == Sex.Female && (a.Age >= breedParams.MinimumAge1stMating & a.Age <= this.MaximumBreederAge)).ToList();
                        int initialBreeders = Convert.ToInt32(cohortList.Sum(a => a.Number));
                        if (initialBreeders < this.MinimumBreedersKept)
                        {
                            double scaleFactor = this.MinimumBreedersKept / Convert.ToDouble(initialBreeders);
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
                                throw new ApsimXException(this, $"Unable to scale breeding female population up to the maximum breeders kept at startup\nNo cohorts representing breeders were found in the initial herd structure [r=InitialCohorts] for [r={breedParams.Name}]\nAdd at least one initial cohort that meets the breeder criteria of age at first mating and max age kept");
                            }
                            breederHerdSize = initialBreeders + numberAdded;
                        }
                        else if (initialBreeders > maxBreeders)
                        {
                            int reduceBy = Math.Max(0, initialBreeders - maxBreeders);
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
                        throw new ApsimXException(this, $"Unable to adjust breeding female population to the maximum breeders kept at startup\nNo initial herd structure [r=InitialCohorts] has been provided in [r={breedParams.Name}]");
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
                    int numberPresent = rumHerd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).Count();
                    if (numberPresent < SiresKept)
                    {
                        // fill to number needed
                        for (int i = numberPresent; i < SiresKept; i++)
                        {
                            RuminantMale newSire = new RuminantMale(SireAgeAtPurchase, Sex.Male, 0, breedParams)
                            {
                                Breed = this.PredictedHerdBreed,
                                HerdName = this.PredictedHerdName,
                                BreedingSire = true,
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
                        List<RuminantMale> sires = rumHerd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).OrderBy(x => Guid.NewGuid()).Take(reduceBy).ToList();
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
                var ah = Apsim.Find(this, typeof(ActivitiesHolder));
                if(Apsim.ChildrenRecursively(ah, typeof(PastureActivityManage)).Count() != 0)
                {
                    Summary.WriteWarning(this, String.Format("Breeders purchased by [a={0}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until mustered and will require feeding while in yards.\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]", this.Name));
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
            if (grazeStoreBreeders == "" && this.SiresKept > 0)
            {
                var ah = Apsim.Find(this, typeof(ActivitiesHolder));
                if (Apsim.ChildrenRecursively(ah, typeof(PastureActivityManage)).Count() != 0)
                {
                    Summary.WriteWarning(this, String.Format("Sires purchased by [a={0}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until mustered and will require feeding while in yards.\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]", this.Name));
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

            List<Ruminant> herd = this.CurrentHerd(true);

            // can sell off males any month as per NABSA
            // if we don't need this monthly, then it goes into next if statement with herd declaration
            // NABSA MALES - weaners, 1-2, 2-3 and 3-4 yo, we check for any male weaned and not a breeding sire.
            // check for sell age/weight of young males
            // if SellYoungFemalesLikeMales then all apply to both sexes else only males.
            // SellFemalesLikeMales will grow out excess heifers until age/weight rather than sell immediately.
            if (this.TimingOK || ContinuousMaleSales)
            {
                this.Status = ActivityStatus.NotNeeded;
                foreach (var ind in herd.Where(a => a.Weaned && (SellFemalesLikeMales ? true : (a.Gender == Sex.Male)) && (a.Age >= MaleSellingAge || a.Weight >= MaleSellingWeight)))
                {
                    bool sell = true;
                    if (ind.GetType() == typeof(RuminantMale))
                    {
                        // don't sell breeding sires.
                        sell = !((ind as RuminantMale).BreedingSire);
                    }
                    else
                    {
                        // only sell females that were marked as excess
                        sell = ind.Tags.Contains("GrowHeifer");
                    }

                    if (sell)
                    {
                        this.Status = ActivityStatus.Success;
                        ind.SaleFlag = HerdChangeReason.AgeWeightSale;
                    }
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

                // check for maximum age (females and males have different cutoffs)
                foreach (var ind in herd.Where(a => a.Age >= ((a.Gender == Sex.Female) ? MaximumBreederAge : MaximumSireAge)))
                {
                    ind.SaleFlag = HerdChangeReason.MaxAgeSale;

                    // ensure females are not pregnant and add warning if pregnant old females found.
                    if (ind.Gender == Sex.Female && (ind as RuminantFemale).IsPregnant)
                    {
                        string warning = "Some females sold at maximum age in [a=" + this.Name + "] were pregnant.\nConsider changing the MaximumBreederAge in [a=RuminantActivityManage] or ensure [r=RuminantType.MaxAgeMating] is less than or equal to the MaximumBreederAge to avoid selling pregnant individuals.";
                        if(!Warnings.Exists(warning))
                        {
                            Warnings.Add(warning);
                            Summary.WriteWarning(this, warning);
                        }
                    }
                }

                // MALES
                // check for sires after sale of old individuals and buy/sell
                int numberMaleSiresInHerd = herd.Where(a => a.Gender == Sex.Male && a.SaleFlag == HerdChangeReason.None).Cast<RuminantMale>().Where(a => a.BreedingSire).Count();

                // Number of females
                int numberFemaleBreedingInHerd = herd.Where(a => a.Gender == Sex.Female && a.Age >= a.BreedParams.MinimumAge1stMating && a.SaleFlag == HerdChangeReason.None).Count();
                int numberFemaleTotalInHerd = herd.Where(a => a.Gender == Sex.Female && a.SaleFlag == HerdChangeReason.None).Count();

                // these are females that will exceed max age and be sold in next 12 months
                int numberFemaleOldInHerd = herd.Where(a => a.Gender == Sex.Female && (a.Age + 12 >= MaximumBreederAge) && a.SaleFlag == HerdChangeReason.None).Count();

                // defined heifers here as weaned and will be a breeder in the next year
                int numberFemaleHeifersInHerd = herd.Where(a => a.Gender == Sex.Female && a.Weaned && ((a.Age - a.BreedParams.MinimumAge1stMating < 0) && (a.Age - a.BreedParams.MinimumAge1stMating > -12)) && a.SaleFlag == HerdChangeReason.None).Count();

                if (numberMaleSiresInHerd > SiresKept)
                {
                    // sell sires
                    // What rule? oldest first as they may be lost soonest?
                    int numberToRemove = numberMaleSiresInHerd - SiresKept;
                    if (numberToRemove > 0)
                    {
                        foreach (var male in herd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).OrderByDescending(a => a.Age).Take(numberToRemove))
                        {
                            male.Location = grazeStoreSires;
                            male.SaleFlag = HerdChangeReason.ExcessSireSale;
                            this.Status = ActivityStatus.Success;
                            numberToRemove--;
                            if (numberToRemove == 0)
                            {
                                break;
                            }
                        }
                    }
                }
                else if(numberMaleSiresInHerd < SiresKept)
                {
                    if ((foodStoreSires == null) || (sufficientFoodSires))
                    {
                        if (AllowSireReplacement)
                        {
                            // remove young sires from sale herd to replace breed sires (not those sold because too old)
                            foreach (RuminantMale male in herd.Where(a => a.Gender == Sex.Male && a.SaleFlag == HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Weight))
                            {
                                male.Location = grazeStoreSires;
                                male.SaleFlag = HerdChangeReason.None;
                                male.BreedingSire = true;
                                numberMaleSiresInHerd++;
                                if (numberMaleSiresInHerd >= SiresKept)
                                {
                                    break;
                                }
                            }
                            // if still insufficent, look into current herd for replacement
                            // remaining males assumed to be too small, so await next time-step
                        }

                        // if still insufficient buy sires.
                        if (numberMaleSiresInHerd < SiresKept && (MaximumSiresPerPurchase>0))
                        {
                            // limit by breeders as proportion of max breeders so we don't spend alot on sires when building the herd and females more valuable
                            double propOfBreeders = (double)numberFemaleBreedingInHerd / (double)maxBreeders;
                            propOfBreeders = 1;

                            int sires = Convert.ToInt32(Math.Ceiling(SiresKept * propOfBreeders), CultureInfo.InvariantCulture);
                            int numberToBuy = Math.Min(MaximumSiresPerPurchase, Math.Max(0, sires - numberMaleSiresInHerd));

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
                                        BreedingSire = true,
                                        Gender = Sex.Male,
                                        ID = 0, // Next unique ide will be assigned when added
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
                // Breeding herd sold as heifers only, purchased as breeders (>= minAge1stMating)
                // Feb2020 - Added ability to provide desticking groups to try and sell non heifer breeders before reverting to heifer sales.
                int excessBreeders = 0;

                // get the mortality rate for the herd if available or assume zero
                double mortalityRate = breedParams.MortalityBase;

                // shortfall between actual and desired numbers of breeders (-ve for shortfall)
                excessBreeders = numberFemaleBreedingInHerd - maxBreeders;
                // IAT-NABSA removes adjusts to account for the old animals that will be sold in the next year
                // This is not required in CLEM as they have been sold in this method, and it wont be until this method is called again that the next lot are sold.
                // Like IAT-NABSA we will account for mortality losses in the next year in our breeder purchases
                // Account for whole individuals only.
                int numberDyingInNextYear = Convert.ToInt32(Math.Floor(numberFemaleBreedingInHerd * mortalityRate), CultureInfo.InvariantCulture);
                // adjust for future mortality
                excessBreeders -= numberDyingInNextYear;

                // account for heifers already in the herd
                // These are the next cohort that will become breeders in the next 12 months (before this method is called again)
                excessBreeders += numberFemaleHeifersInHerd;

                // if negative - i.e. purchases needed limit to min breeders kept not max breeders kept
                if(excessBreeders < 0)
                {
                    excessBreeders = Math.Min(0, numberFemaleBreedingInHerd - MinimumBreedersKept - numberDyingInNextYear + numberFemaleHeifersInHerd);
                }

                if (excessBreeders > 0) // surplus heifers to sell
                {
                    this.Status = ActivityStatus.Success;

                    // go through any ruminant filter groups and try and sell herd
                    // this allows the user to sell old females over young breeders and heifers if required. 
                    // must be female (check that males not included in validation)
                    // remove individuals to sale as specified by destock groups
                    foreach (RuminantDestockGroup item in this.Children.Where(a => a.GetType() == typeof(RuminantDestockGroup)))
                    {
                        // works with current filtered herd to obey filtering.
                        List<Ruminant> herdToSell = herd.Filter(item);
                        int cnt = 0;
                        while (cnt < herdToSell.Count() && excessBreeders > 0)
                        {
                            if(herd[cnt].SaleFlag != HerdChangeReason.ExcessBreederSale)
                            {
                                herd[cnt].SaleFlag = HerdChangeReason.ExcessBreederSale;
                                excessBreeders--;
                            }
                            cnt++;
                        }
                    }

                    // now need to manage heifers to herd size or make for sale like males. 
                    foreach (var female in herd.Where(a => a.Gender == Sex.Female &&  (a as RuminantFemale).IsHeifer).Take(excessBreeders))
                    {
                        // if sell like males tag for grow out otherwise mark for sale
                        if (SellFemalesLikeMales)
                        {
                            if (!female.Tags.Contains("GrowHeifer"))
                            {
                                female.Tags.Add("GrowHeifer");
                            }
                        }
                        else
                        {
                            // tag for sale.
                            female.SaleFlag = HerdChangeReason.ExcessHeiferSale;
                        }
                        excessBreeders--;
                        if (excessBreeders == 0)
                        {
                            break;
                        }
                    }
                }
                else if (excessBreeders < 0) // shortfall breeders to buy
                {
                    double minBreedAge = breedParams.MinimumAge1stMating;
                    excessBreeders *= -1;
                    if ((foodStoreBreeders == null) || (sufficientFoodBreeders))
                    {
                        // remove grow out heifers from grow out herd to replace breeders
                        if (SellFemalesLikeMales)
                        {
                            foreach (Ruminant female in herd.Where(a => a.Tags.Contains("GrowHeifer")).OrderByDescending(a => a.Age))
                            {
                                female.Tags.Remove("GrowHeifer");
                                excessBreeders--;
                                if (excessBreeders == 0)
                                {
                                    break;
                                }
                            }
                        }

                        // remove young females from sale herd to replace breeders (not those sold because too old)
                        foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female && (a.SaleFlag == HerdChangeReason.AgeWeightSale || a.SaleFlag == HerdChangeReason.ExcessHeiferSale)).OrderByDescending(a => a.Age))
                        {
                            female.SaleFlag = HerdChangeReason.None;
                            excessBreeders--;
                            if (excessBreeders == 0)
                            {
                                break;
                            }
                        }

                        // if still insufficient buy breeders.
                        if (excessBreeders > 0 && (MaximumProportionBreedersPerPurchase > 0))
                        {
                            int ageOfBreeder = 0;

                            // IAT-NABSA had buy mortality base% more to account for deaths before these individuals grow to breeding age
                            // These individuals are already of breeding age so we will ignore this in CLEM
                            // minimum of (max kept x prop in single purchase) and (the number needed + annual mortality)
                            int numberToBuy = Math.Min(excessBreeders,Convert.ToInt32(Math.Ceiling(MaximumProportionBreedersPerPurchase*MinimumBreedersKept), CultureInfo.InvariantCulture));
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
                            }
                        }
                    }
                }
                // Breeders themselves don't get sold unless specified in destocking groups below this activity. Sales is with Heifers
                // Breeders can be sold in seasonal and ENSO destocking.
                // sell breeders
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
            html += "\n<div class=\"activitybannerlight\">Breeding females</div>";
            html += "\n<div class=\"activitycontentlight\">";
            html += "\n<div class=\"activityentry\">";

            int maxBreed = Math.Max(MinimumBreedersKept, MaximumBreedersKept);
            html += "The herd will be maintained";
            if (MinimumBreedersKept == 0)
            {
                html += " using only natural recruitment up to <span class=\"setvalue\">" + MaximumBreedersKept.ToString("#,###") + "</span> breeders";
            }
            else if(MinimumBreedersKept == maxBreed)
            {
                html += " with breeder purchases and natural recruitment up to <span class=\"setvalue\">" + MinimumBreedersKept.ToString("#,###") + "</span > breeders";
            }
            else
            {
                html += " with breeder purchases up to <span class=\"setvalue\">" + MinimumBreedersKept.ToString("#,###") + "</span > and natural recruitment to <span class=\"setvalue\">" + maxBreed.ToString("#,###") +"</span> breeders";
            }
            html += "</div>";

            html += "\n<div class=\"activityentry\">";
            html += "Individuals will be sold when over <span class=\"setvalue\">" + MaximumBreederAge.ToString("###") + "</span> months old";
            html += "</div>";
            if (MaximumProportionBreedersPerPurchase < 1 & MinimumBreedersKept > 0)
            {
                html += "\n<div class=\"activityentry\">";
                html += "A maximum of <span class=\"setvalue\">" + MaximumProportionBreedersPerPurchase.ToString("#0.##%") + "</span> of the Minimum Breeders Kept can be purchased in a single transaction";
                html += "</div>";
            }
            html += "</div>";

            html += "\n<div class=\"activitybannerlight\">Breeding males (sires/rams etc)</div>";
            html += "\n<div class=\"activitycontentlight\">";
            html += "\n<div class=\"activityentry\">";
            if (MaximumSiresKept == 0)
            {
                html += "No breeding sires will be kept";
            }
            else if (MaximumSiresKept < 1)
            {
                html += "The number of breeding males will be determined as <span class=\"setvalue\">" + MaximumSiresKept.ToString("###%") + "</span> of the maximum female breeder herd. Currently <span class=\"setvalue\">"+(Convert.ToInt32(Math.Ceiling(MaximumBreedersKept * MaximumSiresKept), CultureInfo.InvariantCulture).ToString("#,##0")) +"</span> individuals";
            }
            else
            {
                html += "A maximum of <span class=\"setvalue\">" + MaximumSiresKept.ToString("#,###") + "</span> will be kept";
            }
            html += "</div>";
            html += "\n<div class=\"activityentry\">";
            html += "Individuals will be sold when over <span class=\"setvalue\">" + MaximumSireAge.ToString("###") + "</span> months old";
            html += "</div>";
            html += "</div>";

            html += "\n<div class=\"activitybannerlight\">General herd</div>";
            html += "\n<div class=\"activitycontentlight\">";
            if (MaleSellingAge + MaleSellingWeight > 0)
            {
                html += "\n<div class=\"activityentry\">";
                html += "Males will be sold when <span class=\"setvalue\">" + MaleSellingAge.ToString("###") + "</span> months old or <span class=\"setvalue\">" + MaleSellingWeight.ToString("#,###") + "</span> kg";
                html += "</div>";
                if (ContinuousMaleSales)
                {
                    html += "\n<div class=\"activityentry\">";
                    html += "Animals will be sold in any month";
                    html += "</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">";
                    html += "Animals will be sold only when activity is due";
                    html += "</div>";
                }
                if (SellFemalesLikeMales)
                {
                    html += "\n<div class=\"activityentry\">";
                    html += "Females will be sold the same as males";
                    html += "</div>";
                }
            }
            else
            {
                html += "\n<div class=\"activityentry\">";
                html += "There are no age or weight sales of individuals.";
                html += "</div>";
            }
            html += "</div>";

            html += "\n<div class=\"activityentry\">";
            html += "Purchased breeders will be placed in ";
            if (GrazeFoodStoreNameBreeders == null || GrazeFoodStoreNameBreeders == "")
            {
                html += "<span class=\"resourcelink\">General yards</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + GrazeFoodStoreNameBreeders + "</span>";
                if (MinimumPastureBeforeRestock > 0)
                {
                    html += " with no restocking while pasture is below <span class=\"activityentry\">" + MinimumPastureBeforeRestock.ToString() + "</span>";
                }
            }
            html += "</div>";

            html += "\n<div class=\"activityentry\">";
            html += "Purchased sires will be placed in ";
            if (GrazeFoodStoreNameSires == null || GrazeFoodStoreNameSires == "")
            {
                html += "<span class=\"resourcelink\">General yards</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + GrazeFoodStoreNameSires + "</span>";
                if (MinimumPastureBeforeRestock > 0)
                {
                    html += " with no restocking while pasture is below <span class=\"activityentry\">" + MinimumPastureBeforeRestock.ToString() + "</span>";
                }
            }
            html += "</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            if (Apsim.Children(this, typeof(RuminantDestockGroup)).Count() > 0)
            {
                html += "\n</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            if (Apsim.Children(this, typeof(RuminantDestockGroup)).Count() > 0)
            {
                html += "\n<div class=\"activitygroupsborder\">";
                html += "<div class=\"labournote\">The following breeders will be sold prior to heifers</div>";
            }
            return html;
        }

    }
}
