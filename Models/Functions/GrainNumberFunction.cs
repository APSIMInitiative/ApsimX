using APSIM.Shared.Utilities;
using Models;
using Models.Core;
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
        /// Final grain number.
        /// </summary>
        /// 
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction finalGrainNumber = null;

        /// <summary>
        /// Number of degree days required for grain number to go from 0 (at start grain fill) to final grain number.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction rampTT = null;

        /// <summary>
        /// Link to phenology.
        /// </summary>
        [Link]
        private Phenology phenology = null;

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
        }

        /// <summary>
        /// Recalculates the grain number at the end of the day.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("EndOfDay")]
        private void UpdateValue(object sender, EventArgs args)
        {
            double finalGrainNo = finalGrainNumber.Value();
            double tempFactor = temperatureFactor.Value();
            double ttRamp = rampTT.Value();
            value = tempFactor * Math.Min(finalGrainNo, finalGrainNo * MathUtilities.Divide(accumulatedTT, ttRamp, 0));
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
