using APSIM.Core;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Models;
using Models.AgPasture;
using Models.Agroforestry;
using Models.Core;
using Models.Core.ApsimFile;
using Models.PMF;
using Models.PMF.Organs;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.Core
{
    [TestFixture]
    public class RootTests
    {
        /// <summary>
        /// Test that the CalcFASW function in Root class works as expected.
        /// </summary>
        [Test]
        public void TestRootCalcFASW()
        {
            string path = System.IO.Path.Combine("%root%", "Examples", "Wheat.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;

            // Replace the soil with a test soil.
            Zone zone = sims.Node.FindChild<Zone>(recurse: true);
            Water water = zone.Node.FindChild<Water>(recurse: true);
            water.InitialValues = [0.521, 0.349, 0.280, 0.280, 0.280, 0.280, 0.280];

            // Modify the clock end date so only 1 year of simulation.
            IClock clock = sim.Node.FindChild<Clock>(recurse: true);
            clock.StartDate = new System.DateTime(2000, 1, 1);
            clock.EndDate = clock.StartDate.AddDays(1);

            // Update the wheat sowing date to be the 1nd day of the simulation.
            sim.Node.Set("[Field].Sow using a variable rule.Script.StartDate", "01-jan");
            sim.Node.Set("[Field].Sow using a variable rule.Script.EndDate", "02-jan");
            sim.Node.Set("[Field].Sow using a variable rule.Script.MinRain", -1);
            sim.Node.Set("[Field].Sow using a variable rule.Script.MinESW", -1);

            // Add detached variable to report.
            var report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = new[]
            {
                "[Clock].Today",
                "[Wheat].Root.FASW as FASW",
                "[Wheat].Root.CalcFASW(100000) as FASW100000",
                "[Wheat].Root.CalcFASW(600) as FASW600"
            };
            report.EventNames = new[] { "[Clock].DoManagement" };

            // Run simulation.
            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Reader.Refresh();

            var dataTable = storage.Reader.GetData("Report");

            var fasw = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW", CultureInfo.InvariantCulture);
            var fasw100000 = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW100000", CultureInfo.InvariantCulture);
            var fasw600 = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW600", CultureInfo.InvariantCulture);

            Assert.That(fasw.Length, Is.EqualTo(2));
            Assert.That(Math.Round(fasw.First(), 3), Is.EqualTo(0.220));
            Assert.That(Math.Round(fasw100000.First(), 3), Is.EqualTo(0.220));
            Assert.That(Math.Round(fasw600.First(), 3), Is.EqualTo(0.390));
        }

        /// <summary>Returns a soil model that can be used for testing.</summary>
        public static Soil SetupSoil()
        {
            var soil = new Soil
            {
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                        BD = new double[] { 1.011, 1.071, 1.094, 1.159, 1.173, 1.163, 1.187 },
                        AirDry = new double[] { 0.130, 0.199, 0.280, 0.280, 0.280, 0.280, 0.280 },
                        LL15 = new double[] { 0.261, 0.248, 0.280, 0.280, 0.280, 0.280, 0.280 },
                        DUL = new double[] { 0.521, 0.497, 0.488, 0.480, 0.472, 0.457, 0.452 },
                        SAT = new double[] { 0.589, 0.566, 0.557, 0.533, 0.527, 0.531, 0.522 },

                        Children = new List<IModel>()
                        {
                            new SoilCrop
                            {
                                Name = "Wheat",
                                KL = new double[] { 0.060, 0.060, 0.060, 0.040, 0.040, 0.020, 0.010 },
                                LL = new double[] { 0.261, 0.248, 0.280, 0.306, 0.360, 0.392, 0.446 }
                            }
                        }
                    },
                    new Water
                    {
                        Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                        InitialValues = new double[] { 0.313, 0.298, 0.322, 0.320, 0.318, 0.315, 0.314 },
                    },
                    new Organic
                    {
                        Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                        Carbon = new double[] { 2, 1, 0.5, 0.4, 0.3, 0.2, 0.2 }
                    },
                    new Solute
                    {
                        Name = "NO3",
                        Thickness = new double[] { 100, 300 },
                        InitialValues = new double[] { 23, 7 },
                        InitialValuesUnits = Solute.UnitsEnum.kgha
                    },
                    new Solute
                    {
                        Name = "CL",
                        Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                        InitialValues = new double[] { 38, double.NaN, 500, 490, 500, 500, 500 },
                        InitialValuesUnits = Solute.UnitsEnum.ppm
                    }
                }
            };

            return soil;
        }
    }
}