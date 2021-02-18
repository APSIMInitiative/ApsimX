using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.PMF;
using Models.PMF.Organs;
using Models.PMF.Phen;
using System;

namespace Models.Functions
{
    /// <summary>
    /// Wraps the grain number function calcs for sorghum.
    /// </summary>
    [Serializable]
    [ValidParent(typeof(ReproductiveOrgan))]
    public class GrainNumberFunction : Model, IFunction
    {
        /// <summary>
        /// Temperature factor.
        /// </summary>
        [Units("0-1")]
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction temperatureFactor = null;

        /// <summary>
        /// Number of degree days required for grain number to go from 0 (at start grain fill) to final grain number.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("degree days")]
        private IFunction rampTT = null;

        /// <summary>
        /// DM per seed.
        /// </summary>
        /// <remarks>todo: check units</remarks>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("?")]
        private IFunction dmPerSeed = null;

        /// <summary>
        /// Link to phenology.
        /// </summary>
        [Link]
        private Phenology phenology = null;

        /// <summary>
        /// Total biomass.
        /// </summary>
        [Link(ByName = true)]
        private Biomass total = null;

        /// <summary>
        /// Total accumulated thermal time from start grain fill to maturity.
        /// </summary>
        [NonSerialized]
        private double accumulatedTT = 0;

        /// <summary>
        /// The grain number, which gets updated at the end of the day, for consistency with old apsim.
        /// </summary>
        [NonSerialized]
        private double value = 0;

        /// <summary>
        /// Final grain number.
        /// </summary>
        [NonSerialized]
        private double finalGrainNumber = 0;

        /// <summary>
        /// Number of days between floral initiation and start grain fill.
        /// </summary>
        [NonSerialized]
        private int daysFIToStartGrainFill = 0;

        /// <summary>
        /// Total live wt of the plant at floral initiation.
        /// </summary>
        [NonSerialized]
        private double greenWtAtFloralInit = 0;

        /// <summary>
        /// Get the grain number.
        /// </summary>
        /// <param name="arrayIndex">Array index (irrelevant for this function).</param>
        public double Value(int arrayIndex = -1) => value;

        /// <summary>
        /// Reset the state variables.
        /// </summary>
        public void Clear()
        {
            accumulatedTT = 0;
            value = 0;
            finalGrainNumber = 0;
            daysFIToStartGrainFill = 0;
            greenWtAtFloralInit = 0;
        }

        /// <summary>
        /// Calculate final grain number. Is called at start grain fill.
        /// </summary>
        /// <returns></returns>
        private double CalculateFinalGrainNumber()
        {
            double plantGrowth = Math.Max(0, total.Wt - greenWtAtFloralInit);
            double growthRate = MathUtilities.Divide(plantGrowth, daysFIToStartGrainFill, 0);
            return MathUtilities.Divide(growthRate, dmPerSeed.Value(), 0);
        }

        /// <summary>
        /// Recalculates the grain number at the end of the day.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("EndOfDay")]
        private void UpdateValue(object sender, EventArgs args)
        {
            double tempFactor = temperatureFactor.Value();
            double ttRamp = rampTT.Value();
            value = tempFactor * Math.Min(finalGrainNumber, finalGrainNumber * MathUtilities.Divide(accumulatedTT, ttRamp, 0));
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (phenology.Between("StartGrainFill", "Maturity"))
                if (phenology.CurrentPhase is GenericPhase phase)
                    accumulatedTT += phase.ProgressionForTimeStep;

            if (phenology.Between("FloralInitiation", "StartGrainFill"))
                daysFIToStartGrainFill++;
        }
    
        /// <summary>Called when phenological phase changes. Updates grain number if the phase is start grainfill.</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == "StartGrainFill")
                finalGrainNumber = CalculateFinalGrainNumber();
            if (phaseChange.StageName == "FloralInitiation")
                greenWtAtFloralInit = total.Wt;
        }

        /// <summary>Called when the simulation starts.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>
        /// Called when the plant is ended. Resets the accumulated TT.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [EventSubscribe("PlantEnding")]
        private void OnHarvest(object sender, EventArgs args)
        {
            Clear();
        }
    }
}
