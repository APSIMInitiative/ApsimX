using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Interfaces;

namespace Models.PMF.Functions
{
    /// <summary>
    /// An air temperature function
    /// </summary>
    [Serializable]
    [Description("A value is calculated from the mean of 3-hourly estimates of air temperature calculated from daily max and min temperatures")]
    public class AirTemperatureFunction : Model, IFunction
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Gets or sets the xy pairs.</summary>
        public XYPairs XYPairs { get; set; }   // Temperature effect on Growth Interpolation Set

        /// <summary>Number of 3 hourly temperatures</summary>
        private const int num3hr = 24 / 3;           // 

        /// <summary>Fraction_of of day's range_of for this 3 hr period</summary>
        private double[] t_range_fract = null;

        /// <summary>Initializes a new instance of the <see cref="AirTemperatureFunction"/> class.</summary>
        public AirTemperatureFunction()
        {
            t_range_fract = new double[num3hr];

            // pre calculate t_range_fract for speed reasons
            for (int period = 1; period <= num3hr; period++)
            {
                double period_no = period;
                t_range_fract[period-1] = 0.92105
                                    + 0.1140 * period_no
                                    - 0.0703 * Math.Pow(period_no, 2)
                                    + 0.0053 * Math.Pow(period_no, 3);
            }
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("deg.day")]
        public double Value
        {
            get
            {
                return Linint3hrlyTemp(MetData.MaxT, MetData.MinT, XYPairs);
            }
        }
        /// <summary>Linint3hrlies the temporary.</summary>
        /// <param name="tmax">The tmax.</param>
        /// <param name="tmin">The tmin.</param>
        /// <param name="ttFn">The tt function.</param>
        /// <returns></returns>
        public double Linint3hrlyTemp(double tmax, double tmin, XYPairs ttFn)
        {
            // --------------------------------------------------------------------------
            // Eight interpolations of the air temperature are
            // calculated using a three-hour correction factor.
            // For each air three-hour air temperature, a value
            // is calculated.  The eight three-hour estimates
            // are then averaged to obtain the daily value.
            // --------------------------------------------------------------------------

            // Local Variables
            double tot = 0.0;            // sum_of of 3 hr interpolations

            for (int period = 1; period <= num3hr; period++)
            {
                // get mean temperature for 3 hr period (oC)
                double tmean_3hour = temp_3hr(tmax, tmin, period);
                tot = tot + ttFn.ValueIndexed(tmean_3hour);
            }
            return tot / (double)num3hr;
        }

        /// <summary>Temp_3hrs the specified tmax.</summary>
        /// <param name="tmax">The tmax.</param>
        /// <param name="tmin">The tmin.</param>
        /// <param name="period">The period.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">
        /// 3 hr. period number is below 1
        /// or
        /// 3 hr. period number is above 8
        /// </exception>
        private double temp_3hr(double tmax, double tmin, int period)
        {
            // --------------------------------------------------------------------------
            //   returns the temperature for a 3 hour period.
            //   a 3 hourly estimate of air temperature
            // --------------------------------------------------------------------------

            if (period < 1)
                throw new Exception("3 hr. period number is below 1");
            else if (period > 8)
                throw new Exception("3 hr. period number is above 8");

            double period_no = period;

            // diurnal temperature range for the day (oC)
            double diurnal_range = tmax - tmin;

            // deviation from day's minimum for this 3 hr period
            double t_deviation = t_range_fract[period-1] * diurnal_range;

            return tmin + t_deviation;
        }

    }

}