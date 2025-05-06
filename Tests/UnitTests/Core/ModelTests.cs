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
        private class MockModel : Model { }
        private class MockModel1 : Model, IInterface { }
        private class MockModel2 : Model { }
        private class MockModel3 : Model
        {
            public MockModel3(string name) => Name = name;
        }

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
            simpleModel = new MockModel()
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
                            new MockModel()
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
                                    new MockModel()
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
            Assert.That(simpleModel.FullPath, Is.EqualTo(".Test"));
            Assert.That(folder1.FullPath, Is.EqualTo(".Test.Container.folder1"));
            Assert.That(noSiblings.FullPath, Is.EqualTo(".Test.folder3.nosiblings"));
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
            Assert.That(simpleModel.FindAncestor("x"), Is.Null);

            // A model is not its own ancestor.
            Assert.That(container.FindAncestor("Container"), Is.Null);

            // No matches - expect null.
            Assert.That(noSiblings.FindAncestor("x"), Is.Null);
            Assert.That(noSiblings.FindAncestor(null), Is.Null); 

            // 1 match.
            Assert.That(container.FindAncestor("Test"), Is.EqualTo(simpleModel));

            // When multiple ancestors match the name, ensure closest is returned.
            Assert.That(folder5.FindAncestor("folder1"), Is.EqualTo(folder4));

            Assert.That(folder5.FindAncestor("Container"), Is.EqualTo(container));
            Assert.That(folder4.FindAncestor("Container"), Is.EqualTo(container));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindDescendant(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindByNameDescendant()
        {
            // No children - expect null.
            Assert.That(noSiblings.FindDescendant("x"), Is.Null);

            // No matches - expect null.
            Assert.That(simpleModel.FindDescendant("x"), Is.Null);
            Assert.That(simpleModel.FindDescendant(null), Is.Null);

            // 1 match.
            Assert.That(simpleModel.FindDescendant("Container"), Is.EqualTo(container));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
            container.Children.Add(folder4);
            IModel folder5 = new MockModel() { Parent = folder1, Name = "folder1" };
            folder1.Children.Add(folder5);

            Assert.That(simpleModel.FindDescendant("folder1"), Is.EqualTo(folder1));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindSibling(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingByName()
        {
            // No parent - expect null.
            Assert.That(simpleModel.FindSibling("anything"), Is.Null);

            // No siblings - expect null.
            Assert.That(noSiblings.FindSibling("anything"), Is.Null);

            // No siblings of correct name - expect null.
            Assert.That(folder1.FindSibling(null), Is.Null);
            Assert.That(folder1.FindSibling("x"), Is.Null);

            // 1 sibling of correct name.
            Assert.That(folder1.FindSibling("folder2"), Is.EqualTo(folder2));

            // Many siblings of correct name - expect first sibling which matches.
            // This isn't really a valid model setup but we'll test it anyway.
            folder1.Parent.Children.Add(new Folder()
            {
                Name = "folder2",
                Parent = folder1.Parent
            });
            Assert.That(folder1.FindSibling("folder2"), Is.EqualTo(folder2));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindChild(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindChildByName()
        {
            // No children - expect null.
            Assert.That(noSiblings.FindChild("*"), Is.Null);
            Assert.That(noSiblings.FindChild(""), Is.Null);
            Assert.That(noSiblings.FindChild(null), Is.Null);

            // No children of correct name - expect null.
            Assert.That(container.FindChild(null), Is.Null);
            Assert.That(folder3.FindChild("x"), Is.Null);
            Assert.That(simpleModel.FindChild("folder2"), Is.Null);

            // 1 child of correct name.
            Assert.That(container.FindChild("folder2"), Is.EqualTo(folder2));
            Assert.That(simpleModel.FindChild("folder3"), Is.EqualTo(folder3));

            // Many children of correct name - expect first child which matches.
            // This isn't really a valid model setup but we'll test it anyway.
            container.Children.Add(new Folder()
            {
                Name = "folder2",
                Parent = folder1.Parent
            });
            Assert.That(container.FindChild("folder2"), Is.EqualTo(folder2));
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
            Assert.That(leaf1.FindInScope("x"), Is.Null);
            Assert.That(leaf1.FindInScope(null), Is.Null);

            // 1 match.
            Assert.That(leaf1.FindInScope("zone1"), Is.EqualTo(scopedSimulation.Children[2]));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.That(managerFolder.FindInScope<Plant>(), Is.EqualTo(plant1));

            // plant1 is actually in scope of itself. You could argue that this is
            // a bug (I think it is) but it is a problem for another day.
            Assert.That(plant1.FindInScope("Plant"), Is.EqualTo(plant1));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.That(leaf1.FindInScope("asdf"), Is.EqualTo(managerFolder));
            Assert.That(plant1.FindInScope("asdf"), Is.EqualTo(managerFolder));
            Assert.That(scopedSimulation.Children[1].FindInScope("asdf"), Is.EqualTo(scopedSimulation));
            Assert.That(scopedSimulation.Children[0].FindInScope("asdf"), Is.EqualTo(scopedSimulation));
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
            Assert.That(simpleModel.FindAncestor<IModel>(), Is.Null);

            // A model is not its own ancestor.
            Assert.That(container.FindAncestor<MockModel1>(), Is.Null);

            Assert.That(container.FindAncestor<IModel>(), Is.EqualTo(simpleModel));

            // When multiple ancestors match the type, ensure closest is returned.
            Assert.That(folder5.FindAncestor<Folder>(), Is.EqualTo(folder4));

            Assert.That(folder5.FindAncestor<MockModel1>(), Is.EqualTo(container));
            Assert.That(folder4.FindAncestor<MockModel1>(), Is.EqualTo(container));

            // Searching for any IModel ancestor should return the node's parent.
            Assert.That(folder1.FindAncestor<IModel>(), Is.EqualTo(folder1.Parent));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindDescendant{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeDescendant()
        {
            // No matches - expect null.
            Assert.That(simpleModel.FindDescendant<MockModel2>(), Is.Null);

            // No children - expect null.
            Assert.That(noSiblings.FindDescendant<IModel>(), Is.Null);

            // 1 match.
            Assert.That(simpleModel.FindDescendant<MockModel1>(), Is.EqualTo(container));
            Assert.That(simpleModel.FindDescendant<IInterface>(), Is.EqualTo(container));

            // Many matches - expect first in depth-first search is returned.
            Assert.That(simpleModel.FindDescendant<Folder>(), Is.EqualTo(folder1));
            Assert.That(simpleModel.FindDescendant<IModel>(), Is.EqualTo(container));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindSibling{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeSibling()
        {
            // No parent - expect null.
            Assert.That(simpleModel.FindSibling<IModel>(), Is.Null);

            // No siblings - expect null.
            Assert.That(noSiblings.FindSibling<IModel>(), Is.Null);

            // No siblings of correct type - expect null.
            Assert.That(folder1.FindSibling<MockModel2>(), Is.Null);

            // 1 sibling of correct type.
            Assert.That(folder1.FindSibling<Folder>(), Is.EqualTo(folder2));

            // Many siblings of correct type - expect first sibling which matches.
            folder1.Parent.Children.Add(new Folder()
            {
                Name = "folder4",
                Parent = folder1.Parent
            });
            Assert.That(folder1.FindSibling<Folder>(), Is.EqualTo(folder2));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindChild{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindByTypeChild()
        {
            // No children - expect null.
            Assert.That(folder1.FindChild<IModel>(), Is.Null);
            Assert.That(noSiblings.FindChild<Model>(), Is.Null);

            // No children of correct type - expect null.
            Assert.That(simpleModel.FindChild<MockModel2>(), Is.Null);
            Assert.That(folder3.FindChild<MockModel1>(), Is.Null);

            // 1 child of correct type.
            Assert.That(simpleModel.FindChild<MockModel1>(), Is.EqualTo(container));
            Assert.That(folder3.FindChild<Model>(), Is.EqualTo(noSiblings));

            // Many children of correct type - expect first sibling which matches.
            Assert.That(container.FindChild<Folder>(), Is.EqualTo(folder1));
            Assert.That(container.FindChild<Model>(), Is.EqualTo(folder1));
            Assert.That(simpleModel.FindChild<IModel>(), Is.EqualTo(container));
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
            Assert.That(leaf1.FindInScope<Summary>(), Is.Null);  

            // 1 match.
            Assert.That(leaf1.FindInScope<Zone>(), Is.EqualTo(scopedSimulation.Children[2]));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.That(managerFolder.FindInScope<Plant>(), Is.EqualTo(plant1));
            Assert.That(plant1.FindInScope<Plant>(), Is.EqualTo(plant1));
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
            Assert.That(simpleModel.FindAncestor<IModel>(""), Is.Null);
            Assert.That(simpleModel.FindAncestor<IModel>(null), Is.Null);

            // A model is not its own ancestor.
            Assert.That(container.FindAncestor<MockModel1>(null), Is.Null);
            Assert.That(container.FindAncestor<MockModel1>("Container"), Is.Null);

            // Ancestor exists with correct type but incorrect name.
            Assert.That(folder1.FindAncestor<MockModel1>(null), Is.Null);
            Assert.That(folder1.FindAncestor<MockModel1>(""), Is.Null);

            // Ancestor exists with correct name but incorrect type.
            Assert.That(folder1.FindAncestor<MockModel2>("Container"), Is.Null);

            // Ancestor exists with correct type but incorrect name.
            // Another ancestor exists with correct name but incorrect type.
            Assert.That(folder1.FindAncestor<MockModel1>("Test"), Is.Null);

            // 1 match.
            Assert.That(folder1.FindAncestor<MockModel1>("Container"), Is.EqualTo(container));
            Assert.That(folder1.FindAncestor<Model>("Container"), Is.EqualTo(container));
            Assert.That(folder1.FindAncestor<IModel>("Test"), Is.EqualTo(simpleModel));

            // When multiple ancestors match, ensure closest is returned.
            Assert.That(folder5.FindAncestor<Folder>("folder1"), Is.EqualTo(folder4));

            Assert.That(folder5.FindAncestor<MockModel1>("Container"), Is.EqualTo(container));
            Assert.That(folder4.FindAncestor<MockModel1>("Container"), Is.EqualTo(container));

            // Test case-insensitive search.
            Assert.That(noSiblings.FindAncestor<Folder>("FoLdEr3"), Is.EqualTo(folder3));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindDescendant{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindDescendantByTypeAndName()
        {
            // No matches - expect null.
            Assert.That(simpleModel.FindDescendant<MockModel2>(""), Is.Null);
            Assert.That(simpleModel.FindDescendant<MockModel2>("Container"), Is.Null);
            Assert.That(simpleModel.FindDescendant<MockModel2>(null), Is.Null);

            // No children - expect null.
            Assert.That(noSiblings.FindDescendant<IModel>(""), Is.Null);
            Assert.That(noSiblings.FindDescendant<IModel>(null), Is.Null);

            // Descendant exists with correct type but incorrect name.
            Assert.That(container.FindDescendant<Folder>(""), Is.Null);
            Assert.That(container.FindDescendant<Folder>(null), Is.Null);

            // Descendant exists with correct name but incorrect type.
            Assert.That(container.FindDescendant<MockModel2>("folder1"), Is.Null);

            // Descendant exists with correct type but incorrect name.
            // Another descendant exists with correct name but incorrect type.
            Assert.That(simpleModel.FindDescendant<MockModel1>("folder2"), Is.Null);

            // 1 match.
            Assert.That(simpleModel.FindDescendant<MockModel1>("Container"), Is.EqualTo(container));
            Assert.That(simpleModel.FindDescendant<Folder>("folder2"), Is.EqualTo(folder2));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);

            Assert.That(simpleModel.FindDescendant<Folder>("folder1"), Is.EqualTo(folder1));
            Assert.That(folder1.FindDescendant<Folder>("folder1"), Is.EqualTo(folder4));
            Assert.That(folder4.FindDescendant<Folder>("folder1"), Is.EqualTo(folder5));

            // Test case-insensitive search.
            Assert.That(simpleModel.FindDescendant<IModel>("fOLDer2"), Is.EqualTo(folder2));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindSibling{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingByTypeAndName()
        {
            // No parent - expect null.
            Assert.That(simpleModel.FindSibling<IModel>(""), Is.Null);
            Assert.That(simpleModel.FindSibling<IModel>(null), Is.Null);

            // No siblings - expect null.
            Assert.That(noSiblings.FindSibling<IModel>(""), Is.Null);
            Assert.That(noSiblings.FindSibling<IModel>(null), Is.Null);

            // A model is not its own sibling.
            Assert.That(folder1.FindSibling<Folder>("folder1"), Is.Null);

            // Sibling exists with correct name but incorrect type.
            Assert.That(folder1.FindSibling<MockModel2>("folder2"), Is.Null);

            // Sibling exists with correct type but incorrect name.
            Assert.That(folder1.FindSibling<Folder>(""), Is.Null);
            Assert.That(folder1.FindSibling<Folder>(null), Is.Null);

            // 1 sibling of correct type and name.
            Assert.That(folder1.FindSibling<Folder>("folder2"), Is.EqualTo(folder2));

            // Many siblings of correct type and name - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
            container.Children.Add(folder4);
            Assert.That(folder2.FindSibling<Folder>("folder1"), Is.EqualTo(folder1));
            Assert.That(folder1.FindSibling<Folder>("folder1"), Is.EqualTo(folder4));

            // Test case-insensitive search.
            Assert.That(folder1.FindSibling<Folder>("fOlDeR2"), Is.EqualTo(folder2));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindChild{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindChildByTypeAndName()
        {
            // No children - expect null.
            Assert.That(folder1.FindChild<IModel>(""), Is.Null);
            Assert.That(folder2.FindChild<IModel>(null), Is.Null);
            Assert.That(noSiblings.FindChild<IModel>(".+"), Is.Null);

            // A model is not its own child.
            Assert.That(folder1.FindChild<Folder>("folder1"), Is.Null);

            // Child exists with correct name but incorrect type.
            Assert.That(container.FindChild<MockModel2>("folder2"), Is.Null);
            Assert.That(simpleModel.FindChild<ILocator>("folder2"), Is.Null);

            // Child exists with correct type but incorrect name.
            Assert.That(container.FindChild<Folder>("*"), Is.Null);
            Assert.That(simpleModel.FindChild<IModel>(null), Is.Null);
            Assert.That(folder3.FindChild<Model>(""), Is.Null);

            // 1 child of correct type and name.
            Assert.That(container.FindChild<Folder>("folder2"), Is.EqualTo(folder2));
            Assert.That(simpleModel.FindChild<MockModel1>("Container"), Is.EqualTo(container));

            // Many children of correct type and name - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
            container.Children.Add(folder4);
            Assert.That(container.FindChild<Folder>("folder1"), Is.EqualTo(folder1));
            Assert.That(container.FindChild<IModel>("folder1"), Is.EqualTo(folder1));

            // Test case-insensitive search.
            Assert.That(container.FindChild<Folder>("fOlDeR2"), Is.EqualTo(folder2));
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
            Assert.That(leaf1.FindInScope<MockModel2>("x"), Is.Null);
            Assert.That(leaf1.FindInScope<MockModel2>(null), Is.Null);

            // Model exists in scope with correct name but incorrect type.
            Assert.That(leaf1.FindInScope<MockModel2>("Plant"), Is.Null);

            // Model exists in scope with correct type but incorrect name.
            Assert.That(leaf1.FindInScope<Zone>("*"), Is.Null);
            Assert.That(leaf1.FindInScope<Zone>(null), Is.Null);

            // 1 match.
            Assert.That(leaf1.FindInScope<Zone>("zone1"), Is.EqualTo(scopedSimulation.Children[2]));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            Assert.That(managerFolder.FindInScope<Plant>("Plant"), Is.EqualTo(plant1));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.That(leaf1.FindInScope<IModel>("asdf"), Is.EqualTo(managerFolder));
            Assert.That(plant1.FindInScope<Simulation>("asdf"), Is.EqualTo(scopedSimulation));
            Assert.That(scopedSimulation.Children[1].FindInScope<IModel>("asdf"), Is.EqualTo(scopedSimulation));
            Assert.That(scopedSimulation.Children[0].FindInScope<Clock>("asdf"), Is.EqualTo(scopedSimulation.Children[0]));
            Assert.That(scopedSimulation.Children[0].FindInScope<IModel>("asdf"), Is.EqualTo(scopedSimulation));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllAncestors()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllAncestors()
        {
            // The top-level model has no ancestors.
            Assert.That(simpleModel.FindAllAncestors().ToArray(), Is.EqualTo(new IModel[0]));

            Assert.That(container.FindAllAncestors().ToArray(), Is.EqualTo(new[] { simpleModel }));

            // Ancestors should be in bottom-up order.
            Assert.That(noSiblings.FindAllAncestors().ToArray(), Is.EqualTo(new[] { folder3, simpleModel }));

            // Note this test may break if we implement caching for this function.
            container.Parent = null;
            Assert.That(folder1.FindAllAncestors(), Is.EqualTo(new[] { container }));

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
            Assert.That(noSiblings.FindAllDescendants().Count(), Is.EqualTo(0));

            // Descendants should be in depth-first search order.
            Assert.That(simpleModel.FindAllDescendants().ToArray(), Is.EqualTo(new[] { container, folder1, folder2, folder3, noSiblings }));
            Assert.That(container.FindAllDescendants().ToArray(), Is.EqualTo(new[] { folder1, folder2 }));
            Assert.That(folder3.FindAllDescendants().ToArray(), Is.EqualTo(new[] { noSiblings }));

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
            Assert.That(simpleModel.FindAllSiblings().Count(), Is.EqualTo(0));

            // No siblings - expect empty enumerable (not null).
            Assert.That(noSiblings.FindAllSiblings().Count(), Is.EqualTo(0));

            // 1 sibling.
            Assert.That(folder1.FindAllSiblings().ToArray(), Is.EqualTo(new[] { folder2 }));

            // Many siblings.
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
            IModel test = new MockModel() { Name = "test", Parent = folder1.Parent };
            folder1.Parent.Children.Add(folder4);
            folder1.Parent.Children.Add(test);
            Assert.That(folder1.FindAllSiblings().ToArray(), Is.EqualTo(new[] { folder2, folder4, test }));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllChildren()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllChildren()
        {
            // No children - expect empty enumerable (not null).
            Assert.That(folder1.FindAllChildren().Count(), Is.EqualTo(0));

            // 1 child.
            Assert.That(folder3.FindAllChildren().ToArray(), Is.EqualTo(new[] { noSiblings }));

            // Many children.
            IModel folder4 = new Folder() { Name = "folder4", Parent = container };
            IModel test = new MockModel() { Name = "test", Parent = container };
            container.Children.Add(folder4);
            container.Children.Add(test);
            Assert.That(container.FindAllChildren().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4, test }));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllInScope()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllInScope()
        {
            // Find all from the top-level model should work.
            Assert.That(scopedSimulation.FindAllInScope().Count(), Is.EqualTo(14));

            // Find all should fail if the top-level is not scoped.
            // Can't enable this check until some refactoring of scoping code.
            //Assert.Throws<Exception>(() => simpleModel.FindAll().Count());

            // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
            // Note that the manager is not in scope. This is not desirable behaviour.
            var leaf1 = scopedSimulation.Children[2].Children[1].Children[0];
            List<IModel> inScopeOfLeaf1 = leaf1.FindAllInScope().ToList();
            Assert.That(inScopeOfLeaf1.Count, Is.EqualTo(11));
            Assert.That(inScopeOfLeaf1[0].Name, Is.EqualTo("Plant"));
            Assert.That(inScopeOfLeaf1[1].Name, Is.EqualTo("leaf1"));
            Assert.That(inScopeOfLeaf1[2].Name, Is.EqualTo("stem1"));
            Assert.That(inScopeOfLeaf1[3].Name, Is.EqualTo("zone1"));
            Assert.That(inScopeOfLeaf1[4].Name, Is.EqualTo("Soil"));
            Assert.That(inScopeOfLeaf1[5].Name, Is.EqualTo("Plant"));
            Assert.That(inScopeOfLeaf1[6].Name, Is.EqualTo("managerfolder"));
            Assert.That(inScopeOfLeaf1[7].Name, Is.EqualTo("Simulation"));
            Assert.That(inScopeOfLeaf1[8].Name, Is.EqualTo("Clock"));
            Assert.That(inScopeOfLeaf1[9].Name, Is.EqualTo("MockSummary"));
            Assert.That(inScopeOfLeaf1[10].Name, Is.EqualTo("zone2"));

            // Ensure correct scoping from soil
            var soil = scopedSimulation.Children[2].Children[0];
            List<IModel> inScopeOfSoil = soil.FindAllInScope().ToList();
            Assert.That(inScopeOfSoil.Count, Is.EqualTo(14));
            Assert.That(inScopeOfSoil[0].Name, Is.EqualTo("zone1"));
            Assert.That(inScopeOfSoil[1].Name, Is.EqualTo("Soil"));
            Assert.That(inScopeOfSoil[2].Name, Is.EqualTo("Plant"));
            Assert.That(inScopeOfSoil[3].Name, Is.EqualTo("leaf1"));
            Assert.That(inScopeOfSoil[4].Name, Is.EqualTo("stem1"));
            Assert.That(inScopeOfSoil[5].Name, Is.EqualTo("Plant"));
            Assert.That(inScopeOfSoil[6].Name, Is.EqualTo("leaf2"));
            Assert.That(inScopeOfSoil[7].Name, Is.EqualTo("stem2"));
            Assert.That(inScopeOfSoil[8].Name, Is.EqualTo("managerfolder"));
            Assert.That(inScopeOfSoil[9].Name, Is.EqualTo("manager"));
            Assert.That(inScopeOfSoil[10].Name, Is.EqualTo("Simulation"));
            Assert.That(inScopeOfSoil[11].Name, Is.EqualTo("Clock"));
            Assert.That(inScopeOfSoil[12].Name, Is.EqualTo("MockSummary"));
            Assert.That(inScopeOfSoil[13].Name, Is.EqualTo("zone2"));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllAncestors{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeAncestors()
        {
            // No parent - expect empty enumerable (not null).
            Assert.That(simpleModel.FindAllAncestors<IModel>().Count(), Is.EqualTo(0));

            // Container has no MockModel2 ancestors.
            Assert.That(container.FindAllAncestors<MockModel2>().Count(), Is.EqualTo(0));

            // Container's only ancestor is simpleModel.
            Assert.That(container.FindAllAncestors<IModel>().ToArray(), Is.EqualTo(new[] { simpleModel }));
            Assert.That(container.FindAllAncestors<Model>().ToArray(), Is.EqualTo(new[] { simpleModel }));

            IModel folder4 = new Folder() { Name = "folder4", Parent = folder3 };
            folder3.Children.Add(folder4);
            IModel folder5 = new Folder() { Name = "folder5", Parent = folder4 };
            folder4.Children.Add(folder5);

            Assert.That(folder5.FindAllAncestors<MockModel2>().Count(), Is.EqualTo(0));
            Assert.That(folder5.FindAllAncestors<Folder>().ToArray(), Is.EqualTo(new[] { folder4, folder3 }));
            Assert.That(folder5.FindAllAncestors<IModel>().ToArray(), Is.EqualTo(new[] { folder4, folder3, simpleModel }));
            Assert.That(folder5.FindAllAncestors<Model>().ToArray(), Is.EqualTo(new[] { folder4, folder3, simpleModel }));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllDescendants{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeDescendants()
        {
            // No children - expect empty enumerable (not null).
            Assert.That(noSiblings.FindAllDescendants<IModel>().Count(), Is.EqualTo(0));

            // No matches - expect empty enumerable (not null).
            Assert.That(simpleModel.FindAllDescendants<MockModel2>().Count(), Is.EqualTo(0));

            // 1 match.
            Assert.That(simpleModel.FindAllDescendants<MockModel1>().ToArray(), Is.EqualTo(new[] { simpleModel.Children[0] }));

            // Many matches - expect depth-first search.
            Assert.That(simpleModel.FindAllDescendants<Folder>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder3 }));
            Assert.That(simpleModel.FindAllDescendants<IModel>().ToArray(), Is.EqualTo(new[] { container, folder1, folder2, folder3, noSiblings }));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllSiblings{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeSiblings()
        {
            // No parent - expect empty enumerable (not null).
            Assert.That(simpleModel.FindAllSiblings<IModel>().Count(), Is.EqualTo(0));

            // No siblings - expect empty enumerable (not null).
            Assert.That(noSiblings.FindAllSiblings<IModel>().Count(), Is.EqualTo(0));

            // No siblings of correct type - expect empty enumerable (not null).
            Assert.That(folder1.FindAllSiblings<MockModel2>().Count(), Is.EqualTo(0));

            // 1 sibling of correct type.
            Assert.That(folder1.FindAllSiblings<Folder>().ToArray(), Is.EqualTo(new[] { folder2 }));

            // Many siblings of correct type - expect first sibling which matches.
            IModel folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
            IModel test = new MockModel() { Name = "test", Parent = folder1.Parent };
            folder1.Parent.Children.Add(folder4);
            folder1.Parent.Children.Add(test);
            Assert.That(folder1.FindAllSiblings<Folder>().ToArray(), Is.EqualTo(new[] { folder2, folder4 }));
            Assert.That(folder1.FindAllSiblings<IModel>().ToArray(), Is.EqualTo(new[] { folder2, folder4, test }));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllChildren{T}()"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByTypeChildren()
        {
            // No children - expect empty enumerable (not null).
            Assert.That(folder2.FindAllChildren<IModel>().Count(), Is.EqualTo(0));
            Assert.That(folder2.FindAllChildren<MockModel1>().Count(), Is.EqualTo(0));
            Assert.That(folder2.FindAllChildren<MockModel2>().Count(), Is.EqualTo(0));

            // No children of correct type - expect empty enumerable (not null).
            Assert.That(container.FindAllChildren<MockModel2>().Count(), Is.EqualTo(0));

            // 1 child of correct type.
            Assert.That(simpleModel.FindAllChildren<Folder>().ToArray(), Is.EqualTo(new[] { folder3 }));

            // Many children of correct type - expect first child which matches.
            IModel folder4 = new Folder() { Name = "folder4", Parent = container };
            IModel test = new MockModel() { Name = "test", Parent = container };
            container.Children.Add(folder4);
            container.Children.Add(test);
            Assert.That(container.FindAllChildren<Folder>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4 }));
            Assert.That(container.FindAllChildren<Model>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4, test }));
            Assert.That(container.FindAllChildren<IModel>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4, test }));
            Assert.That(simpleModel.FindAllChildren<IModel>().ToArray(), Is.EqualTo(new[] { container, folder3 }));
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
            Assert.That(leaf1.FindAllInScope<Summary>().Count(), Is.EqualTo(0));

            // 1 match.
            Assert.That(leaf1.FindAllInScope<Clock>().ToArray(), Is.EqualTo(new[] { scopedSimulation.Children[0] }));

            // Many matches - test order.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel[] allPlants = new[] { plant1, plant2 };
            Assert.That(managerFolder.FindAllInScope<Plant>().ToArray(), Is.EqualTo(allPlants));
            Assert.That(plant1.FindAllInScope<Plant>().ToArray(), Is.EqualTo(allPlants));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllAncestors(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameAncestors()
        {
            // No parent - expect empty enumerable (not null).
            Assert.That(simpleModel.FindAllAncestors("Test").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllAncestors("").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllAncestors(null).Count(), Is.EqualTo(0));

            // No ancestors of correct name - expect empty enumerable.
            Assert.That(container.FindAllAncestors("asdf").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllAncestors("").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllAncestors(null).Count(), Is.EqualTo(0));

            // 1 ancestor of correct name.
            Assert.That(container.FindAllAncestors("Test").ToArray(), Is.EqualTo(new[] { simpleModel }));

            // Multiple ancestors with correct name - expect bottom-up search.
            IModel folder4 = new Folder() { Name = "folder3", Parent = folder3 };
            folder3.Children.Add(folder4);
            IModel folder5 = new Folder() { Name = "folder3", Parent = folder4 };
            folder4.Children.Add(folder5);

            Assert.That(folder5.FindAllAncestors("").Count(), Is.EqualTo(0));
            Assert.That(folder5.FindAllAncestors(null).Count(), Is.EqualTo(0));
            Assert.That(folder5.FindAllAncestors("folder3").ToArray(), Is.EqualTo(new[] { folder4, folder3 }));
            Assert.That(folder5.FindAllAncestors("Test").ToArray(), Is.EqualTo(new[] { simpleModel }));
            Assert.That(folder4.FindAllAncestors("folder3").ToArray(), Is.EqualTo(new[] { folder3 }));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllDescendants(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameDescendants()
        {
            // No children - expect empty enumerable.
            Assert.That(noSiblings.FindAllDescendants("x").Count(), Is.EqualTo(0));

            // No descendants with correct name - expect empty enumerable.
            Assert.That(simpleModel.FindAllDescendants("x").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllDescendants(null).Count(), Is.EqualTo(0));

            // 1 descendant with correct name.
            Assert.That(simpleModel.FindAllDescendants("Container").ToArray(), Is.EqualTo(new[] { container }));

            // Many descendants with correct name - expect results in depth-first order.
            IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
            container.Children.Add(folder4);
            IModel folder5 = new MockModel() { Parent = folder1, Name = "folder1" };
            folder1.Children.Add(folder5);

            Assert.That(simpleModel.FindAllDescendants("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder5, folder4 }));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllSiblings(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameSiblings()
        {
            // No parent - expect empty enumerable.
            Assert.That(simpleModel.FindAllSiblings("Test").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllSiblings("").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllSiblings(null).Count(), Is.EqualTo(0));

            // No siblings - expect empty enumerable.
            Assert.That(noSiblings.FindAllSiblings("anything").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllSiblings("nosiblings").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllSiblings("").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllSiblings(null).Count(), Is.EqualTo(0));

            // No siblings of correct name - expect empty enumerable.
            Assert.That(folder1.FindAllSiblings("x").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllSiblings("folder1").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllSiblings("").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllSiblings(null).Count(), Is.EqualTo(0));

            // 1 sibling of correct name.
            Assert.That(folder1.FindAllSiblings("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));

            // Many siblings of correct name, expect them in indexed order.
            IModel folder4 = new Folder() { Name = "folder2", Parent = container };
            folder1.Parent.Children.Add(folder4);
            Assert.That(folder1.FindAllSiblings("folder2").ToArray(), Is.EqualTo(new[] { folder2, folder4 }));
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindAllChildren(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindAllByNameChildren()
        {
            // No children - expect empty enumerable.
            Assert.That(folder1.FindAllChildren("Test").Count(), Is.EqualTo(0));
            Assert.That(folder2.FindAllChildren("").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllChildren(null).Count(), Is.EqualTo(0));

            // No children of correct name - expect empty enumerable.
            Assert.That(container.FindAllChildren("x").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllChildren("Test").Count(), Is.EqualTo(0));
            Assert.That(folder3.FindAllChildren("folder3").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllChildren(null).Count(), Is.EqualTo(0));

            // 1 child of correct name.
            Assert.That(container.FindAllChildren("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));
            Assert.That(simpleModel.FindAllChildren("Container").ToArray(), Is.EqualTo(new[] { container }));

            // Many (but not all) children of correct name, expect them in indexed order.
            IModel folder4 = new Folder() { Name = "folder2", Parent = container };
            container.Children.Add(folder4);
            Assert.That(container.FindAllChildren("folder2").ToArray(), Is.EqualTo(new[] { folder2, folder4 }));

            // All (>1) children have correct name.
            container.Children.Remove(folder1);
            Assert.That(container.FindAllChildren("folder2").ToArray(), Is.EqualTo(new[] { folder2, folder4 }));
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
            Assert.That(leaf1.FindAllInScope("x").Count(), Is.EqualTo(0));
            Assert.That(leaf1.FindAllInScope("").Count(), Is.EqualTo(0));
            Assert.That(leaf1.FindAllInScope(null).Count(), Is.EqualTo(0));

            // 1 match.
            Assert.That(leaf1.FindAllInScope("zone1").ToArray(), Is.EqualTo(new[] { scopedSimulation.Children[2] }));

            // Many matches - expect first.
            IModel plant1 = scopedSimulation.Children[2].Children[1];
            IModel plant2 = scopedSimulation.Children[2].Children[2];
            IModel managerFolder = scopedSimulation.Children[2].Children[3];
            IModel clock = scopedSimulation.Children[0];
            IModel summary = scopedSimulation.Children[1];
            Assert.That(managerFolder.FindAllInScope("Plant").ToArray(), Is.EqualTo(new[] { plant1, plant2 }));

            managerFolder.Name = "asdf";
            clock.Name = "asdf";
            scopedSimulation.Name = "asdf";
            Assert.That(leaf1.FindAllInScope("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
            Assert.That(plant1.FindAllInScope("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
            Assert.That(summary.FindAllInScope("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation, clock, managerFolder }));
            Assert.That(clock.FindAllInScope("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation, clock, managerFolder }));
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
            Assert.That(simpleModel.FindAllAncestors<Model>("Test").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllAncestors<IModel>("Test").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllAncestors<IModel>("").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllAncestors<IModel>(null).Count(), Is.EqualTo(0));

            // A model is not its own ancestor.
            Assert.That(container.FindAllAncestors<MockModel1>(null).Count(), Is.EqualTo(0));
            Assert.That(container.FindAllAncestors<MockModel1>("").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllAncestors<MockModel1>("Container").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllAncestors<IModel>("Container").Count(), Is.EqualTo(0));

            // Ancestor exists with correct type but incorrect name.
            Assert.That(folder1.FindAllAncestors<MockModel1>(null).Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllAncestors<MockModel1>("").Count(), Is.EqualTo(0));

            // Ancestor exists with correct name but incorrect type.
            Assert.That(folder1.FindAllAncestors<MockModel2>("Container").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllAncestors<Fertiliser>("Test").Count(), Is.EqualTo(0));

            // Ancestor exists with correct type but incorrect name.
            // Another ancestor exists with correct name but incorrect type.
            Assert.That(folder1.FindAllAncestors<MockModel1>("Test").Count(), Is.EqualTo(0));

            // 1 match.
            Assert.That(folder1.FindAllAncestors<MockModel1>("Container").ToArray(), Is.EqualTo(new[] { container }));
            Assert.That(folder2.FindAllAncestors<Model>("Container").ToArray(), Is.EqualTo(new[] { container }));
            Assert.That(noSiblings.FindAllAncestors<IModel>("Test").ToArray(), Is.EqualTo(new[] { simpleModel }));
            Assert.That(noSiblings.FindAllAncestors<IModel>("folder3").ToArray(), Is.EqualTo(new[] { folder3 }));

            // Multiple matches - ensure ordering is bottom-up.
            Assert.That(folder5.FindAllAncestors<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4, folder1 }));

            // An uncle/cousin is not an ancestor.
            folder2.Name = "folder1";
            Assert.That(folder5.FindAllAncestors<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4, folder1 }));

            // Test case-insensitive search.
            Assert.That(noSiblings.FindAllAncestors<Folder>("FoLdEr3").ToArray(), Is.EqualTo(new[] { folder3 }));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllDescendants{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindDescendantsByTypeAndName()
        {
            // No matches - expect empty enumerable.
            Assert.That(simpleModel.FindAllDescendants<MockModel2>("").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllDescendants<MockModel2>("Container").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllDescendants<MockModel2>(null).Count(), Is.EqualTo(0));

            // No children - expect enumerable.
            Assert.That(noSiblings.FindAllDescendants<IModel>("").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllDescendants<IModel>(null).Count(), Is.EqualTo(0));

            // Descendants exist with correct type but incorrect name.
            Assert.That(container.FindAllDescendants<Folder>("").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllDescendants<Folder>(null).Count(), Is.EqualTo(0));
            Assert.That(folder3.FindAllDescendants<IModel>("x").Count(), Is.EqualTo(0));

            // Descendants exist with correct name but incorrect type.
            Assert.That(container.FindAllDescendants<MockModel2>("folder1").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllDescendants<Irrigation>("nosiblings").Count(), Is.EqualTo(0));

            // Descendant exists with correct type but incorrect name.
            // Another descendant exists with correct name but incorrect type.
            Assert.That(simpleModel.FindAllDescendants<MockModel1>("folder2").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllDescendants<Folder>("nosiblings").Count(), Is.EqualTo(0));

            // 1 match.
            Assert.That(simpleModel.FindAllDescendants<MockModel1>("Container").ToArray(), Is.EqualTo(new[] { container }));
            Assert.That(container.FindAllDescendants<IModel>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));
            Assert.That(folder3.FindAllDescendants<Model>("nosiblings").ToArray(), Is.EqualTo(new[] { noSiblings }));

            // Many matches - expect first in depth-first search is returned.
            IModel folder4 = new Folder() { Name = "folder1", Parent = folder1 };
            IModel folder5 = new Folder() { Name = "folder1", Parent = folder4 };
            IModel folder6 = new Folder() { Name = "folder1", Parent = container };
            folder1.Children.Add(folder4);
            folder4.Children.Add(folder5);
            container.Children.Add(folder6);
            folder3.Name = "folder1";
            noSiblings.Name = "folder1";

            Assert.That(simpleModel.FindAllDescendants<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder4, folder5, folder6, folder3 }));
            Assert.That(container.FindAllDescendants<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder4, folder5, folder6 }));
            Assert.That(folder1.FindAllDescendants<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4, folder5 }));
            Assert.That(folder4.FindAllDescendants<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder5 }));

            // Test case-insensitive search.
            Assert.That(simpleModel.FindAllDescendants<IModel>("fOLDer2").ToArray(), Is.EqualTo(new[] { folder2 }));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllSiblings{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindSiblingsByTypeAndName()
        {
            // No parent - expect empty enumerable.
            Assert.That(simpleModel.FindAllSiblings<IModel>("Test").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllSiblings<IModel>("").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllSiblings<IModel>(null).Count(), Is.EqualTo(0));

            // No siblings - expect empty enumerable.
            Assert.That(noSiblings.FindAllSiblings<IModel>("").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllSiblings<IModel>("nosiblings").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllSiblings<IModel>(null).Count(), Is.EqualTo(0));

            // A model is not its own sibling.
            Assert.That(folder1.FindAllSiblings<Folder>("folder1").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllSiblings<IModel>("folder1").Count(), Is.EqualTo(0));

            // Siblings exist with correct name but incorrect type.
            Assert.That(folder1.FindAllSiblings<MockModel2>("folder2").Count(), Is.EqualTo(0));
            Assert.That(folder2.FindAllSiblings<Fertiliser>("Container").Count(), Is.EqualTo(0));

            // Siblings exist with correct type but incorrect name.
            Assert.That(folder1.FindAllSiblings<Folder>("").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllSiblings<Folder>(null).Count(), Is.EqualTo(0));
            Assert.That(container.FindAllSiblings<Folder>("folder1").Count(), Is.EqualTo(0));

            // 1 sibling of correct type and name.
            Assert.That(folder1.FindAllSiblings<Folder>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));

            // Many siblings of correct type and name - expect indexed order.
            IModel folder4 = new Folder() { Name = "folder1", Parent = container };
            container.Children.Add(folder4);
            Assert.That(folder2.FindAllSiblings<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder4 }));
            Assert.That(folder1.FindAllSiblings<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4 }));

            // Test case-insensitive search.
            Assert.That(folder1.FindAllSiblings<Folder>("fOlDeR2").ToArray(), Is.EqualTo(new[] { folder2 }));
        }

        /// <summary>
        /// Tests the <see cref="IModel.FindAllChildren{T}(string)"/> method.
        /// </summary>
        [Test]
        public void TestFindChildrenByTypeAndName()
        {
            // No children- expect empty enumerable.
            Assert.That(folder1.FindAllChildren<IModel>("folder1").Count(), Is.EqualTo(0));
            Assert.That(folder1.FindAllChildren<IModel>("folder2").Count(), Is.EqualTo(0));
            Assert.That(folder2.FindAllChildren<IModel>("Container").Count(), Is.EqualTo(0));
            Assert.That(folder2.FindAllChildren<IModel>("").Count(), Is.EqualTo(0));
            Assert.That(noSiblings.FindAllChildren<IModel>(null).Count(), Is.EqualTo(0));

            // Children exist with correct name but incorrect type.
            Assert.That(container.FindAllChildren<MockModel2>("folder2").Count(), Is.EqualTo(0));
            Assert.That(folder3.FindAllChildren<Fertiliser>("nosiblings").Count(), Is.EqualTo(0));

            // Children exist with correct type but incorrect name.
            Assert.That(container.FindAllChildren<Folder>("folder3").Count(), Is.EqualTo(0));
            Assert.That(container.FindAllChildren<IModel>(null).Count(), Is.EqualTo(0));
            Assert.That(folder3.FindAllChildren<Model>("Test").Count(), Is.EqualTo(0));
            Assert.That(simpleModel.FindAllChildren<Model>("").Count(), Is.EqualTo(0));

            // 1 child of correct type and name.
            Assert.That(container.FindAllChildren<Folder>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));

            // Many siblings of correct type and name - expect indexed order.
            IModel folder4 = new Folder() { Name = "folder1", Parent = container };
            container.Children.Add(folder4);
            Assert.That(container.FindAllChildren<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder4 }));
            folder3.Name = "Container";
            Assert.That(simpleModel.FindAllChildren<IModel>("Container").ToArray(), Is.EqualTo(new[] { container, folder3 }));

            // Test case-insensitive search.
            Assert.That(container.FindAllChildren<Folder>("fOlDeR2").ToArray(), Is.EqualTo(new[] { folder2 }));
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
            Assert.That(leaf1.FindAllInScope<MockModel2>("x").Count(), Is.EqualTo(0));
            Assert.That(leaf1.FindAllInScope<MockModel2>(null).Count(), Is.EqualTo(0));

            // Model exists in scope with correct name but incorrect type.
            Assert.That(leaf1.FindAllInScope<MockModel2>("Plant").Count(), Is.EqualTo(0));

            // Model exists in scope with correct type but incorrect name.
            Assert.That(leaf1.FindAllInScope<Zone>("*").Count(), Is.EqualTo(0));
            Assert.That(leaf1.FindAllInScope<Zone>(null).Count(), Is.EqualTo(0));
            Assert.That(leaf1.FindAllInScope<Zone>("Plant").Count(), Is.EqualTo(0));

            // 1 match.
            Assert.That(leaf1.FindAllInScope<Zone>("zone1").ToArray(), Is.EqualTo(new[] { scopedSimulation.Children[2] }));

            managerFolder.Name = "asdf";
            scopedSimulation.Children[0].Name = "asdf";
            scopedSimulation.Name = "asdf";

            Assert.That(plant1.FindAllInScope<Simulation>("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation }));
            Assert.That(clock.FindAllInScope<Clock>("asdf").ToArray(), Is.EqualTo(new[] { clock }));

            // Many matches - expect first.
            Assert.That(managerFolder.FindAllInScope<Plant>("Plant").ToArray(), Is.EqualTo(new[] { plant1, plant2 }));

            Assert.That(leaf1.FindAllInScope<IModel>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
            Assert.That(plant1.FindAllInScope<IModel>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
            Assert.That(summary.FindAllInScope<IModel>("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation, clock, managerFolder }));
            Assert.That(plant2.FindAllInScope<IModel>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
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
                new CompositeFactor()
            };

            foreach (IModel anyChild in allowAnyChild)
            {
                // Any Model can be added to a folder
                Assert.That(anyChild.IsChildAllowable(typeof(DropOnSimulations)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(DropOnFolder)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(MockModel1)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(DropAnywhere)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(ConflictingDirectives)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(ConflictedButReversed)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(NoValidParents)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(SomeFunction)), Is.True);
                Assert.That(anyChild.IsChildAllowable(typeof(SomeFunction)), Is.True);

                // If it's not a model it cannot be added as a child.
                Assert.That(anyChild.IsChildAllowable(typeof(object)), Is.False);

                // Even if it has a ValidParent attribute.
                Assert.That(anyChild.IsChildAllowable(typeof(NotAModel)), Is.False);

                // Even if it's also an IFunction.
                Assert.That(anyChild.IsChildAllowable(typeof(SimsIFunction)), Is.False);

                // Simulations object also cannot be added to anything.
                Assert.That(anyChild.IsChildAllowable(typeof(Simulations)), Is.False);
            }

            // Simulations object cannot be added to anything.
            Assert.That(container.IsChildAllowable(typeof(Simulations)), Is.False);
            Assert.That(folder2.IsChildAllowable(typeof(Simulations)), Is.False);
            Assert.That(simpleModel.IsChildAllowable(typeof(Simulations)), Is.False);
            Assert.That(new Simulations().IsChildAllowable(typeof(Simulations)), Is.False);
            Assert.That(new SomeFunction().IsChildAllowable(typeof(Simulations)), Is.False);

            // IFunctions can be added to anything.
            Assert.That(simpleModel.IsChildAllowable(typeof(SomeFunction)), Is.True);
            Assert.That(new Simulations().IsChildAllowable(typeof(SomeFunction)), Is.True);
            Assert.That(new MockModel().IsChildAllowable(typeof(SomeFunction)), Is.True);

            // Otherwise, the validity of a child model depends on it sspecific
            // valid parents, as defined in its valid parent attributes.
            Assert.That(new NoValidParents().IsChildAllowable(typeof(CanAddToNoValidParents)), Is.True);
            Assert.That(new NoValidParents().IsChildAllowable(typeof(DropAnywhere)), Is.True);
            Assert.That(new MockModel().IsChildAllowable(typeof(DropAnywhere)), Is.True);
            Assert.That(new NoValidParents().IsChildAllowable(typeof(NoValidParents)), Is.True);
            Assert.That(new MockModel().IsChildAllowable(typeof(NoValidParents)), Is.True);
            Assert.That(new CanAddToNoValidParents().IsChildAllowable(typeof(CanAddToNoValidParents)), Is.False);
            Assert.That(new CanAddToNoValidParents().IsChildAllowable(typeof(DropAnywhere)), Is.True);
            Assert.That(new MockModel().IsChildAllowable(typeof(DropAnywhere)), Is.True);
        }

        /// <summary>
        /// Tests for the <see cref="IModel.FindByPath(string, LocatorFlags)"/> method.
        /// </summary>
        [Test]
        public void TestFindInPath()
        {
            // 1. Absolute paths.
            Assert.That(simpleModel.FindByPath(".Test.Container").Value, Is.EqualTo(container));
            Assert.That(simpleModel.FindByPath(".Test").Value, Is.EqualTo(simpleModel));
            Assert.That(simpleModel.FindByPath(".Test.folder3.nosiblings").Value, Is.EqualTo(noSiblings));
            Assert.That(simpleModel.FindByPath(".Test.folder3.Children[1]").Value, Is.EqualTo(noSiblings));
            Assert.That(simpleModel.FindByPath(".Test.folder3.asdf"), Is.Null);
            Assert.That(simpleModel.FindByPath(".asdf"), Is.Null);
            Assert.That(simpleModel.FindByPath(""), Is.Null);
            Assert.That(simpleModel.FindByPath(null), Is.Null);

            // Do not allow type names in absolute path.
            Assert.That(simpleModel.FindByPath(".Model.Folder.Model"), Is.Null);

            // Absolute path with variable.
            Assert.That(simpleModel.FindByPath(".Test.Container.folder2.Name").Value, Is.EqualTo("folder2"));
            Assert.That(simpleModel.FindByPath(".Test.Name").GetType(), Is.EqualTo(typeof(VariableComposite)));

            // 2. Relative paths.
            Assert.That(simpleModel.FindByPath("[Test]").Value, Is.EqualTo(simpleModel));
            Assert.That(simpleModel.FindByPath("[folder1]").Value, Is.EqualTo(folder1));
            Assert.That(simpleModel.FindByPath("[folder3].nosiblings").Value, Is.EqualTo(noSiblings));
            Assert.That(simpleModel.FindByPath("[Test].Container.folder2").Value, Is.EqualTo(folder2));
            Assert.That(simpleModel.FindByPath("[Test].Children[2]").Value, Is.EqualTo(folder3));
            Assert.That(simpleModel.FindByPath("[asdf]"), Is.Null);
            Assert.That(simpleModel.FindByPath("[folder3].foo"), Is.Null);

            // Do not allow type names in relative path.
            Assert.That(simpleModel.FindByPath(".Model.Folder"), Is.Null);

            // Relative path with variable.
            Assert.That(simpleModel.FindByPath("[Container].Name").Value, Is.EqualTo("Container"));
            Assert.That(simpleModel.FindByPath("[Container].Name").GetType(), Is.EqualTo(typeof(VariableComposite)));

            // 3. Child paths.
            Assert.That(simpleModel.FindByPath("Container").Value, Is.EqualTo(container));
            Assert.That(simpleModel.FindByPath("folder3.nosiblings").Value, Is.EqualTo(noSiblings));
            Assert.That(container.FindByPath("folder2").Value, Is.EqualTo(folder2));
            Assert.That(simpleModel.FindByPath("folder2"), Is.Null);
            Assert.That(simpleModel.FindByPath("x"), Is.Null);
            Assert.That(simpleModel.FindByPath(""), Is.Null);
            Assert.That(simpleModel.FindByPath(null), Is.Null);
        }

        /// <summary>
        /// Ensure that duplicate models (ie siblings with the same name) cause
        /// an exception to be thrown.
        /// </summary>
        [Test]
        public void TestDuplicateModelDetection()
        {
            MockModel3 model = new MockModel3("Parent");
            model.Children.Add(new MockModel3("Child"));
            model.Children.Add(new MockModel3("Child"));
            Assert.Throws<Exception>(() => model.OnCreated());
        }

        /// <summary>
        /// Ensure that duplicate models (ie siblings with the same name) but
        /// with different types cause an exception to be thrown.
        /// </summary>
        [Test]
        public void TestDuplicateModelsWithDifferentTypes()
        {
            MockModel3 model = new MockModel3("Some model");
            model.Children.Add(new MockModel3("A child"));
            model.Children.Add(new MockModel2() { Name = "A child" });
            Assert.Throws<Exception>(() => model.OnCreated());
        }
    }
}
