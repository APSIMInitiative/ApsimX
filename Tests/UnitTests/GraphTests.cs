using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
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
            Simulation sim = new Simulation()
            {
                Name = "Base",
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        Name = "Clock",
                        StartDate = new DateTime(2018, 1, 1),
                        EndDate = new DateTime(2018, 1, 3)
                    },
                    new Models.Report()
                    {
                        Name = "Report",
                        VariableNames = new string[] { "[Clock].Today" },
                        EventNames = new string[] { "[Clock].DoReport" }
                    },
                    new Summary()
                    {
                        Name = "Summary"
                    },
                    new Models.Graph()
                    {
                        Name = "FaultyGraph",
                        Children = new List<IModel>()
                        {
                            new Series()
                            {
                                Name = "FaultySeries",
                                TableName = "Report",
                                XFieldName = "Clock.Today",
                                YFieldName = "Clock.Today"
                            }
                        }
                    },
                    new DataStore(":memory:")
                }
            };

            var storage = sim.Children[4] as DataStore;
            storage.Open();
            var runner = new Runner(sim);
            List<Exception> errors = runner.Run();

            if (errors != null && errors.Count > 0)
                throw errors[0];

            var faultySeries = sim.Children[3].Children[0] as Series;
            List<SeriesDefinition> definitions = faultySeries.GetSeriesDefinitions(storage.Reader).ToList();
            if (definitions == null || definitions.Count < 1)
                throw new Exception("Unable to graph data from a report which exists directly under a simulation.");
        }
    }
}
