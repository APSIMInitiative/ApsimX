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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
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
        [Description("Interest rate charged on negative balance")]
        [Required, Percentage]
        [Units("%")]
        public double InterestRateCharged { get; set; }

        /// <summary>
        /// Interest rate (%) paid on positive balance
        /// </summary>
        [Description("Interest rate paid on positive balance")]
        [Required, Percentage]
        [Units("%")]
        public double InterestRatePaid { get; set; }

        /// <summary>
        /// Current funds available
        /// </summary>
        public double FundsAvailable
        {
            get
            {
                if (!EnforceWithdrawalLimit)
                {
                    return double.PositiveInfinity;
                }
                else
                {
                    return AmountAvailable - WithdrawalLimit;
                }
            }
        }

        /// <summary>
        /// Current balance
        /// </summary>
        public double Balance { get { return AmountAvailable; } }

        /// <inheritdoc/>
        [Core.Display(Format = "N2")]
        public new double AmountTotal => FundsAvailable;

        /// <inheritdoc/>
        public new double? Value => null;

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
            if (OpeningBalance > 0)
            {
                Add(OpeningBalance, null, null, "Opening balance");
            }
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
                Add(amountAdded);

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

        /// <inheritdoc/>
        public new double Remove(double amountToRemove, ResourceRequest pendingRequestActivity)
        {
            if (this.EnforceWithdrawalLimit)
            {
                amountToRemove = Math.Min(amountToRemove, FundsAvailable);
            }
            double amountRemoved = base.Remove(amountToRemove, pendingRequestActivity);
            return amountRemoved;
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            newAmount = Math.Round(newAmount, 2, MidpointRounding.ToEven);
            base.Set(newAmount);
        }

        #endregion

    }
}
