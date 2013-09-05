using Model.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ModelTests
{
    
    /// <summary>
    ///This is a test class for SystemComponentTest and is intended
    ///to contain all SystemComponentTest Unit Tests
    ///</summary>
    [TestClass]
    public class ZoneTest
    {
        private Simulations S;
        public TestContext TestContext {get; set;}

        [TestInitialize]
        public void Initialise()
        {
            FileStream F = new FileStream("Test.apsimx", FileMode.Create);
            F.Write(Properties.Resources.TestFile, 0, Properties.Resources.TestFile.Length);
            F.Close();
            S = Utility.Xml.Deserialise("Test.apsimx") as Simulations;
        }


        [TestCleanup]
        public void Cleanup()
        {
            File.Delete("Test.apsimx");
        }

        /// <summary>
        /// A test for FullPath
        /// </summary>
        [TestMethod]
        public void FullPathTest()
        {
            ISimulation Sim = S.Models[0] as ISimulation;

            Assert.AreEqual(Sim.FullPath, ".");
            Assert.AreEqual((Sim.Models[3] as IZone).FullPath, ".Field1");
        }


        /// <summary>
        /// Scoping rule tests
        /// </summary>
        [TestMethod]
        public void ScopingRules()
        {
            ISimulation Sim = S.Models[0] as ISimulation;

            IZone Field1 = Sim.Models[3] as IZone;

            // Make sure we can get a link to a local model from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find("Field1Report")), "Field1Report");
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find(typeof(Model.Components.Report))), "Field1Report");

            // Make sure we can get a link to a model in top level zone from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find("WeatherFile")), "WeatherFile");
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find(typeof(Model.Components.WeatherFile))), "WeatherFile");

            // Make sure we can get a link to a model in top level Simulations zone from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find(typeof(Model.Components.DataStore))), "DataStore");

            // Make sure we can't get a link to a model in Field2 from Field1
            Assert.IsNull(Field1.Find("Graph"));
            Assert.IsNull(Field1.Find(typeof(Model.Components.Graph.Graph)));

            // Make sure we can't get a link to a model in a child field.
            IZone Field2 = Sim.Models[4] as IZone;
            Assert.IsNull(Field2.Find("Field2SubZoneReport"));
            Assert.IsNull(Field2.Find(typeof(Model.Components.Report)));

            // Make sure we can get a link from a child, child zone to the top level zone.
            IZone Field2SubZone = Field2.Models[1] as IZone;
            Assert.AreEqual(Utility.Reflection.Name(Field2SubZone.Find("WeatherFile")), "WeatherFile");
            Assert.AreEqual(Utility.Reflection.Name(Field2SubZone.Find(typeof(Model.Components.WeatherFile))), "WeatherFile");
        }

        /// <summary>
        /// Scoping rule tests
        /// </summary>
        [TestMethod]
        public void Get()
        {
            ISimulation Sim = S.Models[0] as ISimulation;

            IZone Field1 = Sim.Models[3] as IZone;

            // Make sure we can get a link to a local model from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get("Field1Report")), "Field1Report");
            
            // Make sure we can get a variable from a local model.
            Assert.AreEqual(Field1.Get("Field1Report.Name"), "Field1Report");

            // Make sure we can get a variable from a local model using a full path.
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get(".Field1.Field1Report")), "Field1Report");
            Assert.AreEqual(Field1.Get(".Field1.Field1Report.Name"), "Field1Report");

            // Make sure we get a null when trying to link to a top level model from Field1
            Assert.IsNull(Field1.Get("WeatherFile"));

            // Make sure we can get a top level model from Field1 using a full path.
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get(".WeatherFile")), "WeatherFile");

            // Make sure we can get a model in Field2 from Field1 using a full path.
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get(".Field2.Graph1")), "Graph1");

            // Make sure we can get a property from a model in Field2 from Field1 using a full path.
            Assert.AreEqual(Field1.Get(".Field2.Graph1.Name"), "Graph1");

            // Make sure we can get a property from a model in Field2/Field2SubZone from Field1 using a full path.
            Assert.AreEqual(Field1.Get(".Field2.Field2SubZone.Field2SubZoneReport.Name"), "Field2SubZoneReport");
            
        }

    }


}
