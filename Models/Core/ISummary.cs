namespace Models.Core
{
    /// <summary>
    /// A summary model interface for writing to the summary file.
    /// </summary>
    public interface ISummary
    {
        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="model">The model writing the message</param>
        /// <param name="message">The message to write</param>
        void WriteMessage(IModel model, string message);

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="model">The model writing the message</param>
        /// <param name="message">The message to write</param>
        void WriteWarning(IModel model, string message);

        /// <summary>Write an error message to the summary</summary>
        /// <param name="model">The model writing the message</param>
        /// <param name="message">The warning message to write</param>
        void WriteError(IModel model, string message);
    }
}