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
        originalDir = Directory.GetCurrentDirectory();
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
        var result = ValidationLocationUtility.GetDirectoryPaths();
        Assert.That(result, Does.Not.Contain(PayloadUtilities.EXCLUDED_SIMS_FILEPATHS));
    }
}
