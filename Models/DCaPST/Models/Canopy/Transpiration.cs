using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// Models transpiration in the canopy
    /// </summary>
    public class Transpiration
    {
        // Constants
        private const double MolarMassWater = 18.0;
        private const double GramsToKilograms = 1000.0;
        private const double HoursToSeconds = 3600.0;

        /// <summary>
        /// The canopy parameters
        /// </summary>
        private readonly CanopyParameters canopy;

        /// <summary>
        /// The pathway parameters
        /// </summary>
        private readonly PathwayParameters pathway;

        /// <summary>
        /// Models the leaf water interaction
        /// </summary>
        private readonly IWaterInteraction water;

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        private readonly TemperatureResponse leaf;

        /// <summary>
        /// Provides access to the leaf GmT value.
        /// </summary>
        public double LeafGmT => leaf.GmT;

        /// <summary>
        /// If the transpiration rate is limited
        /// </summary>
        public bool Limited { get; set; }

        /// <summary>
        /// The boundary heat conductance
        /// </summary>
        public double BoundaryHeatConductance { get; set; }

        /// <summary>
        /// Maximum transpiration rate
        /// </summary>
        public double MaxRate { get; set; }

        /// <summary>
        /// Fraction of water allocated
        /// </summary>
        public double Fraction { get; set; }

        /// <summary>
        /// Resistance to water
        /// </summary>
        public double Resistance { get; private set; }

        /// <summary>
        /// The amount of CO2 in the air.
        /// </summary>
        private readonly double ambientCO2;

        /// <summary>
        /// 
        /// Initializes a new instance of the <see cref="Transpiration"/> class.
        /// </summary>
        /// <param name="canopy">Canopy parameters</param>
        /// <param name="pathway">Pathway parameters</param>
        /// <param name="water">Water interaction model</param>
        /// <param name="leaf">Leaf temperature response</param>
        /// <param name="ambientCO2"></param>
        public Transpiration(
            CanopyParameters canopy,
            PathwayParameters pathway,
            IWaterInteraction water,
            TemperatureResponse leaf,
            double ambientCO2
        )
        {
            this.canopy = canopy;
            this.pathway = pathway;
            this.water = water;
            this.leaf = leaf;
            this.ambientCO2 = ambientCO2;
        }

        /// <summary>
        /// Sets the current conditions for transpiration
        /// </summary>
        /// <param name="at25C">Parameter rates at 25°C</param>
        /// <param name="photons">Photon flux density</param>
        /// <param name="radiation">Radiation</param>
        public void SetConditions(ParameterRates at25C, double photons, double radiation)
        {
            leaf.SetConditions(at25C, photons);
            water.SetConditions(BoundaryHeatConductance, radiation);
        }

        /// <summary>
        /// Sets the temperature which is needed by the leaf and water interaction.
        /// </summary>
        /// <param name="leafTemperature">Leaf temperature</param>
        public void SetLeafTemperature(double leafTemperature)
        {
            leaf.LeafTemperature = leafTemperature;
            water.LeafTemp = leafTemperature;
        }

        /// <summary>
        /// Signals that the temperature has been updated so that we can recalculate parameters.
        /// </summary>
        public void TemperatureUpdated()
        {
            water.RecalculateParams();
        }

        /// <summary>
        /// Updates the assimilation function for the given pathway.
        /// </summary>
        /// <param name="assimilation">Assimilation model</param>
        /// <param name="pathway">Assimilation pathway</param>
        /// <returns>The updated assimilation function</returns>
        public AssimilationFunction UpdateA(IAssimilation assimilation, AssimilationPathway pathway)
        {
            var func = assimilation.GetFunction(pathway, leaf);

            if (Limited)
            {
                pathway.WaterUse = MaxRate * Fraction;

                // Convert water use to mol/s
                double waterUseMolsSecond = pathway.WaterUse / MolarMassWater * GramsToKilograms / HoursToSeconds;

                // Calculate resistance and conductance
                Resistance = water.LimitedWaterResistance(pathway.WaterUse);
                double Gt = water.TotalCO2Conductance(Resistance);

                // Precompute repeated terms
                double conductanceTerm = Gt + waterUseMolsSecond / 2.0;

                // Update function parameters
                func.Ci = ambientCO2 - (waterUseMolsSecond * ambientCO2 / conductanceTerm);
                func.Rm = 1.0 / conductanceTerm + 1.0 / leaf.GmT;

                // Update pathway
                pathway.CO2Rate = func.Value();
                assimilation.UpdateIntercellularCO2(pathway, Gt, waterUseMolsSecond);
            }
            else
            {
                pathway.IntercellularCO2 = this.pathway.IntercellularToAirCO2Ratio * ambientCO2;

                // Update function parameters
                func.Ci = pathway.IntercellularCO2;
                func.Rm = 1.0 / leaf.GmT;

                // Update pathway
                pathway.CO2Rate = func.Value();

                Resistance = water.UnlimitedWaterResistance(pathway.CO2Rate, ambientCO2, pathway.IntercellularCO2);
                pathway.WaterUse = water.HourlyWaterUse(Resistance);
            }

            // Update vapor pressure deficit
            pathway.VPD = water.VPD;

            return func;
        }

        /// <summary>
        /// Updates the temperature of a pathway
        /// </summary>
        /// <param name="pathway">Assimilation pathway</param>
        public void UpdateTemperature(AssimilationPathway pathway)
        {
            double leafTemp = water.LeafTemperature(Resistance);
            pathway.Temperature = (leafTemp + pathway.Temperature) / 2.0;
        }
    }
}
