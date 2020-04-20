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
    /// This stores the initialisation parameters for a fodder type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStore))]
    [Description("This resource represents an animal food store (e.g. Lucerne).")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/AnimalFoodStore/AnimalFoodStoreType.htm")]
    public class AnimalFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IFeedType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; private set; }

        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        [Required, Percentage, GreaterThanValue(0)]
        public double DMD { get; set; }

        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        [Required, Percentage, GreaterThanValue(0)]
        public double Nitrogen { get; set; }

        /// <summary>
        /// Current store nitrogen (%)
        /// </summary>
        [XmlIgnore]
        public double CurrentStoreNitrogen { get; set; }

        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Amount currently available (kg dry)
        /// </summary>
        [XmlIgnore]
        public double Amount { get { return amount; } set { return; } }
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalFoodStoreType()
        {
            Units = "kg";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            this.amount = 0;
            if (StartingAmount > 0)
            {
                Add(StartingAmount, this, "Starting value");
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
            double addAmount;
            double nAdded;
            switch (resourceAmount.GetType().ToString())
            {
                case "System.Double":
                    addAmount = (double)resourceAmount;
                    nAdded = Nitrogen;
                    break;
                case "Models.CLEM.Resources.FoodResourcePacket":
                    addAmount = ((FoodResourcePacket)resourceAmount).Amount;
                    nAdded = ((FoodResourcePacket)resourceAmount).PercentN;
                    break;
                default:
                    throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }

            // update N based on new input added
            CurrentStoreNitrogen = ((CurrentStoreNitrogen*Amount) + (nAdded * addAmount)) / (Amount + addAmount);

            this.amount += addAmount;

            ResourceTransaction details = new ResourceTransaction
            {
                Gain = addAmount,
                Activity = activity,
                Reason = reason,
                ResourceType = this
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Remove from animal food store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            // if this request aims to trade with a market see if we need to set up details for the first time
            if (request.MarketTransactionMultiplier > 0)
            {
                FindEquivalentMarketStore();
            }

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.amount, amountRemoved);
            this.amount -= amountRemoved;

            FoodResourcePacket additionalDetails = new FoodResourcePacket
            {
                DMD = this.DMD,
                PercentN = this.CurrentStoreNitrogen
            };
            request.AdditionalDetails = additionalDetails;

            request.Provided = amountRemoved;

            // send to market if needed
            if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
            {
                additionalDetails.Amount = amountRemoved * request.MarketTransactionMultiplier;
                (EquivalentMarketStore as AnimalFoodStoreType).Add(additionalDetails, request.ActivityModel, "Farm sales");
            }

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
            return;
        }

        /// <summary>
        /// Set amount of animal food available
        /// </summary>
        /// <param name="newValue">New value to set food store to</param>
        public new void Set(double newValue)
        {
            this.amount = newValue;
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
            string html = "";
            html += "<div class=\"activityentry\">";
            html += "This food has a nitrogen content of <span class=\"setvalue\">" + this.Nitrogen.ToString("0.###")+"%</span>";
            if(DMD > 0)
            {
                html += " and a Dry Matter Digesibility of <span class=\"setvalue\">" + this.DMD.ToString("0.###") + "%</span>";
            }
            else
            {
                html += " and a Dry Matter Digesibility estimated from N%";
            }
            html += "</div>";
            if (StartingAmount > 0)
            {
                html += "<div class=\"activityentry\">";
                html += "Simulation starts with <span class=\"setvalue\">" + this.StartingAmount.ToString("#,##0.##") + "</span> kg";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            return "";
        }

    }
}