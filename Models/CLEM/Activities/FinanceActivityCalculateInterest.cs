using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Linq;
using Models.Core.Attributes;
using System.IO;

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
                foreach (FinanceType accnt in Structure.FindChildren<FinanceType>(relativeTo: finance))
                {
                    if (accnt.Balance > 0)
                    {
                        if (accnt.InterestRatePaid > 0)
                        {
                            accnt.Add(accnt.Balance * accnt.InterestRatePaid / 1200, this, null, "Interest");
                            SetStatusSuccessOrPartial();
                        }
                    }
                    else if (accnt.Balance < 0)
                    {
                        double interest = Math.Round(Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200, 2, MidpointRounding.ToEven);
                        if (Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200 != 0)
                        {
                            ResourceRequest interestRequest = new ResourceRequest
                            {
                                ActivityModel = this,
                                Required = interest,
                                AllowTransmutation = false,
                                Category = TransactionCategory
                            };
                            accnt.Remove(interestRequest);

                            // report status
                            if (interestRequest.Required > interestRequest.Provided)
                            {
                                interestRequest.ResourceType = typeof(Finance);
                                interestRequest.ResourceTypeName = accnt.NameWithParent;
                                interestRequest.Available = accnt.FundsAvailable;
                                ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = interestRequest };
                                ActivitiesHolder.ReportActivityShortfall(rre);

                                switch (OnPartialResourcesAvailableAction)
                                {
                                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                        throw new ApsimXException(this, String.Format("Insufficient funds in [r={0}] to pay interest charged.\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.", accnt.Name));
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
                                Status = ActivityStatus.Success;
                        }
                    }
                }
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                ZoneCLEM clemParent = FindAncestor<ZoneCLEM>();
                ResourcesHolder resHolder;
                Finance finance = null;
                if (clemParent != null)
                {
                    resHolder = Structure.FindChildren<ResourcesHolder>(relativeTo: clemParent).FirstOrDefault() as ResourcesHolder;
                    finance = resHolder.FindResourceGroup<Finance>();
                    if (finance != null && !finance.Enabled)
                        finance = null;
                }

                if (finance == null)
                    htmlWriter.Write("\r\n<div class=\"activityentry\">This activity is not required as no <span class=\"resourcelink\">Finance</span> resource is available.</div>");
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Interest rates are set in the <span class=\"resourcelink\">FinanceType</span> component</div>");
                    foreach (FinanceType accnt in Structure.FindChildren<FinanceType>(relativeTo: finance).Where(a => a.Enabled))
                    {
                        if (accnt.InterestRateCharged == 0 & accnt.InterestRatePaid == 0)
                            htmlWriter.Write("\r\n<div class=\"activityentry\">This activity is not needed for <span class=\"resourcelink\">" + accnt.Name + "</span> as no interest rates are set.</div>");
                        else
                        {
                            if (accnt.InterestRateCharged > 0)
                                htmlWriter.Write($"\r\n<div class=\"activityentry\">This activity will calculate interest charged for <span class=\"resourcelink\">" + accnt.Name + "</span> at a rate of <span class=\"setvalue\">" + accnt.InterestRateCharged.ToString("#.00") + "</span>%</div>");
                            else
                                htmlWriter.Write($"\r\n<div class=\"activityentry\">This activity will calculate interest paid for <span class=\"resourcelink\">" + accnt.Name + "</span> at a rate of <span class=\"setvalue\">" + accnt.InterestRatePaid.ToString("#.00") + "</span>%</div>");
                        }
                    }
                }
                return htmlWriter.ToString();
            }
        }
        #endregion

    }
}
