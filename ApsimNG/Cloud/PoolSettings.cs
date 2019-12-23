namespace ApsimNG.Cloud
{
    public class PoolSettings
    {
        public string VMSize { get; set; }
        public int VMCount { get; set; }
        public int MaxTasksPerVM { get; set; }
        public string PoolName { get; set; }
        public string State { get; set; }

        public static PoolSettings FromConfiguration()
        {
            object maxTasks = AzureSettings.Default["PoolMaxTasksPerVM"];
            object name = AzureSettings.Default["PoolName"];
            object vmCount = AzureSettings.Default["PoolVMCount"];
            object size = AzureSettings.Default["PoolVMSize"];

            return new PoolSettings
            {
                
                MaxTasksPerVM = (maxTasks is string && (maxTasks as string).Length > 0) ? int.Parse((string)maxTasks) : 1,
                PoolName = (name is string && (name as string).Length > 0) ? (string)name : "",
                VMCount = (vmCount is string && (vmCount as string).Length > 0) ? int.Parse((string)vmCount) : 8,
                // Fallback to the standard_d5_v2 VM type if none is specified.
                // This VM has 16 vCPUs, 56 GiB of memory and 800 GiB temp (SSD) storage.
                // https://docs.microsoft.com/en-us/azure/virtual-machines/windows/sizes-general
                VMSize = (size is string && (size as string).Length > 0) ? (string)size : "standard_d5_v2"
            };
        }
    }
}