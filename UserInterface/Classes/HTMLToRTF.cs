// -----------------------------------------------------------------------
// <copyright file="HTMLToRTF.cs"  company="APSIM Initiative">
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
    public class HTMLToRTF
    {
        /// <summary>
        /// Converts basic HTML to RTF.
        /// </summary>
        /// <param name="HTML">The HTML to convert.</param>
        /// <returns>The RTF.</returns>
        public static string Convert(string HTML)
        {
            List<string> htmlParsed = ParseHTML(HTML);

            string rtf = @"{\rtf1\ansi ";

            foreach (string section in htmlParsed)
            {
                if (section == "<b>")
                    rtf += @"\b ";
                else if (section == "<i>")
                    rtf += @"\i ";
                else if (section == "<ul>")
                    rtf += @"\ul ";
                else if (section == "<strike>")
                    rtf += @"\strike ";
                else if (section == "<sup>")
                    rtf += @"\up9 ";
                else if (section == "<sub>")
                    rtf += @"\dn9 ";
                else if (section == "<h1>")
                    rtf += @"\fs32 ";
                else if (section == "<h2>")
                    rtf += @"\fs28 ";
                else if (section == "<h3>")
                    rtf += @"\fs24 ";
                else if (section == "</b>")
                    rtf += @"\b0";
                else if (section == "</i>")
                    rtf += @"\i0";
                else if (section == "</ul>")
                    rtf += @"\ulnone ";
                else if (section == "</strike>")
                    rtf += @"\strike0";
                else if (section == "</sup>")
                    rtf += @"\up0";
                else if (section == "</sub>")
                    rtf += @"\up0";
                else if (section == "</h1>")
                    rtf += @"\fs20\par ";
                else if (section == "</h2>")
                    rtf += @"\fs20 ";
                else if (section == "</h3>")
                    rtf += @"\fs20 ";
                else if (section == "<br/>")
                    rtf += @"\par ";
                else
                    rtf += section;
            }
            rtf += "}";
            return rtf;
        }

        /// <summary>
        /// Parses the HTML for tags we're interested.
        /// </summary>
        /// <param name="strHTML">The HTML.</param>
        /// <returns>A list of HTML bits we're interested in.</returns>
        private static List<string> ParseHTML(string strHTML)
        {
            string[] tagsToKeep = {"<b>", "</b>", "<i>", "</i>", "<ul>", "</ul>",
                                   "<strike>", "</strike>", "<sup>", "</sup>",
                                   "<sub>", "</sub>",
                                   "<h1>", "</h1>", "<h2>", "</h2>", "<h3>", "</h3>",
                                   "<br/>"};


            List<string> htmlParsed = new List<string>();

            // process text
            int nStart = 0;
            while (nStart < strHTML.Length)
            {
                // looking for start tags
                int pos = strHTML.IndexOf('<', nStart);
                if (nStart >= 0)
                {
                    if (pos > nStart)
                    {
                        // tag is not the first character, so
                        // we need to add text to control and continue
                        // looking for tags at the begining of the text

                        string strData = strHTML.Substring(nStart, pos - nStart);
                        strData = strData.Replace("&amp;", "&");
                        strData = strData.Replace("&lt;", "<");
                        strData = strData.Replace("&gt;", ">");
                        strData = strData.Replace("&apos;", "'");
                        strData = strData.Replace("&quot;", "\"");
                        strData = strData.Replace("\r\n", "");
                        if (strData != string.Empty)
                            htmlParsed.Add(strData);
                        nStart = pos;
                    }

                    // ok, get tag value
                    int nEnd = strHTML.IndexOf('>', nStart);
                    if (nEnd > nStart)
                    {
                        if (nEnd - nStart > 0)
                        {
                            string strTag = strHTML.Substring(nStart, nEnd - nStart + 1);
                            strTag = strTag.ToLower();
                            if (strTag.StartsWith("<a"))
                            {
                                // hyperlink
                                int poshref = strTag.IndexOf("href=");
                                if (poshref != -1)
                                {
                                    poshref += 6;
                                    int posQuote = strTag.IndexOf('"', poshref);
                                    if (posQuote != -1)
                                    {
                                        string url = strTag.Substring(poshref, posQuote - poshref);
                                        htmlParsed.Add(url);
                                        nEnd = strHTML.IndexOf("</a>", posQuote);
                                        if (nEnd != -1)
                                            nEnd += 3;
                                    }
                                }
                            }
                            else if (Array.IndexOf(tagsToKeep, strTag) != -1)
                                htmlParsed.Add(strTag);
                        }
                        nStart = nEnd + 1;
                    }
                }
            }
            return htmlParsed;
        }

    }
}
