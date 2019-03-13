using Models;
using Models.Core;
using Models.Core.Interfaces;
using Models.Functions;
using Models.Storage;
using NUnit.Framework;
using System;

namespace UnitTests
{

    [Serializable]
    class ModelWithLinks
    {
        [Link]
        public Zone[] zones = null;

    }

    [Serializable]
    class ModelWithIFunctions
    {
        [Link]
        public IFunction model2 = null;

    }

    [Serializable]
    class IFunctionProxy : IFunction
    {
        public double value;

        public double Value(int arrayIndex = -1)
        {
            return value;
        }
    }

    [Serializable]
    class ModelWithModelNodeLink
    {
        [Link]
        public ModelWrapper model = null;
    }

    [Serializable]
    class ModelWithScopedLinkByName
    {
        [ScopedLinkByName]
        public Zone zone2 = null;

    }

    [Serializable]
    class ModelWithScopedLink
    {
        [ScopedLink]
        public Zone zone2 = null;
    }


    [Serializable]
    class ModelWithChildLink
    {
        [ChildLink]
        public Zone zone2 = null;
    }

    [Serializable]
    class ModelWithChildLinkByName
    {
        [ChildLinkByName]
        public Zone zone2 = null;
    }

    [Serializable]
    class ModelWithParentLink : Model
    {
        [ParentLink]
        public Zone zone = null;

        [ParentLink]
        public Simulation sim = null;
    }

    [Serializable]
    class ModelWithLinkByPath : Model
    {
        [LinkByPath(Path = "[zone2].irrig1")]
        public IIrrigation irrigation1 = null;

        [LinkByPath(Path = ".Simulations.Simulation.zone2.irrig2")]
        public IIrrigation irrigation2 = null;
    }

    [Serializable]
    class ModelWithServices : Model
    {
        [Link]
        public IDataStore storage = null;

        [Link]
        public ILocator locator = null;

        [Link]
        public IEvent events = null;
    }

    [TestFixture]
    class LinksTests
    {

        /// <summary>Ensure the old style [Link] attribute still works.</summary>
        [Test]
        public void Links_EnsureOldStyleLinkWorks()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.

            Simulation simulationModel = new Simulation();
            Simulations simulationsModel = Simulations.Create(new Model[] { simulationModel });
            ModelWrapper simulations = models.Add(simulationsModel);
            ModelWrapper simulation = simulations.Add(simulationModel);

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Add(summary);
            simulation.Add(new Zone());
            simulation.Add(new Zone());

            ModelWithLinks links = new ModelWithLinks();
            simulation.Add(links);

            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulations);

