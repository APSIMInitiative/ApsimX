using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Models.CLEM.Interfaces;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.IO;
using DocumentFormat.OpenXml.Office2016.Excel;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the initialisation parameters for a Home Food Store type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(HumanFoodStore))]
    [Description("This resource represents a human food store (e.g. milk, eggs, wheat)")]
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
        [JsonIgnore]
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
        [JsonIgnore]
        public double Amount
        {
            get
            {
                return Pools.Sum(a => a.Amount);
            }
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
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
                Add(initialpPool, null, null, "Starting value");
            }
        }

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            HumanFoodStorePool pool;
            switch (resourceAmount)
            {
                case HumanFoodStorePool _:
                    pool = resourceAmount as HumanFoodStorePool;
                    break;
                case double _:
                    pool = new HumanFoodStorePool((double)resourceAmount, 0);
                    break;
                default:
                    throw new Exception($"ResourceAmount object of type [{resourceAmount.GetType().Name}] is not supported in [r={Name}]");
            }

            if (pool.Amount > 0)
            {
                HumanFoodStorePool poolOfAge = Pools.Where(a => a.Age == pool.Age).FirstOrDefault();
                if(poolOfAge is null)
                    Pools.Insert(0, pool);
                else
                    poolOfAge.Add(pool.Amount);

                ReportTransaction(TransactionType.Gain, pool.Amount, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        /// Remove from human food store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
                return;

            // if this request aims to trade with a market see if we need to set up details for the first time
            if(request.MarketTransactionMultiplier > 0)
                FindEquivalentMarketStore();

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
                    (EquivalentMarketStore as HumanFoodStoreType).Add(new HumanFoodStorePool(amountToRemove* request.MarketTransactionMultiplier, pool.Age), request.ActivityModel, this.NameWithParent, "Farm sales");

                if (amountRequired <= 0)
                    break;
            }

            double amountRemoved = request.Required - amountRequired;
            if (amountRemoved > 0)
            {
                request.Provided = amountRemoved;
                ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
            }
        }

        /// <summary>
        /// Cleans up pools
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Pools != null)
                Pools.Clear();
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
                    pool.Age++;

                // remove all spoiled pools
                double spoiled = Pools.Where(a => a.Age >= UseByAge).Sum(a => a.Amount);
                if (spoiled > 0)
                {
                    Pools.RemoveAll(a => a.Age >= UseByAge);
                    // report spoiled loss
                    ReportTransaction(TransactionType.Loss, spoiled, this, "", "Spoiled", this);
                }
            }
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if ((Units ?? "").ToUpper() != "KG")
                {
                    htmlWriter.Write("Each unit of this resource is equivalent to ");
                    if (ConvertToKg == 0)
                        htmlWriter.Write("<span class=\"errorlink\">NOT SET");
                    else
                        htmlWriter.Write("<span class=\"setvalue\">" + this.ConvertToKg.ToString("0.###"));
                    htmlWriter.Write("</span> kg");
                }
                else
                {
                    if (ConvertToKg != 1)
                        htmlWriter.Write("<span class=\"errorlink\">SET UnitsToKg to 1</span> as this Food Type is measured in kg");
                }

                htmlWriter.Write("\r\n</div>");
                if (StartingAmount > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("The simulation starts with <span class=\"setvalue\">" + this.StartingAmount.ToString("0.###") + "</span>");
                    if (StartingAge > 0)
                        htmlWriter.Write(" with an age of <span class=\"setvalue\">" + this.StartingAge.ToString("###") + "%</span> months");

                    htmlWriter.Write("\r\n</div>");
                }

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (UseByAge == 0)
                    htmlWriter.Write("This food does not spoil");
                else
                    htmlWriter.Write("This food must be consumed before <span class=\"setvalue\">" + this.UseByAge.ToString("###") + "</span> month" + ((UseByAge > 1) ? "s" : "") + " old");

                htmlWriter.Write("\r\n</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\"><span class=\"setvalue\">");
                htmlWriter.Write(((EdibleProportion == 1) ? "All" : EdibleProportion.ToString("#0%")) + "</span> of this raw food is edible");
                htmlWriter.Write("\r\n</div>");

                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}