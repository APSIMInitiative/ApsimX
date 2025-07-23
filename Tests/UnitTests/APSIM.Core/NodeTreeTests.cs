using System.Linq;
using Models;
using Models.Core;
using NUnit.Framework;

namespace APSIM.Core.Tests;

[TestFixture]
class NodeTreeTests
{
    /// <summary>Simple POCO model</summary>
    public class DummyPOCO
    {
        public string Name { get; set; } = "poco";
        public DummyPOCOChild Child { get; set; }
    }

    public class DummyPOCOChild
    {
        public string Name { get; set; } = "poco-child";
    }

    public class DummyPOCOAdapter : Model, IModelAdapter
    {
        public DummyPOCO Obj { get; set; }

        public void Initialise()
        {
            Name = "poco";
            Children = [new DummyPOCOChildAdapter() { Obj = Obj.Child }];
        }
    }

    public class DummyPOCOChildAdapter : Model, IModelAdapter
    {
        /// <summary>Instance of POCO</summary>
        public DummyPOCOChild Obj { get; set; }

        public void Initialise()
        {
            Name = Obj?.Name;
        }
    }

    public class MockModelWithOnCreated : Model
    {
        public bool OnCreatedCalled { get; set; } = false;

        public override void OnCreated()
        {
            base.OnCreated();
            OnCreatedCalled = true;
        }
    }


