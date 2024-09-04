namespace UnitTests.Report
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using UnitTests.Storage;
    using UnitTests.Weather;

    [TestFixture]
    public class ReportTests
    {
        private Simulations simulations;
        private Simulation simulation;
        private IClock clock;
        private Report report;
        private MockStorage storage;
        private MockSummary summary;
        private Runner runner;

        /// <summary>
        /// Creates a simulation and links to various models. Used by all tests.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new MockStorage(),
                            new MockSummary(),
                            new Clock()
                            {
                                StartDate = new DateTime(2017, 1, 1),
                                EndDate = new DateTime(2017, 1, 10)
                            },
                            new Report()
                            {
                                VariableNames = new string[] { },
                                EventNames = new string[] { "[Clock].EndOfDay" },
                            }
                        }
                    }
                }
            };

            Utilities.InitialiseModel(simulations);
            simulation = simulations.Children[0] as Simulation;
            runner = new Runner(simulation);
            storage = simulation.Children[0] as MockStorage;
            summary = simulation.Children[1] as MockSummary;
            clock = simulation.Children[2] as Clock;
            report = simulation.Children[3] as Report;
        }

        /// <summary>
        /// Ensure we can reference another report variabel in a report calculation.
        /// </summary>
        [Test]
        public void ReferenceAnotherReportVariable()
        {
            report.VariableNames = new string[]
            {
                "[Clock].Today.DayOfYear as n",
                "2 * n as 2n"
            };
            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];
            double[] actual = storage.Get<double>("2n");
            double[] expected = new double[10] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

            Assert.That(actual, Is.EqualTo(expected));
        }

        /// <summary>
        /// Ensures that multiple components that expose the same variables are reported correctly
        ///
        /// </summary>
        [Test]
        public void TestMultipleChildren()
        {
            var m1 = new Manager()
            {
                Name = "Manager1",
                Code = "using System;\r\nusing Models.Core;\r\nnamespace Models\r\n{\r\n[Serializable]\r\n" +
                            "public class Script1 : Model\r\n {\r\n " +
                            "public double A { get { return (1); } set { } }\r\n" +
                            "public double B { get { return (2); } set { } }\r\n }\r\n}\r\n"
            };
            var m2 = new Manager()
            {
                Name = "Manager2",
                Code = "using System;\r\nusing Models.Core;\r\nnamespace Models\r\n{\r\n[Serializable]\r\n" + "" +
                            "    public class Script2 : Model\r\n {\r\n" +
                            " public double A { get { return (3); } set { } }\r\n" +
                            " public double B { get { return (4); } set { } }\r\n }\r\n}\r\n"
            };
            report.VariableNames = new[]
            {
                "[Manager1].Script1.A as M1A",
                "[Manager2].Script2.A as M2A"
            };
            report.EventNames = new[]
            {
                "[Clock].DoReport"
            };
            simulation.Children.AddRange(new[] { m1, m2 });
            simulation.ParentAllDescendants();
            m1.OnCreated();
            m2.OnCreated();

            var runners = new[]
            {
                new Runner(simulation, runType: Runner.RunTypeEnum.MultiThreaded),
            };
            foreach (Runner runner in runners)
            {
                List<Exception> errors = runner.Run();
                if (errors != null && errors.Count > 0)
                    throw errors[0];

                double[] actual = storage.Get<double>("M1A");
                double[] expected = storage.Get<double>("M2A");
                Assert.That(actual, Is.Not.EqualTo(expected));
            }
        }

        /// <summary>
        /// This test ensures that aggregation to and from variable dates (ie [Clock].Today) works.
        /// This test reproduces a bug where aggregation to [Clock].Today doesn't work, due to
        /// [Clock].Today being evaluated before the simulation starts.
        /// </summary>
        [Test]
        public void TestAllStatsBetweenVariableDates()
        {
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as sum",
                "mean of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as mean",
                "min of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as min",
                "max of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as max",
                "first of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as first",
                "last of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as last",
                "diff of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as diff"
            };

            runner.Run();
            Assert.That(storage.Get<double>("sum"), Is.EqualTo(
                            new double[] { 1, 3, 6, 10, 15, 21, 28, 36, 45, 55 }));
            Assert.That(storage.Get<double>("mean"), Is.EqualTo(
                            new double[] { 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5, 5.5 }));
            Assert.That(storage.Get<double>("min"), Is.EqualTo(
                            new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
            Assert.That(storage.Get<double>("max"), Is.EqualTo(
                            new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            Assert.That(storage.Get<double>("first"), Is.EqualTo(
                            new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
            Assert.That(storage.Get<double>("last"), Is.EqualTo(
                            new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            Assert.That(storage.Get<double>("diff"), Is.EqualTo(
                            new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }

        /// <summary>This test ensures weekly aggregation works with daily reporting frequency.</summary>
        [Test]
        public void EnsureWeeklyAggregationWithDailyOutputWorks()
        {
            clock.EndDate = new DateTime(2017, 1, 15);
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as weekly",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("weekly"), Is.EqualTo(
                            new double[] { 1, 3, 6, 10, 15, 21, 28, 8, 17, 27, 38, 50, 63, 77, 15 }));
        }

        /// <summary>This test ensures the 'on' keyword works.</summary>
        [Test]
        public void EnsureOnKeywordWorks()
        {
            clock.EndDate = new DateTime(2017, 1, 31);
            report.EventNames = new string[]
            {
                "[Clock].EndOfMonth"
            };
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartOfSimulation to [Clock].EndOfSimulation as totalDoy1",
                "sum of [Clock].Today.DayOfYear on [Clock].EndOfWeek from [Clock].StartOfSimulation to [Clock].EndOfSimulation as totalDoy2",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("totalDoy1"), Is.EqualTo(new double[] { 496 }));
            Assert.That(storage.Get<double>("totalDoy2"), Is.EqualTo(new double[] { 70 }));
        }

        /// <summary>This test ensures an expression with spaces works.</summary>
        [Test]
        public void EnsureExpressionWorks()
        {
            report.VariableNames = new string[]
            {
                "sum of ([Clock].Today.DayOfYear + 1) from [Clock].StartOfSimulation to [Clock].EndOfSimulation as totalDoy",
            };
            report.EventNames = new string[]
            {
                "[Clock].EndOfSimulation"
            };
            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("totalDoy"), Is.EqualTo(new double[] { 65 }));
        }

        /// <summary>This test ensures weekly aggregation works with weekly reporting frequency.</summary>
        [Test]
        public void EnsureWeeklyAggregationWithWeeklyOutputWorks()
        {
            clock.EndDate = new DateTime(2017, 1, 15);
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as weekly",
            };
            report.EventNames = new string[]
            {
                "[Clock].EndOfWeek",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("weekly"), Is.EqualTo(new double[] { 28, 77 }));
        }

        /// <summary>This test ensures weekly aggregation works with monthly reporting frequency.</summary>
        [Test]
        public void EnsureWeeklyAggregationWithMonthlyOutputWorks()
        {
            clock.EndDate = new DateTime(2017, 2, 28);
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as weekly",
            };
            report.EventNames = new string[]
            {
                "[Clock].EndOfMonth",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("weekly"), Is.EqualTo(new double[] { 90, 174 }));
        }

        /// <summary>This test ensures weekly aggregation works with yearly reporting frequency.</summary>
        [Test]
        public void EnsureWeeklyAggregationWithYearlyOutputWorks()
        {
            clock.EndDate = new DateTime(2018, 12, 31);
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as weekly",
            };
            report.EventNames = new string[]
            {
                "[Clock].EndOfYear",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("weekly"), Is.EqualTo(new double[] { 365, 729 }));
        }

        /// <summary>This test ensures DayAfterLastOutput aggregation works with daily reporting frequency.</summary>
        [Test]
        public void EnsureDayAfterLastOutputAggregationWithDailyOutputWorks()
        {
            clock.EndDate = new DateTime(2017, 1, 15);
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Report].DayAfterLastOutput to [Clock].Today as values",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("values"), Is.EqualTo(
                            new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }));
        }

        /// <summary>This test ensures DayAfterLastOutput aggregation works with weekly reporting frequency.</summary>
        [Test]
        public void EnsureDayAfterLastOutputAggregationWithWeeklyOutputWorks()
        {
            clock.EndDate = new DateTime(2017, 1, 15);
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Report].DayAfterLastOutput to [Clock].Today as weekly",
            };
            report.EventNames = new string[]
            {
                "[Clock].EndOfWeek",
            };

            // Run the simulation.
            runner.Run();

            // Should be the same as test EnsureWeeklyAggregationWithWeeklyOutputWorks above
            Assert.That(storage.Get<double>("weekly"), Is.EqualTo(new double[] { 28, 77 }));
        }

        /// <summary>
        /// This test ensures that we can aggregate a variable from two hardcoded dates, over the year boundary.
        /// ie from 25-Dec to 5-Jan.
        /// </summary>
        [Test]
        public void TestAggregationOverYearBoundary()
        {
            clock.StartDate = new DateTime(2018, 12, 20);
            clock.EndDate = new DateTime(2019, 1, 10);
            report.VariableNames = new string[]
            {
                "diff of [Clock].Today.DayOfYear from 25-Dec to 5-Jan as difference",
            };

            // Run the simulation.
            runner.Run();

            double[] expected = new double[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, 0, 1, 2, 3, 4, 5, 6, -358, -357, -356, -355, -354, -354, -354, -354, -354, -354 };

            Assert.That(storage.Get<double>("difference"), Is.EqualTo(expected));
        }

        /// <summary>
        /// This test reproduces a bug where aggregation from 1-Jan to 31-Dec doesn't work properly;
        /// values don't reset after 31-dec, they instead continue aggregating.
        /// </summary>
        [Test]
        public void EnsureYearlyAggregationWorks()
        {
            clock.StartDate = new DateTime(2017, 1, 1);
            clock.EndDate = new DateTime(2018, 1, 1);

            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from 1-Jan to 31-Dec as SigmaDay"
            };

            runner.Run();

            var values = storage.Get<double>("SigmaDay").ToList();
            Assert.That(values.Last(), Is.EqualTo(1));
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

            SQLite database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);
            DataStore storage = new DataStore(database);

            Simulations sims = new Simulations();
            sims.Children.Add(sim);
            sims.Children.Add(new Summary());
            sims.Children.Add(storage);
            sims.ParentAllDescendants();

            sim.Prepare();

            storage.Writer.WaitForIdle();
            storage.Reader.Refresh();
            
            Assert.That(storage.Reader.GetData("_Factors"), Is.Not.Null);

            DataTable dtExpected = Utilities.CreateTable(new string[]                      { "CheckpointName", "CheckpointID", "SimulationName", "SimulationID", "ExperimentName", "FolderName", "FactorName", "FactorValue" },
                                                    new List<object[]> { new object[] {        "Current",             1,        "",          1,          "exp1",          "F",         "Cultivar",      "cult1"   },
                                                                         new object[] {        "Current",             1,        "",          1,          "exp1",          "F",             "N",            0      } });
            DataTable dtActual = storage.Reader.GetData("_Factors");

            Assert.That(dtExpected.IsSame(dtActual), Is.True);
        }

        /// <summary>
        /// Reports DayOfYear as doy in multiple reports. Each
        /// report has a different reporting frequency:
        ///
        /// [Fertiliser].Fertilised
        /// [Irrigation].Irrigated
        /// </summary>
        [Test]
        public static void TestReportingOnModelEvents()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Report.ReportOnEvents.apsimx");
            Simulations file = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            // This simulation needs a weather node, but using a legit
            // met component will just slow down the test.
            IModel sim = file.FindInScope<Simulation>();
            Model weather = new MockWeather();
            sim.Children.Add(weather);
            weather.Parent = sim;

            // Run the file.
            var Runner = new Runner(file);
            Runner.Run();

            // Check that the report reported on the correct dates.
            var storage = file.FindInScope<IDataStore>();
            List<string> fieldNames = new List<string>() { "doy" };

            DataTable data = storage.Reader.GetData("ReportOnFertilisation", fieldNames: fieldNames);
            double[] values = DataTableUtilities.GetColumnAsDoubles(data, "doy", CultureInfo.InvariantCulture);
            double[] expected = new double[] { 1, 32, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 364 };
            Assert.That(values, Is.EqualTo(expected));

            data = storage.Reader.GetData("ReportOnIrrigation", fieldNames: fieldNames);
            values = DataTableUtilities.GetColumnAsDoubles(data, "doy", CultureInfo.InvariantCulture);
            // There is one less irrigation event, as the manager script doesn't irrigate.
            expected = new double[] { 1, 32, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335 };
            Assert.That(values, Is.EqualTo(expected));
        }

        /// <summary>
        /// Ensures that comments work in event names:
        ///
        /// Clock.Today.StartOfWeek // works normally
        /// // should be ignored
        /// //Clock.Today.EndOfWeek // entire line should be ignored
        /// </summary>
        [Test]
        public static void TestCommentsInEventNames()
        {
            Simulations file = Utilities.GetRunnableSim();

            Report report = file.FindInScope<Report>();
            report.Name = "Report"; // Just to make sure
            report.VariableNames = new string[] { "[Clock].Today.DayOfYear as doy" };
            report.EventNames = new string[]
            {
                "[Clock].StartOfWeek // works normally",
                "// Should be ignored",
                "//[Clock].EndOfWeek // entire line should be ignored"
            };

            IClock clock = file.FindInScope<Clock>();
            clock.StartDate = new DateTime(2017, 1, 1);
            clock.EndDate = new DateTime(2017, 3, 1);

            Runner runner = new Runner(file);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            List<string> fieldNames = new List<string>() { "doy" };
            IDataStore storage = file.FindInScope<IDataStore>();
            DataTable data = storage.Reader.GetData("Report", fieldNames: fieldNames);
            double[] actual = DataTableUtilities.GetColumnAsDoubles(data, "doy", CultureInfo.InvariantCulture);
            double[] expected = new double[] { 1, 8, 15, 22, 29, 36, 43, 50, 57 };
            Assert.That(actual, Is.EqualTo(expected));
        }


        /// <summary>
        /// Ensure a simple array specification (e.g. soil.water[3]) works.
        /// </summary>
        [Test]
        public void TestArraySpecification()
        {
            var model = new MockModel() { Z = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 } };
            simulation.Children.Add(model);
            Utilities.InitialiseModel(simulation);

            report.VariableNames = new string[] { "[MockModel].Z[3]", "[MockModel].Z[10]" };

            List<Exception> errors = runner.Run();
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors.Count, Is.EqualTo(0));

            Assert.That(storage.Get<double>("MockModel.Z(3)"), Is.EqualTo(
                            new double[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }));
            Assert.That(storage.Get<double>("MockModel.Z(10)"), Is.EqualTo(
                            new double[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 }));
        }

        /// <summary>
        /// Ensure array range specification with a start index (e.g. soil.water[3:]) works.
        /// </summary>
        [Test]
        public void TestArrayRangeWithStartSpecification()
        {
            var mod = new MockModel() { Z = new double[] { 1, 2, 3, 4 } };
            simulation.Children.Add(mod);
            simulation.Children.Remove(storage);
            var datastore = new DataStore();
            simulation.Children.Add(datastore);
            Utilities.InitialiseModel(simulation);

            report.VariableNames = new string[] { "[MockModel].Z[3:]" };

            List<Exception> errors = runner.Run();
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors.Count, Is.EqualTo(0));
            datastore.Writer.Stop();
            datastore.Reader.Refresh();

            var data = datastore.Reader.GetData("Report");
            var columnNames = DataTableUtilities.GetColumnNames(data);
            Assert.That(columnNames.Contains("MockModel.Z(0)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(1)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(2)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(3)"), Is.True);
            Assert.That(columnNames.Contains("MockModel.Z(4)"), Is.True);

            Assert.That(DataTableUtilities.GetColumnAsDoubles(data, "MockModel.Z(3)", CultureInfo.InvariantCulture), Is.EqualTo(
                            new double[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }));
            Assert.That(DataTableUtilities.GetColumnAsDoubles(data, "MockModel.Z(4)", CultureInfo.InvariantCulture), Is.EqualTo(
                            new double[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 }));
        }

        /// <summary>
        /// Ensure array range specification with a end index (e.g. soil.water[:2]) works.
        /// </summary>
        [Test]
        public void TestArrayRangeWithEndSpecification()
        {
            var mod = new MockModel() { Z = new double[] { 1, 2, 3 } };
            simulation.Children.Add(mod);
            simulation.Children.Remove(storage);
            var datastore = new DataStore();
            simulation.Children.Add(datastore);
            Utilities.InitialiseModel(simulation);

            report.VariableNames = new string[] { "[MockModel].Z[:2]" };

            List<Exception> errors = runner.Run();
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors.Count, Is.EqualTo(0));
            datastore.Writer.Stop();
            datastore.Reader.Refresh();

            var data = datastore.Reader.GetData("Report");
            var columnNames = DataTableUtilities.GetColumnNames(data);
            Assert.That(columnNames.Contains("MockModel.Z(0)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(1)"), Is.True);
            Assert.That(columnNames.Contains("MockModel.Z(2)"), Is.True);
            Assert.That(columnNames.Contains("MockModel.Z(3)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(4)"), Is.False);
            Assert.That(DataTableUtilities.GetColumnAsDoubles(data, "MockModel.Z(1)", CultureInfo.InvariantCulture), Is.EqualTo(
                            new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
            Assert.That(DataTableUtilities.GetColumnAsDoubles(data, "MockModel.Z(2)", CultureInfo.InvariantCulture), Is.EqualTo(
                            new double[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }));
        }

        /// <summary>
        /// Ensure array range specification with a start and end index (e.g. soil.water[1:3]) works.
        /// </summary>
        [Test]
        public void TestArrayRangeWithStartAndEndSpecification()
        {
            var mod = new MockModel() { Z = new double[] { 1, 2, 3 } };
            simulation.Children.Add(mod);
            simulation.Children.Remove(storage);
            var datastore = new DataStore();
            simulation.Children.Add(datastore);
            Utilities.InitialiseModel(simulation);

            report.VariableNames = new string[] { "[MockModel].Z[2:3]" };

            List<Exception> errors = runner.Run();
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors.Count, Is.EqualTo(0));
            datastore.Writer.Stop();
            datastore.Reader.Refresh();

            var data = datastore.Reader.GetData("Report");
            var columnNames = DataTableUtilities.GetColumnNames(data);
            Assert.That(columnNames.Contains("MockModel.Z(0)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(1)"), Is.False);
            Assert.That(columnNames.Contains("MockModel.Z(2)"), Is.True);
            Assert.That(columnNames.Contains("MockModel.Z(3)"), Is.True);
            Assert.That(columnNames.Contains("MockModel.Z(4)"), Is.False);

            Assert.That(DataTableUtilities.GetColumnAsDoubles(data, "MockModel.Z(2)", CultureInfo.InvariantCulture), Is.EqualTo(
                            new double[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }));

            Assert.That(DataTableUtilities.GetColumnAsDoubles(data, "MockModel.Z(3)", CultureInfo.InvariantCulture), Is.EqualTo(
                            new double[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }));
        }

        /// <summary>
        /// This one reproduces bug #2038, where reporting an enum
        /// coerces the enum value to an int rather than a string.
        /// https://github.com/APSIMInitiative/ApsimX/issues/2038
        /// </summary>
        [Test]
        public void TestEnumReporting()
        {
            Simulations sims = Utilities.GetRunnableSim();

            IModel paddock = sims.FindInScope<Zone>();
            Manager script = new Manager();
            script.Name = "Manager";
            script.Code = @"using System;
using Models.Core;
using Models.PMF;

namespace Models
{
	[Serializable]
	public class Script : Model
	{
		public enum TestEnum
		{
			Red,
			Green,
			Blue
		};

		public TestEnum Value { get; set; }
    }
}";

            paddock.Children.Add(script);
            script.Parent = paddock;
            script.OnCreated();

            Report report = sims.FindInScope<Report>();
            report.VariableNames = new string[]
            {
                "[Manager].Script.Value as x"
            };
            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new Exception("Errors while running sims", errors[0]);

            List<string> fieldNames = new List<string>() { "x" };
            IDataStore storage = sims.FindInScope<IDataStore>();
            DataTable data = storage.Reader.GetData("Report", fieldNames: fieldNames);
            string[] actual = DataTableUtilities.GetColumnAsStrings(data, "x", CultureInfo.InvariantCulture);

            // The enum values should have been cast to strings before being reported.
            string[] expected = Enumerable.Repeat("Red", actual.Length).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
        }

        /// <summary>
        /// Ensure a group by works.
        /// </summary>
        [Test]
        public void TestGroupBySpecification()
        {
            report.EventNames = new string[] { "[Clock].EndOfSimulation" };
            report.GroupByVariableName = "[Mock].A";

            var model = new MockModelValuesChangeDaily
                (aDailyValues: new double[] { 1, 1, 1, 2, 2, 2, 3, 3, 3,  3 },
                 bDailyValues: new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
            {
                 Name = "Mock"
            };

            simulation.Children.Add(model);
            Utilities.InitialiseModel(simulation);

            report.VariableNames = new string[]
            {
                "[Clock].Today",
                "sum of [Mock].B from [Clock].StartOfSimulation to [Clock].EndOfSimulation as SumA"
            };

            List<Exception> errors = runner.Run();
            Assert.That(errors, Is.Not.Null);   
            Assert.That(errors.Count, Is.EqualTo(0));

            Assert.That(storage.Get<double>("SumA"), Is.EqualTo(
                            new double[] { 6, 15, 34 }));

            Assert.That(storage.Get<DateTime>("Clock.Today"), Is.EqualTo(
                            new DateTime[] { new DateTime(2017, 1, 3), new DateTime(2017, 1, 6), new DateTime(2017, 1, 10) }));
        }

        /// <summary>This test ensures that having lots of spacing is ok.</summary>
        [Test]
        public void EnsureLotsOfSpacingWorks()
        {
            clock.EndDate = new DateTime(2017, 1, 15);
            report.VariableNames = new string[]
            {
                "sum   of   [Clock].Today.DayOfYear   from   [Report].DayAfterLastOutput   to   [Clock].Today   as   values",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("values"), Is.EqualTo(
                            new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }));
        }

        /// <summary>This test ensures that having a dot in the alias is ok.</summary>
        [Test]
        public void EnsureDotInAliasWorks()
        {
            report.VariableNames = new string[]
            {
                "sum of [Clock].Today.DayOfYear from [Clock].StartOfSimulation to [Clock].EndOfSimulation as Total.DayOfYear",
            };
            report.EventNames = new string[]
            {
                "[Clock].EndOfSimulation",
            };

            // Run the simulation.
            runner.Run();

            Assert.That(storage.Get<double>("Total.DayOfYear"), Is.EqualTo(new double[] { 55 }));
        }

        [Test]
        public void ArrayIndexOnScalarIsIllegal()
        {
            report.VariableNames = new[] { "[Clock].Today.DayOfYear[1]" };
            List<Exception> errors = runner.Run();
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Attempt to run a simulation with invalid report variables. Ensure
        /// that the simulation generates an exception.
        /// </summary>
        /// <param name="variableName">The invalid variable name.</param>
        [TestCase("asdf")]
        [TestCase("[Simulation].")]
        [TestCase("sum([Simulation].)")]
        public void TestInvalidVariableName(string variableName)
        {
            report.VariableNames = new[] { variableName };
            List<Exception> errors = runner.Run();
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure report puts a warning in the summary file when user reports on StartOfSimulation.
        /// </summary>
        [Test]
        public void TestWriteMessageToSummaryWhenStartOfSimulationIsUsed()
        {
            report.EventNames = new string[]
            {
                "[Clock].StartOfSimulation",
            };

            // Run the simulation.

            Utilities.ResolveLinks(simulation);
            Utilities.CallEventAll(simulation, "SubscribeToEvents");

            var summary = simulation.FindDescendant<MockSummary>();
            Assert.That(summary.messages.First(), Is.EqualTo("WARNING: Report on StartOfFirstDay instead of StartOfSimulation. At StartOfSimulation, models may not be fully initialised."));
        }
    }
}
