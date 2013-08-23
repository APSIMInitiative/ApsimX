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
            Assert.AreEqual(S.Sims.Count, 1);
            Assert.AreEqual(Utility.Reflection.Name(S.Sims[0]), "Test");

            ISimulation Sim = S.Sims[0] as ISimulation;
            Assert.AreEqual(Sim.Models.Count, 5);
            Assert.AreEqual(Utility.Reflection.Name(Sim.Models[0]), "DataStore");
            Assert.AreEqual(Utility.Reflection.Name(Sim.Models[1]), "WeatherFile");
            Assert.AreEqual(Utility.Reflection.Name(Sim.Models[2]), "Clock");
            Assert.AreEqual(Utility.Reflection.Name(Sim.Models[3]), "Field");
            Assert.AreEqual(Utility.Reflection.Name(Sim.Models[4]), "Graph");

            IZone Z = Sim.Models[3] as IZone;
            Assert.AreEqual(Z.Models.Count, 1);
            Assert.AreEqual(Utility.Reflection.Name(Z.Models[0]), "Report");
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
            IZone Sim = S.Sims[0];

            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(ChildXml);
            Clock Clock = Utility.Xml.Deserialise(Doc.DocumentElement) as Clock;
            Sim.Models.Add(Clock);
        }
    }
}
