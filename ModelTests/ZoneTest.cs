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
        ///A test for FullPath
        ///</summary>
        [TestMethod]
        public void FullPathTest()
        {
            ISimulation Sim = S.Sims[0] as ISimulation;

            Assert.AreEqual(Sim.FullPath, "Tsest");
            Assert.AreEqual((Sim.Models[3] as IZone).FullPath, "Test.Field");
        }
    }


}
