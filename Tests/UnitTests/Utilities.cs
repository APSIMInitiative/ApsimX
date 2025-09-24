using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Core;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Storage;
using NUnit.Framework;
using Models.Soils;
using Models.WaterModel;
using Models.PMF;
using Models.Surface;
using Models.Soils.SoilTemp;
using Models.Soils.Nutrients;

namespace UnitTests
{

    [SetUpFixture]
    public static class Utilities
    {
        private static string tempPath;
        [OneTimeTearDown]
        public static void TearDown()
        {
            Directory.Delete(tempPath, true);
        }

        [OneTimeSetUp]
        public static void OneTimeSetUp()
        {
            tempPath = Path.Combine(Path.GetTempPath(), $"apsimx-unittests-{Guid.NewGuid().ToString()}");
            Environment.SetEnvironmentVariable("TMP", tempPath);
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
        }

        /// <summary>
        /// Event handler for a job runner's <see cref="JobRunner.AllCompleted"/> event.
        /// Asserts that the job ran successfully.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        public static void EnsureJobRanGreen(object sender, JobCompleteArguments args)
        {
            if (args.ExceptionThrowByJob != null)
                throw new Exception(string.Format("Exception was thrown when running via {0}, when we expected no error to be thrown.", sender.GetType().Name), args.ExceptionThrowByJob);
        }

        /// <summary>Call an event in a model</summary>
        public static void CallEvent(object model, string eventName, object[] arguments = null)
        {
            CallMethod(model, "On" + eventName, arguments);
        }

        /// <summary>Call a private method in a model</summary>
        public static object CallMethod(object model, string methodName, object[] arguments = null)
        {
            object returnValue = null;
            MethodInfo eventToInvoke = model.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (eventToInvoke != null)
            {
                if (arguments == null)
                    arguments = new object[] { model, new EventArgs() };
                returnValue = eventToInvoke.Invoke(model, arguments);
            }
            return returnValue;
        }

        /// <summary>Call a private method in a model</summary>
        public static void SetProperty(object model, string propertyName, object value)
        {
            PropertyInfo property = model.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property != null)
                property.SetValue(model, value);
        }

        /// <summary>Call an event in a model and all child models.</summary>
        public static void CallEventAll(IModel model, string eventName, object[] arguments = null)
        {
            CallEvent(model, eventName, arguments);
            foreach (IModel descendant in model.Node.FindChildren<IModel>(recurse: true))
                CallEvent(descendant, eventName, arguments);
        }

        /// <summary>ResolveLinks in a model</summary>
        public static void ResolveLinks(IModel model)
        {
            var links = new Links();
            links.Resolve(model, true, true);
        }

        /// <summary>Inject a link into a model</summary>
        public static void InjectLink(object model, string linkFieldName, object linkFieldValue)
        {
            ReflectionUtilities.SetValueOfFieldOrProperty(linkFieldName, model, linkFieldValue);
        }

        /// <summary>Convert a SQLite table to a string.</summary>
        public static DataTable GetTableFromDatabase(IDatabaseConnection database, string tableName, IEnumerable<string> fieldNames = null)
        {
            string sql = "SELECT ";
            if (fieldNames == null)
                sql += "*";
            else
            {
                bool first = true;
                foreach (string fieldName in fieldNames)
                {
                    if (first)
                        first = false;
                    else
                        sql += ",";
                    sql += fieldName;
                }
            }
            sql += " FROM [" + tableName + "]";
            var orderByFieldNames = new List<string>();
            if (database.GetColumnNames(tableName).Contains("CheckpointID"))
                orderByFieldNames.Add("[CheckpointID]");
            if (database.GetColumnNames(tableName).Contains("SimulationID"))
                orderByFieldNames.Add("[SimulationID]");
            if (database.GetColumnNames(tableName).Contains("Clock.Today"))
                orderByFieldNames.Add("[Clock.Today]");
            if (orderByFieldNames.Count > 0)
                sql += " ORDER BY " + StringUtilities.BuildString(orderByFieldNames.ToArray(), ",");
            return database.ExecuteQuery(sql);
        }

