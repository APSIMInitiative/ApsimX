using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// The Crop Parameter Generator for Sorghum.
    /// </summary>
    public static class SorghumCropParameterGenerator
    {
        /// <summary>
        /// The name of this Crop.
        /// </summary>
        public const string CROP_NAME = "sorghum";

        /// <summary>
        /// psiFactor-Psi Reduction Factor 
        /// </summary>
        private const double PSI_FACTOR = 0.4;

        /// <summary>
        /// The default value used for the extra ATP cost.
        /// Constant as it is also used in other initial calculations and never changes.
        /// </summary>
        private const double DEFAULT_EXTRA_ATP_COST = 2;

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
                Type = CanopyType.C4,
                AirO2 = 210000,
                AirCO2 = 363,
                LeafAngle = 60,
                LeafWidth = 0.15,
                LeafScatteringCoeff = 0.15,
                LeafScatteringCoeffNIR = 0.8,
                DiffuseExtCoeff = 0.78,
                DiffuseExtCoeffNIR = 0.8,
                DiffuseReflectionCoeff = 0.036,
                DiffuseReflectionCoeffNIR = 0.389,
                Windspeed = 1.5,
                WindSpeedExtinction = 1.5,
                CurvatureFactor = 0.7,
                DiffusivitySolubilityRatio = 0.047,
                MinimumN = 14,
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
                IntercellularToAirCO2Ratio = 0.45,
                FractionOfCyclicElectronFlow = DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW,
                RespirationSLNRatio = 0.0 * PSI_FACTOR,
                MaxRubiscoActivitySLNRatio = 0.465 * PSI_FACTOR,
                MaxElectronTransportSLNRatio = 2.7 * PSI_FACTOR,
                MaxPEPcActivitySLNRatio = 1.55 * PSI_FACTOR,
                MesophyllCO2ConductanceSLNRatio = 0.0135 * PSI_FACTOR,
                MesophyllElectronTransportFraction = DEFAULT_EXTRA_ATP_COST / (3.0 + DEFAULT_EXTRA_ATP_COST),
                ATPProductionElectronTransportFactor = (3.0 - DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW) / (4.0 * (1.0 - DEFAULT_FRACTION_OF_CYCLIC_ELECTRON_FLOW)),
                ExtraATPCost = DEFAULT_EXTRA_ATP_COST,
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
                ElectronTransportRateParams = new LeafTemperatureParameters
                {
                    TMin = 0,
                    TOpt = 37.8649150880407,
                    TMax = 55,
                    C = 0.711229539802063,
                    Beta = 1
                },
                MesophyllCO2ConductanceParams = new TemperatureResponseValues
                {
                    At25 = 0,
                    Factor = 40600
                },
                SpectralCorrectionFactor = 0.15,
                PS2ActivityFraction = 0.1,
                PEPRegeneration = 120,
                BundleSheathConductance = 0.003
            };
        }
    }
}
