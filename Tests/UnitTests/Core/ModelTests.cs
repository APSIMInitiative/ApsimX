using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Models;
using Models.Factorial;
using Models.PMF;
using Models.PMF.Organs;

namespace UnitTests.Core
{
    [TestFixture]
    public class ModelTests
    {
        private class MockModel1 : Model { }
        private class MockModel2 : Model { }

        private IModel simpleModel;
        private IModel scopedSimulation;

        // Convenience accessors for some of the models under simpleModel.
        private IModel container;
        private IModel folder1;
        private IModel folder2;
        private IModel folder3;
        private IModel noSiblings;

        /// <summary>
        /// Start up code for all tests.
        /// </summary>
        [SetUp]
        public void Initialise()
        {
            simpleModel = new Model()
            {
                Name = "Test",
                Children = new List<IModel>()
                {
                    new MockModel1()
                    {
                        Name = "Container",
                        Children = new List<IModel>()
                        {
                            new Folder()
                            {
                                Name = "folder1"
                            },
                            new Folder()
                            {
                                Name = "folder2"
                            }
                        }
                    },
                    new Folder()
                    {
                        Name = "folder3",
                        Children = new List<IModel>()
                        {
                            new Model()
                            {
                                Name = "nosiblings"
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(simpleModel);


            container = simpleModel.Children[0];
            folder1 = container.Children[0];
            folder2 = container.Children[1];
            folder3 = simpleModel.Children[1];
            noSiblings = folder3.Children[0];

            // Create a simulation
            scopedSimulation = new Simulation()
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
                                Children = new List<IModel>()
                                {
                                    new Leaf() { Name = "leaf1" },
                                    new GenericOrgan() { Name = "stem1" }
                                }
                            },
                            new Plant()
                            {
                                Children = new List<IModel>()
                                {
                                    new Leaf() { Name = "leaf2" },
                                    new GenericOrgan() { Name = "stem2" }
                                }
                            },
                            new Folder()
                            {
                                Name = "managerfolder",
                                Children = new List<IModel>()
                                {
                                    new Model()
                                    {
                                        Name = "manager"
                                    }
                                }
                            }
                        }

                    },
                    new Zone() { Name = "zone2" }
                }
            };
            Apsim.ParentAllChildren(scopedSimulation);
        }

        /// <summary>
        /// Tests the for the FullPath property.
        /// </summary>
        [Test]
        public void TestFullPath()
        {
            Assert.AreEqual(".Test", simpleModel.FullPath);
            Assert.AreEqual(".Test.Container.folder1", folder1.FullPath);
            Assert.AreEqual(".Test.folder3.nosiblings", noSiblings.FullPath);
        }

