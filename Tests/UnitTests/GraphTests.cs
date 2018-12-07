using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Runners;
using Models.Graph;
using Models.Report;
using Models.Storage;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests for graphs/series.
    /// </summary>
    class GraphTests
    {
        /// <summary>
        /// There was a bug where graphing data from a report which exists
        /// directly under a simulation doesn't work - no data is found.
        /// </summary>
        [Test]
        public void TestGodLevelReport()
        {
            Simulation baseSimulation = new Simulation();
            baseSimulation.Name = "Base";

            Clock clock = new Clock()
            {
                Name = "Clock",
                Parent = baseSimulation,
                StartDate = new DateTime(2018, 1, 1),
                EndDate = new DateTime(2018, 1, 3)
            };
            baseSimulation.Children.Add(clock);

            Report report = new Report()
            {
                Name = "Report",
                Parent = baseSimulation,
                VariableNames = new string[] { "[Clock].Today" },
                EventNames = new string[] { "[Clock].DoReport" }
            };
            baseSimulation.Children.Add(report);

            Summary summary = new Summary();
            summary.Name = "Summary";
            summary.Parent = baseSimulation;
            baseSimulation.Children.Add(summary);

            Graph faultyGraph = new Graph()
            {
                Name = "FaultyGraph",
                Parent = baseSimulation
            };

            Series faultySeries = new Series()
            {
                Name = "FaultySeries",
                Parent = faultyGraph,
                TableName = "Report",
                XFieldName = "Clock.Today",
                YFieldName = "Clock.Today",
            };
            faultyGraph.Children.Add(faultySeries);

            IStorageReader storage = new DataStore();

            Simulations testFile = Simulations.Create(new List<IModel>() { baseSimulation, faultyGraph, storage as IModel });
            testFile.FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");

            IJobManager jobManager = Runner.ForSimulations(testFile, testFile, false);
            IJobRunner jobRunner = new JobRunnerAsync();
            jobRunner.Run(jobManager, wait: true);
            
            List<SeriesDefinition> definitions = new List<SeriesDefinition>();
            faultySeries.GetSeriesToPutOnGraph(storage, definitions);
            if (definitions == null || definitions.Count < 1)
                throw new Exception("Unable to graph data from a report which exists directly under a simulation.");
        }
    }
}
