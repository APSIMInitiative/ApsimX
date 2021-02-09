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
        private interface IInterface { }
        private class MockModel1 : Model, IInterface { }
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
            simpleModel.ParentAllDescendants();


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
            scopedSimulation.ParentAllDescendants();
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
        /// Test the <see cref="IModel.FindAncestor(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameAncestor()
        {
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect null.
            Assert.Null(simpleModel.FindAncestor("x"));

            // A model is not its own ancestor.
            Assert.Null(container.FindAncestor("Container"));

            // No matches - expect null.
            Assert.Null(noSiblings.FindAncestor("x"));
            Assert.Null(noSiblings.FindAncestor(null));

            // 1 match.
            Assert.AreEqual(simpleModel, container.FindAncestor("Test"));

            // When multiple ancestors match the name, ensure closest is returned.
            Assert.AreEqual(folder4, folder5.FindAncestor("folder1"));

            Assert.AreEqual(container, folder5.FindAncestor("Container"));
            Assert.AreEqual(container, folder4.FindAncestor("Container"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindDescendant(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameDescendant()
        {
            // No children - expect null.
            Assert.Null(noSiblings.FindDescendant("x"));

            // No matches - expect null.
            Assert.Null(simpleModel.FindDescendant("x"));
            Assert.Null(simpleModel.FindDescendant(null));

            // 1 match.
            Assert.AreEqual(container, simpleModel.FindDescendant("Container"));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
            container.Children.Add(folder4);
            IModel folder5 = new Model() { Parent = folder1, Name = "folder1" };
            folder1.Children.Add(folder5);

            Assert.AreEqual(folder1, simpleModel.FindDescendant("folder1"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindSibling(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingByName()
        {
            // No parent - expect null.
            Assert.Null(simpleModel.FindSibling("anything"));

            // No siblings - expect null.
            Assert.Null(noSiblings.FindSibling("anything"));

            // No siblings of correct name - expect null.
            Assert.Null(folder1.FindSibling(null));
            Assert.Null(folder1.FindSibling("x"));

            // 1 sibling of correct name.
            Assert.AreEqual(folder2, folder1.FindSibling("folder2"));

            // Many siblings of correct name - expect first sibling which matches.
            // This isn't really a valid model setup but we'll test it anyway.
            folder1.Parent.Children.Add(new Folder()
            {
                Name = "folder2",
                Parent = folder1.Parent
            });
            Assert.AreEqual(folder2, folder1.FindSibling("folder2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindChild(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindChildByName()
        {
            // No children - expect null.
            Assert.Null(noSiblings.FindChild("*"));
            Assert.Null(noSiblings.FindChild(""));
            Assert.Null(noSiblings.FindChild(null));

            // No children of correct name - expect null.
            Assert.Null(container.FindChild(null));
            Assert.Null(folder3.FindChild("x"));
            Assert.Null(simpleModel.FindChild("folder2"));

            // 1 child of correct name.
            Assert.AreEqual(folder2, container.FindChild("folder2"));
            Assert.AreEqual(folder3, simpleModel.FindChild("folder3"));

            // Many children of correct name - expect first child which matches.
            // This isn't really a valid model setup but we'll test it anyway.
            container.Children.Add(new Folder()
            {
                Name = "folder2",
                Parent = folder1.Parent
            });
            Assert.AreEqual(folder2, container.FindChild("folder2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindInScope{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameScoped()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect null.
            Assert.Null(leaf1.FindInScope("x"));
            Assert.Null(leaf1.FindInScope(null));

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.FindInScope("zone1"));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.FindInScope<Plant>());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(plant1, plant1.FindInScope("Plant"));

            // Another interesting bug which we can reproduce here is that, because
            // the scope cache uses full paths, and plant1 and plant2 have the same
            // name and parent (and thus full path), they share each other's cache.
            //
            // We can see that plant1 is the result of plant2.InScope("Plant"):
            Assert.AreEqual(plant1, plant2.FindInScope("Plant"));
            // However, if we clear the cache, and then try again, the result changes:
            Apsim.ClearCaches(scopedSimulation);
            // plant2 is suddenly the first result in scope of both plant1 and 2.
            Assert.AreEqual(plant2, plant2.FindInScope("Plant"));
            Assert.AreEqual(plant2, plant1.FindInScope("Plant"));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.AreEqual(managerFolder, leaf1.FindInScope("asdf"));
            Assert.AreEqual(managerFolder, plant1.FindInScope("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[1].FindInScope("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[0].FindInScope("asdf"));
        }

        /// <summary>
        /// Test the <see cref="IModel.FindAncestor{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeAncestor()
        {
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect null.
            Assert.Null(simpleModel.FindAncestor<IModel>());

            // A model is not its own ancestor.
            Assert.Null(container.FindAncestor<MockModel1>());

            Assert.AreEqual(simpleModel, container.FindAncestor<IModel>());

            // When multiple ancestors match the type, ensure closest is returned.
            Assert.AreEqual(folder4, folder5.FindAncestor<Folder>());

            Assert.AreEqual(container, folder5.FindAncestor<MockModel1>());
            Assert.AreEqual(container, folder4.FindAncestor<MockModel1>());

            // Searching for any IModel ancestor should return the node's parent.
            Assert.AreEqual(folder1.Parent, folder1.FindAncestor<IModel>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindDescendant{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeDescendant()
        {
            // No matches - expect null.
            Assert.Null(simpleModel.FindDescendant<MockModel2>());

            // No children - expect null.
            Assert.Null(noSiblings.FindDescendant<IModel>());

            // 1 match.
            Assert.AreEqual(container, simpleModel.FindDescendant<MockModel1>());
            Assert.AreEqual(container, simpleModel.FindDescendant<IInterface>());

            // Many matches - expect first in depth-first search is returned.
            Assert.AreEqual(folder1, simpleModel.FindDescendant<Folder>());
            Assert.AreEqual(container, simpleModel.FindDescendant<IModel>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindSibling{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeSibling()
        {
            // No parent - expect null.
            Assert.Null(simpleModel.FindSibling<IModel>());

            // No siblings - expect null.
            Assert.Null(noSiblings.FindSibling<IModel>());

            // No siblings of correct type - expect null.
            Assert.Null(folder1.FindSibling<MockModel2>());

            // 1 sibling of correct type.
            Assert.AreEqual(folder2, folder1.FindSibling<Folder>());

            // Many siblings of correct type - expect first sibling which matches.
            folder1.Parent.Children.Add(new Folder()
            {
                Name = "folder4",
                Parent = folder1.Parent
            });
            Assert.AreEqual(folder2, folder1.FindSibling<Folder>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindChild{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeChild()
        {
            // No children - expect null.
            Assert.Null(folder1.FindChild<IModel>());
            Assert.Null(noSiblings.FindChild<Model>());

            // No children of correct type - expect null.
            Assert.Null(simpleModel.FindChild<MockModel2>());
            Assert.Null(folder3.FindChild<MockModel1>());

            // 1 child of correct type.
            Assert.AreEqual(container, simpleModel.FindChild<MockModel1>());
            Assert.AreEqual(noSiblings, folder3.FindChild<Model>());

            // Many children of correct type - expect first sibling which matches.
            Assert.AreEqual(folder1, container.FindChild<Folder>());
            Assert.AreEqual(folder1, container.FindChild<Model>());
            Assert.AreEqual(container, simpleModel.FindChild<IModel>());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindInScope{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeScoped()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches (there is an ISummary but no Summary) - expect null.
            Assert.Null(leaf1.FindInScope<Summary>());

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.FindInScope<Zone>());

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.FindInScope<Plant>());
            Assert.AreEqual(plant1, plant1.FindInScope<Plant>());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(plant1, plant2.FindInScope<Plant>());
        }

        /// <summary>
        /// Test the <see cref="IModel.FindAncestor{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAncestorByTypeAndName()
        {
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect null.
            Assert.Null(simpleModel.FindAncestor<IModel>(""));
            Assert.Null(simpleModel.FindAncestor<IModel>(null));

            // A model is not its own ancestor.
            Assert.Null(container.FindAncestor<MockModel1>(null));
            Assert.Null(container.FindAncestor<MockModel1>("Container"));

            // Ancestor exists with correct type but incorrect name.
            Assert.Null(folder1.FindAncestor<MockModel1>(null));
            Assert.Null(folder1.FindAncestor<MockModel1>(""));

            // Ancestor exists with correct name but incorrect type.
            Assert.Null(folder1.FindAncestor<MockModel2>("Container"));

            // Ancestor exists with correct type but incorrect name.
            // Another ancestor exists with correct name but incorrect type.
            Assert.Null(folder1.FindAncestor<MockModel1>("Test"));

            // 1 match.
            Assert.AreEqual(container, folder1.FindAncestor<MockModel1>("Container"));
            Assert.AreEqual(container, folder1.FindAncestor<Model>("Container"));
            Assert.AreEqual(simpleModel, folder1.FindAncestor<IModel>("Test"));

            // When multiple ancestors match, ensure closest is returned.
            Assert.AreEqual(folder4, folder5.FindAncestor<Folder>("folder1"));

            Assert.AreEqual(container, folder5.FindAncestor<MockModel1>("Container"));
            Assert.AreEqual(container, folder4.FindAncestor<MockModel1>("Container"));

            // Test case-insensitive search.
            Assert.AreEqual(folder3, noSiblings.FindAncestor<Folder>("FoLdEr3"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindDescendant{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindDescendantByTypeAndName()
        {
            // No matches - expect null.
            Assert.Null(simpleModel.FindDescendant<MockModel2>(""));
            Assert.Null(simpleModel.FindDescendant<MockModel2>("Container"));
            Assert.Null(simpleModel.FindDescendant<MockModel2>(null));

            // No children - expect null.
            Assert.Null(noSiblings.FindDescendant<IModel>(""));
            Assert.Null(noSiblings.FindDescendant<IModel>(null));

            // Descendant exists with correct type but incorrect name.
            Assert.Null(container.FindDescendant<Folder>(""));
            Assert.Null(container.FindDescendant<Folder>(null));

            // Descendant exists with correct name but incorrect type.
            Assert.Null(container.FindDescendant<MockModel2>("folder1"));

            // Descendant exists with correct type but incorrect name.
            // Another descendant exists with correct name but incorrect type.
            Assert.Null(simpleModel.FindDescendant<MockModel1>("folder2"));

            // 1 match.
            Assert.AreEqual(container, simpleModel.FindDescendant<MockModel1>("Container"));
            Assert.AreEqual(folder2, simpleModel.FindDescendant<Folder>("folder2"));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            Assert.AreEqual(folder1, simpleModel.FindDescendant<Folder>("folder1"));
            Assert.AreEqual(folder4, folder1.FindDescendant<Folder>("folder1"));
            Assert.AreEqual(folder5, folder4.FindDescendant<Folder>("folder1"));

            // Test case-insensitive search.
            Assert.AreEqual(folder2, simpleModel.FindDescendant<IModel>("fOLDer2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindSibling{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingByTypeAndName()
        {
            // No parent - expect null.
            Assert.Null(simpleModel.FindSibling<IModel>(""));
            Assert.Null(simpleModel.FindSibling<IModel>(null));

            // No siblings - expect null.
            Assert.Null(noSiblings.FindSibling<IModel>(""));
            Assert.Null(noSiblings.FindSibling<IModel>(null));

            // A model is not its own sibling.
            Assert.Null(folder1.FindSibling<Folder>("folder1"));

            // Sibling exists with correct name but incorrect type.
            Assert.Null(folder1.FindSibling<MockModel2>("folder2"));

            // Sibling exists with correct type but incorrect name.
            Assert.Null(folder1.FindSibling<Folder>(""));
            Assert.Null(folder1.FindSibling<Folder>(null));

            // 1 sibling of correct type and name.
            Assert.AreEqual(folder2, folder1.FindSibling<Folder>("folder2"));

            // Many siblings of correct type and name - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
            container.Children.Add(folder4);
            Assert.AreEqual(folder1, folder2.FindSibling<Folder>("folder1"));
            Assert.AreEqual(folder4, folder1.FindSibling<Folder>("folder1"));

            // Test case-insensitive search.
            Assert.AreEqual(folder2, folder1.FindSibling<Folder>("fOlDeR2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindChild{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindChildByTypeAndName()
        {
            // No children - expect null.
            Assert.Null(folder1.FindChild<IModel>(""));
            Assert.Null(folder2.FindChild<IModel>(null));
            Assert.Null(noSiblings.FindChild<IModel>(".+"));

            // A model is not its own child.
            Assert.Null(folder1.FindChild<Folder>("folder1"));

            // Child exists with correct name but incorrect type.
            Assert.Null(container.FindChild<MockModel2>("folder2"));
            Assert.Null(simpleModel.FindChild<ILocator>("folder2"));

            // Child exists with correct type but incorrect name.
            Assert.Null(container.FindChild<Folder>("*"));
            Assert.Null(simpleModel.FindChild<IModel>(null));
            Assert.Null(folder3.FindChild<Model>(""));

            // 1 child of correct type and name.
            Assert.AreEqual(folder2, container.FindChild<Folder>("folder2"));
            Assert.AreEqual(container, simpleModel.FindChild<MockModel1>("Container"));

            // Many children of correct type and name - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
            container.Children.Add(folder4);
            Assert.AreEqual(folder1, container.FindChild<Folder>("folder1"));
            Assert.AreEqual(folder1, container.FindChild<IModel>("folder1"));

            // Test case-insensitive search.
            Assert.AreEqual(folder2, container.FindChild<Folder>("fOlDeR2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindInScope{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindInScopeByTypeAndName()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect null.
            Assert.Null(leaf1.FindInScope<MockModel2>("x"));
            Assert.Null(leaf1.FindInScope<MockModel2>(null));

            // Model exists in scope with correct name but incorrect type.
            Assert.Null(leaf1.FindInScope<MockModel2>("Plant"));

            // Model exists in scope with correct type but incorrect name.
            Assert.Null(leaf1.FindInScope<Zone>("*"));
            Assert.Null(leaf1.FindInScope<Zone>(null));

            // 1 match.
            Assert.AreEqual(scopedSimulation.Children[2], leaf1.FindInScope<Zone>("zone1"));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.AreEqual(plant1, managerFolder.FindInScope<Plant>("Plant"));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.AreEqual(managerFolder, leaf1.FindInScope<IModel>("asdf"));
            Assert.AreEqual(scopedSimulation, plant1.FindInScope<Simulation>("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[1].FindInScope<IModel>("asdf"));
            Assert.AreEqual(scopedSimulation.Children[0], scopedSimulation.Children[0].FindInScope<Clock>("asdf"));
            Assert.AreEqual(scopedSimulation, scopedSimulation.Children[0].FindInScope<IModel>("asdf"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllAncestors()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllAncestors()
        {
            // The top-level model has no ancestors.
            Assert.AreEqual(new IModel[0], simpleModel.FindAllAncestors().ToArray());

            Assert.AreEqual(new[] { simpleModel }, container.FindAllAncestors().ToArray());

            // Ancestors should be in bottom-up order.
            Assert.AreEqual(new[] { folder3, simpleModel }, noSiblings.FindAllAncestors().ToArray());

            // Note this test may break if we implement caching for this function.
            container.Parent = null;
            Assert.AreEqual(new[] { container }, folder1.FindAllAncestors());

            // This will create infinite recursion. However this should not
            // cause an error in and of itself, due to the lazy implementation.
            container.Parent = folder1;
            Assert.DoesNotThrow(() => folder1.FindAllAncestors());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllDescendants()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllDescendants()
        {
            // No children - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.FindAllDescendants().Count());

            // Descendants should be in depth-first search order.
            Assert.AreEqual(new[] { container, folder1, folder2, folder3, noSiblings }, simpleModel.FindAllDescendants().ToArray());
            Assert.AreEqual(new[] { folder1, folder2 }, container.FindAllDescendants().ToArray());
            Assert.AreEqual(new[] { noSiblings }, folder3.FindAllDescendants().ToArray());

            // This will create infinite recursion. However this should not
            // cause an error in and of itself, due to the lazy implementation.
            folder1.Children.Add(folder1);
            Assert.DoesNotThrow(() => folder1.FindAllDescendants());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllSiblings()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllSiblings()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.FindAllSiblings().Count());

            // No siblings - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.FindAllSiblings().Count());

            // 1 sibling.
            Assert.AreEqual(new[] { folder2 }, folder1.FindAllSiblings().ToArray());

            // Many siblings.
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
            IModel test = new Model() { Name = "test", Parent = folder1.Parent };
            folder1.Parent.Children.Add(folder4);
            folder1.Parent.Children.Add(test);
            Assert.AreEqual(new[] { folder2, folder4, test }, folder1.FindAllSiblings().ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllChildren()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllChildren()
        {
            // No children - expect empty enumerable (not null).
            Assert.AreEqual(0, folder1.FindAllChildren().Count());

            // 1 child.
            Assert.AreEqual(new[] { noSiblings }, folder3.FindAllChildren().ToArray());

            // Many children.
            IModel folder4 = new Folder() { Name = "folder4", Parent = container };
            IModel test = new Model() { Name = "test", Parent = container };
            container.Children.Add(folder4);
            container.Children.Add(test);
            Assert.AreEqual(new[] { folder1, folder2, folder4, test }, container.FindAllChildren().ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllInScope()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllInScope()
        {
            // Find all from the top-level model should work.
            Assert.AreEqual(14, scopedSimulation.FindAllInScope().Count());

            // Find all should fail if the top-level is not scoped.
            // Can't enable this check until some refactoring of scoping code.
            //Assert.Throws<Exception>(() => simpleModel.FindAll().Count());

            // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
            // Note that the manager is not in scope. This is not desirable behaviour.
            var leaf1 = scopedSimulation.Children[2].Children[1].Children[0];
            List<IModel> inScopeOfLeaf1 = leaf1.FindAllInScope().ToList();
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
            List<IModel> inScopeOfSoil = soil.FindAllInScope().ToList();
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
        /// Tests the <see cref="IModel.FindAllAncestors{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeAncestors()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.FindAllAncestors<IModel>().Count());

            // Container has no MockModel2 ancestors.
            Assert.AreEqual(0, container.FindAllAncestors<MockModel2>().Count());

            // Container's only ancestor is simpleModel.
            Assert.AreEqual(new[] { simpleModel }, container.FindAllAncestors<IModel>().ToArray());
            Assert.AreEqual(new[] { simpleModel }, container.FindAllAncestors<Model>().ToArray());

            IModel folder4 = new Folder() { Name = "folder4", Parent = folder3 };
            folder3.Children.Add(folder4);
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder4.Children.Add(folder5);

            Assert.AreEqual(0, folder5.FindAllAncestors<MockModel2>().Count());
            Assert.AreEqual(new[] { folder4, folder3 }, folder5.FindAllAncestors<Folder>().ToArray());
            Assert.AreEqual(new[] { folder4, folder3, simpleModel }, folder5.FindAllAncestors<IModel>().ToArray());
            Assert.AreEqual(new[] { folder4, folder3, simpleModel }, folder5.FindAllAncestors<Model>().ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllDescendants{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeDescendants()
        {
            // No children - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.FindAllDescendants<IModel>().Count());

            // No matches - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.FindAllDescendants<MockModel2>().Count());

            // 1 match.
            Assert.AreEqual(new[] { simpleModel.Children[0] }, simpleModel.FindAllDescendants<MockModel1>().ToArray());

            // Many matches - expect depth-first search.
            Assert.AreEqual(new[] { folder1, folder2, folder3 }, simpleModel.FindAllDescendants<Folder>().ToArray());
            Assert.AreEqual(new[] { container, folder1, folder2, folder3, noSiblings }, simpleModel.FindAllDescendants<IModel>().ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllSiblings{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeSiblings()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.FindAllSiblings<IModel>().Count());

            // No siblings - expect empty enumerable (not null).
            Assert.AreEqual(0, noSiblings.FindAllSiblings<IModel>().Count());

            // No siblings of correct type - expect empty enumerable (not null).
            Assert.AreEqual(0, folder1.FindAllSiblings<MockModel2>().Count());

            // 1 sibling of correct type.
            Assert.AreEqual(new[] { folder2 }, folder1.FindAllSiblings<Folder>().ToArray());

            // Many siblings of correct type - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
            IModel test = new Model() { Name = "test", Parent = folder1.Parent };
            folder1.Parent.Children.Add(folder4);
            folder1.Parent.Children.Add(test);
            Assert.AreEqual(new[] { folder2, folder4 }, folder1.FindAllSiblings<Folder>().ToArray());
            Assert.AreEqual(new[] { folder2, folder4, test }, folder1.FindAllSiblings<IModel>().ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllChildren{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeChildren()
        {
            // No children - expect empty enumerable (not null).
            Assert.AreEqual(0, folder2.FindAllChildren<IModel>().Count());
            Assert.AreEqual(0, folder2.FindAllChildren<MockModel1>().Count());
            Assert.AreEqual(0, folder2.FindAllChildren<MockModel2>().Count());

            // No children of correct type - expect empty enumerable (not null).
            Assert.AreEqual(0, container.FindAllChildren<MockModel2>().Count());

            // 1 child of correct type.
            Assert.AreEqual(new[] { folder3 }, simpleModel.FindAllChildren<Folder>().ToArray());

            // Many children of correct type - expect first child which matches.
            IModel folder4 = new Folder() { Name = "folder4", Parent = container };
            IModel test = new Model() { Name = "test", Parent = container };
            container.Children.Add(folder4);
            container.Children.Add(test);
            Assert.AreEqual(new[] { folder1, folder2, folder4 }, container.FindAllChildren<Folder>().ToArray());
            Assert.AreEqual(new[] { folder1, folder2, folder4, test }, container.FindAllChildren<Model>().ToArray());
            Assert.AreEqual(new[] { folder1, folder2, folder4, test }, container.FindAllChildren<IModel>().ToArray());
            Assert.AreEqual(new[] { container, folder3 }, simpleModel.FindAllChildren<IModel>().ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllInScope{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeInScope()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // Test laziness. Unsure if we want to keep this.
            Assert.DoesNotThrow(() => simpleModel.FindAllInScope<IModel>());

            // No matches (there is an ISummary but no Summary) -
            // expect empty enumerable (not null).
            Assert.AreEqual(0, leaf1.FindAllInScope<Summary>().Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[0] }, leaf1.FindAllInScope<Clock>().ToArray());

            // Many matches - test order.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel[] allPlants = new[] { plant1, plant2 };
            Assert.AreEqual(allPlants, managerFolder.FindAllInScope<Plant>().ToArray());
            Assert.AreEqual(allPlants, plant1.FindAllInScope<Plant>().ToArray());

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.AreEqual(allPlants, plant2.FindAllInScope<Plant>().ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllAncestors(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameAncestors()
        {
            // No parent - expect empty enumerable (not null).
            Assert.AreEqual(0, simpleModel.FindAllAncestors("Test").Count());
            Assert.AreEqual(0, simpleModel.FindAllAncestors("").Count());
            Assert.AreEqual(0, simpleModel.FindAllAncestors(null).Count());

            // No ancestors of correct name - expect empty enumerable.
            Assert.AreEqual(0, container.FindAllAncestors("asdf").Count());
            Assert.AreEqual(0, container.FindAllAncestors("").Count());
            Assert.AreEqual(0, container.FindAllAncestors(null).Count());

            // 1 ancestor of correct name.
            Assert.AreEqual(new[] { simpleModel }, container.FindAllAncestors("Test").ToArray());

            // Multiple ancestors with correct name - expect bottom-up search.
            IModel folder4 = new Folder() { Name = "folder3", Parent = folder3 };
            folder3.Children.Add(folder4);
            IModel folder5 = new Folder() { Name = "folder3", Parent = folder4 };
            folder4.Children.Add(folder5);

            Assert.AreEqual(0, folder5.FindAllAncestors("").Count());
            Assert.AreEqual(0, folder5.FindAllAncestors(null).Count());
            Assert.AreEqual(new[] { folder4, folder3 }, folder5.FindAllAncestors("folder3").ToArray());
            Assert.AreEqual(new[] { simpleModel }, folder5.FindAllAncestors("Test").ToArray());
            Assert.AreEqual(new[] { folder3 }, folder4.FindAllAncestors("folder3").ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllDescendants(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameDescendants()
        {
            // No children - expect empty enumerable.
            Assert.AreEqual(0, noSiblings.FindAllDescendants("x").Count());

            // No descendants with correct name - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.FindAllDescendants("x").Count());
            Assert.AreEqual(0, simpleModel.FindAllDescendants(null).Count());

            // 1 descendant with correct name.
            Assert.AreEqual(new[] { container }, simpleModel.FindAllDescendants("Container").ToArray());

            // Many descendants with correct name - expect results in depth-first order.
            IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
            container.Children.Add(folder4);
            IModel folder5 = new Model() { Parent = folder1, Name = "folder1" };
            folder1.Children.Add(folder5);

            Assert.AreEqual(new[] { folder1, folder5, folder4 }, simpleModel.FindAllDescendants("folder1").ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllSiblings(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameSiblings()
        {
            // No parent - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.FindAllSiblings("Test").Count());
            Assert.AreEqual(0, simpleModel.FindAllSiblings("").Count());
            Assert.AreEqual(0, simpleModel.FindAllSiblings(null).Count());

            // No siblings - expect empty enumerable.
            Assert.AreEqual(0, noSiblings.FindAllSiblings("anything").Count());
            Assert.AreEqual(0, noSiblings.FindAllSiblings("nosiblings").Count());
            Assert.AreEqual(0, noSiblings.FindAllSiblings("").Count());
            Assert.AreEqual(0, noSiblings.FindAllSiblings(null).Count());

            // No siblings of correct name - expect empty enumerable.
            Assert.AreEqual(0, folder1.FindAllSiblings("x").Count());
            Assert.AreEqual(0, folder1.FindAllSiblings("folder1").Count());
            Assert.AreEqual(0, folder1.FindAllSiblings("").Count());
            Assert.AreEqual(0, folder1.FindAllSiblings(null).Count());

            // 1 sibling of correct name.
            Assert.AreEqual(new[] { folder2 }, folder1.FindAllSiblings("folder2").ToArray());

            // Many siblings of correct name, expect them in indexed order.
            IModel folder4 = new Folder() { Name = "folder2", Parent = container };
            folder1.Parent.Children.Add(folder4);
            Assert.AreEqual(new[] { folder2, folder4 }, folder1.FindAllSiblings("folder2").ToArray());
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllChildren(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameChildren()
        {
            // No children - expect empty enumerable.
            Assert.AreEqual(0, folder1.FindAllChildren("Test").Count());
            Assert.AreEqual(0, folder2.FindAllChildren("").Count());
            Assert.AreEqual(0, noSiblings.FindAllChildren(null).Count());

            // No children of correct name - expect empty enumerable.
            Assert.AreEqual(0, container.FindAllChildren("x").Count());
            Assert.AreEqual(0, container.FindAllChildren("Test").Count());
            Assert.AreEqual(0, folder3.FindAllChildren("folder3").Count());
            Assert.AreEqual(0, simpleModel.FindAllChildren(null).Count());

            // 1 child of correct name.
            Assert.AreEqual(new[] { folder2 }, container.FindAllChildren("folder2").ToArray());
            Assert.AreEqual(new[] { container }, simpleModel.FindAllChildren("Container").ToArray());

            // Many (but not all) children of correct name, expect them in indexed order.
            IModel folder4 = new Folder() { Name = "folder2", Parent = container };
            container.Children.Add(folder4);
            Assert.AreEqual(new[] { folder2, folder4 }, container.FindAllChildren("folder2").ToArray());

            // All (>1) children have correct name.
            container.Children.Remove(folder1);
            Assert.AreEqual(new[] { folder2, folder4 }, container.FindAllChildren("folder2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllInScope(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameInScope()
        {
            IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

            // This will throw because there is no scoped parent model.
            // Can't uncomment this until we refactor the scoping code.
            //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

            // No matches - expect empty enumerable.
            Assert.AreEqual(0, leaf1.FindAllInScope("x").Count());
            Assert.AreEqual(0, leaf1.FindAllInScope("").Count());
            Assert.AreEqual(0, leaf1.FindAllInScope(null).Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[2] }, leaf1.FindAllInScope("zone1").ToArray());

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel clock = scopedSimulation.Children[0];
            IModel summary = scopedSimulation.Children[1];
            Assert.AreEqual(new[] { plant1, plant2 }, managerFolder.FindAllInScope("Plant").ToArray());

            managerFolder.Name = "asdf";
            clock.Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, leaf1.FindAllInScope("asdf").ToArray());
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, plant1.FindAllInScope("asdf").ToArray());
            Assert.AreEqual(new[] { scopedSimulation, clock, managerFolder }, summary.FindAllInScope("asdf").ToArray());
            Assert.AreEqual(new[] { scopedSimulation, clock, managerFolder }, clock.FindAllInScope("asdf").ToArray());
        }

        /// <summary>
        /// Test the <see cref="IModel.FindAllAncestors{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAncestorsByTypeAndName()
        {
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            // No parent - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.FindAllAncestors<Model>("Test").Count());
            Assert.AreEqual(0, simpleModel.FindAllAncestors<IModel>("Test").Count());
            Assert.AreEqual(0, simpleModel.FindAllAncestors<IModel>("").Count());
            Assert.AreEqual(0, simpleModel.FindAllAncestors<IModel>(null).Count());

            // A model is not its own ancestor.
            Assert.AreEqual(0, container.FindAllAncestors<MockModel1>(null).Count());
            Assert.AreEqual(0, container.FindAllAncestors<MockModel1>("").Count());
            Assert.AreEqual(0, container.FindAllAncestors<MockModel1>("Container").Count());
            Assert.AreEqual(0, container.FindAllAncestors<IModel>("Container").Count());

            // Ancestor exists with correct type but incorrect name.
            Assert.AreEqual(0, folder1.FindAllAncestors<MockModel1>(null).Count());
            Assert.AreEqual(0, folder1.FindAllAncestors<MockModel1>("").Count());

            // Ancestor exists with correct name but incorrect type.
            Assert.AreEqual(0, folder1.FindAllAncestors<MockModel2>("Container").Count());
            Assert.AreEqual(0, folder1.FindAllAncestors<Fertiliser>("Test").Count());

            // Ancestor exists with correct type but incorrect name.
            // Another ancestor exists with correct name but incorrect type.
            Assert.AreEqual(0, folder1.FindAllAncestors<MockModel1>("Test").Count());

            // 1 match.
            Assert.AreEqual(new[] { container }, folder1.FindAllAncestors<MockModel1>("Container").ToArray());
            Assert.AreEqual(new[] { container }, folder2.FindAllAncestors<Model>("Container").ToArray());
            Assert.AreEqual(new[] { simpleModel }, noSiblings.FindAllAncestors<IModel>("Test").ToArray());
            Assert.AreEqual(new[] { folder3 }, noSiblings.FindAllAncestors<IModel>("folder3").ToArray());

            // Multiple matches - ensure ordering is bottom-up.
            Assert.AreEqual(new[] { folder4, folder1 }, folder5.FindAllAncestors<Folder>("folder1").ToArray());

            // An uncle/cousin is not an ancestor.
            folder2.Name = "folder1";
            Assert.AreEqual(new[] { folder4, folder1 }, folder5.FindAllAncestors<Folder>("folder1").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder3 }, noSiblings.FindAllAncestors<Folder>("FoLdEr3").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllDescendants{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindDescendantsByTypeAndName()
        {
            // No matches - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.FindAllDescendants<MockModel2>("").Count());
            Assert.AreEqual(0, simpleModel.FindAllDescendants<MockModel2>("Container").Count());
            Assert.AreEqual(0, simpleModel.FindAllDescendants<MockModel2>(null).Count());

            // No children - expect enumerable.
            Assert.AreEqual(0, noSiblings.FindAllDescendants<IModel>("").Count());
            Assert.AreEqual(0, noSiblings.FindAllDescendants<IModel>(null).Count());

            // Descendants exist with correct type but incorrect name.
            Assert.AreEqual(0, container.FindAllDescendants<Folder>("").Count());
            Assert.AreEqual(0, container.FindAllDescendants<Folder>(null).Count());
            Assert.AreEqual(0, folder3.FindAllDescendants<IModel>("x").Count());

            // Descendants exist with correct name but incorrect type.
            Assert.AreEqual(0, container.FindAllDescendants<MockModel2>("folder1").Count());
            Assert.AreEqual(0, simpleModel.FindAllDescendants<Irrigation>("nosiblings").Count());

            // Descendant exists with correct type but incorrect name.
            // Another descendant exists with correct name but incorrect type.
            Assert.AreEqual(0, simpleModel.FindAllDescendants<MockModel1>("folder2").Count());
            Assert.AreEqual(0, simpleModel.FindAllDescendants<Folder>("nosiblings").Count());

            // 1 match.
            Assert.AreEqual(new[] { container }, simpleModel.FindAllDescendants<MockModel1>("Container").ToArray());
            Assert.AreEqual(new[] { folder2 }, container.FindAllDescendants<IModel>("folder2").ToArray());
            Assert.AreEqual(new[] { noSiblings }, folder3.FindAllDescendants<Model>("nosiblings").ToArray());

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            IModel folder6 = new Folder() { Name = "folder1", Parent = container };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);
            container.Children.Add(folder6);
            folder3.Name = "folder1";
            noSiblings.Name = "folder1";

            Assert.AreEqual(new[] { folder1, folder4, folder5, folder6, folder3 }, simpleModel.FindAllDescendants<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder1, folder4, folder5, folder6 }, container.FindAllDescendants<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder4, folder5 }, folder1.FindAllDescendants<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder5 }, folder4.FindAllDescendants<Folder>("folder1").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder2 }, simpleModel.FindAllDescendants<IModel>("fOLDer2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllSiblings{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingsByTypeAndName()
        {
            // No parent - expect empty enumerable.
            Assert.AreEqual(0, simpleModel.FindAllSiblings<IModel>("Test").Count());
            Assert.AreEqual(0, simpleModel.FindAllSiblings<IModel>("").Count());
            Assert.AreEqual(0, simpleModel.FindAllSiblings<IModel>(null).Count());

            // No siblings - expect empty enumerable.
            Assert.AreEqual(0, noSiblings.FindAllSiblings<IModel>("").Count());
            Assert.AreEqual(0, noSiblings.FindAllSiblings<IModel>("nosiblings").Count());
            Assert.AreEqual(0, noSiblings.FindAllSiblings<IModel>(null).Count());

            // A model is not its own sibling.
            Assert.AreEqual(0, folder1.FindAllSiblings<Folder>("folder1").Count());
            Assert.AreEqual(0, folder1.FindAllSiblings<IModel>("folder1").Count());

            // Siblings exist with correct name but incorrect type.
            Assert.AreEqual(0, folder1.FindAllSiblings<MockModel2>("folder2").Count());
            Assert.AreEqual(0, folder2.FindAllSiblings<Fertiliser>("Container").Count());

            // Siblings exist with correct type but incorrect name.
            Assert.AreEqual(0, folder1.FindAllSiblings<Folder>("").Count());
            Assert.AreEqual(0, folder1.FindAllSiblings<Folder>(null).Count());
            Assert.AreEqual(0, container.FindAllSiblings<Folder>("folder1").Count());

            // 1 sibling of correct type and name.
            Assert.AreEqual(new[] { folder2 }, folder1.FindAllSiblings<Folder>("folder2").ToArray());

            // Many siblings of correct type and name - expect indexed order.
            IModel folder4 = new Folder() { Name = "folder1", Parent = container };
            container.Children.Add(folder4);
            Assert.AreEqual(new[] { folder1, folder4 }, folder2.FindAllSiblings<Folder>("folder1").ToArray());
            Assert.AreEqual(new[] { folder4 }, folder1.FindAllSiblings<Folder>("folder1").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder2 }, folder1.FindAllSiblings<Folder>("fOlDeR2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllChildren{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindChildrenByTypeAndName()
        {
            // No children- expect empty enumerable.
            Assert.AreEqual(0, folder1.FindAllChildren<IModel>("folder1").Count());
            Assert.AreEqual(0, folder1.FindAllChildren<IModel>("folder2").Count());
            Assert.AreEqual(0, folder2.FindAllChildren<IModel>("Container").Count());
            Assert.AreEqual(0, folder2.FindAllChildren<IModel>("").Count());
            Assert.AreEqual(0, noSiblings.FindAllChildren<IModel>(null).Count());

            // Children exist with correct name but incorrect type.
            Assert.AreEqual(0, container.FindAllChildren<MockModel2>("folder2").Count());
            Assert.AreEqual(0, folder3.FindAllChildren<Fertiliser>("nosiblings").Count());

            // Children exist with correct type but incorrect name.
            Assert.AreEqual(0, container.FindAllChildren<Folder>("folder3").Count());
            Assert.AreEqual(0, container.FindAllChildren<IModel>(null).Count());
            Assert.AreEqual(0, folder3.FindAllChildren<Model>("Test").Count());
            Assert.AreEqual(0, simpleModel.FindAllChildren<Model>("").Count());

            // 1 child of correct type and name.
            Assert.AreEqual(new[] { folder2 }, container.FindAllChildren<Folder>("folder2").ToArray());

            // Many siblings of correct type and name - expect indexed order.
            IModel folder4 = new Folder() { Name = "folder1", Parent = container };
            container.Children.Add(folder4);
            Assert.AreEqual(new[] { folder1, folder4 }, container.FindAllChildren<Folder>("folder1").ToArray());
            folder3.Name = "Container";
            Assert.AreEqual(new[] { container, folder3 }, simpleModel.FindAllChildren<IModel>("Container").ToArray());

            // Test case-insensitive search.
            Assert.AreEqual(new[] { folder2 }, container.FindAllChildren<Folder>("fOlDeR2").ToArray());
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllInScope{T}(string)"/> method.
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
            Assert.AreEqual(0, leaf1.FindAllInScope<MockModel2>("x").Count());
            Assert.AreEqual(0, leaf1.FindAllInScope<MockModel2>(null).Count());

            // Model exists in scope with correct name but incorrect type.
            Assert.AreEqual(0, leaf1.FindAllInScope<MockModel2>("Plant").Count());

            // Model exists in scope with correct type but incorrect name.
            Assert.AreEqual(0, leaf1.FindAllInScope<Zone>("*").Count());
            Assert.AreEqual(0, leaf1.FindAllInScope<Zone>(null).Count());
            Assert.AreEqual(0, leaf1.FindAllInScope<Zone>("Plant").Count());

            // 1 match.
            Assert.AreEqual(new[] { scopedSimulation.Children[2] }, leaf1.FindAllInScope<Zone>("zone1").ToArray());

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";

            Assert.AreEqual(new[] { scopedSimulation }, plant1.FindAllInScope<Simulation>("asdf").ToArray());
            Assert.AreEqual(new[] { clock }, clock.FindAllInScope<Clock>("asdf").ToArray());

            // Many matches - expect first.
            Assert.AreEqual(new[] { plant1, plant2 }, managerFolder.FindAllInScope<Plant>("Plant").ToArray());

            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, leaf1.FindAllInScope<IModel>("asdf").ToArray());
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, plant1.FindAllInScope<IModel>("asdf").ToArray());
            Assert.AreEqual(new[] { scopedSimulation, clock, managerFolder }, summary.FindAllInScope<IModel>("asdf").ToArray());
            Assert.AreEqual(new[] { managerFolder, scopedSimulation, clock }, plant2.FindAllInScope<IModel>("asdf").ToArray());
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
        /// Tests for the <see cref="IModel.FindByPath(string, bool)"/> method.
        /// </summary>
        [Test]
        public void TestFindInPath()
        {
            // 1. Absolute paths.
            Assert.AreEqual(container, simpleModel.FindByPath(".Test.Container").Value);
            Assert.AreEqual(simpleModel, simpleModel.FindByPath(".Test").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindByPath(".Test.folder3.nosiblings").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindByPath(".Test.folder3.Children[1]").Value);
            Assert.Null(simpleModel.FindByPath(".Test.folder3.asdf"));
            Assert.Null(simpleModel.FindByPath(".asdf"));
            Assert.Null(simpleModel.FindByPath(""));
            Assert.Null(simpleModel.FindByPath(null));

            // Do not allow type names in absolute path.
            Assert.Null(simpleModel.FindByPath(".Model.Folder.Model"));

            // Absolute path with variable.
            Assert.AreEqual("folder2", simpleModel.FindByPath(".Test.Container.folder2.Name").Value);
            Assert.AreEqual(typeof(VariableComposite), simpleModel.FindByPath(".Test.Name").GetType());

            // 2. Relative paths.
            Assert.AreEqual(simpleModel, simpleModel.FindByPath("[Test]").Value);
            Assert.AreEqual(folder1, simpleModel.FindByPath("[folder1]").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindByPath("[folder3].nosiblings").Value);
            Assert.AreEqual(folder2, simpleModel.FindByPath("[Test].Container.folder2").Value);
            Assert.AreEqual(folder3, simpleModel.FindByPath("[Test].Children[2]").Value);
            Assert.Null(simpleModel.FindByPath("[asdf]"));
            Assert.Null(simpleModel.FindByPath("[folder3].foo"));

            // Do not allow type names in relative path.
            Assert.Null(simpleModel.FindByPath(".Model.Folder"));

            // Relative path with variable.
            Assert.AreEqual("Container", simpleModel.FindByPath("[Container].Name").Value);
            Assert.AreEqual(typeof(VariableComposite), simpleModel.FindByPath("[Container].Name").GetType());

            // 3. Child paths.
            Assert.AreEqual(container, simpleModel.FindByPath("Container").Value);
            Assert.AreEqual(noSiblings, simpleModel.FindByPath("folder3.nosiblings").Value);
            Assert.AreEqual(folder2, container.FindByPath("folder2").Value);
            Assert.Null(simpleModel.FindByPath("folder2"));
            Assert.Null(simpleModel.FindByPath("x"));
            Assert.Null(simpleModel.FindByPath(""));
            Assert.Null(simpleModel.FindByPath(null));
        }
    }
}
