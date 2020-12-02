namespace Models
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;
    using Models.Core;
    using Soils;
    /// <summary>
    /// This model controls irrigation events, which can be triggered using the Apply() method.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Irrigation : Model, IIrrigation
    {
        /// <summary>Access the summary model.</summary>
        [Link] private ISummary summary = null;
        
        /// <summary>Access the soil physical properties.</summary>
        [Link] private IPhysical soilPhysical = null;
        
        /// <summary>Gets the amount of irrigation actually applied (mm).</summary>
        [JsonIgnore]
        public double IrrigationApplied { get; private set; }

        /// <summary>Gets or sets the depth at which irrigation is applied (mm).</summary>
        [JsonIgnore]
        public double Depth { get; private set; }

        /// <summary>Gets or sets the duration of the irrigation event (minutes).</summary>
        [JsonIgnore]
        public double Duration { get; private set; }

        /// <summary>Gets or sets the efficiency of the irrigation system (mm/mm).</summary>
        [JsonIgnore]
        public double Efficiency { get; private set; }

        /// <summary>Gets or sets the flag for whether the irrigation can run off (true/false).</summary>
        [JsonIgnore]
        public bool WillRunoff { get; private set; }

        /// <summary>Occurs when [irrigated].</summary>
        /// <remarks>
        /// Advertises an irrigation and passes its parameters, thus allowing other models to respond accordingly.
        /// </remarks>
        public event EventHandler<IrrigationApplicationType> Irrigated;

        /// <summary>Apply some irrigation.</summary>
        /// <param name="amount">The amount to apply (mm).</param>
        /// <param name="depth">The depth of application (mm).</param>
        /// <param name="duration">The duration of the irrigation event (minutes).</param>
        /// <param name="efficiency">The irrigation efficiency (mm/mm).</param>
        /// <param name="willRunoff">Whether irrigation can run off (<c>true</c>/<c>false</c>).</param>
        /// <param name="no3">Amount of NO3 in irrigation water</param>
        /// <param name="nh4">Amount of NH4 in irrigation water</param>
        /// <param name="doOutput">If true, output will be written to the summary.</param>
        public void Apply(double amount, double depth = 0.0, double duration = 1440.0, double efficiency = 1.0, bool willRunoff = false,
                          double no3 = 0.0, double nh4 = 0.0, bool doOutput = true)
        {
            if (Irrigated != null && amount > 0.0)
            {
                if (depth > soilPhysical.Thickness.Sum())
                    throw new ApsimXException(this, "Check the depth for irrigation, it cannot be deeper than the soil depth");
                Depth = depth;
 
                if (duration > 1440.0)
                    throw new ApsimXException(this, "Check the duration for the irrigation, it must be less than 1440 minutes");
                Duration = duration;

                if (efficiency > 1.0)
                   throw new ApsimXException(this, "Check the value of irrigation efficiency, it must be between 0 and 1");
                Efficiency = efficiency;

                // Sub-surface irrigation cannot run off
                if (Depth > 0.0)
                    willRunoff = false;

                IrrigationApplied = amount * efficiency;
                WillRunoff = willRunoff;

                // Prepare the irrigation data
                IrrigationApplicationType irrigData = new IrrigationApplicationType();
                irrigData.Amount = IrrigationApplied;
                irrigData.Depth = Depth;
                irrigData.Duration = Duration;
                irrigData.WillRunoff = WillRunoff;
                irrigData.NO3 = no3;
                irrigData.NH4 = nh4;

                // Raise event and write log
                Irrigated.Invoke(this, irrigData);
                if (doOutput)
                    summary.WriteMessage(this, string.Format("{0:F1} mm of water added via irrigation at depth {1} mm", IrrigationApplied, Depth));
            }
            else if (doOutput && amount < 0)
                summary.WriteMessage(this, "Irrigation did not occur because the amount given was negative");
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            IrrigationApplied = 0.0;
            Depth = 0.0;
            Duration = 1440.0;
            WillRunoff = false;
        }
    }
}
