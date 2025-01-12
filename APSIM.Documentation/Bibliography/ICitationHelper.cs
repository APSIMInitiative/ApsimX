namespace APSIM.Documentation.Bibliography
{
    /// <summary>
    /// An interface for a class which resolves citations for a bibliography.
    /// </summary>
    public interface ICitationHelper
    {
        /// <summary>Lookups the specified citation name and returns it.</summary>
        /// <param name="citationName">Name of the citation to search for.</param>
        /// <returns>Returns the found citation or null if not found.</returns>
        ICitation Lookup(string citationName);
    }
}
