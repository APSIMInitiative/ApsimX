using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace UnitTests.Functions
{
    [TestFixture]
    public class AccumulateFunctionGeneralTests
    {
        [Test]
        public void UseDatesRespectsStartEndAndReductionDates()
        {
            DataTable reportData = RunAccumulateFunctionExample();

            // StartDate begins on 1-Jun-1901.
            double startDateBeforeStart = GetValue(reportData, "UseDates", new DateTime(1901, 5, 31), "StartDate");
            double startDateOnStart = GetValue(reportData, "UseDates", new DateTime(1901, 6, 1), "StartDate");
            Assert.That(startDateBeforeStart, Is.EqualTo(0));
            Assert.That(startDateOnStart, Is.GreaterThan(0));

            // EndDate stops on 1-Jun-1902.
            double endDateBeforeStop = GetValue(reportData, "UseDates", new DateTime(1902, 5, 31), "EndDate");
            double endDateOnStop = GetValue(reportData, "UseDates", new DateTime(1902, 6, 1), "EndDate");
            Assert.That(endDateOnStop, Is.EqualTo(endDateBeforeStop).Within(1e-10));

            // StartAndEndDate stops on 1-Jun-1903.
            double startAndEndBeforeStop = GetValue(reportData, "UseDates", new DateTime(1903, 5, 31), "StartAndEndDate");
            double startAndEndOnStop = GetValue(reportData, "UseDates", new DateTime(1903, 6, 1), "StartAndEndDate");
            Assert.That(startAndEndOnStop, Is.EqualTo(startAndEndBeforeStop).Within(1e-10));

            // ReduceAnnual should drop on 1-Jun every year.
            double reduceAnnualBefore = GetValue(reportData, "UseDates", new DateTime(1901, 5, 31), "ReduceAnnual");
            double reduceAnnualOn = GetValue(reportData, "UseDates", new DateTime(1901, 6, 1), "ReduceAnnual");
            Assert.That(reduceAnnualOn, Is.LessThan(reduceAnnualBefore));

            // ReduceOnce should drop on 1-Jun-1903 only.
            double reduceOnceBefore = GetValue(reportData, "UseDates", new DateTime(1903, 5, 31), "ReduceOnce");
            double reduceOnceOn = GetValue(reportData, "UseDates", new DateTime(1903, 6, 1), "ReduceOnce");
            Assert.That(reduceOnceOn, Is.LessThan(reduceOnceBefore));

            // ReduceTwice should drop at the second configured date (1-Oct-1903).
            double reduceTwiceBeforeSecond = GetValue(reportData, "UseDates", new DateTime(1903, 9, 30), "ReduceTwice");
            double reduceTwiceOnSecond = GetValue(reportData, "UseDates", new DateTime(1903, 10, 1), "ReduceTwice");
            Assert.That(reduceTwiceOnSecond, Is.LessThan(reduceTwiceBeforeSecond));
        }

        [Test]
        public void UseEventsAndCropStagesRunAndProduceExpectedRelativeTotals()
        {
            DataTable reportData = RunAccumulateFunctionExample();
            DateTime endOfSimulation = new DateTime(1905, 12, 31);

            // Event-based behaviour.
            double eventsBasic = GetValue(reportData, "UseEvents", endOfSimulation, "Basic");
            double eventsStart = GetValue(reportData, "UseEvents", endOfSimulation, "Start");
            double eventsEnd = GetValue(reportData, "UseEvents", endOfSimulation, "End");
            double eventsStartAndEnd = GetValue(reportData, "UseEvents", endOfSimulation, "StartAndEnd");
            double eventsReduce = GetValue(reportData, "UseEvents", endOfSimulation, "Reduce");

            Assert.That(eventsBasic, Is.GreaterThan(0));
            Assert.That(eventsStart, Is.GreaterThan(0));
            Assert.That(eventsEnd, Is.GreaterThan(0));
            Assert.That(eventsStartAndEnd, Is.GreaterThan(eventsEnd));
            Assert.That(eventsReduce, Is.LessThan(eventsBasic));

            // Stage-based behaviour.
            double stagesBasic = GetValue(reportData, "CropStages", endOfSimulation, "Basic");
            double stagesStart = GetValue(reportData, "CropStages", endOfSimulation, "Start");
            double stagesEnd = GetValue(reportData, "CropStages", endOfSimulation, "End");
            double stagesStartAndEnd = GetValue(reportData, "CropStages", endOfSimulation, "StartAndEnd");
            double stagesReduce = GetValue(reportData, "CropStages", endOfSimulation, "Reduce");

            Assert.That(stagesBasic, Is.GreaterThan(0));
            Assert.That(stagesStart, Is.GreaterThan(0));
            Assert.That(stagesEnd, Is.GreaterThan(0));
            Assert.That(stagesStartAndEnd, Is.GreaterThan(0));
            Assert.That(stagesReduce, Is.LessThan(stagesBasic));
        }

        private static DataTable RunAccumulateFunctionExample()
        {
            string path = PathUtilities.GetAbsolutePath("%root%/Examples/AccumulateFunction.apsimx", null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();

            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;

            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new AggregateException(errors);

            storage.Writer.Stop();
            storage.Reader.Refresh();
            return storage.Reader.GetData("Report");
        }

        private static double GetValue(DataTable table, string simulationName, DateTime date, string columnName)
        {
            DataRow row = table.AsEnumerable().FirstOrDefault(r =>
                string.Equals(r.Field<string>("SimulationName"), simulationName, StringComparison.Ordinal) &&
                r.Field<DateTime>("Clock.Today") == date);

            Assert.That(row, Is.Not.Null, $"Could not find row for simulation '{simulationName}' on {date:yyyy-MM-dd}.");
            return Convert.ToDouble(row[columnName]);
        }
    }
}
