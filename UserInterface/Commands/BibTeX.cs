// -----------------------------------------------------------------------
// <copyright file="BibTeX.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Provides functionality for reading .bib (bibliography) files (http://www.bibtex.org/Format/)
    /// </summary>
    public class BibTeX
    {
        /// <summary>The raw BibTeX contents</summary>
        private string contents;

        /// <summary>Initializes a new instance of the <see cref="BibTeX"/> class.</summary>
        /// <param name="fileName">Name of the .bib file.</param>
        public BibTeX(string fileName)
        {
            contents = File.ReadAllText(fileName);
        }

        /// <summary>Lookups the specified citation name and returns it.</summary>
        /// <param name="citationName">Name of the citation to search for.</param>
        /// <returns>Returns the found citation or null if not found.</returns>
        public Citation Lookup(string citationName)
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

                int posCloseBracket = StringUtilities.FindMatchingClosingBracket(contents, posOpenBracket, '{', '}');
                if (posOpenBracket == -1)
                    throw new Exception("Bad format in .bib file around pos " + posAmpersand);

                string articleName = contents.Substring(posOpenBracket + 1, posComma - posOpenBracket - 1);
                if (articleName == citationName)
                    return new Citation(contents.Substring(posAmpersand, posCloseBracket - posAmpersand));
                else
                    posAmpersand = contents.IndexOf('@', posCloseBracket);

            }

            return null;
        }


        public class Citation
        {
            /// <summary>The raw BibTeX contents</summary>
            private string contents;

            /// <summary>
            /// Initializes a new instance of the <see cref="Citation"/> class.
            /// </summary>
            /// <param name="contents">The raw BibTeX contents.</param>
            public Citation(string contents)
            {
                this.contents = contents;
            }

            /// <summary>Gets the name of the citation</summary>
            public string Name 
            { 
                get
                {
                    int posOpenBracket = contents.IndexOf('{');
                    int posComma = contents.IndexOf(',');
                    return contents.Substring(posOpenBracket + 1, posComma - posOpenBracket - 1);
                }
            }

            /// <summary>Gets the in-text cite.</summary>
            public string InTextCite
            {
                get
                {
                    return FirstAuthor + " et al. " + Year;
                }
            }

            /// <summary>Gets the name of the first author</summary>
            private string FirstAuthor
            {
                get
                {
                    string authors = Get("author");
                    int posComma = authors.IndexOf(',');
                    if (posComma == -1)
                        return authors; // one author only.
                    else
                        return authors.Substring(0, posComma);
                }
            }

            /// <summary>Gets the authors</summary>
            private string Authors
            {
                get
                {
                    string authors = Get("author");
                    authors = authors.Replace(" and", ",");
                    return authors;
                }
            }

            /// <summary>Gets the year of publication</summary>
            private int Year
            {
                get
                {
                    string yearString = Get("year");
                    if (yearString == string.Empty)
                        return 0;
                    else
                        return Convert.ToInt32(yearString);
                }
            }

            /// <summary>Gets the URL of the publication</summary>
            public string URL
            {
                get
                {
                    string url = Get("url");
                    if (url.StartsWith("http") || url.StartsWith("www"))
                        return url;
                    else
                        return string.Empty;
                }
            }

            /// <summary>Gets the value of a specified keyword.</summary>
            /// <param name="keyword">The keyword.</param>
            /// <returns>The found value or string.Empty if not found.</returns>
            private string Get(string keyword)
            {
                string stringToFind = keyword + " = {";
                int posKeyWord = contents.IndexOf(stringToFind);
                if (posKeyWord == -1)
                    return string.Empty;
                else
                {
                    posKeyWord += stringToFind.Length;
                    int posEoln = contents.IndexOf('\n', posKeyWord);
                    if (posEoln == -1)
                        return string.Empty;

                    string text = contents.Substring(posKeyWord, posEoln - posKeyWord).TrimEnd(" ,}".ToCharArray());
                    text = text.Replace("{", "");
                    text = text.Replace("}", "");
                    text = text.Replace("--", "-");
                    text = text.Replace(@"\&", " and ");
                    return text;
                }
            }

            /// <summary>Gets the text for the references section.</summary>
            public string BibliographyText
            {
                get
                {
                    string text;

                    text = string.Format("{0}, {1}. {2}. {3}. {4} ({5}), {6}.",
                                         new object[] {
                                             Authors,
                                             Year,
                                             Get("title"),
                                             Get("journal"),
                                             Get("volume"),
                                             Get("number"),
                                             Get("pages") });

                    return text;
                }
            }
        }
    }
}
