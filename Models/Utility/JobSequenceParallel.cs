// -----------------------------------------------------------------------
// <copyright file="JobSequenceParallel.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace WorkFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// A composite class for a sequence of jobs that will be run asynchronously.
    /// If an error occurs in any job, then this job will also produce an error.
    /// </summary>
    public class JobSequenceParallel : Utility.JobManager.IRunnable
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

            for (int j = 0; j < Jobs.Count; j++)
            {
                // Add job to the queue
                jobManager.AddJob(Jobs[j]);

                // Wait for it to be completed.
                while (jobManager.GetJobStatus(j) != Utility.JobManager.StatusEnum.Completed)
                    Thread.Sleep(200);
                
                // Get a possible error message. Null if no error.
                string errorMessage = jobManager.GetJobErrorMessage(j);
                
                // Remove job from queue
                jobManager.RemoveJob(j);

                if (errorMessage != null)
                    throw new Exception(errorMessage);
            }            
        }
    }
}
