using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System.ComponentModel.DataAnnotations;

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
    public class GrazeFoodStoreType : CLEMModel, IResourceWithTransactionType, IResourceType
    {
        [Link]
        ZoneCLEM ZoneCLEM = null;

        /// <summary>
        /// List of pools available
        /// </summary>
        [XmlIgnore]
        public List<GrazeFoodStorePool> Pools = new List<GrazeFoodStorePool>();

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
        [Description("Minimum Nitrogen %")]
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

        /// <summary>
        /// The area provided for this pasture to grow on
        /// </summary>
        [XmlIgnore]
        public double Area { get; set; }

        /// <summary>
        /// The biomass per hectare of parture available
        /// </summary>
        public double kgPerHa { get { return Amount / Area; } }

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
                    if (biomassConsumed > 0)
                    {
                        return 100;
                    }
                }
                if (biomassConsumed == 0) return 0;
                return biomassConsumed / biomassAddedThisYear * 100;
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
        /// DecayOfPasture
        /// </summary>
        [XmlIgnore]
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
        [XmlIgnore]
        public double Amount {
            get
            {
                return Pools.Sum(a => a.Amount);
            }
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [XmlIgnore]
        public double TonnesPerHectare
        {
            get
            {
                return Pools.Sum(a => a.Amount)/1000/Area;
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
        /// Amount (tonnes per ha)
        /// </summary>
        [XmlIgnore]
        public double TonnesPerHectareStartOfTimeStep { get; set; }

        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CurrentEcologicalIndicators = new EcologicalIndicators();
            CurrentEcologicalIndicators.ResourceType = this.Name;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Initialise();
        }

        /// <summary>Clear data stores for utilisation at end of ecological indicators calculation month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void ONCLEMAgeResources(object sender, EventArgs e)
        {
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
            this.TonnesPerHectareStartOfTimeStep = this.TonnesPerHectare;
        }

        /// <summary>
        /// Graze food add method.
        /// This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="ResourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public void Add(object ResourceAmount, string ActivityName, string Reason)
        {
            // expecting a GrazeFoodStoreResource (PastureManage) or FoodResourcePacket (CropManage)
            if (!(ResourceAmount.GetType() == typeof(GrazeFoodStorePool) | ResourceAmount.GetType() != typeof(FoodResourcePacket)))
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported in Add method in {1}", ResourceAmount.GetType().ToString(), this.Name));
            }

            GrazeFoodStorePool pool;
            if (ResourceAmount.GetType() == typeof(GrazeFoodStorePool))
            {
                pool = ResourceAmount as GrazeFoodStorePool;
            }
            else
            {
                pool = new GrazeFoodStorePool();
                FoodResourcePacket packet = ResourceAmount as FoodResourcePacket;
                pool.Set(packet.Amount);
                pool.Nitrogen = packet.PercentN;
                pool.DMD = packet.DMD;
            }

            if (pool.Amount > 0)
            {
                // allow decaying or no pools currently available
                if(PastureDecays | Pools.Count() == 0)
                {
                    Pools.Insert(0, pool);
                }
                else
                {
                    Pools[0].Add(pool);
                }
                // update biomass available
                biomassAddedThisYear += pool.Amount;

                ResourceTransaction details = new ResourceTransaction();
                details.Credit = pool.Amount;
                details.Activity = ActivityName;
                details.Reason = Reason;
                details.ResourceType = this.Name;
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RemoveAmount"></param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public double Remove(double RemoveAmount, string ActivityName, string Reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Request"></param>
        public void Remove(ResourceRequest Request)
        {
            // handles grazing by breed from this pasture pools based on breed pool limits

            if (Request.AdditionalDetails != null && Request.AdditionalDetails.GetType() == typeof(RuminantActivityGrazePastureHerd))
            {
                RuminantActivityGrazePastureHerd thisBreed = Request.AdditionalDetails as RuminantActivityGrazePastureHerd;

                // take from pools as specified for the breed
                double amountRequired = Request.Required;
                bool secondTakeFromPools = thisBreed.RuminantTypeModel.StrictFeedingLimits;
                thisBreed.DMD = 0;
                thisBreed.N = 0;
                int index = 0;
                while (amountRequired > 0)
                {
                    // limiter obtained from breed feed limits or unlimited if second take of pools
                    double limiter = 1.0;
                    if (!secondTakeFromPools)
                    {
                        limiter = thisBreed.PoolFeedLimits[index].Limit;
                    }

                    double amountToRemove = Math.Min(this.Pools[index].Amount, amountRequired * limiter);
                    // update DMD and N based on pool utilised
                    thisBreed.DMD += this.Pools[index].DMD * amountToRemove;
                    thisBreed.N += this.Pools[index].Nitrogen * amountToRemove;
                    amountRequired -= amountToRemove;

                    // remove resource from pool
                    this.Pools[index].Remove(amountToRemove, "Graze", thisBreed.Name);

                    index++;
                    if (index >= this.Pools.Count)
                    {
                        // if we've already given second chance to get food so finish without full satisfying individual
                        // or strict feeding limits are enforced
                        if (secondTakeFromPools) break;
                        // if not strict limits allow a second request for food from previously limited pools.
                        secondTakeFromPools = true;
                        index = 0;
                    }
                }

                Request.Provided = Request.Required - amountRequired;

                // adjust DMD and N of biomass consumed
                thisBreed.DMD /= Request.Provided;
                thisBreed.N /= Request.Provided;

                //if graze activity
                biomassConsumed += Request.Provided;

                // report 
                ResourceTransaction details = new ResourceTransaction();
                details.ResourceType = this.Name;
                details.Debit = Request.Provided * -1;
                details.Activity = Request.ActivityModel.Name;
                details.Reason = Request.Reason;
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);

            }
            else
            {
                // Need to add new section here to allow non grazing activity to remove resources from pasture.
                throw new Exception("Removing resources from native food store can only be performed by a grazing activity at this stage");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NewAmount"></param>
        public void Set(double NewAmount)
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
            if (TransactionOccurred != null)
                TransactionOccurred(this, e);
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

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
            if (EcologicalIndicatorsCalculated != null)
                EcologicalIndicatorsCalculated(this, e);
        }

        /// <summary>
        /// Ecological indicators of this pasture
        /// </summary>
        [XmlIgnore]
        public EcologicalIndicators CurrentEcologicalIndicators { get; set; }


    }

}