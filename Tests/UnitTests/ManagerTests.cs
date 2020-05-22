using System;
using Models;
using Models.Core;
using APSIM.Shared.Utilities;
using NUnit.Framework;
namespace UnitTests
{
    using System.Collections.Generic;
    using Models.Core.ApsimFile;
    using Models.Storage;
    using System.IO;
    using APSIM.Shared.JobRunning;

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
            var simulation = new Simulation()
            {
                Name = "Sim",
                FileName = Path.GetTempFileName(),
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(2019, 1, 1),
                        EndDate = new DateTime(2019, 1, 2)
                    },
                    new MockSummary(),
                    new Manager()
                    {
                        Code = "asdf"
                    }
                }
            };

            Assert.Throws<Exception>(() => simulation.Run());
        }

        /// <summary>
        /// This test ensures that scripts aren't recompiled after events have
        /// been hooked up. Such behaviour would cause scripts to not receive
        /// any events, and the old/discarded scripts would receive events.
        /// </summary>
        [Test]
        public void TestScriptNotRebuilt()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.bork.apsimx");
            IModel file = FileFormat.ReadFromString<IModel>(json, out List<Exception> errors);
            Simulation sim = Apsim.Find(file, typeof(Simulation)) as Simulation;
            Assert.DoesNotThrow(() => sim.Run());
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

        private void EnsureJobRanRed(object sender, JobCompleteArguments args)
        {
            Assert.NotNull(args.ExceptionThrowByJob, "Simulation with a faulty manager script has run green.");
        }
    }
}