        /// <summary>
        /// Runs models.exe on the given sims and passes along the given command line arguments.
        /// Returns StdOut of Models.exe.
        /// </summary>
        /// <param name="sims">Simulations to be run.</param>
        /// <param name="arguments">Command line arguments to be passed to Models.exe.</param>
        public static string RunModels(Simulations sims, string arguments)
        {
            sims.FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            sims.Write(sims.FileName);
            string pathToModels = typeof(IModel).Assembly.Location;
            return RunModels(sims.FileName + " " + arguments);
        }

        public static string RunModels(string arguments)
        {
            return RunModels(StringUtilities.SplitStringHonouringQuotes(arguments, " ").ToArray());
        }

        public static string RunModels(string[] arguments)
        {
            TextWriter stdout = Console.Out;

            try
            {
                StringWriter output = new StringWriter();
                Console.SetOut(output);
                Console.SetError(output);
                int exitCode = Models.Program.Main(arguments);
                if (exitCode != 0)
                    throw new Exception($"Models invocation failed. Output:\n{output.ToString()}");
                return output.ToString();
            }
            finally
            {
                Console.SetOut(stdout);
            }
        }

        /// <summary>
        /// Returns a lightweight skeleton simulation which can be run.
        /// </summary>
        public static Simulations GetRunnableSim(bool useInMemoryDb = false)
        {
            Simulations sims = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new DataStore() { UseInMemoryDB = useInMemoryDb },
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2017, 1, 1),
                                EndDate = new DateTime(2017, 1, 10) // January 10
                            },
                            new Summary(),
                            new Zone()
                            {
                                Area = 1,
                                Children = new List<IModel>()
                                {
                                    new Models.Report()
                                    {
                                        VariableNames = new string[]
                                        {
                                            "[Clock].Today.DayOfYear as n"
                                        },
                                        EventNames = new string[]
                                        {
                                            "[Clock].DoReport"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var tree = Node.Create(sims);
            sims.Write(FileName: Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"));
            return sims;
        }

        /// <summary>
        /// Gets a IPlant model from the resources folder in Models.
        /// </summary>
        /// <returns></returns>
        public static T GetModelFromResource<T>(string modelName)
        {
            string modelResourcePath = Path.Combine("%root%", "Models", "Resources", $"{modelName}.json");
            string fullModelResourcePath = PathUtilities.GetAbsolutePath(modelResourcePath, null);
            Simulations sims = (Simulations)FileFormat.ReadFromFile<Simulations>(fullModelResourcePath).Model;
            T model = sims.Node.FindChild<T>();
            return model;
        }

        /// <summary>
        /// Returns a lightweight simulation which can be used for plant or other complex testing purposes.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static Simulations GetPlantTestingSimulation(bool useInMemoryDb = false)
        {
            Simulations simulations = GetRunnableSim(useInMemoryDb);
            Simulation sim = simulations.Node.FindChild<Simulation>(recurse: true);

            Zone zone = simulations.Node.FindChild<Zone>(recurse: true);
            zone.Name = "Field";
            zone.Node.AddChild(new Fertiliser
            {
                Name = "Fertiliser",
            });

            DataStore storage = simulations.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;

            // Clock setup
            Clock clock = sim.Node.FindChild<Clock>(recurse: true);
            clock.StartDate = new DateTime(2000, 1, 1);
            clock.EndDate = clock.StartDate.AddDays(1);

            // Add the standard Dalby weather file used by the Wheat example.
            sim.Node.AddChild(new Models.Climate.Weather()
            {
                FileName = PathUtilities.GetAbsolutePath(Path.Combine("%root%", "Examples", "WeatherFiles", "AU_Dalby.met"), null)
            });

            AddTestingSoil(simulations);

            // Add Wheat model.
            zone.Node.AddChild(GetModelFromResource<Plant>("Wheat"));

            // Setup SurfaceOrganicMatter model.
            var som = GetModelFromResource<SurfaceOrganicMatter>("SurfaceOrganicMatter");
            som.InitialResidueName = "wheat_stubble";
            som.InitialResidueType = "wheat";
            som.InitialResidueMass = 500;
            som.InitialStandingFraction = 0;
            som.InitialCPR = 0;
            som.InitialCNR = 100;
            zone.Node.AddChild(som);

            SetupSowingRuleManager(zone);

            return simulations;
        }


        ///<summary>Returns a Soil model that can be used for testing.</summary>
        public static void AddTestingSoil(Simulations simulations)
        {
            Zone zone = simulations.Node.FindChild<Zone>(recurse: true);
            zone.Node.AddChild(new Soil());
            var soil = zone.Node.FindChild<Soil>(recurse: true);
            soil.Node.AddChild(new Physical
            {
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                BD = [1.011, 1.071, 1.094, 1.159, 1.173, 1.163, 1.187],
                AirDry = [0.130, 0.199, 0.280, 0.280, 0.280, 0.280, 0.280],
                LL15 = [0.261, 0.248, 0.280, 0.280, 0.280, 0.280, 0.280],
                DUL = [0.521, 0.497, 0.488, 0.480, 0.472, 0.457, 0.452],
                SAT = [0.589, 0.566, 0.557, 0.533, 0.527, 0.531, 0.522],
            });

            var physical = soil.Node.FindChild<Physical>(recurse: true);
            physical.Node.AddChild(new SoilCrop
            {
                Name = "WheatSoil",
                KL = [0.060, 0.060, 0.060, 0.040, 0.040, 0.020, 0.010],
                LL = [0.261, 0.248, 0.280, 0.306, 0.360, 0.392, 0.446]
            });

            soil.Node.AddChild(new Water
            {
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                InitialValues = [0.313, 0.298, 0.322, 0.320, 0.318, 0.315, 0.314],
            });

            soil.Node.AddChild(new Organic
            {
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                Carbon = [1.2, 0.96, 0.6, 0.3, 0.18, 0.12, 0.12],
                SoilCNRatio = [12, 12, 12, 12, 12, 12, 12],
                FBiom = [0.04, 0.02, 0.2, 0.2, 0.1, 0.1, 0.1],
                FInert = [0.4, 0.6, 0.8, 1.0, 1.0, 1.0, 1.0],
                FOM = [347.1, 270.3, 164.0, 99.5, 60.3, 36.6, 22.2],
            });

            soil.Node.AddChild(new Solute
            {
                Name = "NO3",
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                InitialValues = [1, 1, 1, 1, 1, 1, 1], // Make these values match the Wheat example
                InitialValuesUnits = Solute.UnitsEnum.ppm
            });

            soil.Node.AddChild(new Solute
            {
                Name = "NH4",
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                InitialValues = [0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1],
                InitialValuesUnits = Solute.UnitsEnum.ppm
            });

            soil.Node.AddChild(new Solute
            {
                Name = "Urea",
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                InitialValues = [0, 0, 0, 0, 0, 0, 0],
                InitialValuesUnits = Solute.UnitsEnum.ppm
            });

            CreateAndSetupWaterBalance(soil, physical);

            soil.Node.AddChild(GetModelFromResource<Nutrient>("Nutrient"));
            soil.Node.AddChild(new Chemical
            {
                Thickness = [150, 150, 300, 300, 300, 300, 300],
                PH = [8.0, 8.0, 8.0, 8.0, 8.0, 8.0, 8.0],
            });
            soil.Node.AddChild(new SoilTemperature());
        }

        /// <summary>
        /// Creates and adds a WaterBalance model to the provided soil, and sets it up with standard parameters based
        /// on the Wheat example.
        /// </summary>
        /// <param name="soil"></param>
        /// <param name="physical"></param>
        private static void CreateAndSetupWaterBalance(Soil soil, Physical physical)
        {
            soil.Node.AddChild(GetModelFromResource<WaterBalance>("WaterBalance"));
            var waterBalance = soil.Node.FindChild<WaterBalance>(recurse: true);
            waterBalance.Depth = physical.Depth;
            waterBalance.SWCON = [0.300, 0.300, 0.300, 0.300, 0.300, 0.300, 0.300];
            waterBalance.SummerU = 5;
            waterBalance.SummerCona = 5;
            waterBalance.WinterU = 5;
            waterBalance.WinterCona = 5;
            waterBalance.DiffusConst = 40;
            waterBalance.DiffusSlope = 16;
            waterBalance.Salb = 0.12;
            waterBalance.CN2Bare = 73;
            waterBalance.CNRed = 20;
            waterBalance.CNCov = 0.8;
            waterBalance.DischargeWidth = 5;
            waterBalance.CatchmentArea = 10;
            waterBalance.PSIDul = -100;
        }

        public static Simulations GetSimpleExperiment()
        {
            return ReadFromResource<Simulations>("UnitTests.Resources.SimpleExperiment.apsimx", e => throw e);
        }

        public static T ReadFromResource<T>(string resourceName, Action<Exception> errorHandler) where T : IModel
        {
            string json = ReflectionUtilities.GetResourceAsString(resourceName);
            return (T)FileFormat.ReadFromString<Simulations>(json, errorHandler, false).Model;
        }

        public static DataTable CreateTable(IEnumerable<string> columnNames, IEnumerable<object[]> rows)
        {
            var data2 = new DataTable();
            foreach (var columnName in columnNames)
                data2.Columns.Add(columnName);

            foreach (var row in rows)
            {
                var newRow = data2.NewRow();
                newRow.ItemArray = row;
                data2.Rows.Add(newRow);
            }
            return data2;
        }

        public static bool IsSame(this DataTable t1, DataTable t2)
        {
            if (t1 == null)
                return false;
            if (t2 == null)
                return false;
            if (t1.Rows.Count != t2.Rows.Count)
                return false;

            if (t1.Columns.Count != t2.Columns.Count)
                return false;

            if (t1.Columns.Cast<DataColumn>().Any(dc => !t2.Columns.Contains(dc.ColumnName)))
            {
                return false;
            }

            for (int i = 0; i <= t1.Rows.Count - 1; i++)
            {
                if (t1.Columns.Cast<DataColumn>().Any(dc1 => t1.Rows[i][dc1.ColumnName].ToString() != t2.Rows[i][dc1.ColumnName].ToString()))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Setup the manager for sowing in the provided Zone model.
        /// </summary>
        /// <param name="zone">The Zone model to setup the sowing manager for.</param>
        private static void SetupSowingRuleManager(Zone zone)
        {
            // Setup the manager for sowing.
            zone.Node.AddChild(new Manager()
            {
                Name = "SowingRule",
                Enabled = true,
                Code = """
                using APSIM.Numerics;
                using Models.Climate;
                using System.Linq;
                using System;
                using Models.Core;
                using Models.PMF;
                using Models.Soils;
                using Models.Utilities;
                using APSIM.Shared.Utilities;
                using Models.Interfaces;

                namespace Models
                    {
                        [Serializable]
                        public class Script : Model
                        {
                            [Link] Clock Clock;

                            [Description("Crop")]
                            public IPlant Crop { get; set; }

                            [Description("Start of sowing window (d-mmm)")]
                            public string StartDate { get; set; }

                            [Description("End of sowing window (d-mmm)")]
                            public string EndDate { get; set; }

                            [EventSubscribe("DoManagement")]
                            private void OnDoManagement(object sender, EventArgs e)
                            {
                                if (Crop.IsAlive)
                                    return;
                                if (DateUtilities.WithinDates(StartDate, Clock.Today, EndDate))
                                {
                                    Crop.Sow("Hartog", 120, 30, 250);
                                }
                            }
                        }
                    }
                """
            });
            var manager = zone.Node.FindChild<Manager>(name: "SowingRule", recurse: true);
            manager.RebuildScriptModel();

        }
    }
}
