using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Net.Sgoliver.NRtfTree.Core;

namespace UserInterface.Classes
{
    public static class HtmlEditor
    {
        //list of control codes to ignore. The entire group will be ignored.
        private static string[] ignore = new string[] { "*", "f", "flomajor", "fhimajor", "fdbmajor", "fbimajor",
                                                        "flominor", "fhiminor", "fdbminor", "fbiminor", "stylesheet",
                                                        "fonttbl", "colortbl" };
        private static bool pntext = false;       // is this Group a \pntext control?
        private static bool lastpntext = false;   // was the last group a pntext control
        private static bool listtext = false;     // is this Group a \listtext control?
        private static bool lastlisttext = false; // was the last group a listtext control
        private static bool TableOpen = false;    // is a <table> being constructed?

        public static string RTF2HTML(string rtf)
        {
            RtfTree tree = new RtfTree();
            Stack<Group> groups = new Stack<Group>();
            RtfTreeNode root = tree.RootNode;
            StringBuilder sb = new StringBuilder();
            tree.LoadRtfFile(@"C:\temp\text.rtf");
            sb.AppendLine("<html><head />\r\n<body>\r\n<p>");

            groups.Push(new Group("root"));

            foreach (RtfTreeNode node in root.ChildNodes)
                sb = ParseNode(node, sb, groups);

            sb.Append("</body>\r\n</html>");

            System.IO.File.WriteAllText(@"C:\temp\html.html", sb.ToString());

            return sb.ToString();
        }

        private static StringBuilder ParseNode(RtfTreeNode root, StringBuilder sb, Stack<Group> groups)
        {
            // ignore unwanted controls
            if (ignore.Any(groups.Peek().name.Contains))
                return sb;

            // prepend formatting where specified in group name

            // pntext indicates numbered list. We use this instead of the more complex \pn* group.
            // Plus we add more complexity since RTF doesn't have an end of list delimiter.
            if (groups.Peek().name.Equals("pntext"))
            {
                pntext = true;
                if (Regex.Matches(sb.ToString(), "</ol>").Count == Regex.Matches(sb.ToString(), "<ol>").Count)
                    sb.AppendLine("<ol><li>");
                else
                    sb.AppendLine("<li>");
            }
            else
            {
                if (pntext == true)
                {
                    lastpntext = true;
                    pntext = false;
                }
                else
                    lastpntext = false;
            }
            if (pntext == false && lastpntext == false && Regex.Matches(sb.ToString(), "</ol>").Count != Regex.Matches(sb.ToString(), "<ol>").Count)
                sb.AppendLine("</ol>");

            // Now we do the same thing for unordered lists.
            if (groups.Peek().name.Equals("listtext"))
            {
                listtext = true;
                if (Regex.Matches(sb.ToString(), "</ul>").Count == Regex.Matches(sb.ToString(), "<ul>").Count)
                    sb.AppendLine("<ul><li>");
                else
                    sb.AppendLine("<li>");
            }
            else
            {
                if (listtext == true)
                {
                    lastlisttext = true;
                    listtext = false;
                }
                else
                    lastlisttext = false;
            }

            if (listtext == false && lastlisttext == false && Regex.Matches(sb.ToString(), "</ul>").Count != Regex.Matches(sb.ToString(), "<ul>").Count)
                sb.AppendLine("</ul>");

            RtfTreeNode node = new RtfTreeNode();

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {

                node = root.ChildNodes[i];

                if (node.NodeType == RtfNodeType.Group)
                {
                    groups.Push(new Group(node.FirstChild.NodeKey));
                    sb = ParseNode(node, sb, groups);
                }
                else if (node.NodeType == RtfNodeType.Control)
                {
                    //...
                }
                else if (node.NodeType == RtfNodeType.Keyword)
                {
                    switch (node.NodeKey)
                    {
                        case "f":  //Font type
                            //...
                            break;
                        case "cf":  //Font color
                            //...
                            break;
                        case "fs":  //Font size
                            //...
                            break;
                        case "b":
                        case "i":
                        case "sub":
                        case "strike":
                            // If key has parameter it means we're closing the format block.
                            sb.Append("<" + (node.HasParameter ? "/" : "") + node.NodeKey + ">");
                            break;
                        case "super":
                            sb.Append("<sup>");
                            break;
                        case "par":
                            sb.AppendLine("</p>\r\n<p>");
                            break;
                        case "ul":
                            sb.Append("<" + (node.HasParameter ? "/" : "") + "u>");
                            break;
                        case "ulnone":
                            sb.AppendLine("</u>");
                            break;
                    }
                }
                else if (node.NodeType == RtfNodeType.Text && !groups.Peek().name.Equals("pntext")) //don't write numbers for list; HTML will do that.
                {
                    sb.Append(node.NodeKey);
                }
            }
            // It's also valid to have formatting in the group descriptor in which case there
            // will be no closing tag. When a group has been processed, check for open tags and close them.
            if (Regex.Matches(sb.ToString(), "</b>").Count != Regex.Matches(sb.ToString(), "<b>").Count)
                sb.Append("</b>");
            if (Regex.Matches(sb.ToString(), "</i>").Count != Regex.Matches(sb.ToString(), "<i>").Count)
                sb.Append("</i>");
            if (Regex.Matches(sb.ToString(), "</u>").Count != Regex.Matches(sb.ToString(), "<u>").Count)
                sb.Append("</u>");
            if (Regex.Matches(sb.ToString(), "</sub>").Count != Regex.Matches(sb.ToString(), "<sub>").Count)
                sb.Append("</sub>");
            if (Regex.Matches(sb.ToString(), "</strike>").Count != Regex.Matches(sb.ToString(), "<strike>").Count)
                sb.Append("</strike>");
            if (Regex.Matches(sb.ToString(), "</sup>").Count != Regex.Matches(sb.ToString(), "<sup>").Count)
                sb.Append("</sup>");

            groups.Pop();
            return sb;
        }

    }

    struct Group
    {
        public string name;

        public Group(string name) { this.name = name; }
    }
}
