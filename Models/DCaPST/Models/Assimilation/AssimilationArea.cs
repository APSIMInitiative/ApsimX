using Models.DCAPST.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// Models a subsection of the canopy (used for distinguishing between sunlit and shaded)
    /// </summary>
    public class AssimilationArea : IAssimilationArea
    {
        /// <summary>
        /// The assimilation model
        /// </summary>
        readonly IAssimilation assimilation;

        /// <summary>
        /// A group of parameters valued at the reference temperature of 25 Celsius
        /// </summary>
        public ParameterRates At25C { get; private set; } = new ParameterRates();

        /// <summary>
        /// The leaf area index of this part of the canopy
        /// </summary>
        public double LAI { get; set; }

        /// <summary>
        /// The sunlight absorbed by the canopy over a period of time
        /// </summary>
        public double AbsorbedRadiation { get; set; }

        /// <summary>
        /// The number of photons which reached the canopy over a period of time
        /// </summary>
        public double PhotonCount { get; set; }

        /// <summary>
        /// CO2 assimilation rate over a period of time
        /// </summary>
        protected double CO2AssimilationRate { get; set; }

        /// <summary>
        /// Water used during photosynthesis
        /// </summary>
        protected double WaterUse { get; set; }

        /// <summary>
        /// The possible assimilation pathways
        /// </summary>
        protected List<AssimilationPathway> pathways;

        /// <summary>
        /// 
        /// </summary>
        private bool includeAc2Pathway;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="includeAc2Pathway"></param>
        /// <param name="Ac1"></param>
        /// <param name="Ac2"></param>
        /// <param name="Aj"></param>
        /// <param name="assimilation"></param>
        public AssimilationArea(
            bool includeAc2Pathway,
            AssimilationPathway Ac1,
            AssimilationPathway Ac2,
            AssimilationPathway Aj,
            IAssimilation assimilation
        )
        {
            this.includeAc2Pathway = includeAc2Pathway;

            pathways = new List<AssimilationPathway>();

            // Always include Ac1
            Ac1.Type = PathwayType.Ac1;
            pathways.Add(Ac1);

            // Conditionally include Ac2
            if (this.includeAc2Pathway)
            {
                Ac2.Type = PathwayType.Ac2;
                if (assimilation is not AssimilationC3) pathways.Add(Ac2);
            }

            // Always include Aj
            Aj.Type = PathwayType.Aj;
            pathways.Add(Aj);

            this.assimilation = assimilation;
        }

        /// <summary>
        /// Finds the CO2 assimilation rate
        /// </summary>
        public double GetCO2Rate() => pathways.Min(p => p.CO2Rate);

        /// <summary>
        /// Finds the water used during CO2 assimilation
        /// </summary>
        public double GetWaterUse() => pathways.Min(p => p.WaterUse);

        /// <summary>
        /// Calculates the CO2 assimilated by the partial canopy during photosynthesis,
        /// and the water used by the process
        /// </summary>
        public void DoPhotosynthesis(ITemperature temperature, Transpiration transpiration)
        {
            CO2AssimilationRate = 0;
            WaterUse = 0;

            // Do the initial iterations
            DoIterations(transpiration, temperature.AirTemperature, true);

            var co2Rate = GetCO2Rate();
            var waterUse = GetWaterUse();

            // If the iteration results are not sensible (e.g negative/0 concentrations) after performing each iteration
            // for each hour and pathway, return.
            if (co2Rate <= 0 || waterUse <= 0)
            {
                return;
            }

            // Update results only if convergence succeeds
            CO2AssimilationRate = co2Rate;
            WaterUse = waterUse;
        }

        /// <summary>
        /// Repeat the assimilation calculation to let the result converge
        /// </summary>
        private void DoIterations(Transpiration transpiration, double airTemp, bool updateTemperature)
        {
            pathways.ForEach(p => p.SetConditions(airTemp, LAI));
            transpiration.SetConditions(At25C, PhotonCount, AbsorbedRadiation);

            for (int n = 0; n < assimilation.Iterations; n++)
            {
                if (!UpdateAssimilation(transpiration, updateTemperature))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Calculates the assimilation values for each pathway
        /// </summary>
        private bool UpdateAssimilation(Transpiration transpiration, bool updateTemperature)
        {
            foreach (var p in pathways)
            {
                transpiration.SetLeafTemperature(p.Temperature);

                // Calculate the actual photosynthesis rate.
                var func = transpiration.UpdateA(assimilation, p);
                assimilation.UpdatePartialPressures(p, transpiration.LeafGmT, func);

                if (updateTemperature)
                {
                    transpiration.UpdateTemperature(p);
                }

                if (double.IsNaN(p.CO2Rate) || double.IsNaN(p.WaterUse))
                {
                    // If we got here then there was no CO2 assimilation or there was no water use.
                    // DCaPST will use the minimum of either pathway so if any return 0 then we need 
                    // to stop to save time.
                    p.CO2Rate = 0;
                    p.WaterUse = 0;

                    return false;                   
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public AreaValues GetAreaValues()
        {
            // Casting to an array to ensure we don't change pathways outside of this method
            var temp = pathways.ToArray().OrderBy(p => p.CO2Rate).First().Temperature;

            var ac1 = pathways.FirstOrDefault(p => p.Type == PathwayType.Ac1).GetPathValues();

            var ac2 = pathways.FirstOrDefault(p => p.Type == PathwayType.Ac2)
                is AssimilationPathway path ? path.GetPathValues() : new PathValues();

            var aj = pathways.FirstOrDefault(p => p.Type == PathwayType.Aj).GetPathValues();

            var values = new AreaValues()
            {
                A = CO2AssimilationRate,
                Water = WaterUse,
                Temperature = temp,
                Ac1 = ac1,
                Ac2 = ac2,
                Aj = aj
            };

            return values;
        }
    }
}
