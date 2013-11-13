using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Uses a sinusoidal function with Max and Min Temp to calculate 3 hourly temperature, useses these with temperatue profile specified in xy pairs to calculate 3 hourly thermal time and returns the mean of these 8 values")]
    public class AirTemperatureFunction : Function
    {
        #region Class Data Members
        public XYPairs XYPairs { get; set; }   // Temperature effect on Growth Interpolation Set

        #endregion

        
        [Units("deg.day")]
        public override double FunctionValue
        {

            get
            {
                return Linint3hrlyTemp(MetData.MaxT, MetData.MinT, XYPairs);
            }
        }
        static public double Linint3hrlyTemp(double tmax, double tmin, XYPairs ttFn)
        {
            // --------------------------------------------------------------------------
            // Eight interpolations of the air temperature are
            // calculated using a three-hour correction factor.
            // For each air three-hour air temperature, a value
            // is calculated.  The eight three-hour estimates
            // are then averaged to obtain the daily value.
            // --------------------------------------------------------------------------

            //Constants
            int num3hr = 24 / 3;           // number of 3 hourly temperatures

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

        static private double temp_3hr(double tmax, double tmin, int period)
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

            // fraction_of of day's range_of for this 3 hr period
            double t_range_fract = 0.92105
                                 + 0.1140 * period_no
                                 - 0.0703 * Math.Pow(period_no, 2)
                                 + 0.0053 * Math.Pow(period_no, 3);

            // diurnal temperature range for the day (oC)
            double diurnal_range = tmax - tmin;

            // deviation from day's minimum for this 3 hr period
            double t_deviation = t_range_fract * diurnal_range;

            return tmin + t_deviation;
        }

    }

}