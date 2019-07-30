using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// CLEM Resource Type base model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Resource Type Base Class and should not be used directly.")]
    [Version(1, 0, 1, "")]
    public class CLEMResourceTypeBase : CLEMModel
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units")]
        public string Units { get; set; }

        /// <summary>
        /// Resource price
        /// </summary>
        public ResourcePricing Price
        {
            get
            {
                // find pricing that is ok;
                ResourcePricing price = Apsim.Children(this, typeof(ResourcePricing)).Where(a => (a as ResourcePricing).TimingOK).FirstOrDefault() as ResourcePricing;

                var q = Apsim.Children(this, typeof(ResourcePricing));
                var r = q.Where(a => (a as ResourcePricing).TimingOK);

                if (price == null)
                {
                    if (!priceWarningRaised)
                    {
                        string warn = "No pricing is available for [r=" + this.Name + "]";
                        if (Apsim.Children(this, typeof(ResourcePricing)).Count > 0)
                        {
                            warn += " in month [" + Clock.Today.ToString("MM yyyy") + "]";
                        }
                        warn += "\nNo financial transactions will occur and no packet size set.\nAdd [r=ResourcePricing] component to [r=" + this.Name + "] to improve purchase and sales.";
                        Summary.WriteWarning(this, warn);
                        priceWarningRaised = true;
                    }
                    return new ResourcePricing() { PricePerPacket=0, PacketSize=1, UseWholePackets=true };
                }
                return price;
            }
        }

        private bool priceWarningRaised = false;

        /// <summary>
        /// Add resources from various objects
        /// </summary>
        /// <param name="resourceAmount"></param>
        /// <param name="activity"></param>
        /// <param name="reason"></param>
        public void Add(object resourceAmount, CLEMModel activity, string reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove amount based on a ResourceRequest object
        /// </summary>
        /// <param name="request"></param>
        public void Remove(ResourceRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the amount of the resource. Use with caution as resources should be changed by add and remove methods.
        /// </summary>
        /// <param name="newAmount"></param>
        public void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

    }
}
