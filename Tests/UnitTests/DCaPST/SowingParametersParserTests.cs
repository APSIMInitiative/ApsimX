using Models.Core;
using Models.DCAPST;
using Models.PMF;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class SowingParametersParserTests
    {
        #region Constants
        private const string SORGHUM_PLANT_NAME = "Sorghum";
        private const string WHEAT_PLANT_NAME = "Wheat";
        private const string M35_CULTIVAR_NAME = "M35-1";
        private const string CSH13R_CULTIVAR_NAME = "CSH13R";
        #endregion

        #region TestHelpers

        private static IModel CreateChildModel<T>(string name) where T : IModel, new()
        {
            var child = new T()
            {
                Name = name
            };

            return child;
        }

        private static DCaPSTModelNG CreateModel()
        {
            return new DCaPSTModelNG();
        }

        private static DCaPSTModelNG CreateModel(
            string cultivarFolderName, 
            string plantName,
            string cultivarName
        )
        {
            // Create the model hierarchy first.
            var cultivarFolder = CreateChildModel<Folder>(cultivarFolderName);
            var plantFolder = CreateChildModel<Folder>(plantName);
            var cultivar = CreateChildModel<Cultivar>(cultivarName);
            plantFolder.Children.Add(cultivar);
            cultivarFolder.Children.Add(plantFolder);

            return new DCaPSTModelNG()
            {
                Children = new List<IModel>()
                {
                    cultivarFolder
                }
            };
        }

        private static SowingParameters CreateSowingParameters(
            string plantName,
            string cultivarName
        )
        {
            return new SowingParameters()
            {
                Plant = new Plant()
                {
                    Name = plantName
                },
                Cultivar = cultivarName
            };
        }
        #endregion

        #region Tests

        [TestCase(
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            SORGHUM_PLANT_NAME,
            M35_CULTIVAR_NAME
        )]
        [TestCase(
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            WHEAT_PLANT_NAME,
            M35_CULTIVAR_NAME
        )]
        [TestCase(
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            SORGHUM_PLANT_NAME,
            CSH13R_CULTIVAR_NAME
        )]
        [TestCase(
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            WHEAT_PLANT_NAME,
            CSH13R_CULTIVAR_NAME
        )]
        public void GetCultivarFromSowingParameters_MatchingModelSowingCultivar_CultivarReturned(
            string modelCultivarFolderName,
            string plantName,
            string cultivarName
        )
        {
            // Arrange
            var model = CreateModel(modelCultivarFolderName, plantName, cultivarName);
            var sowingParameters = CreateSowingParameters(plantName, cultivarName);

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Not.Null);
            Assert.That(cultivarName, Is.EqualTo(cultivar.Name));
        }

        [TestCase(
            // Plants don't match 1
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            SORGHUM_PLANT_NAME,
            M35_CULTIVAR_NAME,
            WHEAT_PLANT_NAME,
            M35_CULTIVAR_NAME
        )]
        [TestCase(
            // Plants don't match 2
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            WHEAT_PLANT_NAME,
            M35_CULTIVAR_NAME,
            SORGHUM_PLANT_NAME,
            M35_CULTIVAR_NAME
        )]
        [TestCase(
            // Cultivars don't match 1
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            SORGHUM_PLANT_NAME,
            M35_CULTIVAR_NAME,
            SORGHUM_PLANT_NAME,
            CSH13R_CULTIVAR_NAME
        )]
        [TestCase(
            // Cultivars don't match 2
            SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME,
            SORGHUM_PLANT_NAME,
            CSH13R_CULTIVAR_NAME,
            SORGHUM_PLANT_NAME,
            M35_CULTIVAR_NAME
        )]
        public void GetCultivarFromSowingParameters_NonMatchingModelSowingCultivar_CultivarNull(
            string modelCultivarFolderName,
            string modelPlantName,
            string modelCultivarName,
            string sowingParamsPlantName,
            string sowingParamsCultivarName
        )
        {
            // Arrange
            var model = CreateModel(modelCultivarFolderName, modelPlantName, modelCultivarName);
            var sowingParameters = CreateSowingParameters(sowingParamsPlantName, sowingParamsCultivarName);

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }

        [Test]
        public void GetCultivarFromSowingParameters_NoCultivarFolder_CultivarNull()
        {
            // Arrange
            var plantName = SORGHUM_PLANT_NAME;
            var cultivarName = CSH13R_CULTIVAR_NAME;
            var model = CreateModel();
            var sowingParameters = CreateSowingParameters(plantName, cultivarName);

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }

        [Test]
        public void GetCultivarFromSowingParameters_NullModel_CultivarNull()
        {
            // Arrange
            var plantName = SORGHUM_PLANT_NAME;
            var cultivarName = CSH13R_CULTIVAR_NAME;
            IModel model = null;
            var sowingParameters = CreateSowingParameters(plantName, cultivarName);

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }

        [Test]
        public void GetCultivarFromSowingParameters_NullSowingParams_CultivarNull()
        {
            // Arrange
            var modelCultivarFolderName = SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME;
            var plantName = SORGHUM_PLANT_NAME;
            var cultivarName = CSH13R_CULTIVAR_NAME;
            var model = CreateModel(modelCultivarFolderName, plantName, cultivarName);
            SowingParameters sowingParameters = null;

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }

        [Test]
        public void GetCultivarFromSowingParameters_NullPlant_CultivarNull()
        {
            // Arrange
            // Arrange
            var modelCultivarFolderName = SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME;
            var plantName = SORGHUM_PLANT_NAME;
            var cultivarName = CSH13R_CULTIVAR_NAME;
            var model = CreateModel(modelCultivarFolderName, plantName, cultivarName);
            SowingParameters sowingParameters = new SowingParameters();

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }

        [Test]
        public void GetCultivarFromSowingParameters_NullCultivarName_CultivarNull()
        {
            // Arrange
            var modelCultivarFolderName = SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME;
            var plantName = SORGHUM_PLANT_NAME;
            var cultivarName = CSH13R_CULTIVAR_NAME;
            var model = CreateModel(modelCultivarFolderName, plantName, cultivarName);
            var sowingParameters = CreateSowingParameters(plantName, cultivarName);
            sowingParameters.Cultivar = null;

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }

        [Test]
        public void GetCultivarFromSowingParameters_EmptyCultivarName_CultivarNull()
        {
            // Arrange
            var modelCultivarFolderName = SowingParametersParser.CULTIVAR_PARAMETERS_FOLDER_NAME;
            var plantName = SORGHUM_PLANT_NAME;
            var cultivarName = CSH13R_CULTIVAR_NAME;
            var model = CreateModel(modelCultivarFolderName, plantName, cultivarName);
            var sowingParameters = CreateSowingParameters(plantName, cultivarName);
            sowingParameters.Cultivar = string.Empty;

            // Act
            var cultivar = SowingParametersParser.GetCultivarFromSowingParameters(model, sowingParameters);

            // Assert
            Assert.That(cultivar, Is.Null);
        }
        #endregion
    }
}
