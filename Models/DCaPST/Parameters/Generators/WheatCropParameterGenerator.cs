using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// The Crop Parameter Generator for Wheat.
    /// </summary>
    public static class WheatCropParameterGenerator
    {
        /// <summary>
        /// The name of this Crop.
        /// </summary>
        public const string CROP_NAME = "wheat";

        /// <summary>
        /// psiFactor-Psi Reduction Factor 
        /// </summary>
        private const double PSI_FACTOR = 1.0;

        /// <summary>
        /// The default value used for the extra ATP cost.
        /// Constant as it is also used in other initial calculations and never changes.
        /// </summary>
        private const double DEFAULT_EXTRA_ATP_COST = 0.0;

        /// <summary>
        /// The default value used for the fraction of cyclic electron flow.
        /// Constant as it is also used in other initial calculations and never changes.
        /// </summary>
        private const double DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW = 0.25 * DEFAULT_EXTRA_ATP_COST;

        /// <summary>
        /// Handles generating a DCaPSTParameters object, constructed with the defaults for this crop type.
        /// </summary>
        /// <returns>A populated DCaPSTParameters object.</returns>
        public static DCaPSTParameters Generate()
        {
            return new DCaPSTParameters()
            {
                Rpar = 0.5,
                Canopy = GenerateCanopyParameters(),
                Pathway = GeneratePathwayParameters()
            };
        }

        /// <summary>
        /// Handles generating a CanopyParameters object, constructed with the defaults for this crop type.
        /// </summary>
        /// <returns>A populated CanopyParameters object.</returns>
        private static CanopyParameters GenerateCanopyParameters()
        {
            return new CanopyParameters()
            {
                Type = CanopyType.C3,
                AirO2 = 210000,
                LeafAngle = 60,
                LeafWidth = 0.05,
                LeafScatteringCoeff = 0.15,
                LeafScatteringCoeffNIR = 0.8,
                DiffuseExtCoeff = 0.78,
                ExtCoeffReductionSlope = 0.0288,
                ExtCoeffReductionIntercept = 0.5311,
                DiffuseExtCoeffNIR = 0.8,
                DiffuseReflectionCoeff = 0.036,
                DiffuseReflectionCoeffNIR = 0.389,
                Windspeed = 1.5,
                WindSpeedExtinction = 1.5,
                CurvatureFactor = 0.7,
                DiffusivitySolubilityRatio = 0.0,
                MinimumN = 28.6,
                SLNRatioTop = 1.3
            };
        }

        /// <summary>
        /// Handles generating a PathwayParameters object, constructed with the defaults for this crop type.
        /// </summary>
        /// <returns>A populated PathwayParameters object.</returns>
        private static PathwayParameters GeneratePathwayParameters()
        {
            return new PathwayParameters()
            {
                IntercellularToAirCO2Ratio = 0.7,
                FractionOfCyclicElectronFlow = DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW,
                RespirationSLNRatio = 0.0 * PSI_FACTOR,
                MaxRubiscoActivitySLNRatio = 1.45 * PSI_FACTOR,
                MaxElectronTransportSLNRatio = 2.4 * PSI_FACTOR,
                MaxPEPcActivitySLNRatio = 1.0 * PSI_FACTOR,
                MesophyllCO2ConductanceSLNRatio = 0.005 * PSI_FACTOR,
                MesophyllElectronTransportFraction = DEFAULT_EXTRA_ATP_COST / (3.0 + DEFAULT_EXTRA_ATP_COST),
                ATPProductionElectronTransportFactor = (3.0 - DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW) / (4.0 * (1.0 - DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW)),
                ExtraATPCost = DEFAULT_EXTRA_ATP_COST,
                RubiscoCarboxylation = new TemperatureResponseValues
                {
                    At25 = 273.422964228666,
                    Factor = 93720
                },
                RubiscoOxygenation = new TemperatureResponseValues
                {
                    At25 = 165824.064155384,
                    Factor = 33600
                },
                RubiscoCarboxylationToOxygenation = new TemperatureResponseValues
                {
                    At25 = 4.59217066521612,
                    Factor = 35713.19871277176
                },
                RubiscoActivity = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 65330
                },
                PEPc = new TemperatureResponseValues
                {
                    At25 = 0.0,
                    Factor = 0.0
                },
                PEPcActivity = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 0
                },
                Respiration = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 46390
                },
                ElectronTransportRateParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 30.0,
                    TMax = 45,
                    C = 0.911017958600129,
                    Beta = 1
                },
                MesophyllCO2ConductanceParams = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 6048.95289
                },
                SpectralCorrectionFactor = 0.15,
                PS2ActivityFraction = 0.0,
                PEPRegeneration = 0.0,
                BundleSheathConductance = 0.0
            };
        }
    }
}