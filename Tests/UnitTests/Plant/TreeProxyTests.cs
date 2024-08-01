using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Storage;
using Models.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
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

            
            TreeProxy treeProxy = sim.FindDescendant<TreeProxy>();
            //Pull grid data for tree proxy to make sure it's working
            List<GridTable> tables = treeProxy.Tables;
            Assert.That(tables.Count, Is.EqualTo(2));

            DataTable dtTemporal = tables[0].Data;
            DataTable dtTemporal2 = new DataTable("TreeProxySpatial");
            dtTemporal2.Columns.Add("Date");
            dtTemporal2.Columns.Add("Height");
            dtTemporal2.Columns.Add("NDemand");
            dtTemporal2.Columns.Add("ShadeModifier");
            dtTemporal2.Rows.Add(null, "m", "g/m2", "(>=0)");
            dtTemporal2.Rows.Add("1900/01/01", "1", "0.100", "1.000");
            dtTemporal2.Rows.Add("1900/03/01", "2", "0.100", "1.000");
            dtTemporal2.Rows.Add("1900/06/01", "3", "0.100", "1.000");
            dtTemporal2.Rows.Add("1900/09/01", "4", "0.100", "1.000");
            dtTemporal2.Rows.Add("1900/12/31", "5", "0.100", "1.000");
            for (int i = 0; i < dtTemporal2.Rows.Count; i++)
            {
                for (int j = 0; j < dtTemporal2.Columns.Count; j++)
                {
                    Assert.That(dtTemporal.Rows[i].ItemArray[j], Is.EqualTo(dtTemporal2.Rows[i].ItemArray[j]));
                }
            }

            DataTable dtSpatial = tables[1].Data;
            DataTable dtSpatial2 = new DataTable("TreeProxyTemporal");
            dtSpatial2.Columns.Add("Parameter");
            dtSpatial2.Columns.Add("0");
            dtSpatial2.Columns.Add("0.5h");
            dtSpatial2.Columns.Add("1h");
            dtSpatial2.Columns.Add("1.5h");
            dtSpatial2.Columns.Add("2h");
            dtSpatial2.Columns.Add("2.5h");
            dtSpatial2.Columns.Add("3h");
            dtSpatial2.Columns.Add("4h");
            dtSpatial2.Columns.Add("5h");
            dtSpatial2.Columns.Add("6h");
            dtSpatial2.Rows.Add("Shade (%)", "60", "50", "40", "30", "20", "0", "0", "0", "0", "0");
            dtSpatial2.Rows.Add("Root Length Density (cm/cm3)", null, null, null, null, null, null, null, null, null, null);
            dtSpatial2.Rows.Add("Depth (cm)", null, null, null, null, null, null, null, null, null, null);
            dtSpatial2.Rows.Add("0-15", "6", "6", "5", "4", "3", "2", "1", "0", "0", "0");
            dtSpatial2.Rows.Add("15-30", "5", "5", "4", "3", "2", "1", ".5", "0", "0", "0");
            dtSpatial2.Rows.Add("30-60", "4", "4", "3.5", "3", "2", "1", ".2", "0", "0", "0");
            dtSpatial2.Rows.Add("60-90", "2", "2", "2", "1.5", "1", "0", "0", "0", "0", "0");
            dtSpatial2.Rows.Add("90-120", "1.5", "1.5", "1.5", "1", "0", "0", "0", "0", "0", "0");
            dtSpatial2.Rows.Add("120-150", "1", "1", "1", "1", "0", "0", "0", "0", "0", "0");
            dtSpatial2.Rows.Add("150-180", "1", "0", "0", "0", "0", "0", "0", "0", "0", "0");
            for (int i = 0; i < dtSpatial2.Rows.Count; i++)
            {
                for (int j = 0; j < dtSpatial2.Columns.Count; j++)
                {
                    Assert.That(dtSpatial.Rows[i].ItemArray[j], Is.EqualTo(dtSpatial2.Rows[i].ItemArray[j]));
                }
            }

            // Get the tree proxy model instance and initialise it.1
            Utilities.CallEvent(treeProxy, "SimulationCommencing", null);

            SoilState soilState = new(topZone.FindAllChildren<Zone>().Take(1));
            soilState.Zones[0].Water = new double[] { 0.3, 0.3, 0.3 };
            soilState.Zones[0].NO3N = new double[] { 1, 1, 1 };
            treeProxy.GetNitrogenUptakeEstimates(soilState);

            // Make sure NUptake wasn't set.
            Assert.That(treeProxy.NUptake, Is.Null);

            // Once SetActualNitrogenUptakes is called, NUptake should be set.
            treeProxy.SetActualNitrogenUptakes(soilState.Zones);
            Assert.That(treeProxy.NUptake, Is.Not.Null);
        }
    }
}