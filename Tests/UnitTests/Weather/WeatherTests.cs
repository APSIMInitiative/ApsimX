using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UnitTests.Weather
{
    /// <summary>
    /// Tests for weather files.
    /// </summary>
    class WeatherFileTests
    {
        /// <summary>
        /// Tests a simple weather file in .xlsx (Excel) format.
        /// </summary>
        [Test]
        public void ExcelWeatherFileTest()
        {
            string weatherFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".xlsx");
            using (FileStream file = new FileStream(weatherFilePath, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("UnitTests.Weather.WeatherTestsExcelFile.xlsx").CopyTo(file);
            }

            Simulation baseSim = new Simulation()
            {
                Name = "Base",
                Children = new List<IModel>()
                {
                    new Models.Climate.Weather()
                    {
                        Name = "Weather",
                        FullFileName = weatherFilePath,
                        ExcelWorkSheetName = "Sheet1"
                    },
                    new Clock()
                    {
                        Name = "Clock",
                        StartDate = new DateTime(1998, 11, 9),
                        EndDate = new DateTime(1998, 11, 12)
                    },
                    new MockSummary()
                }
            };

            baseSim.Prepare();
            baseSim.Run();
            Assert.AreEqual(MockSummary.messages[0], "Simulation terminated normally");
        }

        [Test]
        public void TestCustomMetData()
        {
            // Open an in-memory database.
            IDatabaseConnection database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);

            string weatherData = ReflectionUtilities.GetResourceAsString("UnitTests.Weather.CustomMetData.met");
            string metFile = Path.GetTempFileName();
            File.WriteAllText(metFile, weatherData);
            try
            {

                Simulation sim = new Simulation()
                {
                    Children = new List<IModel>()
                    {
                        new Clock(),
                        new MockSummary(),
                        new Models.Climate.Weather()
                        {
                            FullFileName = metFile
                        },
                        new Models.Report()
                        {
                            VariableNames = new[]
                            {
                                "[Manager].Script.MyColumn as x"
                            },
                            EventNames = new[]
                            {
                                "[Clock].DoReport"
                            }
                        },
                        new Manager()
                        {
                            Code = "using System;\nusing Models.Core;\nusing Models.Climate;\n\nnamespace Models\n{\n    [Serializable]\n    public class Script : Model\n    {\n        [Link] private Weather weather;\n        \n        public double MyColumn\n        {\n        \tget\n        \t{\n        \t\treturn weather.GetValue(\"my_column_name\");\n        \t}\n        }\n    }\n}\n"
                        }
                    }
                };

                Simulations sims = new Simulations()
                {
                    Children = new List<IModel>()
                    {
                        new DataStore(database),
                        sim
                    }
                };

                // Run simulations.
                Runner runner = new Runner(sims);
                List<Exception> errors = runner.Run();
                Assert.NotNull(errors);
                if (errors.Count != 0)
                    throw new AggregateException(errors);

                int[] rawData = new int[] { 6, 7, 2, 3, 4 };
                List<object[]> rowData = rawData.Select(x => new object[] { x }).ToList();
                DataTable expected = Utilities.CreateTable(new string[] { "x" }, rowData);
                Assert.IsTrue(
                    expected
                .IsSame(database.ExecuteQuery("SELECT [x] FROM Report")));
            }
            finally
            {
                database.CloseDatabase();
                File.Delete(metFile);
            }
        }
    }
}
