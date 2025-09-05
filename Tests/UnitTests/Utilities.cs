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
using UnitTests.Weather;
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
            model.ParentAllDescendants();
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
        public static Simulations GetRunnableSimForPlantTesting(bool useInMemoryDb = false)
        {
            Simulations simulations = GetRunnableSim(useInMemoryDb);
            Simulation sim = simulations.Node.FindChild<Simulation>(recurse: true);
            Zone zone = simulations.Node.FindChild<Zone>(recurse: true);

            sim.Node.AddChild(new MockWeather());

            AddTestingSoil(simulations);

            zone.Node.AddChild(GetModelFromResource<Plant>("Wheat"));

            // Values taken from Wheat example file.
            // zone.Node.AddChild(new SurfaceOrganicMatter()
            // {
            //     InitialResidueName = "wheat_stubble",
            //     InitialResidueType = "wheat",
            //     InitialResidueMass = 500,
            //     InitialStandingFraction = 0,
            //     InitialCPR = 0,
            //     InitialCNR = 100,
            // });
            zone.Node.AddChild(GetModelFromResource<SurfaceOrganicMatter>("SurfaceOrganicMatter"));
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
                Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                BD = new double[] { 1.011, 1.071, 1.094, 1.159, 1.173, 1.163, 1.187 },
                AirDry = new double[] { 0.130, 0.199, 0.280, 0.280, 0.280, 0.280, 0.280 },
                LL15 = new double[] { 0.261, 0.248, 0.280, 0.280, 0.280, 0.280, 0.280 },
                DUL = new double[] { 0.521, 0.497, 0.488, 0.480, 0.472, 0.457, 0.452 },
                SAT = new double[] { 0.589, 0.566, 0.557, 0.533, 0.527, 0.531, 0.522 },
            });

            var physical = soil.Node.FindChild<Physical>(recurse: true);
            physical.Node.AddChild(new SoilCrop
            {
                Name = "Wheat",
                KL = new double[] { 0.060, 0.060, 0.060, 0.040, 0.040, 0.020, 0.010 },
                LL = new double[] { 0.261, 0.248, 0.280, 0.306, 0.360, 0.392, 0.446 }
            });

            soil.Node.AddChild(new Water
            {
                Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                InitialValues = new double[] { 0.313, 0.298, 0.322, 0.320, 0.318, 0.315, 0.314 },
            });

            soil.Node.AddChild(new Organic
            {
                Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                Carbon = new double[] { 2, 1, 0.5, 0.4, 0.3, 0.2, 0.2 }
            });

            soil.Node.AddChild(new Solute
            {
                Name = "NO3",
                Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                InitialValues = new double[] { 1, 1, 1, 1, 1, 1, 1 }, // Make these values match the Wheat example
                InitialValuesUnits = Solute.UnitsEnum.ppm
            });

            // TODO: add NH4 and Urea solutes.
            soil.Node.AddChild(new Solute
            {
                Name = "NH4",
                Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                InitialValues = new double[] { 0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1 },
                InitialValuesUnits = Solute.UnitsEnum.ppm
            });

            soil.Node.AddChild(new Solute
            {
                Name = "Urea",
                Thickness = new double[] { 150, 150, 300, 300, 300, 300, 300 },
                InitialValues = new double[] { 0, 0, 0, 0, 0, 0, 0 },
                InitialValuesUnits = Solute.UnitsEnum.ppm
            });

            soil.Node.AddChild(GetModelFromResource<WaterBalance>("WaterBalance"));
            soil.Node.AddChild(GetModelFromResource<Nutrient>("Nutrient")); 
            soil.Node.AddChild(new Chemical());
            soil.Node.AddChild(new SoilTemperature());
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
    }
}
