using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Models;
using Models.Core;

namespace UnitTests.Core.ApsimFile
{
    /// <summary>
    /// A collection of tests for the json utilities.
    /// </summary>
    class JsonUtilitiesTests
    {
        /// <summary>
        /// Ensure the Name method works correctly.
        /// </summary>
        [Test]
        public void NameTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureNameWorks.json");
            JObject rootNode = JObject.Parse(json);

            // Ensure name is correct for a model.
            Assert.AreEqual(JsonUtilities.Name(rootNode), "Simulations");

            // Ensure name is correct for a property.
            Assert.AreEqual(JsonUtilities.Name(rootNode.Property("Version")), "Version");

            // Ensure property value has a name of "" (empty string).
            Assert.AreEqual(JsonUtilities.Name(rootNode.Property("Version").Value), string.Empty);

            // Ensure that the name of null is null.
            Assert.Null(JsonUtilities.Name(null));
        }

        /// <summary>
        /// Ensure the Type() method works correctly.
        /// </summary>
        [Test]
        public void TypeTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureTypeWorks.json");
            JObject rootNode = JObject.Parse(json);
            List<JObject> children = JsonUtilities.Children(rootNode);

            // Ensure typename with namespace is correct.
            Assert.AreEqual("Models.Core.Simulations", JsonUtilities.Type(rootNode, true));

            // Ensure typename without namespace is correct.
            Assert.AreEqual("Simulations", JsonUtilities.Type(rootNode));

            // Ensure that typename of null is null.
            Assert.Null(JsonUtilities.Type(null));

