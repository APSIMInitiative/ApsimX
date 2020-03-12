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
    using System.IO;
    using UnitTests.Storage;
    using static Models.Core.Run.Runner;

    /// <summary>This is a test class for the RunnableSimulationList class</summary>
    [TestFixture]
    public class RunnerTests
    {
        private IDatabaseConnection database;

        private static RunTypeEnum[] runTypes = new RunTypeEnum[]
        {
            RunTypeEnum.MultiProcess,
            RunTypeEnum.MultiThreaded,
            RunTypeEnum.SingleThreaded
        };

        /// <summary>Initialisation code for all unit tests in this class</summary>
        [SetUp]
        public void Initialise()
        {
            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);

            string sqliteSourceFileName = DataStoreWriterTests.FindSqlite3DLL();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));

            var sqliteFileName = Path.Combine(Directory.GetCurrentDirectory(), "sqlite3.dll");
            if (!File.Exists(sqliteFileName))
            {
                File.Copy(sqliteSourceFileName, sqliteFileName, overwrite: true);
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
                    Children = new List<Model>()
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
                Assert.IsNull(runner.Run());

                // Check that data was written to database.
                Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT [Clock.Today] FROM Report ORDER BY [Clock.Today]"),
                       "Clock.Today\r\n" +
                       " 1980-01-01\r\n" +
                       " 1980-01-02\r\n");

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
                    Children = new List<Model>()
                    {
                        new DataStore(database),
                        new Simulation()
                        {
                            Name = "Sim1",
                            FileName = Path.GetTempFileName(),
                            Children = new List<Model>()
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
                            Children = new List<Model>()
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

                // Ensure number of simulations is correct before any are run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 2);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 0);

                // Run simulations.
                Assert.IsNull(runner.Run());

                // Ensure number of simulations is correct after all simulations are run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 2);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 2);
                Assert.AreEqual(runner.PercentComplete(), 100);

                // Check that data was written to database.
                Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT [Clock.Today] FROM Report ORDER BY [Clock.Today]"),
                       "Clock.Today\r\n" +
                       " 1980-01-01\r\n" +
                       " 1980-01-02\r\n" +
                       " 1980-01-03\r\n" +
                       " 1980-01-04\r\n");

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
                    Children = new List<Model>()
                    {
                        new DataStore(database),
                        new Simulation()
                        {
                            Name = "Sim1",
                            FileName = Path.GetTempFileName(),
                            Children = new List<Model>()
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
                            Children = new List<Model>()
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

                Runner runner = new Runner(folder, runType: typeOfRun, simulationNamesToRun: new string[] { "Sim1" });

                // Ensure number of simulations is correct before any are run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 1);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 0);

                // Run simulations.
                Assert.IsNull(runner.Run());

                // Ensure number of simulations is correct after all simulations are run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 1);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 1);
                Assert.AreEqual(runner.PercentComplete(), 100);

                // Check that data was written to database.
                Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT [Clock.Today] FROM Report ORDER BY [Clock.Today]"),
                       "Clock.Today\r\n" +
                       " 1980-01-01\r\n" +
                       " 1980-01-02\r\n");

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
                    Children = new List<Model>()
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
                Assert.IsTrue(exceptions[0].ToString().Contains("Intentional exception"));

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

                var simulation = new Simulation()
                {
                    Name = "Sim",
                    FileName = Path.GetTempFileName(),
                    Children = new List<Model>()
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
                };

                // Run simulations.
                Runner runner = new Runner(simulation, runType: typeOfRun, runTests: true);
                var exceptions = runner.Run();

                // Make sure an exception is returned.
                Assert.IsTrue(exceptions[0].ToString().Contains("Test has failed."));

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

                var simulation = new Simulation()
                {
                    Name = "Sim",
                    FileName = Path.GetTempFileName(),
                    Children = new List<Model>()
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
                                    "      public void Run() { summary.WriteMessage(this, \"Passed Test\"); }\r\n" +
                                    "   }\r\n" +
                                    "}"
                        }
                    }
                };

                // Run simulations.
                Runner runner = new Runner(simulation, runType: typeOfRun, runTests:true);
                Assert.IsNull(runner.Run());

                // Make sure an exception is returned.
                Assert.IsNotNull(MockSummary.messages.Find(m => m.Contains("Passed Test")));

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
                    Children = new List<Model>()
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
                Assert.AreEqual(argsOfAllCompletedJobs.AllExceptionsThrown.Count, 1);
                Assert.IsTrue(argsOfAllCompletedJobs.AllExceptionsThrown[0].ToString().Contains("Intentional exception"));

                database.CloseDatabase();
            }
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
                    Children = new List<Model>()
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

                // Ensure number of simulations is correct before any are run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 1);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 0);

                AllJobsCompletedArgs argsOfAllCompletedJobs = null;
                runner.AllSimulationsCompleted += (sender, e) => { argsOfAllCompletedJobs = e; };

                // Run simulations.
                runner.Run();

                // Ensure number of simulations is correct after all have been run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 1);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 1);
                Assert.AreEqual(runner.PercentComplete(), 100);

                // Make sure the expected exception was sent through the all completed jobs event.
                Assert.AreEqual(argsOfAllCompletedJobs.AllExceptionsThrown.Count, 1);
                Assert.IsTrue(argsOfAllCompletedJobs.AllExceptionsThrown[0].ToString().Contains("Intentional exception"));

                database.CloseDatabase();
            }
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
                    Children = new List<Model>()
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

                // Ensure number of simulations is correct before any are run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 0);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 0);

                AllJobsCompletedArgs argsOfAllCompletedJobs = null;
                runner.AllSimulationsCompleted += (sender, e) => { argsOfAllCompletedJobs = e; };

                // Run simulations.
                runner.Run();

                // Simulation shouldn't have run. Check the summary messages to make
                // sure there is NOT a 'Simulation completed' message.
                Assert.AreEqual(MockSummary.messages.Count, 0);

                // Ensure number of simulations is correct after all have been run.
                Assert.AreEqual(runner.TotalNumberOfSimulations, 0);
                Assert.AreEqual(runner.NumberOfSimulationsCompleted, 0);
                Assert.AreEqual(runner.PercentComplete(), 0);

                // Make sure the expected exception was sent through the all completed jobs event.
                Assert.AreEqual(argsOfAllCompletedJobs.AllExceptionsThrown.Count, 1);
                Assert.IsTrue(argsOfAllCompletedJobs.AllExceptionsThrown[0].ToString().Contains("Intentional exception"));

                database.CloseDatabase();
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
                Children = new List<Model>()
                {
                    new Simulation()
                    {
                        Name = "Sim1",
                        FileName = Path.GetTempFileName(),
                        Children = new List<Model>()
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
            Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT [Message] FROM _Messages"),
                   "                       Message\r\n" +
                   "Simulation terminated normally\r\n");

            database.CloseDatabase();

            // Check simulation 2 database
            database = new SQLite();
            database.OpenDatabase(Path.Combine(path, "Sim2.db"), readOnly: true);
            Assert.AreEqual(Utilities.TableToStringUsingSQL(database, "SELECT [Message] FROM _Messages"),
                   "                       Message\r\n" +
                   "Simulation terminated normally\r\n");
            database.CloseDatabase();
        }
    }
}
