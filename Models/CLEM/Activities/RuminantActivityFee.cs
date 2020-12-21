using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd cost </summary>
    /// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
    [ValidParent(ParentType = typeof(RuminantActivityBreed))]
    [Description("This activity defines a specific herd expense for buying and selling ruminants or breeding and is based upon the current herd filtering for the parent activity.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFee.htm")]
    public class RuminantActivityFee: CLEMModel
    {
        /// <summary>
        /// Link to resources
        /// </summary>
        [Link]
        public ResourcesHolder Resources = null;

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string BankAccountName { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(AnimalPaymentStyleType.perHead)]
        [Description("Payment style")]
        [Required]
        public AnimalPaymentStyleType PaymentStyle { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Category label to use in ledger
        /// </summary>
        [Description("Shortname of fee for reporting")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Shortname required")]
        public string Category { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        public FinanceType BankAccount;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = Resources.GetResourceItem(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityFee()
        {
            this.SetDefaults();
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Pay ";
            html += "<span class=\"setvalue\">" + Amount.ToString("#,##0.##") + "</span> ";
            html += "<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> ";
            html += " from ";
            if (BankAccountName != null)
            {
                html += "<span class=\"resourcelink\">" + BankAccountName + "</span> ";
            }
            else
            {
                html += "<span class=\"errorlink\">[ACCOUNT NOT SET]</span> ";
            }
            html += "</div>";
            html += "\n<div class=\"activityentry\">This activity uses a category label ";
            if (Category != null && Category!="")
            {
                html += "<span class=\"setvalue\">" + Category + "</span> ";
            }
            else
            {
                html += "<span class=\"errorlink\">[NOT SET]</span> ";
            }
            html += " for all transactions</div>";
            return html;
        } 
        #endregion

    }
}
