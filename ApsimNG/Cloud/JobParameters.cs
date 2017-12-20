using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    public class JobParameters
    {
        public string JobDisplayName { get; set; }
        public Guid JobId { get; set; }
        public string OutputDir { get; set; }
        public bool Summarise { get; set; }
        public string Status { get; set; }
        public string ModelPath { get; set; }
        public string ApplicationPackage { get; set; }
        public string ApplicationPackagePath { get; set; }
        public string ApplicationPackageVersion { get; set; }
        public string Recipient { get; set; }
        public int CoresPerProcess { get; set; }
        public int PoolVMCount { get; set; }
        public int PoolMaxTasksPerVM { get; set; }
        public bool SaveModelFiles { get; set; }
        public bool JobManagerShouldSubmitTasks { get; set; }
        public bool AutoScale { get; set; }
        public bool NoWait { get; set; }
    }
}