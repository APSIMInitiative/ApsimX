namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using NUnit.Framework;
    using System.Data;
    using System.IO;

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
    }
}
