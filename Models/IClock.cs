// -----------------------------------------------------------------------
// <copyright file="IClock.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models
{
    using System;

    /// <summary>Interface for a time server,</summary>
    public interface IClock
    {
        /// <summary>Simulation date.</summary>
        DateTime Today { get; }
    }
}