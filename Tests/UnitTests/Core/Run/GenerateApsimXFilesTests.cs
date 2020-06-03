namespace UnitTests.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.Run;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnitTests.Storage;
    using static Models.Core.Run.Runner;

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
            var progress = new List<int>();

            // Create a runner for our folder.
            Runner runner = new Runner(folder);
            GenerateApsimXFiles.Generate(runner, path, (s) => { progress.Add(s); });

            Assert.AreEqual(progress.Count, 2);
            Assert.AreEqual(progress[0], 50);
            Assert.AreEqual(progress[1], 100);

            var generatedFiles = Directory.GetFiles(path);
            Assert.AreEqual(generatedFiles.Length, 2);
            Assert.AreEqual(Path.GetFileName(generatedFiles[0]), "Sim1.apsimx");
            Assert.AreEqual(Path.GetFileName(generatedFiles[1]), "Sim2.apsimx");
            Directory.Delete(path, true);
        }
    }
}
