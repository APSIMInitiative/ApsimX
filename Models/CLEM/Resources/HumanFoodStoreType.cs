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
    [Description("This resource represents a human food store (e.g. Eggs).")]
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
        /// Dry Matter (%)
        /// </summary>
        [Description("Dry Matter (%)")]
        [Required, Percentage]
        public double DryMatter { get; set; }

        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        [Required, Percentage]
        public double DMD { get; set; }

        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        [Required, Percentage]
        public double Nitrogen { get; set; }

        /// <summary>
        /// Current store nitrogen (%)
        /// </summary>
        [XmlIgnore]
        [Description("Current store nitrogen (%)")]
        [Required, Percentage]
        public double CurrentStoreNitrogen { get; set; }

        /// <summary>
        /// Starting Age of the Fodder (Months)
        /// </summary>
        [Description("Starting Age of Human Food (Months)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAge { get; set; }

        /// <summary>
        /// Starting Amount
        /// </summary>
        [Description("Starting Amount")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

        /// <summary>
        /// The maximum amount of this food that can be eaten by an adult equivalent in a day
        /// </summary>
        [Description("Maximum daily intake per AE")]
        [Required, GreaterThanEqualValue(0)]
        public double MaximumDailyIntakePerAE { get; set; }

        /// <summary>
        /// Age of this Human Food (Months)
        /// </summary>
        [XmlIgnore]
        public double Age { get; set; } 

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [XmlIgnore]
        public double Amount { get { return amount; } }
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            this.Age = this.StartingAge;
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
            CurrentStoreNitrogen = ((Nitrogen / 100 * Amount) + (nAdded / 100 * addAmount)) / (Amount + addAmount) * 100;

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
        /// Remove from human food store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.amount, amountRemoved);
            this.amount -= amountRemoved;

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

    }
}