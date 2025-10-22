﻿using Models.Core;
using Models.Soils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Factorial;
using Models.PMF;
using Models.PMF.Organs;
using UnitTests;

namespace APSIM.Core.Tests;

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
        Node.Create(simpleModel as INodeModel);


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
        Node.Create(scopedSimulation as INodeModel);
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
    /// Test the FindAncestor(string) method.
    /// </summary>
    [Test]
    public void TestFindByNameAncestor()
    {
        var folder4 = new Folder() { Name = "folder1", Parent = folder1 };
        var folder5 = new Folder() { Name = "folder5", Parent = folder4 };
        folder1.Node.AddChild(folder4);
        folder4.Node.AddChild(folder5);

        // No parent - expect null.
        Assert.That(simpleModel.Node.FindParent<IModel>("x", recurse: true), Is.Null);

        // A model is not its own ancestor.
        Assert.That(container.Node.FindParent<IModel>("Container", recurse: true), Is.Null);

        // No matches - expect null.
        Assert.That(noSiblings.Node.FindParent<IModel>("x", recurse: true), Is.Null);

        // 1 match.
        Assert.That(container.Node.FindParent<IModel>("Test", recurse: true), Is.EqualTo(simpleModel));

        // When multiple ancestors match the name, ensure closest is returned.
        Assert.That(folder5.Node.FindParent<IModel>("folder1", recurse: true), Is.EqualTo(folder4));

        Assert.That(folder5.Node.FindParent<IModel>("Container", recurse: true), Is.EqualTo(container));
        Assert.That(folder4.Node.FindParent<IModel>("Container", recurse: true), Is.EqualTo(container));
    }

    /// <summary>
    /// Tests the FindDescendant(string) method.
    /// </summary>
    [Test]
    public void TestFindByNameDescendant()
    {
        // No children - expect null.
        Assert.That(noSiblings.Node.FindChild<IModel>("x", recurse: true), Is.Null);

        // No matches - expect null.
        Assert.That(simpleModel.Node.FindChild<IModel>("x", recurse: true), Is.Null);

        // 1 match.
        Assert.That(simpleModel.Node.FindChild<IModel>("Container", recurse: true), Is.EqualTo(container));

        // Many matches - expect first in depth-first search is returned.
        IModel folder4 = new MockModel2() { Parent = container, Name = "folder1" };
        container.Children.Add(folder4);
        IModel folder5 = new MockModel() { Parent = folder1, Name = "folder1" };
        folder1.Children.Add(folder5);

        Assert.That(simpleModel.Node.FindChild<IModel>("folder1", recurse: true), Is.EqualTo(folder1));
    }

    /// <summary>
    /// Tests the FindSibling method.
    /// </summary>
    [Test]
    public void TestFindSiblingByName()
    {
        // No parent - expect null.
        Assert.That(simpleModel.Node.FindSibling<object>("anything"), Is.Null);

        // No siblings - expect null.
        Assert.That(noSiblings.Node.FindSibling<object>("anything"), Is.Null);

        // No siblings of correct name - expect null.
        Assert.That(folder1.Node.FindSibling<object>("x"), Is.Null);

        // 1 sibling of correct name.
        Assert.That(folder1.Node.FindSibling<Folder>("folder2"), Is.EqualTo(folder2));

        // Many siblings of correct name - expect first sibling which matches.
        // This isn't really a valid model setup but we'll test it anyway.
        folder1.Parent.Children.Add(new Folder()
        {
            Name = "folder2",
            Parent = folder1.Parent
        });
        Assert.That(folder1.Node.FindSibling<Folder>("folder2"), Is.EqualTo(folder2));
    }

    /// <summary>
    /// Tests the FindChild method.
    /// </summary>
    [Test]
    public void TestFindChildByName()
    {
        // No children - expect null.
        Assert.That(noSiblings.Node.FindChild<IModel>("*"), Is.Null);
        Assert.That(noSiblings.Node.FindChild<IModel>(""), Is.Null);
        Assert.That(noSiblings.Node.FindChild<IModel>(null), Is.Null);

        // No children of correct name - expect null.
        Assert.That(folder3.Node.FindChild<IModel>("x"), Is.Null);
        Assert.That(simpleModel.Node.FindChild<IModel>("folder2"), Is.Null);

        // 1 child of correct name.
        Assert.That(container.Node.FindChild<IModel>("folder2"), Is.EqualTo(folder2));
        Assert.That(simpleModel.Node.FindChild<IModel>("folder3"), Is.EqualTo(folder3));

        // Many children of correct name - expect first child which matches.
        // This isn't really a valid model setup but we'll test it anyway.
        container.Children.Add(new Folder()
        {
            Name = "folder2",
            Parent = folder1.Parent
        });
        Assert.That(container.Node.FindChild<IModel>("folder2"), Is.EqualTo(folder2));
    }

    /// <summary>
    /// Tests the scope find method method.
    /// </summary>
    [Test]
    public void TestFindByNameScoped()
    {
        IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

        // This will throw because there is no scoped parent model.
        // Can't uncomment this until we refactor the scoping code.
        //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

        // No matches - expect null.
        Assert.That(leaf1.Node.Find<object>("x"), Is.Null);
        Assert.That(leaf1.Node.Find<object>(null), Is.Null);

        // 1 match.
        Assert.That(leaf1.Node.Find<object>("zone1"), Is.EqualTo(scopedSimulation.Children[2]));

        // Many matches - expect first.
        IModel plant1 = scopedSimulation.Children[2].Children[1];
        IModel plant2 = scopedSimulation.Children[2].Children[2];
        IModel managerFolder = scopedSimulation.Children[2].Children[3];
        Assert.That(managerFolder.Node.Find<Plant>(), Is.EqualTo(plant1));

        // plant1 is actually in scope of itself. You could argue that this is
        // a bug (I think it is) but it is a problem for another day.
        Assert.That(plant1.Node.Find<object>("plant1"), Is.EqualTo(plant1));

        managerFolder.Node.Rename("asdf");
        scopedSimulation.Children[0].Node.Rename("asdf");
        scopedSimulation.Node.Rename("asdf");
        Assert.That(leaf1.Node.Find<object>("asdf"), Is.EqualTo(managerFolder));
        Assert.That(plant1.Node.Find<object>("asdf"), Is.EqualTo(managerFolder));
        Assert.That(scopedSimulation.Children[1].Node.Find<object>("asdf"), Is.EqualTo(scopedSimulation));
        Assert.That(scopedSimulation.Children[0].Node.Find<object>("asdf"), Is.EqualTo(scopedSimulation));
    }

    /// <summary>
    /// Test the FindAncestor method.
    /// </summary>
    [Test]
    public void TestFindByTypeAncestor()
    {
        var folder4 = new Folder() { Name = "folder4", Parent = folder1 };
        var folder5 = new Folder() { Name = "folder5", Parent = folder4 };
        folder1.Node.AddChild(folder4);
        folder4.Node.AddChild(folder5);

        // No parent - expect null.
        Assert.That(simpleModel.Node.FindParent<IModel>(recurse: true), Is.Null);

        // A model is not its own ancestor.
        Assert.That(container.Node.FindParent<MockModel1>(recurse: true), Is.Null);

        Assert.That(container.Node.FindParent<IModel>(recurse: true), Is.EqualTo(simpleModel));

        // When multiple ancestors match the type, ensure closest is returned.
        Assert.That(folder5.Node.FindParent<Folder>(recurse: true), Is.EqualTo(folder4));

        Assert.That(folder5.Node.FindParent<MockModel1>(recurse: true), Is.EqualTo(container));
        Assert.That(folder4.Node.FindParent<MockModel1>(recurse: true), Is.EqualTo(container));

        // Searching for any IModel ancestor should return the node's parent.
        Assert.That(folder1.Node.FindParent<IModel>(recurse: true), Is.EqualTo(folder1.Parent));
    }

    /// <summary>
    /// Tests the FindDescendant method.
    /// </summary>
    [Test]
    public void TestFindByTypeDescendant()
    {
        // No matches - expect null.
        Assert.That(simpleModel.Node.FindChild<MockModel2>(recurse: true), Is.Null);

        // No children - expect null.
        Assert.That(noSiblings.Node.FindChild<IModel>(recurse: true), Is.Null);

        // 1 match.
        Assert.That(simpleModel.Node.FindChild<MockModel1>(recurse: true), Is.EqualTo(container));
        Assert.That(simpleModel.Node.FindChild<IInterface>(recurse: true), Is.EqualTo(container));

        // Many matches - expect first in depth-first search is returned.
        Assert.That(simpleModel.Node.FindChild<Folder>(recurse: true), Is.EqualTo(folder1));
        Assert.That(simpleModel.Node.FindChild<IModel>(recurse: true), Is.EqualTo(container));
    }

    /// <summary>
    /// Tests the FindSibling method.
    /// </summary>
    [Test]
    public void TestFindByTypeSibling()
    {
        // No parent - expect null.
        Assert.That(simpleModel.Node.FindSibling<IModel>(), Is.Null);

        // No siblings - expect null.
        Assert.That(noSiblings.Node.FindSibling<IModel>(), Is.Null);

        // No siblings of correct type - expect null.
        Assert.That(folder1.Node.FindSibling<MockModel2>(), Is.Null);

        // 1 sibling of correct type.
        Assert.That(folder1.Node.FindSibling<Folder>(), Is.EqualTo(folder2));

        // Many siblings of correct type - expect first sibling which matches.
        folder1.Parent.Children.Add(new Folder()
        {
            Name = "folder4",
            Parent = folder1.Parent
        });
        Assert.That(folder1.Node.FindSibling<Folder>(), Is.EqualTo(folder2));
    }

    /// <summary>
    /// Tests the FindChild method.
    /// </summary>
    [Test]
    public void TestFindByTypeChild()
    {
        // No children - expect null.
        Assert.That(folder1.Node.FindChild<IModel>(), Is.Null);
        Assert.That(noSiblings.Node.FindChild<Model>(), Is.Null);

        // No children of correct type - expect null.
        Assert.That(simpleModel.Node.FindChild<MockModel2>(), Is.Null);
        Assert.That(folder3.Node.FindChild<MockModel1>(), Is.Null);

        // 1 child of correct type.
        Assert.That(simpleModel.Node.FindChild<MockModel1>(), Is.EqualTo(container));
        Assert.That(folder3.Node.FindChild<Model>(), Is.EqualTo(noSiblings));

        // Many children of correct type - expect first sibling which matches.
        Assert.That(container.Node.FindChild<Folder>(), Is.EqualTo(folder1));
        Assert.That(container.Node.FindChild<Model>(), Is.EqualTo(folder1));
        Assert.That(simpleModel.Node.FindChild<IModel>(), Is.EqualTo(container));
    }

    /// <summary>
    /// Tests the scope find method.
    /// </summary>
    [Test]
    public void TestFindByTypeScoped()
    {
        IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

        // This will throw because there is no scoped parent model.
        // Can't uncomment this until we refactor the scoping code.
        //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

        // No matches (there is an ISummary but no Summary) - expect null.
        Assert.That(leaf1.Node.Find<Summary>(), Is.Null);

        // 1 match.
        Assert.That(leaf1.Node.Find<Zone>(), Is.EqualTo(scopedSimulation.Children[2]));

        // Many matches - expect first.
        IModel plant1 = scopedSimulation.Children[2].Children[1];
        IModel plant2 = scopedSimulation.Children[2].Children[2];
        IModel managerFolder = scopedSimulation.Children[2].Children[3];
        Assert.That(managerFolder.Node.Find<Plant>(), Is.EqualTo(plant1));
        Assert.That(plant1.Node.Find<Plant>(), Is.EqualTo(plant1));
    }

    /// <summary>
    /// Test the FindAncestor method.
    /// </summary>
    [Test]
    public void TestFindAncestorByTypeAndName()
    {
        var folder4 = new Folder() { Name = "folder1", Parent = folder1 };
        var folder5 = new Folder() { Name = "folder1", Parent = folder4 };
        folder1.Node.AddChild(folder4);
        folder4.Node.AddChild(folder5);

        // No parent - expect null.
        Assert.That(simpleModel.Node.FindParent<IModel>("", recurse: true), Is.Null);
        Assert.That(simpleModel.Node.FindParent<IModel>(null, recurse: true), Is.Null);

        // A model is not its own ancestor.
        Assert.That(container.Node.FindParent<MockModel1>(null, recurse: true), Is.Null);
        Assert.That(container.Node.FindParent<MockModel1>("Container", recurse: true), Is.Null);

        // Ancestor exists with correct type but incorrect name.
        Assert.That(folder1.Node.FindParent<MockModel1>("", recurse: true), Is.Null);

        // Ancestor exists with correct name but incorrect type.
        Assert.That(folder1.Node.FindParent<MockModel2>("Container", recurse: true), Is.Null);

        // Ancestor exists with correct type but incorrect name.
        // Another ancestor exists with correct name but incorrect type.
        Assert.That(folder1.Node.FindParent<MockModel1>("Test", recurse: true), Is.Null);

        // 1 match.
        Assert.That(folder1.Node.FindParent<MockModel1>("Container", recurse: true), Is.EqualTo(container));
        Assert.That(folder1.Node.FindParent<Model>("Container", recurse: true), Is.EqualTo(container));
        Assert.That(folder1.Node.FindParent<IModel>("Test", recurse: true), Is.EqualTo(simpleModel));

        // When multiple ancestors match, ensure closest is returned.
        Assert.That(folder5.Node.FindParent<Folder>("folder1", recurse: true), Is.EqualTo(folder4));

        Assert.That(folder5.Node.FindParent<MockModel1>("Container", recurse: true), Is.EqualTo(container));
        Assert.That(folder4.Node.FindParent<MockModel1>("Container", recurse: true), Is.EqualTo(container));

        // Test case-insensitive search.
        Assert.That(noSiblings.Node.FindParent<Folder>("FoLdEr3", recurse: true), Is.EqualTo(folder3));
    }

    /// <summary>
    /// Tests the FindDescendant method.
    /// </summary>
    [Test]
    public void TestFindDescendantByTypeAndName()
    {
        // No matches - expect null.
        Assert.That(simpleModel.Node.FindChild<MockModel2>("", recurse: true), Is.Null);
        Assert.That(simpleModel.Node.FindChild<MockModel2>("Container", recurse: true), Is.Null);
        Assert.That(simpleModel.Node.FindChild<MockModel2>(null, recurse: true), Is.Null);

        // No children - expect null.
        Assert.That(noSiblings.Node.FindChild<IModel>("", recurse: true), Is.Null);
        Assert.That(noSiblings.Node.FindChild<IModel>(null, recurse: true), Is.Null);

        // Descendant exists with correct type but incorrect name.
        Assert.That(container.Node.FindChild<Folder>("", recurse: true), Is.Null);

        // Descendant exists with correct name but incorrect type.
        Assert.That(container.Node.FindChild<MockModel2>("folder1", recurse: true), Is.Null);

        // Descendant exists with correct type but incorrect name.
        // Another descendant exists with correct name but incorrect type.
        Assert.That(simpleModel.Node.FindChild<MockModel1>("folder2", recurse: true), Is.Null);

        // 1 match.
        Assert.That(simpleModel.Node.FindChild<MockModel1>("Container", recurse: true), Is.EqualTo(container));
        Assert.That(simpleModel.Node.FindChild<Folder>("folder2", recurse: true), Is.EqualTo(folder2));

        // Many matches - expect first in depth-first search is returned.
        var folder4 = new Folder() { Name = "folder1", Parent = folder1 };
        var folder5 = new Folder() { Name = "folder1", Parent = folder4 };
        folder1.Node.AddChild(folder4);
        folder4.Node.AddChild(folder5);

        Assert.That(simpleModel.Node.FindChild<Folder>("folder1", recurse: true), Is.EqualTo(folder1));
        Assert.That(folder1.Node.FindChild<Folder>("folder1", recurse: true), Is.EqualTo(folder4));
        Assert.That(folder4.Node.FindChild<Folder>("folder1", recurse: true), Is.EqualTo(folder5));

        // Test case-insensitive search.
        Assert.That(simpleModel.Node.FindChild<IModel>("fOLDer2", recurse: true), Is.EqualTo(folder2));
    }

    /// <summary>
    /// Tests the FindSibling method.
    /// </summary>
    [Test]
    public void TestFindSiblingByTypeAndName()
    {
        // No parent - expect null.
        Assert.That(simpleModel.Node.FindSibling<IModel>(""), Is.Null);
        Assert.That(simpleModel.Node.FindSibling<IModel>(null), Is.Null);

        // No siblings - expect null.
        Assert.That(noSiblings.Node.FindSibling<IModel>(""), Is.Null);

        // A model is not its own sibling.
        Assert.That(folder1.Node.FindSibling<Folder>("folder1"), Is.Null);

        // Sibling exists with correct name but incorrect type.
        Assert.That(folder1.Node.FindSibling<MockModel2>("folder2"), Is.Null);

        // Sibling exists with correct type but incorrect name.
        Assert.That(folder1.Node.FindSibling<Folder>(""), Is.Null);

        // 1 sibling of correct type and name.
        Assert.That(folder1.Node.FindSibling<Folder>("folder2"), Is.EqualTo(folder2));

        // Many siblings of correct type and name - expect first sibling which matches.
        var folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
        container.Node.AddChild(folder4);
        Assert.That(folder2.Node.FindSibling<Folder>("folder1"), Is.EqualTo(folder1));
        Assert.That(folder1.Node.FindSibling<Folder>("folder1"), Is.EqualTo(folder4));

        // Test case-insensitive search.
        Assert.That(folder1.Node.FindSibling<Folder>("fOlDeR2"), Is.EqualTo(folder2));
    }

    /// <summary>
    /// Tests the FindChild method.
    /// </summary>
    [Test]
    public void TestFindChildByTypeAndName()
    {
        // No children - expect null.
        Assert.That(folder1.Node.FindChild<IModel>(""), Is.Null);
        Assert.That(folder2.Node.FindChild<IModel>(null), Is.Null);
        Assert.That(noSiblings.Node.FindChild<IModel>(".+"), Is.Null);

        // A model is not its own child.
        Assert.That(folder1.Node.FindChild<Folder>("folder1"), Is.Null);

        // Child exists with correct name but incorrect type.
        Assert.That(container.Node.FindChild<MockModel2>("folder2"), Is.Null);
        Assert.That(simpleModel.Node.FindChild<IModel>("folder2"), Is.Null);

        // Child exists with correct type but incorrect name.
        Assert.That(container.Node.FindChild<Folder>("*"), Is.Null);
        Assert.That(folder3.Node.FindChild<Model>(""), Is.Null);

        // 1 child of correct type and name.
        Assert.That(container.Node.FindChild<Folder>("folder2"), Is.EqualTo(folder2));
        Assert.That(simpleModel.Node.FindChild<MockModel1>("Container"), Is.EqualTo(container));

        // Many children of correct type and name - expect first sibling which matches.
        IModel folder4 = new Folder() { Name = "folder1", Parent = folder1.Parent };
        container.Children.Add(folder4);
        Assert.That(container.Node.FindChild<Folder>("folder1"), Is.EqualTo(folder1));
        Assert.That(container.Node.FindChild<IModel>("folder1"), Is.EqualTo(folder1));

        // Test case-insensitive search.
        Assert.That(container.Node.FindChild<Folder>("fOlDeR2"), Is.EqualTo(folder2));
    }

    /// <summary>
    /// Tests the scope find method.
    /// </summary>
    [Test]
    public void TestFindInScopeByTypeAndName()
    {
        IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

        // This will throw because there is no scoped parent model.
        // Can't uncomment this until we refactor the scoping code.
        //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

        // No matches - expect null.
        Assert.That(leaf1.Node.Find<MockModel2>("x"), Is.Null);
        Assert.That(leaf1.Node.Find<MockModel2>(null), Is.Null);

        // Model exists in scope with correct name but incorrect type.
        Assert.That(leaf1.Node.Find<MockModel2>("Plant"), Is.Null);

        // Model exists in scope with correct type but incorrect name.
        Assert.That(leaf1.Node.Find<Zone>("*"), Is.Null);

        // 1 match.
        Assert.That(leaf1.Node.Find<Zone>("zone1"), Is.EqualTo(scopedSimulation.Children[2]));

        // Many matches - expect first.
        IModel plant1 = scopedSimulation.Children[2].Children[1];
        IModel plant2 = scopedSimulation.Children[2].Children[2];
        IModel managerFolder = scopedSimulation.Children[2].Children[3];
        Assert.That(managerFolder.Node.Find<Plant>("plant1"), Is.EqualTo(plant1));

        managerFolder.Node.Rename("asdf");
        scopedSimulation.Children[0].Node.Rename("asdf");
        scopedSimulation.Node.Rename("asdf");
        Assert.That(leaf1.Node.Find<IModel>("asdf"), Is.EqualTo(managerFolder));
        Assert.That(plant1.Node.Find<Simulation>("asdf"), Is.EqualTo(scopedSimulation));
        Assert.That(scopedSimulation.Children[1].Node.Find<IModel>("asdf"), Is.EqualTo(scopedSimulation));
        Assert.That(scopedSimulation.Children[0].Node.Find<Clock>("asdf"), Is.EqualTo(scopedSimulation.Children[0]));
        Assert.That(scopedSimulation.Children[0].Node.Find<IModel>("asdf"), Is.EqualTo(scopedSimulation));
    }

    /// <summary>
    /// Tests the FindAllAncestors() method.
    /// </summary>
    [Test]
    public void TestFindAllAncestors()
    {
        // The top-level model has no ancestors.
        Assert.That(simpleModel.Node.FindParents<IModel>().ToArray(), Is.EqualTo(new IModel[0]));

        Assert.That(container.Node.FindParents<IModel>().ToArray(), Is.EqualTo(new[] { simpleModel }));

        // Ancestors should be in bottom-up order.
        Assert.That(noSiblings.Node.FindParents<IModel>().ToArray(), Is.EqualTo(new[] { folder3, simpleModel }));

        // Note this test may break if we implement caching for this function.
        container.Node.Parent.RemoveChild(container as INodeModel);
        Assert.That(folder1.Node.FindParents<IModel>(), Is.EqualTo(new[] { container }));

        // This will create infinite recursion. However this should not
        // cause an error in and of itself, due to the lazy implementation.
        container.Parent = folder1;
        Assert.DoesNotThrow(() => folder1.Node.FindParents<IModel>());
    }

    /// <summary>
    /// Tests the FindAllDescendants method.
    /// </summary>
    [Test]
    public void TestFindAllDescendants()
    {
        // No children - expect empty enumerable (not null).
        Assert.That(noSiblings.Node.FindChildren<IModel>(recurse: true).Count(), Is.EqualTo(0));

        // Descendants should be in depth-first search order.
        Assert.That(simpleModel.Node.FindChildren<IModel>(recurse: true).ToArray(), Is.EqualTo(new[] { container, folder1, folder2, folder3, noSiblings }));
        Assert.That(container.Node.FindChildren<IModel>(recurse: true).ToArray(), Is.EqualTo(new[] { folder1, folder2 }));
        Assert.That(folder3.Node.FindChildren<IModel>(recurse: true).ToArray(), Is.EqualTo(new[] { noSiblings }));
    }

    /// <summary>
    /// Tests the FindAllSiblings method.
    /// </summary>
    [Test]
    public void TestFindAllSiblings()
    {
        // No parent - expect empty enumerable (not null).
        Assert.That(simpleModel.Node.FindSiblings<IModel>().Count(), Is.EqualTo(0));

        // No siblings - expect empty enumerable (not null).
        Assert.That(noSiblings.Node.FindSiblings<IModel>().Count(), Is.EqualTo(0));

        // 1 sibling.
        Assert.That(folder1.Node.FindSiblings<IModel>().ToArray(), Is.EqualTo(new[] { folder2 }));

        // Many siblings.
        var folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
        var test = new MockModel() { Name = "test", Parent = folder1.Parent };
        folder1.Parent.Node.AddChild(folder4);
        folder1.Parent.Node.AddChild(test);
        Assert.That(folder1.Node.FindSiblings<IModel>().ToArray(), Is.EqualTo(new[] { folder2, folder4, test }));
    }

    /// <summary>
    /// Tests the FindChildren method.
    /// </summary>
    [Test]
    public void TestFindAllChildren()
    {
        // No children - expect empty enumerable (not null).
        Assert.That(folder1.Node.FindChildren<IModel>().Count(), Is.EqualTo(0));

        // 1 child.
        Assert.That(folder3.Node.FindChildren<IModel>().ToArray(), Is.EqualTo(new[] { noSiblings }));

        // Many children.
        var folder4 = new Folder() { Name = "folder4", Parent = container };
        var test = new MockModel() { Name = "test", Parent = container };
        container.Node.AddChild(folder4);
        container.Node.AddChild(test);
        Assert.That(container.Node.FindChildren<IModel>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4, test }));
    }

    /// <summary>
    /// Tests the FindAll method.
    /// </summary>
    [Test]
    public void TestFindAllInScope()
    {
        // Find all from the top-level model should work.
        Assert.That(scopedSimulation.Node.FindAll<IModel>().Count(), Is.EqualTo(14));

        // Find all should fail if the top-level is not scoped.
        // Can't enable this check until some refactoring of scoping code.
        //Assert.Throws<Exception>(() => simpleModel.FindAll().Count());

        // Ensure correct scoping from leaf1 (remember Plant is a scoping unit)
        // Note that the manager is not in scope. This is not desirable behaviour.
        var leaf1 = scopedSimulation.Children[2].Children[1].Children[0];
        List<IModel> inScopeOfLeaf1 = leaf1.Node.FindAll<IModel>().ToList();
        Assert.That(inScopeOfLeaf1.Count, Is.EqualTo(11));
        Assert.That(inScopeOfLeaf1[0].Name, Is.EqualTo("plant1"));
        Assert.That(inScopeOfLeaf1[1].Name, Is.EqualTo("leaf1"));
        Assert.That(inScopeOfLeaf1[2].Name, Is.EqualTo("stem1"));
        Assert.That(inScopeOfLeaf1[3].Name, Is.EqualTo("zone1"));
        Assert.That(inScopeOfLeaf1[4].Name, Is.EqualTo("Soil"));
        Assert.That(inScopeOfLeaf1[5].Name, Is.EqualTo("plant2"));
        Assert.That(inScopeOfLeaf1[6].Name, Is.EqualTo("managerfolder"));
        Assert.That(inScopeOfLeaf1[7].Name, Is.EqualTo("Simulation"));
        Assert.That(inScopeOfLeaf1[8].Name, Is.EqualTo("Clock"));
        Assert.That(inScopeOfLeaf1[9].Name, Is.EqualTo("MockSummary"));
        Assert.That(inScopeOfLeaf1[10].Name, Is.EqualTo("zone2"));

        // Ensure correct scoping from soil
        var soil = scopedSimulation.Children[2].Children[0];
        List<IModel> inScopeOfSoil = soil.Node.FindAll<IModel>().ToList();
        Assert.That(inScopeOfSoil.Count, Is.EqualTo(14));
        Assert.That(inScopeOfSoil[0].Name, Is.EqualTo("zone1"));
        Assert.That(inScopeOfSoil[1].Name, Is.EqualTo("Soil"));
        Assert.That(inScopeOfSoil[2].Name, Is.EqualTo("plant1"));
        Assert.That(inScopeOfSoil[3].Name, Is.EqualTo("leaf1"));
        Assert.That(inScopeOfSoil[4].Name, Is.EqualTo("stem1"));
        Assert.That(inScopeOfSoil[5].Name, Is.EqualTo("plant2"));
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
    /// Tests the FindAllAncestors method.
    /// </summary>
    [Test]
    public void TestFindAllByTypeAncestors()
    {
        // No parent - expect empty enumerable (not null).
        Assert.That(simpleModel.Node.FindParents<IModel>().Count(), Is.EqualTo(0));

        // Container has no MockModel2 ancestors.
        Assert.That(container.Node.FindParents<MockModel2>().Count(), Is.EqualTo(0));

        // Container's only ancestor is simpleModel.
        Assert.That(container.Node.FindParents<IModel>().ToArray(), Is.EqualTo(new[] { simpleModel }));
        Assert.That(container.Node.FindParents<Model>().ToArray(), Is.EqualTo(new[] { simpleModel }));

        var folder4 = new Folder() { Name = "folder4", Parent = folder3 };
        folder3.Node.AddChild(folder4);
        var folder5 = new Folder() { Name = "folder5", Parent = folder4 };
        folder4.Node.AddChild(folder5);

        Assert.That(folder5.Node.FindParents<MockModel2>().Count(), Is.EqualTo(0));
        Assert.That(folder5.Node.FindParents<Folder>().ToArray(), Is.EqualTo(new[] { folder4, folder3 }));
        Assert.That(folder5.Node.FindParents<IModel>().ToArray(), Is.EqualTo(new[] { folder4, folder3, simpleModel }));
        Assert.That(folder5.Node.FindParents<Model>().ToArray(), Is.EqualTo(new[] { folder4, folder3, simpleModel }));
    }

    /// <summary>
    /// Tests the FindAllDescendants method.
    /// </summary>
    [Test]
    public void TestFindAllByTypeDescendants()
    {
        // No children - expect empty enumerable (not null).
        Assert.That(noSiblings.Node.FindChildren<IModel>(recurse: true).Count(), Is.EqualTo(0));

        // No matches - expect empty enumerable (not null).
        Assert.That(simpleModel.Node.FindChildren<MockModel2>(recurse: true).Count(), Is.EqualTo(0));

        // 1 match.
        Assert.That(simpleModel.Node.FindChildren<MockModel1>(recurse: true).ToArray(), Is.EqualTo(new[] { simpleModel.Children[0] }));

        // Many matches - expect depth-first search.
        Assert.That(simpleModel.Node.FindChildren<Folder>(recurse: true).ToArray(), Is.EqualTo(new[] { folder1, folder2, folder3 }));
        Assert.That(simpleModel.Node.FindChildren<IModel>(recurse: true).ToArray(), Is.EqualTo(new[] { container, folder1, folder2, folder3, noSiblings }));
    }

    /// <summary>
    /// Tests for the FindAllSiblings method.
    /// </summary>
    [Test]
    public void TestFindAllByTypeSiblings()
    {
        // No parent - expect empty enumerable (not null).
        Assert.That(simpleModel.Node.FindSiblings<IModel>().Count(), Is.EqualTo(0));

        // No siblings - expect empty enumerable (not null).
        Assert.That(noSiblings.Node.FindSiblings<IModel>().Count(), Is.EqualTo(0));

        // No siblings of correct type - expect empty enumerable (not null).
        Assert.That(folder1.Node.FindSiblings<MockModel2>().Count(), Is.EqualTo(0));

        // 1 sibling of correct type.
        Assert.That(folder1.Node.FindSiblings<Folder>().ToArray(), Is.EqualTo(new[] { folder2 }));

        // Many siblings of correct type - expect first sibling which matches.
        var folder4 = new Folder() { Name = "folder4", Parent = folder1.Parent };
        var test = new MockModel() { Name = "test", Parent = folder1.Parent };
        folder1.Parent.Node.AddChild(folder4);
        folder1.Parent.Node.AddChild(test);
        Assert.That(folder1.Node.FindSiblings<Folder>().ToArray(), Is.EqualTo(new[] { folder2, folder4 }));
        Assert.That(folder1.Node.FindSiblings<IModel>().ToArray(), Is.EqualTo(new[] { folder2, folder4, test }));
    }

    /// <summary>
    /// Tests for the FindAllChildren method.
    /// </summary>
    [Test]
    public void TestFindAllByTypeChildren()
    {
        // No children - expect empty enumerable (not null).
        Assert.That(folder2.Node.FindChildren<IModel>().Count(), Is.EqualTo(0));
        Assert.That(folder2.Node.FindChildren<MockModel1>().Count(), Is.EqualTo(0));
        Assert.That(folder2.Node.FindChildren<MockModel2>().Count(), Is.EqualTo(0));

        // No children of correct type - expect empty enumerable (not null).
        Assert.That(container.Node.FindChildren<MockModel2>().Count(), Is.EqualTo(0));

        // 1 child of correct type.
        Assert.That(simpleModel.Node.FindChildren<Folder>().ToArray(), Is.EqualTo(new[] { folder3 }));

        // Many children of correct type - expect first child which matches.
        var folder4 = new Folder() { Name = "folder4", Parent = container };
        var test = new MockModel() { Name = "test", Parent = container };
        container.Node.AddChild(folder4);
        container.Node.AddChild(test);
        Assert.That(container.Node.FindChildren<Folder>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4 }));
        Assert.That(container.Node.FindChildren<Model>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4, test }));
        Assert.That(container.Node.FindChildren<IModel>().ToArray(), Is.EqualTo(new[] { folder1, folder2, folder4, test }));
        Assert.That(simpleModel.Node.FindChildren<IModel>().ToArray(), Is.EqualTo(new[] { container, folder3 }));
    }

    /// <summary>
    /// Tests for the FindAll method.
    /// </summary>
    [Test]
    public void TestFindAllByTypeInScope()
    {
        IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

        // Test laziness. Unsure if we want to keep this.
        Assert.DoesNotThrow(() => simpleModel.Node.FindAll<IModel>());

        // No matches (there is an ISummary but no Summary) -
        // expect empty enumerable (not null).
        Assert.That(leaf1.Node.FindAll<Summary>().Count(), Is.EqualTo(0));

        // 1 match.
        Assert.That(leaf1.Node.FindAll<Clock>().ToArray(), Is.EqualTo(new[] { scopedSimulation.Children[0] }));

        // Many matches - test order.
        IModel plant1 = scopedSimulation.Children[2].Children[1];
        IModel plant2 = scopedSimulation.Children[2].Children[2];
        IModel managerFolder = scopedSimulation.Children[2].Children[3];
        IModel[] allPlants = new[] { plant1, plant2 };
        Assert.That(managerFolder.Node.FindAll<Plant>().ToArray(), Is.EqualTo(allPlants));
        Assert.That(plant1.Node.FindAll<Plant>().ToArray(), Is.EqualTo(allPlants));
    }

    /// <summary>
    /// Tests for the FindAllAncestors(string) method.
    /// </summary>
    [Test]
    public void TestFindAllByNameAncestors()
    {
        // No parent - expect empty enumerable (not null).
        Assert.That(simpleModel.Node.FindParents<IModel>("Test").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindParents<IModel>("").Count(), Is.EqualTo(0));

        // No ancestors of correct name - expect empty enumerable.
        Assert.That(container.Node.FindParents<IModel>("asdf").Count(), Is.EqualTo(0));
        Assert.That(container.Node.FindParents<IModel>("").Count(), Is.EqualTo(0));

        // 1 ancestor of correct name.
        Assert.That(container.Node.FindParents<IModel>("Test").ToArray(), Is.EqualTo(new[] { simpleModel }));

        // Multiple ancestors with correct name - expect bottom-up search.
        var folder4 = new Folder() { Name = "folder3", Parent = folder3 };
        folder3.Node.AddChild(folder4);
        var folder5 = new Folder() { Name = "folder3", Parent = folder4 };
        folder4.Node.AddChild(folder5);

        Assert.That(folder5.Node.FindParents<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(folder5.Node.FindParents<IModel>("folder3").ToArray(), Is.EqualTo(new[] { folder4, folder3 }));
        Assert.That(folder5.Node.FindParents<IModel>("Test").ToArray(), Is.EqualTo(new[] { simpleModel }));
        Assert.That(folder4.Node.FindParents<IModel>("folder3").ToArray(), Is.EqualTo(new[] { folder3 }));
    }

    /// <summary>
    /// Tests for the FindAllDescendants method.
    /// </summary>
    [Test]
    public void TestFindAllByNameDescendants()
    {
        // No children - expect empty enumerable.
        Assert.That(noSiblings.Node.FindChildren<IModel>("x", recurse: true).Count(), Is.EqualTo(0));

        // No descendants with correct name - expect empty enumerable.
        Assert.That(simpleModel.Node.FindChildren<IModel>("x", recurse: true).Count(), Is.EqualTo(0));

        // 1 descendant with correct name.
        Assert.That(simpleModel.Node.FindChildren<IModel>("Container", recurse: true).ToArray(), Is.EqualTo(new[] { container }));

        // Many descendants with correct name - expect results in depth-first order.
        var folder4 = new MockModel2() { Parent = container, Name = "folder1" };
        container.Node.AddChild(folder4);
        var folder5 = new MockModel() { Parent = folder1, Name = "folder1" };
        folder1.Node.AddChild(folder5);

        Assert.That(simpleModel.Node.FindChildren<IModel>("folder1", recurse: true).ToArray(), Is.EqualTo(new[] { folder1, folder5, folder4 }));
    }

    /// <summary>
    /// Tests for the FindAllSiblings method.
    /// </summary>
    [Test]
    public void TestFindAllByNameSiblings()
    {
        // No parent - expect empty enumerable.
        Assert.That(simpleModel.Node.FindSiblings<IModel>("Test").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindSiblings<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindSiblings<IModel>(null).Count(), Is.EqualTo(0));

        // No siblings - expect empty enumerable.
        Assert.That(noSiblings.Node.FindSiblings<IModel>("anything").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindSiblings<IModel>("nosiblings").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindSiblings<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindSiblings<IModel>(null).Count(), Is.EqualTo(0));

        // No siblings of correct name - expect empty enumerable.
        Assert.That(folder1.Node.FindSiblings<IModel>("x").Count(), Is.EqualTo(0));
        Assert.That(folder1.Node.FindSiblings<IModel>("folder1").Count(), Is.EqualTo(0));
        Assert.That(folder1.Node.FindSiblings<IModel>("").Count(), Is.EqualTo(0));

        // 1 sibling of correct name.
        Assert.That(folder1.Node.FindSiblings<IModel>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));

        // Many siblings of correct name, expect them in indexed order.
        var folder4 = new Folder() { Name = "folder2", Parent = container };
        folder1.Parent.Node.AddChild(folder4);
        Assert.That(folder1.Node.FindSiblings<IModel>("folder2").ToArray(), Is.EqualTo(new[] { folder2, folder4 }));
    }

    /// <summary>
    /// Tests for the FindAllChildren method.
    /// </summary>
    [Test]
    public void TestFindAllByNameChildren()
    {
        // No children - expect empty enumerable.
        Assert.That(folder1.Node.FindChildren<IModel>("Test").Count(), Is.EqualTo(0));
        Assert.That(folder2.Node.FindChildren<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindChildren<IModel>(null).Count(), Is.EqualTo(0));

        // No children of correct name - expect empty enumerable.
        Assert.That(container.Node.FindChildren<IModel>("x").Count(), Is.EqualTo(0));
        Assert.That(container.Node.FindChildren<IModel>("Test").Count(), Is.EqualTo(0));
        Assert.That(folder3.Node.FindChildren<IModel>("folder3").Count(), Is.EqualTo(0));

        // 1 child of correct name.
        Assert.That(container.Node.FindChildren<IModel>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));
        Assert.That(simpleModel.Node.FindChildren<IModel>("Container").ToArray(), Is.EqualTo(new[] { container }));

        // Many (but not all) children of correct name, expect them in indexed order.
        var folder4 = new Folder() { Name = "folder2", Parent = container };
        container.Node.AddChild(folder4);
        Assert.That(container.Node.FindChildren<IModel>("folder2").ToArray(), Is.EqualTo(new[] { folder2, folder4 }));

        // All (>1) children have correct name.
        container.Node.RemoveChild(folder1 as INodeModel);
        Assert.That(container.Node.FindChildren<IModel>("folder2").ToArray(), Is.EqualTo(new[] { folder2, folder4 }));
    }

    /// <summary>
    /// Tests the FindAll method.
    /// </summary>
    [Test]
    public void TestFindAllByNameInScope()
    {
        IModel leaf1 = scopedSimulation.Children[2].Children[1].Children[0];

        // This will throw because there is no scoped parent model.
        // Can't uncomment this until we refactor the scoping code.
        //Assert.Throws<Exception>(() => simpleModel.Find<IModel>());

        // No matches - expect empty enumerable.
        Assert.That(leaf1.Node.FindAll<object>("x").Count(), Is.EqualTo(0));
        Assert.That(leaf1.Node.FindAll<object>("").Count(), Is.EqualTo(0));

        // 1 match.
        Assert.That(leaf1.Node.FindAll<object>("zone1").ToArray(), Is.EqualTo(new[] { scopedSimulation.Children[2] }));

        // Many matches - expect first.
        IModel plant1 = scopedSimulation.Children[2].Children[1];
        IModel plant2 = scopedSimulation.Children[2].Children[2];
        IModel managerFolder = scopedSimulation.Children[2].Children[3];
        IModel clock = scopedSimulation.Children[0];
        IModel summary = scopedSimulation.Children[1];
        Assert.That(managerFolder.Node.FindAll<object>("plant1").ToArray(), Is.EqualTo(new[] { plant1 }));

        managerFolder.Node.Rename("asdf");
        clock.Node.Rename("asdf");
        scopedSimulation.Node.Rename("asdf");
        Assert.That(leaf1.Node.FindAll<object>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
        Assert.That(plant1.Node.FindAll<object>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
        Assert.That(summary.Node.FindAll<object>("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation, clock, managerFolder }));
        Assert.That(clock.Node.FindAll<object>("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation, clock, managerFolder }));
    }

    /// <summary>
    /// Test the FindAllAncestors{T}(string) method.
    /// </summary>
    [Test]
    public void TestFindAncestorsByTypeAndName()
    {
        var folder4 = new Folder() { Name = "folder1", Parent = folder1 };
        var folder5 = new Folder() { Name = "folder1", Parent = folder4 };
        folder1.Node.AddChild(folder4);
        folder4.Node.AddChild(folder5);

        // No parent - expect empty enumerable.
        Assert.That(simpleModel.Node.FindParents<Model>("Test").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindParents<IModel>("Test").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindParents<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindParents<IModel>(null).Count(), Is.EqualTo(0));

        // A model is not its own ancestor.
        Assert.That(container.Node.FindParents<MockModel1>(null).Count(), Is.EqualTo(0));
        Assert.That(container.Node.FindParents<MockModel1>("").Count(), Is.EqualTo(0));
        Assert.That(container.Node.FindParents<MockModel1>("Container").Count(), Is.EqualTo(0));
        Assert.That(container.Node.FindParents<IModel>("Container").Count(), Is.EqualTo(0));

        // Ancestor exists with correct type but incorrect name.
        Assert.That(folder1.Node.FindParents<MockModel1>("").Count(), Is.EqualTo(0));

        // Ancestor exists with correct name but incorrect type.
        Assert.That(folder1.Node.FindParents<MockModel2>("Container").Count(), Is.EqualTo(0));
        Assert.That(folder1.Node.FindParents<Fertiliser>("Test").Count(), Is.EqualTo(0));

        // Ancestor exists with correct type but incorrect name.
        // Another ancestor exists with correct name but incorrect type.
        Assert.That(folder1.Node.FindParents<MockModel1>("Test").Count(), Is.EqualTo(0));

        // 1 match.
        Assert.That(folder1.Node.FindParents<MockModel1>("Container").ToArray(), Is.EqualTo(new[] { container }));
        Assert.That(folder2.Node.FindParents<Model>("Container").ToArray(), Is.EqualTo(new[] { container }));
        Assert.That(noSiblings.Node.FindParents<IModel>("Test").ToArray(), Is.EqualTo(new[] { simpleModel }));
        Assert.That(noSiblings.Node.FindParents<IModel>("folder3").ToArray(), Is.EqualTo(new[] { folder3 }));

        // Multiple matches - ensure ordering is bottom-up.
        Assert.That(folder5.Node.FindParents<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4, folder1 }));

        // An uncle/cousin is not an ancestor.
        folder2.Node.Rename("folder1");
        Assert.That(folder5.Node.FindParents<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4, folder1 }));

        // Test case-insensitive search.
        Assert.That(noSiblings.Node.FindParents<Folder>("FoLdEr3").ToArray(), Is.EqualTo(new[] { folder3 }));
    }

    /// <summary>
    /// Tests the FindAllDescendants method.
    /// </summary>
    [Test]
    public void TestFindDescendantsByTypeAndName()
    {
        // No matches - expect empty enumerable.
        Assert.That(simpleModel.Node.FindChildren<MockModel2>("", recurse: true).Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindChildren<MockModel2>("Container", recurse: true).Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindChildren<MockModel2>(null, recurse: true).Count(), Is.EqualTo(0));

        // No children - expect enumerable.
        Assert.That(noSiblings.Node.FindChildren<IModel>("", recurse: true).Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindChildren<IModel>(null, recurse: true).Count(), Is.EqualTo(0));

        // Descendants exist with correct type but incorrect name.
        Assert.That(container.Node.FindChildren<Folder>("", recurse: true).Count(), Is.EqualTo(0));
        Assert.That(folder3.Node.FindChildren<IModel>("x", recurse: true).Count(), Is.EqualTo(0));

        // Descendants exist with correct name but incorrect type.
        Assert.That(container.Node.FindChildren<MockModel2>("folder1", recurse: true).Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindChildren<Irrigation>("nosiblings", recurse: true).Count(), Is.EqualTo(0));

        // Descendant exists with correct type but incorrect name.
        // Another descendant exists with correct name but incorrect type.
        Assert.That(simpleModel.Node.FindChildren<MockModel1>("folder2", recurse: true).Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindChildren<Folder>("nosiblings", recurse: true).Count(), Is.EqualTo(0));

        // 1 match.
        Assert.That(simpleModel.Node.FindChildren<MockModel1>("Container", recurse: true).ToArray(), Is.EqualTo(new[] { container }));
        Assert.That(container.Node.FindChildren<IModel>("folder2", recurse: true).ToArray(), Is.EqualTo(new[] { folder2 }));
        Assert.That(folder3.Node.FindChildren<Model>("nosiblings", recurse: true).ToArray(), Is.EqualTo(new[] { noSiblings }));

        // Many matches - expect first in depth-first search is returned.
        var folder4 = new Folder() { Name = "folder1", Parent = folder1 };
        var folder5 = new Folder() { Name = "folder1", Parent = folder4 };
        var folder6 = new Folder() { Name = "folder1", Parent = container };
        folder1.Node.AddChild(folder4);
        folder4.Node.AddChild(folder5);
        container.Node.AddChild(folder6);
        folder3.Node.Rename("folder1");
        noSiblings.Node.Rename("folder1");

        Assert.That(simpleModel.Node.FindChildren<Folder>("folder1", recurse: true).ToArray(), Is.EqualTo(new[] { folder1, folder4, folder5, folder6, folder3 }));
        Assert.That(container.Node.FindChildren<Folder>("folder1", recurse: true).ToArray(), Is.EqualTo(new[] { folder1, folder4, folder5, folder6 }));
        Assert.That(folder1.Node.FindChildren<Folder>("folder1", recurse: true).ToArray(), Is.EqualTo(new[] { folder4, folder5 }));
        Assert.That(folder4.Node.FindChildren<Folder>("folder1", recurse: true).ToArray(), Is.EqualTo(new[] { folder5 }));

        // Test case-insensitive search.
        Assert.That(simpleModel.Node.FindChildren<IModel>("fOLDer2", recurse: true).ToArray(), Is.EqualTo(new[] { folder2 }));
    }

    /// <summary>
    /// Tests the FindAllSiblings method.
    /// </summary>
    [Test]
    public void TestFindSiblingsByTypeAndName()
    {
        // No parent - expect empty enumerable.
        Assert.That(simpleModel.Node.FindSiblings<IModel>("Test").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindSiblings<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindSiblings<IModel>(null).Count(), Is.EqualTo(0));

        // No siblings - expect empty enumerable.
        Assert.That(noSiblings.Node.FindSiblings<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindSiblings<IModel>("nosiblings").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindSiblings<IModel>(null).Count(), Is.EqualTo(0));

        // A model is not its own sibling.
        Assert.That(folder1.Node.FindSiblings<Folder>("folder1").Count(), Is.EqualTo(0));
        Assert.That(folder1.Node.FindSiblings<IModel>("folder1").Count(), Is.EqualTo(0));

        // Siblings exist with correct name but incorrect type.
        Assert.That(folder1.Node.FindSiblings<MockModel2>("folder2").Count(), Is.EqualTo(0));
        Assert.That(folder2.Node.FindSiblings<Fertiliser>("Container").Count(), Is.EqualTo(0));

        // Siblings exist with correct type but incorrect name.
        Assert.That(folder1.Node.FindSiblings<Folder>("").Count(), Is.EqualTo(0));
        Assert.That(container.Node.FindSiblings<Folder>("folder1").Count(), Is.EqualTo(0));

        // 1 sibling of correct type and name.
        Assert.That(folder1.Node.FindSiblings<Folder>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));

        // Many siblings of correct type and name - expect indexed order.
        var folder4 = new Folder() { Name = "folder1", Parent = container };
        container.Node.AddChild(folder4);
        Assert.That(folder2.Node.FindSiblings<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder4 }));
        Assert.That(folder1.Node.FindSiblings<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder4 }));

        // Test case-insensitive search.
        Assert.That(folder1.Node.FindSiblings<Folder>("fOlDeR2").ToArray(), Is.EqualTo(new[] { folder2 }));
    }

    /// <summary>
    /// Tests the FindAllChildren method.
    /// </summary>
    [Test]
    public void TestFindChildrenByTypeAndName()
    {
        // No children- expect empty enumerable.
        Assert.That(folder1.Node.FindChildren<IModel>("folder1").Count(), Is.EqualTo(0));
        Assert.That(folder1.Node.FindChildren<IModel>("folder2").Count(), Is.EqualTo(0));
        Assert.That(folder2.Node.FindChildren<IModel>("Container").Count(), Is.EqualTo(0));
        Assert.That(folder2.Node.FindChildren<IModel>("").Count(), Is.EqualTo(0));
        Assert.That(noSiblings.Node.FindChildren<IModel>(null).Count(), Is.EqualTo(0));

        // Children exist with correct name but incorrect type.
        Assert.That(container.Node.FindChildren<MockModel2>("folder2").Count(), Is.EqualTo(0));
        Assert.That(folder3.Node.FindChildren<Fertiliser>("nosiblings").Count(), Is.EqualTo(0));

        // Children exist with correct type but incorrect name.
        Assert.That(container.Node.FindChildren<Folder>("folder3").Count(), Is.EqualTo(0));
        Assert.That(folder3.Node.FindChildren<Model>("Test").Count(), Is.EqualTo(0));
        Assert.That(simpleModel.Node.FindChildren<Model>("").Count(), Is.EqualTo(0));

        // 1 child of correct type and name.
        Assert.That(container.Node.FindChildren<Folder>("folder2").ToArray(), Is.EqualTo(new[] { folder2 }));

        // Many siblings of correct type and name - expect indexed order.
        var folder4 = new Folder() { Name = "folder1", Parent = container };
        container.Node.AddChild(folder4);
        Assert.That(container.Node.FindChildren<Folder>("folder1").ToArray(), Is.EqualTo(new[] { folder1, folder4 }));

        // Test case-insensitive search.
        Assert.That(container.Node.FindChildren<Folder>("fOlDeR2").ToArray(), Is.EqualTo(new[] { folder2 }));
    }

    /// <summary>
    /// Tests the FindAll method.
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
        Assert.That(leaf1.Node.FindAll<MockModel2>("x").Count(), Is.EqualTo(0));
        Assert.That(leaf1.Node.FindAll<MockModel2>(null).Count(), Is.EqualTo(0));

        // Model exists in scope with correct name but incorrect type.
        Assert.That(leaf1.Node.FindAll<MockModel2>("plant1").Count(), Is.EqualTo(0));

        // Model exists in scope with correct type but incorrect name.
        Assert.That(leaf1.Node.FindAll<Zone>("*").Count(), Is.EqualTo(0));
        Assert.That(leaf1.Node.FindAll<Zone>("plant1").Count(), Is.EqualTo(0));

        // 1 match.
        Assert.That(leaf1.Node.FindAll<Zone>("zone1").ToArray(), Is.EqualTo(new[] { scopedSimulation.Children[2] }));

        managerFolder.Node.Rename("asdf");
        scopedSimulation.Children[0].Node.Rename("asdf");
        scopedSimulation.Node.Rename("asdf");

        Assert.That(plant1.Node.FindAll<Simulation>("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation }));
        Assert.That(clock.Node.FindAll<Clock>("asdf").ToArray(), Is.EqualTo(new[] { clock }));

        // Many matches - expect first.
        Assert.That(managerFolder.Node.FindAll<Plant>().ToArray(), Is.EqualTo(new[] { plant1, plant2 }));

        Assert.That(leaf1.Node.FindAll<IModel>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
        Assert.That(plant1.Node.FindAll<IModel>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
        Assert.That(summary.Node.FindAll<IModel>("asdf").ToArray(), Is.EqualTo(new[] { scopedSimulation, clock, managerFolder }));
        Assert.That(plant2.Node.FindAll<IModel>("asdf").ToArray(), Is.EqualTo(new[] { managerFolder, scopedSimulation, clock }));
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
    /// Tests for the IsChildAllowable method.
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
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(DropOnSimulations)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(DropOnFolder)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(MockModel1)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(DropAnywhere)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(ConflictingDirectives)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(ConflictedButReversed)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(NoValidParents)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(SomeFunction)), Is.True);
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(SomeFunction)), Is.True);

            // If it's not a model it cannot be added as a child.
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(object)), Is.False);

            // Even if it's also an IFunction.
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(SimsIFunction)), Is.True);

            // Simulations object also cannot be added to anything.
            Assert.That(Apsim.IsChildAllowable(anyChild, typeof(Simulations)), Is.False);
        }

        // Simulations object cannot be added to anything.
        Assert.That(Apsim.IsChildAllowable(container, typeof(Simulations)), Is.False);
        Assert.That(Apsim.IsChildAllowable(folder2, typeof(Simulations)), Is.False);
        Assert.That(Apsim.IsChildAllowable(simpleModel, typeof(Simulations)), Is.False);
        Assert.That(Apsim.IsChildAllowable(new Simulations(), typeof(Simulations)), Is.False);
        Assert.That(Apsim.IsChildAllowable(new SomeFunction(), typeof(Simulations)), Is.False);

        // IFunctions can be added to anything.
        Assert.That(Apsim.IsChildAllowable(simpleModel, typeof(SomeFunction)), Is.True);
        Assert.That(Apsim.IsChildAllowable(new Simulations(), typeof(SomeFunction)), Is.True);
        Assert.That(Apsim.IsChildAllowable(new MockModel(), typeof(SomeFunction)), Is.True);

        // Otherwise, the validity of a child model depends on it specific
        // valid parents, as defined in its valid parent attributes.
        Assert.That(Apsim.IsChildAllowable(new NoValidParents(), typeof(CanAddToNoValidParents)), Is.True);
        Assert.That(Apsim.IsChildAllowable(new NoValidParents(), typeof(DropAnywhere)), Is.True);
        Assert.That(Apsim.IsChildAllowable(new MockModel(), typeof(DropAnywhere)), Is.True);
        Assert.That(Apsim.IsChildAllowable(new CanAddToNoValidParents(), typeof(CanAddToNoValidParents)), Is.False);
        Assert.That(Apsim.IsChildAllowable(new CanAddToNoValidParents(), typeof(DropAnywhere)), Is.True);
        Assert.That(Apsim.IsChildAllowable(new MockModel(), typeof(DropAnywhere)), Is.True);
    }

    /// <summary>
    /// Tests for the Get method.
    /// </summary>
    [Test]
    public void TestFindInPath()
    {
        // 1. Absolute paths.
        Assert.That(simpleModel.Node.Get(".Test.Container"), Is.EqualTo(container));
        Assert.That(simpleModel.Node.Get(".Test"), Is.EqualTo(simpleModel));
        Assert.That(simpleModel.Node.Get(".Test.folder3.nosiblings"), Is.EqualTo(noSiblings));
        Assert.That(simpleModel.Node.Get(".Test.folder3.Children[1]"), Is.EqualTo(noSiblings));
        Assert.That(simpleModel.Node.Get(".Test.folder3.asdf"), Is.Null);
        Assert.That(simpleModel.Node.Get(".asdf"), Is.Null);
        Assert.That(simpleModel.Node.Get(""), Is.Null);
        Assert.That(simpleModel.Node.Get(null), Is.Null);

        // Do not allow type names in absolute path.
        Assert.That(simpleModel.Node.Get(".Model.Folder.Model"), Is.Null);

        // Absolute path with variable.
        Assert.That(simpleModel.Node.Get(".Test.Container.folder2.Name"), Is.EqualTo("folder2"));
        Assert.That(simpleModel.Node.GetObject(".Test.Name").GetType(), Is.EqualTo(typeof(VariableComposite)));

        // 2. Relative paths.
        Assert.That(simpleModel.Node.Get("[Test]"), Is.EqualTo(simpleModel));
        Assert.That(simpleModel.Node.Get("[folder1]"), Is.EqualTo(folder1));
        Assert.That(simpleModel.Node.Get("[folder3].nosiblings"), Is.EqualTo(noSiblings));
        Assert.That(simpleModel.Node.Get("[Test].Container.folder2"), Is.EqualTo(folder2));
        Assert.That(simpleModel.Node.Get("[Test].Children[2]"), Is.EqualTo(folder3));
        Assert.That(simpleModel.Node.Get("[asdf]"), Is.Null);
        Assert.That(simpleModel.Node.Get("[folder3].foo"), Is.Null);

        // Do not allow type names in relative path.
        Assert.That(simpleModel.Node.Get(".Model.Folder"), Is.Null);

        // Relative path with variable.
        Assert.That(simpleModel.Node.Get("[Container].Name"), Is.EqualTo("Container"));
        Assert.That(simpleModel.Node.GetObject("[Container].Name").GetType(), Is.EqualTo(typeof(VariableComposite)));

        // 3. Child paths.
        Assert.That(simpleModel.Node.Get("Container"), Is.EqualTo(container));
        Assert.That(simpleModel.Node.Get("folder3.nosiblings"), Is.EqualTo(noSiblings));
        Assert.That(container.Node.Get("folder2"), Is.EqualTo(folder2));
        Assert.That(simpleModel.Node.Get("folder2"), Is.Null);
        Assert.That(simpleModel.Node.Get("x"), Is.Null);
        Assert.That(simpleModel.Node.Get(""), Is.Null);
        Assert.That(simpleModel.Node.Get(null), Is.Null);
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
        Assert.Throws<Exception>(() => Node.Create(model));
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
        Assert.Throws<Exception>(() => Node.Create(model));
    }
}
