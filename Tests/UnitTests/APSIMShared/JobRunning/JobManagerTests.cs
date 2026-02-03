using APSIM.Shared.JobRunning;
using NUnit.Framework;

namespace UnitTests.APSIMShared.JobRunning
{

    public class JobManagerTests
    {
        [Test]
        public void RunJobsSuccessfully()
        {
            var jobs = new JobManager();
            jobs.Add(new MockJob());
            jobs.Add(new MockJob());

            var enumerator = jobs.GetJobs().GetEnumerator();
            
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.False);
        }
    }
}
