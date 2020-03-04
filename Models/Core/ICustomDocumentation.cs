namespace Models.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// This interface provides a custom documentation method called by the auto
    /// documentation process
    /// </summary>
    public interface ICustomDocumentation
    {
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent);
    }
}