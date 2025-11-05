using NUnit.Framework;

namespace APSIM.Core.Tests;

[TestFixture]
public class ModelRegistryTests
{
    /// <summary>Ensure the ModelRegistry can find a model.</summary>
    [Test]
    public void TestGetMatchingTypes()
    {
        var simulationType = ModelRegistry.ModelNameToType("Simulation");

        Assert.That(simulationType, Is.EqualTo(typeof(Models.Core.Simulation)));
    }

    /// <summary>Ensure the ModelRegistry can create instances of a specified model.</summary>
    [Test]
    public void TestCreateModel()
    {
        var simulation = ModelRegistry.CreateModel("Simulation");

        Assert.That(simulation, Is.Not.Null);
    }
}