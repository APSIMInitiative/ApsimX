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
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "")]
    public class RuminantActivityFee: CLEMModel
    {
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
        /// Constructor
        /// </summary>
        public RuminantActivityFee()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Pay ";
            html += "<span class=\"setvalue\">" + Amount.ToString("#,##0.##") + "</span> ";
            html += "<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> ";
            html += "</div>";
            return html;
        }

    }
}
