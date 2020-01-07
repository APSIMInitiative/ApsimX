using System;
using System.IO;

namespace ApsimNG.Cloud.Azure
{
    class BatchConstants
    {
        public const string SevenZipFileName = "7za.exe";
        public const string ModelZipFileName = "model.zip";
        public const string JobManagerName = "JobManager";
        private const string ComputeNodeModelPath = "%AZ_BATCH_NODE_SHARED_DIR%\\{0}";

        public static string GetJobInputPath(Guid jobId)
        {
            return string.Format(ComputeNodeModelPath, jobId);
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
