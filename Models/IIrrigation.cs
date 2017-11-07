// -----------------------------------------------------------------------
// <copyright file="IIrrigation.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Models
{
    using System;

    /// <summary>Interface for an irrigation class.</summary>
    public interface IIrrigation
    {
        /// <summary>The amount of irrigation applied (mm).</summary>
        double IrrigationApplied { get; }

        /// <summary>The depth at which irrigation is applied (mm).</summary>
        double Depth { get; }

        /// <summary>The duration that irrigation is applied for (hrs).</summary>
        double Duration { get; }

        /// <summary>The flag for whether the irrigation can run off (true/false).</summary>
        bool WillRunoff { get; }

        /// <summary>Invoked when an irrigation occurs.</summary>
        event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Called to apply some irrigation.</summary>
        /// <param name="amount">The amount to apply (mm).</param>
        /// <param name="depth">The depth of application (mm).</param>
        /// <param name="duration">The duration of irrigation event (hrs)</param>
        /// <param name="efficiency">The irrigation efficiency (mm/mm).</param>
        /// <param name="willRunoff">Whether irrigation can run off (<c>true</c>/<c>false</c>).</param>
        void Apply(double amount, double depth = 0, double duration = 1, double efficiency = 1, bool willRunoff = false);
    }
}
