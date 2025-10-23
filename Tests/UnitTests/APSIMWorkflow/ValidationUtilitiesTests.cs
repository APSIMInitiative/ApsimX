using NUnit.Framework;
using System.IO;
using APSIM.Workflow;

namespace UnitTests.APSIMWorkflowTests;

[TestFixture]
public class ValidationLocationUtilityTest
{
    public static string originalDir = Directory.GetCurrentDirectory();

    [SetUp]
    public static void Setup()
    {
        Directory.SetCurrentDirectory(Path.Combine(originalDir, @"../../../"));
    }


    [TearDown]
    public static void Teardown()
    {
        Directory.SetCurrentDirectory(originalDir);
    }

    [Test]
    public void TestGetDirectoryPaths_ShouldNotIncludeExcludedSims()
    {
        var result = ValidationLocationUtility.GetValidationFilePaths();
        Assert.That(result, Does.Not.Contain(PayloadUtilities.EXCLUDED_SIMS_FILEPATHS));
    }
    
    [Test]
    public void TestGetSimulationCount_ShouldReturnNonZeroCount()
    {
        int count = ValidationLocationUtility.GetSimulationCount();
        Assert.That(count, Is.GreaterThan(0)); // Assuming there are valid validation directories in the test setup.
    }
}
