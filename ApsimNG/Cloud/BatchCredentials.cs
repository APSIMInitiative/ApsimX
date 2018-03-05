using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace ApsimNG.Cloud
{    
    public class BatchCredentials
    {
        public string Url { get; set; }
        public string Account { get; set; }
        public string Key { get; set; }

        public static BatchCredentials FromConfiguration()
        {
            return new BatchCredentials
            {
                Url = (string)AzureSettings.Default["BatchUrl"],
                Account = (string)AzureSettings.Default["BatchAccount"],
                Key = (string)AzureSettings.Default["BatchKey"]
            };
        }
    }
}
