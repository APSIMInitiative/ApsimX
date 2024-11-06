
namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A link to an external document.
    /// </summary>
    internal class ExternalDocument : IDocumentationFile
    {
        /// <summary>
        /// Display name of the file.
        /// </summary>
        public string Name { get; private set; }


        /// <summary>
        /// File URI.
        /// </summary>
        public string OutputFileName { get; private set; }

        /// <summary>
        /// Generate the output files.
        /// </summary>
        /// <param name="path"></param>
        public void Generate(string path)
        {
            // noop - file already exists.
        }

        public ExternalDocument(string name, string uri)
        {
            Name = name;
            OutputFileName = uri;
        }
    }
}
