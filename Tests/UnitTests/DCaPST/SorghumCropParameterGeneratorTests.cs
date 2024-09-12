using Models.DCAPST;
using Models.DCAPST.Interfaces;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class SorghumCropParameterGeneratorTests
    {
        #region TestHelpers

        /// <summary>
        /// This will create the DCaPSTParameters as per the classic code so that we 
        /// can compare it to our defaults.
        /// </summary>
        /// <returns></returns>
        private static DCaPSTParameters CreateClassicSorghumDcapstParameters()
        {
            var classicCanopy = ClassicDCaPSTDefaultDataSetup.SetUpCanopy(
                CanopyType.C4, // Canopy type
                363, // CO2 partial pressure
                0.675, // Curvature factor
                0.047, // Diffusivity-solubility ratio
                210000, // O2 partial pressure
                0.78, // Diffuse extinction coefficient
                0.8, // Diffuse extinction coefficient NIR
                0.036, // Diffuse reflection coefficient
                0.389, // Diffuse reflection coefficient NIR
                60, // Leaf angle
                0.15, // Leaf scattering coefficient
                0.8, // Leaf scattering coefficient NIR
                0.09, // Leaf width
                1.3, // SLN ratio at canopy top
                28.6, // Minimum Nitrogen
                1.5, // Wind speed
                1.5 // Wind speed extinction
            );

            var classicPathway = ClassicDCaPSTDefaultDataSetup.SetUpPathway(
                0, // jTMin
                37.8649150880407, // jTOpt
                55, // jTMax
                0.711229539802063, // jC
                1, // jBeta
                //         0, // gTMin
                //         42, // gTOpt
                //         55, // gTMax
                //         0.462820450976839, // gC
                //         1, // gBeta
                40600, // gmFactor
                1210, // KcAt25
                64200, // KcFactor
                292000, // KoAt25
                10500, // KoFactor
                5.51328906454566, // VcVoAt25
                21265.4029552906, // VcVoFactor
                75, // KpAt25
                36300, // KpFactor
                78000, // VcFactor
                46390, // RdFactor
                57043.2677590512, // VpFactor
                1000, // pepRegeneration
                0.39609236234459, // spectralCorrectionFactor
                0.1, // ps2ActivityFraction
                0.003, // bundleSheathConductance
                0.349, // maxRubiscoActivitySLNRatio
                3.0, // maxElectronTransportSLNRatio  2.7 * PsiFactor or 1.9
                0.0, // respirationSLNRatio
                1.165, // maxPEPcActivitySLNRatio
                0.011, // mesophyllCO2ConductanceSLNRatio
                2, // extraATPCost
                0.45 // intercellularToAirCO2Ratio
            );

            return new DCaPSTParameters
            {
                Rpar = 0.5,
                Canopy = classicCanopy,
                Pathway = classicPathway
            };
        }

        #endregion

        #region Tests

        [Test]
        public void Generate_ReturnsDefaultValues()
        {
            // Arrange
            var classicSorghumParams = CreateClassicSorghumDcapstParameters();

            // Act
            var nextGenSorghumParams = SorghumCropParameterGenerator.Generate();

            // Add in any new ones that didn't exist in Classic.
            classicSorghumParams.Canopy.ExtCoeffReductionIntercept = nextGenSorghumParams.Canopy.ExtCoeffReductionIntercept;
            classicSorghumParams.Canopy.ExtCoeffReductionSlope = nextGenSorghumParams.Canopy.ExtCoeffReductionSlope;

            // Assert
            DCaPSTParametersComparer.AssertDCaPSTParametersValuesEqual(classicSorghumParams, nextGenSorghumParams);
        }

        #endregion
    }
}
