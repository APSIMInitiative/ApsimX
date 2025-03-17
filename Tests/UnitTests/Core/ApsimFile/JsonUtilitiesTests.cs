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

            Assert.That(clock1, Is.EqualTo(clock2), "Clock1:" + Environment.NewLine + clock1?.ToString() + Environment.NewLine + "Clock2:" + Environment.NewLine + clock2?.ToString());

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
            Assert.That(children, Is.Not.Null);
            Assert.That(children.Count, Is.EqualTo(3));

            JObject childClock1 = children[0];
            JObject childClock2 = children[1];
            JObject childClock3 = children[2];

            // Ensure that all children are identical.
            Assert.That(childClock2, Is.EqualTo(childClock1));
            Assert.That(childClock3, Is.EqualTo(childClock1));

            // Ensure that first child node has correct name and type.
            Assert.That(JsonUtilities.Name(childClock1), Is.EqualTo("Clock"));
            Assert.That(JsonUtilities.Type(childClock1, withNamespace: false), Is.EqualTo("Clock"));
        }
    }
}
