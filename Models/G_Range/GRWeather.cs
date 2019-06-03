using System;

namespace Models
{
    using Models.Core;
    using Models.Interfaces;

    public partial class G_Range : Model, IPlant, ICanopy, IUptake
    {
        [Serializable]
        private class Accumulator
        {
            /// <summary>
            /// Stores total number of observations
            /// </summary>
            public uint nDays { get; private set; }

            /// <summary>
            /// Stores total precipitation
            /// </summary>
            private double precipTotal;

            /// <summary>
            /// Stores sum of maximum temperatures
            /// </summary>
            private double maxTempTotal;

            /// <summary>
            /// Stores sum of minimum temperatures
            /// </summary>
            private double minTempTotal;

            /// <summary>
            /// Returns total precipitation in mm
            /// </summary>
            public double precip
            {
                get
                {
                    if (nDays > 0)
                        return precipTotal / 10.0;
                    else
                        return Double.NaN;
                }
            }

            /// <summary>
            /// Returns average maximum temperature for the period
            /// </summary>
            public double maxTemp
            {
                get
                {
                    if (nDays > 0)
                        return maxTempTotal / nDays;
                    else
                        return Double.NaN;
                }
            }

            /// <summary>
            /// Returns average minimum temperature for the period
            /// </summary>
            public double minTemp
            {
                get
                {
                    if (nDays > 0)
                        return minTempTotal / nDays;
                    else
                        return Double.NaN;
                }
            }

            /// <summary>
            /// Resets counter and totals to zero
            /// </summary>
            public void Reset()
            {
                nDays = 0;
                precipTotal = 0.0;
                maxTempTotal = 0.0;
                minTempTotal = 0.0;
            }

            /// <summary>
            /// Adds values to the accumulator. Should generally be called once per day.
            /// </summary>
            /// <param name="precip">Precipitation (including snow) in mm</param>
            /// <param name="minT">Minimum temperature in degrees C</param>
            /// <param name="maxT">Maximum temperature in degrees C</param>
            public void Accumulate(double precip, double minT, double maxT)
            {
                nDays++;
                precipTotal += precip;
                maxTempTotal += maxT;
                minTempTotal += minT;
            }
        }

        private Accumulator metAccumulator = new Accumulator();

        /// <summary>
        /// Gets weather (precipitation, and maximum and minimum temperatures) data for each day, and accumulates it 
        /// </summary>
        private void ReadWeather()
        {
            metAccumulator.Accumulate(Weather.Rain, Weather.MinT, Weather.MaxT);
        }

        private void UpdateWeather()
        {
            // Get the monthly values
            globe.precip = metAccumulator.precip / 10.0;  // G-Range uses centimeters, not millimeters!!
            globe.maxTemp = metAccumulator.maxTemp;
            globe.minTemp = metAccumulator.minTemp;
            uint monthDays = metAccumulator.nDays;
            metAccumulator.Reset();

            stormFlow = parms.prcpThresholdFraction * globe.precip;
            double tempMean = (globe.maxTemp + globe.minTemp) / 2.0;
            if (tempMean > baseTemp)
                heatAccumulation = heatAccumulation + ((tempMean - baseTemp) * monthDays);

            PEvap();
            DayLength();
            SnowDynamics();
        }

        /// <summary>
        /// Calculate Penmon-Monteith potential evapotraspiration for all the rangeland cells
        /// NOTE:  Using a subroutine here, rather than function call, given the structures used in GRange.
        /// </summary>
        private void PEvap()
        {
            double const1 = 0.0023;
            double const2 = 17.8;
            double langley2watts = 54.0;
            double fwloss_4 = 0.8;  // A variable in Century, but defaults to 0.8;

            // Calculate PET for the Julian day in the middle of the current month
            double site_latitude = Latitude; // globe.latitude;
            double temp_range = globe.maxTemp - globe.minTemp;
            double temp_mean = (globe.maxTemp + globe.minTemp) / 2.0;
            double day_pet = (const1 * (temp_mean + const2) * Math.Sqrt(temp_range) * (Shortwave() / langley2watts));
            // Calculate monthly PET and convert to cm

            double month_pet = (day_pet * 30.0 ) / 10.0;
            if (month_pet > vLarge || month_pet < 0.5)
                month_pet = 0.5;

            // Modified by FWLoss_4, the scaling factor for potential evapotranspiration

            potEvap = month_pet * fwloss_4;
        }

        /// <summary>
        /// Calculates the short wave radiation outside the atmosphere using Pennman's equation (1948).
        /// R.Boone, almost directly from CENTURY.Last modified: September 25, 2010
        /// </summary>
        private double Shortwave()
        {
            // In CENTURY, a transmission coefficient is set to 0.8 for each month throughout the year.
            // Given that it has probably been that for years, I will hardwire the value.

            // Convert latitude of site to radians
            double radians_lat = globe.latitude * (Math.PI / 180.0);

            // Calculate short wave solar radiation on a clear day using equation in Sellers(1965)
            double declination = 0.401426 * Math.Sin(6.283185 * (julianDayMid[month - 1] - 77.0) / 365.0);
            double temp = 1.0 - Math.Pow(-Math.Tan(radians_lat) * Math.Tan(declination), 2.0);
            if (temp < 0.0)
                temp = 0.0;
            double par1 = Math.Sqrt(temp);
            double par2 = (-Math.Tan(radians_lat) * Math.Tan(declination));

            double ahou = Math.Atan2(par1, par2);
            if (ahou < 0.0)
                ahou = 0.0;

            double solar_radiation = 917.0 * 0.8 * (ahou * Math.Sin(radians_lat) * Math.Sin(declination) + Math.Cos(radians_lat) * Math.Cos(declination) * Math.Sin(ahou));

            return solar_radiation / 0.8;
        }

