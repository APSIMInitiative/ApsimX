using System.IO;

namespace ApsimNG.Cloud.Azure
{
    internal class Licence
    {
        public string BatchUrl { get; private set; }
        public string BatchAccount { get; private set; }
        public string BatchKey { get; private set; }
        public string StorageAccount { get; private set; }
        public string StorageKey { get; private set; }
        public string EmailSender { get; private set; }
        public string EmailPW { get; private set; }

        public Licence(string fileName)
        {
            foreach (string line in File.ReadAllLines(fileName))
            {
                int index = line.IndexOf('=');
                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();
                switch (key)
                {
                    case "BatchUrl":
                        BatchUrl = value;
                        break;
                    case "BatchAccount":
                        BatchAccount = value;
                        break;
                    case "BatchKey":
                        BatchKey = value;
                        break;
                    case "StorageAccount":
                        StorageAccount = value;
                        break;
                    case "StorageKey":
                        StorageKey = value;
                        break;
                    case "GmailAccount":
                        EmailSender = value;
                        break;
                    case "GmailPassword":
                        EmailPW = value;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
