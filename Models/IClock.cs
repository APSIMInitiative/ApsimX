using System;

namespace Models
{

    /// <summary>Interface for a time server,</summary>
    public interface IClock
    {
        /// <summary>Simulation date.</summary>
        DateTime Today { get; }

        /// <summary>Returns the current fraction of the overall simulation which has been completed</summary>
        double FractionComplete { get; }

        /// <summary>Simulation start date.</summary>
        DateTime StartDate { get; set; }

        /// <summary>Simulation end date.</summary>
        DateTime EndDate { get; set; }

    }
}