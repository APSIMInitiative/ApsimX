using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Functions;
using Models.PMF;
using Models.PMF.Organs;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.Core
{
    [TestFixture]
    public class RootTests
    {

        /// <summary>Test that the CalcFSW() function in Root class works as expected.</summary>
        [Test]
        public void TestRootCalcFASW()
        {
            Simulations simulations = Utilities.GetPlantTestingSimulation(true);
            Simulation sim = simulations.Node.FindChild<Simulation>(recurse: true);

            // Setup the report.
            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Root.FASW as FASW",
                "[Wheat].Root.CalcFASW(100000) as FASW100000",
                "[Wheat].Root.CalcFASW(600) as FASW600"
            ];
            report.EventNames = [ "[Clock].DoManagement" ];

            // Initialise water start values
            Water water = sim.Node.FindChild<Water>(recurse: true);
            water.InitialValues = [0.521, 0.349, 0.280, 0.280, 0.280, 0.280, 0.280];
            Node.Create(simulations);

            // Configure the sowing rules
            Manager sowingRuleManager = sim.Node.FindChild<Manager>(name: "SowingRule", recurse: true);
            IPlant wheat = sim.Node.FindChild<IPlant>(name: "Wheat", recurse: true);
            sowingRuleManager.SetProperty("StartDate", "01-jan");
            sowingRuleManager.SetProperty("EndDate", "02-jan");
            sowingRuleManager.SetProperty("Crop", wheat);

            sim.Prepare();
            sim.Run();

            // Get the results.
            DataStore storage = simulations.Node.FindChild<DataStore>(recurse: true);
            storage.Writer.Stop();
            storage.Refresh();
            var dataTable = storage.Reader.GetData("Report");

            var fasw = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW", CultureInfo.InvariantCulture);
            var fasw100000 = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW100000", CultureInfo.InvariantCulture);
            var fasw600 = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW600", CultureInfo.InvariantCulture);

            // Assertions
            Assert.That(fasw.Length, Is.EqualTo(2));
            Assert.That(Math.Round(fasw.First(), 3), Is.EqualTo(0.220));
            Assert.That(Math.Round(fasw100000.First(), 3), Is.EqualTo(0.220));
            Assert.That(Math.Round(fasw600.First(), 3), Is.EqualTo(0.390));
        }

        /// <summary>Tolerance for daily root storage balance checks (g/m2).</summary>
        private const double balanceTolerance = 1e-9;

        /// <summary>
        /// Root storage DM must close day by day when DM retranslocation and maintenance respiration are active.
        /// Root.Live is rebuilt from the soil-layer pools, so these checks fail if a flow is applied only to the
        /// Live aggregate. Also checks the retranslocation supply, that Allocated.StorageWt is net of retranslocation,
        /// and that the daily flows are reset once the crop has ended.
        /// </summary>
        [Test]
        public void TestRootStorageBalanceWithRetranslocationAndRespiration()
        {
            const double retranslocationFactor = 0.05;
            const double respirationRate = 0.01;
            RootReport r = new RootReport(RunWheatWithRootStorage(retranslocationFactor, respirationRate));

            int daysWithCrop = 0;
            int daysAfterCrop = 0;
            for (int t = 1; t < r.Rows; t++)
            {
                if (r.Structural[t] > 0 && r.Structural[t - 1] > 0)
                {
                    daysWithCrop++;
                    r.AssertStorageBalance(t);

                    // Retranslocation supply = factor x storage at the start of the day (not offered for reallocation).
                    double expectedSupply = retranslocationFactor * Math.Max(0.0, r.Storage[t - 1] - r.ReallocationSupply[t]);
                    Assert.That(r.RetranslocationSupply[t], Is.EqualTo(expectedSupply).Within(balanceTolerance), $"Retranslocation supply on row {t}");
                    Assert.That(r.Retranslocated[t], Is.LessThanOrEqualTo(r.RetranslocationSupply[t] + balanceTolerance), $"Retranslocation on row {t}");

                    // Maintenance respiration is today's loss only and has been removed from the layer pools:
                    // respired = rate x (pools before respiration) and pools after = (1 - rate) x pools before.
                    Assert.That(r.Respiration[t] * (1.0 - respirationRate), Is.EqualTo(respirationRate * (r.Storage[t] + r.Metabolic[t])).Within(balanceTolerance),
                                $"Maintenance respiration on row {t}");
                }
                else if (daysWithCrop > 0 && r.Structural[t] == 0 && r.Structural[t - 1] == 0)
                {
                    // Crop has ended: no daily flows may carry over from the last day of the crop.
                    daysAfterCrop++;
                    Assert.That(r.Respiration[t], Is.EqualTo(0.0), $"Maintenance respiration after crop end, row {t}");
                    Assert.That(r.Retranslocated[t], Is.EqualTo(0.0), $"Retranslocation after crop end, row {t}");
                    Assert.That(r.Allocated[t], Is.EqualTo(0.0), $"Allocation after crop end, row {t}");
                }
            }

            // Make sure the flows under test actually happened.
            Assert.That(daysWithCrop, Is.GreaterThan(100));
            Assert.That(daysAfterCrop, Is.GreaterThan(0), "Crop was not harvested before the end of the simulation");
            Assert.That(r.Storage.Max(), Is.GreaterThan(0.0));
            Assert.That(r.Retranslocated.Sum(), Is.GreaterThan(0.0), "No root DM was retranslocated");
            Assert.That(r.Respiration.Sum(), Is.GreaterThan(0.0), "No root maintenance respiration");
            // On at least one day retranslocation exceeds the gross storage growth, so the net allocation is negative.
            Assert.That(r.Allocated.Min(), Is.LessThan(0.0), "Allocated.StorageWt is never net of retranslocation");
        }

        /// <summary>
        /// The maintenance respiration rate is bounded to 0-1: a rate above 1 respires all storage and metabolic DM
        /// (no negative pools) and a negative rate respires nothing (no DM is created). The storage balance still closes.
        /// </summary>
        [TestCase(1.5)]
        [TestCase(-0.5)]
        public void TestRootMaintenanceRespirationRateIsBounded(double respirationRate)
        {
            RootReport r = new RootReport(RunWheatWithRootStorage(retranslocationFactor: 0.0, respirationRate));

            int daysWithCrop = 0;
            for (int t = 1; t < r.Rows; t++)
            {
                if (r.Structural[t] > 0 && r.Structural[t - 1] > 0)
                {
                    daysWithCrop++;
                    r.AssertStorageBalance(t);
                    Assert.That(r.Respiration[t], Is.GreaterThanOrEqualTo(0.0), $"Maintenance respiration on row {t}");
                    if (respirationRate >= 1.0)
                    {
                        Assert.That(r.Storage[t], Is.EqualTo(0.0).Within(balanceTolerance), $"Storage on row {t}");
                        Assert.That(r.Metabolic[t], Is.EqualTo(0.0).Within(balanceTolerance), $"Metabolic DM on row {t}");
                    }
                    else
                        Assert.That(r.Respiration[t], Is.EqualTo(0.0), $"Maintenance respiration on row {t}");
                }
            }

            Assert.That(daysWithCrop, Is.GreaterThan(50));
            Assert.That(r.Allocated.Sum(), Is.GreaterThan(0.0), "No storage DM was allocated to the root");
            if (respirationRate >= 1.0)
                Assert.That(r.Respiration.Sum(), Is.GreaterThan(0.0));
            else
                Assert.That(r.Storage.Max(), Is.GreaterThan(0.0));
        }

        /// <summary>Daily root DM report columns written by <see cref="RunWheatWithRootStorage"/>.</summary>
        private sealed class RootReport
        {
            public double[] Structural, Storage, Metabolic, Allocated, Retranslocated, RetranslocationSupply,
                            ReallocationSupply, Detached, Removed, Respiration;

            public int Rows => Storage.Length;

            public RootReport(DataTable data)
            {
                double[] column(string name) => DataTableUtilities.GetColumnAsDoubles(data, name, CultureInfo.InvariantCulture);
                Structural = column("StructuralWt");
                Storage = column("StorageWt");
                Metabolic = column("MetabolicWt");
                Allocated = column("AllocatedStorageWt");
                Retranslocated = column("RetranslocationWt");
                RetranslocationSupply = column("RetranslocationSupply");
                ReallocationSupply = column("ReallocationSupply");
                Detached = column("DetachedStorageWt");
                Removed = column("RemovedStorageWt");
                Respiration = column("MaintenanceRespiration");
            }

            /// <summary>
            /// Assert that the root storage DM balance closes on row t:
            /// change in Live.StorageWt = Allocated.StorageWt (net of retranslocation) - Detached - Removed - maintenance respiration.
            /// Root metabolic DM is zero in these tests, so maintenance respiration comes from storage only.
            /// </summary>
            public void AssertStorageBalance(int t)
            {
                Assert.That(Metabolic[t], Is.EqualTo(0.0), $"Root metabolic DM on row {t}");
                double change = Storage[t] - Storage[t - 1];
                double net = Allocated[t] - Detached[t] - Removed[t] - Respiration[t];
                Assert.That(change - net, Is.EqualTo(0.0).Within(balanceTolerance), $"Root storage balance residual on row {t}");
            }
        }

        /// <summary>
        /// Run the first year of the Wheat example with a root storage pool, the given root DM retranslocation factor
        /// and maintenance respiration rate, and return the daily report.
        /// </summary>
        private static DataTable RunWheatWithRootStorage(double retranslocationFactor, double respirationRate)
        {
            string path = PathUtilities.GetAbsolutePath(Path.Combine("%root%", "Examples", "Wheat.apsimx"), null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);

            // One year: the crop is sown, harvested and ended.
            IClock clock = sim.Node.FindChild<Clock>(recurse: true);
            clock.EndDate = clock.StartDate.AddYears(1);

            // Give the root a storage pool (40% of root DM), a DM retranslocation supply and maintenance respiration.
            Root root = sim.Node.FindChild<Root>(recurse: true);
            NutrientDemandFunctions dmDemands = root.Node.FindChild<NutrientDemandFunctions>(name: "DMDemands");
            dmDemands.Node.FindChild<Constant>(name: "StructuralFraction", recurse: true).FixedValue = 0.6;
            root.Node.FindChild<Constant>(name: "MaintenanceRespirationFunction").FixedValue = respirationRate;
            root.Node.AddChild(new Constant { Name = "DMRetranslocationFactor", FixedValue = retranslocationFactor });

            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Root.Live.StructuralWt as StructuralWt",
                "[Wheat].Root.Live.StorageWt as StorageWt",
                "[Wheat].Root.Live.MetabolicWt as MetabolicWt",
                "[Wheat].Root.Allocated.StorageWt as AllocatedStorageWt",
                "[Wheat].Root.RetranslocationWt as RetranslocationWt",
                "[Wheat].Root.DMSupply.ReTranslocation as RetranslocationSupply",
                "[Wheat].Root.DMSupply.ReAllocation as ReallocationSupply",
                "[Wheat].Root.Detached.StorageWt as DetachedStorageWt",
                "[Wheat].Root.Removed.StorageWt as RemovedStorageWt",
                "[Wheat].Root.MaintenanceRespiration as MaintenanceRespiration"
            ];
            report.EventNames = [ "[Clock].DoReport" ];

            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Refresh();
            return storage.Reader.GetData("Report");
        }
    }
}