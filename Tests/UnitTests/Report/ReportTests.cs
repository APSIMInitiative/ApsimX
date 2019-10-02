namespace UnitTests.Report
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Report;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using UnitTests.Core;
    using UnitTests.Storage;

    [TestFixture]
    public class ReportTests
    {
        /// <summary>
        /// Template simulations object used to run tests in this class.
        /// </summary>
        private Simulations sims;

        [SetUp]
        public void InitSimulations()
        {
            sims = new Simulations()
            {
                FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"),
                Children = new List<Model>()
                {
                    new DataStore() { Name = "DataStore" },
                    new Simulation()
                    {
                        Name = "Simulation",
                        Children = new List<Model>()
                        {
                            new Clock()
                            {
                                Name = "Clock",
                                StartDate = new DateTime(2017, 1, 1),
                                EndDate = new DateTime(2017, 1, 10) // January 10
                            },
                            new Summary() { Name = "Summary" },
                            new Zone()
                            {
                                Name = "Zone",
                                Area = 1,
                                Children = new List<Model>()
                                {
                                    new Report()
                                    {
                                        Name = "Report",
                                        VariableNames = new string[]
                                        {
                                            "[Clock].Today.DayOfYear as n",
                                            "sum of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as TriangularNumbers",
                                            "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as test",
                                            "[Clock].Today.Year as Year",
                                            "sum of [Clock].Today.DayOfYear from 1-Jan to 31-Dec as SigmaDay",
                                            "sum of [Clock].Today.DayOfYear from 1-Jan to 9-Jan as HardCoded"
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
        }

        /// <summary>
        /// This test ensures that aggregation to and from variable dates (ie [Clock].Today) works.
        /// </summary>
        [Test]
        public void TestVariableAggregation()
        {
            Simulations file = RunResource("UnitTests.Report.ReportAggregation.apsimx");
            
            var storage = Apsim.Find(file, typeof(IDataStore)) as IDataStore;
            List<string> fieldNames = new List<string>() { "sum", "avg", "min", "max", "first", "last", "diff" };
            DataTable data = storage.Reader.GetData("Report", fieldNames: fieldNames);

            // We are aggregating from last report date. Therefore on the first report date, the value
            // will be null, hence the nasty nullable types here. Additionally, all numbers in DB are
            // stored as doubles, so a simple cast to double? will work for avg (which is a double).
            // For the other variables we will need an explicit conversion.
            List<int?> sum = data.AsEnumerable().Select(x => ParseNullableInt(x["sum"])).ToList();
            List<double?> avg = data.AsEnumerable().Select(x => x["avg"] as double?).ToList();
            List<int?> min = data.AsEnumerable().Select(x => ParseNullableInt(x["min"])).ToList();
            List<int?> max = data.AsEnumerable().Select(x => ParseNullableInt(x["max"])).ToList();
            List<int?> first = data.AsEnumerable().Select(x => ParseNullableInt(x["first"])).ToList();
            List<int?> last = data.AsEnumerable().Select(x => ParseNullableInt(x["last"])).ToList();
            List<int?> diff = data.AsEnumerable().Select(x => ParseNullableInt(x["diff"])).ToList();

            List<int?> expectedSum = new List<int?>()       { null, 63, 112, 161, 210, 259, 308, 357 };
            List<double?> expectedAvg = new List<double?>() { null, 9,  16,  23,  30,  37,  44,  51 };
            List<int?> expectedMin = new List<int?>()       { null, 6,  13,  20,  27,  34,  41,  48 }; // == expectedFirst
            List<int?> expectedMax = new List<int?>()       { null, 12, 19,  26,  33,  40,  47,  54 }; // == expectedLast
            List<int?> expectedDiff = new List<int?>()      { null, 6,  6,   6,   6,   6,   6,   6 };

            Assert.AreEqual(expectedSum, sum);
            Assert.AreEqual(expectedAvg, avg);
            Assert.AreEqual(expectedMin, min);
            Assert.AreEqual(expectedMax, max);
            Assert.AreEqual(expectedMin, first);
            Assert.AreEqual(expectedMax, last);
            Assert.AreEqual(expectedDiff, diff);
        }

        /// <summary>
        /// This test reproduces a bug where aggregation to [Clock].Today doesn't work, due to
        /// [Clock].Today being evaluated before the simulation starts.
        /// </summary>
        [Test]
        public void EnsureAggregationWorks()
        {
            Clock clock = Apsim.Find(sims, typeof(Clock)) as Clock;
            clock.StartDate = new DateTime(2017, 1, 1);
            clock.EndDate = new DateTime(2017, 1, 10);

            // To test aggregation to [Clock].Today, we generate the first 10
            // triangular numbers by summing [Clock].Today over the first 10 days of the year.
            List<int> triangularNumbers = new List<int>() { 1, 3, 6, 10, 15, 21, 28, 36, 45, 55 };

            // To test aggregation to/from events, we sum day of year from start of week to end of week.
            // The simulation starts in 2017 January 1, which is a Sunday (start of week).
            List<int> weeklyNumbers = new List<int>() { 1, 3, 6, 10, 15, 21, 28, 8, 17, 27 };

            // To test aggregation to/from hardcoded date in the format dd-mmm, we sum day of year
            // from 1-Jan to 9-Jan. The simulation stops on 10-Jan, so the last two numbers should
            // be the same.
            List<int> hardCoded = new List<int>() { 1, 3, 6, 10, 15, 21, 28, 36, 45, 45 };

            var runner = new Runner(sims);
            runner.Run();

            var storage = sims.Children[0] as IDataStore;
            DataTable data = storage.Reader.GetData("Report", fieldNames: new List<string>() { "n", "TriangularNumbers", "test", "HardCoded" });
            List<int> predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["TriangularNumbers"], CultureInfo.InvariantCulture)).ToList();
            Assert.AreEqual(triangularNumbers, predicted, "Error in report aggregation involving [Clock].Today");

            predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["test"], CultureInfo.InvariantCulture)).ToList();
            Assert.AreEqual(weeklyNumbers, predicted);

            predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["HardCoded"], CultureInfo.InvariantCulture)).ToList();
            Assert.AreEqual(hardCoded, predicted);
        }

        /// <summary>
        /// This test ensures that we can aggregate a variable from two hardcoded dates, over the year boundary.
        /// ie from 25-Dec to 5-Jan.
        /// </summary>
        [Test]
        public void TestAggregationOverYearBoundary()
        {
            Simulations file = RunResource("UnitTests.Report.ReportAggregationOverYear.apsimx");
            IDataStore storage = Apsim.Find(file, typeof(IDataStore)) as IDataStore;

            string[] fieldNames = new string[] { "difference" };
            DataTable data = storage.Reader.GetData("Report", fieldNames: fieldNames);

            double?[] values = GetColumnOfNullableDoubles(data, "difference");
            double?[] expected = new double?[] { null, null, null, null, null, 0, 1, 2, 3, 4, 5, 6, -358, -357, -356, -355, -354, -354, -354, -354, -354, -354 };

            Assert.AreEqual(expected, values);
        }

        /// <summary>
        /// This test reproduces a bug where aggregation from 1-Jan to 31-Dec doesn't work properly;
        /// values don't reset after 31-dec, they instead continue aggregating.
        /// </summary>
        [Test]
        public void EnsureYearlyAggregationWorks()
        {
            Clock clock = Apsim.Find(sims, typeof(Clock)) as Clock;
            clock.StartDate = new DateTime(2017, 1, 1);
            clock.EndDate = new DateTime(2019, 1, 1);

            var runner = new Runner(sims);
            runner.Run();

            var storage = sims.Children[0] as IDataStore;
            DataTable data = storage.Reader.GetData("Report", fieldNames: new List<string>() { "Year", "SigmaDay" });
            int finalValFirstYear = int.Parse(data.AsEnumerable().Where(x => int.Parse(x["Year"].ToString()) == 2017).Select(x => x["SigmaDay"]).Last().ToString());
            int firstValSecondYear = int.Parse(data.AsEnumerable().Where(x => int.Parse(x["Year"].ToString()) == 2018).Select(x => x["SigmaDay"]).First().ToString());
            Assert.That(finalValFirstYear > firstValSecondYear, $"Error: Report aggregation from 01-Jan to 31-Dec did not reset after the end date. Final value in first year: {finalValFirstYear}, first value in second year: {firstValSecondYear}");
        }

        [Test]
        public void FactorsTableIsWritten()
        {
            // When report gets an oncommencing it should write a _Factors table to storage.

            var sim = new Simulation();
            sim.Descriptors = new List<SimulationDescription.Descriptor>();
            sim.Descriptors.Add(new SimulationDescription.Descriptor("Experiment", "exp1"));
            sim.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", "sim1"));
            sim.Descriptors.Add(new SimulationDescription.Descriptor("FolderName", "F"));
            sim.Descriptors.Add(new SimulationDescription.Descriptor("Zone", "z"));
            sim.Descriptors.Add(new SimulationDescription.Descriptor("Cultivar", "cult1"));
            sim.Descriptors.Add(new SimulationDescription.Descriptor("N", "0"));

            var report = new Report()
            {
                VariableNames = new string[0],
                EventNames = new string[0]
            };
            Utilities.InjectLink(report, "simulation", sim);
            Utilities.InjectLink(report, "locator", new MockLocator());
            Utilities.InjectLink(report, "storage", new MockStorage());
            Utilities.InjectLink(report, "clock", new MockClock());

            var events = new Events(report);
            events.Publish("StartOfSimulation", new object[] { report, new EventArgs() });

            Assert.AreEqual(MockStorage.tables[0].TableName, "_Factors");
            Assert.AreEqual(Utilities.TableToString(MockStorage.tables[0]),
               "ExperimentName,SimulationName,FolderName,FactorName,FactorValue\r\n" +
               "          exp1,          sim1,         F,  Cultivar,      cult1\r\n" +
               "          exp1,          sim1,         F,         N,          0\r\n");
        }

        /// <summary>
        /// Reads an .apsimx file from an embedded resource, runs it,
        /// and returns the root simulations node.
        /// </summary>
        /// <param name="resourceName">Name of the .apsimx file resource.</param>
        private static Simulations RunResource(string resourceName)
        {
            string json = ReflectionUtilities.GetResourceAsString(resourceName);
            Simulations file = FileFormat.ReadFromString<Simulations>(json, out List<Exception> fileErrors);
            if (fileErrors != null && fileErrors.Count > 0)
                throw fileErrors[0];

            var Runner = new Runner(file);
            Runner.Run();

            return file;
        }

        /// <summary>
        /// Gets a single column of data from a data table. Returns it as an array of nullable ints.
        /// </summary>
        /// <param name="table">Table containing the data.</param>
        /// <param name="columnName">Column name of the data to be fetched.</param>
        private static int?[] GetColumnOfNullableInts(DataTable table, string columnName)
        {
            return table.AsEnumerable().Select(r => ParseNullableInt(r[columnName])).ToArray();
        }

        /// <summary>
        /// Gets a single column of data from a data table. Returns it as an array of nullable doubles.
        /// </summary>
        /// <param name="table">Table containing the data.</param>
        /// <param name="columnName">Column name of the data to be fetched.</param>
        private static double?[] GetColumnOfNullableDoubles(DataTable table, string columnName)
        {
            return table.AsEnumerable().Select(r => ParseNullableDouble(r[columnName])).ToArray();
        }

        /// <summary>
        /// Parses an object to a nullable int.
        /// </summary>
        /// <param name="input">Input object.</param>
        private static int? ParseNullableInt(object input)
        {
            if (int.TryParse(input?.ToString(), out int result))
                return result;

            return null;
        }

        /// <summary>
        /// Parses an object to a nullable double.
        /// </summary>
        /// <param name="input">Input object.</param>
        private static double? ParseNullableDouble(object input)
        {
            if (double.TryParse(input?.ToString(), out double result))
                return result;

            return null;
        }
    }
}