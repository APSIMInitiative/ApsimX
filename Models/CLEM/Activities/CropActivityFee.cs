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
    /// <summary>Crop cost</summary>
    /// <summary>This activity will arrange payment of a crop task expense</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [Description("A fee required to perform a crop management task")]
    [HelpUri(@"Content/Features/Activities/Crop/CropFee.htm")]
    [Version(1, 0, 1, "")]
    public class CropActivityFee: CLEMActivityBase, IValidatableObject
    {
        private string relatesToResourceName = "";

        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(CropPaymentStyleType.perHa)]
        [Description("Payment style")]
        [Required]
        public CropPaymentStyleType PaymentStyle { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanValue(0)]
        public double Amount { get; set; }

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

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            CropActivityManageProduct productParent = FindAncestor<CropActivityManageProduct>();

            if (!productParent.IsTreeCrop)
            {
                if (this.PaymentStyle == CropPaymentStyleType.perTree)
                {
                    string[] memberNames = new string[] { this.Name + ".PaymentStyle" };
                    results.Add(new ValidationResult("The payment style " + this.PaymentStyle.ToString() + " is not supported for crops defined as non tree crops", memberNames));
                }
            }
            return results;
        }
        #endregion

        /// <inheritdoc/>
        protected override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            return new LabourRequiredArgs(0, null, null);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> DetermineResourcesForActivity(double argument = 0)
        {
            if (this.TimingOK)
            {
                List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();
                double sumneeded;
                switch (PaymentStyle)
                {
                    case CropPaymentStyleType.Fixed:
                        sumneeded = Amount;
                        break;
                    case CropPaymentStyleType.perUnitOfLand:
                        CropActivityManageCrop cropParent = FindAncestor<CropActivityManageCrop>();
                        sumneeded = cropParent.Area * Amount;
                        break;
                    case CropPaymentStyleType.perHa:
                        cropParent = FindAncestor<CropActivityManageCrop>();
                        CropActivityManageProduct productParent = FindAncestor<CropActivityManageProduct>();
                        sumneeded = cropParent.Area * productParent.UnitsToHaConverter * Amount;
                        break;
                    case CropPaymentStyleType.perTree:
                        cropParent = FindAncestor<CropActivityManageCrop>();
                        productParent = FindAncestor<CropActivityManageProduct>();
                        sumneeded = productParent.TreesPerHa * cropParent.Area * productParent.UnitsToHaConverter * Amount;
                        break;
                    default:
                        throw new Exception(String.Format("PaymentStyle ({0}) is not supported for ({1}) in ({2})", PaymentStyle, Name, this.Name));
                }
                resourcesNeeded.Add(new ResourceRequest()
                {
                    Resource = BankAccount,
                    AllowTransmutation = false,
                    Required = sumneeded,
                    ResourceType = typeof(Finance),
                    ResourceTypeName = AccountName,
                    ActivityModel = this,
                    FilterDetails = null,
                    RelatesToResource = relatesToResourceName,
                    Category = TransactionCategory
                }
                );
                return resourcesNeeded;
            }
            return null;
        }

        #region descriptive summary
        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> ");
                htmlWriter.Write("<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> from ");
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
