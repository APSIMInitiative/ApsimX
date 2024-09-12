using Models.DCAPST;
using Models.DCAPST.Interfaces;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class WheatCropParameterGeneratorTests
    {
        #region TestHelpers

        /// <summary>
        /// This will create the DCaPSTParameters as per the classic code so that we 
        /// can compare it to our defaults.
        /// </summary>
        /// <returns></returns>
        private static DCaPSTParameters CreateClassicWheatDcapstParameters()
        {
            const double PSI_FACTOR = 1.0;

            var classicCanopy = ClassicDCaPSTDefaultDataSetup.SetUpCanopy(
                CanopyType.C3, // Canopy type
                370, // CO2 partial pressure
                0.7, // Empirical curvature factor
                0.00000, // Diffusivity-solubility ratio
                210000, // O2 partial pressure
                0.78, // PAR diffuse extinction coefficient
                0.8, // NIR diffuse extinction coefficient
                0.036, // PAR diffuse reflection coefficient
                0.389, // NIR diffuse reflection coefficient
                60, // Leaf angle
                0.15, // PAR leaf scattering coefficient
                0.8, // NIR leaf scattering coefficient
                0.05, // Leaf width
                1.3, // SLN ratio at canopy top
                28.6, // Minimum structural nitrogen
                1.5, // Wind speed
                1.5 // Wind speed profile distribution coefficient
            );

            var classicPathway = ClassicDCaPSTDefaultDataSetup.SetUpPathway(
                0, // Electron transport minimum temperature
                30.0, // Electron transport optimum temperature
                45.0, // Electron transport maximum temperature
                0.911017958600129, // Electron transport scaling constant
                1, // Electron transport Beta value

                //         0, // Mesophyll conductance minimum temperature
                //         29.2338417788683, // Mesophyll conductance optimum temperature
                //         45, // Mesophyll conductance maximum temperature
                //         0.875790608584141, // Mesophyll conductance scaling constant
                //         1, // Mesophyll conductance Beta value

                6048.95289, //mesophyll conductance factor

                273.422964228666, // Kc25 - Michaelis Menten constant of Rubisco carboxylation at 25 degrees C
                93720, // KcFactor

                165824.064155384, // Ko25 - Michaelis Menten constant of Rubisco oxygenation at 25 degrees C
                33600, // KoFactor

                4.59217066521612, // VcVo25 - Rubisco carboxylation to oxygenation at 25 degrees C
                35713.19871277176, // VcVoFactor

                0.00000, // Kp25 - Michaelis Menten constant of PEPc activity at 25 degrees C (Unused in C3)
                0.00000, // KpFactor (Unused in C3)

                65330, // VcFactor
                46390, // RdFactor
                0.00000, // VpFactor

                0.00000, // PEPc regeneration (Unused in C3)
                0.15, // Spectral correction factor
                0.00000, // Photosystem II activity fraction
                0.00000, // Bundle sheath CO2 conductance
                1.45 * PSI_FACTOR, // Max Rubisco activity to SLN ratio
                2.4 * PSI_FACTOR, // Max electron transport to SLN ratio
                0.0 * PSI_FACTOR, // Respiration to SLN ratio
                1.0 * PSI_FACTOR, // Max PEPc activity to SLN ratio
                0.005 * PSI_FACTOR, // Mesophyll CO2 conductance to SLN ratio
                0.00000, // Extra ATP cost
                0.7 // Intercellular CO2 to air CO2 ratio
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
            var classicWheatParams = CreateClassicWheatDcapstParameters();

            // Act
            var nextGenWheatParams = WheatCropParameterGenerator.Generate();
            // Add in any new ones that didn't exist in Classic.
            classicWheatParams.Canopy.ExtCoeffReductionIntercept = nextGenWheatParams.Canopy.ExtCoeffReductionIntercept;
            classicWheatParams.Canopy.ExtCoeffReductionSlope = nextGenWheatParams.Canopy.ExtCoeffReductionSlope;

            // Assert
            DCaPSTParametersComparer.AssertDCaPSTParametersValuesEqual(classicWheatParams, nextGenWheatParams);
        }

        #endregion
    }
}
