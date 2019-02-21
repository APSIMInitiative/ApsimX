using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace UnitTests
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.EnsureNameWorks.json");
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.EnsureTypeWorks.json");
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.EnsureChildrenWorks.json");
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.EnsureChildrenWorks.json");
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.EnsureChildrenWorks.json");
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.EnsureChildrenRecursivelyWorks.json");
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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.DescendantsByType.json");
            JObject rootNode = JObject.Parse(json);
            List<JObject> descendants = JsonUtilities.ChildrenRecursively(rootNode, "Models.Graph.Axis");
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
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(null, "Models.Graph.Axis"));

            // Ensure descendants of null is an empty list when filtering by type without namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(null, "Axis"));

            // Ensure descendants of a node with an empty children property
            // is an empty list when filtering by type with namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[1], "Models.Graph.Axis"));

            // Ensure descendants of a node with an empty children property
            // is an empty list when filtering by type without namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[1], "Axis"));

            // Ensure descendants of a node with no children property
            // is an empty list when filtering by type with namespace.
            Assert.AreEqual(emptyList, JsonUtilities.ChildrenRecursively(children[2], "Models.Graph.Axis"));

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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.DescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            Assert.IsNull(JsonUtilities.Parent(rootNode));

            List<JObject> descendants = JsonUtilities.ChildrenRecursively(rootNode, "Models.Graph.Axis");
            var graph = JsonUtilities.Parent(descendants[0]);
            Assert.AreEqual(JsonUtilities.Name(graph), "Graph");

        }

        /// <summary>
        /// Ensures the RenameProperty() method works correctly
        /// </summary>
        [Test]
        public void RenamePropertyTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.DescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            List<JObject> axes = JsonUtilities.ChildrenRecursively(rootNode, "Models.Graph.Axis");

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
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.JsonUtilitiesTests.DescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            List<JObject> axes = JsonUtilities.ChildrenRecursively(rootNode, "Models.Graph.Axis");

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
    }
}
