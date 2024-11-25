using Models.DCAPST;
using NUnit.Framework;
using System;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class CropParameterGeneratorTests
    {
        #region Tests

        [TestCase(SorghumCropParameterGenerator.CROP_NAME)]
        [TestCase(WheatCropParameterGenerator.CROP_NAME)]
        public void Generate_KnownCropDefaultModifiers_ReturnsValue(string cropName)
        {
            // Arrange
            var cropParameterGenerator = new CropParameterGenerator();

            // Act
            var cropParams = cropParameterGenerator.Generate(cropName);

            // Assert
            Assert.That(cropParams, Is.Not.Null);
        }

        [TestCase(SorghumCropParameterGenerator.CROP_NAME)]
        [TestCase(WheatCropParameterGenerator.CROP_NAME)]
        public void Generate_KnownCropNonDefaultModifiers_ReturnsValue(string cropName)
        {
            // Arrange
            var rubiscoLimitedModifier = 1.2;
            var electronTransportLimitedModifier = 0.8;
            var cropParameterGenerator = new CropParameterGenerator();

            // Act
            var cropParams = cropParameterGenerator.Generate(cropName);
            var defaultMaxRubiscoActivitySLNRatio = cropParams.Pathway.MaxRubiscoActivitySLNRatio;
            var defaultMaxPEPcActivitySLNRatio = cropParams.Pathway.MaxPEPcActivitySLNRatio;
            var defaultMesophyllCO2ConductanceSLNRatio = cropParams.Pathway.MesophyllCO2ConductanceSLNRatio;
            var defaultMaxElectronTransportSLNRatio = cropParams.Pathway.MaxElectronTransportSLNRatio;
            var defaultSpectralCorrectionFactor = cropParams.Pathway.SpectralCorrectionFactor;
            cropParameterGenerator.ApplyRubiscoLimitedModifier(cropName, cropParams, rubiscoLimitedModifier);
            cropParameterGenerator.ApplyElectronTransportLimitedModifier(cropName, cropParams, electronTransportLimitedModifier);

            // Assert
            var expectedModifiedMaxRubiscoActivitySLNRatio = defaultMaxRubiscoActivitySLNRatio * rubiscoLimitedModifier;
            var expectedModifiedMaxPEPcActivitySLNRatio = defaultMaxPEPcActivitySLNRatio * rubiscoLimitedModifier;
            var expectedModifiedMesophyllCO2ConductanceSLNRatio = defaultMesophyllCO2ConductanceSLNRatio * rubiscoLimitedModifier;
            var expectedModifiedMaxElectronTransportSLNRatio = defaultMaxElectronTransportSLNRatio * electronTransportLimitedModifier;
            var expectedModifiedSpectralCorrectionFactor = 
                1 + (electronTransportLimitedModifier * defaultSpectralCorrectionFactor) -
                electronTransportLimitedModifier;

            // 0.000001
            double tolerance = Math.Pow(10, -6);
            Assert.That(cropParams.Pathway.MaxRubiscoActivitySLNRatio, Is.EqualTo(expectedModifiedMaxRubiscoActivitySLNRatio).Within(tolerance));
            Assert.That(cropParams.Pathway.MaxPEPcActivitySLNRatio, Is.EqualTo(expectedModifiedMaxPEPcActivitySLNRatio).Within(tolerance));
            Assert.That(cropParams.Pathway.MesophyllCO2ConductanceSLNRatio, Is.EqualTo(expectedModifiedMesophyllCO2ConductanceSLNRatio).Within(tolerance));
            Assert.That(cropParams.Pathway.MaxElectronTransportSLNRatio, Is.EqualTo(expectedModifiedMaxElectronTransportSLNRatio).Within(tolerance));
            Assert.That(cropParams.Pathway.SpectralCorrectionFactor, Is.EqualTo(expectedModifiedSpectralCorrectionFactor).Within(tolerance));
        }

        [TestCase("InvalidCropName")]
        public void Generate_UnknownCrop_ReturnsNull(string cropName)
        {
            // Arrange
            var cropParameterGenerator = new CropParameterGenerator();

            // Act
            var cropParams = cropParameterGenerator.Generate(cropName);

            // Assert
            Assert.That(cropParams, Is.Null);
        }

        #endregion
    }
}
