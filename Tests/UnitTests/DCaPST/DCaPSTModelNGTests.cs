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

        #endregion
    }
}
