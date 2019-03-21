namespace UnitTests.Report
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.Run;
    using Models.Report;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;

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
                                            "sum of [Clock].Today.DayOfYear from [Clock].StartOfWeek to [Clock].EndOfWeek as test",
                                            "[Clock].Today.Year as Year",
                                            "sum of [Clock].Today.DayOfYear from 1-Jan to 31-Dec as SigmaDay",
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

            var runner = new Runner(sims);
            runner.Run(Runner.RunTypeEnum.MultiThreaded);

            var storage = sims.Children[0] as IDataStore;
            DataTable data = storage.Reader.GetData("Report", fieldNames: new List<string>() { "n", "TriangularNumbers", "test" });
            List<int> predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["TriangularNumbers"])).ToList();
            Assert.AreEqual(triangularNumbers, predicted, "Error in report aggregation involving [Clock].Today");

            predicted = data.AsEnumerable().Select(x => Convert.ToInt32(x["test"])).ToList();
            Assert.AreEqual(weeklyNumbers, predicted);
        }

        [Test]
        public void EnsureYearlyAggregationWorks()
        {
            Clock clock = Apsim.Find(sims, typeof(Clock)) as Clock;
            clock.StartDate = new DateTime(2017, 1, 1);
            clock.EndDate = new DateTime(2019, 1, 1);

            var runner = new Runner(sims);
            runner.Run(Runner.RunTypeEnum.MultiThreaded);

            var storage = sims.Children[0] as IDataStore;
            DataTable data = storage.Reader.GetData("Report", fieldNames: new List<string>() { "Year", "SigmaDay" });
            int finalValFirstYear = int.Parse(data.AsEnumerable().Where(x => int.Parse(x["Year"].ToString()) == 2017).Select(x => x["SigmaDay"]).Last().ToString());
            int firstValSecondYear = int.Parse(data.AsEnumerable().Where(x => int.Parse(x["Year"].ToString()) == 2018).Select(x => x["SigmaDay"]).First().ToString());
            Console.WriteLine($"finalValFirstYear={finalValFirstYear}, firstValSecondYear={firstValSecondYear}");
            Assert.That(finalValFirstYear > firstValSecondYear, $"Error: Report aggregation does not work from a dd-MMM date to another dd-MMM date. finalValFirstYear={finalValFirstYear}, firstValSecondYear={firstValSecondYear}");
        }
    }
}