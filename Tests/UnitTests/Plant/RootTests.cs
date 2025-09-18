using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Soils;
using Models.Storage;
using Models.WaterModel;
using NUnit.Framework;
using System;
using System.Globalization;
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
            storage.Reader.Refresh();
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

        

        /// <summary>Returns a soil model that can be used for testing.</summary>
        public static Soil SetupSoil(Simulations sims)
        {
            var soil = new Soil
            {
                Children =
                [
                    new Physical()
                    {
                        Thickness = [150, 150, 300, 300, 300, 300, 300],
                        BD = [1.011, 1.071, 1.094, 1.159, 1.173, 1.163, 1.187],
                        AirDry = [0.130, 0.199, 0.280, 0.280, 0.280, 0.280, 0.280],
                        LL15 = [0.261, 0.248, 0.280, 0.280, 0.280, 0.280, 0.280],
                        DUL = [0.521, 0.497, 0.488, 0.480, 0.472, 0.457, 0.452],
                        SAT = [0.589, 0.566, 0.557, 0.533, 0.527, 0.531, 0.522],

                        Children =
                        [
                            new SoilCrop
                            {
                                Name = "Wheat",
                                KL = [0.060, 0.060, 0.060, 0.040, 0.040, 0.020, 0.010],
                                LL = [0.261, 0.248, 0.280, 0.306, 0.360, 0.392, 0.446]
                            }
                        ]
                    },
                    new Water
                    {
                        Thickness = [150, 150, 300, 300, 300, 300, 300],
                        InitialValues = [0.313, 0.298, 0.322, 0.320, 0.318, 0.315, 0.314],
                    },
                    new Organic
                    {
                        Thickness = [150, 150, 300, 300, 300, 300, 300],
                        Carbon = [2, 1, 0.5, 0.4, 0.3, 0.2, 0.2]
                    },
                    new Solute
                    {
                        Name = "NO3",
                        Thickness = [100, 300],
                        InitialValues = [23, 7],
                        InitialValuesUnits = Solute.UnitsEnum.kgha
                    },
                    new Solute
                    {
                        Name = "CL",
                        Thickness = [150, 150, 300, 300, 300, 300, 300],
                        InitialValues = [38, double.NaN, 500, 490, 500, 500, 500],
                        InitialValuesUnits = Solute.UnitsEnum.ppm
                    },
                ]
            };
            soil.Children.Add(sims.Node.FindChild<WaterBalance>(recurse: true));

            return soil;
        }
    }
}