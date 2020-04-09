using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Resource type pricing
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStoreType))]
    [ValidParent(ParentType = typeof(EquipmentType))]
    [ValidParent(ParentType = typeof(GrazeFoodStoreType))]
    [ValidParent(ParentType = typeof(GreenhouseGasesType))]
    [ValidParent(ParentType = typeof(HumanFoodStoreType))]
    [ValidParent(ParentType = typeof(LandType))]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [ValidParent(ParentType = typeof(ProductStoreType))]
    [ValidParent(ParentType = typeof(ProductStoreTypeManure))]
    [ValidParent(ParentType = typeof(WaterType))]
    [Description("This component defines the pricing of a resource type")]
    [Version(1, 0, 2, "Includes option to specify sale and purchase pricing")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/ResourcePricing.htm")]
    public class ResourcePricing : CLEMModel
    {
        /// <summary>
        /// Number of resource units per packet
        /// </summary>
        [Description("Size of packet")]
        [Required]
        public double PacketSize { get; set; }

        /// <summary>
        /// Buy and sell as whole packets
        /// </summary>
        [Description("Only buy and sell whole packets")]
        [Required]
        public bool UseWholePackets { get; set; }

        /// <summary>
        /// Price of packet
        /// </summary>
        [Description("Price per packet")]
        [Required]
        public double PricePerPacket { get; set; }

        /// <summary>
        /// Determine whether this is a purchase or sale price, or both
        /// </summary>
        [Description("Purchase or sale price")]
        [System.ComponentModel.DefaultValueAttribute(PurchaseOrSalePricingStyleType.Both)]
        [Required]
        public PurchaseOrSalePricingStyleType PurchaseOrSale { get; set; }

        /// <summary>
        /// Is the packet currently available
        /// </summary>
        public bool TimingOK
        {
            get
            {
                int res = this.Children.Where(a => typeof(IActivityTimer).IsAssignableFrom(a.GetType())).Sum(a => (a as IActivityTimer).ActivityDue ? 0 : 1);

                var q = this.Children.Where(a => typeof(IActivityTimer).IsAssignableFrom(a.GetType()));
                var w = q.Sum(a => (a as IActivityTimer).ActivityDue ? 0 : 1);

                return (res==0);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourcePricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "\n<div class=\"activityentry\">";
            html += "\nThis is a <span class=\"setvalue\">";
            switch (PurchaseOrSale)
            {
                case PurchaseOrSalePricingStyleType.Both:
                    html += "purchase and sell";
                    break;
                case PurchaseOrSalePricingStyleType.Purchase:
                    html += "purchase";
                    break;
                case PurchaseOrSalePricingStyleType.Sale:
                    html += "sell";
                    break;
                default:
                    break;
            }
            html += "</span> price</div>";

            html += "\n<div class=\"activityentry\">";
            html += "\nThis resource is managed ";
            if (UseWholePackets)
            {
                html += "only in whole ";
            }
            else
            {
                html += "in ";
            }
            html += "packets ";
            if (PacketSize > 0)
            {
                html += "<span class=\"setvalue\">" + this.PacketSize.ToString("#.###") + "</span>";
            }
            else
            {
                html += "<span class=\"errorlink\">Not defined</span>";
            }
            html += " unit" + ((this.PacketSize == 1) ? "" : "s");
            html += " in size\n</div>";

            html += "\n<div class=\"activityentry\">\nEach packet is worth ";
            if (PricePerPacket > 0)
            {
                html += "<span class=\"setvalue\">" + this.PricePerPacket.ToString("#.00") + "</span>";
            }
            else
            {
                html += "<span class=\"errorlink\">Not defined</span>";
            }
            html += "\n</div>";
            return html;
        }

    }
}
