using Models.Core;
using System;

namespace Models.PMF.Phen
{

    /// <summary>Interface for a function</summary>
    public interface IParallelPhase : IModel
    {
        /// <summary> Fraction of progress through the phase</summary>
        double FractionComplete { get; }

        /// <summary>Is the parallel phenology currently in this phase</summary>
        bool IsInPhase { get; }

        /// <summary> The stage in the main phenology sequence that this parallel phase started at</summary>
        double StartStage { get; }
    }
}