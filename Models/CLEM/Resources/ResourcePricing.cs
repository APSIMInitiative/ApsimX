using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Resource type pricing
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStoreType))]
    [ValidParent(ParentType = typeof(EquipmentType))]
    [ValidParent(ParentType = typeof(GrazeFoodStoreType))]
    [ValidParent(ParentType = typeof(GreenhouseGasesType))]
    [ValidParent(ParentType = typeof(HumanFoodStoreType))]
    [ValidParent(ParentType = typeof(LandType))]
    [ValidParent(ParentType = typeof(ProductStoreType))]
    [ValidParent(ParentType = typeof(ProductStoreTypeManure))]
    [ValidParent(ParentType = typeof(WaterType))]
    [Description("Defines the pricing of a resource type")]
    [Version(1, 0, 2, "Includes option to specify sale and purchase pricing")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/ResourcePricing.htm")]
    public class ResourcePricing : CLEMModel, IResourcePricing, IReportPricingChange
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

        /// <inheritdoc/>
        [JsonIgnore]
        public ResourcePriceChangeDetails LastPriceChange { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourcePricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Calulate the value of an amount of resource 
        /// </summary>
        /// <param name="amount">Amount of resource to value</param>
        /// <param name="respectUseWholePacket">Determing if purchase in whole packets is to be obeyed in calculation</param>
        public double CalculateValue(double amount, bool respectUseWholePacket = true)
        {
            if (PurchaseOrSale == PurchaseOrSalePricingStyleType.Sale)
            {
                throw new ApsimXException(this, "Cannot calculate the purchase price based on a sale pricing");
            }
            else
            {
                var packets = (amount / PacketSize);
                if (respectUseWholePacket && UseWholePackets)
                    packets = Math.Truncate(packets);

                return packets * PricePerPacket;
            }
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public IResourceType Resource { get { return FindAncestor<IResourceType>(); } }

        /// <inheritdoc/>
        [JsonIgnore]
        public double CurrentPrice { get { return PricePerPacket; } }

        /// <inheritdoc/>
        [JsonIgnore]
        public double PreviousPrice { get; set; }

        /// <inheritdoc/>
        public event EventHandler PriceChangeOccurred;

        /// <inheritdoc/>
        public void SetPrice(double amount, IModel model)
        {
            PreviousPrice = CurrentPrice;
            PricePerPacket = amount;

            if (LastPriceChange is null)
                LastPriceChange = new ResourcePriceChangeDetails();

            LastPriceChange.ChangedBy = model;
            LastPriceChange.PriceChanged = this;

            // price change event
            OnPriceChanged(new PriceChangeEventArgs() { Details = LastPriceChange });
        }

        /// <summary>
        /// Price changed event
        /// </summary>
        /// <param name="e"></param>
        protected void OnPriceChanged(PriceChangeEventArgs e)
        {
            PriceChangeOccurred?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
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
                    htmlWriter.Write("only in whole ");
                else
                    htmlWriter.Write("in ");

                htmlWriter.Write("packets ");
                if (PacketSize > 0)
                    htmlWriter.Write("<span class=\"setvalue\">" + this.PacketSize.ToString("#.###") + "</span>");
                else
                    htmlWriter.Write("<span class=\"errorlink\">Not defined</span>");

                htmlWriter.Write(" unit" + ((this.PacketSize == 1) ? "" : "s"));
                htmlWriter.Write(" in size\r\n</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">\r\nEach packet is worth ");
                if (PricePerPacket > 0)
                    htmlWriter.Write("<span class=\"setvalue\">" + this.PricePerPacket.ToString("#.00") + "</span>");
                else
                    htmlWriter.Write("<span class=\"errorlink\">Not defined</span>");

                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
