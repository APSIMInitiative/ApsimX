using APSIM.Shared.Utilities;
using System;
using System.IO;

namespace APSIM.Interop.Documentation.Helpers
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
        /// <param name="fileName">Name of the .bib file.</param>
        public BibTeX(string fileName)
        {
            try
            {
                contents = File.ReadAllText(fileName).Replace("\r\n", "\n");
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to open bib file {fileName}", err);
            }
        }

        /// <summary>Lookups the specified citation name and returns it.</summary>
        /// <param name="citationName">Name of the citation to search for.</param>
        /// <returns>Returns the found citation or null if not found.</returns>
        public ICitation Lookup(string citationName)
        {
            int posAmpersand = contents.IndexOf('@');
            while (posAmpersand != -1)
            {
                int posOpenBracket = contents.IndexOf('{', posAmpersand);
                if (posOpenBracket == -1)
                    throw new Exception("Bad format in .bib file around pos " + posAmpersand);

                int posComma = contents.IndexOf(',', posOpenBracket);
                if (posComma == -1)
                    throw new Exception("Bad format in .bib file around pos " + posAmpersand);

                string articleName = contents.Substring(posOpenBracket + 1, posComma - posOpenBracket - 1);

                int posCloseBracket = StringUtilities.FindMatchingClosingBracket(contents, posOpenBracket, '{', '}');
                if (posOpenBracket == -1 || posCloseBracket == -1)
                    throw new Exception($"Bad format (closing bracket) in .bib file after article: {articleName}, opening bracket at: {posOpenBracket}");

                if (articleName == citationName)
                    return new Citation(contents.Substring(posAmpersand, posCloseBracket - posAmpersand));
                else
                    posAmpersand = contents.IndexOf('@', posCloseBracket);

            }

            return null;
        }
    }
}