            // Ensure that typename of node with no $type property is null.
            Assert.Null(JsonUtilities.Type(children[0]));
        }

        /// <summary>
        /// Ensures the Children() method works correctly.
        /// </summary>
        [Test]
        public void ChildrenTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureChildrenWorks.json");
            JObject rootNode = JObject.Parse(json);
            List<JObject> children = JsonUtilities.Children(rootNode);
            List<JObject> emptyList = new List<JObject>();

            // Ensure children is not null.
            Assert.NotNull(children);

            // Ensure number of children is correct.
            Assert.AreEqual(4, children.Count);

            // Ensure children of null is an empty list.
            Assert.AreEqual(emptyList, JsonUtilities.Children(null));

            // Ensure children of a node with empty children property is empty list.
            Assert.AreEqual(emptyList, JsonUtilities.Children(children[0] as JObject));

            // Ensure children of a node with no children property is an empty list.
            Assert.AreEqual(emptyList, JsonUtilities.Children(children[1] as JObject));
        }

        /// <summary>
        /// Ensures the ChildWithName() method works correctly.
        /// </summary>
        [Test]
        public void ChildWithName()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureChildrenWorks.json");
            JObject rootNode = JObject.Parse(json);

            Assert.NotNull(JsonUtilities.ChildWithName(rootNode, "Clock"));

            Assert.IsNull(JsonUtilities.ChildWithName(rootNode, "XYZ"));
        }

        /// <summary>
        /// Ensures the ChildWithName() method works correctly.
        /// </summary>
        [Test]
        public void ChildOfType()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureChildrenWorks.json");
            JObject rootNode = JObject.Parse(json);

            var clocks = JsonUtilities.ChildrenOfType(rootNode, "Clock");
            Assert.AreEqual(clocks.Count, 3);

            var folders = JsonUtilities.ChildrenOfType(rootNode, "Folder");
            Assert.AreEqual(folders.Count, 1);

            var empty = JsonUtilities.ChildrenOfType(rootNode, "XYZ");
            Assert.AreEqual(empty.Count, 0);
        }

        /// <summary>
        /// Ensures the ChildrenRecursively() method works correctly.
        /// </summary>
        [Test]
        public void ChildrenRecursivelyTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureChildrenRecursivelyWorks.json");
            JObject rootNode = JObject.Parse(json);
            List<JObject> children = JsonUtilities.Children(rootNode);
            List<JObject> descendants = JsonUtilities.ChildrenRecursively(rootNode);
            List<JObject> emptyList = new List<JObject>();

            // Ensure descendants is not null.
            Assert.NotNull(descendants);

            // Ensure number of descendants is correct.
            Assert.AreEqual(6, descendants.Count);

            // Ensure descendants of null is an empty list (not null).
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(null));

            // Ensure descendants of a node with an empty children property is an empty list.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[0] as JObject));

            // Ensure descendants of a node with no children property is an empty list.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[1] as JObject));
        }

        /// <summary>
        /// Ensures the ChildrenRecursively method works correctly when
        /// provided with a type filter.
        /// </summary>
        [Test]
        public void DescendantsByTypeTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsDescendantsByType.json");
            JObject rootNode = JObject.Parse(json);
            List<JObject> descendants = JsonUtilities.ChildrenRecursively(rootNode, "Models.Axis");
            List<JObject> descendatnsWithoutNamespace = JsonUtilities.ChildrenRecursively(rootNode, "Axis");
            List<JObject> children = JsonUtilities.Children(rootNode).Cast<JObject>().ToList();
            List<JObject> emptyList = new List<JObject>();

            // Ensure descendants is not null.
            Assert.NotNull(descendants);

            // Ensure number of descendants of type Models.Core.Axis is correct.
            Assert.AreEqual(2, descendants.Count);

            // Ensure number of descendatns of type Axis is correct.
            Assert.AreEqual(2, descendatnsWithoutNamespace.Count);

            // Ensure descendants of null is an empty list when filtering by type with namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(null, "Models.Axis"));

            // Ensure descendants of null is an empty list when filtering by type without namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(null, "Axis"));

            // Ensure descendants of a node with an empty children property
            // is an empty list when filtering by type with namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[1], "Models.Axis"));

            // Ensure descendants of a node with an empty children property
            // is an empty list when filtering by type without namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[1], "Axis"));

            // Ensure descendants of a node with no children property
            // is an empty list when filtering by type with namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[2], "Models.Axis"));

            // Ensure descendants of a node with no children property
            // is an empty list when filtering by type without namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[2], "Axis"));
        }

        /// <summary>
        /// Ensures the Parent() method works correctly
        /// </summary>
        [Test]
        public void ParentTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsDescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            Assert.IsNull(JsonUtilities.Parent(rootNode));

            List<JObject> descendants = JsonUtilities.ChildrenRecursively(rootNode, "Models.Axis");
            var graph = JsonUtilities.Parent(descendants[0]);
            Assert.AreEqual(JsonUtilities.Name(graph), "Graph");

        }

        /// <summary>
        /// Ensures the RenameProperty() method works correctly
        /// </summary>
        [Test]
        public void RenamePropertyTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsDescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            List<JObject> axes = JsonUtilities.ChildrenRecursively(rootNode, "Models.Axis");

            JsonUtilities.RenameProperty(axes[0], "Title", "Title2");
            Assert.IsNull(axes[0]["Title"]);
            Assert.AreEqual(axes[0]["Title2"].Value<string>(), "Date");
        }

        /// <summary>
        /// Ensures the AddConstantFunctionIfNotExists() method works correctly
        /// </summary>
        [Test]
        public void AddConstantFunctionIfNotExistsTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsDescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            List<JObject> axes = JsonUtilities.ChildrenRecursively(rootNode, "Models.Axis");

            JsonUtilities.AddConstantFunctionIfNotExists(axes[0], "ConstantFunction", "1");

            var constant = JsonUtilities.ChildWithName(axes[0], "ConstantFunction");
            Assert.NotNull(constant);
            Assert.AreEqual(constant["$type"].Value<string>(), "Models.Functions.Constant, Models");
            Assert.AreEqual(constant["FixedValue"].Value<string>(), "1");
        }
        
        /// <summary>
        /// Ensures the Values() method works correctly
        /// </summary>
        [Test]
        public void ValuesTests()
        {
            var originalValues = new string[] { "string1", "string2", "string3" };

            JArray arr = new JArray();
            originalValues.ToList().ForEach(value => arr.Add(value));

            JObject rootNode = new JObject();
            rootNode["A"] = arr;

            List<string> values = JsonUtilities.Values(rootNode, "A");
            Assert.AreEqual(values, originalValues);
        }

        /// <summary>
        /// Ensures the SetValues() method works correctly
        /// </summary>
        [Test]
        public void SetValuesTests()
        {
            var values = new string[] { "string1", "string2", "string3" };
            
            JObject rootNode = new JObject();
            JsonUtilities.SetValues(rootNode, "A", values.ToList());

            JArray arr = rootNode["A"] as JArray;

            Assert.AreEqual(arr.Values<string>(), values);
        }

        /// <summary>
        /// Tests the AddModel functions.
        /// </summary>
        [Test]
        public void AddModelTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTests.AddModelTests.json");
            JObject node = JObject.Parse(json);

            // Clock1 has an empty Children array, whereas clock2 does not have
            // a Children array. This test ensures that this does not matter.
            JObject clock1 = JsonUtilities.ChildWithName(node, "Clock1");
            JObject clock2 = JsonUtilities.ChildWithName(node, "Clock2");
            AddChildren(clock1);
            AddChildren(clock2);

            string clock2Name = clock2["Name"]?.ToString();
            clock2["Name"] = clock1["Name"]; // For comparison purposes.

            Assert.AreEqual(clock1, clock2, "Clock1:" + Environment.NewLine + clock1?.ToString() + Environment.NewLine + "Clock2:" + Environment.NewLine + clock2?.ToString());

            JObject nullJObject = null;
            IModel nullModel = null;
            Type nullType = null;
            string nullString = null;

            // JsonUtilities.AddModel(JObject, IModel)
            Assert.That(() => JsonUtilities.AddModel(nullJObject, new Clock()),                 Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(clock1, nullModel),                        Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(nullJObject, nullModel),                   Throws.InstanceOf<Exception>());

            // JsonUtilities.AddModel(JObject, Type)
            Assert.That(() => JsonUtilities.AddModel(nullJObject, typeof(Clock)),               Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(clock1, nullType),                         Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(nullJObject, nullType),                    Throws.InstanceOf<Exception>());

            // JsonUtilities.AddModel(JObject, Type, string)
            Assert.That(() => JsonUtilities.AddModel(clock1, typeof(Clock), nullString),        Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(clock1, nullType, ""),                     Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(clock1, nullType, nullString),             Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(nullJObject, typeof(Clock), ""),           Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(nullJObject, typeof(Clock), nullString),   Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(nullJObject, nullType, nullString),        Throws.InstanceOf<Exception>());
            Assert.That(() => JsonUtilities.AddModel(nullJObject, nullType, ""),                Throws.InstanceOf<Exception>());
        }

        /// <summary>
        /// Adds children to a node using each overload of the AddModel()
        /// function, and ensures that all children are identical. And are
        /// correctly named and typed.
        /// </summary>
        /// <param name="node"></param>
        private void AddChildren(JObject node)
        {
            Clock clock = new Clock();
            clock.Name = "Clock";

            // First, add a model using type and instance methods, and ensure
            // that the resultant children are identical.
            JsonUtilities.AddModel(node, clock);
            JsonUtilities.AddModel(node, typeof(Clock), "Clock");
            JsonUtilities.AddModel(node, typeof(Clock));

            List<JObject> children = JsonUtilities.Children(node);

            // Node should now have 3 children.
            Assert.NotNull(children);
            Assert.AreEqual(3, children.Count);

            JObject childClock1 = children[0];
            JObject childClock2 = children[1];
            JObject childClock3 = children[2];

            // Ensure that all children are identical.
            Assert.AreEqual(childClock1, childClock2);
            Assert.AreEqual(childClock1, childClock3);

            // Ensure that first child node has correct name and type.
            Assert.AreEqual("Clock", JsonUtilities.Name(childClock1));
            Assert.AreEqual("Clock", JsonUtilities.Type(childClock1, withNamespace: false));
        }
    }
}
