// -----------------------------------------------------------------------
// <copyright file="IFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------

namespace Models.PMF.Phen
{
    using System.IO;
    
    /// <summary>Interface for a function</summary>
    public interface IPhase
    {
        /// <summary>The start</summary>
        string Start { get; set; }

        /// <summary>The end</summary>
        string End { get; set; }
       
        /// <summary> Fraction of progress through the phase  /// </summary>
        double FractionComplete { get; set; }

        /// <summary>Resets the phase.</summary>
        void ResetPhase();

        /// <summary> Write summary to file each time a phase completes  /// </summary>
        void WriteSummary(TextWriter writer);

    }
}