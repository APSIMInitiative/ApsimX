using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
        /// Constructor
        /// </summary>
        public ResourcePricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\nThis is a <span class=\"setvalue\">");
                switch (PurchaseOrSale)
                {
                    case PurchaseOrSalePricingStyleType.Both:
                        htmlWriter.Write("purchase and sell");
                        break;
                    case PurchaseOrSalePricingStyleType.Purchase:
                        htmlWriter.Write("purchase");
                        break;
                    case PurchaseOrSalePricingStyleType.Sale:
                        htmlWriter.Write("sell");
                        break;
                    default:
                        break;
                }
                htmlWriter.Write("</span> price</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\nThis resource is managed ");
                if (UseWholePackets)
                {
                    htmlWriter.Write("only in whole ");
                }
                else
                {
                    htmlWriter.Write("in ");
                }
                htmlWriter.Write("packets ");
                if (PacketSize > 0)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + this.PacketSize.ToString("#.###") + "</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not defined</span>");
                }
                htmlWriter.Write(" unit" + ((this.PacketSize == 1) ? "" : "s"));
                htmlWriter.Write(" in size\r\n</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">\r\nEach packet is worth ");
                if (PricePerPacket > 0)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + this.PricePerPacket.ToString("#.00") + "</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not defined</span>");
                }
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}
