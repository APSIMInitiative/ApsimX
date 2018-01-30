using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for bank account
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Finance))]
    [Description("This resource represents a finance type (e.g. General bank account).")]
    public class FinanceType : CLEMModel, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Opening balance
        /// </summary>
        [Description("Opening balance")]
        [Required]
        public double OpeningBalance { get; set; }

        /// <summary>
        /// Enforce withdrawal limit
        /// </summary>
        [Description("Enforce withdrawal limit. (false, no limit to spending)")]
        [Required]
        public bool EnforceWithdrawalLimit { get; set; }

        /// <summary>
        /// The amount this account can be withdrawn to (-ve)
        /// </summary>
        [Description("The amount this account can be withdrawn to (<0 credit, 0 no credit)")]
        [Required ]
        public double WithdrawalLimit { get; set; }

        /// <summary>
        /// Interest rate (%) charged on negative balance
        /// </summary>
        [Description("Interest rate (%) charged on negative balance")]
        [Required, Percentage]
        public double InterestRateCharged { get; set; }

        /// <summary>
        /// Interest rate (%) paid on positive balance
        /// </summary>
        [Description("Interest rate (%) paid on positive balance")]
        [Required, Percentage]
        public double InterestRatePaid { get; set; }

        /// <summary>
        /// Current funds available
        /// </summary>
        public double FundsAvailable
        {
            get
            {
                if(!EnforceWithdrawalLimit)
                {
                    return double.PositiveInfinity;
                }
                else
                {
                    return amount - WithdrawalLimit;
                }
            }
        }

        /// <summary>
        /// Current balance
        /// </summary>
        public double Balance { get { return amount; } }

        private double amount;
        /// <summary>
        /// Current amount of this resource
        /// </summary>
        public double Amount
        {
            get
            {
                return FundsAvailable;
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

        /// <summary>
        /// Initialise resource type
        /// </summary>
        public void Initialise()
        {
            this.amount = 0;
            if (OpeningBalance > 0)
            {
                Add(OpeningBalance, "Bank", "Opening balance");
            }
        }

        #region transactions

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
        /// Add money to account
        /// </summary>
        /// <param name="ResourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="ActivityName"></param>
        /// <param name="Reason"></param>
        public void Add(object ResourceAmount, string ActivityName, string Reason)
        {
            if(ResourceAmount.GetType().ToString()!="System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", ResourceAmount.GetType().ToString(), this.Name));
            }
            double addAmount = (double)ResourceAmount;
            if (addAmount>0)
            {
                addAmount = Math.Round(addAmount, 2, MidpointRounding.ToEven);
                amount += addAmount;

                ResourceTransaction details = new ResourceTransaction();
                details.Credit = addAmount;
                details.Activity = ActivityName;
                details.Reason = Reason;
                details.ResourceType = this.Name;
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// Remove money (object) from account
        /// </summary>
        /// <param name="RemoveRequest"></param>
        public void Remove(object RemoveRequest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="Request">Resource request class with details.</param>
        public void Remove(ResourceRequest Request)
        {
            if (Request.Required == 0) return;
            double amountRemoved = Math.Round(Request.Required, 2, MidpointRounding.ToEven); 
            // avoid taking too much
            amountRemoved = Math.Min(this.Amount, amountRemoved);
            if (amountRemoved == 0) return;

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

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="NewAmount"></param>
        public void Set(double NewAmount)
        {
            amount = Math.Round(NewAmount, 2, MidpointRounding.ToEven);
        }

        #endregion
    }
}
