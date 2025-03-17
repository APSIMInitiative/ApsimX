namespace APSIM.Documentation.Bibliography
{
    /// <summary>
    /// An interface for a citation in a bibliography.
    /// </summary>
    public interface ICitation
    {
        /// <summary>Gets the name of the citation</summary>
        string Name  { get; }

        /// <summary>Gets the citation as it should be shown in-text.</summary>
        string InTextCite { get; }

        /// <summary>
        /// Gets the last name of the first author.
        /// </summary>
        string FirstAuthor { get; }

        /// <summary>Gets the year of publication</summary>
        int Year { get; }

        /// <summary>Gets the URL of the publication</summary>
        string URL { get; }

        /// <summary>Gets the citation as it should be shown in-bibliography.</summary>
        string BibliographyText { get; }
    }
}
