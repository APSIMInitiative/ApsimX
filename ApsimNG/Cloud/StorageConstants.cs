using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    public class StorageConstants
    {
        public const string APSIM_MODEL_CONTAINER = "apsim-models";
        public const string APSIM_MODEL_ZIP_FORMAT = "models-{0}.zip";
        public const string APSIM_BIN_CONTAINER = "apsim-bins";
        public const string APSIM_7ZIP_NAME = "7za.exe";
        public const string APSIM_BIN_ZIP_FORMAT = "bins-{0}.zip";
        public const string APSIM_SIM_CONTAINER = "apsim-sims";
        public const string APSIM_SIM_ZIP_FORMAT = "sims-{0}.zip";
        public const string APSIM_INPUTS_CONTAINER = "apsim-inputs";
        public const string APSIM_INPUTS_ZIP_FORMAT = "inputs-{0}.zip";

        public static string GetJobOutputContainer(Guid jobId)
        {
            return string.Format("job-{0}-outputs", jobId);
        }

        public static string GetJobContainer(Guid jobId)
        {
            return string.Format("job-{0}", jobId);
        }
    }
}
