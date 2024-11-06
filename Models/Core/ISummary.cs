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
        /// <param name="messageType">Message output/verbosity level.</param>
        void WriteMessage(IModel model, string message, MessageType messageType);

        /// <summary>Writes all the stored messages to the datastore. Called when the table is big enough and at the end of simulation.</summary>
        void WriteMessagesToDataStore();
    }
}