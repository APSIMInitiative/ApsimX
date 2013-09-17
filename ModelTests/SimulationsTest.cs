using Model.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Model;
using System.Xml;
using Model.Components;

namespace ModelTests
{
    /// <summary>
    /// This is a test class for SimulationsTest and is intended
    /// to contain all SimulationsTest Unit Tests
    ///</summary>
    [TestClass]
    public class SimulationsTest
    {
        private static Simulations S = null;
        public TestContext TestContext { get; set; }

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
        /// A test to ensure that 'Models' property is valid.
        /// </summary>
        [TestMethod]
        public void ModelsTest()
        {
            Assert.AreEqual(S.Models.Count, 2);
            Assert.AreEqual(Utility.Reflection.Name(S.Models[0]), "Test");

            Simulation Sim = S.Models[0] as Simulation;
            Assert.AreEqual(Sim.Models.Count, 5);
            Assert.AreEqual(Sim.Models[0].Name, "WeatherFile");
            Assert.AreEqual(Sim.Models[1].Name, "Clock");
            Assert.AreEqual(Sim.Models[2].Name, "Summary");
            Assert.AreEqual(Sim.Models[3].Name, "Field1");
            Assert.AreEqual(Sim.Models[4].Name, "Field2");

            Zone Z = Sim.Models[3] as Zone;
            Assert.AreEqual(Z.Models.Count, 1);
            Assert.AreEqual(Utility.Reflection.Name(Z.Models[0]), "Field1Report");
        }

        /// <summary>
        /// A test for AddChild method
        /// </summary>
        [TestMethod]
        public void AddChildTest()
        {
            string ChildXml = "<Clock>" +
                              "   <Name>Clock</Name>" +
                              "   <StartDate>1940-01-01T00:00:00</StartDate>" +
                              "   <EndDate>1989-12-31T00:00:00</EndDate>" +
                              "</Clock>";
            Zone Sim = S.Models[0] as Zone;

            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(ChildXml);
            Clock Clock = Utility.Xml.Deserialise(Doc.DocumentElement) as Clock;
            Sim.AddModel(Clock as Model.Core.Model);
        }
    }
}
