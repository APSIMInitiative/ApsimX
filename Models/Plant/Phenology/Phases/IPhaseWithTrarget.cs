// -----------------------------------------------------------------------
// <copyright file="IFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------

namespace Models.PMF.Phen
{
    using System.IO;
    
    /// <summary>Interface for a function</summary>
    public interface IPhaseWithTarget : IPhase
    {
        /// <summary> ThermalTimeTarget</summary>
        double Target { get; }
    }
}