// -----------------------------------------------------------------------
// <copyright file="JobSequence.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// A composite class for a sequence of jobs that will be run sequentially.
    /// If an error occurs in any job, then subsequent jobs won't be run.
    /// </summary>
    public class JobSequence : Utility.JobManager.IRunnable
    {
        /// <summary>A list of jobs that will be run in sequence.
        public List<Utility.JobManager.IRunnable> Jobs { get; set; }

        /// <summary>Called to start the job.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Get a reference to the JobManager so that we can add jobs to it.
            Utility.JobManager jobManager = e.Argument as Utility.JobManager;

            // Add all jobs to the queue
            foreach (Utility.JobManager.IRunnable job in Jobs)
            {
                jobManager.AddJob(job);
            }

            // Wait for it to be completed.
            while (jobManager.CountOfJobsFinished < jobManager.CountOfJobs)
                Thread.Sleep(200);
                
            // Get a possible error message. Null if no error.
            string errorMessage = string.Empty;
            for (int j = 0; j < jobManager.CountOfJobs; j++)
            {
                errorMessage += jobManager.GetJobErrorMessage(j);
                jobManager.RemoveJob(j);
            }
                
            if (errorMessage != null)
                throw new Exception(errorMessage);
        }
    }
}
