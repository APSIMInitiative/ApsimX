// -----------------------------------------------------------------------
// <copyright file="RTFToHTML.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class RTFToHTML
    {
        /// <summary>
        /// Converts RTF to HTML.
        /// </summary>
        /// <param name="RTF">The RTF.</param>
        /// <returns>The HTML</returns>
        public static string Convert(string RTF)
        {
            List<string> RTFParsed = ParseRTF(RTF);

            // Create HTML from our list of RTF bits.
            StringBuilder HTML = new StringBuilder("\r\n<html><body>\r\n");

            string style = string.Empty;
            bool superscript = false;
            foreach (string code in RTFParsed)
            {
                if (code == "fs32") { HTML.Append("<h1>"); style = "h1"; }
                else if (code == "fs28") { HTML.Append("<h2>"); style = "h2"; }
                else if (code == "fs24") { HTML.Append("<h3>"); style = "h3"; }
                else if (code == "fs20")
                {
                    if (style != string.Empty)
                    {
                        HTML.Append("</" + style + ">\r\n");
                        style = string.Empty;
                    }

                }
                else if (code == "par")
                {
                    HTML.Append("<br/>\r\n");
                }
                else if (code == "b") HTML.Append("<b>");
                else if (code == "b0") HTML.Append("</b>");
                else if (code == "i") HTML.Append("<i>");
                else if (code == "i0") HTML.Append("</i>");
                else if (code == "ul") HTML.Append("<u>");
                else if (code == "ulnone") HTML.Append("</u>");
                else if (code == "strike") HTML.Append("<strike>");
                else if (code == "strike0") HTML.Append("</strike>");
                else if (code == "up9") { HTML.Append("<sup>"); superscript = true; }
                else if (code == "dn9") { HTML.Append("<sub>"); }
                else if (code == "up0")
                {
                    if (superscript)
                    { HTML.Append("</sup>"); superscript = false; }
                    else
                    { HTML.Append("</sub>"); }
                }
                else if (code.StartsWith("http://"))
                    HTML.Append("<a href=\"" + code + "\">" + code + "</a>");
                else
                    HTML.Append(code);
            }

            // Make sure we close any outstanding styles.
            if (style != string.Empty)
            {
                HTML.Append("</" + style + ">\r\n");
                style = string.Empty;
            }

            HTML.Append("\r\n</body></html>");

            return HTML.ToString();
        }

        /// <summary>
        /// Parses the RTF, returning a list of all RTF codes we're interested in.
        /// </summary>
        /// <param name="RTF">The RTF to parse.</param>
        /// <returns>The list of RTF codes.</returns>
        private static List<string> ParseRTF(string RTF)
        {
            List<string> RTFParsed = new List<string>();

            // parse RTF
            string text = string.Empty;
            int pos = 1;
            while (pos < RTF.Length)
            {
                if (RTF[pos] == '\\')
                {
                    if (text != string.Empty)
                    {
                        AddRTFItem(text, ref RTFParsed);
                        text = string.Empty;
                    }

                    ParseControlCode(RTF, ref pos, ref RTFParsed);
                }

                else if (RTF[pos] != '\r' && RTF[pos] != '\n' && RTF[pos] != '{' && RTF[pos] != '}')
                {
                    text += RTF[pos];
                    pos++;
                }
                else
                    pos++;
            }

            if (text != string.Empty)
                AddRTFItem(text, ref RTFParsed);


            return RTFParsed;
        }

        /// <summary>
        /// Adds the specified RTF item to the list of items.
        /// </summary>
        /// <param name="RTFItem">The RTF item.</param>
        /// <param name="RTFParsed">The RTF items list</param>
        private static void AddRTFItem(string RTFItem, ref List<string> RTFParsed)
        {
            int posURL = RTFItem.IndexOf("http://");
            if (posURL != -1)
            {
                while (posURL != -1)
                {
                    int posEndURL = RTFItem.IndexOfAny(" \r\n!@#$%^&*()-_<>".ToCharArray(), posURL);
                    if (posEndURL == -1)
                        posEndURL = RTFItem.Length;

                    string beforeURL = RTFItem.Substring(0, posURL);
                    string url = RTFItem.Substring(posURL, posEndURL - posURL);
                    string afterURL = RTFItem.Substring(posEndURL);

                    if (beforeURL != string.Empty)
                        RTFParsed.Add(beforeURL);
                    RTFParsed.Add(url);
                    if (afterURL != string.Empty)
                        RTFParsed.Add(afterURL);

                    posURL = RTFItem.IndexOf("http://", posURL + 1);
                }
            }
            else
                RTFParsed.Add(RTFItem);
        }

        /// <summary>
        /// Parse an RTF control code adding to HTML as necessary.
        /// </summary>
        /// <param name="RTF">The RTF to scan.</param>
        /// <param name="pos">The current position in RTF.</param>
        /// <param name="HTML">The HTML that we're building.</param>
        private static void ParseControlCode(string RTF, ref int pos, ref List<string> RTFCodes)
        {
            string[] codesToKeep = {"b", "b0", "i", "i0",
                                    "ul", "ulnone", "strike", "strike0",
                                    "up9", "dn9", "up0",
                                    "fs32", "fs28", "fs24", "fs20",
                                    "par"};

            string code = string.Empty;
            pos++;

            // process text
            int numOpenBrackets = 0;
            while (pos < RTF.Length && (numOpenBrackets > 0 || 
                                        (!Char.IsWhiteSpace(RTF[pos]) && RTF[pos] != '\\')))
            {
                if (RTF[pos] == '{')
                    numOpenBrackets++;
                else if (RTF[pos] == '}')
                    numOpenBrackets--;
                else if (RTF[pos] != '\r' && RTF[pos] != '\n')
                    code += RTF[pos];

                pos++;
            }

            if (Array.IndexOf(codesToKeep, code) != -1)
            {
                if (code == "par" && RTFCodes[RTFCodes.Count - 1] == "fs20")
                {
                    // Skip the par - associated with a heading.
                }
                else
                    RTFCodes.Add(code);
            }

            if (pos < RTF.Length && RTF[pos] == ' ')
                pos++; // skip past the space.
        }

    }
}