            Assert.AreEqual(links.zones.Length, 2);
            Assert.NotNull(links.zones[0]);
            Assert.NotNull(links.zones[1]);
        }

        /// <summary>Ensure the old style IFunction are linked correctly i.e. treated specially.</summary>
        [Test]
        public void Links_EnsureIFunctionLinksCorrectly()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.
            Simulation simulationModel = new Simulation();
            Simulations simulationsModel = Simulations.Create(new Model[] { simulationModel });
            ModelWrapper simulations = models.Add(simulationsModel);
            ModelWrapper simulation = simulations.Add(simulationModel);

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Add(summary);
            simulation.Add(new Zone());
            simulation.Add(new Zone());

            ModelWrapper links = simulation.Add(new ModelWithIFunctions());
            simulation.Add(links);

            links.Add(new IFunctionProxy() { value = 1 }).Name = "model1";
            links.Add(new IFunctionProxy() { value = 2 }).Name = "model2";
            links.Add(new IFunctionProxy() { value = 3 }).Name = "model3";

            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulations);

            Assert.AreEqual((links.Model as ModelWithIFunctions).model2.Value(), 2);
        }

        /// <summary>Ensure a [ScopedLinkByName] works.</summary>
        [Test]
        public void Links_EnsureScopedLinkByNameWorks()
        {
            ModelWithScopedLinkByName modelWithScopedLink = new UnitTests.ModelWithScopedLinkByName();

            // Create a simulation
            ModelWrapper simulation = new ModelWrapper(new Simulation());
            simulation.Add(new Clock());
            simulation.Add(new MockSummary());
            simulation.Add(new Zone() { Name = "zone1" });
            simulation.Add(new Zone() { Name = "zone2" });
            simulation.Children[1].Add(modelWithScopedLink); // added to zone1

            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulation);

            Assert.AreEqual(modelWithScopedLink.zone2.Name, "zone2");
        }

        /// <summary>Ensure a [ScopedLink] finds the closest match</summary>
        [Test]
        public void Links_EnsureScopedLinkWorks()
        {
            ModelWithScopedLink modelWithScopedLink = new UnitTests.ModelWithScopedLink();

            // Create a simulation
            ModelWrapper simulation = new ModelWrapper(new Simulation());
            simulation.Add(new Clock());
            simulation.Add(new MockSummary());
            simulation.Add(new Zone() { Name = "zone1" });
            simulation.Add(new Zone() { Name = "zone2" });
            simulation.Children[1].Add(modelWithScopedLink); // added to zone1

            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulation);

            // Should find the closest match.
            Assert.AreEqual(modelWithScopedLink.zone2.Name, "zone1");
        }

        /// <summary>Ensure a [ChildLink] finds works</summary>
        [Test]
        public void Links_EnsureChildLinkWorks()
        {
            ModelWithChildLink modelWithChildLink = new UnitTests.ModelWithChildLink();

            // Create a simulation
            ModelWrapper simulation = new ModelWrapper(new Simulation());
            simulation.Add(new Clock());
            simulation.Add(new MockSummary());
            simulation.Add(modelWithChildLink);
            simulation.Children[2].Add(new Zone() { Name = "zone1" }); // added to modelWithChildLink

            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulation);

            // Should find zone1 as a match i.e. not use the zones name when doing a match.
            Assert.AreEqual(modelWithChildLink.zone2.Name, "zone1");

            // If we now add another child, resolve should fail as there are two matches.
            simulation.Children[2].Add(new Zone() { Name = "zone2" }); // added to modelWithChildLink
            Assert.Throws<Exception>(() => linksAlgorithm.Resolve(simulation) );
        }

        /// <summary>Ensure a [ChildLinkByName] finds works</summary>
        [Test]
        public void Links_EnsureChildLinkByNameWorks()
        {
            ModelWithChildLinkByName modelWithChildLinkByName = new UnitTests.ModelWithChildLinkByName();

            // Create a simulation
            ModelWrapper simulation = new ModelWrapper(new Simulation());
            simulation.Add(new Clock());
            simulation.Add(new MockSummary());
            simulation.Add(modelWithChildLinkByName);
            simulation.Children[2].Add(new Zone() { Name = "zone1" }); // added to modelWithChildLink
            simulation.Children[2].Add(new Zone() { Name = "zone2" }); // added to modelWithChildLink

            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulation);

            // Should find zone2 as a match as it uses the fields name.
            Assert.AreEqual(modelWithChildLinkByName.zone2.Name, "zone2");
        }

        /// <summary>Ensure a [ParentLink] works</summary>
        [Test]
        public void Links_EnsureParentLinkWorks()
        {
            ModelWithParentLink modelWithParentLink = new UnitTests.ModelWithParentLink();

            // Create a simulation
            ModelWrapper simulation = new ModelWrapper(new Simulation());
            simulation.Add(new Clock());
            simulation.Add(new MockSummary());
            simulation.Add(new Zone() { Name = "zone1" });
            simulation.Add(new Zone() { Name = "zone2" });
            simulation.Children[2].Add(modelWithParentLink); // added to zone1
            simulation.ParentAllModels();


            Links linksAlgorithm = new Links();
            linksAlgorithm.Resolve(simulation);

            // Should find the closest match.
            Assert.AreEqual(modelWithParentLink.zone.Name, "zone1");
            Assert.AreEqual(modelWithParentLink.sim.Name, "Simulation");
        }

        /// <summary>Ensure a [LinkByPath] works</summary>
        [Test]
        public void Links_EnsureLinkByPathWorks()
        {
            ModelWithLinkByPath modelWithLinkByPath = new UnitTests.ModelWithLinkByPath();

            // Create a simulation
            Simulation simulation = new Simulation();
            simulation.Children.Add(new Clock());
            simulation.Children.Add(new MockSummary());
            simulation.Children.Add(new Zone() { Name = "zone1" });
            simulation.Children.Add(new Zone() { Name = "zone2" });
            simulation.Children[2].Children.Add(modelWithLinkByPath); // added to zone1
            MockIrrigation irrig1 = new MockIrrigation() { Name = "irrig1" };
            MockIrrigation irrig2 = new MockIrrigation() { Name = "irrig2" };
            simulation.Children[3].Children.Add(irrig1); // added to zone2
            simulation.Children[3].Children.Add(irrig2); // added to zone2

            Simulations engine = Simulations.Create(new Model[] { simulation, new DataStore() });
            engine.Links.Resolve(simulation, allLinks:true);

            Assert.AreEqual(modelWithLinkByPath.irrigation1, irrig1);
            Assert.AreEqual(modelWithLinkByPath.irrigation2, irrig2);
        }

        /// <summary>Ensure link can resolve services</summary>
        [Test]
        public void Links_EnsureServicesResolve()
        {
            ModelWithServices modelWithServices = new ModelWithServices();
            Simulation simulation = new Simulation();
            simulation.Children.Add(new Clock());
            simulation.Children.Add(new MockSummary());
            simulation.Children.Add(modelWithServices);

            Simulations engine = Simulations.Create(new Model[] { simulation, new DataStore() });

            engine.Links.Resolve(simulation, allLinks:true);

            Assert.IsNotNull(modelWithServices.storage);
            Assert.IsNotNull(modelWithServices.locator);
            Assert.IsNotNull(modelWithServices.events);
        }

    }
}
