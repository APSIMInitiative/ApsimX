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
            var rubiscoLimitedModifier = 1.0;
            var electronTransportLimitedModifier = 1.0;
            var cropParameterGenerator = new CropParameterGenerator();

            // Act
            var cropParams = cropParameterGenerator.Generate(cropName, rubiscoLimitedModifier, electronTransportLimitedModifier);

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
            var cropParamsDefault = cropParameterGenerator.Generate(cropName, 1.0, 1.0);
            var cropParamsModified = cropParameterGenerator.Generate(cropName, rubiscoLimitedModifier, electronTransportLimitedModifier);

            // Assert
            var defaultMaxRubiscoActivitySLNRatio = cropParamsDefault.Pathway.MaxRubiscoActivitySLNRatio;
            var defaultMaxPEPcActivitySLNRatio = cropParamsDefault.Pathway.MaxPEPcActivitySLNRatio;
            var defaultMesophyllCO2ConductanceSLNRatio = cropParamsDefault.Pathway.MesophyllCO2ConductanceSLNRatio;
            var defaultMaxElectronTransportSLNRatio = cropParamsDefault.Pathway.MaxElectronTransportSLNRatio;
            var defaultSpectralCorrectionFactor = cropParamsDefault.Pathway.SpectralCorrectionFactor;

            var modifiedMaxRubiscoActivitySLNRatio = cropParamsModified.Pathway.MaxRubiscoActivitySLNRatio;
            var modifiedMaxPEPcActivitySLNRatio = cropParamsModified.Pathway.MaxPEPcActivitySLNRatio;
            var modifiedMesophyllCO2ConductanceSLNRatio = cropParamsModified.Pathway.MesophyllCO2ConductanceSLNRatio;
            var modifiedMaxElectronTransportSLNRatio = cropParamsModified.Pathway.MaxElectronTransportSLNRatio;
            var modifiedSpectralCorrectionFactor = cropParamsModified.Pathway.SpectralCorrectionFactor;

            var expectedModifiedMaxRubiscoActivitySLNRatio = defaultMaxRubiscoActivitySLNRatio * rubiscoLimitedModifier;
            var expectedModifiedMaxPEPcActivitySLNRatio = defaultMaxPEPcActivitySLNRatio * rubiscoLimitedModifier;
            var expectedModifiedMesophyllCO2ConductanceSLNRatio = defaultMesophyllCO2ConductanceSLNRatio * rubiscoLimitedModifier;
            var expectedModifiedMaxElectronTransportSLNRatio = defaultMaxElectronTransportSLNRatio * electronTransportLimitedModifier;
            var expectedModifiedSpectralCorrectionFactor = 
                1 + (electronTransportLimitedModifier * defaultSpectralCorrectionFactor) -
                electronTransportLimitedModifier;

            // 0.000001
            double tolerance = Math.Pow(10, -6);
            Assert.That(modifiedMaxRubiscoActivitySLNRatio, Is.EqualTo(expectedModifiedMaxRubiscoActivitySLNRatio).Within(tolerance));
            Assert.That(modifiedMaxPEPcActivitySLNRatio, Is.EqualTo(expectedModifiedMaxPEPcActivitySLNRatio).Within(tolerance));
            Assert.That(modifiedMesophyllCO2ConductanceSLNRatio, Is.EqualTo(expectedModifiedMesophyllCO2ConductanceSLNRatio).Within(tolerance));
            Assert.That(modifiedMaxElectronTransportSLNRatio, Is.EqualTo(expectedModifiedMaxElectronTransportSLNRatio).Within(tolerance));
            Assert.That(modifiedSpectralCorrectionFactor, Is.EqualTo(expectedModifiedSpectralCorrectionFactor).Within(tolerance));
        }

        [TestCase("InvalidCropName")]
        public void Generate_UnknownCrop_ReturnsNull(string cropName)
        {
            // Arrange
            var rubiscoLimitedModifier = 1.0;
            var electronTransportLimitedModifier = 1.0;
            var cropParameterGenerator = new CropParameterGenerator();

            // Act
            var cropParams = cropParameterGenerator.Generate(cropName, rubiscoLimitedModifier, electronTransportLimitedModifier);

            // Assert
            Assert.That(cropParams, Is.Null);
        }

        #endregion
    }
}