        /// <summary>
        /// Calculate day length.  Rather than passing month, site latitude, and daylength (as a function), the information 
        /// is stored in structures.Original was in C.Modified as needed(e.g., different array base).  See DAYLEN.C
        /// 
        /// NOTE: This function also resets heat accumulation at the appropriate month, based on when the day length is at a minimum.
        /// </summary>
        private void DayLength()
        {
            // Convert latitude of site to radians
            double radians_lat = globe.latitude * (Math.PI / 180.0);

            double temp_1 = 2.0 * Math.PI * (julianDayStart[month - 1] - 77.0) / 365.0;
            double adelt = 0.4014 * Math.Sin(temp_1);
            temp_1 = 1.0 - Math.Pow(-Math.Tan(radians_lat) * adelt, 2.0);
            if (temp_1 < 0.0)
                temp_1 = 0.0;
            temp_1 = Math.Sqrt(temp_1);
            double temp_2 = -Math.Tan(radians_lat) * Math.Tan(adelt);
            double ahou = Math.Atan2(temp_1, temp_2);
            dayLength = (ahou / Math.PI) * 24.0;

            // Set day length for this and the previous month, to be able to judge if seasons are changing.
            if (dayLength > lastMonthDayLength)
            {
                if (!dayLengthIncreasing) // Starting a new ecological year, so to speak.The days started getting longer this month.
                    heatAccumulation = 0.0;
                dayLengthIncreasing = true;
            }
            else
            {
                dayLengthIncreasing = false;

                // The season is changing, it is the middle of the local summer.   Hopefully not too early to shift snow and snow liquid to the long-term storage
                oldSnow = oldSnow + snow;
                snow = 0.0;
                oldSnowLiquid = oldSnowLiquid + snowLiquid;
                snowLiquid = 0.0;
            }
            lastMonthDayLength = dayLength;
        }

        /// <summary>
        /// Calculate for all the rangeland cells any weather-related attributes, such as snowfall, snowmelt, 
        /// and evapotranspiration.
        /// This section draws heavily on the CENTURY code, as much of this material does.  Especially SNOWCENT
        /// </summary>
        private void SnowDynamics()
        {
            double accum_snow = 0.0;
            double add_to_soil = 0.0;
            double sublimated = 0.0;
            double snow_total = 0.0;
            pptSoil = globe.precip;
            double temp_avg = (globe.maxTemp + globe.minTemp) / 2.0;   // The method used in Century

            // Judge whether precipitation is snow or liquid
            if (temp_avg <= 0.0)
            {
                snow = snow + globe.precip;  // Recall snowpack is water equivalent
                accum_snow = globe.precip;   // Track snow accumulation
                pptSoil = 0.0;        // No water left to move into soil
            }

            // Add rain - on - snow to snowpack liquid
            if (snow > 0.0)
            {
                snowLiquid = snowLiquid + pptSoil;
                pptSoil = 0.0;
            }

            // Evaporate water from the snowpack
            if (snow > 0.0)
            {
                // Calculate cm of snow that remaining PET energy can evaporate
                sublimated = petRemaining * 0.87;   // 0.87 relates to heat of fusion for ice versus liquid water
                // Calculate total snowpack water, ice and liquid
                snow_total = snow + snowLiquid;
                if (sublimated > snow_total)
                    sublimated = snow_total;        // Don't sublimate more than is present
                if (sublimated < 0.0)
                    sublimated = 0.0;

                // Take sublimation from snow and snow liquid in proportion
                snow = snow - (sublimated * (snow / snow_total));
                snowLiquid = snowLiquid - (sublimated * (snowLiquid / snow_total));  // Snow_total cannot be zero, but may be very small.  A problem?)
                evaporation = evaporation + sublimated;                    // Accumulate sublimated snow
                                                                           // Decrement remaining PET by the energy that was used to evaporate snow
                petRemaining = petRemaining - (sublimated / 0.87);
                if (petRemaining < 0.0)
                    petRemaining = 0.0;
            }

            // Melt snow if the temperature is high enough
            if (snow > 0.0 && temp_avg >= parms.meltingTemp)
            {
                melt = parms.meltingSlope * (temp_avg - parms.meltingTemp) * Shortwave();
                if (melt < 0.0)
                    melt = 0.0;
                if ((snow - melt) > 0.0)
                {
                    snow = snow - melt;
                }
                else
                {
                    melt = snow;
                    snow = 0.0;
                }

                // Melting snow is liquid and drains excess
                snowLiquid = snowLiquid + melt;
                // Drain snowpack to 50 % liquid content, and excess to soil.
                if (snowLiquid > (0.5 * snow))
                {
                    add_to_soil = snowLiquid - (0.5 * snow);
                    snowLiquid = snowLiquid - add_to_soil;
                    pptSoil = pptSoil + add_to_soil;
                    // Return drained water into the soil
                    melt = pptSoil;
                }
            }
        }
    }
}
