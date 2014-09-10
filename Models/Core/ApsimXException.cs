using System;

namespace Models.Core
{
    public class ApsimXException : Exception
    {
        public IModel model { get; set; }
        public ApsimXException(IModel model, string message)
            : base(message)
        {
            this.model = model;
        }
    }
}
