namespace Utility
{
    internal class FileInput : InputAttribute
    {
        /// <summary>
        /// Recommended file extension.
        /// </summary>
        public string[] Extensions { get; set; }

        /// <summary>
        /// Constructor to provide recommended file extensions.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <param name="extensions">Recommended file extensions.</param>
        public FileInput(string name, params string[] extensions) : base(name)
        {
            Extensions = extensions;
        }
    }
}
