using Models;
using Models.Core;
using Models.PMF;
using Models.PMF.Organs;
using Models.Soils;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests
{

    [TestFixture]
    class ScopeTests
    {

        /// <summary>
        /// FindAll method tests.
        /// </summary>
        [Test]
        public void Scope_EnsureFindAllWorks()
        {
            ModelWithParentLink modelWithParentLink = new UnitTests.ModelWithParentLink();

            // Create a simulation
            ModelWrapper simulation = new ModelWrapper(new Simulation());
            simulation.Add(new Clock());
            simulation.Add(new MockSummary());
            simulation.Add(new Zone() { Name = "zone1" });
            simulation.Add(new Zone() { Name = "zone2" });
            simulation.Children[2].Add(new Soil());                                        // added to zone1
            simulation.Children[2].Add(new Plant());                                       // added to zone1
            simulation.Children[2].Children[1].Add(new Leaf() { Name = "leaf1" });         // added to plant1
            simulation.Children[2].Children[1].Add(new GenericOrgan() { Name = "stem1" }); // added to plant1
            simulation.Children[2].Add(new Plant());                                       // added to zone1
            simulation.Children[2].Children[2].Add(new Leaf() { Name = "leaf2" });         // added to plant2
            simulation.Children[2].Children[2].Add(new GenericOrgan() { Name = "stem2" }); // added to plant2
            simulation.ParentAllModels();

            // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
            List<IModel> inScopeOfLeaf1 = Apsim.FindAll(simulation.Children[2].Children[1].Children[0].Model as IModel);
            Assert.AreEqual(inScopeOfLeaf1.Count, 9);
            Assert.AreEqual(inScopeOfLeaf1[0].Name, "Plant");
            Assert.AreEqual(inScopeOfLeaf1[1].Name, "leaf1");
            Assert.AreEqual(inScopeOfLeaf1[2].Name, "stem1");
            Assert.AreEqual(inScopeOfLeaf1[3].Name, "zone1");
            Assert.AreEqual(inScopeOfLeaf1[4].Name, "Soil");
            Assert.AreEqual(inScopeOfLeaf1[5].Name, "Plant");
            Assert.AreEqual(inScopeOfLeaf1[6].Name, "Simulation");
            Assert.AreEqual(inScopeOfLeaf1[7].Name, "Clock");
            Assert.AreEqual(inScopeOfLeaf1[8].Name, "zone2");

            // Ensure correct scoping from soil
            List<IModel> inScopeOfSoil = Apsim.FindAll(simulation.Children[2].Children[0].Model as IModel);
            Assert.AreEqual(inScopeOfSoil.Count, 11);
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
            Assert.AreEqual(inScopeOfSoil[10].Name, "zone2");
        }


    }
}
