using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    public class FinanceActivityCalculateInterest : CLEMActivityBase
    {
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
                        accnt.Add(accnt.Balance * accnt.InterestRatePaid / 1200, this.Name, "Interest earned");
                        SetStatusSuccess();
                    }
                    else
                    {
                        if (Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200 != 0)
                        {
                            ResourceRequest interestRequest = new ResourceRequest();
                            interestRequest.ActivityModel = this;
                            interestRequest.Required = Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200;
                            interestRequest.AllowTransmutation = false;
                            interestRequest.Reason = "Pay interest charged";
                            accnt.Remove(interestRequest);
    
                            // report status
                            if(interestRequest.Required > interestRequest.Provided)
                            {
                                switch (OnPartialResourcesAvailableAction)
                                {
                                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                        throw new ApsimXException(this, "Insufficient finances to pay interest charged");
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }


    }
}
