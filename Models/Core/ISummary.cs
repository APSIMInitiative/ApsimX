namespace Models.Core
{
    public interface ISummary
    {
        /// <summary>
        /// Write a message to the summary
        /// </summary>
        void WriteMessage(string Message);

        /// <summary>
        /// Write a property to the summary.
        /// </summary>
        void WriteProperty(string Name, string Value);

        /// <summary>
        /// Return the html for the summary file.
        /// </summary>
        string GetHtml(string apsimSummaryImageFileName);
    }
}