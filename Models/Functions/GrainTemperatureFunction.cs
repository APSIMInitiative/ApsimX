using APSIM.Shared.Utilities;
using System;
using Models;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class GrainTemperatureFunction : Model, IFunction
    {
        /// <summary>
        /// Heat severity response.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction heatSeverity = null; // .Simulations.Replacements.Sorghum.Grain.TemperatureFactor.TempFactorCalc.if heatSeverity

        /// <summary>
        /// Total thermal time between flag leaf and flowering.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction ttFlagToFlowering = null; // .Simulations.Replacements.Sorghum.Grain.TemperatureFactor.TempFactorCalc.else.if eTT

        /// <summary>
        /// Grain temperature window 0.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)] // = -50
        private IFunction grainTempWindow0 = null; // .Simulations.Replacements.Sorghum.Grain.TemperatureFactor.TempFactorCalc.else.is less than target TT.GrainTempWindow0

        /// <summary>
        /// Grain temperature window 0.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)] // = 100
        private IFunction grainTempWindow1 = null; // .Simulations.Replacements.Sorghum.Grain.GrainTempWindow1

        /// <summary>
        /// Accumulated TT between flag leaf and flowering.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction eTT = null; // .Simulations.Replacements.Sorghum.Grain.TemperatureFactor.TempFactorCalc.else.if eTT

        /// <summary>
        /// Accumulated TT post-anthesis.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction eTTPostAnthesis = null; // .Simulations.Replacements.Sorghum.Grain.TemperatureFactor.TempFactorCalc.else.else.SubtractFunction.eTTPostAnthesis

        /// <summary>
        /// Today's thermaltime.
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction dltTT = null;

        /// <summary>
        /// Returns the daily change in heat temperature.
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public double Value(int arrayIndex = -1)
        {
            // calculate a daily contribution to stress on grain number
            // if we are within the grain stress window (grainTempWindow)calculate stress factor
            // from grainTempTable and this day's contribution to the total stress

            // Calculate heat severity.
            double heatStress = heatSeverity.Value();
            // First see if it is a hot day.
            if(heatStress < 0.001)
                return 0.0;

            // then see if we are in the pre-anthesis or post-anthesis window 
            // if not return 0                                      (grainTempWindow[0] is -ve)
            double tempWindow0 = grainTempWindow0.Value();
            double targetTT = ttFlagToFlowering.Value() + tempWindow0;
            double ttFlagToFlower = eTT.Value();
            if (ttFlagToFlower < targetTT)
                return 0.0;

            // Check if in the post flag leaf window.
            double tempWindow1 = grainTempWindow1.Value();
            double ttPostAnthesis = eTTPostAnthesis.Value();
            if (ttPostAnthesis > tempWindow1)
                return 0.0;

            double ttToday = dltTT.Value();
            double ttContrib;

            // Check window
            if(ttPostAnthesis > 0.0)
                // Post-anthesis
                ttContrib = Math.Min(tempWindow1 - ttPostAnthesis, ttToday);
            else
                // Pre-flag leaf
                ttContrib = Math.Min(ttFlagToFlower - targetTT, ttToday);

            double dayFract = MathUtilities.Divide(ttContrib, -1.0 * tempWindow0 + tempWindow1, 0);
            return dayFract * heatStress;
        }
    }
}
