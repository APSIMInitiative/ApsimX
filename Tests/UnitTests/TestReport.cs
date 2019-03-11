namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Report;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;

    [TestFixture]
    public class TestReport
    {
        [Test]
        public void EnsureReportWritesToStorage()
        {
            MockLocator locator = new MockLocator();
            MockStorage storage = new MockStorage();

            Report report = new Report();
            report.VariableNames = new string[] { "A", "B", "C" };
            report.EventNames = new string[0];

            Utilities.InjectLink(report, "simulation", new Simulation() { Name = "Sim1" });
            Utilities.InjectLink(report, "clock", new MockClock());
            Utilities.InjectLink(report, "storage", storage);
            Utilities.InjectLink(report, "locator", locator);
            Utilities.InjectLink(report, "events", new MockEvents());

            Utilities.CallEvent(report, "Commencing");

            locator.Values["A"] = new VariableObject(10);
            locator.Values["B"] = new VariableObject(20);
            locator.Values["C"] = new VariableObject(30);
            report.DoOutput();

            locator.Values["A"] = new VariableObject(40);
            locator.Values["B"] = new VariableObject(50);
            locator.Values["C"] = new VariableObject(60);
            report.DoOutput();
            Utilities.CallEvent(report, "Completed");


            Assert.AreEqual(storage.columnNames.ToArray(), new string[] { "Zone", "A", "B", "C" });
            Assert.AreEqual(storage.rows.Count, 2);
            Assert.AreEqual(storage.rows[0].values, new object[] { null, 10, 20, 30 });
            Assert.AreEqual(storage.rows[1].values, new object[] { null, 40, 50, 60 });
        }

        /// <summary>
        /// This test reproduces a bug where aggregation to [Clock].Today doesn't work, due to
        /// [Clock].Today being evaluated before the simulation starts.
        /// </summary>
        [Test]
        public void EnsureAggregationWorks()
        {
            // To test aggregation to [Clock].Today, we generate the first 10
            // triangular numbers by summing [Clock].Today over the first 10 days of the year.
            List<int> triangularNumbers = new List<int>() { 1, 3, 6, 10, 15, 21, 28, 36, 45, 55 };

            // To test aggregation to/from events, we sum day of year from start of week to end of week.
            // The simulation starts in 2017 January 1, which is a Sunday (start of week).
            List<int> weeklyNumbers = new List<int>() { 1, 3, 6, 10, 15, 21, 28, 8, 17, 27 };

            var sims = new Simulations()
            {
                FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"),
                Children = new List<Model>()
                {
                    new DataStore(),
                    new Simulation()
                    {
                        Children = new List<Model>()
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
                                Children = new List<Model>()
                                {
                                    new Report()
                                    {
                                        VariableNames = new string[]
                                        {
                                            "[Clock].Today.DayOfYear as n",
                                            "sum of [Clock].Today.DayOfYear from [Clock].StartDate to [Clock].Today as TriangularNumbers",
                                            "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as test"
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

            Apsim.ParentAllChildren(sims);
            Apsim.ChildrenRecursively(sims).ForEach(m => m.OnCreated());
            IJobManager jobManager = Runner.ForSimulations(sims, sims, false);
            IJobRunner jobRunner = new JobRunnerSync();
            jobRunner.Run(jobManager, wait: true);

			var storage = sims.Children[0] as IDataStore;
            storage.Writer.Stop();
            DataTable data = storage.Reader.GetData("Report", fieldNames: new List<string>() { "n", "TriangularNumbers", "test" });
            List<int> predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["TriangularNumbers"])).ToList();
            Assert.AreEqual(triangularNumbers, predicted, "Error in report aggregation involving [Clock].Today");

            predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["test"])).ToList();
            Assert.AreEqual(weeklyNumbers, predicted);
        }
    }
}