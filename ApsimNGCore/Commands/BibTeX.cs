namespace UserInterface.Commands
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

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
            contents = File.ReadAllText(fileName).Replace("\r\n", "\n");
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
                if (posOpenBracket == -1 || posCloseBracket == -1)
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
                    string[] authors = Authors;
                    if (authors.Length == 1)
                        return LastName(authors[0]) + ", " + Year;
                    else if (authors.Length > 1)
                        return LastName(authors[0]) + " et al., " + Year;
                    else
                        return "<No author>, " + Year;
                }
            }

            /// <summary>Return the last name of the specified author.</summary>
            /// <param name="author">The author.</param>
            /// <returns>The last name</returns>
            private string LastName(string author)
            {
                int posComma = author.IndexOf(',');
                if (posComma != -1)
                    return author.Substring(0, posComma);
                else
                    return author;
            }

            /// <summary>Gets the authors</summary>
            private string[] Authors
            {
                get
                {
                    string authorsString = Get("author");
                    return authorsString.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            /// <summary>
            /// Gets the last name of the first author.
            /// </summary>
            public string FirstAuthor
            {
                get
                {
                    return LastName(Authors[0]);
                }
            }

            /// <summary>Gets the year of publication</summary>
            public int Year
            {
                get
                {
                    string yearString = Get("year");
                    if (yearString == string.Empty)
                        return 0;
                    else
                        return Convert.ToInt32(yearString, CultureInfo.InvariantCulture);
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
                string stringToFind = keyword + " = ";
                int posKeyWord = contents.IndexOf(stringToFind, StringComparison.InvariantCultureIgnoreCase);
                if (posKeyWord == -1)
                {
                    stringToFind = keyword + "=";
                    posKeyWord = contents.IndexOf(stringToFind, StringComparison.InvariantCultureIgnoreCase);
                }                
                if (posKeyWord != -1)
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
                    text = text.Replace("\"", "");
                    return text;
                }

                return string.Empty;
            }

            /// <summary>Gets the text for the references section.</summary>
            public string BibliographyText
            {
                get
                {
                    string text;

                    string authors = StringUtilities.BuildString(Authors, ", ");

                    text = string.Format("{0}, {1}. {2}.",
                                             new object[] {
                                             authors,
                                             Year,
                                             Get("title") });

                    if (Get("series") != string.Empty)
                        text += PrefixString(Get("series"), " ") + PrefixString(Get("publisher"), ", ") +
                                             PrefixString(Get("address"), ", ") +
                                             PrefixString(Get("pages"), ", ");

                    else if (Get("institution") != string.Empty)
                        text += " " + Get("institution");

                    else if (Get("university") != string.Empty)
                        text += AppendString(Get("type"), ".") + Get("university");
                    
                    else
                    {
                        text = text + PrefixString(Get("journal") + Get("Booktitle"), " ") +
                                      PrefixString(Get("Editor"), ", Eds: ") +
                                      PrefixString(Get("volume"), " ") +
                                      PrefixString(WrapInBrackets(Get("number")), " ") +
                                      PrefixString(Get("pages"), ", ");
                    }

                    text = AppendString(text, ".");

                    return text;
                }
            }
        }

        /// <summary>Append a string if it isn't already there.</summary>
        /// <param name="st">The original string.</param>
        /// <param name="stringToAppend">The string to append.</param>
        /// <returns>The new string.</returns>
        private static string AppendString(string st, string stringToAppend)
        {
            if (st != string.Empty && !st.EndsWith(stringToAppend))
                return st + stringToAppend;
            return st;
        }

        /// <summary>Prefix a string if it isn't already there.</summary>
        /// <param name="st">The original string.</param>
        /// <param name="stringToAppend">The string to prefix.</param>
        /// <returns>The new string.</returns>
        private static string PrefixString(string st, string stringToPrefix)
        {
            if (st != string.Empty && !st.StartsWith(stringToPrefix))
                return stringToPrefix + st;
            return st;
        }

        /// <summary>Wrap a string in brackets</summary>
        /// <param name="st">The original string.</param>
        /// <returns>The new string.</returns>
        private static string WrapInBrackets(string st)
        {
            if (st != string.Empty)
                return "(" + st + ")";
            return st;
        }
        /// <summary>
        /// A private property comparer.
        /// </summary>
        public class CitationComparer : IComparer<Citation>
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            public int Compare(Citation x, Citation y)
            {
                int result = x.FirstAuthor.CompareTo(y.FirstAuthor);
                return result == 0 ? x.Year.CompareTo(y.Year) : result;
            }
        }
    }
}
