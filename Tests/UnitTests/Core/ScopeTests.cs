namespace UnitTests.Core
{
    using Models;
    using Models.Core;
    using Models.PMF;
    using Models.PMF.Organs;
    using Models.Soils;
    using NUnit.Framework;
    using System.Collections.Generic;

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
                Children = new List<Model>()
                {
                    new Clock(),
                    new MockSummary(),
                    new Zone()
                    {
                        Name = "zone1",
                        Children = new List<Model>()
                        {
                            new Soil(),
                            new Plant()
                            {
                                Children = new List<Model>()
                                {
                                    new Leaf() { Name = "leaf1" },
                                    new GenericOrgan() { Name = "stem1" }
                                }
                            },
                            new Plant()
                            {
                                Children = new List<Model>()
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
            Apsim.ParentAllChildren(simulation);

            // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
            var leaf1 = simulation.Children[2].Children[1].Children[0];
            List<IModel> inScopeOfLeaf1 = Apsim.FindAll(leaf1);
            Assert.AreEqual(inScopeOfLeaf1.Count, 10);
            Assert.AreEqual(inScopeOfLeaf1[0].Name, "Plant");
            Assert.AreEqual(inScopeOfLeaf1[1].Name, "leaf1");
            Assert.AreEqual(inScopeOfLeaf1[2].Name, "stem1");
            Assert.AreEqual(inScopeOfLeaf1[3].Name, "zone1");
            Assert.AreEqual(inScopeOfLeaf1[4].Name, "Soil");
            Assert.AreEqual(inScopeOfLeaf1[5].Name, "Plant");
            Assert.AreEqual(inScopeOfLeaf1[6].Name, "Simulation");
            Assert.AreEqual(inScopeOfLeaf1[7].Name, "Clock");
            Assert.AreEqual(inScopeOfLeaf1[8].Name, "MockSummary");
            Assert.AreEqual(inScopeOfLeaf1[9].Name, "zone2");

            // Ensure correct scoping from soil
            var soil = simulation.Children[2].Children[0];
            List<IModel> inScopeOfSoil = Apsim.FindAll(soil);
            Assert.AreEqual(inScopeOfSoil.Count, 12);
            Assert.AreEqual(inScopeOfSoil[0].Name, "zone1");
            Assert.AreEqual(inScopeOfSoil[1].Name, "Soil");
            Assert.AreEqual(inScopeOfSoil[2].Name, "Plant");
            Assert.AreEqual(inScopeOfSoil[3].Name, "leaf1");
            Assert.AreEqual(inScopeOfSoil[4].Name, "stem1");
            Assert.AreEqual(inScopeOfSoil[5].Name, "Plant");
            Assert.AreEqual(inScopeOfSoil[6].Name, "leaf2");
            Assert.AreEqual(inScopeOfSoil[7].Name, "stem2");
            Assert.AreEqual(inScopeOfSoil[8].Name, "Simulation");
            Assert.AreEqual(inScopeOfSoil[9].Name, "Clock");
            Assert.AreEqual(inScopeOfSoil[10].Name, "MockSummary");
            Assert.AreEqual(inScopeOfSoil[11].Name, "zone2");
        }
    }
}