    /// <summary>Ensure that calling Node.Create sets up the parent child relationship correctly.</summary>
    [Test]
    public void NodeCreate_EstablishesParentChildRelationship()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone()
                {
                    Children =
                    [
                        new DummyPOCOAdapter()
                        {
                            Obj = new DummyPOCO()
                            {
                                Child = new DummyPOCOChild()
                            }
                        },
                        new MockModelWithOnCreated()
                    ]
                }
            ]
        };
        var zone = simulation.Children.First();
        var dummyPOCOAdapter = zone.Children.First();

        // Convert the models to node tree.
        Node node = Node.Create(simulation);

        // Walk nodes.
        var nodes = node.Walk().ToArray();

        var simNode = nodes[0];
        Assert.That(simNode.Name, Is.EqualTo("Sim"));
        Assert.That(simNode.Parent, Is.Null);
        Assert.That(simNode.Model, Is.EqualTo(simulation));
        Assert.That(simNode.FullNameAndPath, Is.EqualTo(".Sim"));
        Assert.That(simNode.Children.Count(), Is.EqualTo(1));

        var zoneNode = nodes[1];
        Assert.That(zoneNode.Name, Is.EqualTo("Zone"));
        Assert.That(zoneNode.Parent, Is.EqualTo(simNode));
        Assert.That(zoneNode.Model, Is.EqualTo(zone));
        Assert.That(zoneNode.FullNameAndPath, Is.EqualTo(".Sim.Zone"));
        Assert.That(zoneNode.Children.Count, Is.EqualTo(2));

        var pocoNode = nodes[2];
        Assert.That(pocoNode.Name, Is.EqualTo("poco"));
        Assert.That(pocoNode.Parent, Is.EqualTo(zoneNode));
        Assert.That(pocoNode.Model, Is.AssignableTo<DummyPOCOAdapter>());
        Assert.That(pocoNode.FullNameAndPath, Is.EqualTo(".Sim.Zone.poco"));
        Assert.That(pocoNode.Children.Count, Is.EqualTo(1));

        var pocoChildNode = nodes[3];
        Assert.That(pocoChildNode.Name, Is.EqualTo("poco-child"));
        Assert.That(pocoChildNode.Parent, Is.EqualTo(pocoNode));
        Assert.That(pocoChildNode.Model, Is.AssignableTo<DummyPOCOChildAdapter>());
        Assert.That(pocoChildNode.FullNameAndPath, Is.EqualTo(".Sim.Zone.poco.poco-child"));
        Assert.That(pocoChildNode.Children, Is.Empty);

        var mockModelNode = nodes[4];
        Assert.That(mockModelNode.Name, Is.EqualTo("MockModelWithOnCreated"));
        Assert.That(mockModelNode.Parent, Is.EqualTo(zoneNode));
        Assert.That(mockModelNode.Model, Is.AssignableTo<MockModelWithOnCreated>());
        Assert.That(mockModelNode.FullNameAndPath, Is.EqualTo(".Sim.Zone.MockModelWithOnCreated"));
        Assert.That(mockModelNode.Children, Is.Empty);
        Assert.That((mockModelNode.Model as MockModelWithOnCreated).OnCreatedCalled, Is.True);
    }

    /// <summary>Ensure NodeTree.Add adds a model and node.</summary>
    [Test]
    public void NodeAdd_AddsNodeNodeAndModel()
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
                }
            ]
        };

        Node node = Node.Create(simulation);
        var zoneNode = node.Walk().First(n => n.Model is Zone);

        zoneNode.AddChild(new MockModelWithOnCreated());

        // Check nodes
        var nodes = node.Walk().ToArray();
        Assert.That(nodes[0].Name, Is.EqualTo("Sim"));
        Assert.That(nodes[0].Model is Simulation);
        Assert.That(nodes[1].Name, Is.EqualTo("Zone"));
        Assert.That(nodes[1].Model is Zone);
        Assert.That(nodes[2].Name, Is.EqualTo("MockModelWithOnCreated"));
        Assert.That(nodes[2].Model is MockModelWithOnCreated);

        // Check old IModel parent/children
        var zone = simulation.Children[0] as Zone;
        Assert.That(zone.Parent, Is.EqualTo(simulation));
        var mockModel = zone.Children[0] as MockModelWithOnCreated;
        Assert.That(mockModel.Parent, Is.EqualTo(zone));

        // Check that OnCreated was called.
        Assert.That(mockModel.OnCreatedCalled, Is.True);
    }

    /// <summary>Ensure NodeTree.Remove removes a model and node as well as child nodes and models.</summary>
    [Test]
    public void NodeRemove_RemovesNodeAndModel()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone()
                {
                    Children =
                    [
                        new Clock()
                    ]
                }
            ]
        };

        Node node = Node.Create(simulation);
        var simNode = node.Walk().First(n => n.Model is Simulation);
        var zoneNode = node.Walk().First(n => n.Model is Zone);

        simNode.RemoveChild(zoneNode.Model);

        // Check nodes
        var nodes = node.Walk().ToArray();
        Assert.That(nodes[0].Name, Is.EqualTo("Sim"));
        Assert.That(nodes[0].Model is Simulation);
        Assert.That(nodes.Count(), Is.EqualTo(1));

        // Check old IModel parent/children
        Assert.That(simulation.Children.Count, Is.EqualTo(0));
    }

    /// <summary>Ensure NodeTree.Replace replaces a model and node.</summary>
    [Test]
    public void NodeReplace_ReplacesNodeAndModel()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone()
                {
                    Children =
                    [
                        new Clock()
                    ]
                }
            ]
        };

        Node node = Node.Create(simulation);
        var zoneNode = node.Walk().First(n => n.Model is Zone);
        var clockNode = node.Walk().First(n => n.Model is Clock);

        zoneNode.ReplaceChild(clockNode.Model, new MockModelWithOnCreated());

        // Check nodes
        var nodes = node.Walk().ToArray();
        Assert.That(nodes.Count(), Is.EqualTo(3));
        Assert.That(nodes[0].Name, Is.EqualTo("Sim"));
        Assert.That(nodes[0].Model is Simulation);
        Assert.That(nodes[1].Name, Is.EqualTo("Zone"));
        Assert.That(nodes[1].Model is Zone);
        Assert.That(nodes[2].Name, Is.EqualTo("MockModelWithOnCreated"));
        Assert.That(nodes[2].Model is MockModelWithOnCreated);

        // Check old IModel parent/children
        var zone = simulation.Children[0] as Zone;
        Assert.That(zone.Parent, Is.EqualTo(simulation));
        var mockModel = zone.Children[0] as MockModelWithOnCreated;
        Assert.That(mockModel.Parent, Is.EqualTo(zone));

        // Check OnCreated was called.
        Assert.That(mockModel.OnCreatedCalled, Is.True);
    }

    /// <summary>Ensure NodeTree.Insert replaces a model and node.</summary>
    [Test]
    public void NodeInsert_InsertsNodeAndModelAtCorrectPosition()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone()
                {
                    Children =
                    [
                        new Clock()
                    ]
                }
            ]
        };

        Node node = Node.Create(simulation);
        var zoneNode = node.Walk().First(n => n.Model is Zone);
        var clockNode = node.Walk().First(n => n.Model is Clock);

        zoneNode.InsertChild(0, new MockModelWithOnCreated());

        // Check nodes
        var nodes = node.Walk().ToArray();
        Assert.That(nodes.Length, Is.EqualTo(4));
        Assert.That(nodes[0].Name, Is.EqualTo("Sim"));
        Assert.That(nodes[0].Model is Simulation);
        Assert.That(nodes[1].Name, Is.EqualTo("Zone"));
        Assert.That(nodes[1].Model is Zone);
        Assert.That(nodes[2].Name, Is.EqualTo("MockModelWithOnCreated"));
        Assert.That(nodes[2].Model is MockModelWithOnCreated);
        Assert.That(nodes[3].Name, Is.EqualTo("Clock"));
        Assert.That(nodes[3].Model is Clock);

        // Check old IModel parent/children
        var zone = simulation.Children[0] as Zone;
        Assert.That(zone.Parent, Is.EqualTo(simulation));
        var mockModel = zone.Children[0] as MockModelWithOnCreated;
        Assert.That(mockModel.Parent, Is.EqualTo(zone));
        var clock = zone.Children[1] as Clock;
        Assert.That(clock.Parent, Is.EqualTo(zone));

        // Check OnCreated was called.
        Assert.That(mockModel.OnCreatedCalled, Is.True);
    }

    /// <summary>Ensure Node.Rename renames sucessfully.</summary>
    [Test]
    public void NodeRename_RenamesSuccessfully()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone() { Name = "Zone1" },
                new Zone() { Name = "Zone2" },
            ]
        };

        var sim = Node.Create(simulation);
        var zone1 = sim.Children.First();
        zone1.Rename("NewName");
        Assert.That(zone1.Name, Is.EqualTo("NewName"));
        Assert.That(zone1.Model.Name, Is.EqualTo("NewName"));
    }

    /// <summary>Ensure Node.Rename successfully renames and avoids name clash with sibling.</summary>
    [Test]
    public void NodeRename_AvoidsNameClash()
    {
        // Create a simulation
        var simulation = new Simulation()
        {
            Name = "Sim",
            Children =
            [
                new Zone() { Name = "Zone1" },
                new Zone() { Name = "Zone2" },
            ]
        };

        var sim = Node.Create(simulation);
        var zone1 = sim.Children.First();
        zone1.Rename("Zone2");
        Assert.That(zone1.Name, Is.EqualTo("Zone21"));
        Assert.That(zone1.Model.Name, Is.EqualTo("Zone21"));
    }
}