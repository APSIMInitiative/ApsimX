using System;
using APSIM.Core;
using Models.Core;
using Models.Interfaces;
using Models.Functions;

namespace Models
{
    /// <summary>
    /// Exposes the Penman–Monteith vapour-pressure-deficit
    /// that MicroClimate used internally when it calls CalcSpecificVPD().
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(MicroClimate))]
    public class VPDCalculator : Model, IFunction
    {
        [Link] private IWeather weather = null!;

        // These are the same from MicroClimateZone (Teten constants + weight)
        private const double svpA = 6.106;
        private const double svpB = 17.27;
        private const double svpC = 237.3;
        private const double svpFract = 0.66;

        /// <summary>
        /// The standard IFunction.Value() entry point.
        /// </summary>
        public double Value(int arrayIndex = -1)
        {
            // exactly MicroClimateZone.CalcSpecificVPD()
            double vp = weather.VP;
            double mint = weather.MinT;
            double maxt = weather.MaxT;

            // Teten SVP
            double svp(double T) => svpA * Math.Exp(svpB * T / (T + svpC));

            // layer-weighted VPD
            double vpdMin = Math.Max(0.0, svp(mint) - vp);
            double vpdMax = Math.Max(0.0, svp(maxt) - vp);
            return svpFract * vpdMax + (1.0 - svpFract) * vpdMin;
        }
    }
}
