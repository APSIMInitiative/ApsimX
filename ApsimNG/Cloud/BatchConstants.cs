using System;
using System.IO;

namespace ApsimNG.Cloud
{
    class BatchConstants
    {
        public const string SEVENZIP_FILE_NAME = "7za.exe";
        public const string MODEL_ZIPFILE_NAME = "model.zip";
        public const string JOB_MANAGER_NAME = "JobManager";
        private const string COMPUTE_NODE_MODEL_PATH = "%AZ_BATCH_NODE_SHARED_DIR%\\{0}";

        public static string GetJobInputPath(Guid jobId)
        {
            return string.Format(COMPUTE_NODE_MODEL_PATH, jobId);
        }

        public static string GetModelPath(Guid jobId)
        {
            return Path.Combine(GetJobInputPath(jobId), "Model");
        }

        public static string GetApsimExe(Guid jobId)
        {
            return Path.Combine(GetJobInputPath(jobId), "Apsim", "Model", "ApsimModel.exe");
        }

        public static string GetApsimPath(Guid jobId)
        {
            return Path.Combine(GetJobInputPath(jobId), "Apsim");
        }

        public static string Get7ZipExe(Guid jobId)
        {
            return Path.Combine(GetJobInputPath(jobId), "7za.exe");
        }

        public static string GetAzCopyExe(Guid jobId)
        {
            return Path.Combine(GetJobInputPath(jobId), "AzCopy.exe");
        }

        public static string GetJobManagerPath(Guid jobId)
        {
            return string.Format("azure-apsim.exe");
        }
    }
}
