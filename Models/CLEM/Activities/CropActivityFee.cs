using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
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
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Crop cost</summary>
    /// <summary>This activity will arrange payment of a crop task expense</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [Description("A fee required to perform a crop management task")]
    [HelpUri(@"Content/Features/Activities/Crop/CropFee.htm")]
    [Version(1, 0, 1, "")]
    public class CropActivityFee: CLEMActivityBase, IActivityCompanionModel
    {
        private string relatesToResourceName = "";
        private ResourceRequest resourceRequest;

        /// <summary>
        /// An identifier for this Fee based on parent requirements
        /// </summary>
        [Description("Fee identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        public string Identifier { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedUnits")]
        [Description("Payment style")]
        public string Units { get; set; }

        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanValue(0)]
        public double Amount { get; set; }

        /// <inheritdoc/>
        [Description("Allow finance shortfall to affect activity")]
        [Required]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ShortfallCanAffectParentActivity { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        public FinanceType BankAccount { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CropActivityFee()
        {
            this.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivityLevel2;
            TransactionCategory = "Crop.[Activity]";
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>At start of simulation</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
            relatesToResourceName = this.FindAncestor<CropActivityManageProduct>().StoreItemName;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            double metric = (Parent as CLEMActivityBase).ValueForCompanionModel(this);
            double charge = metric * Amount;

            if (MathUtilities.IsPositive(metric))
                Status = ActivityStatus.NotNeeded;

            resourceRequest = new ResourceRequest()
            {
                Resource = BankAccount,
                ResourceType = typeof(Finance),
                AllowTransmutation = true,
                Required = charge,
                Category = TransactionCategory,
                AdditionalDetails = this,
                RelatesToResource = relatesToResourceName,
                ActivityModel = this
            };
            return new List<ResourceRequest>() { resourceRequest };
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            bool shortfalls = MathUtilities.IsLessThan(resourceRequest.Provided, resourceRequest.Required);
            SetStatusSuccessOrPartial(shortfalls);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> ");
                htmlWriter.Write("<span class=\"setvalue\">" + "".ToString() + "</span> from ");
                if (AccountName == null || AccountName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[ACCOUNT NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + AccountName + "</span>");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">This activity uses a category label ");
                if (TransactionCategory != null && TransactionCategory != "")
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + TransactionCategory + "</span> ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">[NOT SET]</span> ");
                }
                htmlWriter.Write(" for all transactions</div>");

                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
