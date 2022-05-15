namespace UnitTests.APSIMShared
{
    using APSIM.Shared.JobRunning;
    using System;
    using System.Threading;

    public class MockJob : IRunnable
    {
        private bool doThrow;
        public bool HasRun { get; set; } = false;
        public string Name { get; set; }
        public double Progress { get; set; }

        public MockJob(bool throws = false)
        {
            doThrow = throws;
        }
        /// <summary>
        /// Prepare the job for running.
        /// </summary>
        public void Prepare()
        {
            // Do nothing.
        }

        public void Run(CancellationTokenSource cancelToken)
        {
            Thread.Sleep(50);
            if (doThrow)
                throw new Exception("Intentional exception");
            HasRun = true;
        }

        public void Cleanup()
        {
        }
    }
}
