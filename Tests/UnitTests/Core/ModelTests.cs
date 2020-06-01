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
using Models.Functions;

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

            // Create a second simulation, with some scoped models.
            // This one is only used for tests involving scoped searches.
            // All other tests use the simpleModel as setup above.
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
        /// Test the <see cref="IModel.Ancestor(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameAncestor()
        {
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect null.
            Assert.Null(simpleModel.Ancestor("x"));

            // A model is not its own ancestor.
            Assert.Null(container.Ancestor("Container"));

            // No matches - expect null.
            Assert.Null(noSiblings.Ancestor("x"));
            Assert.Null(noSiblings.Ancestor(null));

            // 1 match.
            Assert.AreEqual(simpleModel, container.Ancestor("Test"));

            // When multiple ancestors match the name, ensure closest is returned.
            Assert.AreEqual(folder4, folder5.Ancestor("folder1"));

            Assert.AreEqual(container, folder5.Ancestor("Container"));
            Assert.AreEqual(container, folder4.Ancestor("Container"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.Descendant(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameDescendant()
        {
            // No children - expect null.
            Assert.Null(noSiblings.Descendant("x"));

            // No matches - expect null.
            Assert.Null(simpleModel.Descendant("x"));
            Assert.Null(simpleModel.Descendant(null));

            // 1 match.
            Assert.AreEqual(container, simpleModel.Descendant("Container"));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
            container.Children.Add(folder4);
            IModel folder5 = new Model() { Parent = folder1, Name = "folder1" };
            folder1.Children.Add(folder5);

            Assert.AreEqual(folder1, simpleModel.Descendant("folder1"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.Sibling(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingByName()
        {
            // No parent - expect null.
            Assert.Null(simpleModel.Sibling("anything"));

            // No siblings - expect null.
            Assert.Null(noSiblings.Sibling("anything"));

            // No siblings of correct name - expect null.
            Assert.Null(folder1.Sibling(null));
            Assert.Null(folder1.Sibling("x"));

            // 1 sibling of correct name.
            Assert.AreEqual(folder2, folder1.Sibling("folder2"));

            // Many siblings of correct name - expect first sibling which matches.
            // This isn't really a valid model setup but we'll test it anyway.
            folder1.Parent.Children.Add(new Folder()
            {
                Name = "folder2",
                Parent = folder1.Parent
            });
            Assert.AreEqual(folder2, folder1.Sibling("folder2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.InScope{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameScoped()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect null.
            Assert.Null(leaf1.InScope("x"));
            Assert.Null(leaf1.InScope(null));

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.InScope("zone1"));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.InScope<Plant>());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(plant1, plant1.InScope("Plant"));

            // Another interesting bug which we can reproduce here is that, because
            // the scope cache uses full paths, and plant1 and plant2 have the same
            // name and parent (and thus full path), they share each other's cache.
            //
            // We can see that plant1 is the result of plant2.InScope("Plant"):
            Assert.AreEqual(plant1, plant2.InScope("Plant"));
            // However, if we clear the cache, and then try again, the result changes:
            Apsim.ClearCaches(scopedSimulation);
            // plant2 is suddenly the first result in scope of both plant1 and 2.
            Assert.AreEqual(plant2, plant2.InScope("Plant"));
            Assert.AreEqual(plant2, plant1.InScope("Plant"));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.AreEqual(managerFolder, leaf1.InScope("asdf"));
            Assert.AreEqual(managerFolder, plant1.InScope("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[1].InScope("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[0].InScope("asdf"));
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
        /// Tests the <see cref="IModel.InScope{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeScoped()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches (there is an ISummary but no Summary) - expect null.
            Assert.Null(leaf1.InScope<Summary>());

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.InScope<Zone>());

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.InScope<Plant>());
            Assert.AreEqual(plant1, plant1.InScope<Plant>());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(plant1, plant2.InScope<Plant>());
        }

        /// <summary>
        /// Test the <see cref="IModel.Ancestor{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAncestorByTypeAndName()
        {
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect null.
            Assert.Null(simpleModel.Ancestor<IModel>(""));
            Assert.Null(simpleModel.Ancestor<IModel>(null));

            // A model is not its own ancestor.
            Assert.Null(container.Ancestor<MockModel1>(null));
            Assert.Null(container.Ancestor<MockModel1>("Container"));

            // Ancestor exists with correct type but incorrect name.
            Assert.Null(folder1.Ancestor<MockModel1>(null));
            Assert.Null(folder1.Ancestor<MockModel1>(""));

            // Ancestor exists with correct name but incorrect type.
            Assert.Null(folder1.Ancestor<MockModel2>("Container"));

            // Ancestor exists with correct type but incorrect name.
            // Another ancestor exists with correct name but incorrect type.
            Assert.Null(folder1.Ancestor<MockModel1>("Test"));

            // 1 match.
            Assert.AreEqual(container, folder1.Ancestor<MockModel1>("Container"));
            Assert.AreEqual(container, folder1.Ancestor<Model>("Container"));
            Assert.AreEqual(simpleModel, folder1.Ancestor<IModel>("Test"));

            // When multiple ancestors match, ensure closest is returned.
            Assert.AreEqual(folder4, folder5.Ancestor<Folder>("folder1"));

            Assert.AreEqual(container, folder5.Ancestor<MockModel1>("Container"));
            Assert.AreEqual(container, folder4.Ancestor<MockModel1>("Container"));

            // Test case-insensitive search.
            Assert.AreEqual(folder3, noSiblings.Ancestor<Folder>("FoLdEr3"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.Descendant{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindDescendantByTypeAndName()
        {
            // No matches - expect null.
            Assert.Null(simpleModel.Descendant<MockModel2>(""));
            Assert.Null(simpleModel.Descendant<MockModel2>("Container"));
            Assert.Null(simpleModel.Descendant<MockModel2>(null));

            // No children - expect null.
            Assert.Null(noSiblings.Descendant<IModel>(""));
            Assert.Null(noSiblings.Descendant<IModel>(null));

            // Descendant exists with correct type but incorrect name.
            Assert.Null(container.Descendant<Folder>(""));
            Assert.Null(container.Descendant<Folder>(null));

            // Descendant exists with correct name but incorrect type.
            Assert.Null(container.Descendant<MockModel2>("folder1"));

            // Descendant exists with correct type but incorrect name.
            // Another descendant exists with correct name but incorrect type.
            Assert.Null(simpleModel.Descendant<MockModel1>("folder2"));

            // 1 match.
            Assert.AreEqual(container, simpleModel.Descendant<MockModel1>("Container"));
            Assert.AreEqual(folder2, simpleModel.Descendant<Folder>("folder2"));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            Assert.AreEqual(folder1, simpleModel.Descendant<Folder>("folder1"));
            Assert.AreEqual(folder4, folder1.Descendant<Folder>("folder1"));
            Assert.AreEqual(folder5, folder4.Descendant<Folder>("folder1"));

            // Test case-insensitive search.
            Assert.AreEqual(folder2, simpleModel.Descendant<IModel>("fOLDer2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.Sibling{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingByTypeAndName()
        {
            // No parent - expect null.
            Assert.Null(simpleModel.Sibling<IModel>(""));
            Assert.Null(simpleModel.Sibling<IModel>(null));

            // No siblings - expect null.
            Assert.Null(noSiblings.Sibling<IModel>(""));
            Assert.Null(noSiblings.Sibling<IModel>(null));

            // A model is not its own sibling.
            Assert.Null(folder1.Sibling<Folder>("folder1"));

            // Sibling exists with correct name but incorrect type.
            Assert.Null(folder1.Sibling<MockModel2>("folder2"));

            // Sibling exists with correct type but incorrect name.
            Assert.Null(folder1.Sibling<Folder>(""));
            Assert.Null(folder1.Sibling<Folder>(null));

            // 1 sibling of correct type and name.
            Assert.AreEqual(folder2, folder1.Sibling<Folder>("folder2"));

            // Many siblings of correct type and name - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
            container.Children.Add(folder4);
            Assert.AreEqual(folder1, folder2.Sibling<Folder>("folder1"));
            Assert.AreEqual(folder4, folder1.Sibling<Folder>("folder1"));

            // Test case-insensitive search.
            Assert.AreEqual(folder2, folder1.Sibling<Folder>("fOlDeR2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.InScope{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindInScopeByTypeAndName()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect null.
            Assert.Null(leaf1.InScope<MockModel2>("x"));
            Assert.Null(leaf1.InScope<MockModel2>(null));

            // Model exists in scope with correct name but incorrect type.
            Assert.Null(leaf1.InScope<MockModel2>("Plant"));

            // Model exists in scope with correct type but incorrect name.
            Assert.Null(leaf1.InScope<Zone>("*"));
            Assert.Null(leaf1.InScope<Zone>(null));

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.InScope<Zone>("zone1"));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.InScope<Plant>("Plant"));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.AreEqual(managerFolder, leaf1.InScope<IModel>("asdf"));
            Assert.AreEqual(scopedSimulation, plant1.InScope<Simulation>("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[1].InScope<IModel>("asdf"));
            Assert.AreEqual(scopedSimulation.Children[0], scopedSimulation.Children[0].InScope<Clock>("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[0].InScope<IModel>("asdf"));
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
        /// Tests the <see cref="IModel.InScopeAll()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllInScope()
        {
            // Find all from the top-level model should work.
            Assert.AreEqual(14, scopedSimulation.InScopeAll().Count());

            // Find all should fail if the top-level is not scoped.
            // Can't enable this check until some refactoring of scoping code.
            //Assert.Throws<Exception>(() => simpleModel.FindAll().Count());

            // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
            // Note that the manager is not in scope. This is not desirable behaviour.
            var leaf1 = scopedSimulation.Children[2].Children[1].Children[0];
            List<IModel> inScopeOfLeaf1 = leaf1.InScopeAll().ToList();
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
            List<IModel> inScopeOfSoil = soil.InScopeAll().ToList();
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

        /// <summary>
        /// Tests for the <see cref="IModel.Siblings{T}()"/> method.
        /// </summary>
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

        /// <summary>
        /// Tests for the <see cref="IModel.InScopeAll{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeInScope()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // Test laziness. Unsure if we want to keep this.
            Assert.DoesNotThrow(() => simpleModel.InScopeAll<IModel>());

            // No matches (there is an ISummary but no Summary) -
            // expect empty enumerable (not null).
            Assert.AreEqual(0, leaf1.InScopeAll<Summary>().Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[0] }, leaf1.InScopeAll<Clock>().ToArray());

            // Many matches - test order.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel[] allPlants = new[] { plant1, plant2 };
            Assert.AreEqual(allPlants, managerFolder.InScopeAll<Plant>().ToArray());
            Assert.AreEqual(allPlants, plant1.InScopeAll<Plant>().ToArray());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(allPlants, plant2.InScopeAll<Plant>().ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.Ancestors(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameAncestors()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.Ancestors("Test").Count());
            Assert.AreEqual(0, simpleModel.Ancestors("").Count());
            Assert.AreEqual(0, simpleModel.Ancestors(null).Count());

            // No ancestors of correct name - expect empty enumerable.
            Assert.AreEqual(0, container.Ancestors("asdf").Count());
            Assert.AreEqual(0, container.Ancestors("").Count());
            Assert.AreEqual(0, container.Ancestors(null).Count());

            // 1 ancestor of correct name.
            Assert.AreEqual(new[] { simpleModel }, container.Ancestors("Test").ToArray());

            // Multiple ancestors with correct name - expect bottom-up search.
            IModel folder4 = new Folder() { Name = "folder3", Parent = folder3 };
            folder3.Children.Add(folder4);
            IModel folder5 = new Folder() { Name = "folder3", Parent = folder4 };
            folder4.Children.Add(folder5);

            Assert.AreEqual(0, folder5.Ancestors("").Count());
            Assert.AreEqual(0, folder5.Ancestors(null).Count());
            Assert.AreEqual(new[] { folder4, folder3 }, folder5.Ancestors("folder3").ToArray());
            Assert.AreEqual(new[] { simpleModel }, folder5.Ancestors("Test").ToArray());
            Assert.AreEqual(new[] { folder3 }, folder4.Ancestors("folder3").ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.Descendants(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameDescendants()
        {
            // No children - expect empty enumerable.
            Assert.AreEqual(0, noSiblings.Descendants("x").Count());

            // No descendants with correct name - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.Descendants("x").Count());
            Assert.AreEqual(0, simpleModel.Descendants(null).Count());

            // 1 descendant with correct name.
            Assert.AreEqual(new[] { container }, simpleModel.Descendants("Container").ToArray());

            // Many descendants with correct name - expect results in depth-first order.
            IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
            container.Children.Add(folder4);
            IModel folder5 = new Model() { Parent = folder1, Name = "folder1" };
            folder1.Children.Add(folder5);

            Assert.AreEqual(new[] { folder1, folder5, folder4 }, simpleModel.Descendants("folder1").ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.Siblings(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameSiblings()
        {
            // No parent - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.Siblings("Test").Count());
            Assert.AreEqual(0, simpleModel.Siblings("").Count());
            Assert.AreEqual(0, simpleModel.Siblings(null).Count());

            // No siblings - expect empty enumerable.
            Assert.AreEqual(0, noSiblings.Siblings("anything").Count());
            Assert.AreEqual(0, noSiblings.Siblings("nosiblings").Count());
            Assert.AreEqual(0, noSiblings.Siblings("").Count());
            Assert.AreEqual(0, noSiblings.Siblings(null).Count());

            // No siblings of correct name - expect empty enumerable.
            Assert.AreEqual(0, folder1.Siblings("x").Count());
            Assert.AreEqual(0, folder1.Siblings("folder1").Count());
            Assert.AreEqual(0, folder1.Siblings("").Count());
            Assert.AreEqual(0, folder1.Siblings(null).Count());

            // 1 sibling of correct name.
            Assert.AreEqual(new[] { folder2 }, folder1.Siblings("folder2").ToArray());

            // Many siblings of correct name, expect them in indexed order.
            IModel folder4 = new Folder() { Name = "folder2", Parent = container };
            folder1.Parent.Children.Add(folder4);
            Assert.AreEqual(new[] { folder2, folder4 }, folder1.Siblings("folder2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.InScopeAll(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameInScope()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect empty enumerable.
            Assert.AreEqual(0, leaf1.InScopeAll("x").Count());
            Assert.AreEqual(0, leaf1.InScopeAll("").Count());
            Assert.AreEqual(0, leaf1.InScopeAll(null).Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[2] }, leaf1.InScopeAll("zone1").ToArray());

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel clock = scopedSimulation.Children[0];
            IModel summary = scopedSimulation.Children[1];
            Assert.AreEqual(new[] { plant1, plant2 }, managerFolder.InScopeAll("Plant").ToArray());

            managerFolder.Name = "asdf";
            clock.Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, leaf1.InScopeAll("asdf").ToArray());
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, plant1.InScopeAll("asdf").ToArray());
            Assert.AreEqual(new[] { scopedSimulation, clock, managerFolder }, summary.InScopeAll("asdf").ToArray());
            Assert.AreEqual(new[] { scopedSimulation, clock, managerFolder }, clock.InScopeAll("asdf").ToArray());
        }

        /// <summary>
        /// Test the <see cref="IModel.Ancestors{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAncestorsByTypeAndName()
        {
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.Ancestors<Model>("Test").Count());
            Assert.AreEqual(0, simpleModel.Ancestors<IModel>("Test").Count());
            Assert.AreEqual(0, simpleModel.Ancestors<IModel>("").Count());
            Assert.AreEqual(0, simpleModel.Ancestors<IModel>(null).Count());

            // A model is not its own ancestor.
            Assert.AreEqual(0, container.Ancestors<MockModel1>(null).Count());
            Assert.AreEqual(0, container.Ancestors<MockModel1>("").Count());
            Assert.AreEqual(0, container.Ancestors<MockModel1>("Container").Count());
            Assert.AreEqual(0, container.Ancestors<IModel>("Container").Count());

            // Ancestor exists with correct type but incorrect name.
            Assert.AreEqual(0, folder1.Ancestors<MockModel1>(null).Count());
            Assert.AreEqual(0, folder1.Ancestors<MockModel1>("").Count());

            // Ancestor exists with correct name but incorrect type.
            Assert.AreEqual(0, folder1.Ancestors<MockModel2>("Container").Count());
            Assert.AreEqual(0, folder1.Ancestors<Fertiliser>("Test").Count());

            // Ancestor exists with correct type but incorrect name.
            // Another ancestor exists with correct name but incorrect type.
            Assert.AreEqual(0, folder1.Ancestors<MockModel1>("Test").Count());

            // 1 match.
            Assert.AreEqual(new[] { container }, folder1.Ancestors<MockModel1>("Container").ToArray());
            Assert.AreEqual(new[] { container }, folder2.Ancestors<Model>("Container").ToArray());
            Assert.AreEqual(new[] { simpleModel }, noSiblings.Ancestors<IModel>("Test").ToArray());
            Assert.AreEqual(new[] { folder3 }, noSiblings.Ancestors<IModel>("folder3").ToArray());

            // Multiple matches - ensure ordering is bottom-up.
            Assert.AreEqual(new[] { folder4, folder1 }, folder5.Ancestors<Folder>("folder1").ToArray());

            // An uncle/cousin is not an ancestor.
            folder2.Name = "folder1";
            Assert.AreEqual(new[] { folder4, folder1 }, folder5.Ancestors<Folder>("folder1").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder3 }, noSiblings.Ancestors<Folder>("FoLdEr3").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Descendants{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindDescendantsByTypeAndName()
        {
            // No matches - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.Descendants<MockModel2>("").Count());
            Assert.AreEqual(0, simpleModel.Descendants<MockModel2>("Container").Count());
            Assert.AreEqual(0, simpleModel.Descendants<MockModel2>(null).Count());

            // No children - expect enumerable.
            Assert.AreEqual(0, noSiblings.Descendants<IModel>("").Count());
            Assert.AreEqual(0, noSiblings.Descendants<IModel>(null).Count());

            // Descendants exist with correct type but incorrect name.
            Assert.AreEqual(0, container.Descendants<Folder>("").Count());
            Assert.AreEqual(0, container.Descendants<Folder>(null).Count());
            Assert.AreEqual(0, folder3.Descendants<IModel>("x").Count());

            // Descendants exist with correct name but incorrect type.
            Assert.AreEqual(0, container.Descendants<MockModel2>("folder1").Count());
            Assert.AreEqual(0, simpleModel.Descendants<Irrigation>("nosiblings").Count());

            // Descendant exists with correct type but incorrect name.
            // Another descendant exists with correct name but incorrect type.
            Assert.AreEqual(0, simpleModel.Descendants<MockModel1>("folder2").Count());
            Assert.AreEqual(0, simpleModel.Descendants<Folder>("nosiblings").Count());

            // 1 match.
            Assert.AreEqual(new[] { container }, simpleModel.Descendants<MockModel1>("Container").ToArray());
            Assert.AreEqual(new[] { folder2 }, container.Descendants<IModel>("folder2").ToArray());
            Assert.AreEqual(new[] { noSiblings }, folder3.Descendants<Model>("nosiblings").ToArray());

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            IModel folder6 = new Folder() { Name = "folder1", Parent = container };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);
            container.Children.Add(folder6);
            folder3.Name = "folder1";
            noSiblings.Name = "folder1";

            Assert.AreEqual(new[] { folder1, folder4, folder5, folder6, folder3 }, simpleModel.Descendants<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder1, folder4, folder5, folder6 }, container.Descendants<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder4, folder5 }, folder1.Descendants<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder5 }, folder4.Descendants<Folder>("folder1").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder2 }, simpleModel.Descendants<IModel>("fOLDer2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.Siblings{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingsByTypeAndName()
        {
            // No parent - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.Siblings<IModel>("Test").Count());
            Assert.AreEqual(0, simpleModel.Siblings<IModel>("").Count());
            Assert.AreEqual(0, simpleModel.Siblings<IModel>(null).Count());

            // No siblings - expect empty enumerable.
            Assert.AreEqual(0, noSiblings.Siblings<IModel>("").Count());
            Assert.AreEqual(0, noSiblings.Siblings<IModel>("nosiblings").Count());
            Assert.AreEqual(0, noSiblings.Siblings<IModel>(null).Count());

            // A model is not its own sibling.
            Assert.AreEqual(0, folder1.Siblings<Folder>("folder1").Count());
            Assert.AreEqual(0, folder1.Siblings<IModel>("folder1").Count());

            // Siblings exist with correct name but incorrect type.
            Assert.AreEqual(0, folder1.Siblings<MockModel2>("folder2").Count());
            Assert.AreEqual(0, folder2.Siblings<Fertiliser>("Container").Count());

            // Siblings exist with correct type but incorrect name.
            Assert.AreEqual(0, folder1.Siblings<Folder>("").Count());
            Assert.AreEqual(0, folder1.Siblings<Folder>(null).Count());
            Assert.AreEqual(0, container.Siblings<Folder>("folder1").Count());

            // 1 sibling of correct type and name.
            Assert.AreEqual(new[] { folder2 }, folder1.Siblings<Folder>("folder2").ToArray());

            // Many siblings of correct type and name - expect indexed order.
            IModel folder4 = new Folder() { Name = "folder1", Parent = container };
            container.Children.Add(folder4);
            Assert.AreEqual(new[] { folder1, folder4 }, folder2.Siblings<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder4 }, folder1.Siblings<Folder>("folder1").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder2 }, folder1.Siblings<Folder>("fOlDeR2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.InScopeAll{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllInScopeByTypeAndName()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel clock = scopedSimulation.Children[0];
            IModel summary = scopedSimulation.Children[1];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect null.
            Assert.AreEqual(0, leaf1.InScopeAll<MockModel2>("x").Count());
            Assert.AreEqual(0, leaf1.InScopeAll<MockModel2>(null).Count());

            // Model exists in scope with correct name but incorrect type.
            Assert.AreEqual(0, leaf1.InScopeAll<MockModel2>("Plant").Count());

            // Model exists in scope with correct type but incorrect name.
            Assert.AreEqual(0, leaf1.InScopeAll<Zone>("*").Count());
            Assert.AreEqual(0, leaf1.InScopeAll<Zone>(null).Count());
            Assert.AreEqual(0, leaf1.InScopeAll<Zone>("Plant").Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[2] }, leaf1.InScopeAll<Zone>("zone1").ToArray());

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";

            Assert.AreEqual(new[] { scopedSimulation }, plant1.InScopeAll<Simulation>("asdf").ToArray());
            Assert.AreEqual(new[] { clock }, clock.InScopeAll<Clock>("asdf").ToArray());

            // Many matches - expect first.
            Assert.AreEqual(new[] { plant1, plant2 }, managerFolder.InScopeAll<Plant>("Plant").ToArray());

            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, leaf1.InScopeAll<IModel>("asdf").ToArray());
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, plant1.InScopeAll<IModel>("asdf").ToArray());
            Assert.AreEqual(new[] { scopedSimulation, clock, managerFolder }, summary.InScopeAll<IModel>("asdf").ToArray());
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, plant2.InScopeAll<IModel>("asdf").ToArray());
        }

        // Note - this class actually has several valid parents as defined in class Model.
        private class NoValidParents : Model { }

        [ValidParent(typeof(Simulations))]
        private class DropOnSimulations : Model { }

        [ValidParent(typeof(Folder))]
        private class DropOnFolder : Model { }

        [ValidParent(DropAnywhere = true)]
        private class DropAnywhere : Model { }

        [ValidParent(DropAnywhere = true)]
        [ValidParent(DropAnywhere = false)]
        private class ConflictingDirectives : Model { }

        [ValidParent(DropAnywhere = false)]
        [ValidParent(DropAnywhere = true)]
        private class ConflictedButReversed : Model { }

        [ValidParent(DropAnywhere = true)]
        private class NotAModel { }

        [ValidParent(typeof(NoValidParents))]
        private class CanAddToNoValidParents : Model { }

        private class SomeFunction : Model, IFunction
        {
            public double Value(int arrayIndex = -1)
            {
                throw new NotImplementedException();
            }
        }

        // Don't expect to ever see something like this in the wild...
        private class SimsIFunction : Simulations, IFunction
        {
            public double Value(int arrayIndex = -1)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Tests for the <see cref="IModel.IsChildAllowable(Type)"/> method.
        /// </summary>
        [Test]
        public void TestIsChildAllowable()
        {
            IModel[] allowAnyChild = new IModel[]
            {
                new Folder(),
                new Factor(),
                new CompositeFactor(),
                new Replacements(),
            };

            foreach (IModel anyChild in allowAnyChild)
            {
                // Any Model can be added to a folder
                Assert.True(anyChild.IsChildAllowable(typeof(DropOnSimulations)));
                Assert.True(anyChild.IsChildAllowable(typeof(DropOnFolder)));
                Assert.True(anyChild.IsChildAllowable(typeof(MockModel1)));
                Assert.True(anyChild.IsChildAllowable(typeof(DropAnywhere)));
                Assert.True(anyChild.IsChildAllowable(typeof(ConflictingDirectives)));
                Assert.True(anyChild.IsChildAllowable(typeof(ConflictedButReversed)));
                Assert.True(anyChild.IsChildAllowable(typeof(NoValidParents)));
                Assert.True(anyChild.IsChildAllowable(typeof(SomeFunction)));
                Assert.True(anyChild.IsChildAllowable(typeof(SomeFunction)));

                // If it's not a model it cannot be added as a child.
                Assert.False(anyChild.IsChildAllowable(typeof(object)));

                // Even if it has a ValidParent attribute.
                Assert.False(anyChild.IsChildAllowable(typeof(NotAModel)));

                // Even if it's also an IFunction.
                Assert.False(anyChild.IsChildAllowable(typeof(SimsIFunction)));

                // Simulations object also cannot be added to anything.
                Assert.False(anyChild.IsChildAllowable(typeof(Simulations)));
            }

            // Simulations object cannot be added to anything.
            Assert.False(container.IsChildAllowable(typeof(Simulations)));
            Assert.False(folder2.IsChildAllowable(typeof(Simulations)));
            Assert.False(simpleModel.IsChildAllowable(typeof(Simulations)));
            Assert.False(new Simulations().IsChildAllowable(typeof(Simulations)));
            Assert.False(new SomeFunction().IsChildAllowable(typeof(Simulations)));

            // IFunctions can be added to anything.
            Assert.True(simpleModel.IsChildAllowable(typeof(SomeFunction)));
            Assert.True(new Simulations().IsChildAllowable(typeof(SomeFunction)));
            Assert.True(new Model().IsChildAllowable(typeof(SomeFunction)));

            // Otherwise, the validity of a child model depends on it sspecific
            // valid parents, as defined in its valid parent attributes.
            Assert.True(new NoValidParents().IsChildAllowable(typeof(CanAddToNoValidParents)));
            Assert.True(new NoValidParents().IsChildAllowable(typeof(DropAnywhere)));
            Assert.True(new Model().IsChildAllowable(typeof(DropAnywhere)));
            Assert.False(new NoValidParents().IsChildAllowable(typeof(NoValidParents)));
            Assert.False(new Model().IsChildAllowable(typeof(NoValidParents)));
            Assert.False(new CanAddToNoValidParents().IsChildAllowable(typeof(CanAddToNoValidParents)));
            Assert.True(new CanAddToNoValidParents().IsChildAllowable(typeof(DropAnywhere)));
            Assert.True(new Model().IsChildAllowable(typeof(DropAnywhere)));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindInPath(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindInPath()
        {
            // 1. Absolute paths.
            Assert.AreEqual(container, simpleModel.FindInPath(".Test.Container").Value);
            Assert.AreEqual(simpleModel, simpleModel.FindInPath(".Test").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindInPath(".Test.folder3.nosiblings").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindInPath(".Test.folder3.Children[1]").Value);
            Assert.Null(simpleModel.FindInPath(".Test.folder3.asdf"));
            Assert.Null(simpleModel.FindInPath(".asdf"));
            Assert.Null(simpleModel.FindInPath(""));
            Assert.Null(simpleModel.FindInPath(null));

            // Do not allow type names in absolute path.
            Assert.Null(simpleModel.FindInPath(".Model.Folder.Model"));

            // Absolute path with variable.
            Assert.AreEqual("folder2", simpleModel.FindInPath(".Test.Container.folder2.Name").Value);
            Assert.AreEqual(typeof(VariableComposite), simpleModel.FindInPath(".Test.Name").GetType());

            // 2. Relative paths.
            Assert.AreEqual(simpleModel, simpleModel.FindInPath("[Test]").Value);
            Assert.AreEqual(folder1, simpleModel.FindInPath("[folder1]").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindInPath("[folder3].nosiblings").Value);
            Assert.AreEqual(folder2, simpleModel.FindInPath("[Test].Container.folder2").Value);
            Assert.AreEqual(folder3, simpleModel.FindInPath("[Test].Children[2]").Value);
            Assert.Null(simpleModel.FindInPath("[asdf]"));
            Assert.Null(simpleModel.FindInPath("[folder3].foo"));

            // Do not allow type names in relative path.
            Assert.Null(simpleModel.FindInPath(".Model.Folder"));

            // Relative path with variable.
            Assert.AreEqual("Container", simpleModel.FindInPath("[Container].Name").Value);
            Assert.AreEqual(typeof(VariableComposite), simpleModel.FindInPath("[Container].Name").GetType());

            // 3. Child paths.
            Assert.AreEqual(container, simpleModel.FindInPath("Container").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindInPath("folder3.nosiblings").Value);
            Assert.AreEqual(folder2, container.FindInPath("folder2").Value);
            Assert.Null(simpleModel.FindInPath("folder2"));
            Assert.Null(simpleModel.FindInPath("x"));
            Assert.Null(simpleModel.FindInPath(""));
            Assert.Null(simpleModel.FindInPath(null));
        }
    }
}
