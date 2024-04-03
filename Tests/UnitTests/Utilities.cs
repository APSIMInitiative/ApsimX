using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Storage;
using NUnit.Framework;

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
        /// Parent all children of 'model' and call 'OnCreated' in each child.
        /// </summary>
        /// <param name="model">The model to parent</param>
        public static void InitialiseModel(IModel model)
        {
            model.ParentAllDescendants();
            model.OnCreated();
            foreach (var child in model.FindAllDescendants())
                child.OnCreated();
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
            foreach (IModel descendant in model.FindAllDescendants())
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
            return RunModels(StringUtilities.SplitStringHonouringQuotes(arguments, " ").Cast<string>().ToArray());
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
                FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"),
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
            sims.ParentAllDescendants();
            sims.Write(sims.FileName);
            return sims;
        }

        public static Simulations GetSimpleExperiment()
        {
            return ReadFromResource<Simulations>("UnitTests.Resources.SimpleExperiment.apsimx", e => throw e);
        }

        public static T ReadFromResource<T>(string resourceName, Action<Exception> errorHandler) where T : IModel
        {
            string json = ReflectionUtilities.GetResourceAsString(resourceName);
            return (T)FileFormat.ReadFromString<T>(json, errorHandler, false).NewModel;
        }

        /// <summary>
        /// Call OnCreated in a model and all child models.
        /// </summary>
        /// <param name="model"></param>
        public static void CallOnCreated(IModel model)
        {
            model.OnCreated();
            foreach (var child in model.Children)
                CallOnCreated(child);
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
