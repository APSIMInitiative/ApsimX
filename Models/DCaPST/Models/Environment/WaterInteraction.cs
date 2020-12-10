using System;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Environment
{
    /// <summary>
    /// Models how temperature impacts the water used by the leaf during photosynthesis
    /// </summary>
    public class WaterInteraction : IWaterInteraction
    {
        #region Constants
        /// <summary>
        /// Boltzmann's constant
        /// </summary>
        private readonly double kb = 0.0000000567;

        /// <summary>
        /// Volumetric heat capacity of air
        /// </summary>
        private readonly double sAir = 1200;

        /// <summary>
        /// Psychrometric constant
        /// </summary>
        private readonly double g = 0.066;

        /// <summary>
        /// Heat of vapourisation of water
        /// </summary>
        private readonly double latentHeatOfVapourisation = 2447000;

        /// <summary>
        /// Boundary water diffusion factor
        /// </summary>
        private readonly double m = 1.37;

        /// <summary>
        /// Stomata water diffusion factor
        /// </summary>
        private readonly double n = 1.6;

        /// <summary>
        /// Hours to seconds unit conversion
        /// </summary>
        private readonly double hrs_to_seconds = 3600;

        #endregion

        /// <summary> Environment temperature model </summary>
        private readonly ITemperature temp;

        /// <summary> Current leaf temperature </summary>
        public double LeafTemp { get; set; }

        /// <summary> Canopy boundary heat conductance </summary>
        private double gbh;

        /// <summary> Absorbed radiation </summary>
        private double radiation;

        /// <summary> Boundary H20 conductance </summary>
        private double Gbw => gbh / 0.92;

        /// <summary> Boundary heat resistance </summary>
        private double Rbh => 1 / gbh;

        /// <summary> Boundary CO2 conductance </summary>
        private double GbCO2 => temp.AtmosphericPressure * temp.AirMolarDensity * Gbw / m;

        /// <summary> Outgoing thermal radiation</summary>
        private double ThermalRadiation => 8 * kb * Math.Pow(temp.AirTemperature + 273, 3) * (LeafTemp - temp.AirTemperature);
                
        /// <summary> Vapour pressure at the leaf temperature </summary>
        private double VpLeaf => 0.61365 * Math.Exp(17.502 * LeafTemp / (240.97 + LeafTemp));

        /// <summary> Vapour pressure at the air temperature</summary>
        private double VpAir => 0.61365 * Math.Exp(17.502 * temp.AirTemperature / (240.97 + temp.AirTemperature));

        /// <summary> Vapour pressure at one degree above air temperature</summary>
        private double VpAir1 => 0.61365 * Math.Exp(17.502 * (temp.AirTemperature + 1) / (240.97 + (temp.AirTemperature + 1)));

        /// <summary> Vapour pressure at the daily minimum temperature</summary>
        private double VptMin => 0.61365 * Math.Exp(17.502 * temp.MinTemperature / (240.97 + temp.MinTemperature));

        /// <summary> Difference in air vapour pressures </summary>
        private double DeltaAirVP => VpAir1 - VpAir;

        /// <inheritdoc/>
        public double VPD => VpLeaf - VptMin;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="temperature"></param>
        public WaterInteraction(ITemperature temperature)
        {
            temp = temperature;
        }

        /// <summary>
        /// Sets conditions for the water interaction
        /// </summary>
        /// <param name="gbh">Boundary heat conductance</param>
        /// <param name="radiation">Radiation</param>
        public void SetConditions(double gbh, double radiation)
        {
            this.gbh = (gbh != 0) ? gbh : throw new Exception("Gbh cannot be 0");
            this.radiation = radiation;
        }
        
        /// <summary>
        /// Calculates the leaf resistance to water when the supply is unlimited
        /// </summary>
        /// <param name="A">CO2 assimilation rate</param>
        /// <param name="Ca">Air CO2 partial pressure</param>
        /// <param name="Ci">Intercellular CO2 partial pressure</param>
        public double UnlimitedWaterResistance(double A, double Ca, double Ci)
        {
            // Unit conversion
            var atm_to_kPa = 100;

            // Leaf water mol fraction
            double Wl = VpLeaf / (temp.AtmosphericPressure * atm_to_kPa);

            // Air water mol fraction
            double Wa = VptMin / (temp.AtmosphericPressure * atm_to_kPa);
            
            // temporary variables
            double b = (Wl - Wa) * (Ca + Ci) / (2 - (Wl + Wa));
            double c = Ca - Ci;
            double d = A / GbCO2;
            double e = d * (m + n) + m * (b * n - c);
            double f = d * m * n * (d + b * m - c);
            
            // Stomatal CO2 conductance
            double gsCO2 = 2 * A * m / (Math.Sqrt(e * e - 4 * f) - e);
            
            // Resistances
            double rsCO2 = 1 / (n * gsCO2); // Stomatal
            double rbCO2 = 1 / (m * GbCO2); // Boundary
            double total = rsCO2 + rbCO2;

            // Total leaf water conductance
            double gtw = 1 / total;
            
            // Total resistance to water
            double rtw = temp.AirMolarDensity / gtw * temp.AtmosphericPressure;

            return rtw;
        }

        /// <summary>
        /// Calculates the leaf resistance to water when supply is limited
        /// </summary>
        public double LimitedWaterResistance(double availableWater)
        {        
            // Transpiration in kilos of water per second
            double ekg = latentHeatOfVapourisation * availableWater / hrs_to_seconds;
            double rtw = (DeltaAirVP * Rbh * (radiation - ThermalRadiation - ekg) + VPD * sAir) / (ekg * g);
            return rtw;
        }

        /// <summary>
        /// Calculates the hourly water requirements
        /// </summary>
        /// <param name="rtw">Resistance to water</param>
        public double HourlyWaterUse(double rtw)
        {
            // TODO: Make this work with the timestep model

            // dummy variables
            double a_lump = DeltaAirVP * (radiation - ThermalRadiation) + VPD * sAir / Rbh;
            double b_lump = DeltaAirVP + g * rtw / Rbh;
            double latentHeatLoss = a_lump / b_lump;

            return (latentHeatLoss / latentHeatOfVapourisation) * hrs_to_seconds;
        }

        /// <summary>
        /// Calculates the total CO2 conductance across the leaf
        /// </summary>
        /// <param name="rtw">Resistance to water</param>
        public double TotalCO2Conductance(double rtw)
        {
            // Limited water gsCO2
            var gsCO2 = temp.AirMolarDensity * (temp.AtmosphericPressure / (rtw - (1 / Gbw))) / n;
            var boundaryCO2Resistance = 1 / GbCO2;
            var stomatalCO2Resistance = 1 / gsCO2;
            return 1 / (boundaryCO2Resistance + stomatalCO2Resistance);
        }

        /// <summary>
        /// Finds the leaf temperature after the water interaction
        /// </summary>
        /// <param name="rtw">Resistance to water</param>
        public double LeafTemperature(double rtw)
        {
            // dummy variables
            double a = g * (radiation - ThermalRadiation) * rtw / sAir - VPD;
            double d = DeltaAirVP + g * rtw / Rbh;

            double deltaT = a / d;

            return temp.AirTemperature + deltaT;
        }
    }
}
