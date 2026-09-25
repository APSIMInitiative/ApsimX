using Models.Core;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Values calculated by DCaPST for one sub-daily interval.
    /// </summary>
    public class DCaPSTIntervalOutput : EventArgs
    {
        /// <summary>
        /// Creates an empty interval output. This supports discovery of nested
        /// properties by the report variable locator.
        /// </summary>
        public DCaPSTIntervalOutput()
        {
        }

        /// <summary>Creates an output from a calculated DCaPST interval.</summary>
        /// <param name="date">Date on which the interval was calculated.</param>
        /// <param name="interval">Calculated interval values.</param>
        internal DCaPSTIntervalOutput(DateTime date, IntervalValues interval)
        {
            Hour = interval.Time;
            IntervalDateTime = date.Date.AddHours(Hour);
            AirTemperature = interval.AirTemperature;
            SunlitLAI = interval.SunlitLAI;
            ShadedLAI = interval.ShadedLAI;

            SunlitAssimilation = interval.Sunlit.A;
            SunlitWater = interval.Sunlit.Water;
            SunlitTemperature = interval.Sunlit.Temperature;
            SunlitVPD = interval.Sunlit.VPD;
            SunlitAc1 = interval.Sunlit.Ac1.Assimilation;
            SunlitAc2 = interval.Sunlit.Ac2.Assimilation;
            SunlitAj = interval.Sunlit.Aj.Assimilation;

            ShadedAssimilation = interval.Shaded.A;
            ShadedWater = interval.Shaded.Water;
            ShadedTemperature = interval.Shaded.Temperature;
            ShadedVPD = interval.Shaded.VPD;
            ShadedAc1 = interval.Shaded.Ac1.Assimilation;
            ShadedAc2 = interval.Shaded.Ac2.Assimilation;
            ShadedAj = interval.Shaded.Aj.Assimilation;

            double totalLAI = SunlitLAI + ShadedLAI;
            CanopyTemperature = LAIWeightedMean(SunlitTemperature, SunlitLAI, ShadedTemperature, ShadedLAI, totalLAI);
            CanopyVPD = LAIWeightedMean(SunlitVPD, SunlitLAI, ShadedVPD, ShadedLAI, totalLAI);
        }

        /// <summary>Date and time of the interval.</summary>
        public DateTime IntervalDateTime { get; private set; }

        /// <summary>Hour of the interval.</summary>
        [Units("hours")]
        public double Hour { get; private set; }

        /// <summary>Air temperature during the interval.</summary>
        [Units("°C")]
        public double AirTemperature { get; private set; }

        /// <summary>Leaf area index of the sunlit canopy.</summary>
        [Units("m^2/m^2")]
        public double SunlitLAI { get; private set; }

        /// <summary>Leaf area index of the shaded canopy.</summary>
        [Units("m^2/m^2")]
        public double ShadedLAI { get; private set; }

        /// <summary>LAI-weighted canopy temperature.</summary>
        [Units("°C")]
        public double CanopyTemperature { get; private set; }

        /// <summary>LAI-weighted canopy vapour pressure deficit.</summary>
        [Units("kPa")]
        public double CanopyVPD { get; private set; }

        /// <summary>Sunlit canopy assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAssimilation { get; private set; }

        /// <summary>Sunlit canopy water use.</summary>
        [Units("mm")]
        public double SunlitWater { get; private set; }

        /// <summary>Sunlit canopy temperature.</summary>
        [Units("°C")]
        public double SunlitTemperature { get; private set; }

        /// <summary>Sunlit canopy vapour pressure deficit.</summary>
        [Units("kPa")]
        public double SunlitVPD { get; private set; }

        /// <summary>Sunlit AC1 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAc1 { get; private set; }

        /// <summary>Sunlit AC2 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAc2 { get; private set; }

        /// <summary>Sunlit AJ pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double SunlitAj { get; private set; }

        /// <summary>Shaded canopy assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAssimilation { get; private set; }

        /// <summary>Shaded canopy water use.</summary>
        [Units("mm")]
        public double ShadedWater { get; private set; }

        /// <summary>Shaded canopy temperature.</summary>
        [Units("°C")]
        public double ShadedTemperature { get; private set; }

        /// <summary>Shaded canopy vapour pressure deficit.</summary>
        [Units("kPa")]
        public double ShadedVPD { get; private set; }

        /// <summary>Shaded AC1 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAc1 { get; private set; }

        /// <summary>Shaded AC2 pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAc2 { get; private set; }

        /// <summary>Shaded AJ pathway assimilation.</summary>
        [Units("umol CO2/m^2/s")]
        public double ShadedAj { get; private set; }

        /// <summary>Calculates a canopy mean weighted by sunlit and shaded leaf area.</summary>
        private static double LAIWeightedMean(double sunlitValue, double sunlitLAI, double shadedValue, double shadedLAI, double totalLAI)
        {
            if (totalLAI <= 0)
                return 0;

            return (sunlitValue * sunlitLAI + shadedValue * shadedLAI) / totalLAI;
        }
    }
}
