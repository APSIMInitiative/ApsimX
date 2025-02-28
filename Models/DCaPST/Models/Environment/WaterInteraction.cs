using Models.DCAPST.Interfaces;
using System;

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
        private readonly ITemperature temperature;

        /// <summary> Current leaf temperature </summary>
        private double leafTemp;

        /// <summary> Current leaf temperature </summary>
        public double LeafTemp 
        {
            get
            {
                return leafTemp;
            }
            
            set
            {
                if (leafTemp != value)
                {
                    leafTemp = value;
                    RecalculateParams();
                }
            }
        }

        /// <inheritdoc/>
        public double VPD { get; private set; }

        /// <inheritdoc/>
        public void RecalculateParams()
        {
            var airTemperature = temperature.AirTemperature;
            var minTemperature = temperature.MinTemperature;

            gbw = gbh / 0.92;
            rbh = 1 / gbh;
            gbCO2 = temperature.AtmosphericPressure * temperature.AirMolarDensity * gbw / m;
            thermalRadiation =  8 * kb * Math.Pow(airTemperature + 273, 3) * (LeafTemp - airTemperature);
            vpLeaf = 0.61365 * Math.Exp(17.502 * LeafTemp / (240.97 + LeafTemp));
            vpAir = 0.61365 * Math.Exp(17.502 * airTemperature / (240.97 + airTemperature));
            vpAir1 = 0.61365 * Math.Exp(17.502 * (airTemperature + 1) / (240.97 + (airTemperature + 1)));
            vptMin = 0.61365 * Math.Exp(17.502 * minTemperature / (240.97 + minTemperature));
            deltaAirVP = vpAir1 - vpAir;
            VPD = vpLeaf - vptMin;
        }

        /// <summary> Canopy boundary heat conductance </summary>
        private double gbh;

        /// <summary> Absorbed radiation </summary>
        private double radiation;

        /// <summary> Boundary H20 conductance </summary>
        private double gbw;

        /// <summary> Boundary heat resistance </summary>
        private double rbh;

        /// <summary> Boundary CO2 conductance </summary>
        private double gbCO2;

        /// <summary> Outgoing thermal radiation</summary>
        private double thermalRadiation;
                
        /// <summary> Vapour pressure at the leaf temperature </summary>
        private double vpLeaf;

        /// <summary> Vapour pressure at the air temperature</summary>
        private double vpAir;

        /// <summary> Vapour pressure at one degree above air temperature</summary>
        private double vpAir1;

        /// <summary> Vapour pressure at the daily minimum temperature</summary>
        private double vptMin;

        /// <summary> Difference in air vapour pressures </summary>
        private double deltaAirVP;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="temperature"></param>
        public WaterInteraction(ITemperature temperature)
        {
            this.temperature = temperature;
        }

        /// <summary>
        /// Sets conditions for the water interaction
        /// </summary>
        /// <param name="gbh">Boundary heat conductance</param>
        /// <param name="radiation">Radiation</param>
        public void SetConditions(double gbh, double radiation)
        {
            if (gbh == 0) throw new Exception("Gbh cannot be 0");

            this.gbh = gbh;
            this.radiation = radiation;

            RecalculateParams();
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

            var atmosphericPressure = temperature.AtmosphericPressure;
            var atmosphericPressurekPa = atmosphericPressure * atm_to_kPa;

            // Leaf water mol fraction
            double Wl = vpLeaf / atmosphericPressurekPa;

            // Air water mol fraction
            double Wa = vptMin / atmosphericPressurekPa;
            
            // temporary variables
            double b = (Wl - Wa) * (Ca + Ci) / (2 - (Wl + Wa));
            double c = Ca - Ci;
            double d = A / gbCO2;
            double e = d * (m + n) + m * (b * n - c);
            double f = d * m * n * (d + b * m - c);
            
            // Stomatal CO2 conductance
            double gsCO2 = 2 * A * m / (Math.Sqrt(e * e - 4 * f) - e);
            
            // Resistances
            double rsCO2 = 1 / (n * gsCO2); // Stomatal
            double rbCO2 = 1 / (m * gbCO2); // Boundary
            double total = rsCO2 + rbCO2;

            // Total leaf water conductance
            double gtw = 1 / total;
            
            // Total resistance to water
            double rtw = temperature.AirMolarDensity / gtw * atmosphericPressure;

            return rtw;
        }

        /// <summary>
        /// Calculates the leaf resistance to water when supply is limited
        /// </summary>
        public double LimitedWaterResistance(double availableWater)
        {        
            // Transpiration in kilos of water per second
            double ekg = latentHeatOfVapourisation * availableWater / hrs_to_seconds;
            double rtw = (deltaAirVP * rbh * (radiation - thermalRadiation - ekg) + VPD * sAir) / (ekg * g);
            return rtw;
        }

        /// <summary>
        /// Calculates the hourly water requirements
        /// </summary>
        /// <param name="rtw">Resistance to water</param>
        public double HourlyWaterUse(double rtw)
        {
            double a_lump = deltaAirVP * (radiation - thermalRadiation) + VPD * sAir / rbh;
            double b_lump = deltaAirVP + g * rtw / rbh;
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
            var gsCO2 = temperature.AirMolarDensity * (temperature.AtmosphericPressure / (rtw - (1 / gbw))) / n;
            var boundaryCO2Resistance = 1 / gbCO2;
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
            double a = g * (radiation - thermalRadiation) * rtw / sAir - VPD;
            double d = deltaAirVP + g * rtw / rbh;

            double deltaT = a / d;

            return temperature.AirTemperature + deltaT;
        }
    }
}
