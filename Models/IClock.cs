namespace Models
{
    using System;

    /// <summary>Interface for a time server,</summary>
    public interface IClock
    {
        /// <summary>Simulation date.</summary>
        DateTime Today { get; }

        /// <summary>Returns the current fraction of the overall simulation which has been completed</summary>
        double FractionComplete { get;  }
    }
}