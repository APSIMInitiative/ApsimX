using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Storage;
using NUnit.Framework;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.Core
{
    [TestFixture]
    public class TreeProxyTests
    {
        /// <summary>
        /// Ensure that the TreeProxy model calculates its NUptake in SetActualNitrogenUptakes method
        /// rather than GetNitrogenUptakeEstimates. Issue #3566
        /// </summary>
        /// <param name="fileName"></param>
        [Test]
        public void TestTreeProxyDoesNUptakeInSetActualNitrogenUptakes()
        {
            // Open the wheat example.
            string path = Path.Combine("%root%", "Examples", "Agroforestry", "Single Tree Example.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path, e => throw e, false).NewModel as Simulations;
            foreach (Soil soil in sims.FindAllDescendants<Soil>())
                soil.Standardise();
            DataStore storage = sims.FindDescendant<DataStore>();
            storage.UseInMemoryDB = true;
            Simulation sim = sims.FindDescendant<Simulation>();
            Utilities.ResolveLinks(sim);
            Zone topZone = sim.FindChild<Zone>();    

            // Get the clockmodel instance and initialise it.
            var clock = sim.FindDescendant<Clock>();
            clock.StartDate = new System.DateTime(1900, 10, 1);
            Utilities.CallEvent(clock, "SimulationCommencing", null);

            // Get the tree proxy model instance and initialise it.
            var treeProxy = sim.FindDescendant<TreeProxy>();
            Utilities.CallEvent(treeProxy, "SimulationCommencing", null);

            SoilState soilState = new(topZone.FindAllChildren<Zone>().Take(1));
            soilState.Zones[0].Water = new double[] { 0.3, 0.3, 0.3 };
            soilState.Zones[0].NO3N = new double[] { 1, 1, 1 };
            treeProxy.GetNitrogenUptakeEstimates(soilState);

            // Make sure NUptake wasn't set.
            Assert.IsNull(treeProxy.NUptake);

            // Once SetActualNitrogenUptakes is called, NUptake should be set.
            treeProxy.SetActualNitrogenUptakes(soilState.Zones);
            Assert.IsNotNull(treeProxy.NUptake);
        }
    }
}