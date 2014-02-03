using System;

namespace Models.Core
{
    public class ApsimXException : Exception
    {
        public string ModelFullPath { get; set; }
        public ApsimXException(string modelFullPath, string message)
            : base(message)
        {
            ModelFullPath = modelFullPath;
        }
    }
}
