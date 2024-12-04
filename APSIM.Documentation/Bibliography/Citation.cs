using System;
using System.Globalization;
using System.Text;

namespace APSIM.Documentation.Bibliography
{
    /// <summary>
    /// A citation in a bibliography.
    /// </summary>
    public class Citation : ICitation
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
            BibliographyText = GetBibliographyText();
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
                    return $"{LastName(authors[0])}, {Year}";
                else if (authors.Length > 1)
                    return $"{LastName(authors[0])} et al., {Year}";
                else
                    return $"<No author>, {Year}";
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
                if (Authors.Length > 0)
                    return LastName(Authors[0]);
                else
                    return "";
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

        /// <summary>
        /// The full text of the citation, as it will appear in the bibliography.
        /// </summary>
        public string BibliographyText { get; private set; }

        /// <summary>Gets the text for the references section.</summary>
        private string GetBibliographyText()
        {
            StringBuilder text = new StringBuilder();

            string authors = string.Join(", ", Authors);

            text.Append(string.Format("{0}, {1}. {2}.",
                                        new object[] {
                                        authors,
                                        Year,
                                        Get("title") }));

            if (Get("series") != string.Empty)
                text.Append(PrefixString(Get("series"), " ") + PrefixString(Get("publisher"), ", ") +
                                        PrefixString(Get("address"), ", ") +
                                        PrefixString(Get("pages"), ", "));

            else if (Get("institution") != string.Empty)
                text.Append($" {Get("institution")}");

            else if (Get("university") != string.Empty)
                text.Append(AppendString(Get("type"), ".") + Get("university"));
            
            else
            {
                text.Append(PrefixString(Get("journal") + Get("Booktitle"), " "));
                text.Append(PrefixString(Get("Editor"), ", Eds: "));
                text.Append(PrefixString(Get("volume"), " "));
                text.Append(PrefixString(WrapInBrackets(Get("number")), " "));
                text.Append(PrefixString(Get("pages"), ", "));
            }

            text.Append(".");

            return text.ToString();
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

            var match = System.Text.RegularExpressions.Regex.Match(contents, $@"{keyword}\s*=\s*{{([^}}]+)}}");
            if (match.Groups.Count == 2)
                return match.Groups[1].ToString();
            return string.Empty;
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
        /// <param name="stringToPrefix">The string to prefix.</param>
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
    }
}
