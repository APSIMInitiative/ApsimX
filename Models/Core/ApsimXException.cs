using System;

namespace Models.Core
{
    /// <summary>
    /// Apsim's exception object
    /// </summary>
    public class ApsimXException : Exception
    {
        /// <summary>Gets or sets the model.</summary>
        /// <value>The model.</value>
        public IModel model { get; set; }
        /// <summary>Initializes a new instance of the <see cref="ApsimXException"/> class.</summary>
        /// <param name="model">The model.</param>
        /// <param name="message">The message.</param>
        public ApsimXException(IModel model, string message)
            : base(message)
        {
            this.model = model;
        }
    }
}
