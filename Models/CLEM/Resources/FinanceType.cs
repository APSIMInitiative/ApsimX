using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;

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
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Finance/FinanceType.htm")]
    public class FinanceType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units")]
        public string Units { get { return (Parent as Finance).CurrencyName; } }

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
            this.amount = 0;
            if (OpeningBalance > 0)
            {
                Add(OpeningBalance, this, "", "Opening balance");
            }
        }

        #region Transactions

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

        /// <summary>
        /// Add money to account
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            double multiplier = 0;
            double addAmount = 0;

            switch (resourceAmount.GetType().Name)
            {
                case "Double":
                    addAmount = (double)resourceAmount;
                    break;
                case "ResourceRequest":
                    addAmount = (resourceAmount as ResourceRequest).Required;
                    multiplier = (resourceAmount as ResourceRequest).MarketTransactionMultiplier;
                    break;
                default:
                    throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }

            if (addAmount > 0)
            {
                addAmount = Math.Round(addAmount, 2, MidpointRounding.ToEven);
                amount += addAmount;

                ResourceTransaction details = new ResourceTransaction
                {
                    Gain = addAmount,
                    Activity = activity,
                    RelatesToResource = relatesToResource,
                    Category = category,
                    ResourceType = this
                };
                LastTransaction = details;
                LastGain = addAmount;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);

                // if this request aims to trade with a market see if we need to set up details for the first time
                if (multiplier > 0)
                {
                    FindEquivalentMarketStore();
                    if (EquivalentMarketStore != null)
                    {
                        (resourceAmount as ResourceRequest).Required *= (resourceAmount as ResourceRequest).MarketTransactionMultiplier;
                        (resourceAmount as ResourceRequest).MarketTransactionMultiplier = 0;
                        (resourceAmount as ResourceRequest).Category = "Farm transaction";
                        (resourceAmount as ResourceRequest).RelatesToResource = EquivalentMarketStore.NameWithParent;
                        (EquivalentMarketStore as FinanceType).Remove(resourceAmount as ResourceRequest);
                    }
                }
            }
        }

        /// <summary>
        /// Remove from finance type store
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

            double amountRemoved = Math.Round(request.Required, 2, MidpointRounding.ToEven); 
            
            // more than positive balance can be taken if withdrawal limit set to false
            if(this.EnforceWithdrawalLimit)
            {
                amountRemoved = Math.Min(amountRemoved, FundsAvailable);
            }

            if (amountRemoved == 0)
            {
                return;
            }

            this.amount -= amountRemoved;

            // send to market if needed
            if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
            {
                (EquivalentMarketStore as FinanceType).Add(amountRemoved * request.MarketTransactionMultiplier, request.ActivityModel, this.NameWithParent,  "Farm purchases");
            }

            request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction
            {
                ResourceType = this,
                Loss = amountRemoved,
                Activity = request.ActivityModel,
                RelatesToResource = request.RelatesToResource,
                Category = request.Category
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            amount = Math.Round(newAmount, 2, MidpointRounding.ToEven);
        }

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
                htmlWriter.Write("Opening balance of <span class=\"setvalue\">" + this.OpeningBalance.ToString("#,##0.00") + "</span>");
                if (this.EnforceWithdrawalLimit)
                {
                    htmlWriter.Write(" that can be withdrawn to <span class=\"setvalue\">" + this.WithdrawalLimit.ToString("#,##0.00") + "</span>");
                }
                else
                {
                    htmlWriter.Write(" with no withdrawal limit");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (this.InterestRateCharged + this.InterestRatePaid == 0)
                {
                    htmlWriter.Write("No interest rates included");
                }
                else
                {
                    htmlWriter.Write("Interest rate of ");
                    if (this.InterestRateCharged > 0)
                    {
                        htmlWriter.Write("<span class=\"setvalue\">");
                        htmlWriter.Write(this.InterestRateCharged.ToString("0.##") + "</span>% charged ");
                        if (this.InterestRatePaid > 0)
                        {
                            htmlWriter.Write("and ");
                        }
                    }
                    if (this.InterestRatePaid > 0)
                    {
                        htmlWriter.Write("<span class=\"setvalue\">");
                        htmlWriter.Write(this.InterestRatePaid.ToString("0.##") + "</span>% paid");
                    }
                }
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
