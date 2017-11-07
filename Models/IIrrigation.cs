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
        /// <summary>The amount of irrigation applied.</summary>
        double IrrigationApplied { get; }

        /// <summary>Indicates whether the irrigation should runoff.</summary>
        bool WillRunoff { get; }

        /// <summary>Indicates the depth of irrigation.</summary>
        double Depth { get; }
        /// <summary>the duration, h, that irrigation is applied for</summary>
        double Duration { get; }

        /// <summary>Invoked when an irrigation occurs.</summary>
        event EventHandler<Models.Soils.IrrigationApplicationType> Irrigated;

        /// <summary>Called to apply irrigation.</summary>
        /// <param name="amount">Amount of irrigation (mm)</param>
        /// <param name="depth">Depth of irrigation (mm)</param>
        /// <param name="efficiency">Efficiency of irrigation (%)</param>
        /// <param name="willRunoff">Irrigation will runoff if true.</param>
        /// <param name="duration">The duration that irrigation is applied for, hour</param>
        void Apply(double amount, double depth = 0, double efficiency = 1, bool willRunoff = false, double duration = 1);
    }
}
