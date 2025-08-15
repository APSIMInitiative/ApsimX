namespace APSIM.Core.Tests
{
    using APSIM.Core;
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
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureParentChildRelationshipConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureParentChildRelationshipConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure arrays of values convert.</summary>
        [Test]
        public void XMLToJSONTests_EnsureArraysConvert()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureArraysConvert.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureArraysConvert.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>
        /// If there are multiple models of same type (e.g. manager) then the NewtonSoft
        /// JSON library groups them into an array. Need to handle this.
        /// </summary>
        [Test]
        public void XMLToJSONTests_EnsureArraysOfModelsWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureArraysOfModelsWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureArraysOfModelsWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure a manager model can be converted.</summary>
        [Test]
        public void XMLToJSONTests_EnsureManagerWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureManagerWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureManagerWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure a child not implementing IModel (e.g. Axis) is converted OK.</summary>
        [Test]
        public void XMLToJSONTests_ChildNotOfTypeModel()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_ChildNotOfTypeModel.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_ChildNotOfTypeModel.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure a memo model can be converted.</summary>
        [Test]
        public void XMLToJSONTests_EnsureMemoWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureMemoWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureMemoWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure a memo model can be converted.</summary>
        [Test]
        public void XMLToJSONTests_EnsureTestsWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureTestsWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureTestsWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure a one line model in XML can be converted.</summary>
        [Test]
        public void XMLToJSONTests_EnsureOneLineModelConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureOneLineModelConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureOneLineModelConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure a cultivar is converted.</summary>
        [Test]
        public void XMLToJSONTests_EnsureCultivarConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureCultivarConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureCultivarConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure residuetypes is converted.</summary>
        [Test]
        public void XMLToJSONTests_EnsureResidueTypesConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureResidueTypesConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureResidueTypesConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure Leaf.Leaves isn't written.</summary>
        [Test]
        public void XMLToJSONTests_EnsureLeavesIsntWritten()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureLeavesIsntWritten.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureLeavesIsntWritten.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure List of List of strings.</summary>
        [Test]
        public void XMLToJSONTests_EnsureListListStringWorks()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureListListStringWorks.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureListListStringWorks.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Some models have an xsi:type attribute.</summary>
        [Test]
        public void XMLToJSONTests_EnsureXSITypeConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureXSITypeConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureXSITypeConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure suppliment works.</summary>
        [Test]
        public void XMLToJSONTests_EnsureSupplimentConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureSupplimentConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureSupplimentConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure stock works.</summary>
        [Test]
        public void XMLToJSONTests_EnsureStockConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureStockConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureStockConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

        /// <summary>Ensure 2 models of same name works.</summary>
        [Test]
        public void XMLToJSONTests_EnsureModelsWithSameNameConverts()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureModelsWithSameNameConverts.xml");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.APSIM.Core.Resources.XMLToJSONTests_EnsureModelsWithSameNameConverts.json");

            string json = XmlToJson.Convert(xml);
            Assert.That(json, Is.EqualTo(expectedJson));
        }

    }
}
