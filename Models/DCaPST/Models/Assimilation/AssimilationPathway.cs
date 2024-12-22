using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// 
    /// </summary>
    public class AssimilationPathway
    {
        /// <summary>
        /// The canopy parameters
        /// </summary>
        private readonly CanopyParameters Canopy;

        /// <summary>
        /// The pathway parameters
        /// </summary>
        private readonly PathwayParameters Pathway;

        /// <summary>
        /// The current pathway type
        /// </summary>
        public PathwayType Type { get; set; }

        /// <summary>
        /// The current temperature of the pathway
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// The rate at which CO2 is assimilated
        /// </summary>
        public double CO2Rate { get; set; }

        /// <summary>
        /// The water required to maintain the CO2 rate
        /// </summary>
        public double WaterUse { get; set; }

        /// <summary>
        /// Vapour pressure deficit
        /// </summary>
        public double VPD { get; set; }

        /// <summary>
        /// Intercellular airspace CO2 partial pressure (microbar)
        /// </summary>
        public double IntercellularCO2 { get; set; }

        /// <summary>
        /// Mesophyll CO2 partial pressure (microbar)
        /// </summary>
        public double MesophyllCO2 { get; set; }

        /// <summary>
        /// Chloroplastic CO2 partial pressure at the site of Rubisco carboxylation (microbar)
        /// </summary>
        public double ChloroplasticCO2 { get; set; }

        /// <summary>
        /// Chloroplastic O2 partial pressure at the site of Rubisco carboxylation (microbar)
        /// </summary>
        public double ChloroplasticO2 { get; set; }

        /// <summary>
        /// Bundle sheath conductance
        /// </summary>
        public double Gbs { get; private set; }

        /// <summary>
        /// PEP regeneration
        /// </summary>
        public double Vpr { get; private set; }

        /// <summary>
        /// The amount of CO2 in the air.
        /// </summary>
        private readonly double ambientCO2;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        /// <param name="ambientCO2"></param>
        public AssimilationPathway(CanopyParameters canopy, PathwayParameters pathway, double ambientCO2)
        {
            Canopy = canopy;
            Pathway = pathway;
            this.ambientCO2 = ambientCO2;
        }

        /// <summary>
        /// Establishes the current conditions of the pathway
        /// </summary>
        /// <param name="temperature">The current temperature</param>
        /// <param name="lai">The current leaf area index</param>
        public void SetConditions(double temperature, double lai)
        {
            Temperature = temperature;
            Gbs = Pathway.BundleSheathConductance * lai;
            Vpr = Pathway.PEPRegeneration * lai;

            MesophyllCO2 = ambientCO2 * Pathway.IntercellularToAirCO2Ratio;
            ChloroplasticCO2 = 1000;
            ChloroplasticO2 = 210000;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public PathValues GetPathValues()
        {
            var values = new PathValues()
            {
                Assimilation = CO2Rate,
                Water = WaterUse,
                Temperature = Temperature,
                VPD = VPD
            };

            return values;
        }
    }
}
