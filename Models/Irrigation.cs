using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models
{
    /// <summary>Interface for an irrigation class.</summary>
    public interface IIrrigation
    {
        /// <summary>The amount of irrigation applied.</summary>
        double IrrigationApplied { get; }

        /// <summary>Indicates whether the irrigation should runoff.</summary>
        bool WillRunoff { get; }

        /// <summary>Indicates the depth of irrigation.</summary>
        double Depth { get; }

        /// <summary>Invoked when an irrigation occurs.</summary>
        event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Called to apply irrigation.</summary>
        /// <param name="amount">Amount of irrigation (mm)</param>
        /// <param name="depth">Depth of irrigation (mm)</param>
        /// <param name="efficiency">Efficiency of irrigation (%)</param>
        /// <param name="willRunoff">Irrigation will runoff if true.</param>
        void Apply(double amount, double depth = 0, double efficiency = 1, bool willRunoff = false);
    }


    /// <summary>Irrigation class.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Irrigation : Model, IIrrigation
    {
        /// <summary>The summary</summary>
        [Link] private ISummary Summary = null;

        /// <summary>Gets or sets the irrigation applied.</summary>
        /// <value>The irrigation applied.</value>
        [XmlIgnore]
        public double IrrigationApplied { get; set; }

        /// <summary>Indicates whether the irrigation should runoff.</summary>
        [XmlIgnore]
        public bool WillRunoff { get; set; }

        /// <summary>Indicates the depth of irrigation.</summary>
        public double Depth { get; set; }

        // Events we're going to send.
        /// <summary>Occurs when [irrigated].</summary>
        public event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Apply irrigatopm.</summary>
        /// <param name="amount">The amount.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="efficiency">The efficiency.</param>
        /// <param name="willRunoff">if set to <c>true</c> [will runoff].</param>
        /// <exception cref="ApsimXException">Efficiency value for irrigation event must bet between 0 and 1 </exception>
        public void Apply(double amount, double depth = 0.0, double efficiency = 1.0, bool willRunoff = false)
        {
            if (Irrigated != null && amount > 0)
            {
                if(efficiency > 1.0 || efficiency < 0)
                    throw new ApsimXException(this, "Efficiency value for irrigation event must bet between 0 and 1 ");

                Models.Soils.IrrigationApplicationType water = new Models.Soils.IrrigationApplicationType();
                water.Amount = amount * efficiency;
                water.Depth = depth;
                water.will_runoff = willRunoff;
                IrrigationApplied = amount;
                WillRunoff = willRunoff;
                Depth = depth;
                Irrigated.Invoke(this, water);
                Summary.WriteMessage(this, string.Format("{0:F1} mm of water added at depth {1}", amount * efficiency, depth));
            }
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            IrrigationApplied = 0;
            WillRunoff = false;
        }

    }
}
