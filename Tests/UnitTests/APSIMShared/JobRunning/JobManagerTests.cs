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
            
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }
    }
}
