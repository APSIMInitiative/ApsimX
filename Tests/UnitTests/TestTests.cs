using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.Runners;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests for ITest implementations.
    /// </summary>
    class TestTests
    {
        /// <summary>
        /// Tests a simple implementation of ITest via a manager script.
        /// </summary>
        [Test]
        public void ManagerScriptTest()
        {
            string managerCode = "using System; using Models.Core; namespace Models { public class Script : Model, ITest { public void Run() { @action } } }";
            Manager testManager = new Manager();
            testManager.Name = "TestScript";

            Folder testFolder = new Folder();
            testFolder.Name = "TestFolder";

            testFolder.Children = new List<Model>() { testManager };
            testManager.Parent = testFolder;

            MockStorage storage = new MockStorage();

            Simulations simToRun = Simulations.Create(new List<IModel>() { testFolder, storage });
            IJobManager jobManager = Runner.ForSimulations(simToRun, simToRun, true);

            // Test should fail if it throws.
            testManager.Code = managerCode.Replace("@action", "throw new Exception(\"Test has failed.\");");
            TestWithAllJobRunners(jobManager, EnsureSimulationRanRed);

            // Test should pass if it doesn't throw.
            testManager.Code = managerCode.Replace("@action", "return;");
            TestWithAllJobRunners(jobManager, EnsureSimulationRanGreen);
        }

        /// <summary>
        /// Runs an <see cref="IJobManager"/> with all implementations of <see cref="IJobRunner"/>.
        /// </summary>
        /// <param name="jobManager">The job manager to run.</param>
        /// <param name="onSimulationCompleted">Event handler which will be invoked by each job runner after it finishes running.</param>
        private void TestWithAllJobRunners(IJobManager jobManager, EventHandler<AllCompletedArgs> onSimulationCompleted)
        {
            IJobRunner jobRunner = new JobRunnerSync();
            jobRunner.AllJobsCompleted += onSimulationCompleted;
            Assert.DoesNotThrow(() => jobRunner.Run(jobManager, true));

            jobRunner.AllJobsCompleted -= onSimulationCompleted;

            jobRunner = new JobRunnerAsync();
            jobRunner.AllJobsCompleted += onSimulationCompleted;
            Assert.DoesNotThrow(() => jobRunner.Run(jobManager, true));

            jobRunner.AllJobsCompleted -= onSimulationCompleted;

            jobRunner = new JobRunnerMultiProcess(false);
            jobRunner.AllJobsCompleted += onSimulationCompleted;
            Assert.DoesNotThrow(() => jobRunner.Run(jobManager, true));
            jobRunner.AllJobsCompleted -= onSimulationCompleted;
        }

        /// <summary>
        /// Event handler for a job runner's <see cref="IJobRunner.AllJobsCompleted"/> event.
        /// Asserts that the job ran successfully.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void EnsureSimulationRanGreen(object sender, AllCompletedArgs args)
        {
            if (args.exceptionThrown != null)
                throw new Exception(string.Format("Exception was thrown when running via {0}, when we expected no error to be thrown.", sender.GetType().Name), args.exceptionThrown);
        }

        /// <summary>
        /// Event handler for a job runner's <see cref="IJobRunner.AllJobsCompleted"/> event.
        /// Asserts that the job ran unsuccessfully.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void EnsureSimulationRanRed(object sender, AllCompletedArgs args)
        {
            if (args.exceptionThrown == null)
                throw new Exception(string.Format("{0} failed to throw an exception, when we expected an error to be thrown.", sender.GetType().Name));
        }
    }
}
