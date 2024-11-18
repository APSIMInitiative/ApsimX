namespace UnitTests.Core.Run
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
    using System.IO;
    using System.Linq;
    using UnitTests.Storage;
    using static Models.Core.Run.Runner;

    /// <summary>This is a test class for the RunnableSimulationList class</summary>
    [TestFixture]
    public class RunnerTests
    {
        private IDatabaseConnection database;

        private static RunTypeEnum[] runTypes = new RunTypeEnum[]
        {
            RunTypeEnum.MultiThreaded,
            RunTypeEnum.SingleThreaded
        };

        /// <summary>Initialisation code for all unit tests in this class</summary>
        [SetUp]
        public void Initialise()
        {
            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);

            if (ProcessUtilities.CurrentOS.IsWindows)
            {
                string sqliteSourceFileName = DataStoreWriterTests.FindSqlite3DLL();
                Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));

                var sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
                if (!File.Exists(sqliteFileName))
                {
                    File.Copy(sqliteSourceFileName, sqliteFileName, overwrite: true);
                }
            }
        }

        /// <summary>Ensure that runner can run a single simulation.</summary>
        [Test]
        public void EnsureSimulationRuns()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                // Create a simulation and add a datastore.
                var simulation = new Simulation()
                {
                    Name = "Sim",
                    FileName = Path.GetTempFileName(),
                    Children = new List<IModel>()
                    {
                        new Clock()
                        {
                            StartDate = new DateTime(1980, 1, 1),
                            EndDate = new DateTime(1980, 1, 2)
                        },
                        new MockSummary(),
                        new Models.Report()
                        {
                            Name = "Report",
                            VariableNames = new string[] {"[Clock].Today"},
                            EventNames = new string[] {"[Clock].DoReport"}
                        },
                        new DataStore(database)
                    }
                };

                // Run simulations.
                Runner runner = new Runner(simulation, runType: typeOfRun);
                List<Exception> errors = runner.Run();
                Assert.That(errors, Is.Not.Null);
                Assert.That(errors.Count, Is.EqualTo(0));


                Assert.That(
                    Utilities.CreateTable(new string[]                      {              "Clock.Today" },
                                          new List<object[]> { new object[] { new DateTime(1980, 01, 01) },
                                                               new object[] { new DateTime(1980, 01, 02) }})
                   .IsSame(database.ExecuteQuery("SELECT [Clock.Today] FROM Report ORDER BY [Clock.Today]")), Is.True);

                database.CloseDatabase();
            }
        }

        /// <summary>Ensure running two simulations in a folder runs.</summary>
        [Test]
        public void EnsureFolderOfSimulationsRuns()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                // Create a folder of 2 simulations.
                var folder = new Folder()
                {
                    Children = new List<IModel>()
                    {
                        new DataStore(database),
                        new Simulation()
                        {
                            Name = "Sim1",
                            FileName = Path.GetTempFileName(),
                            Children = new List<IModel>()
                            {
                                new Clock()
                                {
                                    StartDate = new DateTime(1980, 1, 1),
                                    EndDate = new DateTime(1980, 1, 2)
                                },
                                new MockSummary(),
                                new Models.Report()
                                {
                                    Name = "Report",
                                    VariableNames = new string[] {"[Clock].Today"},
                                    EventNames = new string[] {"[Clock].DoReport"}
                                },
                            }
                        },
                        new Simulation()
                        {
                            Name = "Sim2",
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
                                    Name = "Report",
                                    VariableNames = new string[] {"[Clock].Today"},
                                    EventNames = new string[] {"[Clock].DoReport"}
                                },
                            }
                        }
                    }
                };

                Runner runner = new Runner(folder, runType: typeOfRun);

                // Run simulations.
                List<Exception> errors = runner.Run();
                Assert.That(errors, Is.Not.Null);
                Assert.That(errors.Count, Is.EqualTo(0));

                // Check that data was written to database.

                Assert.That(
                    Utilities.CreateTable(new string[]                      {               "Clock.Today" },
                                          new List<object[]> { new object[] { new DateTime(1980, 01, 01) },
                                                               new object[] { new DateTime(1980, 01, 02) },
                                                               new object[] { new DateTime(1980, 01, 03) },
                                                               new object[] { new DateTime(1980, 01, 04) }})
                   .IsSame(database.ExecuteQuery("SELECT [Clock.Today] FROM Report ORDER BY [Clock.Today]")), Is.True);

                database.CloseDatabase();
            }
        }

        /// <summary>Ensure only selected simulations are run when specified.</summary>
        [Test]
        public void EnsureOnlySelectedSimulationsAreRun()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                // Create a folder of 2 simulations.
                var folder = new Folder()
                {
                    Children = new List<IModel>()
                    {
                        new DataStore(database),
                        new Simulation()
                        {
                            Name = "Sim1",
                            FileName = Path.GetTempFileName(),
                            Children = new List<IModel>()
                            {
                                new Clock()
                                {
                                    StartDate = new DateTime(1980, 1, 1),
                                    EndDate = new DateTime(1980, 1, 2)
                                },
                                new MockSummary(),
                                new Models.Report()
                                {
                                    Name = "Report",
                                    VariableNames = new string[] {"[Clock].Today"},
                                    EventNames = new string[] {"[Clock].DoReport"}
                                },
                            }
                        },
                        new Simulation()
                        {
                            Name = "Sim2",
                            FileName = Path.GetTempFileName(),
                            Children = new List<IModel>()
                            {
                                new Clock()
                                {
                                    StartDate = new DateTime(1980, 1, 3),
                                    EndDate = new DateTime(1980, 1, 4)
                                },
                                new MockSummary(),
                                new Report()
                                {
                                    Name = "Report",
                                    VariableNames = new string[] {"[Clock].Today"},
                                    EventNames = new string[] {"[Clock].DoReport"}
                                },
                            }
                        }
                    }
                };

                Runner runner = new Runner(folder, runType: typeOfRun, simulationNamesToRun: new string[] { "Sim1" });

                // Run simulations.
                List<Exception> errors = runner.Run();
                Assert.That(errors, Is.Not.Null);
                Assert.That(errors.Count, Is.EqualTo(0));

                // Check that data was written to database.
                Assert.That(
                    Utilities.CreateTable(new string[] { "Clock.Today" },
                                          new List<object[]> { new object[] { new DateTime(1980, 01, 01) },
                                                               new object[] { new DateTime(1980, 01, 02) }})
                   .IsSame(database.ExecuteQuery("SELECT [Clock.Today] FROM Report ORDER BY [Clock.Today]")), Is.True);

                database.CloseDatabase();
            }
        }

        /// <summary>Ensure running two simulations with exceptions return the exceptions.</summary>
        [Test]
        public void EnsureRunErrorsAreReturned()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                // Create a simulation and add a datastore.
                var simulation = new Simulation()
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
                        new DataStore(database),
                        new MockModelThatThrows()
                    }
                };

                // Run simulations.
                Runner runner = new Runner(simulation, runType: typeOfRun);
                var exceptions = runner.Run();

                // Make sure an exception is returned.
                Assert.That(exceptions[0].ToString().Contains("Intentional exception"), Is.True);

                database.CloseDatabase();
            }
        }

        /// <summary>Tests an implementation of failing ITest via a manager script.</summary>
        [Test]
        public void EnsureFailedITestExceptionIsReturned()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                var simulations = new Simulations()
                {
                    Children = new List<IModel>()
                    {
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
                                new DataStore(database),
                                new Manager()
                                {
                                    Code =  "using System;\r\n" +
                                            "using Models.Core;\r\n" +
                                            "namespace Models\r\n" +
                                            "{\r\n" +
                                            "   [Serializable]\r\n" +
                                            "   public class Script : Model, ITest\r\n" +
                                            "   {\r\n" +
                                            "      public void Run() { throw new Exception(\"Test has failed.\"); }\r\n" +
                                            "   }\r\n" +
                                            "}"
                                }
                            }
                        }
                    }
                };

                // Run simulations.
                Runner runner = new Runner(simulations, runType: typeOfRun, runTests: true);
                var exceptions = runner.Run();

                // Make sure an exception is returned.
                Assert.That(exceptions[0].ToString().Contains("Test has failed."), Is.True, $"Exception message {exceptions[0].ToString()} does not contain 'Test has failed.'.");

                database.CloseDatabase();
            }
        }

        /// <summary>Tests a passing ITest implementation.</summary>
        [Test]
        public void EnsurePassedITestWorks()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                var simulations = new Simulations()
                {
                    Children = new List<IModel>()
                    {
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
                                new DataStore(database),
                                new Manager()
                                {
                                    Code =  "using System;\r\n" +
                                            "using Models.Core;\r\n" +
                                            "namespace Models\r\n" +
                                            "{\r\n" +
                                            "   [Serializable]\r\n" +
                                            "   public class Script : Model, ITest\r\n" +
                                            "   {\r\n" +
                                            "      [Link]\r\n" +
                                            "      ISummary summary = null;\r\n" +
                                            "      public void Run() { summary.WriteMessage(this, \"Passed Test\", MessageType.Information); }\r\n" +
                                            "   }\r\n" +
                                            "}"
                                }
                            }
                        }
                    }
                };

                // Run simulations.
                Runner runner = new Runner(simulations, runType: typeOfRun, runTests:true);
                List<Exception> errors = runner.Run();
                Assert.That(errors, Is.Not.Null);
                Assert.That(errors.Count, Is.EqualTo(0));

                // Make sure an exception is returned.
                var summary = simulations.FindDescendant<MockSummary>();
                Assert.That(summary.messages.Find(m => m.Contains("Passed Test")), Is.Not.Null);

                database.CloseDatabase();
            }
        }

        /// <summary>Ensure the events from Runner are invoked.</summary>
        [Test]
        public void EnsureRunnerEventsAreInvoked()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                var simulation = new Simulation()
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
                        new DataStore(database),
                        new MockModelThatThrows()
                    }
                };

                // Run simulations.
                Runner runner = new Runner(simulation, runType: typeOfRun);

                AllJobsCompletedArgs argsOfAllCompletedJobs = null;
                runner.AllSimulationsCompleted += (sender, e) => { argsOfAllCompletedJobs = e; };

                runner.Run();

                // Make sure the expected exception was sent through the all completed jobs event.
                Assert.That(argsOfAllCompletedJobs.AllExceptionsThrown.Count, Is.EqualTo(1));
                Assert.That(argsOfAllCompletedJobs.AllExceptionsThrown[0].ToString().Contains("Intentional exception"), Is.True);

                database.CloseDatabase();
            }
        }
        [Serializable]
        private class PostSimToolWhichThrows : Model, IPostSimulationTool
        {
            public void Run()
            {
                throw new Exception("error from post-simulation tool");
            }
        }

        /// <summary>
        /// Ensure that only the post-simulation tools which are "in scope"
        /// are run.
        /// </summary>
        [Test]
        public void EnsureScopedPostSimulationToolsAreRun()
        {
            Simulation sim0 = new Simulation()
            {
                Name = "s0",
                Children = new List<IModel>()
                {
                    new MockClock(),
                    new MockSummary(),
                    new PostSimToolWhichThrows()
                }
            };
            Simulation sim1 = sim0.Clone();
            sim1.Name = "s1";

            Simulations sims = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new MockStorage()
                    {
                        Children = new List<IModel>()
                        {
                            new PostSimToolWhichThrows()
                        }
                    },
                    sim0,
                    sim1
                }
            };
            sims.ParentAllDescendants();

            Runner runner = new Runner(sim1);
            List<Exception> errors = runner.Run();

            // We ran sim1 only. Therefore two post-simulation tools
            // should have been run (the one under the simulation and the
            // one under the datastore).
            Assert.That(errors.Count, Is.EqualTo(2));
        }

        /// <summary>Ensure post simulation tools are run.</summary>
        [Test]
        public void EnsurePostSimulationToolsAreRun()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                var simulation = new Simulation()
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
                        new DataStore(database),
                        new MockPostSimulationTool(doThrow: true) { Name = "PostSim" }
                    }
                };

                Runner runner = new Runner(simulation, runType: typeOfRun);

                AllJobsCompletedArgs argsOfAllCompletedJobs = null;
                runner.AllSimulationsCompleted += (sender, e) => { argsOfAllCompletedJobs = e; };

                // Run simulations.
                runner.Run();

                // Make sure the expected exception was sent through the all completed jobs event.
                Assert.That(argsOfAllCompletedJobs.AllExceptionsThrown.Count, Is.EqualTo(1));
                Assert.That(argsOfAllCompletedJobs.AllExceptionsThrown[0].ToString().Contains("Intentional exception"), Is.True);

                database.CloseDatabase();
            }
        }

        [Serializable]
        private class TestPostSim : Model, IPostSimulationTool
        {
            [Link] private IDataStore storage = null;
            public List<string> TablesModified { get; set; }

            public void Run()
            {
                TablesModified = storage.Writer.TablesModified;
            }
        }

        /// <summary>
        /// Tests the TablesModified property of DataStoreWriter.
        /// This property should contain only the tables which were
        /// modified during the most recent simulation run.
        /// </summary>
        [Test]
        public void TestTablesModified()
        {
            IModel sim1 = new Simulation()
            {
                Name = "sim1",
                Children = new List<IModel>()
                {
                    new Report()
                    {
                        Name = "Report1",
                        VariableNames = new[] { "[Clock].Today" },
                        EventNames = new[] { "[Clock].DoReport" },
                    },
                    new MockSummary(),
                    new Clock()
                    {
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = new DateTime(2020, 1, 2),
                    },
                }
            };

            IModel sim2 = Apsim.Clone(sim1);
            sim2.Name = "sim2";
            sim2.Children[0].Name = "Report2";

            TestPostSim testPostSim = new TestPostSim();
            sim1.Children.Add(testPostSim);

            Simulations sims = Simulations.Create(new[] { sim1, sim2, new DataStore() });
            Utilities.InitialiseModel(sims);

            Runner runner = new Runner(sims, simulationNamesToRun: new[] { "sim1" });
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            List<string> tablesMod = new List<string>()
            {
                "_Factors",
                "Report1",
                "_Simulations",
                "_Checkpoints",
            };
            Assert.That(testPostSim.TablesModified.OrderBy(x => x), Is.EqualTo(tablesMod.OrderBy(x => x)));

            runner = new Runner(sims, simulationNamesToRun: new[] { "sim2" });
            errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            tablesMod = new List<string>()
            {
                "_Factors",
                "Report2",
                "_Simulations",
                "_Checkpoints",
            };
            Assert.That(testPostSim.TablesModified.OrderBy(x => x), Is.EqualTo(tablesMod.OrderBy(x => x)));

            // Now run both sims
            runner = new Runner(sims);
            errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            tablesMod = new List<string>()
            {
                "_Factors",
                "Report2",
                "Report1",
                "_Simulations",
                "_Checkpoints",
            };
            Assert.That(testPostSim.TablesModified.OrderBy(x => x), Is.EqualTo(tablesMod.OrderBy(x => x)));
        }

        /// <summary>Ensure only post simulation tools are run when specified.</summary>
        [Test]
        public void EnsureOnlyPostSimulationToolsAreRun()
        {
            foreach (var typeOfRun in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                var simulation = new Simulation()
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
                        new DataStore(database),
                        new MockPostSimulationTool(doThrow: true) { Name = "PostSim" }
                    }
                };

                Runner runner = new Runner(simulation, runType:typeOfRun, runSimulations:false);

                AllJobsCompletedArgs argsOfAllCompletedJobs = null;
                runner.AllSimulationsCompleted += (sender, e) => { argsOfAllCompletedJobs = e; };

                // Run simulations.
                runner.Run();

                // Simulation shouldn't have run. Check the summary messages to make
                // sure there is NOT a 'Simulation completed' message.
                var summary = simulation.FindDescendant<MockSummary>();
                Assert.That(summary.messages.Count, Is.EqualTo(0));

                Assert.That(runner.Progress, Is.EqualTo(1));

                // Make sure the expected exception was sent through the all completed jobs event.
                Assert.That(argsOfAllCompletedJobs.AllExceptionsThrown.Count, Is.EqualTo(1));
                Assert.That(argsOfAllCompletedJobs.AllExceptionsThrown[0].ToString().Contains("Intentional exception"), Is.True);

                database.CloseDatabase();
            }
        }

        /// <summary>
        /// In this test, we run only post-simualtion tools and ensure
        /// that any old data is correctly cleaned.
        /// </summary>
        [Test]
        public void EnsurePostSimulationToolsDoCleanup()
        {
            IEnumerable<RunTypeEnum> runTypes = new[] { RunTypeEnum.MultiThreaded, RunTypeEnum.SingleThreaded };
            foreach (RunTypeEnum runType in runTypes)
            {
                // Open an in-memory database.
                database = new SQLite();
                database.OpenDatabase(":memory:", readOnly: false);

                DataTable data = new DataTable("PostSimulationTool");
                data.Columns.Add("x");
                data.Rows.Add(data.NewRow());

                var storage = new DataStore(database)
                {
                    Children = new List<IModel>()
                    {
                        new SimpleDataWriter(data)
                    }
                };

                var sims = new Simulations()
                {
                    Children = new List<IModel>()
                    {
                        storage
                    }
                };

                Runner runner = new Runner(sims, runType: runType, runSimulations: false);
                List<Exception> errors = runner.Run();
                Assert.That(errors.Count, Is.EqualTo(0), errors.Count > 0 ? errors[0].ToString() : "");

                storage.Reader.Refresh();
                DataTable storedData = storage.Reader.GetData(data.TableName);
                Assert.That(storedData, Is.Not.Null);
                Assert.That(storedData.Rows.Count, Is.EqualTo(1));

                // Now run it again.
                runner = new Runner(new[] { sims }, runType: runType, runSimulations: false);
                errors = runner.Run();
                Assert.That(errors.Count, Is.EqualTo(0), errors.Count > 0 ? errors[0].ToString() : "");

                storage.Reader.Refresh();
                storedData = storage.Reader.GetData(data.TableName);
                Assert.That(storedData.Rows.Count, Is.EqualTo(1), "Post-simulation tool data was not cleaned when running only post-simulation tools");
            }
        }

        /// <summary>Ensure a folder of simulation files can be run.</summary>
        [Test]
        public void RunDirectoryOfFiles()
        {
            var simulations = new Simulations()
            {
                Name = "Simulations",
                Version = Converter.LatestVersion,
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Name = "Sim1",
                        FileName = Path.GetTempFileName(),
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(1980, 1, 1),
                                EndDate = new DateTime(1980, 1, 2)
                            },
                            new Summary()
                        }
                    },
                    new DataStore(),
                }
            };

            // Create a temporary directory.
            var path = Path.Combine(Path.GetTempPath(), "RunDirectoryOfFiles");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            File.WriteAllText(Path.Combine(path, "Sim1.apsimx"), FileFormat.WriteToString(simulations));

            simulations.Children[0].Name = "Sim2";
            File.WriteAllText(Path.Combine(path, "Sim2.apsimx"), FileFormat.WriteToString(simulations));

            var runner = new Runner(Path.Combine(path, "*.apsimx"));
            runner.Run();

            // Check simulation 1 database
            database = new SQLite();
            database.OpenDatabase(Path.Combine(path, "Sim1.db"), readOnly: true);

            Assert.That(
                    Utilities.CreateTable(new string[]                      {                        "Message" },
                                          new List<object[]> { new object[] { "Simulation terminated normally" }})
                   .IsSame(database.ExecuteQuery("SELECT [Message] FROM _Messages")), Is.True);

            database.CloseDatabase();

            // Check simulation 2 database
            database = new SQLite();
            database.OpenDatabase(Path.Combine(path, "Sim2.db"), readOnly: true);
            Assert.That(
                    Utilities.CreateTable(new string[]                      {                        "Message" },
                                          new List<object[]> { new object[] { "Simulation terminated normally" }})
                   .IsSame(database.ExecuteQuery("SELECT [Message] FROM _Messages")), Is.True);

            database.CloseDatabase();
        }
    }
}
