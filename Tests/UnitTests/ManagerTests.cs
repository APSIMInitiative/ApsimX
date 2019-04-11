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
            Simulations sims = sims = FileFormat.ReadFromString<Simulations>(ReflectionUtilities.GetResourceAsString("UnitTests.ManagerTestsFaultyManager.apsimx"), out errors);
            DataStore storage = Apsim.Find(sims, typeof(DataStore)) as DataStore;
            sims.FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            
            IJobManager jobManager = Runner.ForSimulations(sims, sims, false);
            IJobRunner jobRunner = new JobRunnerSync();
            jobRunner.JobCompleted += EnsureJobRanRed;
            jobRunner.Run(jobManager, true);
        }

        /// <summary>
        /// Ensures that Manager Scripts are allowed to override the
        /// OnCreated() method.
        /// </summary>
        /// <remarks>
        /// OnCreatedError.apsimx contains a manager script which overrides
        /// the OnCreated() method and throws an exception from this method.
        /// 
        /// This test ensures that an exception is thrown and that it is the
        /// correct exception.
        /// 
        /// The manager in this file is disabled, but its OnCreated() method
        /// should still be called.
        /// </remarks>
        [Test]
        public void ManagerScriptOnCreated()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.OnCreatedError.apsimx");
            List<Exception> errors = new List<Exception>();
            FileFormat.ReadFromString<IModel>(json, out errors);

            Assert.NotNull(errors);
            Assert.AreEqual(1, errors.Count, "Encountered the wrong number of errors when opening OnCreatedError.apsimx.");
            Assert.That(errors[0].ToString().Contains("Error thrown from manager script's OnCreated()"), "Encountered an error while opening OnCreatedError.apsimx, but it appears to be the wrong error: {0}.", errors[0].ToString());
        }

        private void EnsureJobRanRed(object sender, JobCompleteArgs args)
        {
            Assert.NotNull(args.exceptionThrowByJob, "Simulation with a faulty manager script has run green.");
        }
    }
}
