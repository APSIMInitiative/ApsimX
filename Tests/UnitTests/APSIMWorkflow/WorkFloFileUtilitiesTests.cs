using System;
using NUnit.Framework;
using APSIM.Workflow;
using System.Reflection;
using System.IO;

namespace UnitTests.APSIMWorkflow;

[TestFixture]
public class WorkFloFileUtilitiesTests
{

    /// <summary>
    /// Gets the directory of the unit tests.
    /// </summary>
    /// <returns></returns>
    private static string GetUnitTestsDirectory()
    {
        string execLocationPath = Assembly.GetExecutingAssembly().Location;
        int binIndex = execLocationPath.IndexOf("bin");
        return execLocationPath.Remove(binIndex) + "Tests\\UnitTests\\APSIMWorkflow\\".Replace("\\", "/");
    }
    
    [Test]
    public void TestCreateValidationWorkFloFile()
    {
        // Arrange
        string testDirPathStr = Path.Combine(GetUnitTestsDirectory(),"WorkFloFileUtilitiesExampleDir");

        // Act
        WorkFloFileUtilities.CreateValidationWorkFloFile(testDirPathStr);

        // Assert
        Assert.That(File.Exists(Path.Combine(testDirPathStr,"workflow.yml")), Is.True);
    }
}
