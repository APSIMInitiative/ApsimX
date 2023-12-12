using APSIM.Shared.JobRunning;
using System;
using System.Collections.Generic;
using Models.Core.Run;
using System.Linq;
using static Models.Core.Overrides;

namespace APSIM.Server
{
    /// <summary>
    /// This class extends the standard job manager. It's designed to be hold the
    /// jobs in memory, in a state that is ready to be run and can be re-run
    /// multiple times without re-preparing the jobs each time.
    /// </summary>
    public class ServerJobRunner : JobRunner, IDisposable
    {
        public IEnumerable<Override> Replacements { get; set; } = Enumerable.Empty<Override>();
        private List<(IRunnable, IJobManager)> jobs = new List<(IRunnable, IJobManager)>();

        /// <summary>
        /// Get a list of jobs to be run.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<(IRunnable, IJobManager)> GetJobs() => jobs;

        /// <summary>
        /// Add the job manager and prepare all jobs to be run.
        /// </summary>
        /// <param name="jobManager"></param>
        public override void Add(IJobManager jobManager)
        {
            base.Add(jobManager);
            foreach (IRunnable job in jobManager.GetJobs())
            {
                job.Prepare();
                jobs.Add((job, jobManager));
            }
        }

        protected override void Prepare(IRunnable job)
        {
            // Do nothing - jobs are already prepared at this point.
            // todo: should we call base.Prepare if job is not a simulation?
        }

        protected override void Run(IRunnable job)
        {
            if (job is SimulationDescription sim)
            {
                sim.Storage.Writer.Clean(new[] { sim.SimulationToRun.Name }, false);
                sim.Run(cancelToken, Replacements);
            }
            else
            {
                base.Prepare(job);
                base.Run(job);
            }
        }

        protected override void Cleanup(IRunnable job)
        {
            // Don't cleanup any jobs until all have been run.
        }

        /// <summary>
        /// Cleanup all jobs now.
        /// </summary>
        public void Dispose()
        {
            foreach ( (IRunnable job, _) in jobs)
            {
                job.Cleanup(cancelToken);
            }
        }
    }
}