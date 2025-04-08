using Models.DCAPST.Interfaces;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Models the parameters of the leaf necessary to calculate photosynthesis
    /// </summary>
    public class TemperatureResponse
    {
        private const double UNIVERSAL_GAS_CONSTANT = 8.314;
        private const double ABSOLUTE_0C = 273;
        private const double ABSOLUTE_25C = 298.15;
        private const double ABSOLUTE_25C_X_GAS_CONSTANT = ABSOLUTE_25C * UNIVERSAL_GAS_CONSTANT;

        /// <summary>
        /// A collection of parameters as valued at 25 degrees Celsius
        /// </summary>
        private ParameterRates rateAt25;

        /// <summary>
        /// The parameters describing the canopy
        /// </summary>
        private readonly CanopyParameters _canopy;

        /// <summary>
        /// The static parameters describing the assimilation pathway
        /// </summary>
        private readonly PathwayParameters _pathway;

        /// <summary>
        /// Number of photons that reached the leaf
        /// </summary>
        private double photonCount;

        /// <summary>
        /// The leaf temperature.
        /// </summary>
        private double leafTemperature;

        /// <summary>
        /// Records whether the params need to be updated, due to something changing which 
        /// has invalidated them.
        /// </summary>
        private bool paramsNeedUpdate;

        // Store recalculated parameters
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
        /// Constructor
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        public TemperatureResponse(CanopyParameters canopy, PathwayParameters pathway)
        {
            _canopy = canopy;
            _pathway = pathway;
        }

        /// <summary>
        /// Sets rates and photon count.
        /// </summary>
        /// <param name="rates"></param>
        /// <param name="photons"></param>
        public void SetConditions(ParameterRates rates, double photons)
        {
            rateAt25 = rates ?? throw new ArgumentNullException(nameof(rates));
            photonCount = photons;
            paramsNeedUpdate = true;
        }

        /// <summary>
        /// The current leaf temperature
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
        /// Maximum rate of rubisco carboxylation at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VcMaxT
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return vcMaxT;
            }
        }

        /// <summary>
        /// Leaf respiration at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double RdT
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return rdT;
            }
        }

        /// <summary>
        /// Maximum rate of electron transport at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double JMaxT
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return jMaxT;
            }
        }

        /// <summary>
        /// Maximum PEP carboxylase activity at the current leaf temperature (micro mol CO2 m^-2 ground s^-1)
        /// </summary>
        public double VpMaxT
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return vpMaxT;
            }
        }

        /// <summary>
        /// Mesophyll conductance at the current leaf temperature (mol CO2 m^-2 ground s^-1 bar^-1)
        /// </summary>
        public double GmT
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return gmT;
            }
        }

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for CO2 (microbar)
        /// </summary>
        public double Kc
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return kc;
            }
        }

        /// <summary>
        /// Michaelis-Menten constant of Rubsico for O2 (microbar)
        /// </summary>
        public double Ko
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return ko;
            }
        }

        /// <summary>
        /// Ratio of Rubisco carboxylation to Rubisco oxygenation
        /// </summary>
        public double VcVo
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return vcVo;
            }
        }

        /// <summary>
        /// Michaelis-Menten constant of PEP carboxylase for CO2 (micro bar)
        /// </summary>
        public double Kp
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return kp;
            }
        }

        /// <summary>
        /// Electron transport rate
        /// </summary>
        public double J
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return j;
            }
        }

        /// <summary>
        /// Relative CO2/O2 specificity of Rubisco (bar bar^-1)
        /// </summary>
        public double Sco
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return sco;
            }
        }

        /// <summary>
        /// Half the reciprocal of the relative rubisco specificity
        /// </summary>
        public double Gamma
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return gamma;
            }
        }

        /// <summary>
        /// Mesophyll respiration
        /// </summary>
        public double GmRd
        {
            get
            {
                if (paramsNeedUpdate) RecalculateParams();
                return gmRd;
            }
        }
        private void RecalculateParams()
        {
            paramsNeedUpdate = false;

            // Precompute shared values
            var leafTempAbs = leafTemperature + ABSOLUTE_0C;
            var leafTempAbsMinus25C = leafTempAbs - ABSOLUTE_25C;
            var denominator = ABSOLUTE_25C_X_GAS_CONSTANT * leafTempAbs;

            // Recalculate photosynthetic parameters
            vcMaxT = CalculateParam(rateAt25.VcMax, _pathway.RubiscoActivity.Factor, leafTempAbsMinus25C, denominator);
            rdT = CalculateParam(rateAt25.Rd, _pathway.Respiration.Factor, leafTempAbsMinus25C, denominator);
            jMaxT = CalculateParamOptimum(leafTemperature, rateAt25.JMax, _pathway.ElectronTransportRateParams);
            vpMaxT = CalculateParam(rateAt25.VpMax, _pathway.PEPcActivity.Factor, leafTempAbsMinus25C, denominator);

            // Recalculate gas exchange parameters
            gmT = CalculateParam(rateAt25.Gm, _pathway.MesophyllCO2ConductanceParams.Factor, leafTempAbsMinus25C, denominator);
            kc = CalculateParam(_pathway.RubiscoCarboxylation.At25, _pathway.RubiscoCarboxylation.Factor, leafTempAbsMinus25C, denominator);
            ko = CalculateParam(_pathway.RubiscoOxygenation.At25, _pathway.RubiscoOxygenation.Factor, leafTempAbsMinus25C, denominator);
            vcVo = CalculateParam(_pathway.RubiscoCarboxylationToOxygenation.At25, _pathway.RubiscoCarboxylationToOxygenation.Factor, leafTempAbsMinus25C, denominator);
            kp = CalculateParam(_pathway.PEPc.At25, _pathway.PEPc.Factor, leafTempAbsMinus25C, denominator);

            // Recalculate derived parameters
            UpdateElectronTransportRate();

            var koOverKc = ko / kc;
            sco = koOverKc * vcVo;
            gamma = 0.5 / sco;
            gmRd = rdT * 0.5;
        }

        /// <summary>
        /// Helper method for temperature-dependent parameter calculation.
        /// </summary>
        private static double CalculateParam(double p25, double tMin, double leafTempAbsMinus25C, double denominator)
        {
            // Compute result directly with precomputed denominator
            return p25 * Math.Exp((tMin * leafTempAbsMinus25C) / denominator);
        }

        /// <summary>
        /// Helper method for parameters with an apparent optimum in temperature response.
        /// </summary>
        private static double CalculateParamOptimum(double temp, double p25, LeafTemperatureParameters p)
        {
            double tMin = p.TMin;
            double tOpt = p.TOpt;
            double tMax = p.TMax;

            // Precompute shared values
            double tOptMinusTMin = tOpt - tMin;
            double tempMinusTMin = temp - tMin;
            double alpha = Math.Log(2) / Math.Log((tMax - tMin) / tOptMinusTMin);

            // Precompute powers for efficiency
            double tempMinusTMinAlpha = Math.Pow(tempMinusTMin, alpha);
            double tOptMinusTMinAlpha = Math.Pow(tOptMinusTMin, alpha);
            double tOptAlphaSquared = Math.Pow(tOptMinusTMin, 2 * alpha);

            // Use direct computation for numerator and denominator
            double numerator = 2 * tempMinusTMinAlpha * tOptMinusTMinAlpha - Math.Pow(tempMinusTMin, 2 * alpha);
            return p25 * Math.Pow(numerator / tOptAlphaSquared, p.Beta) / p.C;
        }

        /// <summary>
        /// Calculates the electron transport rate of the leaf.
        /// </summary>
        private void UpdateElectronTransportRate()
        {
            double photonFactor = photonCount * (1.0 - _pathway.SpectralCorrectionFactor) / 2.0;
            double sumFactor = photonFactor + jMaxT;
            double discriminant = Math.Sqrt(sumFactor * sumFactor - 4 * _canopy.CurvatureFactor * jMaxT * photonFactor);

            // Simplified formula for j
            j = (sumFactor - discriminant) / (2 * _canopy.CurvatureFactor);
        }

    }
}
