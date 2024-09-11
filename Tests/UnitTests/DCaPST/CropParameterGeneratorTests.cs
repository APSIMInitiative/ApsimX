using Models.DCAPST;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class CropParameterGeneratorTests
    {
        #region Tests

        [TestCase(SorghumCropParameterGenerator.CROP_NAME)]
        [TestCase(WheatCropParameterGenerator.CROP_NAME)]
        public void Generate_KnownCrop_ReturnsValue(string cropName)
        {
            // Arrange
            var cropParameterGenerator = new CropParameterGenerator();

            // Act
            var cropParams = cropParameterGenerator.Generate(cropName);

            // Assert
            Assert.That(cropParams, Is.Not.Null);
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
