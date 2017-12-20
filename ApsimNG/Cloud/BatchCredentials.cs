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
                Url = (string)Properties.Settings.Default["BatchUrl"],
                Account = (string)Properties.Settings.Default["BatchAccount"],
                Key = (string)Properties.Settings.Default["BatchKey"]
            };
        }
    }
}
