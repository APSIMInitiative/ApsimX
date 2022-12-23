using Models.DCAPST;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class WheatCropParameterGeneratorTests
    {
        #region Tests
        
        [Test]
        public void Generate_ReturnsDefaultValues()
        {
            // Arrange

            // Act
            var paramDefaults = WheatCropParameterGenerator.Generate();

            // Assert
        }

        #endregion
    }
}
