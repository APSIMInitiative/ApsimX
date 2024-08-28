namespace UnitTests.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Factorial;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnitTests.Storage;
    using static Models.Core.Run.Runner;
    using Models.Utilities.Extensions;

    /// <summary>This is a test class for the GenerateApsimxFiles class</summary>
    [TestFixture]
    public class GenerateApsimXFilesTests
    {
        /// <summary>Ensure that 2 seperate files are generated for 2 simulations.</summary>
        [Test]
        public void EnsureFilesAreGeneratedForTwoSimulations()
        {
            // Create a folder of 2 simulations.
            var folder = new Folder()
            {
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
                                new MockSummary(),
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
                            }
                        }
                    }
            };

            var path = Path.Combine(Path.GetTempPath(), "GenerateApsimXFiles");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            // Create a list of progress ints.
            var progress = new List<double>();

            // Create a runner for our folder.
            Runner runner = new Runner(folder);
            IEnumerable<string> generatedFiles = GenerateApsimXFiles.Generate(runner, 1, path, (s) => { progress.Add(s); });

            Assert.That(progress.Count, Is.EqualTo(2));
            Assert.That(progress[0], Is.EqualTo(0.5));
            Assert.That(progress[1], Is.EqualTo(1));

            Assert.That(generatedFiles.Count(), Is.EqualTo(2));
            Assert.That(Path.GetFileName(generatedFiles.First()), Is.EqualTo("generated-0.apsimx"));
            Assert.That(Path.GetFileName(generatedFiles.Last()), Is.EqualTo("generated-1.apsimx"));
            Directory.Delete(path, true);
        }

        /// <summary>
        /// This test reproduces bug #6461 on github. The problem occurs when
        /// an experiment overrides manager script properties via a factor.
        /// The bug is that the changes aren't saved when the model is serialized.
        /// </summary>
        [Test]
        public void TestManagerParameterChanges()
        {
            Manager m = new Manager()
            {
                Name = "Manager",
                Code = "using System; namespace Models { using Core; [Serializable] public class Script : Models.Core.Model { [Description(\"x\")] public string X { get; set; } } }"
            };
            Simulations sims = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new DataStore(),
                    new Experiment()
                    {
                        Name = "expt",
                        Children = new List<IModel>()
                        {
                            new Factors()
                            {
                                Children = new List<IModel>()
                                {
                                    new Factor()
                                    {
                                        Name = "x",
                                        Specification = "[Manager].Script.X = 1"
                                    }
                                }
                            },
                            new Simulation()
                            {
                                Name = "sim",
                                Children = new List<IModel>()
                                {
                                    new Clock()
                                    {
                                        StartDate = new DateTime(2020, 1, 1),
                                        EndDate = new DateTime(2020, 1, 2),
                                        Name = "Clock"
                                    },
                                    new Summary(),
                                    m
                                }
                            }
                        }
                    }
                }
            };
            sims.ParentAllDescendants();
            m.OnCreated();
            Runner runner = new Runner(sims);
            string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                IEnumerable<string> files = GenerateApsimXFiles.Generate(runner, 1, temp, _ => {});
                Assert.That(files.Count(), Is.EqualTo(1));
                string file = files.First();
                sims = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false).NewModel as Simulations;
                Assert.That(sims.FindByPath("[Manager].Script.X").Value, Is.EqualTo("1"));
            }
            finally
            {
                if (Directory.Exists(temp))
                {
                    string fileName = "generated-0.db";
                    FileInfo file = new(temp + Path.DirectorySeparatorChar + fileName);
                    while(!file.IsLocked())
                        Directory.Delete(temp, true);
                }
            }
        }
    }
}
