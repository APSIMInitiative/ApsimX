// -----------------------------------------------------------------------
// <copyright file="TestConverter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UnitTests
{
    using Models;
    using Models.Core;
    using Models.Graph;
    using Models.Soils;
    using NUnit.Framework;
    using System.IO;
    using System.Xml;

    /// <summary>This is a test class for the .apsimx file converter.</summary>
    [TestFixture]
    public class TestConverter
    {
        /// <summary>Test version 1</summary>
        [Test]
        public void Version1()
        {
            string fromXML = "<Simulation Version=\"0\">" +
                             "  <Graph>" +
                             "    <Series>" +
                             "      <X>" +
                             "        <TableName>HarvestReport</TableName>" +
                             "        <FieldName>Maize.Population</FieldName>" +
                             "      </X>" +
                             "      <Y>" +
                             "        <TableName>HarvestReport</TableName>" +
                             "        <FieldName>GrainWt</FieldName>" +
                             "      </Y>" +
                             "    </Series>" +
                             "  </Graph>" +
                             "</Simulation>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fromXML);
            Assert.IsTrue(APSIMFileConverter.ConvertToLatestVersion(doc.DocumentElement));

            string toXML = "<Simulation Version=\"" + APSIMFileConverter.LastestVersion + "\">" +
                             "<Graph>" +
                               "<Series>" +
                                 "<TableName>HarvestReport</TableName>" +
                                 "<XFieldName>Maize.Population</XFieldName>" +
                                 "<YFieldName>GrainWt</YFieldName>" +
                               "</Series>" +
                             "</Graph>" +
                           "</Simulation>";
            Assert.AreEqual(doc.DocumentElement.OuterXml, toXML);
        }

        /// <summary>Test version 2</summary>
        [Test]
        public void Version2()
        {
            string fromXML = "<Simulation Version=\"0\">" +
                             "  <Cultivar>" +
                             "    <Alias>Cultivar1</Alias>" +
                             "    <Alias>Cultivar2</Alias>" +
                             "  </Cultivar>" +
                             "</Simulation>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fromXML);
            Assert.IsTrue(APSIMFileConverter.ConvertToLatestVersion(doc.DocumentElement));

            string toXML = "<Simulation Version=\"" + APSIMFileConverter.LastestVersion + "\">" +
                             "<Cultivar>" +
                                 "<Alias>" +
                                   "<Name>Cultivar1</Name>" +
                                 "</Alias>" +
                                 "<Alias>" +
                                   "<Name>Cultivar2</Name>" +
                                 "</Alias>" +
                             "</Cultivar>" +
                           "</Simulation>";
            Assert.AreEqual(doc.DocumentElement.OuterXml, toXML);
        }

        /// <summary>Test version 7</summary>
        [Test]
        public void Version7()
        {
            string fromXML = "<Simulation Version=\"6\">\r\n" +
                             "  <Manager>\r\n" +
                             "    <Code><![CDATA[using System;\r\n" +
                             "using Models.Core;\r\n" +
                             "using Models.PMF;\r\n" +
                             "namespace Models\r\n" +
                             "{\r\n" +
                             "    	[Serializable]\r\n" +
                             "    	public class Script : Model\r\n" +
                             "    	{\r\n" +
                             "    		[Link] Clock Clock;\r\n" +
                             "    		[Link] Fertiliser Fertiliser;\r\n" +
                             "    		[Link] Summary Summary;\r\n" +
                             "          private void OnDoManagement(object sender, EventArgs e)\r\n" +
                             "          {\r\n" +
                             "          	accumulatedRain.Update();\r\n" +
                             "          	if (DateUtilities.WithinDates(StartDate, Clock.Today, EndDate) &&\r\n" +
                             "          	    Wheat.plant_status == \"out\" &&\r\n" +
                             "          	    Soil.SoilWater.ESW > MinESW &&\r\n" +
                             "          	    accumulatedRain.Sum > MinRain)\r\n" +
                             "          	{\r\n" +
                             "          		Wheat.Sow(population: Population, cultivar: CultivarName, depth: SowingDepth, rowSpacing: RowSpacing);\r\n" +
                             "          	}\r\n" +
                             "          }\r\n" +
                             "    	}\r\n" +
                             "}\r\n" +
                             "]]></Code>\r\n" +
                             "      </Manager>\r\n" +
                             "   <Report>\r\n" +
                                   "<Name>Report</Name>\r\n" +
                                   "<VariableNames>\r\n" +
                                      "<string>[Clock].Today</string>\r\n" +
                                      "<string>MySoil.SoilWater.ESW</string>\r\n" +
                                   "</VariableNames>\r\n" +
                             "   </Report>\r\n" +
                " </Simulation>\r\n";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fromXML);
            Assert.IsTrue(APSIMFileConverter.ConvertToLatestVersion(doc.DocumentElement));

            string toXML = "<Simulation Version=\"" + APSIMFileConverter.LastestVersion + "\">" +
                             "<Manager>" +
                             "<Code>"+
                             "<![CDATA[using System;\r\n" +
                             "using Models.Core;\r\n" +
                             "using Models.PMF;\r\n" +
                             "using APSIM.Shared.Utilities;\r\n" +
                             "namespace Models\r\n" +
                             "{\r\n" +
                             "    	[Serializable]\r\n" +
                             "    	public class Script : Model\r\n" +
                             "    	{\r\n" +
                             "    		[Link] Clock Clock;\r\n" +
                             "    		[Link] Fertiliser Fertiliser;\r\n" +
                             "    		[Link] Summary Summary;\r\n" +
                             "          private void OnDoManagement(object sender, EventArgs e)\r\n" +
                             "          {\r\n" +
                             "          	accumulatedRain.Update();\r\n" +
                             "          	if (DateUtilities.WithinDates(StartDate, Clock.Today, EndDate) &&\r\n" +
                             "          	    Wheat.plant_status == \"out\" &&\r\n" +
                             "          	    MathUtilities.Sum(Soil.SoilWater.ESW) > MinESW &&\r\n" +
                             "          	    accumulatedRain.Sum > MinRain)\r\n" +
                             "          	{\r\n" +
                             "          		Wheat.Sow(population: Population, cultivar: CultivarName, depth: SowingDepth, rowSpacing: RowSpacing);\r\n" +
                             "          	}\r\n" +
                             "          }\r\n" +
                             "    	}\r\n" +
                             "}\r\n" +
                             "]]></Code>" +
                             "</Manager>" +
                             "<Report>" +
                                "<Name>Report</Name>" +
                                "<VariableNames>" +
                                    "<string>[Clock].Today</string>" +
                                    "<string>sum(MySoil.SoilWater.ESW)</string>" +
                                "</VariableNames>" +
                             "</Report>" +
                             "</Simulation>";
            Assert.AreEqual(doc.DocumentElement.OuterXml, toXML);
        }

    }
}
