using System.Collections.Generic;
using Models.Core;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
class ParentChildTreeTests
{
    /// <summary>Simple POCO model</summary>
    public class DummyPOCO
    {

    }

    public (string name, IEnumerable<object> children ) DummyPOCOToParentChildren(object obj)
    {
        return ("DummyPOCO", null);
    }

    /// <summary>.</summary>
    [Test]
    public void SimpleTest()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone()
                {
                    Children = new()
                    {
                        new ClassAdaptor()
                        {
                            Obj = new DummyPOCO()
                        }
                    }
                }
            ]
        };

        ModelNodeTree tree = new();
        tree.RegisterDiscoveryFunction(typeof(DummyPOCO), DummyPOCOToParentChildren);
        tree.Initialise(simulation);

        var sim = tree.GetNode();
        Assert.That(sim.Name, Is.EqualTo("Sim"));
        Assert.That(sim.Parent, Is.Null);
        Assert.That(sim.FullNameAndPath, Is.EqualTo(".Sim"));
        Assert.That(sim.Children.Count, Is.EqualTo(1));

        var zone = sim.Children[0];
        Assert.That(zone.Name, Is.EqualTo("Zone"));
        Assert.That(zone.Parent, Is.EqualTo(sim));
        Assert.That(zone.FullNameAndPath, Is.EqualTo(".Sim.Zone"));
        Assert.That(zone.Children.Count, Is.EqualTo(1));

        var poco = zone.Children[0];
        Assert.That(poco.Name, Is.EqualTo("DummyPOCO"));
        Assert.That(poco.Parent, Is.EqualTo(zone));
        Assert.That(poco.FullNameAndPath, Is.EqualTo(".Sim.Zone.DummyPOCO"));
        Assert.That(poco.Children, Is.Null);
    }
}