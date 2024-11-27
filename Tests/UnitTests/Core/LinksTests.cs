using Models;
using Models.Core;
using Models.Core.Interfaces;
using Models.Functions;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UnitTests.Core
{

    [Serializable]
    class ModelWithLinks : Model
    {
        [Link]
        public Zone[] zones = null;

    }

    [Serializable]
    class ModelWithIFunctions : Model
    {
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction model2 = null;

    }

    [Serializable]
    class IFunctionProxy : Model, IFunction
    {
        public double value;

        public double Value(int arrayIndex = -1)
        {
            return value;
        }
    }

    [Serializable]
    class ModelWithScopedLinkByName : Model
    {
        [Link(ByName = true)]
        public Zone zone2 = null;

    }

    [Serializable]
    class ModelWithScopedLink : Model
    {
        [Link]
        public Zone zone2 = null;
    }


    [Serializable]
    class ModelWithChildLink : Model
    {
        [Link(Type = LinkType.Child)]
        public Zone zone2 = null;
    }

    [Serializable]
    class ModelWithChildLinkByName : Model
    {
        [Link(Type = LinkType.Child, ByName = true)]
        public Zone zone2 = null;
    }

    [Serializable]
    class ModelWithParentLink : Model
    {
        [Link(Type = LinkType.Ancestor)]
        public Zone zone = null;

        [Link(Type = LinkType.Ancestor)]
        public Simulation sim = null;
    }

    [Serializable]
    class ModelWithLinkByPath : Model
    {
        [Link(Type = LinkType.Path, Path = "[zone2].irrig1")]
        public IIrrigation irrigation1 = null;

        [Link(Type = LinkType.Path, Path = ".Simulation.zone2.irrig2")]
        public IIrrigation irrigation2 = null;
    }

    [Serializable]
    class ModelWithServices : Model
    {
        [Link]
        public IDataStore storage = null;

        [Link]
        public IEvent events = null;
    }

    [TestFixture]
    class LinksTests
    {

        /// <summary>Ensure the old style [Link] attribute still works.</summary>
        [Test]
        public void EnsureOldStyleLinkWorks()
        {
            var sim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new Zone(),
                    new Zone(),
                    new ModelWithLinks()
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            var modelWithLinks = sim.Children[4] as ModelWithLinks;
            Assert.That(modelWithLinks.zones.Length, Is.EqualTo(2));
            Assert.That(modelWithLinks.zones[0], Is.Not.Null);
            Assert.That(modelWithLinks.zones[1], Is.Not.Null);
        }

        /// <summary>Ensure the old style IFunction are linked correctly i.e. treated specially.</summary>
        [Test]
        public void EnsureIFunctionLinksCorrectly()
        {
            var sim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new Zone(),
                    new Zone(),
                    new ModelWithIFunctions()
                    {
                        Children = new List<IModel>()
                        {
                            new IFunctionProxy()
                            {
                                value = 1,
                                Name = "model1"
                            },
                            new IFunctionProxy()
                            {
                                value = 2,
                                Name = "model2"
                            },
                            new IFunctionProxy()
                            {
                                value = 3,
                                Name = "model3"
                            }
                        }
                    }
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            var model = sim.Children[4] as ModelWithIFunctions;

            Assert.That(model.model2.Value(), Is.EqualTo(2));
        }

        /// <summary>Ensure a [Link(ByName = true)] works.</summary>
        [Test]
        public void EnsureScopedLinkByNameWorks()
        {
            var sim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new Zone() { Name = "zone1" },
                    new Zone() { Name = "zone2" },
                    new ModelWithScopedLinkByName()
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            var model = sim.Children[4] as ModelWithScopedLinkByName;
            Assert.That(model.zone2.Name, Is.EqualTo("zone2"));
        }

        /// <summary>Ensure a [Link] finds the closest match</summary>
        [Test]
        public void EnsureScopedLinkWorks()
        {
            var sim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new Zone() { Name = "zone1" },
                    new Zone() { Name = "zone2" },
                    new ModelWithScopedLink()
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            // Should find the closest match.
            var model = sim.Children[4] as ModelWithScopedLink;
            Assert.That(model.zone2.Name, Is.EqualTo("zone1"));
        }

        /// <summary>Ensure a [Link(Type = LinkType.Child)] finds works</summary>
        [Test]
        public void EnsureChildLinkWorks()
        {
            var sim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new ModelWithChildLink()
                    {
                        Children = new List<IModel>()
                        {
                            new Zone() { Name = "zone1" },
                        }
                    },
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            // Should find zone1 as a match i.e. not use the zones name when doing a match.
            var model = sim.Children[2] as ModelWithChildLink;
            Assert.That(model.zone2.Name, Is.EqualTo("zone1"));

            // If we now add another child, resolve should fail as there are two matches.
            model.Children.Add(new Zone() { Name = "zone2" }); // added to modelWithChildLink
            sim.ParentAllDescendants();
            Assert.Throws<Exception>(() =>
            {
                links.Resolve(sim, true);
            });
        }

        /// <summary>Ensure a [Link(Type = LinkType.Child, ByName = true)] finds works</summary>
        [Test]
        public void EnsureChildLinkByNameWorks()
        {
            var sim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new MockSummary(),
                    new ModelWithChildLinkByName()
                    {
                        Children = new List<IModel>()
                        {
                            new Zone() { Name = "zone1" },
                            new Zone() { Name = "zone2" }
                        }
                    },
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            // Should find zone2 as a match as it uses the fields name.
            var model = sim.Children[2] as ModelWithChildLinkByName;
            Assert.That(model.zone2.Name, Is.EqualTo("zone2"));
        }

        /// <summary>Ensure a [Link(Type = LinkType.Ancestor)] works</summary>
        [Test]
        public void EnsureParentLinkWorks()
        {
            var sim = new Simulation()
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
                            new ModelWithParentLink()
                        }
                    },
                    new Zone() { Name = "zone2" }
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            // Should find the closest match.
            var model = sim.Children[2].Children[0] as ModelWithParentLink;
            Assert.That(model.zone.Name, Is.EqualTo("zone1"));
            Assert.That(model.sim.Name, Is.EqualTo("Simulation"));
        }

        /// <summary>Ensure a [LinkByPath] works</summary>
        [Test]
        public void EnsureLinkByPathWorks()
        {
            var sim = new Simulation()
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
                            new ModelWithLinkByPath()
                        }
                    },
                    new Zone()
                    {
                        Name = "zone2",
                        Children = new List<IModel>()
                        {
                            new MockIrrigation() { Name = "irrig1" },
                            new MockIrrigation() { Name = "irrig2" }
                        }
                    },
                }
            };
            sim.ParentAllDescendants();

            var links = new Links();
            links.Resolve(sim, true);

            var model = sim.Children[2].Children[0] as ModelWithLinkByPath;
            var zone2 = sim.Children[3];
            Assert.That(zone2.Children[0], Is.EqualTo(model.irrigation1));
            Assert.That(zone2.Children[1], Is.EqualTo(model.irrigation2));
        }

        /// <summary>Ensure link can resolve services</summary>
        [Test]
        public void EnsureServicesResolve()
        {
            var simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new DataStore(),
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock(),
                            new MockSummary(),
                            new ModelWithServices()
                            
                        }
                    }
                 }
            };
            simulations.ParentAllDescendants();

            var links = new Links();
            links.Resolve(simulations.Children[1], true);

            var modelWithServices = simulations.Children[1].Children[2] as ModelWithServices;
            Assert.That(modelWithServices.storage, Is.Not.Null);
            Assert.That(modelWithServices.Locator, Is.Not.Null);
            Assert.That(modelWithServices.events, Is.Not.Null);
        }

    }
}
