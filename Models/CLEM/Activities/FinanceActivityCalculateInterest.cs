using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>manage enterprise activity</summary>
    /// <summary>This activity undertakes the overheads of running the enterprise.</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity peforms monthly interest transactions.")]
    [Version(1, 0, 1, "")]
    public class FinanceActivityCalculateInterest : CLEMActivityBase
    {
        /// <summary>
        /// test for whether finances are included.
        /// </summary>
        private bool financesExist = false;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            financesExist = ((Resources.FinanceResource() != null));
        }

        /// <summary>An event handler to allow us to make all payments when needed</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            Status = ActivityStatus.NotNeeded;
            if (financesExist)
            {
                // make interest payments on bank accounts
                foreach (FinanceType accnt in Apsim.Children(Resources.FinanceResource(), typeof(FinanceType)))
                {
                    if (accnt.Balance > 0)
                    {
                        accnt.Add(accnt.Balance * accnt.InterestRatePaid / 1200, this, "Interest earned");
                        SetStatusSuccess();
                    }
                    else
                    {
                        double interest = Math.Round(Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200, 2, MidpointRounding.ToEven);
                        if (Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200 != 0)
                        {
                            ResourceRequest interestRequest = new ResourceRequest();
                            interestRequest.ActivityModel = this;
                            interestRequest.Required = interest;
                            interestRequest.AllowTransmutation = false;
                            interestRequest.Reason = "Pay interest charged";
                            accnt.Remove(interestRequest);
    
                            // report status
                            if(interestRequest.Required > interestRequest.Provided)
                            {
                                interestRequest.ResourceType = typeof(Finance);
                                interestRequest.ResourceTypeName = accnt.Name;
                                interestRequest.Available = accnt.FundsAvailable;
                                ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = interestRequest };
                                OnShortfallOccurred(rre);

                                switch (OnPartialResourcesAvailableAction)
                                {
                                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                        throw new ApsimXException(this, String.Format("Insufficient funds in [r={0}] to pay interest charged.\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.", accnt.Name));
                                    case OnPartialResourcesAvailableActionTypes.SkipActivity:
                                        Status = ActivityStatus.Ignored;
                                        break;
                                    case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
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

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "Interest rates are set in the <span class=\"resourcelink\">FinanceType</span> component";
            
            return html;
        }

    }
}
