using Models.Core;
using System;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// This class holds details about a job to be run on the cloud.
    /// </summary>
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
        /// Directory or .zip file containing ApsimX to be uploaded.
        /// </summary>
        public string ApsimXPath { get; set; }

        /// <summary>
        /// Version of ApsimX to be uploaded.
        /// </summary>
        /// <remarks>
        /// This is currently something stupid, like tmp-hol430-X.
        /// Changing this may break everything though so it stays for now.
        /// </remarks>
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
        /// <remarks>
        /// Under the current implementation (Azure) there is no reason
        /// *not* to set this to 1.
        /// </remarks>
        public int CoresPerProcess { get; set; }

        /// <summary>
        /// Number of vCPUs to use when running the job.
        /// </summary>
        public int CpuCount { get; set; }

        /// <summary>
        /// Maximum number of tasks allowed to run concurrently on a single VM.
        /// </summary>
        /// <remarks>
        /// This will always be 16 for now because the azure component
        /// uses the standard_d5_v2 VM type, which has 16 vCPUs.
        /// </remarks>
        public int MaxTasksPerVM { get; set; }

        /// <summary>
        /// If true, model (.apsimx) files will be saved after they are generated.
        /// </summary>
        /// <remarks>
        /// Do we really need this option?
        /// </remarks>
        public bool SaveModelFiles { get; set; }

        /// <summary>
        /// Directory to which model (.apsimx) files should be saved.
        /// </summary>
        public string ModelPath { get; set; }

        /// <summary>
        /// Iff true, the job manager will submit the tasks.
        /// </summary>
        /// <remarks>
        /// Always true. todo: remove this property.
        /// </remarks>
        public bool JobManagerShouldSubmitTasks { get; set; }

        /// <summary>
        /// This is used in some way by the job manager (azure-apsim.exe).
        /// It's always set to true but I'm not brave enough to remove it.
        /// </summary>
        public bool AutoScale { get; set; }
    }
}