using GLib;
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
        public void SetCropName_NewValue_HandleCropChangeCalledOnce()
        {
            // Arrange
            var cropName = "testCrop";
            var mock = new Mock<ICropParameterGenerator>(MockBehavior.Strict);
            mock.Setup(cropGen => cropGen.Generate(It.IsAny<string>())).Returns(new DCaPSTParameters()).Verifiable();

            var model = new DCaPSTModelNG();
            DCaPSTModelNG.ParameterGenerator = mock.Object;

            // Act
            model.CropName = cropName;

            // Assert
            mock.Verify(cropGen => cropGen.Generate(cropName), Times.Once());
            Assert.That(model.CropName, Is.EqualTo(cropName));
        }

        [Test]
        public void SetCropName_SameValue_HandleCropChangeCalledOnce()
        {
            // Arrange
            var cropName = "testCrop";
            var mock = new Mock<ICropParameterGenerator>(MockBehavior.Strict);
            mock.Setup(cropGen => cropGen.Generate(It.IsAny<string>())).Returns(new DCaPSTParameters()).Verifiable();

            var model = new DCaPSTModelNG();
            DCaPSTModelNG.ParameterGenerator = mock.Object;

            // Act
            model.CropName = cropName;

            // Assert
            mock.Verify(cropGen => cropGen.Generate(cropName), Times.Once());
            Assert.That(model.CropName, Is.EqualTo(cropName));
        }

        [Test]
        public void SetCropName_DifferentValue_HandleCropChangeCalledTwice()
        {
            // Arrange
            var cropName = "testCrop";
            var differentCropName = $"Different-{cropName}";
            var mock = new Mock<ICropParameterGenerator>(MockBehavior.Strict);
            mock.Setup(cropGen => cropGen.Generate(It.IsAny<string>())).Returns(new DCaPSTParameters()).Verifiable();

            var model = new DCaPSTModelNG();
            DCaPSTModelNG.ParameterGenerator = mock.Object;

            // Act
            model.CropName = cropName;
            model.CropName = differentCropName;

            // Assert
            mock.Verify(cropGen => cropGen.Generate(cropName), Times.Once());
            mock.Verify(cropGen => cropGen.Generate(differentCropName), Times.Once());
            Assert.That(model.CropName, Is.EqualTo(differentCropName));
        }

        [Test]
        public void SetupModel_ValueSet()
        {
            // Arrange
            var canopyParameters = new CanopyParameters();
            var pathwayParameters = new PathwayParameters();
            var dayOfYear = DateTime.NewNowUtc().DayOfYear;
            var latitude = 50.7220;
            var maxT = 30.0;
            var minT = -10.0;
            var radn = 1.0;
            var rpar = 2.0;
            var biolimit = 0.0;
            var reduction = 0.0;

            // Act
            var model = DCaPSTModelNG.SetUpModel(
                canopyParameters,
                pathwayParameters,
                dayOfYear,
                latitude,
                maxT,
                minT,
                radn,
                rpar,
                biolimit,
                reduction
            );

            // Assert - Nothing else can be tested.
            Assert.That(model.B, Is.EqualTo(0.409));
        }

        #endregion
    }
}
