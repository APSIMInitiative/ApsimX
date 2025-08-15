using Models;
using Models.Core;
using Models.Factorial;
using Models.PMF;
using Models.PMF.Organs;
using Models.Soils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnitTests;
using UnitTests.Core;

namespace APSIM.Core.Tests;


[TestFixture]
class ScopeTests
{
    /// <summary>
    /// FindAll method tests.
    /// </summary>
    [Test]
    public void EnsureFindAllWorks()
    {
        var modelWithParentLink = new ModelWithParentLink();

        // Create a simulation
        var simulation = new Simulation()
        {
            Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new Zone()
                    {
                        Name = "zone1",
                        Children = new List<IModel>()
                        {
                            new Soil(),
                            new Plant()
                            {
                                Name = "plant1",
                                Children = new List<IModel>()
                                {
                                    new Leaf() { Name = "leaf1" },
                                    new GenericOrgan() { Name = "stem1" }
                                }
                            },
                            new Plant()
                            {
                                Name = "plant2",
                                Children = new List<IModel>()
                                {
                                    new Leaf() { Name = "leaf2" },
                                    new GenericOrgan() { Name = "stem2" }
                                }
                            }
                        }

                    },
                    new Zone() { Name = "zone2" }
                }
        };
        var simulationNode = Node.Create(simulation);

        // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
        var leaf1 = simulationNode.Walk().First(n => n.Name == "leaf1");
        List<Node> inScopeOfLeaf1 = leaf1.WalkScoped().ToList();
        Assert.That(inScopeOfLeaf1.Count, Is.EqualTo(10));
        Assert.That(inScopeOfLeaf1[0].Name, Is.EqualTo("plant1"));
        Assert.That(inScopeOfLeaf1[1].Name, Is.EqualTo("leaf1"));
        Assert.That(inScopeOfLeaf1[2].Name, Is.EqualTo("stem1"));
        Assert.That(inScopeOfLeaf1[3].Name, Is.EqualTo("zone1"));
        Assert.That(inScopeOfLeaf1[4].Name, Is.EqualTo("Soil"));
        Assert.That(inScopeOfLeaf1[5].Name, Is.EqualTo("plant2"));
        Assert.That(inScopeOfLeaf1[6].Name, Is.EqualTo("Simulation"));
        Assert.That(inScopeOfLeaf1[7].Name, Is.EqualTo("Clock"));
        Assert.That(inScopeOfLeaf1[8].Name, Is.EqualTo("MockSummary"));
        Assert.That(inScopeOfLeaf1[9].Name, Is.EqualTo("zone2"));

        // Ensure correct scoping from soil
        var soil = simulationNode.Walk().First(n => n.Model is Soil);
        List<Node> inScopeOfSoil = soil.WalkScoped().ToList();
        Assert.That(inScopeOfSoil.Count, Is.EqualTo(12));
        Assert.That(inScopeOfSoil[0].Name, Is.EqualTo("zone1"));
        Assert.That(inScopeOfSoil[1].Name, Is.EqualTo("Soil"));
        Assert.That(inScopeOfSoil[2].Name, Is.EqualTo("plant1"));
        Assert.That(inScopeOfSoil[3].Name, Is.EqualTo("leaf1"));
        Assert.That(inScopeOfSoil[4].Name, Is.EqualTo("stem1"));
        Assert.That(inScopeOfSoil[5].Name, Is.EqualTo("plant2"));
        Assert.That(inScopeOfSoil[6].Name, Is.EqualTo("leaf2"));
        Assert.That(inScopeOfSoil[7].Name, Is.EqualTo("stem2"));
        Assert.That(inScopeOfSoil[8].Name, Is.EqualTo("Simulation"));
        Assert.That(inScopeOfSoil[9].Name, Is.EqualTo("Clock"));
        Assert.That(inScopeOfSoil[10].Name, Is.EqualTo("MockSummary"));
        Assert.That(inScopeOfSoil[11].Name, Is.EqualTo("zone2"));
    }

    /// <summary>
    /// If a model is under Factor then it needs to be able to find a model in the base simulation.
    /// </summary>
    [Test]
    public void EnsureFindUnderFactorWorks()
    {
        // Create a simulation
        var simulations = new Simulations()
        {
            Children = new List<IModel>()
            {
                new Experiment()
                {
                    Children = new()
                    {
                        new Factors()
                        {
                            Children = new()
                            {
                                new Factor()
                            }
                        },
                    new Simulation()
                    {
                        Children = new()
                        {
                            new Clock(),
                            new Zone()
                        },
                    }
                    }
                },
            }
        };
        var simulationsNode = Node.Create(simulations);

        // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
        var factor = simulationsNode.Walk().First(n => n.Name == "Factor");
        List<Node> inScopeOfFactor = factor.WalkScoped().ToList();
        Assert.That(inScopeOfFactor.Count, Is.EqualTo(7));
        Assert.That(inScopeOfFactor[0].Name, Is.EqualTo("Experiment"));
        Assert.That(inScopeOfFactor[1].Name, Is.EqualTo("Factors"));
        Assert.That(inScopeOfFactor[2].Name, Is.EqualTo("Factor"));
        Assert.That(inScopeOfFactor[3].Name, Is.EqualTo("Simulation"));
        Assert.That(inScopeOfFactor[4].Name, Is.EqualTo("Clock"));
        Assert.That(inScopeOfFactor[5].Name, Is.EqualTo("Zone"));
        Assert.That(inScopeOfFactor[6].Name, Is.EqualTo("Simulations"));
    }
}
