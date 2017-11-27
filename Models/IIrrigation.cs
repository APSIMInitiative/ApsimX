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
        /// <summary>The amount of irrigation actually applied (mm).</summary>
        double IrrigationApplied { get; }

        /// <summary>The depth at which irrigation is applied (mm).</summary>
        double Depth { get; }

        /// <summary>The time, since midnight, to start irrigation (minutes).</summary>
        double StartTime { get; }

        /// <summary>The duration of the irrigation event (minutes).</summary>
        double Duration { get; }

        /// <summary>The efficiency of the irrigation system (mm/mm).</summary>
        double Efficiency { get; }

        /// <summary>The flag for whether the irrigation can be intercepted by canopy (true/false).</summary>
        bool WillIntercept { get; }

        /// <summary>The flag for whether the irrigation can run off (true/false).</summary>
        bool WillRunoff { get; }

        /// <summary>Invoked when an irrigation occurs.</summary>
        event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Called to apply some irrigation.</summary>
        /// <param name="amount">The amount to apply (mm).</param>
        /// <param name="depth">The depth of application (mm).</param>
        /// <param name="startTime">The time to start the irrigation (minutes).</param>
        /// <param name="duration">The duration of irrigation event (minutes).</param>
        /// <param name="efficiency">The irrigation efficiency (mm/mm).</param>
        /// <param name="willIntercept">Whether irrigation can be intercepted by canopy (<c>true</c>/<c>false</c>).</param>
        /// <param name="willRunoff">Whether irrigation can run off (<c>true</c>/<c>false</c>).</param>
        void Apply(double amount, double depth = 0.0, double startTime = 0.0, double duration = 1.0, double efficiency = 1.0, bool willIntercept = false, bool willRunoff = false);
    }
}
