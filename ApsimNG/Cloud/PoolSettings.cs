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
                VMSize = (size is string && (size as string).Length > 0) ? (string)size : "standard_d5_v2"
            };
        }
    }
}