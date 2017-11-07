// -----------------------------------------------------------------------
// <copyright file="Irrigation.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Models
{
    using System;
    using Models.Core;
    using System.Xml.Serialization;

    /// <summary>The irrigation class.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Irrigation : Model, IIrrigation
    {
        /// <summary>Access the summary model.</summary>
        [Link] private ISummary Summary = null;

        /// <summary>Gets or sets the amount of irrigation applied (mm).</summary>
        [XmlIgnore]
        public double IrrigationApplied { get; private set; }

        /// <summary>Gets or sets the depth at which irrigation is applied (mm).</summary>
        [XmlIgnore]
        public double Depth { get; set; }

        /// <summary>Gets or sets the duration that irrigation is applied for (hrs).</summary>
        [XmlIgnore]
        public double Duration { get; set; }

        /// <summary>Gets or sets the flag for whether the irrigation can run off (true/false).</summary>
        [XmlIgnore]
        public bool WillRunoff { get; set; }

        /// <summary>Occurs when [irrigated].</summary>
        /// <remarks>Advertises an irrigation and allows other models to respond accordingly.</remarks>
        public event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Apply some irrigation.</summary>
        /// <param name="amount">The amount to apply (mm).</param>
        /// <param name="depth">The depth of application (mm).</param>
        /// <param name="duration">The duration of irrigation event (hrs)</param>
        /// <param name="efficiency">The irrigation efficiency (mm/mm).</param>
        /// <param name="willRunoff">Whether irrigation can run off (<c>true</c>/<c>false</c>).</param>
        /// <exception cref="ApsimXException">Efficiency value for irrigation event must be between 0 and 1 </exception>
        public void Apply(double amount, double depth = 0.0, double duration = 1, double efficiency = 1.0, bool willRunoff = false)
        {
            if (Irrigated != null && amount > 0)
            {
                // Check the parameters given
                if (efficiency > 1.0 || efficiency < 0)
                    throw new ApsimXException(this, "Efficiency value for irrigation event must bet between 0 and 1 ");

                IrrigationApplied = amount;
                WillRunoff = willRunoff;
                Depth = depth;
                Duration = duration;

                // Prepare the irrigation data
                Models.Soils.IrrigationApplicationType water = new Models.Soils.IrrigationApplicationType();
                water.Amount = amount * efficiency;
                water.Depth = depth;
                water.will_runoff = willRunoff;
                water.Duration = duration;

                // Raise event and write log
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
