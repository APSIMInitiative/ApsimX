using Models.DCAPST.Interfaces;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Models the parameters of the leaf necessary to calculate photosynthesis.
    /// </summary>
    public class TemperatureResponse
    {
        private const double UNIVERSAL_GAS_CONSTANT = 8.314;
        private const double ABSOLUTE_0C = 273;
        private const double ABSOLUTE_25C = 298.15;
        private const double ABSOLUTE_25C_X_GAS_CONSTANT = ABSOLUTE_25C * UNIVERSAL_GAS_CONSTANT;

        /// <summary>
        /// A collection of parameters as valued at 25 degrees Celsius.
        /// </summary>
        private ParameterRates rateAt25;

        /// <summary>
        /// The parameters describing the canopy.
        /// </summary>
        private readonly ICanopyParameters canopy;

        /// <summary>
        /// The static parameters describing the assimilation pathway.
        /// </summary>
        private readonly IPathwayParameters pathway;

        /// <summary>
        /// Number of photons that reached the leaf.
        /// </summary>
        private double photonCount;

        /// <summary>
        /// The leaf temperature.
        /// </summary>
        private double leafTemperature;

        /// <summary>
        /// Records whether the parameters need to be updated due to invalidation.
        /// </summary>
        private bool paramsNeedUpdate;

        // Cached calculated values.
        private double vcMaxT;
        private double rdT;
        private double jMaxT;
        private double vpMaxT;
        private double gmT;
        private double kc;
        private double ko;
        private double vcVo;
        private double kp;
        private double j;
        private double sco;
        private double gamma;
        private double gmRd;

        /// <summary>
        /// Constructor for TemperatureResponse.
        /// </summary>
        /// <param name="canopy">The canopy parameters.</param>
        /// <param name="pathway">The pathway parameters.</param>
        public TemperatureResponse(ICanopyParameters canopy, IPathwayParameters pathway)
        {
            this.canopy = canopy ?? throw new ArgumentNullException(nameof(canopy));
            this.pathway = pathway ?? throw new ArgumentNullException(nameof(pathway));
        }

        /// <summary>
        /// Sets rates and photon count.
        /// </summary>
        /// <param name="rates">The parameter rates at 25 degrees Celsius.</param>
        /// <param name="photons">The number of photons.</param>
        public void SetConditions(ParameterRates rates, double photons)
        {
            rateAt25 = rates ?? throw new ArgumentNullException(nameof(rates));
            photonCount = photons;
            paramsNeedUpdate = true;
        }

        /// <summary>
        /// The current leaf temperature.
        /// </summary>
        public double LeafTemperature
        {
            get => leafTemperature;
            set
            {
                if (leafTemperature != value)
                {
                    leafTemperature = value;
                    paramsNeedUpdate = true;
                }
            }
        }

        /// <summary>
        /// Maximum rate of rubisco carboxylation at the current leaf temperature (micro mol CO2 m^-2 ground s^-1).
        /// </summary>
        public double VcMaxT => EnsureUpdated(ref vcMaxT);

        /// <summary>
        /// Leaf respiration at the current leaf temperature (micro mol CO2 m^-2 ground s^-1).
        /// </summary>
        public double RdT => EnsureUpdated(ref rdT);

        /// <summary>
        /// Maximum rate of electron transport at the current leaf temperature (micro mol CO2 m^-2 ground s^-1).
        /// </summary>
        public double JMaxT => EnsureUpdated(ref jMaxT);

        /// <summary>
        /// Maximum PEP carboxylase activity at the current leaf temperature (micro mol CO2 m^-2 ground s^-1).
        /// </summary>
        public double VpMaxT => EnsureUpdated(ref vpMaxT);

        /// <summary>
        /// Mesophyll conductance at the current leaf temperature (mol CO2 m^-2 ground s^-1 bar^-1).
        /// </summary>
        public double GmT => EnsureUpdated(ref gmT);

        /// <summary>
        /// Michaelis-Menten constant of Rubisco for CO2 (microbar).
        /// </summary>
        public double Kc => EnsureUpdated(ref kc);

        /// <summary>
        /// Michaelis-Menten constant of Rubisco for O2 (microbar).
        /// </summary>
        public double Ko => EnsureUpdated(ref ko);

        /// <summary>
        /// Ratio of Rubisco carboxylation to Rubisco oxygenation.
        /// </summary>
        public double VcVo => EnsureUpdated(ref vcVo);

        /// <summary>
        /// Michaelis-Menten constant of PEP carboxylase for CO2 (micro bar).
        /// </summary>
        public double Kp => EnsureUpdated(ref kp);

        /// <summary>
        /// Electron transport rate of the leaf (micro mol e- m^-2 ground s^-1).
        /// </summary>
        public double J => EnsureUpdated(ref j);

        /// <summary>
        /// Relative CO2/O2 specificity of Rubisco (bar bar^-1).
        /// </summary>
        public double Sco => EnsureUpdated(ref sco);

        /// <summary>
        /// Half the reciprocal of the relative rubisco specificity (bar).
        /// </summary>
        public double Gamma => EnsureUpdated(ref gamma);

        /// <summary>
        /// Mesophyll respiration rate at the current leaf temperature.
        /// </summary>
        public double GmRd => EnsureUpdated(ref gmRd);

        /// <summary>
        /// Ensures the parameters are updated before returning the requested value.
        /// </summary>
        private double EnsureUpdated(ref double value)
        {
            if (paramsNeedUpdate)
                RecalculateParams();
            return value;
        }

        /// <summary>
        /// Recalculates temperature-dependent parameters.
        /// </summary>
        private void RecalculateParams()
        {
            paramsNeedUpdate = false;

            double leafTemperaturePlus0C = leafTemperature + ABSOLUTE_0C;
            double leafTemperaturePlus0CMinus25C = leafTemperaturePlus0C - ABSOLUTE_25C;

            vcMaxT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.VcMax, pathway.RubiscoActivity.Factor);
            rdT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.Rd, pathway.Respiration.Factor);
            jMaxT = ValueOptimum(leafTemperature, rateAt25.JMax, pathway.ElectronTransportRateParams);
            vpMaxT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.VpMax, pathway.PEPcActivity.Factor);
            gmT = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, rateAt25.Gm, pathway.MesophyllCO2ConductanceParams.Factor);
            kc = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.RubiscoCarboxylation.At25, pathway.RubiscoCarboxylation.Factor);
            ko = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.RubiscoOxygenation.At25, pathway.RubiscoOxygenation.Factor);
            vcVo = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.RubiscoCarboxylationToOxygenation.At25, pathway.RubiscoCarboxylationToOxygenation.Factor);
            kp = Value(leafTemperaturePlus0C, leafTemperaturePlus0CMinus25C, pathway.PEPc.At25, pathway.PEPc.Factor);

            UpdateElectronTransportRate();

            sco = Ko / Kc * VcVo;
            gamma = 0.5 / Sco;
            gmRd = RdT * 0.5;
        }

        /// <summary>
        /// Uses an exponential function to model temperature response parameters.
        /// </summary>
        /// <remarks>
        /// See equation (1), A. Wu et al (2018) for details.
        /// </remarks>
        private static double Value(double leafTempPlus0C, double leafTempPlus0CMinus25C, double P25, double factor)
        {
            double exponent = (factor * leafTempPlus0CMinus25C) / (ABSOLUTE_25C_X_GAS_CONSTANT * leafTempPlus0C);
            return P25 * Math.Exp(exponent);
        }

        /// <summary>
        /// Uses a normal distribution to model parameters with an apparent optimum in temperature response.
        /// </summary>
        /// <remarks>
        /// See equation (2), A. Wu et al (2018) for details.
        /// </remarks>
        private static double ValueOptimum(double temp, double P25, LeafTemperatureParameters p)
        {
            double tMin = p.TMin, tOpt = p.TOpt, tMax = p.TMax;
            double tOptMinusTMin = tOpt - tMin;
            double alpha = Math.Log(2) / Math.Log((tMax - tMin) / tOptMinusTMin);

            double tempMinusTMin = temp - tMin;
            double tempAlpha = Math.Pow(tempMinusTMin, alpha);
            double tOptAlpha = Math.Pow(tOptMinusTMin, alpha);

            double numerator = 2 * tempAlpha * tOptAlpha - Math.Pow(tempMinusTMin, 2 * alpha);
            double denominator = Math.Pow(tOptMinusTMin, 2 * alpha);

            return P25 * Math.Pow(numerator / denominator, p.Beta) / p.C;
        }

        /// <summary>
        /// Calculates the electron transport rate of the leaf.
        /// </summary>
        private void UpdateElectronTransportRate()
        {
            double factor = photonCount * (1.0 - pathway.SpectralCorrectionFactor) / 2.0;
            double sumFactor = factor + jMaxT;
            double sqrtTerm = Math.Sqrt(sumFactor * sumFactor - 4 * canopy.CurvatureFactor * jMaxT * factor);
            j = (sumFactor - sqrtTerm) / (2 * canopy.CurvatureFactor);
        }
    }
}
