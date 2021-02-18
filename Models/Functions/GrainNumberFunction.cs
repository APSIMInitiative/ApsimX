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
        /// Heat severity response.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction heatSeverity = null;

        /// <summary>
        /// Daily thermaltime delta.
        /// </summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].DltTT")]
        private IFunction dltTT = null;

        /// <summary>
        /// Flag leaf to flowering phenological phase.
        /// </summary>
        /// <remarks>
        /// We need this because we need to know the TT target for this phase.
        /// Could potentially refactor this out.
        /// </remarks>
        [Link(ByName = true)]
        private GenericPhase flagLeafToFlowering = null;

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
        private double ttGrainFillToMaturity = 0;

        /// <summary>
        /// Total accumulated thermaltime between flowering and maturity.
        /// </summary>
        [NonSerialized]
        private double ttFloweringToMaturity = 0;

        /// <summary>
        /// Total accumulated thermal time between flag leaf and flowering.
        /// </summary>
        [NonSerialized]
        private double ttFlagToFlowering = 0;

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
        /// Effect of temperature on grain number.
        /// </summary>
        [NonSerialized]
        private double temperatureFactor = 0;

        /// <summary>
        /// grainTempWindow[0] in old apsim.
        /// </summary>
        [NonSerialized]
        private const double tempWindow0 = -50;
        
        /// <summary>
        /// grainTempWindow[1] in old apsim.
        /// </summary>
        [NonSerialized]
        private const double tempWindow1 = 100;

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
            ttGrainFillToMaturity = 0;
            ttFloweringToMaturity = 0;
            ttFlagToFlowering = 0;
            value = 0;
            finalGrainNumber = 0;
            daysFIToStartGrainFill = 0;
            greenWtAtFloralInit = 0;
            temperatureFactor = 1;
        }

        /// <summary>
        /// Calculate final grain number. Is called at start grain fill.
        /// </summary>
        private double CalculateFinalGrainNumber()
        {
            double plantGrowth = Math.Max(0, total.Wt - greenWtAtFloralInit);
            double growthRate = MathUtilities.Divide(plantGrowth, daysFIToStartGrainFill, 0);
            return MathUtilities.Divide(growthRate, dmPerSeed.Value(), 0);
        }

        /// <summary>
        /// Calculate the daily temperature factor delta. Is called once per day, after flag leaf.
        /// </summary>
        private double CalculateTemperatureFactorDelta()
        {
            // calculate a daily contribution to stress on grain number
            // if we are within the grain stress window (grainTempWindow)calculate stress factor
            // from grainTempTable and this day's contribution to the total stress

            // Calculate heat severity.
            double heatStress = heatSeverity.Value();
            // First see if it is a hot day.
            if(heatStress < 0.001)
                return 0.0;

            // Check if we are in the pre-anthesis or post-anthesis window. If not return 0.
            // Note: tempWindow0 is negative.
            // todo: Could we change this to use one of the modern nextgen methods in phenology class?
            double targetTT = flagLeafToFlowering.Target + tempWindow0;
            if (ttFlagToFlowering < targetTT)
                return 0.0;

            // Check if in the post flag leaf window.
            if (ttFloweringToMaturity > tempWindow1)
                return 0.0;

            double ttToday = dltTT.Value();
            double ttContrib;

            // Check window
            if (ttFloweringToMaturity > 0.0)
                // Post-anthesis
                ttContrib = Math.Min(tempWindow1 - ttFloweringToMaturity, ttToday);
            else
                // Pre-flag leaf
                ttContrib = Math.Min(ttFlagToFlowering - targetTT, ttToday);

            double dayFract = MathUtilities.Divide(ttContrib, -1.0 * tempWindow0 + tempWindow1, 0);
            return dayFract * heatStress;
        }

        /// <summary>
        /// Recalculates the grain number at the end of the day.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("EndOfDay")]
        private void UpdateValue(object sender, EventArgs args)
        {
            value = temperatureFactor * Math.Min(finalGrainNumber, finalGrainNumber * MathUtilities.Divide(ttGrainFillToMaturity, rampTT.Value(), 0));
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (phenology.Between("StartGrainFill", "Maturity"))
                if (phenology.CurrentPhase is GenericPhase phase)
                    ttGrainFillToMaturity += phase.ProgressionForTimeStep;

            if (phenology.Between("FlagLeaf", "Flowering"))
                if (phenology.CurrentPhase is GenericPhase genPhase)
                    ttFlagToFlowering += genPhase.ProgressionForTimeStep;

            if (phenology.Between("FloralInitiation", "StartGrainFill"))
                daysFIToStartGrainFill++;

            if (phenology.Between("Flowering", "Maturity"))
                if (phenology.CurrentPhase is GenericPhase genericPhase)
                    ttFloweringToMaturity += dltTT.Value();
        
            // Note: temperature factor calculations need to be BELOW the previous accumulator
            // code, as it assumes that these TT values already include today's deltas.
            if (phenology.Between("FlagLeaf", "Maturity"))
            {
                temperatureFactor -= CalculateTemperatureFactorDelta();
                temperatureFactor = Math.Max(temperatureFactor, 0);
            }
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
