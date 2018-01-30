using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd management activity</summary>
    /// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs the management of ruminant numbers based upon the current herd filtering. It requires a RuminantActivityBuySell to undertake the purchases and sales.")]
    public class RuminantActivityManage: CLEMRuminantActivityBase
    {
        /// <summary>
        /// Maximum number of breeders that can be kept
        /// </summary>
        [Description("Maximum number of breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        [GreaterThanEqual("MinimumBreedersKept")]
        public int MaximumBreedersKept { get; set; }

        /// <summary>
        /// Minimum number of breeders that can be kept
        /// </summary>
        [Description("Minimum number of breeders to be kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MinimumBreedersKept { get; set; }

        /// <summary>
        /// Maximum breeder age (months) for culling
        /// </summary>
        [Description("Maximum breeder age (months) for culling")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumBreederAge { get; set; }

        /// <summary>
        /// Maximum number of breeders in a single purchase
        /// </summary>
        [Description("Maximum number of breeders in a single purchase")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumBreedersPerPurchase { get; set; }

        /// <summary>
        /// Maximum number of breeding sires kept
        /// </summary>
        [Description("Maximum number of breeding sires kept")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumSiresKept { get; set; }

        /// <summary>
        /// Maximum bull age (months) for culling
        /// </summary>
        [Description("Maximum bull age (months) for culling")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumBullAge { get; set; }

        /// <summary>
        /// Allow natural herd replacement of sires
        /// </summary>
        [Description("Allow sire replacement from herd")]
        [Required]
        public bool AllowSireReplacement { get; set; }

        /// <summary>
        /// Maximum number of sires in a single purchase
        /// </summary>
        [Description("Maximum number of sires in a single purchase")]
        [Required, GreaterThanEqualValue(0)]
        public int MaximumSiresPerPurchase { get; set; }

        /// <summary>
        /// Male selling age (months)
        /// </summary>
        [Description("Male selling age (months)")]
        [Required, GreaterThanEqualValue(0)]
        public double MaleSellingAge { get; set; }

        /// <summary>
        /// Male selling weight (kg)
        /// </summary>
        [Description("Male selling weight (kg)")]
        [Required]
        public double MaleSellingWeight { get; set; }

        /// <summary>
        /// Name of GrazeFoodStore (paddock) to place purchases in for grazing (leave blank for general yards)
        /// </summary>
        [Description("Name of GrazeFoodStore (paddock) to place purchases in (leave blank for general yards)")]
        public string GrazeFoodStoreName { get; set; }

        /// <summary>
        /// Minimum pasture (kg/ha) before restocking if placed in paddock
        /// </summary>
        [Description("Minimum pasture (kg/ha) before restocking if placed in paddock")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double MinimumPastureBeforeRestock { get; set; }

        /// <summary>
        /// Perform selling of young females the same as males
        /// </summary>
        [Description("Perform selling of young females the same as males")]
        [Required]
        public bool SellFemalesLikeMales { get; set; }

        /// <summary>
        /// Identify males for sale every time step
        /// </summary>
        [Description("Identify males for sale every time step")]
        [Required]
        public bool ContinuousMaleSales { get; set; }

        /// <summary>
        /// Store graze 
        /// </summary>
        private GrazeFoodStoreType foodStore;

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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);
            breedParams = Resources.GetResourceItem(this, typeof(RuminantHerd), this.PredictedHerdBreed, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            // check GrazeFoodStoreExists
            if (GrazeFoodStoreName == null) GrazeFoodStoreName = "";
            if(GrazeFoodStoreName!="")
            {
                foodStore = Resources.GetResourceItem(this, typeof(GrazeFoodStore), GrazeFoodStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalManage")]
        private void OnCLEMAnimalManage(object sender, EventArgs e)
        {
            //List<Ruminant> localHerd = this.CurrentHerd();
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            // clear store of individuals to try and purchase
            //            ruminantHerd.PurchaseIndividuals.Clear();

            // remove only the individuals that are affected by this activity.
            ruminantHerd.PurchaseIndividuals.RemoveAll(a => a.Breed == this.PredictedHerdBreed);

            List<Ruminant> herd = this.CurrentHerd(true);
            //            List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.HerdName == HerdName).ToList();

            // can sell off males any month as per NABSA
            // if we don't need this monthly, then it goes into next if statement with herd declaration
            // NABSA MALES - weaners, 1-2, 2-3 and 3-4 yo, we check for any male weaned and not a breeding sire.
            // check for sell age/weight of young males
            // if SellYoungFemalesLikeMales then all apply to both sexes else only males.
            if (this.TimingOK || ContinuousMaleSales)
            {
                foreach (var ind in herd.Where(a => a.Weaned & (SellFemalesLikeMales ? true : (a.Gender == Sex.Male)) & (a.Age >= MaleSellingAge || a.Weight >= MaleSellingWeight)))
                {
                    bool sell = true;
                    if (ind.GetType() == typeof(RuminantMale))
                    {
                        // don't sell breeding sires.
                        sell = !((ind as RuminantMale).BreedingSire);
                    }
                    if (sell)
                    {
                        ind.SaleFlag = HerdChangeReason.AgeWeightSale;
                    }
                }
            }

            // if management month
            if (this.TimingOK)
            {
                bool sufficientFood = true;
                if(foodStore != null)
                {
                    sufficientFood = (foodStore.TonnesPerHectare * 1000) > MinimumPastureBeforeRestock;
                }

                // check for maximum age (females and males have different cutoffs)
                foreach (var ind in herd.Where(a => a.Age >= ((a.Gender == Sex.Female) ? MaximumBreederAge : MaximumBullAge)))
                {
                    ind.SaleFlag = HerdChangeReason.MaxAgeSale;
                }

                // MALES
                // check for breeder bulls after sale of old individuals and buy/sell
                int numberMaleSiresInHerd = herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.None).Cast<RuminantMale>().Where(a => a.BreedingSire).Count();

                // Number of females
                int numberFemaleBreedingInHerd = herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating & a.SaleFlag == HerdChangeReason.None).Count();
                int numberFemaleTotalInHerd = herd.Where(a => a.Gender == Sex.Female & a.SaleFlag == HerdChangeReason.None).Count();
                int numberFemaleOldInHerd = herd.Where(a => a.Gender == Sex.Female & MaximumBreederAge - a.Age <= 12 & a.SaleFlag == HerdChangeReason.None).Count();

                if (numberMaleSiresInHerd > MaximumSiresKept)
                {
                    // sell bulls
                    // What rule? oldest first as they may be lost soonest
                    int numberToRemove = MaximumSiresKept - numberMaleSiresInHerd;
                    foreach (var male in herd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).OrderByDescending(a => a.Age).Take(numberToRemove))
                    {
                        male.SaleFlag = HerdChangeReason.ExcessBullSale;
                        numberToRemove--;
                        if (numberToRemove == 0) break;
                    }
                }
                else if(numberMaleSiresInHerd < MaximumSiresKept)
                {
                    if ((foodStore == null) || (sufficientFood))
                    {
                        if (AllowSireReplacement)
                        {
                            // remove young bulls from sale herd to replace breed bulls (not those sold because too old)
                            foreach (RuminantMale male in herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Weight))
                            {
                                male.SaleFlag = HerdChangeReason.None;
                                male.BreedingSire = true;
                                numberMaleSiresInHerd++;
                                if (numberMaleSiresInHerd >= MaximumSiresKept) break;
                            }
                            // if still insufficent, look into current herd for replacement
                            // remaining males assumed to be too small, so await next time-step
                        }

                        // if still insufficient buy bulls.
                        if (numberMaleSiresInHerd < MaximumSiresKept && (MaximumSiresPerPurchase>0))
                        {
                            // limit by breeders as proportion of max breeders so we don't spend alot on sires when building the herd and females more valuable
                            double propOfBreeders = (double)numberFemaleBreedingInHerd / (double)MaximumBreedersKept;

                            int sires = Convert.ToInt32(Math.Ceiling(Math.Ceiling(MaximumSiresKept * propOfBreeders)));
                            int numberToBuy = Math.Min(MaximumSiresPerPurchase, Math.Max(0, sires - numberMaleSiresInHerd));

                            for (int i = 0; i < numberToBuy; i++)
                            {
                                RuminantMale newbull = new RuminantMale
                                {
                                    Location = GrazeFoodStoreName,
                                    Age = 48,
                                    Breed = this.PredictedHerdBreed,// breedParams.Breed;
                                    HerdName = this.PredictedHerdName,
                                    BreedingSire = true,
                                    BreedParams = breedParams,
                                    Gender = Sex.Male,
                                    ID = 0, // ruminantHerd.NextUniqueID;
                                    Weight = 450,
                                    HighWeight = 450,
                                    SaleFlag = HerdChangeReason.SirePurchase
                                };

                                // add to purchase request list and await purchase in Buy/Sell
                                ruminantHerd.PurchaseIndividuals.Add(newbull);
                            }
                        }
                    }
                }

                // FEMALES
                // Breeding herd traded as heifers only
                int excessHeifers = 0;

                // check for maximum number of breeders remaining after sale and buy/sell
                if (numberFemaleBreedingInHerd > MaximumBreedersKept)
                {
                    // herd mortality of 5% plus those that will be culled in next 12 months
                    excessHeifers = Convert.ToInt32((numberFemaleTotalInHerd - numberFemaleOldInHerd) * 0.05) + numberFemaleOldInHerd;
                    // shortfall + (number of young - replacement heifers)
                    excessHeifers = (numberFemaleTotalInHerd - MaximumBreedersKept) + ((numberFemaleTotalInHerd - numberFemaleBreedingInHerd) - excessHeifers);
                }
                else
                {
                    // shortfall between actual and desired numbers
                    excessHeifers = MaximumBreedersKept - numberFemaleBreedingInHerd;
                    // add future cull for age + 5%
                    excessHeifers += Convert.ToInt32((numberFemaleTotalInHerd - numberFemaleOldInHerd) * 0.05) + numberFemaleOldInHerd;
                    excessHeifers = (numberFemaleTotalInHerd - numberFemaleBreedingInHerd) - excessHeifers;
                }

                // surplus heifers to sell
                if (excessHeifers > 0)
                {
                    foreach (var female in herd.Where(a => a.Gender == Sex.Female & a.Age < a.BreedParams.MinimumAge1stMating & a.Weaned).OrderByDescending(a => a.Age).Take(excessHeifers))
                    {
                        // tag fo sale.
                        female.SaleFlag = HerdChangeReason.ExcessHeiferSale;
                        excessHeifers--;
                        if (excessHeifers == 0) break;
                    }
                }
                else if (excessHeifers < 0)
                {
                    excessHeifers *= -1;
                    if ((foodStore == null) || (sufficientFood))
                    {
                        // remove young females from sale herd to replace breeders (not those sold because too old)
                        foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female & a.SaleFlag == HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Age))
                        {
                            female.SaleFlag = HerdChangeReason.None;
                            excessHeifers--;
                            if (excessHeifers == 0) break;
                        }

                        // if still insufficient buy heifers.
                        if (excessHeifers > 0 & (MaximumBreedersPerPurchase > 0))
                        {
                            int ageOfHeifer = 12;
                            double weightOfHeifer = 260;

                            // buy 5% more to account for deaths before these individuals grow to breeding age
                            int numberToBuy = Math.Min(MaximumBreedersPerPurchase, Math.Max(0, Convert.ToInt32(excessHeifers * 1.05)));

                            for (int i = 0; i < numberToBuy; i++)
                            {
                                RuminantFemale newheifer = new RuminantFemale
                                {
                                    Location = GrazeFoodStoreName,
                                    Age = ageOfHeifer,
                                    Breed = this.PredictedHerdBreed, 
                                    HerdName = this.PredictedHerdName,
                                    BreedParams = breedParams,
                                    Gender = Sex.Female,
                                    ID = 0,
                                    Weight = weightOfHeifer,
                                    HighWeight = weightOfHeifer,
                                    SaleFlag = HerdChangeReason.HeiferPurchase
                                };

                                // add to purchase request list and await purchase in Buy/Sell
                                ruminantHerd.PurchaseIndividuals.Add(newheifer);
                            }
                        }
                    }
                }

                // report that this activity was performed as it does not use base GetResourcesRequired
                //this.TriggerOnActivityPerformed();

                // Breeders themselves don't get sold. Trading is with Heifers
                // Breeders can be sold in seasonal and ENSO destocking.
                // sell breeders
                // What rule? oldest first as they may be lost soonest
                // should keep pregnant females... and young...
                // this will currently remove pregnant females and females with suckling calf

                //            int numberToRemove = Convert.ToInt32((numberFemaleInHerd-MaximumBreedersKept));
                //    foreach (var female in herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating).OrderByDescending(a => a.Age).Take(numberToRemove))
                //    {
                //        female.SaleFlag = HerdChangeReason.ExcessBreederSale;
                //                    numberToRemove--;
                //                    if (numberToRemove == 0) break;
                //                }
                //            }
                //else
                //{
                //}
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
