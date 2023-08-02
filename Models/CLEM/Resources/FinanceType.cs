using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for bank account
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Finance))]
    [Description("This resource represents a finance store (e.g. general bank account)")]
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
        [Core.Display(Format = "N2")]
        public double OpeningBalance { get; set; }

        /// <summary>
        /// Enforce withdrawal limit
        /// </summary>
        [Description("Enforce withdrawal limit (false, no limit to spending)")]
        [Required]
        public bool EnforceWithdrawalLimit { get; set; }

        /// <summary>
        /// The amount this account can be withdrawn to (-ve)
        /// </summary>
        [Description("Withdrawal limit (<0 credit, 0 no credit)")]
        [Core.Display(EnabledCallback = "WithdrawalLimitEnabled")]
        [Required]
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
                if (!EnforceWithdrawalLimit)
                    return double.PositiveInfinity;
                else
                    return amount - WithdrawalLimit;
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
        [Core.Display(Format = "N2")]
        public double Amount
        {
            get
            {
                return FundsAvailable;
            }
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether the withdrawal limit has been set for enabling amount property
        /// </summary>
        /// <returns></returns>
        public bool WithdrawalLimitEnabled()
        {
            return EnforceWithdrawalLimit;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            this.amount = 0;
            if (OpeningBalance > 0)
                Add(OpeningBalance, null, null, "Opening balance");
        }

        #region Transactions

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
            double amountAdded;
            switch (resourceAmount)
            {
                case ResourceRequest _:
                    amountAdded = (resourceAmount as ResourceRequest).Required;
                    multiplier = (resourceAmount as ResourceRequest).MarketTransactionMultiplier;
                    break;
                case double _:
                    amountAdded = (double)resourceAmount;
                    break;
                default:
                    throw new Exception($"ResourceAmount object of type [{resourceAmount.GetType().Name}] is not supported in [r={Name}]");
            }

            if (amountAdded > 0)
            {
                amount += amountAdded;

                ReportTransaction(TransactionType.Gain, amountAdded, activity, relatesToResource, category, this);

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
                return;

            // if this request aims to trade with a market see if we need to set up details for the first time
            if (request.MarketTransactionMultiplier > 0)
                FindEquivalentMarketStore();

            double amountRemoved = request.Required;

            // more than positive balance can be taken if withdrawal limit set to false
            if (this.EnforceWithdrawalLimit)
                amountRemoved = Math.Min(amountRemoved, FundsAvailable);

            if (amountRemoved == 0)
                return;

            this.amount -= amountRemoved;

            // send to market if needed
            if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
                (EquivalentMarketStore as FinanceType).Add(amountRemoved * request.MarketTransactionMultiplier, request.ActivityModel, (request.RelatesToResource != "" ? request.RelatesToResource : this.NameWithParent), "Household purchase");

            request.Provided = amountRemoved;

            ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
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

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Opening balance of <span class=\"setvalue\">" + this.OpeningBalance.ToString("#,##0.00") + "</span>");
                if (this.EnforceWithdrawalLimit)
                    htmlWriter.Write(" that can be withdrawn to <span class=\"setvalue\">" + this.WithdrawalLimit.ToString("#,##0.00") + "</span>");
                else
                    htmlWriter.Write(" with no withdrawal limit");

                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (this.InterestRateCharged + this.InterestRatePaid == 0)
                    htmlWriter.Write("No interest rates included");

                else
                {
                    htmlWriter.Write("Interest rate of ");
                    if (this.InterestRateCharged > 0)
                    {
                        htmlWriter.Write("<span class=\"setvalue\">");
                        htmlWriter.Write(this.InterestRateCharged.ToString("0.##") + "</span>% charged ");
                        if (this.InterestRatePaid > 0)
                            htmlWriter.Write("and ");
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
