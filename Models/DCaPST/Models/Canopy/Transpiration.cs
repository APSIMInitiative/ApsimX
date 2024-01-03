using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// 
    /// </summary>
    public class Transpiration
    {
        /// <summary>
        /// The canopy parameters
        /// </summary>
        public ICanopyParameters Canopy { get; private set; }

        /// <summary>
        /// The pathway parameters
        /// </summary>

        public IPathwayParameters Pathway { get; private set; }

        /// <summary>
        /// Models the leaf water interaction
        /// </summary>
        public IWaterInteraction Water { get; }

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        public TemperatureResponse Leaf { get; set; }

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
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        /// <param name="water"></param>
        /// <param name="leaf"></param>
        public Transpiration(
            ICanopyParameters canopy,
            IPathwayParameters pathway,
            IWaterInteraction water,
            TemperatureResponse leaf
        )
        {
            Canopy = canopy;
            Pathway = pathway;
            Water = water;
            Leaf = leaf;
        }

        /// <summary>
        /// Sets the current conditions for transpiration
        /// </summary>
        public void SetConditions(ParameterRates At25C, double photons, double radiation)
        {
            Leaf.SetConditions(At25C, photons);
            Water.SetConditions(BoundaryHeatConductance, radiation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assimilation"></param>
        /// <param name="pathway"></param>
        /// <returns></returns>
        public AssimilationFunction UpdateA(IAssimilation assimilation, AssimilationPathway pathway)
        {
            var func = assimilation.GetFunction(pathway, Leaf);

            if (Limited)
            {
                var molarMassWater = 18;
                var g_to_kg = 1000;
                var hrs_to_seconds = 3600;

                pathway.WaterUse = MaxRate * Fraction;
                var WaterUseMolsSecond = pathway.WaterUse / molarMassWater * g_to_kg / hrs_to_seconds;

                Resistance = Water.LimitedWaterResistance(pathway.WaterUse);
                var Gt = Water.TotalCO2Conductance(Resistance);

                func.Ci = Canopy.AirCO2 - WaterUseMolsSecond * Canopy.AirCO2 / (Gt + WaterUseMolsSecond / 2.0);
                func.Rm = 1 / (Gt + WaterUseMolsSecond / 2) + 1.0 / Leaf.GmT;

                pathway.CO2Rate = func.Value();

                assimilation.UpdateIntercellularCO2(pathway, Gt, WaterUseMolsSecond);
            }
            else
            {
                pathway.IntercellularCO2 = Pathway.IntercellularToAirCO2Ratio * Canopy.AirCO2;

                func.Ci = pathway.IntercellularCO2;
                func.Rm = 1 / Leaf.GmT;

                pathway.CO2Rate = func.Value();

                Resistance = Water.UnlimitedWaterResistance(pathway.CO2Rate, Canopy.AirCO2, pathway.IntercellularCO2);
                pathway.WaterUse = Water.HourlyWaterUse(Resistance);
            }
            pathway.VPD = Water.VPD;

            return func;
        }

        /// <summary>
        /// Updates the temperature of a pathway
        /// </summary>
        public void UpdateTemperature(AssimilationPathway pathway)
        {
            var leafTemp = Water.LeafTemperature(Resistance);
            pathway.Temperature = (leafTemp + pathway.Temperature) / 2.0;
        }
    }
}
