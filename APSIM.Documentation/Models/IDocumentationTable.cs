namespace APSIM.Documentation.Models
{
    internal interface IDocumentationTable
    {
        /// <summary>
        /// Build all documents referenced by the table.
        /// </summary>
        /// <param name="path">Output path for the generated documents.</param>
        void BuildDocuments(string path);

        /// <summary>
        /// Build a HTML document representing this table.
        /// </summary>
        string BuildHtmlDocument();
    }
}
