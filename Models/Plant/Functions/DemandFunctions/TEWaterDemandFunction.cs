using System;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Water demand is calculated using the Transpiration Efficiency (TE) approach (ie TE=Coefficient/VDP).
    /// </summary>
    [Serializable]
    public class TEWaterDemandFunction : Model, IFunction
    {
        /// <summary>Average Daily Vapour Pressure Deficit as a proportion of daily Maximum.</summary>
        [Link]
        IFunction SVPFrac = null;
        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;
        /// <summary>The photosynthesis</summary>
        [Link]
        IFunction Photosynthesis = null;
        /// <summary>Transpiration Efficiency Coefficient to relate TE to daily VPD</summary>
        [Link]
        IFunction TranspirationEfficiencyCoefficient = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                double SVPmax = MetUtilities.svp(MetData.MaxT) * 0.1;
                double SVPmin = MetUtilities.svp(MetData.MinT) * 0.1;
                double VPD = Math.Max(SVPFrac.Value * (SVPmax - SVPmin), 0.01);

                return Photosynthesis.Value / (TranspirationEfficiencyCoefficient.Value / VPD / 0.001);

            }
        }

    }
}


