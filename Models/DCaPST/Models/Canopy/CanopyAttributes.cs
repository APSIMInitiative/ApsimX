using System;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Canopy
{
    /// <summary>
    /// Models a complete canopy
    /// </summary>
    public class CanopyAttributes : ICanopyAttributes
    {
        /// <summary>
        /// The initial parameters of the canopy
        /// </summary>
        public ICanopyParameters Canopy { get; set; }

        /// <summary>
        /// The pathway parameters
        /// </summary>
        private IPathwayParameters pathway;

        /// <summary>
        /// The part of the canopy in sunlight
        /// </summary>
        public IAssimilationArea Sunlit { get; private set; }

        /// <summary>
        /// The part of the canopy in shade
        /// </summary>
        public IAssimilationArea Shaded { get; private set; }

        /// <summary>
        /// Models radiation absorbed by the canopy
        /// </summary>
        private CanopyRadiation Absorbed { get; set; }

        /// <summary>
        /// Leaf area index of the canopy
        /// </summary>
        private double LAI { get; set; }

        /// <summary>
        /// The leaf angle (radians)
        /// </summary>
        private double LeafAngle { get; set; }

        /// <summary>
        /// The width of the leaf
        /// </summary>
        private double LeafWidth { get; set; }

        /// <summary>
        /// Nitrogen at the top of the canopy
        /// </summary>
        private double LeafNTopCanopy { get; set; }

        /// <summary>
        /// Wind speed
        /// </summary>
        private double WindSpeed { get; set; }

        /// <summary>
        /// Wind speed extinction
        /// </summary>
        private double WindSpeedExtinction { get; set; }

        /// <summary>
        /// Coefficient of nitrogen allocation through the canopy
        /// </summary>
        private double NAllocation { get; set; }

        /// <summary>
        /// The number of layers in the canopy
        /// </summary>
        public int Layers { get; set; } = 1;

        /// <summary>This will return the reduced extinction coeffecient</summary>
        private double GetReducedExtinctionCoeffecient(double extinctionCoeffecient)
        {
            var extinctionCoeffecientReduction =
                extinctionCoeffecient * ((Canopy.ExtCoeffReductionSlope * LAI) + Canopy.ExtCoeffReductionIntercept);

            // Cap the value to ensure that if the LAI was really high it doesn't return a bigger 
            // value than the original Coefficient/K value.
            var cappedExtinctionCoeffecientReduction = Math.Min(extinctionCoeffecient, extinctionCoeffecientReduction);

            return cappedExtinctionCoeffecientReduction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="canopy"></param>
        /// <param name="pathway"></param>
        /// <param name="sunlit"></param>
        /// <param name="shaded"></param>
        public CanopyAttributes(
            ICanopyParameters canopy,
            IPathwayParameters pathway,
            IAssimilationArea sunlit,
            IAssimilationArea shaded
        )
        {
            Canopy = canopy;
            this.pathway = pathway;
            Sunlit = sunlit;
            Shaded = shaded;
        }

        /// <summary>
        /// Establishes the initial conditions for the daily photosynthesis calculation
        /// </summary>
        public void InitialiseDay(double lai, double sln)
        {
            WindSpeed = Canopy.Windspeed;
            WindSpeedExtinction = Canopy.WindSpeedExtinction;
            LeafAngle = Canopy.LeafAngle.ToRadians();
            LeafWidth = Canopy.LeafWidth;

            LAI = lai;

            var kg_to_g = 1000;
            var molarMassNitrogen = 14;

            var NcAv = sln * kg_to_g / molarMassNitrogen;
            LeafNTopCanopy = Canopy.SLNRatioTop * NcAv;

            NAllocation = -1 * Math.Log((NcAv - Canopy.MinimumN) / (LeafNTopCanopy - Canopy.MinimumN)) * 2;

            Absorbed = new CanopyRadiation(Layers, LAI)
            {
                DiffuseExtinction = GetReducedExtinctionCoeffecient(Canopy.DiffuseExtCoeff),
                LeafScattering = Canopy.LeafScatteringCoeff,
                DiffuseReflection = Canopy.DiffuseReflectionCoeff
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
        /// Calculates the LAI for the sunlit/shaded areas of the canopy, based on the position of the sun
        /// </summary>
        private void CalcLAI()
        {
            Sunlit.LAI = Absorbed.CalculateSunlitLAI();
            Shaded.LAI = LAI - Sunlit.LAI;
        }

        /// <summary>
        /// Calculates the radiation absorbed by the canopy, based on the position of the sun
        /// </summary>
        private void CalcAbsorbedRadiations(ISolarRadiation radiation)
        {
            // Set parameters
            Absorbed.DiffuseExtinction = GetReducedExtinctionCoeffecient(Canopy.DiffuseExtCoeff);
            Absorbed.LeafScattering = Canopy.LeafScatteringCoeff;
            Absorbed.DiffuseReflection = Canopy.DiffuseReflectionCoeff;

            // Photon calculations (used by photosynthesis)
            var photons = Absorbed.CalcTotalRadiation(radiation.DirectPAR, radiation.DiffusePAR);
            Sunlit.PhotonCount = Absorbed.CalcSunlitRadiation(radiation.DirectPAR, radiation.DiffusePAR);
            Shaded.PhotonCount = photons - Sunlit.PhotonCount;

            // Energy calculations (used by water interaction)
            var PARDirect = radiation.Direct * 0.5 * 1000000;
            var PARDiffuse = radiation.Diffuse * 0.5 * 1000000;
            var NIRDirect = radiation.Direct * 0.5 * 1000000;
            var NIRDiffuse = radiation.Diffuse * 0.5 * 1000000;

            var PARTotalIrradiance = Absorbed.CalcTotalRadiation(PARDirect, PARDiffuse);
            var SunlitPARTotalIrradiance = Absorbed.CalcSunlitRadiation(PARDirect, PARDiffuse);
            var ShadedPARTotalIrradiance = PARTotalIrradiance - SunlitPARTotalIrradiance;

            // Adjust parameters for NIR calculations
            Absorbed.DiffuseExtinction = GetReducedExtinctionCoeffecient(Canopy.DiffuseExtCoeffNIR);
            Absorbed.LeafScattering = Canopy.LeafScatteringCoeffNIR;
            Absorbed.DiffuseReflection = Canopy.DiffuseReflectionCoeffNIR;

            var NIRTotalIrradiance = Absorbed.CalcTotalRadiation(NIRDirect, NIRDiffuse);
            var SunlitNIRTotalIrradiance = Absorbed.CalcSunlitRadiation(NIRDirect, NIRDiffuse);
            var ShadedNIRTotalIrradiance = NIRTotalIrradiance - SunlitNIRTotalIrradiance;

            Sunlit.AbsorbedRadiation = SunlitPARTotalIrradiance + SunlitNIRTotalIrradiance;
            Shaded.AbsorbedRadiation = ShadedPARTotalIrradiance + ShadedNIRTotalIrradiance;
        }

        /// <summary>
        /// Calculates properties of the canopy, based on how much of the canopy is currently in direct sunlight
        /// </summary>
        private void CalcMaximumRates()
        {
            var coefficient = NAllocation;
            var sunlitCoefficient = NAllocation + (Absorbed.DirectExtinction * LAI);

            var RubiscoActivity25 = CalcMaximumRate(pathway.MaxRubiscoActivitySLNRatio, coefficient);
            Sunlit.At25C.VcMax = CalcMaximumRate(pathway.MaxRubiscoActivitySLNRatio, sunlitCoefficient);
            Shaded.At25C.VcMax = RubiscoActivity25 - Sunlit.At25C.VcMax;

            var Rd25 = CalcMaximumRate(pathway.RespirationSLNRatio, coefficient);
            Sunlit.At25C.Rd = CalcMaximumRate(pathway.RespirationSLNRatio, sunlitCoefficient);
            Shaded.At25C.Rd = Rd25 - Sunlit.At25C.Rd;

            var JMax25 = CalcMaximumRate(pathway.MaxElectronTransportSLNRatio, coefficient);
            Sunlit.At25C.JMax = CalcMaximumRate(pathway.MaxElectronTransportSLNRatio, sunlitCoefficient);
            Shaded.At25C.JMax = JMax25 - Sunlit.At25C.JMax;

            var PEPcActivity25 = CalcMaximumRate(pathway.MaxPEPcActivitySLNRatio, coefficient);
            Sunlit.At25C.VpMax = CalcMaximumRate(pathway.MaxPEPcActivitySLNRatio, sunlitCoefficient);
            Shaded.At25C.VpMax = PEPcActivity25 - Sunlit.At25C.VpMax;

            var MesophyllCO2Conductance25 = CalcMaximumRate(pathway.MesophyllCO2ConductanceSLNRatio, coefficient);
            Sunlit.At25C.Gm = CalcMaximumRate(pathway.MesophyllCO2ConductanceSLNRatio, sunlitCoefficient);
            Shaded.At25C.Gm = MesophyllCO2Conductance25 - Sunlit.At25C.Gm;
        }

        /// <summary>
        /// Models a maximum rate calculation
        /// </summary>
        private double CalcMaximumRate(double psi, double coefficient)
        {
            var factor = LAI * (LeafNTopCanopy - Canopy.MinimumN) * psi;
            var exp = Absorbed.CalcExp(coefficient / LAI);

            return factor * exp / coefficient;
        }

        /// <summary>
        /// Find the total heat conductance across the boundary of the canopy
        /// </summary>
        public double CalcBoundaryHeatConductance()
        {
            var a = 0.5 * WindSpeedExtinction;
            var b = 0.01 * Math.Pow(WindSpeed / LeafWidth, 0.5);
            var c = 1 - Math.Exp(-a * LAI);

            return b * c / a;
        }

        /// <summary>
        /// Find the heat conductance across the boundary of the sunlit area of the canopy
        /// </summary>
        public double CalcSunlitBoundaryHeatConductance()
        {
            var a = 0.5 * WindSpeedExtinction + Absorbed.DirectExtinction;
            var b = 0.01 * Math.Pow(WindSpeed / LeafWidth, 0.5);
            var c = 1 - Math.Exp(-a * LAI);

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
                Absorbed.DirectExtinction = GetReducedExtinctionCoeffecient(rawShadowProjection);
            }
            else
            {
                // Night time.
                Absorbed.DirectExtinction = 0;
            }
        }

        /// <summary>
        /// Calculates the radiation intercepted by the current layer of the canopy
        /// </summary>
        public double GetInterceptedRadiation()
        {
            // Intercepted radiation
            return Absorbed.CalcInterceptedRadiation();

            // TODO: Make this work with multiple layers 
            // (by subtracting the accumulated intercepted radiation of the previous layers) e.g:
            // InterceptedRadiation_1 = Absorbed.CalcInterceptedRadiation() - InterceptedRadiation_0;
        }

        /// <summary>
        /// Calculates the geometry of the shadows across the canopy
        /// </summary>
        private double CalcShadowProjection(double sunAngle)
        {
            if (LeafAngle <= sunAngle)
            {
                return Math.Cos(LeafAngle) * Math.Sin(sunAngle);
            }
            else
            {
                double theta = Math.Acos(1 / Math.Tan(LeafAngle) * Math.Tan(sunAngle));

                var a = 2 / Math.PI * Math.Sin(LeafAngle) * Math.Cos(sunAngle) * Math.Sin(theta);
                var b = (1 - theta * 2 / Math.PI) * Math.Cos(LeafAngle) * Math.Sin(sunAngle);
                return a + b;
            }
        }
    }
}
