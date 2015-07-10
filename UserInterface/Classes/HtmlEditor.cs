using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace UserInterface.Classes
{
    public static class HtmlEditor
    {
        //list of control codes to ignore. The entire group will be ignored.
        private static string[] ignore = new string[] { "*", "f", "flomajor", "fhimajor", "fdbmajor", "fbimajor",
                                                        "flominor", "fhiminor", "fdbminor", "fbiminor", "stylesheet",
                                                        "fonttbl", "colortbl" };
        private static bool TableOpen = false;          // is a <table> being constructed?
        private static bool OrderedListOpen = false;    // is a <ol> being constructed?
        private static bool UnorderedListOpen = false;  // is a <ul> being constructed?
        private static bool Processed = false;          // has line been processed by a previous format block?

        public static string RTF2HTML(string rtf)
        {
            string[] Lines = rtf.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("<html><head />\r\n<body>\r\n<p>");

            sb = ParseRTF(Lines, sb);

            sb.Append("</body>\r\n</html>");

            System.IO.File.WriteAllText(Path.Combine(Path.GetTempPath(), "html.html"), sb.ToString());

            return sb.ToString();
        }

        private static StringBuilder ParseRTF(string[] Lines, StringBuilder sb)
        {
            for(int i=0;i<Lines.Count();i++)
            {
                Processed = false;
                //tables
                if (Lines[i].Contains("\\trowd")) //table header
                {
                    TableOpen = true;
                    sb.AppendLine("<table border=\"1\"");
                    sb.Append("<tr>");

                    MatchCollection text = Regex.Matches(Lines[i], @"\s(\w\s*)+"); 
                    foreach (Match m in text)
                        sb.Append("<td>" + m.Value + "</td>");
                    sb.AppendLine("</tr>");
                    Processed = true;
                }
                if (Lines[i].Contains("\\row") && !Lines[i].Contains("\\trowd"))
                {
                    MatchCollection text = Regex.Matches(Lines[i], @"\s(\w\s*)+");
                    foreach (Match m in text)
                        sb.Append("<td>" + m.Value + "</td>");
                    sb.Append("</tr>" + Environment.NewLine + "<tr>");
                    Processed = true;
                }
                if (TableOpen) //every row in table will end with /row. If the identifer is not found the table is ended.
                {
                    if (i == Lines.Count() - 1 || (i < Lines.Count() - 2 && !Lines[i + 1].Contains("\\row")))
                    {
                        sb.AppendLine("</table>");
                        TableOpen = false;
                    }
                }

                //ordered lists
                Regex ol = new Regex(@"\d+\.\\tab\s(\w\s*)+");
                if (ol.IsMatch(Lines[i]))
                {
                    if (!OrderedListOpen)
                    {
                        OrderedListOpen = true;
                        sb.AppendLine("<ol>");
                    }
                    string str = ol.Match(Lines[i]).Value;
                    str = str.Substring(str.IndexOf(' '), str.Length - str.IndexOf(' '));
                    sb.AppendLine("<li>" + str + "</li>");
                    Processed = true;

                    if (OrderedListOpen)
                    {
                        if (i == Lines.Count() - 1)
                        {
                            sb.AppendLine("</ol>");
                            OrderedListOpen = false;
                        }
                        else if (!ol.IsMatch(Lines[i + 1]))
                        {
                            sb.AppendLine("</ol>");
                            OrderedListOpen = false;
                        }
                    }
                }

                //unordered lists
                if (Lines[i].Contains(@"\'b7\tab")) //the rich text box doesn't use RTF tags; it just constructs a list using tabs and special chars
                {
                    Regex ul = new Regex(@"\s(\w\s*)+");
                    if (!UnorderedListOpen)
                    {
                        UnorderedListOpen = true;
                        sb.AppendLine("<ul>");
                    }
                    sb.AppendLine("<li>" + ul.Match(Lines[i]).Value + "</li>");
                    Processed = true;

                    if (UnorderedListOpen)
                    {
                        if (i == Lines.Count() - 1)
                        {
                            sb.AppendLine("</ul>");
                            UnorderedListOpen = false;
                        }
                        else if (!ul.IsMatch(Lines[i + 1]))
                        {
                            sb.AppendLine("</ul>");
                            UnorderedListOpen = false;
                        }
                    }
                }

                //bold
                Regex b = new Regex(@"\\b\s(\w\s*)+\\b");
                if (b.IsMatch(Lines[i]))
                {
                    string Text = b.Match(Lines[i]).Value;
                   // Lines[i]
                    //sb.AppendLine("<b>" + ))
                }

                //regular text
                if (!Processed)
                {
                    string Text = Regex.Replace(Lines[i], @"\\\w+|\{.*?\}|}", string.Empty);
                    sb.AppendLine("<p>" + Text.Trim() + "</p>");
                }
            }

            return sb;
        }

        private static string ProcessFormatting(string rtf)
        {
            rtf = rtf.Replace("\\b", "<b>");
            rtf = rtf.Replace("\\b0", "</b>");
            rtf = rtf.Replace("\\i", "<i>");
            rtf = rtf.Replace("\\i0", "</i>");
            rtf = rtf.Replace("\\ul", "<ul>");
            rtf = rtf.Replace("\\ulnone", "</ul>");
            rtf = rtf.Replace("\\strike", "<strike>");
            rtf = rtf.Replace("\\strike0", "</strike>");
            rtf = rtf.Replace("\\super", "<ul>");
            rtf = rtf.Replace("\\ulnone", "</ul>");
            rtf = rtf.Replace("\\ul", "<ul>");
            rtf = rtf.Replace("\\ulnone", "</ul>");
            return "";
        }
    }
}
