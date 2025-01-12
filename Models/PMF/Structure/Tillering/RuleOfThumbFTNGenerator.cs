using Models.Interfaces;
using System;

namespace Models.PMF.Struct
{
    /// <summary>
    /// Estimate tillering given latitude, density, time of planting and
    /// row configuration. this will be replaced with dynamic
    /// calculations in the near future. Above latitude -25 is CQ, -25
    /// to -29 is SQ, below is NNSW.
    /// </summary>
    public static class RuleOfThumbFTNGenerator
    {
        /// <summary>
        /// Calculate the FTN
        /// </summary>
        /// <param name="weather"></param>
        /// <param name="plant"></param>
        /// <param name="clock"></param>
        /// <returns>The FTN</returns>
        /// <exception cref="Exception"></exception>
        public static double CalculateFtn(
            IWeather weather,
            Plant plant,
            IClock clock
        )
        {
            double intercept = 0.0;
            double slope = 0.0;

            if (weather.Latitude > -12.5 || weather.Latitude < -38.0)
            {
                // Unknown region.
                throw new Exception("Unable to estimate number of tillers at latitude {weather.Latitude}");
            }

            if (weather.Latitude > -25.0)
            {
                // Central Queensland.
                if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
                {
                    // Between 1 July and 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.5786; slope = -0.0521;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 0.8786; slope = -0.0696;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 1.1786; slope = -0.0871;
                    }
                }
                else
                {
                    // After 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.4786; slope = -0.0421;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5)
                        intercept = 0.6393; slope = -0.0486;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 0.8000; slope = -0.0550;
                    }
                }
            }
            else if (weather.Latitude > -29.0)
            {
                // South Queensland.
                if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
                {
                    // Between 1 July and 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double  (2.0).
                        intercept = 1.1571; slope = -0.1043;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.7571; slope = -0.1393;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 2.3571; slope = -0.1743;
                    }
                }
                else
                {
                    // After 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.6786; slope = -0.0621;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.1679; slope = -0.0957;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 1.6571; slope = -0.1293;
                    }
                }
            }
            else
            {
                // Northern NSW.
                if (clock.Today.DayOfYear < 319 && clock.Today.DayOfYear > 182)
                {
                    //  Between 1 July and 15 November.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 1.3571; slope = -0.1243;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 2.2357; slope = -0.1814;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 3.1143; slope = -0.2386;
                    }
                }
                else if (clock.Today.DayOfYear > 349 || clock.Today.DayOfYear < 182)
                {
                    // Between 15 December and 1 July.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.4000; slope = -0.0400;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.0571; slope = -0.0943;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 1.7143; slope = -0.1486;
                    }
                }
                else
                {
                    // Between 15 November and 15 December.
                    if (plant.SowingData.SkipRow > 1.9)
                    {
                        // Double (2.0).
                        intercept = 0.8786; slope = -0.0821;
                    }
                    else if (plant.SowingData.SkipRow > 1.4)
                    {
                        // Single (1.5).
                        intercept = 1.6464; slope = -0.1379;
                    }
                    else
                    {
                        // Solid (1.0).
                        intercept = 2.4143; slope = -0.1936;
                    }
                }
            }

            return Math.Max(slope * plant.SowingData.Population + intercept, 0);
        }
    }
}
