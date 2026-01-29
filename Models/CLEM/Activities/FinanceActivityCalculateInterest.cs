using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Linq;
using Models.Core.Attributes;
using System.IO;
using APSIM.Numerics;

namespace Models.CLEM.Activities
{
    /// <summary>Interest calculation activity</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs monthly interest calculations and transactions")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Finances/CalculateInterest.htm")]
    public class FinanceActivityCalculateInterest : CLEMActivityBase
    {
        private Finance finance;

        /// <summary>
        /// Constructor
        /// </summary>
        public FinanceActivityCalculateInterest()
        {
            TransactionCategory = "Interest";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            finance = Resources.FindResourceGroup<Finance>();
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            if (finance != null)
            {
                // make interest payments on bank accounts
                foreach (FinanceType account in Structure.FindChildren<FinanceType>(relativeTo: finance))
                {
                    if (MathUtilities.IsPositive(account.Balance))
                    {
                        if (MathUtilities.IsPositive(account.InterestRatePaid))
                        {
                            account.Add(account.Balance * account.InterestRatePaid / 1200, this, null, "Interest");
                            SetStatusSuccessOrPartial();
                        }
                    }
                    else if (MathUtilities.IsNegative(account.Balance))
                    {
                        double interest = Math.Round(Math.Abs(account.Balance) * account.InterestRateCharged / 1200, 2, MidpointRounding.ToEven);
                        if (Math.Abs(account.Balance) * account.InterestRateCharged / 1200 != 0)
                        {
                            ResourceRequest interestRequest = new()
                            {
                                ActivityModel = this,
                                Required = interest,
                                AllowTransmutation = false,
                                Category = TransactionCategory
                            };
                            account.Remove(interestRequest);

                            // report status
                            if (interestRequest.Required > interestRequest.Provided)
                            {
                                interestRequest.ResourceType = typeof(Finance);
                                interestRequest.ResourceTypeName = account.NameWithParent;
                                interestRequest.Available = account.FundsAvailable;
                                ResourceRequestEventArgs rre = new() { Request = interestRequest };
                                ActivitiesHolder.ReportActivityShortfall(rre);

                                switch (OnPartialResourcesAvailableAction)
                                {
                                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                        throw new ApsimXException(this, String.Format("Insufficient funds in [r={0}] to pay interest charged.\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.", account.Name));
                                    case OnPartialResourcesAvailableActionTypes.SkipActivity:
                                        Status = ActivityStatus.Ignored;
                                        break;
                                    case OnPartialResourcesAvailableActionTypes.UseAvailableResources:
                                        Status = ActivityStatus.Partial;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                Status = ActivityStatus.Success;
                            }
                        }
                    }
                }
            }
        }
    }
}
