using NUnit.Framework;
using System;
using System.IO;
using APSIM.Workflow;
using System.IO.Compression;
using System.Linq;

namespace UnitTests.APSIMWorkflowTests;

[TestFixture]
public class PayloadUtilitiesTest
{
    string[] VAL_FILE_PATHS = {
        "/Prototypes/C4Maize/C4Maize.apsimx",
        "/Prototypes/C4Maize/C4Maize.apsimx",
        "/Prototypes/CroptimizR/template.apsimx",
        "/Prototypes/DEROPAPY/Deropapy.apsimx",
        "/Prototypes/FieldPea/FieldPeaValidation.apsimx",
        "/Prototypes/ForageBrassica/ForageBrassica.apsimx",
        "/Prototypes/LeafApex/CanopyPhotosynthesis.apsimx",
        "/Prototypes/LeafApex/LeafApex.apsimx",
        "/Prototypes/Lentil/Lentil.apsimx",
        "/Prototypes/Lifecycle/PotatoPests.apsimx",
        "/Prototypes/LimitedTranspirationRateWheat/LimitedTranspirationRateWheat.apsimx",
        "/Prototypes/Lucerne/LucerneValidation.apsimx",
        "/Prototypes/MultiSpeciesPasture/MultiSpeciesPastures.apsimx",
        "/Prototypes/MultiZoneRoots/MultiRootZone.apsimx",
        "/Prototypes/Pasture/Pasture.apsimx",
        "/Prototypes/Pasture/PastureExample.apsimx",
        "/Prototypes/Pasture/pasture_test_1.apsimx",
        "/Prototypes/Pasture/pasture_test_2.apsimx",
        "/Prototypes/Pasture/pasture_test_3.apsimx",
        "/Prototypes/Pasture/pasture_test_4.apsimx",
        "/Prototypes/Pasture/pasture_test_store.apsimx",
        "/Prototypes/PigeonPea/PigeonPea.apsimx",
        "/Prototypes/Rice/rice.apsimx",
        "/Prototypes/SimplifiedOrganArbitrator/FodderBeetOptimise.apsimx",
        "/Prototypes/STRUM/Orchard.apsimx",
        "/Prototypes/STRUM/STRUM.apsimx",
        "/Prototypes/Teff/Teff.apsimx",
        "/Prototypes/TropicalPasture/TropicalPasture.apsimx",
        "/Prototypes/WaterBalance/WaterBalance.apsimx",
        "/Prototypes/WEIRDO/ReportDetail.apsimx",
        "/Prototypes/WEIRDO/WEIRDO.apsimx",
        "/Prototypes/Aqua/FoodInPond/FoodInPond.apsimx",
        "/Prototypes/Aqua/PondWater/PondWater.apsimx",
        "/Prototypes/Aqua/Prawns/Prawns.apsimx",
        "/Tests/Simulation/ApsimComparisons/Comparison.apsimx",
        "/Tests/Simulation/BiomassRemoval/BiomassRemovalTests.apsimx",
        "/Tests/Simulation/CO2 response/CO2 response Wheat.apsimx",
        "/Tests/Simulation/DairyFarmManager/PlantainGrazing.apsimx",
        "/Tests/Simulation/DairyFarmManager/SimpleRotation.apsimx",
        "/Tests/Simulation/Infrastructure/DisabledReplacements.apsimx",
        "/Tests/Simulation/Infrastructure/PlantInReplacements.apsimx",
        "/Tests/Simulation/Infrastructure/RotationManager/RotationManager.apsimx",
        "/Tests/Simulation/Infrastructure/RotationManager/RotationTest.apsimx",
        "/Tests/Simulation/Infrastructure/SetSimulationParametersFromFile/SetSimulationParametersFromFile.apsimx",
        "/Tests/Simulation/Initialisation/EucalyptusRotation.apsimx",
        "/Tests/Simulation/MultiZoneManagement/MultiFieldMultiZoneManagementBasic.apsimx",
        "/Tests/Simulation/MultiZoneManagement/MultiPaddock.apsimx",
        "/Tests/Simulation/MultiZoneManagement/MultiPaddockEcon.apsimx",
        "/Tests/Simulation/MultiZoneManagement/MultiZoneManagement.apsimx",
        "/Tests/Simulation/ReadExternalFiles/ReadFileTest.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PatchyMcPatchFace.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/SoilNitrogenPatchValidationOfPatchiness.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/BivariateNormal.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/Paddock_V1.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/SimpleCow.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/SimpleCowScriptVersion.apsimx",
        "/Tests/Simulation/Volatilisation/Volatilisation.apsimx",
        "/Tests/Simulation/ZMQ-Sync/ZMQ-sync.apsimx",
        "/Tests/Simulation/DamageFunction/Canola/CanolaDamageFunction.apsimx",
        "/Tests/Simulation/DamageFunction/Wheat/WheatDamageFunction.apsimx",
        "/Tests/Simulation/Infrastructure/RotationManager/RotationManager.apsimx",
        "/Tests/Simulation/Infrastructure/RotationManager/RotationTest.apsimx",
        "/Tests/Simulation/Infrastructure/SetSimulationParametersFromFile/SetSimulationParametersFromFile.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/BivariateNormal.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/Paddock_V1.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/SimpleCow.apsimx",
        "/Tests/Simulation/SoilNitrogenPatch/PaddockSims/SimpleCowScriptVersion.apsimx",
        "/Tests/UnderReview/Cotton/Cotton.apsimx",
        "/Tests/Validation/AgPasture/AgPasture.apsimx",
        "/Tests/Validation/AgPasture/SpeciesTable.apsimx",
        "/Tests/Validation/Agroforestry/AgroforestrySystem.apsimx",
        "/Tests/Validation/Barley/Barley.apsimx",
        "/Tests/Validation/Canola/Canola.apsimx",
        "/Tests/Validation/Chickpea/Chickpea.apsimx",
        "/Tests/Validation/Chicory/Chicory.apsimx",
        "/Tests/Validation/CLEM/CLEM_Sensibility_GrowCrop.apsimx",
        "/Tests/Validation/Clock/Clock.apsimx",
        "/Tests/Validation/Eucalyptus/Eucalyptus.apsimx",
        "/Tests/Validation/FodderBeet/FodderBeet.apsimx",
        "/Tests/Validation/Gliricidia/Gliricidia.apsimx",
        "/Tests/Validation/Grapevine/grapevine.apsimx",
        "/Tests/Validation/Lucerne/LucerneValidation.apsimx",
        "/Tests/Validation/Maize/Maize.apsimx",
        "/Tests/Validation/MicroClimate/MicroClimate.apsimx",
        "/Tests/Validation/Mungbean/Mungbean.apsimx",
        "/Tests/Validation/NDVI/NDVItest.apsimx",
        "/Tests/Validation/Nutrient/Nutrient.apsimx",
        "/Tests/Validation/Oats/Oats.apsimx",
        "/Tests/Validation/OilPalm/OilPalm.apsimx",
        "/Tests/Validation/Peanut/Peanut.apsimx",
        "/Tests/Validation/Pinus/Pinus.apsimx",
        "/Tests/Validation/PlantainForage/PlantainForage.apsimx",
        "/Tests/Validation/Potato/Potato.apsimx",
        "/Tests/Validation/RedClover/RedClover.apsimx",
        "/Tests/Validation/SCRUM/SCRUM.apsimx",
        "/Tests/Validation/Slurp/Slurp.apsimx",
        "/Tests/Validation/SoilArbitrator/SoilArbitrator.apsimx",
        "/Tests/Validation/SoilTemperature/SoilTemperature.apsimx",
        "/Tests/Validation/SoilWater/SoilWater.apsimx",
        "/Tests/Validation/SoilWater/Sensibility/Test_Each_Process_Separately.apsimx",
        "/Tests/Validation/Sorghum/Sorghum.apsimx",
        "/Tests/Validation/Sorghum/DynamicTillering/DynamicTillering.apsimx",
        "/Tests/Validation/Soybean/Soybean.apsimx",
        "/Tests/Validation/SPRUM/SPRUM.apsimx",
        "/Tests/Validation/Stock/Stock.apsimx",
        "/Tests/Validation/Sugarcane/Sugarcane.apsimx",
        "/Tests/Validation/SurfaceOrganicMatter/SurfaceOrganicMatter.apsimx",
        "/Tests/Validation/SWIM/SWIM.apsimx",
        "/Tests/Validation/Weather/Weather.apsimx",
        "/Tests/Validation/Wheat/Wheat.apsimx",
        "/Tests/Validation/Wheat/GxExM/GxExM.apsimx",
        "/Tests/Validation/WhiteClover/WhiteClover.apsimx",
        "/Tests/Validation/WorkFloTest/WorkFloTest.apsimx",
        "/Tests/Validation/DCaPST/Sorghum/SorghumDCaPST.apsimx",
        "/Tests/Validation/SoilWater/Sensibility/Test_Each_Process_Separately.apsimx",
        "/Tests/Validation/Sorghum/DynamicTillering/DynamicTillering.apsimx",
        "/Tests/Validation/System/FACTS_CornSoy/FACTS_Ames.apsimx",
        "/Tests/Validation/System/FACTS_CornSoy/FACTS_Ames3N.apsimx",
        "/Tests/Validation/System/FACTS_CornSoy/FACTS_Kanawha.apsimx",
        "/Tests/Validation/System/FACTS_CornSoy/FACTS_McNay.apsimx",
        "/Tests/Validation/System/FACTS_CornSoy/FACTS_Muscatine.apsimx",
        "/Tests/Validation/System/Ginniderra/GinniderraApsimX.apsimx",
        "/Tests/Validation/System/Hudson/Hudson.apsimx",
        "/Tests/Validation/System/Hudson/HudsonSWIM3.apsimx",
        "/Tests/Validation/System/LincolnRotation2017/RainshelterRotationSim.apsimx",
        "/Tests/Validation/System/NZRiskIndexTool/RITTemplate2025.apsimx",
        "/Tests/Validation/System/Tarlee Rotation Trial/TarleeRotationTrial.apsimx",
        "/Tests/Validation/System/Wagga/Wagga.apsimx",
        "/Tests/Validation/Wheat/GxExM/GxExM.apsimx",
        "/Examples/Agroforestry/Gliricidia Stripcrop Example.apsimx",
        "/Examples/Agroforestry/Single Tree Example.apsimx",
        "/Examples/Agroforestry/Tree Belt Example.apsimx",
        "/Examples/CLEM/CLEM_Example_Cropping.apsimx",
        "/Examples/CLEM/CLEM_Example_Grazing.apsimx",
        "/Examples/CLEM/CLEM_Sensibility_HerdManagement.apsimx",
        "/Examples/ManagerExamples/RegressionExample.apsimx",
        "/Examples/Optimisation/CroptimizRExample.apsimx",
        "/Examples/Sensitivity/Morris.apsimx",
        "/Examples/Sensitivity/Sobol.apsimx",
        "/Examples/Tutorials/ClimateController.apsimx",
        "/Examples/Tutorials/CO2.apsimx",
        "/Examples/Tutorials/EoCalculateAndInput.apsimx",
        "/Examples/Tutorials/EventPublishSubscribe.apsimx",
        "/Examples/Tutorials/ExcelDataExample.apsimx",
        "/Examples/Tutorials/InitialisingSoilCarbonPools.apsimx",
        "/Examples/Tutorials/Manager.apsimx",
        "/Examples/Tutorials/Memo.apsimx",
        "/Examples/Tutorials/PredictedObserved.apsimx",
        "/Examples/Tutorials/PropertyUI.apsimx",
        "/Examples/Tutorials/Report.apsimx",
        "/Examples/Tutorials/Sensitivity_FactorialANOVA.apsimx",
        "/Examples/Tutorials/Sensitivity_MorrisMethod.apsimx",
        "/Examples/Tutorials/Sensitivity_SobolMethod.apsimx",
        "/Examples/Tutorials/SlowReleaseFertiliser.apsimx",
        "/Examples/Tutorials/SWIM.apsimx",
        "/Examples/Tutorials/Lifecycle/lifecycle.apsimx",
        "/Examples/Tutorials/Lifecycle/lifecycle.apsimx",
    };

