using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// The possible types of assimilation pathways
    /// </summary>
    public enum PathwayType
    {
        /// <summary>
        /// 
        /// </summary>
        Ac1,

        /// <summary>
        /// 
        /// </summary>
        Ac2,

        /// <summary>
        /// 
        /// </summary>
        Aj
    }

    /// <summary>
    /// 
    /// </summary>
    public class AssimilationPathway
    {
        /// <summary>
        /// The canopy parameters
        /// </summary>
        ICanopyParameters Canopy;

        /// <summary>
        /// The pathway parameters
        /// </summary>
        IPathwayParameters Pathway;

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
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        public AssimilationPathway(ICanopyParameters canopy, IPathwayParameters pathway)
        {
            Canopy = canopy;
            Pathway = pathway;
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

            MesophyllCO2 = Canopy.AirCO2 * Pathway.IntercellularToAirCO2Ratio;
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

    /// <summary>
    /// 
    /// </summary>
    public struct PathValues
    {
        /// <summary>
        /// 
        /// </summary>
        public double Assimilation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Water { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double VPD { get; set; }
    }
}
