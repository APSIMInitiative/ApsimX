using System;

namespace Models.Core
{
    /// <summary>
    /// An exception class which includes context information about
    /// the erroneous model in its error message.
    /// </summary>
    public class ModelException : Exception
    {
        /// <summary>
        /// Create a new <see cref="ModelException" /> instance.
        /// The model path will be included in the error message.
        /// </summary>
        /// <param name="model">Model which has thrown the exception.</param>
        /// <param name="message">Error message.</param>
        public ModelException(IModel model, string message) : base($"Error in model {GetContext(model)}: {message}")
        {
        }

        /// <summary>
        /// Create a new <see cref="ModelException" /> instance.
        /// The model path will be included in the error message.
        /// </summary>
        /// <param name="model">Model which has thrown the exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public ModelException(IModel model, Exception innerException) : base($"Error in model {GetContext(model)}", innerException)
        {
        }

        /// <summary>
        /// Create a new <see cref="ModelException" /> instance.
        /// The model path will be included in the error message.
        /// </summary>
        /// <param name="model">Model which has thrown the exception.</param>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ModelException(IModel model, string message, Exception innerException) : base($"Error in model {GetContext(model)}: {message}", innerException)
        {
        }

        /// <summary>
        /// Return the model context which will be included in the error
        /// message. This is currently the model's full path, but could
        /// really be the model's name or other debug info.
        /// </summary>
        /// <param name="model">The model.</param>
        private static string GetContext(IModel model) => model.FullPath;
    }
}
