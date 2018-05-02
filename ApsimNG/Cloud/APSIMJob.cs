namespace ApsimNG.Cloud
{
    public class APSIMJob
    {
        /// <summary>
        /// Display name/description for the job.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// URL of the model once it has been uploaded.
        /// </summary>
        public string ModelZipFileSas { get; set; }

        /// <summary>
        /// Path of the apsim directory/archive to be zipped and uploaded.
        /// </summary>
        public string ApplicationPackagePath { get; set; }

        /// <summary>
        /// Version of Apsim being uploaded.
        /// </summary>
        public string ApsimApplicationPackageVersion { get; set; }

        /// <summary>
        /// Email address. An automated message will be sent here when the job is finished.
        /// </summary>        
        public string Recipient { get; set; }

        /// <summary>
        /// Azure batch credentials. Each user will have their own credentials.
        /// </summary>
        public BatchCredentials BatchAuth { get; set; }

        /// <summary>
        /// Azure storage credentials. Each user will have their own credentials.
        /// </summary>
        public StorageCredentials StorageAuth { get; set; }

        /// <summary>
        /// Azure pool settings for the job.
        /// </summary>
        public PoolSettings PoolInfo { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dispName"></param>
        /// <param name="zipFileSas"></param>
        /// <param name="packagePath"></param>
        /// <param name="packageVersion"></param>
        /// <param name="recipient"></param>
        /// <param name="batch"></param>
        /// <param name="storage"></param>
        /// <param name="pool"></param>
        public APSIMJob (string dispName, string zipFileSas, string packagePath, string packageVersion, string recipient, BatchCredentials batch, StorageCredentials storage, PoolSettings pool)
        {
            DisplayName = dispName;
            ModelZipFileSas = zipFileSas;            
            ApplicationPackagePath = packagePath;
            ApsimApplicationPackageVersion = packageVersion;            
            Recipient = recipient;
            BatchAuth = batch;
            StorageAuth = storage;
            PoolInfo = pool;
        }
    }
}
