using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud.Azure
{
    public class StorageConstants
    {
        public const string ApsimModelContainer = "apsim-models";
        public const string ApsimModelZipFormat = "models-{0}.zip";
        public const string ApsimBinContainer = "apsim-bins";
        public const string Apsim7ZipFileName = "7za.exe";
        public const string ApsimBinZipFormat = "bins-{0}.zip";
        public const string ApsimSimsContainer = "apsim-sims";
        public const string ApsimSimsZipFormat = "sims-{0}.zip";
        public const string ApsimInputsContainer = "apsim-inputs";
        public const string ApsimInputsZip = "inputs-{0}.zip";

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
