using NUnit.Framework;
using APSIM.Workflow;
using APSIM.Shared.Utilities;
using System.Reflection;
using System.IO;
using System.Linq;

namespace APSIMWorkflowTests;

[TestFixture]
public class ProgramTests
{
    /// <summary>
    /// Gets the directory of the unit tests.
    /// </summary>
    /// <returns></returns>
    private static string GetUnitTestsDirectory()
    {
        string execLocationPath = Assembly.GetExecutingAssembly().Location;
        int binIndex = execLocationPath.IndexOf("bin");
        return execLocationPath.Remove(binIndex) + "Tests\\UnitTests\\".Replace("\\", "/");
    }

    [TearDown]
    public void TearDown()
    {
        Program.apsimFileName = string.Empty;
        string apsimFileResetText = ReflectionUtilities.GetResourceAsString("UnitTests.apsimworkflow_unaltered.apsimx");
        string unitTestDir = GetUnitTestsDirectory();
        File.WriteAllText(Path.Combine(unitTestDir,"apsimworkflow.apsimx").Replace("\\","/"), apsimFileResetText);
    }

    [Test]
    public void TestUpdateWeatherFileNamePathInApsimXFile()
    {
        // Arrange
        string oldWeatherFilePath = "%root%/Examples/WeatherFiles/AU_Dalby.met";
        string text = ReflectionUtilities.GetResourceAsString("UnitTests.apsimworkflow.apsimx");
        string unitTestDir = GetUnitTestsDirectory();
        string apsimFileName = "apsimworkflow.apsimx";
        string newFilePath = "C:/Test/newWeatherfile.met";
        Program.apsimFileName = apsimFileName;

        // Act
        Program.UpdateWeatherFileNamePathInApsimXFile(text, oldWeatherFilePath, newFilePath, new Options() { DirectoryPath = unitTestDir });

        string textAfterUpdate = File.ReadAllText(Path.Combine(unitTestDir, apsimFileName).Replace("\\", "/"));

        // Assert
        Assert.That(text, Is.Not.EqualTo(textAfterUpdate)); 
    }

    [Test]
    public void GetApsimXFileTextFromDirectory()
    {
        // Arrange
        string unitTestDir = GetUnitTestsDirectory();
        string apsimFileText = ReflectionUtilities.GetResourceAsString("UnitTests.APSIMWorkflow.example_directory.apsimworkflow.apsimx");
        string fullTestDir = Path.Combine(unitTestDir, "APSIMWorkflow/example_directory/").Replace("\\", "/");
        string apsimFileName = Path.Combine(fullTestDir,"apsimworkflow.apsimx").Replace("\\","/");

        // Act
        string[] result = Program.GetApsimXFileTextFromDirectory(fullTestDir);

        // Assert
        Assert.That(result[0].Replace("\\","/"), Is.EqualTo(apsimFileName.Replace("\\","/")));
        Assert.That(result[1], Is.EqualTo(apsimFileText));
    }

    
}

