namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using NUnit.Framework;

    /// <summary>This is a test class for the .apsimx file converter.</summary>
    [TestFixture]
    public class XMLToJSONTests
    {
        /// <summary>Ensure the parent / child XML relationship converts OK</summary>
        [Test]
        public void XMLToJSONTests_EnsureParentChildRelationshipConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureParentChildRelationshipConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureParentChildRelationshipConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }

        /// <summary>Ensure arrays of values convert.</summary>
        [Test]
        public void XMLToJSONTests_EnsureArraysConvert()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureArraysConvert.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureArraysConvert.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }

        /// <summary>
        /// If there are multiple models of same type (e.g. manager) then the NewtonSoft
        /// JSON library groups them into an array. Need to handle this.
        /// </summary>
        [Test]
        public void XMLToJSONTests_EnsureArraysOfModelsWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureArraysOfModelsWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureArraysOfModelsWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }

        /// <summary>
        /// Ensure a manager model can be converted.
        /// </summary>
        [Test]
        public void XMLToJSONTests_EnsureManagerWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureManagerWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureManagerWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }

        /// <summary>
        /// Ensure a child not implementing IModel (e.g. Axis) is converted OK.
        /// </summary>
        [Test]
        public void XMLToJSONTests_ChildNotOfTypeModel()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_ChildNotOfTypeModel.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_ChildNotOfTypeModel.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }

        /// <summary>
        /// Ensure a memo model can be converted.
        /// </summary>
        [Test]
        public void XMLToJSONTests_EnsureMemoWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureMemoWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureMemoWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }
        /// <summary>
        /// Ensure a memo model can be converted.
        /// </summary>
        [Test]
        public void XMLToJSONTests_EnsureTestsWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureTestsWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.XMLToJSONTests_EnsureTestsWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.AreEqual(json, expectedJson);
        }

    }
}
