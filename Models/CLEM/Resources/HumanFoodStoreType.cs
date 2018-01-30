using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;

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
    public class HumanFoodStoreType : CLEMModel, IResourceType, IResourceWithTransactionType
    {
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
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

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
        private double amount;

        /// <summary>
        /// Initialise the current state to the starting amount of fodder
        /// </summary>
        public void Initialise()
        {
            this.Age = this.StartingAge;
            this.amount = 0;
            if (StartingAmount > 0)
            {
                Add(StartingAmount, this.Name, "Starting value");
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Initialise();
        }

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="ResourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen in a Res) of food being added</param>
        /// <param name="ActivityName">Name of activity adding resource</param>
        /// <param name="Reason">Name of individual radding resource</param>
        public void Add(object ResourceAmount, string ActivityName, string Reason)
        {
            double addAmount = 0;
            double nAdded = 0;
            switch (ResourceAmount.GetType().ToString())
            {
                case "System.Double":
                    addAmount = (double)ResourceAmount;
                    nAdded = Nitrogen;
                    break;
                case "Models.CLEM.Resources.FoodResourcePacket":
                    addAmount = ((FoodResourcePacket)ResourceAmount).Amount;
                    nAdded = ((FoodResourcePacket)ResourceAmount).PercentN;
                    break;
                default:
                    throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", ResourceAmount.GetType().ToString(), this.Name));
            }

            // update N based on new input added
            CurrentStoreNitrogen = ((Nitrogen / 100 * Amount) + (nAdded / 100 * addAmount)) / (Amount + addAmount) * 100;

            this.amount = this.amount + addAmount;
            ResourceTransaction details = new ResourceTransaction();
            details.Credit = addAmount;
            details.Activity = ActivityName;
            details.Reason = Reason;
            details.ResourceType = this.Name;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Remove from human food store
        /// </summary>
        /// <param name="Request">Resource request class with details.</param>
        public void Remove(ResourceRequest Request)
        {
            if (Request.Required == 0) return;
            double amountRemoved = Request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.amount, amountRemoved);
            this.amount -= amountRemoved;

            Request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction();
            details.ResourceType = this.Name;
            details.Debit = amountRemoved * -1;
            details.Activity = Request.ActivityModel.Name;
            details.Reason = Request.Reason;
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        ///// <summary>
        ///// Remove from food store
        ///// </summary>
        ///// <param name="RemoveAmount">Amount to remove. NOTE: This is a positive value not a negative value.</param>
        ///// <param name="ActivityName">Name of activity requesting resource</param>
        ///// <param name="Reason">Name of individual requesting resource</param>
        //public double Remove(double RemoveAmount, string ActivityName, string Reason)
        //{
        //    double amountRemoved = RemoveAmount;
        //    if (this.amount - RemoveAmount < 0)
        //    {
        //        string message = "Tried to remove more " + this.Name + " than exists." + Environment.NewLine
        //            + "Current Amount: " + this.amount + Environment.NewLine
        //            + "Tried to Remove: " + RemoveAmount;
        //        Summary.WriteWarning(this, message);
        //        amountRemoved = this.amount;
        //        this.amount = 0;
        //    }
        //    else
        //    {
        //        this.amount = this.amount - RemoveAmount;
        //    }
        //    ResourceTransaction details = new ResourceTransaction();
        //    details.ResourceType = this.Name;
        //    details.Debit = amountRemoved * -1;
        //    details.Activity = ActivityName;
        //    details.Reason = Reason;
        //    LastTransaction = details;
        //    TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
        //    OnTransactionOccurred(te);
        //    return amountRemoved;
        //}

        /// <summary>
        /// Remove Food
        /// </summary>
        /// <param name="RemoveRequest">A feed request object with required information</param>
        public void Remove(object RemoveRequest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set amount of animal food available
        /// </summary>
        /// <param name="NewValue">New value to set food store to</param>
        public void Set(double NewValue)
        {
            this.amount = NewValue;
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

        #endregion

    }
}