using Models.DCAPST.Interfaces;
using System;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// Models a complete canopy
    /// </summary>
    public class CanopyAttributes : ICanopyAttributes
    {
        /// <summary>
        /// The part of the canopy in sunlight
        /// </summary>
        public IAssimilationArea Sunlit { get; private set; }

        /// <summary>
        /// The part of the canopy in shade
        /// </summary>
        public IAssimilationArea Shaded { get; private set; }

        /// <summary>
        /// The number of layers in the canopy
        /// </summary>
        public int Layers { get; set; } = 1;

        private const double KG_TO_G = 1000.0;
        private const double MOLAR_MASS_NITROGEN = 14.0;

        private readonly CanopyParameters _canopyParameters;
        private readonly PathwayParameters _pathwayParameters;

        private double _nitrogenAllocation;
        private double _lai;
        private CanopyRadiation _absorbed;
        private double _leafNTopCanopy;

        /// <summary>
        /// Constructor
        /// </summary>
        public CanopyAttributes(
            CanopyParameters canopyParameters,
            PathwayParameters pathwayParameters,
            IAssimilationArea sunlit,
            IAssimilationArea shaded
        )
        {
            _canopyParameters = canopyParameters;
            _pathwayParameters = pathwayParameters;
            Sunlit = sunlit;
            Shaded = shaded;
        }

        /// <summary>This will return the reduced extinction coeffecient</summary>
        private double GetReducedExtinctionCoeffecient(double extinctionCoeffecient)
        {
            var extinctionCoeffecientReduction =
                extinctionCoeffecient * ((_canopyParameters.ExtCoeffReductionSlope * _lai) + _canopyParameters.ExtCoeffReductionIntercept);

            // Cap the value to ensure that if the _lai was really high it doesn't return a bigger 
            // value than the original Coefficient/K value.
            var cappedExtinctionCoeffecientReduction = Math.Min(extinctionCoeffecient, extinctionCoeffecientReduction);

            return cappedExtinctionCoeffecientReduction;
        }

        /// <summary>
        /// Establishes the initial conditions for the daily photosynthesis calculation
        /// </summary>
        public void InitialiseDay(double lai, double sln)
        {
            _lai = lai;
            var NcAv = sln * KG_TO_G / MOLAR_MASS_NITROGEN;
            _leafNTopCanopy = _canopyParameters.SLNRatioTop * NcAv;

            _nitrogenAllocation = -1 * Math.Log((NcAv - _canopyParameters.MinimumN) / (_leafNTopCanopy - _canopyParameters.MinimumN)) * 2;

            _absorbed = new CanopyRadiation(Layers, _lai)
            {
                DiffuseExtinction = GetReducedExtinctionCoeffecient(_canopyParameters.DiffuseExtCoeff),
                LeafScattering = _canopyParameters.LeafScatteringCoeff,
                DiffuseReflection = _canopyParameters.DiffuseReflectionCoeff
            };
        }

        /// <summary>
        /// Recalculates canopy parameters for a new time step
        /// </summary>
        public void DoTimestepAdjustment(ISolarRadiation radiation)
        {
            CalcLAI();
            CalcAbsorbedRadiations(radiation);
            CalcMaximumRates();
        }

        /// <summary>
        /// Find the total heat conductance across the boundary of the canopy
        /// </summary>
        public double CalcBoundaryHeatConductance()
        {
            var a = 0.5 * _canopyParameters.Windspeed;
            var b = 0.01 * Math.Pow(_canopyParameters.Windspeed / _canopyParameters.LeafWidth, 0.5);
            var c = 1 - Math.Exp(-a * _lai);

            return b * c / a;
        }

        /// <summary>
        /// Find the heat conductance across the boundary of the sunlit area of the canopy
        /// </summary>
        public double CalcSunlitBoundaryHeatConductance()
        {
            var a = 0.5 * _canopyParameters.Windspeed + _absorbed.DirectExtinction;
            var b = 0.01 * Math.Pow(_canopyParameters.Windspeed / _canopyParameters.LeafWidth, 0.5);
            var c = 1 - Math.Exp(-a * _lai);

            return b * c / a;
        }

        /// <summary>
        /// Calculates how the movement of the sun affects the absorbed radiation
        /// </summary>
        public void DoSolarAdjustment(double sunAngle)
        {
            // Beam Extinction Coefficient
            if (sunAngle > 0)
            {
                var rawShadowProjection = CalcShadowProjection(sunAngle) / Math.Sin(sunAngle);
                _absorbed.DirectExtinction = GetReducedExtinctionCoeffecient(rawShadowProjection);
            }
            else
            {
                // Night time.
                _absorbed.DirectExtinction = 0;
            }
        }

        /// <summary>
        /// Calculates the radiation intercepted by the current layer of the canopy
        /// </summary>
        public double GetInterceptedRadiation()
        {
            // Intercepted radiation
            return _absorbed.CalcInterceptedRadiation();
        }

        /// <summary>
        /// Models a maximum rate calculation
        /// </summary>
        private double CalcMaximumRate(double psi, double coefficient)
        {
            var factor = _lai * (_leafNTopCanopy - _canopyParameters.MinimumN) * psi;
            var exp = _absorbed.CalcExp(coefficient / _lai);

            return factor * exp / coefficient;
        }

        /// <summary>
        /// Calculates the _lai for the sunlit/shaded areas of the canopy, based on the position of the sun
        /// </summary>
        private void CalcLAI()
        {
            Sunlit.LAI = _absorbed.CalculateSunlitLAI();
            Shaded.LAI = _lai - Sunlit.LAI;
        }

        /// <summary>
        /// Calculates the radiation absorbed by the canopy, based on the position of the sun
        /// </summary>
        private void CalcAbsorbedRadiations(ISolarRadiation radiation)
        {
            // Set parameters
            _absorbed.DiffuseExtinction = GetReducedExtinctionCoeffecient(_canopyParameters.DiffuseExtCoeff);
            _absorbed.LeafScattering = _canopyParameters.LeafScatteringCoeff;
            _absorbed.DiffuseReflection = _canopyParameters.DiffuseReflectionCoeff;

            // Photon calculations (used by photosynthesis)
            var photons = _absorbed.CalcTotalRadiation(radiation.DirectPAR, radiation.DiffusePAR);
            Sunlit.PhotonCount = _absorbed.CalcSunlitRadiation(radiation.DirectPAR, radiation.DiffusePAR);
            Shaded.PhotonCount = photons - Sunlit.PhotonCount;

            // Energy calculations (used by water interaction)
            var PARDirect = radiation.Direct * 0.5 * 1000000;
            var PARDiffuse = radiation.Diffuse * 0.5 * 1000000;
            var NIRDirect = radiation.Direct * 0.5 * 1000000;
            var NIRDiffuse = radiation.Diffuse * 0.5 * 1000000;

            var PARTotalIrradiance = _absorbed.CalcTotalRadiation(PARDirect, PARDiffuse);
            var SunlitPARTotalIrradiance = _absorbed.CalcSunlitRadiation(PARDirect, PARDiffuse);
            var ShadedPARTotalIrradiance = PARTotalIrradiance - SunlitPARTotalIrradiance;

            // Adjust parameters for NIR calculations
            _absorbed.DiffuseExtinction = GetReducedExtinctionCoeffecient(_canopyParameters.DiffuseExtCoeffNIR);
            _absorbed.LeafScattering = _canopyParameters.LeafScatteringCoeffNIR;
            _absorbed.DiffuseReflection = _canopyParameters.DiffuseReflectionCoeffNIR;

            var NIRTotalIrradiance = _absorbed.CalcTotalRadiation(NIRDirect, NIRDiffuse);
            var SunlitNIRTotalIrradiance = _absorbed.CalcSunlitRadiation(NIRDirect, NIRDiffuse);
            var ShadedNIRTotalIrradiance = NIRTotalIrradiance - SunlitNIRTotalIrradiance;

            Sunlit.AbsorbedRadiation = SunlitPARTotalIrradiance + SunlitNIRTotalIrradiance;
            Shaded.AbsorbedRadiation = ShadedPARTotalIrradiance + ShadedNIRTotalIrradiance;
        }

        /// <summary>
        /// Calculates properties of the canopy, based on how much of the canopy is currently in direct sunlight
        /// </summary>
        private void CalcMaximumRates()
        {
            var coefficient = _nitrogenAllocation;
            var sunlitCoefficient = _nitrogenAllocation + (_absorbed.DirectExtinction * _lai);

            var rubiscoActivity25 = CalcMaximumRate(_pathwayParameters.MaxRubiscoActivitySLNRatio, coefficient);
            Sunlit.At25C.VcMax = CalcMaximumRate(_pathwayParameters.MaxRubiscoActivitySLNRatio, sunlitCoefficient);
            Shaded.At25C.VcMax = rubiscoActivity25 - Sunlit.At25C.VcMax;

            var rd25 = CalcMaximumRate(_pathwayParameters.RespirationSLNRatio, coefficient);
            Sunlit.At25C.Rd = CalcMaximumRate(_pathwayParameters.RespirationSLNRatio, sunlitCoefficient);
            Shaded.At25C.Rd = rd25 - Sunlit.At25C.Rd;

            var jMax25 = CalcMaximumRate(_pathwayParameters.MaxElectronTransportSLNRatio, coefficient);
            Sunlit.At25C.JMax = CalcMaximumRate(_pathwayParameters.MaxElectronTransportSLNRatio, sunlitCoefficient);
            Shaded.At25C.JMax = jMax25 - Sunlit.At25C.JMax;

            var pepcActivity25 = CalcMaximumRate(_pathwayParameters.MaxPEPcActivitySLNRatio, coefficient);
            Sunlit.At25C.VpMax = CalcMaximumRate(_pathwayParameters.MaxPEPcActivitySLNRatio, sunlitCoefficient);
            Shaded.At25C.VpMax = pepcActivity25 - Sunlit.At25C.VpMax;

            var mesophyllCO2Conductance25 = CalcMaximumRate(_pathwayParameters.MesophyllCO2ConductanceSLNRatio, coefficient);
            Sunlit.At25C.Gm = CalcMaximumRate(_pathwayParameters.MesophyllCO2ConductanceSLNRatio, sunlitCoefficient);
            Shaded.At25C.Gm = mesophyllCO2Conductance25 - Sunlit.At25C.Gm;
        }

        /// <summary>
        /// Calculates the geometry of the shadows across the canopy
        /// </summary>
        private double CalcShadowProjection(double sunAngle)
        {
            if (_canopyParameters.LeafAngle.ToRadians() <= sunAngle)
            {
                return Math.Cos(_canopyParameters.LeafAngle.ToRadians()) * Math.Sin(sunAngle);
            }
            else
            {
                double theta = Math.Acos(1 / Math.Tan(_canopyParameters.LeafAngle.ToRadians()) * Math.Tan(sunAngle));

                var a = 2 / Math.PI * Math.Sin(_canopyParameters.LeafAngle.ToRadians()) * Math.Cos(sunAngle) * Math.Sin(theta);
                var b = (1 - theta * 2 / Math.PI) * Math.Cos(_canopyParameters.LeafAngle.ToRadians()) * Math.Sin(sunAngle);
                return a + b;
            }
        }
    }
}
