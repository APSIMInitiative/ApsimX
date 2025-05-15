using APSIM.Shared.Utilities;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Models.Core;
using APSIM.Core;

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
            Assert.That(JsonUtilities.Name(rootNode), Is.EqualTo("Simulations"));

            // Ensure name is correct for a property.
            Assert.That(JsonUtilities.Name(rootNode.Property("Version")), Is.EqualTo("Version"));

            // Ensure property value has a name of "" (empty string).
            Assert.That(JsonUtilities.Name(rootNode.Property("Version").Value), Is.EqualTo(string.Empty));

            // Ensure that the name of null is null.
            Assert.That(JsonUtilities.Name(null), Is.Null);
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
            Assert.That(JsonUtilities.Type(rootNode, true), Is.EqualTo("Models.Core.Simulations"));

            // Ensure typename without namespace is correct.
            Assert.That(JsonUtilities.Type(rootNode), Is.EqualTo("Simulations"));

            // Ensure that typename of null is null.
            Assert.That(JsonUtilities.Type(null), Is.Null);

            // Ensure that typename of node with no $type property is null.
            Assert.That(JsonUtilities.Type(children[0]), Is.Null);
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
            Assert.That(children, Is.Not.Null);

            // Ensure number of children is correct.
            Assert.That(children.Count, Is.EqualTo(4));

            // Ensure children of null is an empty list.
            Assert.That(JsonUtilities.Children(null), Is.EqualTo(emptyList));

            // Ensure children of a node with empty children property is empty list.
            Assert.That(JsonUtilities.Children(children[0] as JObject), Is.EqualTo(emptyList));

            // Ensure children of a node with no children property is an empty list.
            Assert.That(JsonUtilities.Children(children[1] as JObject), Is.EqualTo(emptyList));
        }

        /// <summary>
        /// Ensures the ChildWithName() method works correctly.
        /// </summary>
        [Test]
        public void ChildWithName()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsEnsureChildrenWorks.json");
            JObject rootNode = JObject.Parse(json);

            Assert.That(JsonUtilities.ChildWithName(rootNode, "Clock"), Is.Not.Null);

            Assert.That(JsonUtilities.ChildWithName(rootNode, "XYZ"), Is.Null);
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
            Assert.That(clocks.Count, Is.EqualTo(3));

            var folders = JsonUtilities.ChildrenOfType(rootNode, "Folder");
            Assert.That(folders.Count, Is.EqualTo(1));

            var empty = JsonUtilities.ChildrenOfType(rootNode, "XYZ");
            Assert.That(empty.Count, Is.EqualTo(0));
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
            Assert.That(descendants, Is.Not.Null);

            // Ensure number of descendants is correct.
            Assert.That(descendants.Count, Is.EqualTo(6));

            // Ensure descendants of null is an empty list (not null).
            Assert.That(JsonUtilities.ChildrenRecursively(null), Is.EqualTo(emptyList));

            // Ensure descendants of a node with an empty children property is an empty list.
            Assert.That(JsonUtilities.ChildrenRecursively(children[0] as JObject), Is.EqualTo(emptyList));

            // Ensure descendants of a node with no children property is an empty list.
            Assert.That(JsonUtilities.ChildrenRecursively(children[1] as JObject), Is.EqualTo(emptyList));
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
            Assert.That(descendants, Is.Not.Null);

            // Ensure number of descendants of type Models.Core.Axis is correct.
            Assert.That(descendants.Count, Is.EqualTo(2));

            // Ensure number of descendatns of type Axis is correct.
            Assert.That(descendatnsWithoutNamespace.Count, Is.EqualTo(2));

            // Ensure descendants of null is an empty list when filtering by type with namespace.
            Assert.That(JsonUtilities.ChildrenRecursively(null, "Models.Axis"), Is.EqualTo(emptyList));

            // Ensure descendants of null is an empty list when filtering by type without namespace.
            Assert.That(JsonUtilities.ChildrenRecursively(null, "Axis"), Is.EqualTo(emptyList));

            // Ensure descendants of a node with an empty children property
            // is an empty list when filtering by type with namespace.
            Assert.That(JsonUtilities.ChildrenRecursively(children[1], "Models.Axis"), Is.EqualTo(emptyList));

            // Ensure descendants of a node with an empty children property
            // is an empty list when filtering by type without namespace.
            Assert.That(JsonUtilities.ChildrenRecursively(children[1], "Axis"), Is.EqualTo(emptyList));

            // Ensure descendants of a node with no children property
            // is an empty list when filtering by type with namespace.
            Assert.That(JsonUtilities.ChildrenRecursively(children[2], "Models.Axis"), Is.EqualTo(emptyList));

            // Ensure descendants of a node with no children property
            // is an empty list when filtering by type without namespace.
            Assert.That(JsonUtilities.ChildrenRecursively(children[2], "Axis"), Is.EqualTo(emptyList));
        }

        /// <summary>
        /// Ensures the Parent() method works correctly
        /// </summary>
        [Test]
        public void ParentTests()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.JsonUtilitiesTestsDescendantsByType.json");
            JObject rootNode = JObject.Parse(json);

            Assert.That(JsonUtilities.Parent(rootNode), Is.Null);

            List<JObject> descendants = JsonUtilities.ChildrenRecursively(rootNode, "Models.Axis");
            var graph = JsonUtilities.Parent(descendants[0]);
            Assert.That(JsonUtilities.Name(graph), Is.EqualTo("Graph"));

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
            Assert.That(axes[0]["Title"], Is.Null);
            Assert.That(axes[0]["Title2"].Value<string>(), Is.EqualTo("Date"));
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
            Assert.That(constant, Is.Not.Null);
            Assert.That(constant["$type"].Value<string>(), Is.EqualTo("Models.Functions.Constant, Models"));
            Assert.That(constant["FixedValue"].Value<string>(), Is.EqualTo("1"));
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
            Assert.That(values, Is.EqualTo(originalValues));
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

            Assert.That(arr.Values<string>(), Is.EqualTo(values));
        }
    }
}
