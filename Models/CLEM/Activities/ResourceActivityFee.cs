using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Resource cost</summary>
    /// <summary>This activity will arrange payment of a resource activity expense</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourceActivityProcess))]
    [Description("Provides a fee required to perform processing of a resource")]
    [HelpUri(@"Content/Features/Activities/All resources/ResourceFee.htm")]
    [Version(1, 0, 1, "")]
    public class ResourceActivityFee: CLEMModel
    {
        [Link]
        private ResourcesHolder resources = null;

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Category for transactions required")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(ResourcePaymentStyleType.Fixed)]
        [Description("Payment style")]
        [Required]
        public ResourcePaymentStyleType PaymentStyle { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        [JsonIgnore]
        public FinanceType BankAccount { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceActivityFee()
        {
            this.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            TransactionCategory = "Expense";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
        }


        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.##") + "</span> ");
                htmlWriter.Write("<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> from ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(AccountName, "Account not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 

    }
}
