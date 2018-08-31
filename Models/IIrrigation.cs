namespace Models
{
    using System;

    /// <summary>Interface for an irrigation class.</summary>
    public interface IIrrigation
    {
        /// <summary>The amount of irrigation actually applied (mm).</summary>
        double IrrigationApplied { get; }

        /// <summary>The depth at which irrigation is applied (mm).</summary>
        double Depth { get; }

        /// <summary>The efficiency of the irrigation system (mm/mm).</summary>
        double Efficiency { get; }

        /// <summary>The duration of the irrigation event (minutes).</summary>
        double Duration { get; }

        /// <summary>The flag for whether the irrigation can run off (true/false).</summary>
        bool WillRunoff { get; }

        /// <summary>Invoked when an irrigation occurs.</summary>
        event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Called to apply some irrigation.</summary>
        /// <param name="amount">The amount to apply (mm).</param>
        /// <param name="depth">The depth of application (mm).</param>
        /// <param name="duration">The duration of irrigation event (minutes).</param>
        /// <param name="efficiency">The irrigation efficiency (mm/mm).</param>
        /// <param name="willRunoff">Whether irrigation can run off (<c>true</c>/<c>false</c>).</param>
        /// <param name="no3">Amount of NO3 in irrigation water</param>
        /// <param name="nh4">Amount of NH4 in irrigation water</param>
        /// <param name="doOutput">If true, output will be written to the summary.</param>
        void Apply(double amount, double depth = 0.0, double duration = 1.0, double efficiency = 1.0, bool willRunoff = false,
                   double no3 = -1.0, double nh4 = -1.0, bool doOutput = true);
    }
}
