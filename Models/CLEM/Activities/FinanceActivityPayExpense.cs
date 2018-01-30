using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to arrange and pay an enterprise expenses
    /// Expenses can be flagged as overheads for accounting
    /// </summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs payment of a specified expense.")]
    public class FinanceActivityPayExpense : CLEMActivityBase
    {
        /// <summary>
        /// Amount payable
        /// </summary>
        [Description("Amount payable")]
        [Required, GreaterThanValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// name of account to use
        /// </summary>
        [Description("Name of account to use")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Farm overhead
        /// </summary>
        [Description("Farm overhead")]
        [Required]
        public bool IsOverhead { get; set; }

        /// <summary>
        /// Month this overhead is next due.
        /// </summary>
        [XmlIgnore]
        public DateTime NextDueDate { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

        /// <summary>
        /// Constructor
        /// </summary>
        public FinanceActivityPayExpense()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            bankAccount = Resources.GetResourceItem(this, typeof(Finance), AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = new List<ResourceRequest>();
            if (this.TimingOK)
            {
                ResourceRequestList.Add(new ResourceRequest()
                {
                    Resource = bankAccount,
                    ResourceType = typeof(Finance),
                    AllowTransmutation = false,
                    Required = this.Amount,
                    ResourceTypeName = this.AccountName,
                    ActivityModel = this,
                    Reason = ((IsOverhead) ? "Overhead" : "Expense")
                }
                );
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            SetStatusSuccess();
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
