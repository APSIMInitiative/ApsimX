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
        Directory.SetCurrentDirectory(Path.Combine(originalDir, @"..\..\..\"));
    }

    
    [TearDown]
    public static void Teardown()
    {
        Directory.SetCurrentDirectory(originalDir);
    }

    [Test]
    public void GetDirectoryPaths_ShouldNotIncludeExcludedSims()
    {

        var newDir = Directory.GetCurrentDirectory();
        var result = ValidationLocationUtility.GetDirectoryPaths();
        Assert.That(PayloadUtilities.EXCLUDED_SIMS_FILEPATHS, Does.Not.Contain(result));
    }
}
