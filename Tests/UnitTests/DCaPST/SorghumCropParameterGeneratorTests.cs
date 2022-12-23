using Models.DCAPST;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class SorghumCropParameterGeneratorTests
    {
        #region Tests
        
        [Test]
        public void Generate_ReturnsDefaultValues()
        {
            // Arrange

            // Act
            var paramDefaults = SorghumCropParameterGenerator.Generate();

            // Assert
        }

        #endregion
    }
}
