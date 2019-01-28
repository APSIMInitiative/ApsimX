using System;
using Models;
using Models.Core;
using Models.Core.Runners;
using APSIM.Shared.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using Models.Core.ApsimFile;
using Models.Storage;
using System.IO;

namespace UnitTests
{
    /// <summary>
    /// Unit Tests for manager scripts.
    /// </summary>
    class ManagerTests
    {
        /// <summary>
        /// This test reproduces a bug in which a simulation could run without
        /// error despite a manager script containing a syntax error.
        /// </summary>
        [Test]
        public void TestManagerWithError()
        {
            List<Exception> errors = null;
            Simulations sims = sims = FileFormat.ReadFromString<Simulations>(ReflectionUtilities.GetResourceAsString("UnitTests.Resources.FaultyManager.apsimx"), out errors);
            DataStore storage = Apsim.Find(sims, typeof(DataStore)) as DataStore;
            sims.FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            
            IJobManager jobManager = Runner.ForSimulations(sims, sims, false);
            IJobRunner jobRunner = new JobRunnerSync();
            jobRunner.JobCompleted += EnsureJobRanRed;
            jobRunner.Run(jobManager, true);
        }

        private void EnsureJobRanRed(object sender, JobCompleteArgs args)
        {
            Assert.NotNull(args.exceptionThrowByJob, "Simulation with a faulty manager script has run green.");
        }
    }
}
