using APSIM.Shared.JobRunning;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.APSIMShared.JobRunning
{

    public class JobRunnerTests
    {
        [Test]
        public void RunJobs()
        {
            JobRunner[] runners = new JobRunner[] { new JobRunner(), new JobRunner(numProcessors:1) };
            foreach (var runner in runners)
            {
                MockJob job1;
                MockJob job2;
                MockJob job3;

                var jobs1 = new JobManager();
                jobs1.Add(job1 = new MockJob());
                jobs1.Add(job2 = new MockJob());
                runner.Add(jobs1);

                var jobs2 = new JobManager();
                jobs2.Add(job3 = new MockJob());
                runner.Add(jobs2);

                runner.Run(wait: true);
                Assert.IsTrue(job1.HasRun);
                Assert.IsTrue(job2.HasRun);
                Assert.IsTrue(job3.HasRun);
            }
        }

        [Test]
        public void RunJobsEnsuingEventsAreInvoked()
        {
            JobRunner[] runners = new JobRunner[] { new JobRunner(), new JobRunner(numProcessors: 1) };
            foreach (var runner in runners)
            {
                var jobs1 = new JobManager();
                jobs1.Add(new MockJob());
                jobs1.Add(new MockJob());
                runner.Add(jobs1);

                var jobs2 = new JobManager();
                jobs2.Add(new MockJob());
                runner.Add(jobs2);

                var jobCompleteArgsRunner = new List<JobCompleteArguments>();
                var allCompleteArgsRunner = new List<AllCompleteArguments>();
                runner.JobCompleted += (sender, e) => { lock (this) jobCompleteArgsRunner.Add(e); };
                runner.AllCompleted += (sender, e) => { lock (this) allCompleteArgsRunner.Add(e); };

                runner.Run(wait: true);

                // Test the runner events.
                Assert.AreEqual(jobCompleteArgsRunner.Count, 3);
                Assert.Greater(jobCompleteArgsRunner[0].ElapsedTime.TotalMilliseconds, 50);
                Assert.IsNull(jobCompleteArgsRunner[0].ExceptionThrowByJob);
                Assert.IsNotNull(jobCompleteArgsRunner[0].Job);
                Assert.Greater(jobCompleteArgsRunner[1].ElapsedTime.TotalMilliseconds, 50);
                Assert.IsNull(jobCompleteArgsRunner[1].ExceptionThrowByJob);
                Assert.IsNotNull(jobCompleteArgsRunner[1].Job);
                Assert.Greater(jobCompleteArgsRunner[2].ElapsedTime.TotalMilliseconds, 50);
                Assert.IsNull(jobCompleteArgsRunner[2].ExceptionThrowByJob);
                Assert.IsNotNull(jobCompleteArgsRunner[2].Job);

                Assert.AreEqual(allCompleteArgsRunner.Count, 1);
                Assert.IsNull(allCompleteArgsRunner[0].ExceptionThrowByRunner);
            }
        }

        [Test]
        public void RunJobsWithExceptionEnsuingEventsAreInvoked()
        {
            JobRunner[] runners = new JobRunner[] { new JobRunner(), new JobRunner(numProcessors: 1) };
            foreach (var runner in runners)
            {
                var jobs1 = new JobManager();
                jobs1.Add(new MockJob(throws: true));
                runner.Add(jobs1);

                var jobCompleteArgsRunner = new List<JobCompleteArguments>();
                var allCompleteArgsRunner = new List<AllCompleteArguments>();
                runner.JobCompleted += (sender, e) => { lock (this) jobCompleteArgsRunner.Add(e); };
                runner.AllCompleted += (sender, e) => { lock (this) allCompleteArgsRunner.Add(e); };

                runner.Run(wait: true);

                // Test the runner events.
                Assert.AreEqual(jobCompleteArgsRunner.Count, 1);
                Assert.Greater(jobCompleteArgsRunner[0].ElapsedTime.TotalMilliseconds, 50);
                Assert.IsNotNull(jobCompleteArgsRunner[0].ExceptionThrowByJob);
                Assert.IsNotNull(jobCompleteArgsRunner[0].Job);

                Assert.AreEqual(allCompleteArgsRunner.Count, 1);
                Assert.IsNull(allCompleteArgsRunner[0].ExceptionThrowByRunner);
                Assert.Greater(allCompleteArgsRunner[0].ElapsedTime.TotalMilliseconds, 50);
            }
        }
    }
}