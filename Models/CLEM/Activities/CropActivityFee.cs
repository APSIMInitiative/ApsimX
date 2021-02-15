using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
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
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [Description("This is a fee required to perform a crop management task.")]
    [HelpUri(@"Content/Features/Activities/Crop/CropFee.htm")]
    [Version(1, 0, 1, "")]
    public class CropActivityFee: CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(Finance) })]
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
        /// Category label to use in ledger
        /// </summary>
        [Description("Shortname of fee for reporting")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Shortname required")]
        public string Category { get; set; }

        private string RelatesToResourceName = "";

        /// <summary>
        /// Constructor
        /// </summary>
        public CropActivityFee()
        {
            this.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivityLevel2;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = Resources.GetResourceItem(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;

            RelatesToResourceName = this.FindAncestor<CropActivityManageProduct>().StoreItemName;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requirement"></param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            return new GetDaysLabourRequiredReturnArgs(0, null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
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
                    AllowTransmutation = false,
                    Required = sumneeded,
                    ResourceType = typeof(Finance),
                    ResourceTypeName = AccountName,
                    ActivityModel = this,
                    FilterDetails = null,
                    RelatesToResource = RelatesToResourceName,
                    Category = Category
                }
                );
                return resourcesNeeded;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void DoActivity()
        {
            return;
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

        #region descriptive summary
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
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
                if (Category != null && Category != "")
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + Category + "</span> ");
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
