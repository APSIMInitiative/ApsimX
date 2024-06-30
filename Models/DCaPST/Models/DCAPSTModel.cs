using Models.DCAPST.Canopy;
using Models.DCAPST.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class DCAPSTModel : IPhotosynthesisModel
    {
        /// <summary>
        /// The solar geometry
        /// </summary>
        private ISolarGeometry Solar { get; set; }

        /// <summary>
        /// The solar radiation
        /// </summary>
        private ISolarRadiation Radiation { get; set; }

        /// <summary>
        /// The environmental temperature
        /// </summary>
        private ITemperature Temperature { get; set; }

        /// <summary>
        /// The canopy undergoing photosynthesis
        /// </summary>
        private ICanopyAttributes Canopy { get; set; }

        /// <summary>
        /// The pathway parameters
        /// </summary>
        private readonly IPathwayParameters pathway;

        /// <summary>
        /// The transpiration model
        /// </summary>
        private readonly Transpiration transpiration;

        /// <summary>
        /// A public option to toggle if the interval values are tracked or not
        /// </summary>
        public bool PrintIntervalValues { get; set; } = false;

        /// <summary>
        /// The biological transpiration limit of a plant
        /// </summary>
        public double Biolimit { get; set; } = 0;

        /// <summary>
        /// Excess water reduction fraction
        /// </summary>
        public double Reduction { get; set; } = 0;

        /// <summary>
        /// Used to track the interval values that are printed
        /// </summary>
        public string IntervalResults { get; private set; } = "";

        /// <summary>
        /// Biochemical Conversion and Maintenance Respiration
        /// </summary>
        public double B { get; set; } = 0.409;

        /// <summary>
        /// Potential total daily biomass
        /// </summary>
        public double PotentialBiomass { get; private set; }

        /// <summary>
        /// Actual total daily biomass 
        /// </summary>
        public double ActualBiomass { get; private set; }

        /// <summary>
        /// Daily water demand
        /// </summary>
        public double WaterDemanded { get; private set; }

        /// <summary>
        /// Daily water supplied
        /// </summary>
        public double WaterSupplied { get; private set; }

        /// <summary>
        /// Daily intercepted radiation
        /// </summary>
        public double InterceptedRadiation { get; private set; }

        private readonly double start = 6.0;
        private readonly double end = 18.0;
        private readonly double timestep = 1.0;

        // Seconds in an hour
        private const double SECONDS_IN_HOUR = 3600;

        // 1,000,000 mmol to mol
        private const double MMOL_TO_MOL = 1000000;

        // 44 mol wt CO2
        private const double MOL_WT_CO2 = 44;

        /// <summary>
        /// 
        /// </summary>
        public IntervalValues[] Intervals;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solar"></param>
        /// <param name="radiation"></param>
        /// <param name="temperature"></param>
        /// <param name="pathway"></param>
        /// <param name="canopy"></param>
        /// <param name="trans"></param>
        public DCAPSTModel(
            ISolarGeometry solar,
            ISolarRadiation radiation,
            ITemperature temperature,
            IPathwayParameters pathway,
            ICanopyAttributes canopy,
            Transpiration trans
        )
        {
            Solar = solar;
            Radiation = radiation;
            Temperature = temperature;
            this.pathway = pathway;
            Canopy = canopy;
            transpiration = trans;
        }

        /// <summary>
        /// Calculates the potential and actual biomass growth of a canopy across the span of a day,
        /// as well as the water requirements for both cases.
        /// </summary>
        public void DailyRun(
            double lai,
            double sln,
            double soilWater,
            double rootShootRatio
        )
        {
            var steps = (end - start) / timestep;
            if (steps % 1 == 0) steps++;

            Intervals = Enumerable.Range(0, (int)Math.Ceiling(steps))
                .Select(i => new IntervalValues() { Time = start + i * timestep })
                .ToArray();


            Solar.Initialise();
            Canopy.InitialiseDay(lai, sln);

            // Unlimited potential calculations
            // Note: In the potential case, we assume unlimited water and therefore supply = demand
            transpiration.Limited = false;
            var potential = CalculatePotential();
            var waterDemands = Intervals.Select(i => i.Sunlit.Water + i.Shaded.Water).ToList();

            // Bio-limited calculations
            transpiration.Limited = true;

            // Check if the plant is biologically self-limiting
            if (Biolimit > 0)
            {
                // Percentile reduction
                if (Reduction > 0)
                {
                    waterDemands = waterDemands.Select(w => ReductionFunction(w, Biolimit, Reduction)).ToList();
                }
                // Truncation
                else
                {
                    // Reduce to the flat biological limit
                    waterDemands = waterDemands.Select(w => Math.Min(w, Biolimit)).ToList();
                }

                potential = CalculateLimited(waterDemands);
            }

            // Actual calculations
            var totalDemand = waterDemands.Sum();
            var limitedSupply = CalculateWaterSupplyLimits(soilWater, waterDemands);

            var actual = (soilWater > totalDemand) ? potential : CalculateActual(limitedSupply.ToArray());

            ActualBiomass = CalculateBiomass(actual, rootShootRatio);
            PotentialBiomass = CalculateBiomass(potential, rootShootRatio);
            WaterDemanded = totalDemand;
            WaterSupplied = (soilWater < totalDemand) ? limitedSupply.Sum() : waterDemands.Sum();
        }

        private double CalculateBiomass(double biomass, double rootShootRatio)
        {
            var biomassConversionFactor = biomass * SECONDS_IN_HOUR / MMOL_TO_MOL * MOL_WT_CO2 * B;
            var calculatedBiomass = biomassConversionFactor / (1 + rootShootRatio);
            return calculatedBiomass;
        }

        /// <summary>
        /// Calculates the ratio of A to A + B
        /// </summary>
        private static double RatioFunction(double A, double B)
        {
            var total = A + B;

            return A / total;
        }

        /// <summary>
        /// Reduces the value of any excess water past the limit by a given percentage
        /// </summary>
        /// <param name="water">The total water</param>
        /// <param name="limit">The water limit</param>
        /// <param name="percent">The precentage to reduce excess water by</param>
        /// <returns>Water with reduced excess</returns>
        private static double ReductionFunction(double water, double limit, double percent)
        {
            if (water < limit) return water;

            // Find amount of water past the limit
            var excess = water - limit;

            // Reduce the excess by the percentage
            var reduced = excess * percent;

            return limit + reduced;
        }

        /// <summary>
        /// Attempt to initialise models based on the current time, and test if they are sensible
        /// </summary>
        private bool TryInitiliase(IntervalValues I)
        {
            Temperature.UpdateAirTemperature(I.Time);
            Radiation.UpdateRadiationValues(I.Time);
            var sunAngle = Solar.SunAngle(I.Time);
            Canopy.DoSolarAdjustment(sunAngle);

            if (IsSensible())
                return true;
            else
            {
                I.Sunlit = new AreaValues();
                I.Shaded = new AreaValues();

                return false;
            }
        }

        /// <summary>
        /// Tests if the basic conditions for photosynthesis to occur are met
        /// </summary>
        private bool IsSensible()
        {
            var CPath = Canopy.Canopy;
            var temp = Temperature.AirTemperature;

            bool[] tempConditions = new bool[2]
            {
                temp > pathway.ElectronTransportRateParams.TMax,
                temp < pathway.ElectronTransportRateParams.TMin,
            };

            bool invalidTemp = tempConditions.Any(b => b == true);
            bool invalidRadn = Radiation.Total <= double.Epsilon;

            if (invalidTemp || invalidRadn)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Determine the total potential biomass for the day under ideal conditions
        /// </summary>
        public double CalculatePotential()
        {
            foreach (var I in Intervals)
            {
                if (!TryInitiliase(I)) continue;

                InterceptedRadiation += Radiation.Total * Canopy.GetInterceptedRadiation() * 3600;

                DoTimestepUpdate(I);
            }

            return Intervals.Select(i => i.Sunlit.A + i.Shaded.A).Sum();
        }

        /// <summary>
        /// Calculates the potential biomass in the case where a plant has a biological limit
        /// on transpiration rate
        /// </summary>
        public double CalculateLimited(IEnumerable<double> demands)
        {
            var ratios = Intervals.Select(i => RatioFunction(i.Sunlit.Water, i.Shaded.Water));
            double[] sunlitDemand = demands.Zip(ratios, (d, ratio) => d * ratio).ToArray();
            double[] shadedDemand = demands.Zip(ratios, (d, ratio) => d * (1 - ratio)).ToArray();

            for (int i = 0; i < Intervals.Length; i++)
            {
                var interval = Intervals[i];

                if (!TryInitiliase(interval)) continue;

                double total = sunlitDemand[i] + shadedDemand[i];
                transpiration.MaxRate = total;
                DoTimestepUpdate(interval, sunlitDemand[i] / total, shadedDemand[i] / total);
            }

            return Intervals.Select(i => i.Sunlit.A + i.Shaded.A).Sum();
        }

        /// <summary>
        /// Determine the total biomass that can be assimilated under the actual conditions 
        /// </summary>
        public double CalculateActual(double[] waterSupply)
        {
            double[] sunlitDemand = Intervals.Select(i => i.Sunlit.Water).ToArray();
            double[] shadedDemand = Intervals.Select(i => i.Shaded.Water).ToArray();

            for (int i = 0; i < Intervals.Length; i++)
            {
                var interval = Intervals[i];

                if (!TryInitiliase(interval)) continue;

                transpiration.MaxRate = waterSupply[i];

                double intervalSunlitDemand = sunlitDemand[i];
                double intervalShadedDemand = shadedDemand[i];
                double intervalTotalDemand = intervalSunlitDemand + intervalShadedDemand;
                double intervalSunFraction = intervalSunlitDemand / intervalTotalDemand;
                double intervalShadeFraction = intervalShadedDemand / intervalTotalDemand;

                DoTimestepUpdate(interval, intervalSunFraction, intervalShadeFraction);
            }

            return Intervals.Select(i => i.Sunlit.A + i.Shaded.A).Sum();
        }

        /// <summary>
        /// Updates the model to a new timestep
        /// </summary>
        public void DoTimestepUpdate(IntervalValues interval, double sunFraction = 0, double shadeFraction = 0)
        {
            Canopy.DoTimestepAdjustment(Radiation);

            var totalHeat = Canopy.CalcBoundaryHeatConductance();
            var sunlitHeat = Canopy.CalcSunlitBoundaryHeatConductance();

            var shadedHeat = (totalHeat == sunlitHeat) ? double.Epsilon : totalHeat - sunlitHeat;

            interval.AirTemperature = Temperature.AirTemperature;

            PerformPhotosynthesis(Canopy.Sunlit, sunlitHeat, sunFraction);
            interval.Sunlit = Canopy.Sunlit.GetAreaValues();

            PerformPhotosynthesis(Canopy.Shaded, shadedHeat, shadeFraction);
            interval.Shaded = Canopy.Shaded.GetAreaValues();
        }

        /// <summary>
        /// Runs the photosynthesis simulation for an assimilating area
        /// </summary>
        /// <param name="area">The area to run photosynthesis for</param>
        /// <param name="gbh">The boundary heat conductance</param>
        /// <param name="fraction">Fraction of water allowance</param>
        public void PerformPhotosynthesis(IAssimilationArea area, double gbh, double fraction)
        {
            transpiration.BoundaryHeatConductance = gbh;
            transpiration.Fraction = fraction;
            area.DoPhotosynthesis(Temperature, transpiration);
        }

        /// <summary>
        /// In the case where there is greater water demand than supply allows, the water supply limit for each hour
        /// must be calculated. 
        /// 
        /// This is done by adjusting the maximum rate of water supply each hour, until the total water demand across
        /// the day is within some tolerance of the actual water available, as we want to make use of all the 
        /// accessible water.
        /// </summary>
        private static IEnumerable<double> CalculateWaterSupplyLimits(double soilWaterAvail, IEnumerable<double> demand)
        {
            double initialDemand = demand.Sum();

            if (initialDemand < soilWaterAvail) return demand;

            if (soilWaterAvail < 0.0001) return demand.Select(d => 0.0);

            double maxDemandRate = demand.Max();
            double minDemandRate = 0;
            double averageDemandRate = 0;

            double dailyDemand = initialDemand;

            // While the daily demand is outside some tolerance of the available water
            while (dailyDemand < (soilWaterAvail - 0.000001) || (0.000001 + soilWaterAvail) < dailyDemand)
            {
                averageDemandRate = (maxDemandRate + minDemandRate) / 2;

                // Find the total daily demand when the hourly rate is limited to the average rate
                dailyDemand = demand.Select(d => d > averageDemandRate ? averageDemandRate : d).Sum();

                // Find the total daily demand when the hourly rate is limited to the maximum rate
                var maxDemand = demand.Select(d => d > maxDemandRate ? maxDemandRate : d).Sum();

                // If there is more water available than is being demanded, adjust the minimum demand upwards
                if (dailyDemand < soilWaterAvail) minDemandRate = averageDemandRate;
                // Else, there is less water available than is being demanded, so adjust the maximum demand downwards
                else maxDemandRate = averageDemandRate;
            }
            return demand.Select(d => d > averageDemandRate ? averageDemandRate : d);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class IntervalValues
    {
        /// <summary>
        /// The time of the interval
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Area values for the sunlit canopy
        /// </summary>
        public AreaValues Sunlit { get; set; }

        /// <summary>
        /// Area values for the shaded canopy
        /// </summary>
        public AreaValues Shaded { get; set; }
    }
}
