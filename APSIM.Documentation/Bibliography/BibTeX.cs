using APSIM.Shared.Utilities;
using System;

namespace APSIM.Documentation.Bibliography
{

    /// <summary>
    /// Provides functionality for reading .bib (bibliography) files (http://www.bibtex.org/Format/)
    /// </summary>
    public class BibTeX : ICitationHelper
    {
        /// <summary>The raw BibTeX contents</summary>
        /// <remarks>
        /// The entire .bib file is read into memory during the constructor,
        /// for each instance of this class, and is only released when the
        /// instance is finalized. Should consider alternative ways of handling
        /// this. As at Sep 2021, the .bib file is approx 2MB.
        /// </remarks>
        private string contents;

        /// <summary>Initializes a new instance of the <see cref="BibTeX"/> class.</summary>
        public BibTeX()
        {
            contents = ReflectionUtilities.GetResourceAsString("APSIM.Documentation.Resources.APSIM.bib").Replace("\r", "");
        }

        /// <summary>Lookups the specified citation name and returns it.</summary>
        /// <param name="citationName">Name of the citation to search for.</param>
        /// <returns>Returns the found citation or null if not found.</returns>
        public ICitation Lookup(string citationName)
        {
            string lastArticleName = ""; //used to debug after error

            int posAmpersand = contents.IndexOf('@');
            while (posAmpersand != -1)
            {
                int posOpenBracket = contents.IndexOf('{', posAmpersand);
                if (posOpenBracket == -1)
                    throw new Exception($"Bad format ({{) in .bib file around pos {posAmpersand}, artice: {lastArticleName}");
                
                int posComma = contents.IndexOf(',', posOpenBracket);
                if (posComma == -1)
                {
                    //using a 3rd party tool exposed other possible bibtex types in particular a comment type that doesn't contain a ',' after the @type
                    string bibType = contents.Substring(posAmpersand + 1, posOpenBracket - (posAmpersand + 1));
                    if (bibType != "Comment")
                    {
                        throw new Exception($"Bad format (,) in .bib file around pos {posAmpersand}, article: {lastArticleName}");
                    }
                }
                else
                {
                    lastArticleName = contents.Substring(posOpenBracket + 1, posComma - posOpenBracket - 1);
                }

                int posCloseBracket = StringUtilities.FindMatchingClosingBracket(contents, posOpenBracket, '{', '}');
                if (posOpenBracket == -1 || posCloseBracket == -1)
                    throw new Exception($"Bad format (closing bracket) in .bib file after article: {lastArticleName}, opening bracket at: {posOpenBracket}");

                if (lastArticleName == citationName)
                    return new Citation(contents.Substring(posAmpersand, posCloseBracket - posAmpersand));
                else
                    posAmpersand = contents.IndexOf('@', posCloseBracket);

            }

            return null;
        }
    }
}
