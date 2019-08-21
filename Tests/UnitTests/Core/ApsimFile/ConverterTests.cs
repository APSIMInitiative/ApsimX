namespace UnitTests.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using NUnit.Framework;
    using System.Data;
    using System.IO;
    using System.Xml;

    /// <summary>This is a test class for the .apsimx file converter.</summary>
    [TestFixture]
    public class ConverterTests
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

            var converter = Converter.DoConvert(fromXML, 1);
            Assert.IsTrue(converter.DidConvert);

            string toXML = "<Simulation Version=\"1\">" +
                             "<Graph>" +
                               "<Series>" +
                                 "<TableName>HarvestReport</TableName>" +
                                 "<XFieldName>Maize.Population</XFieldName>" +
                                 "<YFieldName>GrainWt</YFieldName>" +
                               "</Series>" +
                             "</Graph>" +
                           "</Simulation>";
            Assert.AreEqual(converter.RootXml.OuterXml, toXML);
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

            var converter = Converter.DoConvert(fromXML, 2);
            Assert.IsTrue(converter.DidConvert);

            string toXML = "<Simulation Version=\"2\">" +
                             "<Cultivar>" +
                                 "<Alias>" +
                                   "<Name>Cultivar1</Name>" +
                                 "</Alias>" +
                                 "<Alias>" +
                                   "<Name>Cultivar2</Name>" +
                                 "</Alias>" +
                             "</Cultivar>" +
                           "</Simulation>";
            Assert.AreEqual(converter.RootXml.OuterXml, toXML);
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
                             "    [Serializable]\r\n" +
                             "    public class Script : Model\r\n" +
                             "    {\r\n" +
                             "        [Link] Clock Clock;\r\n" +
                             "        [Link] Fertiliser Fertiliser;\r\n" +
                             "        [Link] Summary Summary;\r\n" +
                             "        private void OnDoManagement(object sender, EventArgs e)\r\n" +
                             "        {\r\n" +
                             "            accumulatedRain.Update();\r\n" +
                             "            if (DateUtilities.WithinDates(StartDate, Clock.Today, EndDate) &&\r\n" +
                             "                Soil.SoilWater.ESW > MinESW &&\r\n" +
                             "                accumulatedRain.Sum > MinRain)\r\n" +
                             "            {\r\n" +
                             "                Wheat.Sow(population: Population, cultivar: CultivarName, depth: SowingDepth, rowSpacing: RowSpacing);\r\n" +
                             "            }\r\n" +
                             "        }\r\n" +
                             "    }\r\n" +
                             "}\r\n" +
                             "]]></Code>\r\n" +
                             "      </Manager>\r\n" +
                             "   <Report>\r\n" +
                                   "<Name>Report</Name>\r\n" +
                                   "<VariableNames>\r\n" +
                                      "<string>[Clock].Today</string>\r\n" +
                                      "<string>[MySoil].SoilWater.ESW</string>\r\n" +
                                   "</VariableNames>\r\n" +
                             "   </Report>\r\n" +
                " </Simulation>\r\n";

            var converter = Converter.DoConvert(fromXML, 7);
            Assert.IsTrue(converter.DidConvert);

            string toXML = "<Simulation Version=\"7\">" +
                             "<Manager>" +
                             "<Code>"+
                             "<![CDATA[using System;\r\n" +
                             "using Models.Core;\r\n" +
                             "using Models.PMF;\r\n" +
                             "using APSIM.Shared.Utilities;\r\n" +
                             "namespace Models\r\n" +
                             "{\r\n" +
                             "    [Serializable]\r\n" +
                             "    public class Script : Model\r\n" +
                             "    {\r\n" +
                             "        [Link] Clock Clock;\r\n" +
                             "        [Link] Fertiliser Fertiliser;\r\n" +
                             "        [Link] Summary Summary;\r\n" +
                             "        private void OnDoManagement(object sender, EventArgs e)\r\n" +
                             "        {\r\n" +
                             "            accumulatedRain.Update();\r\n" +
                             "            if (DateUtilities.WithinDates(StartDate, Clock.Today, EndDate) &&\r\n" +
                             "                MathUtilities.Sum(Soil.SoilWater.ESW) > MinESW &&\r\n" +
                             "                accumulatedRain.Sum > MinRain)\r\n" +
                             "            {\r\n" +
                             "                Wheat.Sow(population: Population, cultivar: CultivarName, depth: SowingDepth, rowSpacing: RowSpacing);\r\n" +
                             "            }\r\n" +
                             "        }\r\n" +
                             "    }\r\n" +
                             "}\r\n" +
                             "]]></Code>" +
                             "</Manager>" +
                             "<Report>" +
                                "<Name>Report</Name>" +
                                "<VariableNames>" +
                                    "<string>[Clock].Today</string>" +
                                    "<string>sum([MySoil].SoilWater.ESW)</string>" +
                                "</VariableNames>" +
                             "</Report>" +
                           "</Simulation>";
            Assert.AreEqual(converter.RootXml.OuterXml, toXML);
        }

        /// <summary>Test version 10</summary>
        [Test]
        public void Version10()
        {
            string fromXML = "<Simulation Version=\"9\">\r\n" +
                             "   <GenericOrgan>\r\n" +
                                   "<Name>Stem</Name>\r\n" +
                             "   </GenericOrgan>\r\n" +
                             " </Simulation>\r\n";

            var converter = Converter.DoConvert(fromXML, 10);
            Assert.IsTrue(converter.DidConvert);

            string toXML = "<Simulation Version=\"10\">" +
                             "<GenericOrgan>" +
                               "<Name>Stem</Name>" +
                               "<Constant>" +
                                 "<Name>NRetranslocationFactor</Name>" +
                                 "<FixedValue>0.0</FixedValue>" +
                               "</Constant>" +
                               "<Constant>" +
                                 "<Name>NitrogenDemandSwitch</Name>" +
                                 "<FixedValue>1.0</FixedValue>" +
                               "</Constant>" +
                               "<Constant>" +
                                 "<Name>DMReallocationFactor</Name>" +
                                 "<FixedValue>0.0</FixedValue>" +
                               "</Constant>" +
                               "<Constant>" +
                                 "<Name>DMRetranslocationFactor</Name>" +
                                 "<FixedValue>0.0</FixedValue>" +
                               "</Constant>" +
                               "<VariableReference>" +
                                 "<Name>CriticalNConc</Name>" +
                                 "<VariableName>[Stem].MinimumNConc.Value()</VariableName>" +
                               "</VariableReference>" +
                             "</GenericOrgan>" +
                            "</Simulation>";
            Assert.AreEqual(converter.RootXml.OuterXml, toXML);
        }

        /// <summary>Test version 11</summary>
        [Test]
        public void Version11()
        {
            string fromXML = "<Simulation Version=\"10\">\r\n" +
                             "  <Manager>\r\n" +
                             "    <Code><![CDATA[using System;\r\n" +
                             "using Models.Core;\r\n" +
                             "using Models.PMF;\r\n" +
                             "Wheat.NonStructuralDemand + xyz\r\n" +
                             "Wheat.TotalNonStructuralDemand + xyz\r\n" +
                             "]]></Code>\r\n" +
                             "      </Manager>\r\n" +
                             "   <Report>\r\n" +
                                   "<Name>Report</Name>\r\n" +
                                   "<VariableNames>\r\n" +
                                      "<string>[Wheat].NonStructural</string>\r\n" +
                                      "<string>[Wheat].NonStructural.Wt</string>\r\n" +
                                   "</VariableNames>\r\n" +
                             "   </Report>\r\n" +
                             "  <Graph>" +
                             "    <Series>" +
                             "      <XFieldName>Observed.Wheat.AboveGround.NonStructural.Wt</XFieldName>\r\n" +
                             "      <YFieldName>Predicted.Wheat.AboveGround.Wt</YFieldName>\r\n" +
                             "    </Series>" +
                             "  </Graph>" +
                             "  <VariableReference>" +
                             "    <Name>WSC</Name>" +
                             "    <IncludeInDocumentation>true</IncludeInDocumentation>" +
                             "    <VariableName>[Stem].Live.NonStructural.Wt</VariableName>" +
                             "  </VariableReference>" +
                             "  <LinearInterpolationFunction>" +
                             "    <Name>WaterStressEffect</Name>" +
                             "    <XYPairs>" +
                             "      <Name>XYPairs</Name>" +
                             "      <IncludeInDocumentation>true</IncludeInDocumentation>" +
                             "      <X>" +
                             "        <double>0.5</double>" +
                             "        <double>1</double>" +
                             "      </X>" +
                             "      <Y>" +
                             "        <double>0.1</double>" +
                             "        <double>1</double>" +
                             "      </Y>" +
                             "    </XYPairs>" +
                             "    <IncludeInDocumentation>true</IncludeInDocumentation>" +
                             "    <XProperty>[Stem].Live.NonStructural.Wt</XProperty>" +
                             "  </LinearInterpolationFunction>" +
                             "  <NonStructuralNReallocated>" +
                             "    <Value>1</Value>" +
                             "  </NonStructuralNReallocated>" +
                             "</Simulation>\r\n";

            var converter = Converter.DoConvert(fromXML, 11);
            Assert.IsTrue(converter.DidConvert);

            string toXML = "<Simulation Version=\"11\">" +
                             "<Manager>" +
                               "<Code><![CDATA[using System;\r\n" +
                               "using Models.Core;\r\n" +
                               "using Models.PMF;\r\n" +
                               "Wheat.StorageDemand + xyz\r\n" +
                               "Wheat.TotalStorageDemand + xyz\r\n" +
                               "]]></Code>" +
                             "</Manager>" +
                             "<Report>" +
                               "<Name>Report</Name>" +
                               "<VariableNames>" +
                                  "<string>[Wheat].Storage</string>" +
                                  "<string>[Wheat].Storage.Wt</string>" +
                               "</VariableNames>" +
                             "</Report>" +
                             "<Graph>" +
                               "<Series>" +
                                 "<XFieldName>Observed.Wheat.AboveGround.Storage.Wt</XFieldName>" +
                                 "<YFieldName>Predicted.Wheat.AboveGround.Wt</YFieldName>" +
                               "</Series>" +
                             "</Graph>" +
                             "<VariableReference>" +
                               "<Name>WSC</Name>" +
                               "<IncludeInDocumentation>true</IncludeInDocumentation>" +
                               "<VariableName>[Stem].Live.Storage.Wt</VariableName>" +
                             "</VariableReference>" +
                             "<LinearInterpolationFunction>" +
                               "<Name>WaterStressEffect</Name>" +
                               "<XYPairs>" +
                                 "<Name>XYPairs</Name>" +
                                 "<IncludeInDocumentation>true</IncludeInDocumentation>" +
                                 "<X>" +
                                   "<double>0.5</double>" +
                                   "<double>1</double>" +
                                 "</X>" +
                                 "<Y>" +
                                   "<double>0.1</double>" +
                                   "<double>1</double>" +
                                 "</Y>" +
                               "</XYPairs>" +
                               "<IncludeInDocumentation>true</IncludeInDocumentation>" +
                               "<XProperty>[Stem].Live.Storage.Wt</XProperty>" +
                             "</LinearInterpolationFunction>" +
                             "<StorageNReallocated>" +
                               "<Value>1</Value>" +
                             "</StorageNReallocated>" +
                           "</Simulation>";
            Assert.AreEqual(converter.RootXml.OuterXml, toXML);
        }

        public void Version9()
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());

            string fileName = Path.Combine(Path.GetTempPath(), "TestConverter.db");
            File.Delete(fileName);
            SQLite connection = new SQLite();
            connection.OpenDatabase(fileName, false);
            try
            {
                connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT COLLATE NOCASE)");
                connection.ExecuteNonQuery("CREATE TABLE Messages (SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");
                connection.ExecuteNonQuery("CREATE TABLE _Units (TableName TEXT, ColumnHeading TEXT, Units TEXT)");
                connection.ExecuteNonQuery("CREATE TABLE Report (Col1 TEXT, Col2 TEXT, Col3 TEXT)");

                string fromXML = "<Simulation Version=\"8\"/>";

                var converter = Converter.DoConvert(fromXML, 9);
                Assert.IsTrue(converter.DidConvert);

                DataTable tableData = connection.ExecuteQuery("SELECT * FROM sqlite_master");
                string[] tableNames = DataTableUtilities.GetColumnAsStrings(tableData, "Name");
                Assert.AreEqual(tableNames, new string[] { "_Simulations", "_Messages", "_Units", "Report" } );
            }
            finally
            {
                connection.CloseDatabase();
                File.Delete(fileName);
            }
        }

        [Test]
        public void Version32()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion32 before.xml");
            string expectedXml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion32 after.xml");

            var converter = Converter.DoConvert(xml, 33);
            Assert.IsTrue(converter.DidConvert);

            using (StringWriter writer = new StringWriter())
            {
                converter.RootXml.Save(writer);
                Assert.AreEqual(writer.ToString(), expectedXml);
            }
        }

        [Test]
        public void Version59()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion59 before.json");
            string expectedXml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion59 after.json");

            var converter = Converter.DoConvert(xml, 59);
            Assert.IsTrue(converter.DidConvert);

            using (StringWriter writer = new StringWriter())
            {
                writer.Write(converter.Root.ToString());
                Assert.AreEqual(writer.ToString(), expectedXml);
            }
        }

        [Test]
        public void Version60()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion60 before.json");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion60 after.json");

            var converter = Converter.DoConvert(json, 60);
            Assert.IsTrue(converter.DidConvert);

            using (StringWriter writer = new StringWriter())
            {
                writer.Write(converter.Root.ToString());
                Assert.AreEqual(writer.ToString(), expectedJson);
            }
        }

        [Test]
        public void Version63()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion63 before.json");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion63 after.json");

            var converter = Converter.DoConvert(json, 63);
            Assert.IsTrue(converter.DidConvert);

            using (StringWriter writer = new StringWriter())
            {
                writer.Write(converter.Root.ToString());
                Assert.AreEqual(writer.ToString(), expectedJson);
            }
        }

    }
}
