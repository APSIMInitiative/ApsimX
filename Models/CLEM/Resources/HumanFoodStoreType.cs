using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for a Home Food Store type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(HumanFoodStore))]
    [Description("This resource represents a human food store (e.g. milk, eggs, wheat).")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Human food store/HumanFoodStoreType.htm")]
    public class HumanFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// Convert to kg
        /// </summary>
        [Description("kg per unit")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValueAttribute(1)]
        public double ConvertToKg { get; set; }

        /// <summary>
        /// Edible proportion of raw product
        /// </summary>
        [Description("Edible proportion of raw product")]
        [Required, GreaterThanValue(0), Proportion]
        [System.ComponentModel.DefaultValueAttribute(1)]
        public double EdibleProportion { get; set; }

        /// <summary>
        /// The number of months before this food store spoils and is unfit for consumption by humans
        /// </summary>
        [Description("Use by age (0 unlimited)")]
        [Units("months")]
        [Required, GreaterThanEqualValue(0)]
        public int UseByAge { get; set; }

        /// <summary>
        /// Starting Amount
        /// </summary>
        [Description("Starting amount")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Starting age of the food
        /// </summary>
        [Description("Starting age")]
        [Units("months")]
        [Required, GreaterThanEqualValue(0)]
        public int StartingAge { get; set; }

        /// <summary>
        /// List of pools available
        /// </summary>
        [XmlIgnore]
        public List<HumanFoodStorePool> Pools = new List<HumanFoodStorePool>();

        /// <summary>
        /// Constructor
        /// </summary>
        public HumanFoodStoreType()
        {
            base.SetDefaults();
        }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount
        {
            get
            {
                return Pools.Sum(a => a.Amount);
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (StartingAmount > 0)
            {
                HumanFoodStorePool initialpPool = new HumanFoodStorePool(StartingAmount, StartingAge);
                Add(initialpPool, this, "Starting value");
            }
        }

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="reason">Name of individual adding resource</param>
        public new void Add(object resourceAmount, CLEMModel activity, string reason)
        {
            HumanFoodStorePool pool;
            switch (resourceAmount.GetType().Name)
            {
                case "HumanFoodStorePool":
                    pool = resourceAmount as HumanFoodStorePool;
                    break;
                case "Double":
                    pool = new HumanFoodStorePool((double)resourceAmount, 0);
                    break;
                default:
                    // expecting a HumanFoodStorePool or Double
                    throw new Exception(String.Format("ResourceAmount object of type {0} is not supported in Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }

            if (pool.Amount > 0)
            {
                HumanFoodStorePool poolOfAge = Pools.Where(a => a.Age == pool.Age).FirstOrDefault();
                if(poolOfAge is null)
                {
                    Pools.Insert(0, pool);
                }
                else
                {
                    poolOfAge.Add(pool.Amount);
                }

                ResourceTransaction details = new ResourceTransaction
                {
                    Gain = pool.Amount,
                    Activity = activity,
                    Reason = reason,
                    ResourceType = this
                };
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// Remove from human food store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            // if this request aims to trade with a market see if we need to set up details for the first time
            if(request.MarketTransactionMultiplier > 0)
            {
                FindEquivalentMarketStore();
            }

            double amountRequired = request.Required;
            foreach (HumanFoodStorePool pool in Pools.OrderByDescending(a => a.Age))
            {
                // take min of amount in pool, remaining intake needed
                double amountToRemove = Math.Min(pool.Amount, amountRequired);
                amountRequired -= amountToRemove;

                // remove resource from pool
                pool.Remove(amountToRemove, request.ActivityModel, "Consumed");

                // send to market if needed
                if(request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
                {
                    (EquivalentMarketStore as HumanFoodStoreType).Add(new HumanFoodStorePool(amountToRemove* request.MarketTransactionMultiplier, pool.Age), request.ActivityModel, "Farm sales");
                }

                if (amountRequired <= 0)
                {
                    break;
                }
            }

            double amountRemoved = request.Required - amountRequired;
            if (amountRemoved > 0)
            {
                request.Provided = amountRemoved;
                ResourceTransaction details = new ResourceTransaction
                {
                    ResourceType = this,
                    Loss = amountRemoved,
                    Activity = request.ActivityModel,
                    Reason = request.Reason
                };
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);

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

        /// <summary>
        /// Function to age resource pools
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            if (UseByAge > 0)
            {
                foreach (var pool in Pools)
                {
                    pool.Age++;
                }
                // remove all spoiled pools
                double spoiled = Pools.Where(a => a.Age >= UseByAge).Sum(a => a.Amount);
                if (spoiled > 0)
                {
                    Pools.RemoveAll(a => a.Age >= UseByAge);
                    // report spoiled loss
                    ResourceTransaction details = new ResourceTransaction
                    {
                        ResourceType = this,
                        Loss = spoiled,
                        Activity = this,
                        Reason = "Spoiled"
                    };
                    LastTransaction = details;
                    TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                    OnTransactionOccurred(te);
                }
            }
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
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        #endregion

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "\n<div class=\"activityentry\">";
            if ((Units??"").ToUpper() != "KG")
            {
                html += "Each unit of this resource is equivalent to ";
                if (ConvertToKg == 0)
                {
                    html += "<span class=\"errorlink\">NOT SET";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + this.ConvertToKg.ToString("0.###");
                }
                html += "</span> kg";
            }
            else
            {
                if(ConvertToKg != 1)
                {
                    html += "<span class=\"errorlink\">SET UnitsToKg to 1</span> as this Food Type is measured in kg";
                }
            }
            html += "\n</div>";
            if (StartingAmount > 0)
            {
                html += "\n<div class=\"activityentry\">";
                html += "The simulation starts with <span class=\"setvalue\">" + this.StartingAmount.ToString("0.###") + "</span>";
                if (StartingAge > 0)
                {
                    html += " with an age of <span class=\"setvalue\">" + this.StartingAge.ToString("###") + "%</span> months";
                }
                html += "\n</div>";
            }

            html += "\n<div class=\"activityentry\">";
            if (UseByAge == 0)
            {
                html += "This food does not spoil";
            }
            else
            {
                html += "This food must be consumed before <span class=\"setvalue\">" + this.UseByAge.ToString("###") + "</span> month"+((UseByAge>1)?"s":"")+" old";
            }
            html += "\n</div>";

            html += "\n<div class=\"activityentry\"><span class=\"setvalue\">";
            html += ((EdibleProportion == 1)?"All":EdibleProportion.ToString("#0%"))+"</span> of this raw food is edible";
            html += "\n</div>";

            return html;
        }
    }
}