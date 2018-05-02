using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ApsimNG.Cloud
{
    public class StorageCredentials
    {
        //this class should perhaps be renamed so as not to be identical to the Azure-provided one
        public string Account { get; set; }
        public string Key { get; set; }
        public static StorageCredentials FromConfiguration()
        {
            return new StorageCredentials
            {
                Account = (string)AzureSettings.Default["StorageAccount"],
                Key = (string)AzureSettings.Default["StorageKey"]
            };
        }
    }
}
