namespace Models.DCAPST
{
    /// <summary>
    /// The Crop Parameter Generator for C4Maize.
    /// </summary>
    public static class C4MaizeCropParameterGenerator
    {
        /// <summary>
        /// The name of this Crop.
        /// </summary>
        public const string CROP_NAME = "c4maize";

        /// <summary>
        /// Handles generating a DCaPSTParameters object, constructed with the defaults for this crop type.
        /// </summary>
        /// <returns>A populated DCaPSTParameters object.</returns>
        public static DCaPSTParameters Generate()
        {
            return new DCaPSTParameters()
            {
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
                Type = CanopyType.C4,
                LeafAngle = 60,
                LeafWidth = 0.09,
                LeafScatteringCoeff = 0.15,
                LeafScatteringCoeffNIR = 0.8,
                DiffuseExtCoeff = 0.78,
                ExtCoeffReductionSlope = 0.0288,
                ExtCoeffReductionIntercept = 0.5311,
                DiffuseExtCoeffNIR = 0.8,
                DiffuseReflectionCoeff = 0.036,
                DiffuseReflectionCoeffNIR = 0.389,
                WindSpeedExtinction = 1.5,
                CurvatureFactor = 0.3,
                DiffusivitySolubilityRatio = 0.047,
                MinimumN = 28.6,
                SLNRatioTop = 1.00001
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
                IntercellularToAirCO2Ratio = 0.45,
                FractionOfCyclicElectronFlow = 0.5,
                RespirationSLNRatio = 0.0,
                MaxRubiscoActivitySLNRatio = 0.63,
                MaxElectronTransportSLNRatio = 4.03,
                MaxPEPcActivitySLNRatio = 1.88,
                MesophyllCO2ConductanceSLNRatio = 0.0153,
                MesophyllElectronTransportFraction = 0.4,
                ATPProductionElectronTransportFactor = 1.25,
                ExtraATPCost = 2.0,
                RubiscoCarboxylation = new TemperatureResponseValues
                {
                    At25 = 1210,
                    Factor = 64200
                },
                RubiscoOxygenation = new TemperatureResponseValues
                {
                    At25 = 292000,
                    Factor = 10500
                },
                RubiscoCarboxylationToOxygenation = new TemperatureResponseValues
                {
                    At25 = 5.51328906454566,
                    Factor = 21265.4029552906
                },
                RubiscoActivity = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 78000
                },
                PEPc = new TemperatureResponseValues
                {
                    At25 = 75,
                    Factor = 36300
                },
                PEPcActivity = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 57043.2677590512
                },
                Respiration = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 46390
                },
                RubiscoActivityParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 37.719,
                    TMax = 50,
                    C = 0.701541,
                    Beta = 0.683
                },
                PEPcActivityParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 45.272,
                    TMax = 50,
                    C = 0.49215,
                    Beta = 0.205
                },
                ElectronTransportRateParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 37.16,
                    TMax = 50,
                    C = 0.674383,
                    Beta = 0.869
                },
                RespirationParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 40.085,
                    TMax = 50,
                    C = 0.341793,
                    Beta = 1.182
                },
                EpsilonParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 35.311,
                    TMax = 50,
                    C = 0.870928,
                    Beta = 0.486
                },
                MesophyllCO2ConductanceParams = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 40600
                },
                Epsilon = new TemperatureResponseValues
                {
                    At25 = 0.2,
                    Factor = 0.3
                },
                SpectralCorrectionFactor = 0.39609236234459,
                PS2ActivityFraction = 0.1,
                PEPRegeneration = 1000,
                BundleSheathConductance = 0.003
            };
        }
    }
}
