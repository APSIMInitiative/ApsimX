namespace UnitTests.APSIMShared.JobRunning
{
    using APSIM.Shared.JobRunning;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;

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
                Assert.That(job1.HasRun, Is.True);
                Assert.That(job2.HasRun, Is.True);
                Assert.That(job3.HasRun, Is.True);
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
                Assert.That(jobCompleteArgsRunner.Count, Is.EqualTo(3));
                Assert.That(jobCompleteArgsRunner[0].ElapsedTime.TotalMilliseconds, Is.GreaterThan(50));
                Assert.That(jobCompleteArgsRunner[0].ExceptionThrowByJob, Is.Null);
                Assert.That(jobCompleteArgsRunner[0].Job, Is.Not.Null);
                Assert.That(jobCompleteArgsRunner[1].ElapsedTime.TotalMilliseconds, Is.GreaterThan(50));
                Assert.That(jobCompleteArgsRunner[1].ExceptionThrowByJob, Is.Null);
                Assert.That(jobCompleteArgsRunner[1].Job, Is.Not.Null);
                Assert.That(jobCompleteArgsRunner[2].ElapsedTime.TotalMilliseconds, Is.GreaterThan(50));
                Assert.That(jobCompleteArgsRunner[2].ExceptionThrowByJob, Is.Null);
                Assert.That(jobCompleteArgsRunner[2].Job, Is.Not.Null);

                Assert.That(allCompleteArgsRunner.Count, Is.EqualTo(1));
                Assert.That(allCompleteArgsRunner[0].ExceptionThrowByRunner, Is.Null);
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
                Assert.That(jobCompleteArgsRunner.Count, Is.EqualTo(1));
                Assert.That(jobCompleteArgsRunner[0].ElapsedTime.TotalMilliseconds, Is.GreaterThan(50));
                Assert.That(jobCompleteArgsRunner[0].ExceptionThrowByJob, Is.Not.Null);
                Assert.That(jobCompleteArgsRunner[0].Job, Is.Not.Null);

                Assert.That(allCompleteArgsRunner.Count, Is.EqualTo(1));
                Assert.That(allCompleteArgsRunner[0].ExceptionThrowByRunner, Is.Null);
                Assert.That(allCompleteArgsRunner[0].ElapsedTime.TotalMilliseconds, Is.GreaterThan(50));
            }
        }
    }
}