using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Core;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
class ModelNodeTreeTests
{
    /// <summary>Simple POCO model</summary>
    public class DummyPOCO
    {

    }

    public (string name, IEnumerable<object> children ) DummyPOCOToParentChildren(object obj)
    {
        return ("DummyPOCO", null);
    }

    /// <summary>Ensure that calling Tree.Initialise sets up the parent child relationship correctly.</summary>
    [Test]
    public void TreeInitialise_EstablishesParentChildRelationship()
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
                        new ClassAdapter()
                        {
                            Obj = new DummyPOCO()
                        }
                    }
                }
            ]
        };

        NodeTree tree = new();
        tree.RegisterDiscoveryFunction(typeof(DummyPOCO), DummyPOCOToParentChildren);
        tree.Initialise(simulation, didConvert:false);

        var sim = tree.GetNode();
        Assert.That(sim.Name, Is.EqualTo("Sim"));
        Assert.That(sim.Parent, Is.Null);
        Assert.That(sim.FullNameAndPath, Is.EqualTo(".Sim"));
        Assert.That(sim.Children.Count(), Is.EqualTo(1));

        var zone = sim.Children.First();
        Assert.That(zone.Name, Is.EqualTo("Zone"));
        Assert.That(zone.Parent, Is.EqualTo(sim));
        Assert.That(zone.FullNameAndPath, Is.EqualTo(".Sim.Zone"));
        Assert.That(zone.Children.Count, Is.EqualTo(1));

        var poco = zone.Children.First();
        Assert.That(poco.Name, Is.EqualTo("DummyPOCO"));
        Assert.That(poco.Parent, Is.EqualTo(zone));
        Assert.That(poco.FullNameAndPath, Is.EqualTo(".Sim.Zone.DummyPOCO"));
        Assert.That(poco.Children, Is.Empty);
    }

    /// <summary>Ensure WalkModels iterates through all models.</summary>
    [Test]
    public void WalkModels_IteratesThroughAllNodes()
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
                        new ClassAdapter()
                        {
                            Obj = new DummyPOCO()
                        }
                    },
                },
                new Zone()
                {
                    Children = new()
                    {
                        new Clock()
                        {
                        }
                    },
                }
            ]
        };

        NodeTree tree = new();
        tree.RegisterDiscoveryFunction(typeof(DummyPOCO), DummyPOCOToParentChildren);
        tree.Initialise(simulation, didConvert:false);

        var models = tree.WalkModels.ToArray();
        Assert.That(models[0] is Simulation);
        Assert.That(models[1] is Zone);
        Assert.That(models[2] is DummyPOCO);
        Assert.That(models[3] is Zone);
        Assert.That(models[4] is Clock);
    }

    /// <summary>Ensure NodeRescan removes existing children before adding new children.</summary>
    [Test]
    public void NodeRescan_RemovesExistingChildNodes()
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
                        new Clock()
                        {
                        }
                    },
                }
            ]
        };

        NodeTree tree = new();
        tree.RegisterDiscoveryFunction(typeof(DummyPOCO), DummyPOCOToParentChildren);
        tree.Initialise(simulation, didConvert:false);

        var zone = simulation.Children.First();
        var summary = new Summary();
        zone.Children.Clear();
        zone.Children.Add(summary);
        tree.Rescan(tree.GetNode(zone));

        var models = tree.WalkModels.ToArray();
        Assert.That(models[0] is Simulation);
        Assert.That(models[1] is Zone);
        Assert.That(models[2] is Summary);

        models = tree.Models.ToArray();
        Assert.That(models.Length, Is.EqualTo(3));
        Assert.That(models.Contains(simulation));
        Assert.That(models.Contains(zone));
        Assert.That(models.Contains(summary));

    }
}