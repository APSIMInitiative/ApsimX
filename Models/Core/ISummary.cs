namespace Models.Core
{
    public interface ISummary
    {
        /// <summary>
        /// Write a message to the summary
        /// </summary>
        void WriteMessage(string FullPath, string Message);

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        void WriteWarning(string FullPath, string Message);

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        void WriteError(string FullPath, string Message);

        /// <summary>
        /// Return the html for the summary file.
        /// </summary>
        string GetSummary(string apsimSummaryImageFileName);

        /// <summary>
        /// Create a report file in text format.
        /// </summary>
        void CreateReportFile(bool baseline);

        bool html { get; set; }
        bool AutoCreate { get; set; }
        bool StateVariables { get; set; }
    }
}