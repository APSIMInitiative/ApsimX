using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a GrazeFoodType and holds values in the store
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [Description("This resource represents a graze food store of native pasture (e.g. a specific paddock).")]
    [Version(1, 0, 2, "Grazing from pasture pools is fixed to reflect NABSA approach.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreType.htm")]
    public class GrazeFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        [Link]
        ZoneCLEM ZoneCLEM = null;

        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; private set; }

        /// <summary>
        /// List of pools available
        /// </summary>
        [JsonIgnore]
        public List<GrazeFoodStorePool> Pools =  new List<GrazeFoodStorePool>();

        /// <summary>
        /// Return the specified pool 
        /// </summary>
        /// <param name="index">index to use</param>
        /// <param name="getByAge">return where index is age</param>
        /// <returns>GraxeFoodStore pool</returns>
        public GrazeFoodStorePool Pool(int index, bool getByAge)
        {
            if(getByAge)
            {
                var res = Pools.Where(a => a.Age == index);
                if (res.Count() > 1)
                {
                    // return an average pool for N and DMD
                    GrazeFoodStorePool average = new GrazeFoodStorePool()
                    {
                        Age = index,
                        Consumed = res.Sum(a => a.Consumed),
                        Detached = res.Sum(a => a.Detached),
                        Growth = res.Sum(a => a.Growth),
                        DMD = res.Sum(a => a.DMD * a.Amount) / res.Sum(a => a.Amount),
                        Nitrogen = res.Sum(a => a.Nitrogen * a.Amount) / res.Sum(a => a.Amount)
                    };
                    average.Set(res.Sum(a => a.Amount));
                    return average;
                }
                else
                {
                    return res.FirstOrDefault();
                }
            }
            else
            {
                if (index < Pools.Count())
                {
                    return Pools[index];
                }
                else
                {
                    return null;
                }

            }
        } 

        /// <summary>
        /// Coefficient to convert initial N% to DMD%
        /// </summary>
        [Description("Coefficient to convert initial N% to DMD%")]
        [Required]
        public double NToDMDCoefficient { get; set; }

        /// <summary>
        /// Intercept to convert initial N% to DMD%
        /// </summary>
        [Description("Intercept to convert initial N% to DMD%")]
        [Required]
        public double NToDMDIntercept { get; set; }

        /// <summary>
        /// Crude protein denominator to convert initial N% to DMD%
        /// </summary>
        [Description("Crude protein denominator to convert initial N% to DMD%")]
        [Required]
        public double NToDMDCrudeProteinDenominator { get; set; }

        /// <summary>
        /// Nitrogen of new growth (%)
        /// </summary>
        [Description("Nitrogen of new growth (%)")]
        [Required, Percentage]
        public double GreenNitrogen { get; set; }

        /// <summary>
        /// Proportion Nitrogen loss each month from pools
        /// </summary>
        [Description("%Nitrogen loss each month from pools (note: amount not proportion)")]
        [Required, GreaterThanEqualValue(0)]
        public double DecayNitrogen { get; set; }

        /// <summary>
        /// Minimum Nitrogen %
        /// </summary>
        [Description("Minimum nitrogen %")]
        [Required, Percentage]
        public double MinimumNitrogen { get; set; }

        /// <summary>
        /// Proportion Dry Matter Digestibility loss each month from pools
        /// </summary>
        [Description("Proportion DMD loss each month from pools")]
        [Required, Proportion]
        public double DecayDMD { get; set; }

        /// <summary>
        /// Minimum Dry Matter Digestibility
        /// </summary>
        [Description("Minimum Dry Matter Digestibility")]
        [Required, Percentage]
        public double MinimumDMD { get; set; }

        /// <summary>
        /// Monthly detachment rate
        /// </summary>
        [Description("Detachment rate")]
        [Required, Proportion]
        public double DetachRate { get; set; }

        /// <summary>
        /// Detachment rate of 12 month or older plants
        /// </summary>
        [Description("Carryover detachment rate")]
        [Required, Proportion]
        public double CarryoverDetachRate { get; set; }

        /// <summary>
        /// Coefficient to adjust intake for tropical herbage quality
        /// </summary>
        [Description("Coefficient to adjust intake for tropical herbage quality")]
        [Required]
        public double IntakeTropicalQualityCoefficient { get; set; }

        /// <summary>
        /// Coefficient to adjust intake for herbage quality
        /// </summary>
        [Description("Coefficient to adjust intake for herbage quality")]
        [Required]
        public double IntakeQualityCoefficient { get; set; }

        private IPastureManager manager;
        private GrazeFoodStoreFertilityLimiter grazeFoodStoreFertilityLimiter;

        /// <summary>
        /// A link to the Activity managing this Graze Food Store
        /// </summary>
        [JsonIgnore]
        public IPastureManager Manager
        {
            get
            { return manager; }
            set
            {
                if(manager!=null)
                {
                    throw new ApsimXException(this, String.Format("Each [r=GrazeStoreType] can only be managed by a single activity./nTwo managing activities ([a={0}] and [a={1}]) are trying to manage [r={2}]", manager.Name, value.Name, this.Name));
                }
                manager = value;
            }
        }

        /// <summary>
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa
        {
            get
            {
                if (Manager != null)
                {
                    return Amount / Manager.Area;
                }
                else
                {
                    return 0;
                }
            }
        }

        private double biomassAddedThisYear;
        private double biomassConsumed;

        /// <summary>
        /// Percent utilisation
        /// </summary>
        public double PercentUtilisation
        {
            get
            {
                if (biomassAddedThisYear == 0)
                {
                    return (biomassConsumed > 0) ? 100: 0;
                }
                return biomassConsumed == 0 ? 0 : Math.Min(biomassConsumed / biomassAddedThisYear * 100,100);
            }
        }

        /// <summary>
        /// Calculated total pasture (all pools) Dry Matter Digestibility (%)
        /// </summary>
        public double DMD
        {
            get
            {
                double dmd = 0;
                if (this.Amount > 0)
                {
                    dmd = Pools.Sum(a => a.Amount * a.DMD) / this.Amount;
                }
                return Math.Max(MinimumDMD, dmd);
            }
        }

        /// <summary>
        /// Calculated total pasture (all pools) Nitrogen (%)
        /// </summary>
        public double Nitrogen
        {
            get
            {
                double n = 0;
                if (this.Amount > 0)
                {
                    n = Pools.Sum(a => a.Amount * a.Nitrogen) / this.Amount;
                }
                return Math.Max(MinimumNitrogen, n);
            }
        }

        /// <summary>
        /// DecayOfPasture
        /// </summary>
        [JsonIgnore]
        public bool PastureDecays
        {
            get
            {
                return (DetachRate+CarryoverDetachRate+DecayDMD+DecayNitrogen != 0);
            }
        }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
        public double Amount {
            get
            {
                return Pools.Sum(a => a.Amount);
            }
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectare
        {
            get
            {
                if (Manager != null)
                {
                    return Pools.Sum(a => a.Amount) / 1000 / Manager.Area;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Get a property of pools by pool age
        /// </summary>
        public double GetValueByPoolAge(int age, string property)
        {
            IEnumerable<GrazeFoodStorePool> pools;
            // group all pools >12 months old.
            if (age < 12)
            {
                pools = Pools.Where(a => a.Age == age);
            }
            else
            {
                pools = Pools.Where(a => a.Age >= 12);
            }
            switch (property)
            {
                case "Detached":
                    return pools.Sum(a => a.Detached);
                case "Growth":
                    return pools.Sum(a => a.Growth);
                case "Consumed":
                    return pools.Sum(a => a.Consumed);
                case "Amount":
                    return pools.Sum(a => a.Amount);
                case "DMD":
                    return pools.Sum(a => a.Amount* a.DMD)/ pools.Sum(a => a.Amount);
                case "Nitrogen":
                    return pools.Sum(a => a.Amount * a.Nitrogen) / pools.Sum(a => a.Amount);
                default:
                    throw new ApsimXException(this, "Property [" + property + "] not available for reporting pools");
            }
        }

        /// <summary>
        /// Method to estimate DMD from N%
        /// </summary>
        /// <returns></returns>
        public double EstimateDMD(double nitrogenPercent)
        {
            return Math.Max(MinimumDMD, nitrogenPercent * NToDMDCoefficient + NToDMDIntercept);
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectareStartOfTimeStep { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CurrentEcologicalIndicators = new EcologicalIndicators
            {
                ResourceType = this.Name
            };
            grazeFoodStoreFertilityLimiter = FindAllChildren<GrazeFoodStoreFertilityLimiter>().FirstOrDefault() as GrazeFoodStoreFertilityLimiter;
        }

        /// <summary>An event handler to allow us to make checks after resources and activities initialised.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMFinalSetupBeforeSimulation")]
        private void OnCLEMFinalSetupBeforeSimulation(object sender, EventArgs e)
        {
            if(Manager == null)
            {
                Summary.WriteWarning(this, String.Format("There is no activity managing [r={0}]. This resource cannot be used and will have no growth.\r\nTo manage [r={0}] include a [a=CropActivityManage]+[a=CropActivityManageProduct] or a [a=PastureActivityManage] depending on your external data type.", this.Name));
            }
        }

        /// <summary>
        /// Cleans up pools
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Pools != null)
            {
                Pools.Clear();
            }
            Pools = null;
        }

        /// <summary>An event handler to allow us to clear pools.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // reset pool counters
            foreach (var pool in Pools)
            {
                pool.Reset();
            }
        }

        /// <summary>
        /// Function to detach pasture before reporting
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDetachPasture")]
        private void OnCLEMDetachPasture(object sender, EventArgs e)
        {
            if (DetachRate < 1 | CarryoverDetachRate < 1)
            {
                foreach (var pool in Pools)
                {
                    double detach = CarryoverDetachRate;
                    if (pool.Age < 12)
                    {
                        detach = DetachRate;
                    }
                    double detachedAmount = pool.Amount * (1 - detach);
                    pool.Detached = pool.Amount * detach;
                    pool.Set(detachedAmount);
                }
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
            if (DecayNitrogen != 0 | DecayDMD > 0)
            {
                // decay N and DMD of pools and age by 1 month
                foreach (var pool in Pools)
                {
                    // N is a loss of N% (x = x -loss)
                    pool.Nitrogen = Math.Max(pool.Nitrogen - DecayNitrogen, MinimumNitrogen);
                    // DMD is a proportional loss (x = x*(1-proploss))
                    pool.DMD = Math.Max(pool.DMD * (1 - DecayDMD), MinimumDMD);

                    if (pool.Age < 12)
                    {
                        pool.Age++;
                    }
                }
                // remove all pools with less than 10g of food
                Pools.RemoveAll(a => a.Amount < 0.01);
            }

            if (ZoneCLEM.IsEcologicalIndicatorsCalculationMonth())
            {
                OnEcologicalIndicatorsCalculated(new EcolIndicatorsEventArgs() { Indicators = CurrentEcologicalIndicators });
                // reset so available is sum of years growth
                biomassAddedThisYear = 0;
                biomassConsumed = 0;
            }

        }

        /// <summary>Store amount of pasture available for everyone at the start of the step (kg per hectare)</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPastureReady")]
        private void ONCLEMPastureReady(object sender, EventArgs e)
        {
            // do not return zero as there is always something there and zero affects calculations.
            this.TonnesPerHectareStartOfTimeStep = Math.Max(this.TonnesPerHectare,0.01);
        }

        /// <summary>
        /// Ecological indicators have been calculated
        /// </summary>
        public event EventHandler EcologicalIndicatorsCalculated;

        /// <summary>
        /// Ecological indicators calculated 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnEcologicalIndicatorsCalculated(EventArgs e)
        {
            EcologicalIndicatorsCalculated?.Invoke(this, e);
            CurrentEcologicalIndicators.Reset();
        }

        /// <summary>
        /// Ecological indicators of this pasture
        /// </summary>
        [JsonIgnore]
        public EcologicalIndicators CurrentEcologicalIndicators { get; set; }


        #region transactions

        /// <summary>
        /// Graze food add method.
        /// This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            GrazeFoodStorePool pool;
            switch (resourceAmount.GetType().Name)
            {
                case "GrazeFoodStorePool":
                    pool = resourceAmount as GrazeFoodStorePool;
                    // adjust N content only if new growth (age = 0) based on yield limits and month range defined in GrazeFoodStoreFertilityLimiter if present
                    if (pool.Age == 0 && !(grazeFoodStoreFertilityLimiter is null))
                    {
                        double reduction = grazeFoodStoreFertilityLimiter.GetProportionNitrogenLimited(pool.Amount / Manager.Area);
                        pool.Nitrogen = Math.Max(MinimumNitrogen, pool.Nitrogen * reduction);
                    }
                    break;
                case "FoodResourcePacket":
                    pool = new GrazeFoodStorePool();
                    FoodResourcePacket packet = resourceAmount as FoodResourcePacket;
                    pool.Set(packet.Amount);
                    pool.Nitrogen = packet.PercentN;
                    pool.DMD = packet.DMD;
                    break;
                case "Double":
                    pool = new GrazeFoodStorePool();
                    pool.Set((double)resourceAmount);
                    pool.Nitrogen = this.Nitrogen;
                    pool.DMD = this.EstimateDMD(this.Nitrogen);
                    break;
                default:
                    // expecting a GrazeFoodStoreResource (PastureManage) or FoodResourcePacket (CropManage) or Double from G-Range
                    throw new Exception(String.Format("ResourceAmount object of type {0} is not supported in Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }
            if (pool.Amount > 0)
            {
                // allow decaying or no pools currently available
                if (PastureDecays || Pools.Count() == 0)
                {
                    Pools.Insert(0, pool);
                }
                else
                {
                    Pools[0].Add(pool);
                }
                // update biomass available
                if (!category.StartsWith("Initialise"))
                {
                    // do not update if this is ian initialisation pool
                    biomassAddedThisYear += pool.Amount;
                }

                ResourceTransaction details = new ResourceTransaction
                {
                    Gain = pool.Amount,
                    Activity = activity,
                    RelatesToResource = relatesToResource,
                    Category = category,
                    ResourceType = this
                };
                LastTransaction = details;
                LastGain = pool.Amount;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="removeAmount"></param>
        /// <param name="activityName"></param>
        /// <param name="reason"></param>
        public double Remove(double removeAmount, string activityName, string reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        public new void Remove(ResourceRequest request)
        {
            // handles grazing by breed from this pasture pools based on breed pool limits

            if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(RuminantActivityGrazePastureHerd))
            {
                RuminantActivityGrazePastureHerd thisBreed = request.AdditionalDetails as RuminantActivityGrazePastureHerd;

                // take from pools as specified for the breed
                double amountRequired = request.Required;
                thisBreed.DMD = 0;
                thisBreed.N = 0;

                // first take from pools
                foreach (GrazeBreedPoolLimit pool in thisBreed.PoolFeedLimits)
                {
                    // take min of amount in pool, intake*limiter, remaining intake needed
                    double amountToRemove = Math.Min(request.Required * pool.Limit, Math.Min(pool.Pool.Amount, amountRequired));
                    // update DMD and N based on pool utilised
                    thisBreed.DMD += pool.Pool.DMD * amountToRemove;
                    thisBreed.N += pool.Pool.Nitrogen * amountToRemove;

                    amountRequired -= amountToRemove;

                    // remove resource from pool
                    pool.Pool.Remove(amountToRemove, thisBreed, "Graze");

                    if (amountRequired <= 0)
                    {
                        break;
                    }
                }

                // if forage still limiting and second take allowed (enforce strict limits is false)
                if (amountRequired > 0 & !thisBreed.RuminantTypeModel.StrictFeedingLimits)
                {
                    // allow second take for the limited pools
                    double forage = thisBreed.PoolFeedLimits.Sum(a => a.Pool.Amount);

                    // this will only be the previously limited pools
                    double amountTakenDuringSecondTake = 0;
                    foreach (GrazeBreedPoolLimit pool in thisBreed.PoolFeedLimits.Where(a => a.Limit < 1))
                    {
                        //if still not enough take all
                        double amountToRemove = 0;
                        if (amountRequired >= forage)
                        {
                            // take as a proportion of the pool to total forage remaining
                            amountToRemove = pool.Pool.Amount / forage * amountRequired;
                        }
                        else
                        {
                            amountToRemove = pool.Pool.Amount;
                        }
                        // update DMD and N based on pool utilised
                        thisBreed.DMD += pool.Pool.DMD * amountToRemove;
                        thisBreed.N += pool.Pool.Nitrogen * amountToRemove;
                        amountTakenDuringSecondTake += amountToRemove;
                        // remove resource from pool
                        pool.Pool.Remove(amountToRemove, thisBreed, "Graze");
                    }
                    amountRequired -= amountTakenDuringSecondTake;
                }

                request.Provided = request.Required - amountRequired;

                // adjust DMD and N of biomass consumed
                thisBreed.DMD /= request.Provided;
                thisBreed.N /= request.Provided;

                //if graze activity
                biomassConsumed += request.Provided;

                // report 
                ResourceTransaction details = new ResourceTransaction
                {
                    ResourceType = this,
                    Loss = request.Provided,
                    Activity = request.ActivityModel,
                    Category = request.Category,
                    RelatesToResource = request.RelatesToResource
                };
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
            else if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(PastureActivityCutAndCarry))
            {
                // take from pools by cut and carry
                double amountRequired = request.Required;
                double amountCollected = 0;
                double dryMatterDigestibility = 0;
                double nitrogen = 0;

                // take proportionally from all pools.
                double useproportion = Math.Min(1.0, amountRequired / Pools.Sum(a => a.Amount));
                // if less than pools then take required as proportion of pools
                foreach (GrazeFoodStorePool pool in Pools)
                {
                    double amountToRemove = pool.Amount * useproportion;
                    amountCollected += amountToRemove;
                    dryMatterDigestibility += pool.DMD * amountToRemove;
                    nitrogen += pool.Nitrogen * amountToRemove;
                    pool.Remove(amountToRemove, this, "Cut and Carry");
                }
                request.Provided = amountCollected;

                // adjust DMD and N of biomass consumed
                dryMatterDigestibility /= request.Provided;
                nitrogen /= request.Provided;

                // report 
                ResourceTransaction details = new ResourceTransaction
                {
                    ResourceType = this,
                    Gain = request.Provided * -1,
                    Activity = request.ActivityModel,
                    Category = request.Category,
                    RelatesToResource = request.RelatesToResource
                };
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
            else
            {
                // Need to add new section here to allow non grazing activity to remove resources from pasture.
                throw new Exception("Removing resources from native food store can only be performed by a grazing and cut and carry activities at this stage");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Back account transaction occured
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [JsonIgnore]
        public ResourceTransaction LastTransaction { get; set; }

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
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("This pasture has an initial green nitrogen content of ");
                if (this.GreenNitrogen == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not set</span>%");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + this.GreenNitrogen.ToString("0.###") + "%</span>");
                }

                if (DecayNitrogen > 0)
                {
                    htmlWriter.Write(" and will decline by <span class=\"setvalue\">" + this.DecayNitrogen.ToString("0.###") + "%</span> per month to a minimum nitrogen of <span class=\"setvalue\">" + this.MinimumNitrogen.ToString("0.###") + "%</span>");
                }
                htmlWriter.Write("\r\n</div>");
                if (DecayDMD > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Dry Matter Digestibility will decay at a rate of <span class=\"setvalue\">" + this.DecayDMD.ToString("0.###") + "</span> per month to a minimum DMD of <span class=\"setvalue\">" + this.MinimumDMD.ToString("0.###") + "%</span>");
                    htmlWriter.Write("\r\n</div>");
                }
                if (DetachRate > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Pasture is lost through detachment at a rate of <span class=\"setvalue\">" + this.DetachRate.ToString("0.###") + "</span> per month");
                    if (CarryoverDetachRate > 0)
                    {
                        htmlWriter.Write(" and <span class=\"setvalue\">" + this.CarryoverDetachRate.ToString("0.###") + "</span> per month after 12 months");
                    }
                    htmlWriter.Write("\r\n</div>");
                }
                else
                {
                    if (CarryoverDetachRate > 0)
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">");
                        htmlWriter.Write("Pasture is lost through detachement at a rate of <span class=\"setvalue\">" + this.CarryoverDetachRate.ToString("0.###") + "</span> per month after 12 months");
                        htmlWriter.Write("\r\n</div>");
                    }
                }
                return htmlWriter.ToString(); 
            }
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