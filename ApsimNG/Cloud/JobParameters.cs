using System;

namespace ApsimNG.Cloud
{
    public class JobParameters
    {
        /// <summary>
        /// Display name of the job.
        /// </summary>
        public string JobDisplayName { get; set; }

        /// <summary>
        /// Unique ID of the job.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// Directory to save results to.
        /// </summary>
        public string OutputDir { get; set; }

        /// <summary>
        /// If true, results will be combined into a single .csv file.
        /// </summary>
        public bool Summarise { get; set; }

        /// <summary>
        /// State of the job (active, running, complete...).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Directory that model files should be saved to.
        /// </summary>
        public string ModelPath { get; set; }

        /// <summary>
        /// If true, ApplicationPackagePath points to a directory. If false, it points to a .zip file.
        /// </summary>
        public bool ApsimFromDir { get; set; }

        /// <summary>
        /// Directory or zip file containing ApsimX to be uploaded.
        /// </summary>
        public string ApplicationPackagePath { get; set; }

        /// <summary>
        /// Version of APSIM to be uploaded.
        /// </summary>
        public string ApplicationPackageVersion { get; set; }

        /// <summary>
        /// An email will be sent to this address when the job finishes.
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// Number of cores per process.
        /// </summary>
        public int CoresPerProcess { get; set; }

        /// <summary>
        /// Number of VMs per pool.
        /// </summary>
        public int PoolVMCount { get; set; }

        /// <summary>
        /// Maximum number of tasks allowed on a single VM.
        /// </summary>
        public int PoolMaxTasksPerVM { get; set; }

        /// <summary>
        /// If true, results will automatically be downloaded once the job is finished.
        /// </summary>
        public bool AutoDownload { get; set; }
        /// <summary>
        /// If true, model files will be saved after they are generated.
        /// </summary>
        public bool SaveModelFiles { get; set; }

        /// <summary>
        /// If true, the job manager will submit the tasks.
        /// </summary>
        public bool JobManagerShouldSubmitTasks { get; set; }


        public bool AutoScale { get; set; }
    }
}