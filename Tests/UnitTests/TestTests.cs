using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
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
            string managerCode = "using System; using Models.Core; namespace Models { public class Script : Model, ITest { public bool Run() { @action } } }";
            Manager testManager = new Manager()
            {
                Name = "TestScript"
            };

            Folder testFolder = new Folder()
            {
                Name = "TestFolder"
            };

            testFolder.Children = new List<Model>() { testManager };
            testManager.Parent = testFolder;

            MockStorage storage = new MockStorage();

            Simulations simToRun = Simulations.Create(new List<IModel>() { testFolder, storage });
            IJobManager jobManager = Runner.ForSimulations(simToRun, simToRun, true);
            IJobRunner jobRunner = new JobRunnerSync();

            // Test should fail if it throws.
            jobRunner.AllJobsCompleted += EnsureSimulationRanRed;
            testManager.Code = managerCode.Replace("@action", "throw new Exception(\"Test has failed.\");");
            jobRunner.Run(jobManager, true);

            // Test should pass if it doesn't throw.
            jobRunner.AllJobsCompleted -= EnsureSimulationRanRed;
            jobRunner.AllJobsCompleted += EnsureSimulationRanGreen;
            testManager.Code = managerCode.Replace("@action", "return true;");
            jobRunner.Run(jobManager, true);
        }

        private void EnsureSimulationRanGreen(object sender, AllCompletedArgs args)
        {
            Assert.Null(args.exceptionThrown);
        }

        private void EnsureSimulationRanRed(object sender, AllCompletedArgs args)
        {
            Assert.NotNull(args.exceptionThrown);
        }
    }
}
