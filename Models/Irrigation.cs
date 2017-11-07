// -----------------------------------------------------------------------
// <copyright file="Irrigation.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Models
{
    using System;
    using System.Linq;
    using System.Xml.Serialization;
    using Models.Core;

    /// <summary>
    /// This model controls the irrigation events, which can be triggered using the Apply() method.
    /// </summary>
    /// <remarks>
    /// The Apply method can be invoked by external models or manager scripts, it has one mandatory parameter and several optionals:
    /// - amount: The amount to apply (mm) (M);
    /// - depth: The depth of application (mm) (O);
    /// - startTime: The time to start the irrigation (hrs) (O);
    /// - duration: The duration of irrigation event (hrs) (O);
    /// - efficiency: The irrigation efficiency (mm/mm) (O);
    /// - willIntercept: Whether irrigation can be intercepted by canopy (true/false) (O);
    /// - willRunoff: Whether irrigation can run off (true/false) (O);
    /// 
    /// The actual amount of irrigation applied is computed by multiplying the 'amount' × 'efficiency'.
    /// The Irrigated event is raised to advertise an irrigation, with it goes the actual amount applied and other
    ///  parameters.
    /// Solutes in irrigated water have not been not implemented yet.
    /// </remarks>
    [Serializable]
    [ValidParent(ParentType = typeof(Zone))]
    public class Irrigation : Model, IIrrigation
    {
        /// <summary>Access the summary model.</summary>
        [Link] private ISummary summary = null;

        /// <summary>Access the soil model.</summary>
        [Link] private Soils.Soil soil = null;

        /// <summary>Gets the total irrigation resource used (mm).</summary>
        [XmlIgnore]
        public double IrrigationTotal { get; private set; }

        /// <summary>Gets the amount of irrigation applied (mm).</summary>
        [XmlIgnore]
        public double IrrigationApplied { get; private set; }

        /// <summary>Gets or sets the depth at which irrigation is applied (mm).</summary>
        [XmlIgnore]
        public double Depth { get; set; }

        /// <summary>Gets or sets the time, from midnight, to start irrigation (hrs).</summary>
        [XmlIgnore]
        public double StartTime { get; set; }

        /// <summary>Gets or sets the duration that irrigation is applied for (hrs).</summary>
        [XmlIgnore]
        public double Duration { get; set; }

        /// <summary>Gets or sets the efficiency of the irrigation system (mm/mm).</summary>
        [XmlIgnore]
        public double Efficiency { get; set; }

        /// <summary>Gets or sets the flag for whether the irrigation can be intercepted by canopy.</summary>
        [XmlIgnore]
        public bool WillIntercept { get; set; }

        /// <summary>Gets or sets the flag for whether the irrigation can run off (true/false).</summary>
        [XmlIgnore]
        public bool WillRunoff { get; set; }

        /// <summary>Occurs when [irrigated].</summary>
        /// <remarks>Advertises an irrigation and allows other models to respond accordingly.</remarks>
        public event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Default value for depth (mm).</summary>
        private double defaultDepth = 0.0;

        /// <summary>Default value for start time of irrigation (hrs).</summary>
        private double defaultStartTime = 0.0;
        
        /// <summary>Default value for duration of irrigation (hrs).</summary>
        private double defaultDuration = 24.0;

        /// <summary>Default value for irrigation efficiency (mm/mm).</summary>
        private double defaultEfficiency = 1.0;

        /// <summary>Apply some irrigation.</summary>
        /// <param name="amount">The amount to apply (mm).</param>
        /// <param name="depth">The depth of application (mm).</param>
        /// <param name="startTime">The time to start the irrigation (hrs).</param>
        /// <param name="duration">The duration of irrigation event (hrs).</param>
        /// <param name="efficiency">The irrigation efficiency (mm/mm).</param>
        /// <param name="willIntercept">Whether irrigation can be intercepted by canopy (<c>true</c>/<c>false</c>).</param>
        /// <param name="willRunoff">Whether irrigation can run off (<c>true</c>/<c>false</c>).</param>
        /// <exception cref="ApsimXException">Check the depth for irrigation, it cannot be deeper than the soil depth</exception>
        /// <exception cref="ApsimXException">Check the duration for the irrigation, it must be less than 24hrs</exception>
        /// <exception cref="ApsimXException">Check the start time for irrigation, it must be less than 24hrs</exception>
        /// <exception cref="ApsimXException">Check the start time and duration of irrigation, the sum must be smaller than 24hrs</exception>
        /// <exception cref="ApsimXException">Check the value of irrigation efficiency, it must be between 0 and 1</exception>
        public void Apply(double amount, double depth = -1.0, double startTime = -1.0, double duration = -1.0, double efficiency = -1.0, bool willIntercept = false, bool willRunoff = false)
        {
            if (Irrigated != null && amount > 0.0)
            {
                // Check the parameters given
                if (depth < 0.0)
                { Depth = defaultDepth; }
                else
                {
                    if (depth > soil.Thickness.Sum())
                        throw new ApsimXException(this, "Check the depth for irrigation, it cannot be deeper than the soil depth");
                    Depth = depth;
                }

                if (startTime < 0.0)
                { StartTime = defaultStartTime; }
                else
                {
                    if (startTime >= 24.0)
                        throw new ApsimXException(this, "Check the start time for irrigation, it must be less than 24hrs");
                    StartTime = startTime;
                }

                if (duration < 0.0)
                { Duration = defaultDuration; }
                else
                {
                    if (duration > 24.0)
                        throw new ApsimXException(this, "Check the duration for the irrigation, it must be less than 24hrs");
                    Duration = duration;
                }

                if (StartTime + Duration > 24.0)
                    throw new ApsimXException(this, "Check the start time and duration of irrigation, the sum must be smaller than 24hrs");

                if (efficiency < 0.0)
                { Efficiency = defaultEfficiency; }
                else
                {
                    if (efficiency > 1.0)
                        throw new ApsimXException(this, "Check the value of irrigation efficiency, it must be between 0 and 1");
                    Efficiency = efficiency;
                }

                if (Depth > 0.0)
                { // Subsurface irrigation, cannot have interception nor direct runoff
                    willIntercept = false;
                    willRunoff = false;
                }

                IrrigationTotal = amount;
                IrrigationApplied = IrrigationTotal * Efficiency;
                WillRunoff = willRunoff;
                WillIntercept = willIntercept;

                // Prepare the irrigation data
                Models.Soils.IrrigationApplicationType irrigData = new Models.Soils.IrrigationApplicationType();
                irrigData.Amount = IrrigationApplied;
                irrigData.Depth = Depth;
                irrigData.StartTime = StartTime;
                irrigData.Duration = Duration;
                irrigData.WillIntercept = WillIntercept;
                irrigData.WillRunoff = WillRunoff;

                // Raise event and write log
                Irrigated.Invoke(this, irrigData);
                summary.WriteMessage(this, string.Format("{0:F1} mm of water added via irrigation at depth {1} mm", IrrigationApplied, Depth));
            }
            else
            {
                // write log of aborted event
                summary.WriteMessage(this, "Irrigation did not occur because the amount given was negative");
            }
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // Set values to zero or defaults
            IrrigationTotal = 0.0;
            IrrigationApplied = 0.0;
            Depth = defaultDepth;
            StartTime = defaultStartTime;
            Duration = defaultDuration;
            Efficiency = defaultEfficiency;
            WillIntercept = false;
            WillRunoff = false;
        }
    }
}