    [Test]
    public void GetAllFilesMatchingPath_ValidSource_ReturnsMatchingFiles()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string testFile1 = Path.Combine(testDirectory, "test1.txt");
        string testFile2 = Path.Combine(testDirectory, "test2.txt");
        File.WriteAllText(testFile1, "Test file 1 content");
        File.WriteAllText(testFile2, "Test file 2 content");

        string source = Path.Combine(testDirectory, "*.txt");

        try
        {
            // Act
            string[] result = PayloadUtilities.GetAllFilesMatchingPath(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result, Does.Contain(testFile1));
            Assert.That(result, Does.Contain(testFile2));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void GetAllFilesMatchingPath_InvalidSource_ThrowsArgumentNullException()
    {
        // Arrange
        string source = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PayloadUtilities.GetAllFilesMatchingPath(source));
    }

    [Test]
    public void GetAllFilesMatchingPath_NoMatchingFiles_ReturnsEmptyArray()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string source = Path.Combine(testDirectory, "*.txt");

        try
        {
            // Act
            string[] result = PayloadUtilities.GetAllFilesMatchingPath(source);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void GetAllFilesMatchingPath_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        string source = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "*.txt");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => PayloadUtilities.GetAllFilesMatchingPath(source));
    }

    [Test]
    public void GetAllFilesMatchingPath_CaseInsensitiveMatching_ReturnsMatchingFiles()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string testFile = Path.Combine(testDirectory, "TESTFILE.TXT");

        File.WriteAllText(testFile, "Test file content");

        string testFileLower = Path.Combine(testDirectory, "testfile.txt");

        try
        {
            // Act
            string[] result = PayloadUtilities.GetAllFilesMatchingPath(testFileLower);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(1, Is.EqualTo(result.Length));
            Assert.That(result, Contains.Item(testFile));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void GetActualFilePath_SingleMatchingFile_ReturnsFilePath()
    {
        // Arrange
        string[] matchingFiles = { "file1.txt" };

        // Act
        string result = PayloadUtilities.GetActualFilePath(matchingFiles);

        // Assert
        Assert.That(result, Is.EqualTo("file1.txt"));
    }

    [Test]
    public void GetActualFilePath_MultipleMatchingFiles_ThrowsException()
    {
        // Arrange
        string[] matchingFiles = { "file1.txt", "file2.txt" };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => PayloadUtilities.GetActualFilePath(matchingFiles));
        Assert.That(ex.Message, Is.EqualTo("Multiple files found matching the weather file path."));
    }

    [Test]
    public void GetActualFilePath_NoMatchingFiles_ReturnsEmptyString()
    {
        // Arrange
        string[] matchingFiles = Array.Empty<string>();

        // Act
        string result = PayloadUtilities.GetActualFilePath(matchingFiles);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }


    [Test]
    public void RemoveUnusedFilesFromArchive_ValidZipFile_RemovesUnusedFiles()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string zipFilePath = Path.Combine(Path.GetDirectoryName(testDirectory), Guid.NewGuid() + ".zip");
        string validFile = Path.Combine(testDirectory, "valid.apsimx");
        string invalidFile = Path.Combine(testDirectory, "invalid.txt");

        File.WriteAllText(validFile, "Valid file content");
        File.WriteAllText(invalidFile, "Invalid file content");

        ZipFile.CreateFromDirectory(testDirectory, zipFilePath);
        File.Delete(validFile);
        File.Delete(invalidFile);

        try
        {
            // Act
            PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath);

            // Assert
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            Assert.That(archive.Entries.Count, Is.EqualTo(1));
            Assert.That(archive.Entries[0].FullName, Is.EqualTo("valid.apsimx"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
            File.Delete(zipFilePath);
        }
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_NullZipFilePath_ThrowsException()
    {
        // Arrange
        string zipFilePath = null;

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath));
        Assert.That(ex.Message, Is.EqualTo("Error: Zip file path is null while trying to remove unused files from payload archive."));
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_NonExistentZipFile_ThrowsException()
    {
        // Arrange
        string zipFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.zip");

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath));
        Assert.That(ex.Message, Is.EqualTo("Error: Zip file does not exist while trying to remove unused files from payload archive."));
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_EmptyZipFile_NoChanges()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string zipFilePath = Path.Combine(Path.GetDirectoryName(testDirectory), Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(testDirectory, zipFilePath);

        try
        {
            // Act
            PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath);

            // Assert
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            Assert.That(archive.Entries.Count, Is.EqualTo(0));
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDirectory, true);
            File.Delete(zipFilePath);
        }
    }

    [Test]
    public void RemoveUnusedFilesFromArchive_ArchiveWithOnlyValidFiles_NoChanges()
    {
        // Arrange
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        string zipFilePath = Path.Combine(Path.GetDirectoryName(testDirectory), Guid.NewGuid() + ".zip");
        string validFile1 = Path.Combine(testDirectory, "file1.apsimx");
        string validFile2 = Path.Combine(testDirectory, "file2.csv");

        File.WriteAllText(validFile1, "Valid file 1 content");
        File.WriteAllText(validFile2, "Valid file 2 content");

        ZipFile.CreateFromDirectory(testDirectory, zipFilePath);
        File.Delete(validFile1);
        File.Delete(validFile2);

        try
        {
            // Act
            PayloadUtilities.RemoveUnusedFilesFromArchive(zipFilePath);

            // Assert
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            Assert.That(archive.Entries.Count, Is.EqualTo(2));
            string[] expectedFiles = { "file1.apsimx", "file2.csv" };
            Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EquivalentTo(expectedFiles));
        }
        finally
        {
            // Cleanup
            File.Delete(zipFilePath);
            Directory.Delete(testDirectory, true);
        }
    }
    
    [Test]
    public void RemoveRSimsDirsFromValidationDirs_RemovesRSimPaths_CaseInsensitive()
    {
        // Act
        var result = typeof(PayloadUtilities)
            .GetMethod("RemoveRSimsDirsFromValidationDirs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { VAL_FILE_PATHS }) as string[];
        // Assert: only the non-R paths remain
        Assert.That(result, Does.Not.Contain(PayloadUtilities.R_SIMS_FILEPATHS));
    }
}