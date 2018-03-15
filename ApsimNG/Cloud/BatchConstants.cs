using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return string.Format("{0}\\Model", GetJobInputPath(jobId));
        }

        public static string GetApsimExe(Guid jobId)
        {
            return string.Format("{0}\\Apsim\\Model\\ApsimModel.exe", GetJobInputPath(jobId));
        }

        public static string GetApsimPath(Guid jobId)
        {
            return string.Format("{0}\\Apsim", GetJobInputPath(jobId));
        }

        public static string Get7ZipExe(Guid jobId)
        {
            return string.Format("{0}\\7za.exe", GetJobInputPath(jobId));
        }

        public static string GetAzCopyExe(Guid jobId)
        {
            return string.Format("{0}\\AzCopy.exe", GetJobInputPath(jobId));
        }

        public static string GetJobManagerPath(Guid jobId)
        {
            return string.Format("azure-apsim.exe");
        }
    }
}
