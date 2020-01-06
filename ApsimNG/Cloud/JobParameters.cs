using Models.Core;
using System;

namespace ApsimNG.Cloud
{
    public class JobParameters
    {
        /// <summary>
        /// The model to be run on the cloud platform.
        /// </summary>
        public IModel Model { get; set; }

        /// <summary>
        /// Display name of the job.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Unique ID of the job.
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// Directory that model files should be saved to.
        /// </summary>
        public string ModelPath { get; set; }

        /// <summary>
        /// Directory or zip file containing ApsimX to be uploaded.
        /// </summary>
        public string ApsimXPath { get; set; }

        /// <summary>
        /// Version of APSIM to be uploaded.
        /// </summary>
        public string ApsimXVersion { get; set; }

        /// <summary>
        /// Iff true, an email will be sent to <see cref="EmailRecipient"/> when the job finishes.
        /// </summary>
        public bool SendEmail { get; set; }

        /// <summary>
        /// iff <see cref="SendEmail"/> is true, an email will be sent to this address when the job finishes.
        /// </summary>
        public string EmailRecipient { get; set; }

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
        /// If true, model files will be saved after they are generated.
        /// </summary>
        public bool SaveModelFiles { get; set; }

        /// <summary>
        /// If true, the job manager will submit the tasks.
        /// </summary>
        public bool JobManagerShouldSubmitTasks { get; set; }

        /// <summary>
        /// This is used in some way by the job manager (azure-apsim.exe).
        /// It's always set to true but I'm not brave enough to remove it.
        /// </summary>
        public bool AutoScale { get; set; }
    }
}