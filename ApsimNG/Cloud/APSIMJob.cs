using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    public class APSIMJob
    {
        public string DisplayName { get; set; }
        public string ModelZipFileSas { get; set; }
        public string ApsimApplicationPackage { get; set; }
        public string ApplicationPackagePath { get; set; }
        public string ApsimApplicationPackageVersion { get; set; }
        public string SevenZipApplicationPackage { get; set; }
        public string Recipient { get; set; }
        // TODO : think of better names for these last 3?
        public BatchCredentials BatchAuth { get; set; }
        public StorageCredentials StorageAuth { get; set; }
        public PoolSettings PoolInfo { get; set; }

        public APSIMJob (string dispName, string zipFileSas, string package, string packagePath, string packageVersion, string sevenZip, string recipient, BatchCredentials batch, StorageCredentials storage, PoolSettings pool)
        {
            DisplayName = dispName;
            ModelZipFileSas = zipFileSas;
            ApsimApplicationPackage = package;
            ApplicationPackagePath = packagePath;
            ApsimApplicationPackageVersion = packageVersion;
            SevenZipApplicationPackage = sevenZip;
            Recipient = recipient;
            BatchAuth = batch;
            StorageAuth = storage;
            PoolInfo = pool;
        }
    }
}
