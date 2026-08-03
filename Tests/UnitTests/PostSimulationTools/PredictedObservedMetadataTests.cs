using APSIM.Core;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.PostSimulationTools;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace UnitTests
{
    public class PredictedObservedMetadataTests
    {
        [Test]
        public void PredictedObservedMetadataTest()
        {
            Simulations simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new DataStore()
                    {
                        Children = new List<IModel>()
                        {
                            new PredictedObserved()
                            {
                                PredictedTableName = "Report1",
                                ObservedTableName = "Report2",
                                FieldNameUsedForMatch = "SimulationName",
                                FieldName2UsedForMatch = "Clock.Today"
                            }
                        }
                    },
                    new Simulation()
                    {
                        Name = "Sim",
                        FileName = Path.GetTempFileName(),
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(1980, 1, 3),
                                EndDate = new DateTime(1980, 1, 4)
                            },
                            new MockSummary(),
                            new Models.Report()
                            {
                                Name = "Report1",
                                VariableNames = ["[Clock].Today"],
                                EventNames = ["[Clock].EndOfDay"]
                            },
                            new Models.Report()
                            {
                                Name = "Report2",
                                VariableNames = ["[Clock].Today"],
                                EventNames = ["[Clock].EndOfDay"]
                            }
                        }
                    }
                }
            };
            Node root = Node.Create(simulations);

            // Run simulations.
            Runner runner = new Runner(root.Model as Simulations);
            List<Exception> errors = runner.Run();
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors.Count, Is.EqualTo(0));

            DataStore dataStore = simulations.Node.Find<DataStore>();
            dataStore.Refresh();

            DataTable dt = dataStore.Reader.GetData("_PredictedObserved");
            Assert.That(dt.Rows.Count, Is.EqualTo(1));

            object[] row = dt.Rows[0].ItemArray;
            Assert.That(row[3], Is.EqualTo("PredictedObserved"));
            Assert.That(row[4], Is.EqualTo("Report1"));
            Assert.That(row[5], Is.EqualTo("Report2"));
            Assert.That(row[6], Is.EqualTo("SimulationName"));
            Assert.That(row[7], Is.EqualTo("Clock.Today"));
            Assert.That(row[8], Is.EqualTo(DBNull.Value));
            Assert.That(row[9], Is.EqualTo(DBNull.Value));
        }
    }
}