        /// <summary>
        /// Test the <see cref="IModel.Ancestor{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeAncestor()
        {
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect null.
            Assert.Null(simpleModel.Ancestor<IModel>());

            // A model is not its own ancestor.
            Assert.Null(container.Ancestor<MockModel1>());

            Assert.AreEqual(simpleModel, container.Ancestor<IModel>());

            // When multiple ancestors match the type, ensure closest is returned.
            Assert.AreEqual(folder4, folder5.Ancestor<Folder>());

            Assert.AreEqual(container, folder5.Ancestor<MockModel1>());
            Assert.AreEqual(container, folder4.Ancestor<MockModel1>());

            // Searching for any IModel ancestor should return the node's parent.
            Assert.AreEqual(folder1.Parent, folder1.Ancestor<IModel>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Descendant{T}"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeDescendant()
        {
            // No matches - expect null.
            Assert.Null(simpleModel.Descendant<MockModel2>());

            // No children - expect null.
            Assert.Null(noSiblings.Descendant<IModel>());

            // 1 match.
            Assert.AreEqual(container, simpleModel.Descendant<MockModel1>());

            // Many matches - expect first in depth-first search is returned.
            Assert.AreEqual(folder1, simpleModel.Descendant<Folder>());
            Assert.AreEqual(container, simpleModel.Descendant<IModel>());
        }

        [Test]
        public void TestFindByTypeSibling()
        {
            // No parent - expect null.
            Assert.Null(simpleModel.Sibling<IModel>());

            // No siblings - expect null.
            Assert.Null(noSiblings.Sibling<IModel>());

            // No siblings of correct type - expect null.
            Assert.Null(folder1.Sibling<MockModel2>());

            // 1 sibling of correct type.
            Assert.AreEqual(folder2, folder1.Sibling<Folder>());

            // Many siblings of correct type - expect first sibling which matches.
            folder1.Parent.Children.Add(new Folder()
            {
                Name = "folder4",
                Parent = folder1.Parent
            });
            Assert.AreEqual(folder2, folder1.Sibling<Folder>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Find{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeScoped()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches (there is an ISummary but no Summary) - expect null.
            Assert.Null(leaf1.Find<Summary>());

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.Find<Zone>());

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.Find<Plant>());
            Assert.AreEqual(plant1, plant1.Find<Plant>());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(plant1, plant2.Find<Plant>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Ancestors()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllAncestors()
        {
            // The top-level model has no ancestors.
            Assert.AreEqual(new IModel[0], simpleModel.Ancestors().ToArray());

            Assert.AreEqual(new[] { simpleModel }, container.Ancestors().ToArray());

            // Ancestors should be in bottom-up order.
            Assert.AreEqual(new[] { folder3, simpleModel }, noSiblings.Ancestors().ToArray());

            // Note this test may break if we implement caching for this function.
            container.Parent = null;
            Assert.AreEqual(new[] { container }, folder1.Ancestors());

            // This will create infinite recursion. However this should not
            // cause an error in and of itself, due to the lazy implementation.
            container.Parent = folder1;
            Assert.DoesNotThrow(() => folder1.Ancestors());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Descendants()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllDescendants()
        {
            // No children - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.Descendants().Count());

            // Descendants should be in depth-first search order.
            Assert.AreEqual(new[] { container, folder1, folder2, folder3, noSiblings }, simpleModel.Descendants().ToArray());
            Assert.AreEqual(new[] { folder1, folder2 }, container.Descendants().ToArray());
            Assert.AreEqual(new[] { noSiblings }, folder3.Descendants().ToArray());

            // This will create infinite recursion. However this should not
            // cause an error in and of itself, due to the lazy implementation.
            folder1.Children.Add(folder1);
            Assert.DoesNotThrow(() => folder1.Descendants());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Siblings()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllSiblings()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.Siblings().Count());

            // No siblings - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.Siblings().Count());

            // 1 sibling.
            Assert.AreEqual(new[] { folder2 }, folder1.Siblings().ToArray());

            // Many siblings.
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
            IModel test = new Model() { Name = "test", Parent = folder1.Parent };
            folder1.Parent.Children.Add(folder4);
            folder1.Parent.Children.Add(test);
            Assert.AreEqual(new[] { folder2, folder4, test }, folder1.Siblings().ToArray());
        }


        /// <summary>
        /// Tests the <see cref="IModel.FindAll()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllInScope()
        {
            // Find all from the top-level model should work.
            Assert.AreEqual(14, scopedSimulation.FindAll().Count());

            // Find all should fail if the top-level is not scoped.
            // Can't enable this check until some refactoring of scoping code.
            //Assert.Throws<Exception>(() => simpleModel.FindAll().Count());

            // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
            // Note that the manager is not in scope. This is not desirable behaviour.
            var leaf1 = scopedSimulation.Children[2].Children[1].Children[0];
            List<IModel> inScopeOfLeaf1 = leaf1.FindAll().ToList();
            Assert.AreEqual(inScopeOfLeaf1.Count, 11);
            Assert.AreEqual("Plant", inScopeOfLeaf1[0].Name);
            Assert.AreEqual("leaf1", inScopeOfLeaf1[1].Name);
            Assert.AreEqual("stem1", inScopeOfLeaf1[2].Name);
            Assert.AreEqual("zone1", inScopeOfLeaf1[3].Name);
            Assert.AreEqual("Soil", inScopeOfLeaf1[4].Name);
            Assert.AreEqual("Plant", inScopeOfLeaf1[5].Name);
            Assert.AreEqual("managerfolder", inScopeOfLeaf1[6].Name);
            Assert.AreEqual("Simulation", inScopeOfLeaf1[7].Name);
            Assert.AreEqual("Clock", inScopeOfLeaf1[8].Name);
            Assert.AreEqual("MockSummary", inScopeOfLeaf1[9].Name);
            Assert.AreEqual("zone2", inScopeOfLeaf1[10].Name);

            // Ensure correct scoping from soil
            var soil = scopedSimulation.Children[2].Children[0];
            List<IModel> inScopeOfSoil = soil.FindAll().ToList();
            Assert.AreEqual(inScopeOfSoil.Count, 14);
            Assert.AreEqual("zone1", inScopeOfSoil[0].Name);
            Assert.AreEqual("Soil", inScopeOfSoil[1].Name);
            Assert.AreEqual("Plant", inScopeOfSoil[2].Name);
            Assert.AreEqual("leaf1", inScopeOfSoil[3].Name);
            Assert.AreEqual("stem1", inScopeOfSoil[4].Name);
            Assert.AreEqual("Plant", inScopeOfSoil[5].Name);
            Assert.AreEqual("leaf2", inScopeOfSoil[6].Name);
            Assert.AreEqual("stem2", inScopeOfSoil[7].Name);
            Assert.AreEqual("managerfolder", inScopeOfSoil[8].Name);
            Assert.AreEqual("manager", inScopeOfSoil[9].Name);
            Assert.AreEqual("Simulation", inScopeOfSoil[10].Name);
            Assert.AreEqual("Clock", inScopeOfSoil[11].Name);
            Assert.AreEqual("MockSummary", inScopeOfSoil[12].Name);
            Assert.AreEqual("zone2", inScopeOfSoil[13].Name);
        }

        /// <summary>
        /// Tests the <see cref="IModel.Ancestors{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeAncestors()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.Ancestors<IModel>().Count());

            // Container has no MockModel2 ancestors.
            Assert.AreEqual(0, container.Ancestors<MockModel2>().Count());

            // Container's only ancestor is simpleModel.
            Assert.AreEqual(new[] { simpleModel }, container.Ancestors<IModel>().ToArray());
            Assert.AreEqual(new[] { simpleModel }, container.Ancestors<Model>().ToArray());

            IModel folder4 = new Folder() { Name = "folder4", Parent = folder3 };
            folder3.Children.Add(folder4);
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder4.Children.Add(folder5);

            Assert.AreEqual(0, folder5.Ancestors<MockModel2>().Count());
            Assert.AreEqual(new[] { folder4, folder3 }, folder5.Ancestors<Folder>().ToArray());
            Assert.AreEqual(new[] { folder4, folder3, simpleModel }, folder5.Ancestors<IModel>().ToArray());
            Assert.AreEqual(new[] { folder4, folder3, simpleModel }, folder5.Ancestors<Model>().ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Descendants{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeDescendants()
        {
            // No children - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.Descendants<IModel>().Count());

            // No matches - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.Descendants<MockModel2>().Count());

            // 1 match.
            Assert.AreEqual(new[] { simpleModel.Children[0] }, simpleModel.Descendants<MockModel1>().ToArray());

            // Many matches - expect depth-first search.
            Assert.AreEqual(new[] { folder1, folder2, folder3 }, simpleModel.Descendants<Folder>().ToArray());
            Assert.AreEqual(new[] { container, folder1, folder2, folder3, noSiblings }, simpleModel.Descendants<IModel>().ToArray());
        }

        [Test]
        public void TestFindAllByTypeSiblings()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.Siblings<IModel>().Count());

            // No siblings - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.Siblings<IModel>().Count());

            // No siblings of correct type - expect empty enumerable (not null).
            Assert.AreEqual(0, folder1.Siblings<MockModel2>().Count());

            // 1 sibling of correct type.
            Assert.AreEqual(new[] { folder2 }, folder1.Siblings<Folder>().ToArray());

            // Many siblings of correct type - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
            IModel test = new Model() { Name = "test", Parent = folder1.Parent };
            folder1.Parent.Children.Add(folder4);
            folder1.Parent.Children.Add(test);
            Assert.AreEqual(new[] { folder2, folder4 }, folder1.Siblings<Folder>().ToArray());
            Assert.AreEqual(new[] { folder2, folder4, test }, folder1.Siblings<IModel>().ToArray());
        }

        [Test]
        public void TestFindAllByTypeInScope()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // Test laziness. Unsure if we want to keep this.
            Assert.DoesNotThrow(() => simpleModel.FindAll<IModel>());

            // No matches (there is an ISummary but no Summary) -
            // expect empty enumerable (not null).
            Assert.AreEqual(0, leaf1.FindAll<Summary>().Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[0] }, leaf1.FindAll<Clock>().ToArray());

            // Many matches - test order.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel[] allPlants = new[] { plant1, plant2 };
            Assert.AreEqual(allPlants, managerFolder.FindAll<Plant>().ToArray());
            Assert.AreEqual(allPlants, plant1.FindAll<Plant>().ToArray());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(allPlants, plant2.FindAll<Plant>().ToArray());
        }
    }
}
