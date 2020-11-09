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
        public void Run(CancellationTokenSource cancelToken)
        {
            Thread.Sleep(50);
            if (doThrow)
                throw new Exception("Intentional exception");
            HasRun = true;
        }
    }
}
