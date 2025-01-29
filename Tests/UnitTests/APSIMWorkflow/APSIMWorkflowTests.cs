using NUnit.Framework;
using APSIM.Workflow;
using APSIM.Shared.Utilities;
using System.Reflection;
using System.IO;

namespace APSIMWorkflowTests;

[TestFixture]
public class ProgramTests
{
    [Test]
    public void TestUpdateWeatherFileNamePathInApsimXFile()
    {
        // Arrange
        string oldWeatherFilePath = "%root%/Examples/WeatherFiles/AU_Dalby.met";
        string text = ReflectionUtilities.GetResourceAsString("UnitTests.apsimworkflow.apsimx");
        string expectedNewFileName = "C:/Test/newWeatherfile.met";

        // APSIM.Workflow.Program program = new();

        // // // Act
        string unitTestDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("\\","/");
        string apsimFileName = Path.Combine(unitTestDir, "apsimworkflow.apsimx").Replace("\\","/");
        Program.apsimFileName = apsimFileName;
        Program.UpdateWeatherFileNamePathInApsimXFile(text, oldWeatherFilePath, expectedNewFileName, new Options() { DirectoryPath = unitTestDir });

        // // Assert
        // Assert.That(expectedNewFileName, Is.EqualTo("C:/Test/newWeatherfile.met"));
    }
}

