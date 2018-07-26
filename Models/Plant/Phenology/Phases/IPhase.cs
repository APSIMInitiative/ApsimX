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
        /// <summary>The plases name</summary>
        string Name { get; }
        
        /// <summary>The start</summary>
        string Start { get; set; }

        /// <summary>The end</summary>
        string End { get; set; }

        /// <summary>
        /// This function increments thermal time accumulated in each phase
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        /// <param name="PropOfDayToUse"></param>
        /// <returns></returns>
        double DoTimeStep(double PropOfDayToUse);

        /// <summary> Fraction of progress through the phase  /// </summary>
        double FractionComplete { get; set; }

        /// <summary>Resets the phase.</summary>
        void ResetPhase();

        /// <summary> Write summary to file each time a phase completes  /// </summary>
        void WriteSummary(TextWriter writer);

        /// <summary> Adds the specified DLT_TT./// </summary>
        void Add(double dlt_tt);

        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        double TTinPhase { get; set; }

        /// <summary>The tt for today</summary>
        double TTForToday { get; }

    }
}