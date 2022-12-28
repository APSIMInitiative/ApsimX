using DocumentFormat.OpenXml.Spreadsheet;
using Models.DCAPST;
using Moq;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class DCaPSTModelNGTests
    {
        #region Tests
        
        [Test]
        public void SetCropName_ValueSet()
        {
            // Arrange
            var cropName = "testCrop";
            var model = new DCaPSTModelNG
            {
                // Act
                CropName = cropName
            };

            // Assert
            Assert.AreEqual(model.CropName, cropName);
        }

        [Test]
        public void SetParameters_ValueSet()
        {
            // Arrange
            // Choose a few random params to test.
            var airC02 = 55.5;
            var atpProductionElectronTransportFactor = 1.043;
            var rpar = 20.7;

            var dcapstParameters = new DCaPSTParameters()
            {
                Canopy = new CanopyParameters()
                {
                    AirCO2 = airC02
                },
                Pathway = new PathwayParameters()
                {
                    ATPProductionElectronTransportFactor = atpProductionElectronTransportFactor
                },
                Rpar = rpar
            };

            var model = new DCaPSTModelNG
            {
                // Act
                Parameters = dcapstParameters
            };

            // Assert
            Assert.AreEqual(model.Parameters.Canopy.AirCO2, airC02);
            Assert.AreEqual(model.Parameters.Pathway.ATPProductionElectronTransportFactor, atpProductionElectronTransportFactor);
            Assert.AreEqual(model.Parameters.Rpar, rpar);
        }

        #endregion
    }
}
