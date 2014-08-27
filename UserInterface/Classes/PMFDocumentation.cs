// -----------------------------------------------------------------------
// <copyright file="PMFDocumentation.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Xml;
    using Models.PMF;
    using System.Reflection;
    using System.Drawing;
    using Views;
    using Models.Core;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class PMFDocumentation
    {

        public class NameDescUnitsValue
        {
            public string Name = "";
            public string Description = "";
            public string Units = "";
            public string Value = "";
        }

        public static int Go(XmlDocument XML, string DocFileName)
        {
            int Code;
            string SavedDirectory = Directory.GetCurrentDirectory();

            try
            {
                if (Path.GetDirectoryName(DocFileName) != "")
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(DocFileName));

                string cssText = Properties.Resources.ResourceManager.GetString("Plant2");
                StreamWriter css = new StreamWriter("Plant2.css");
                css.WriteLine(cssText);
                css.Close();

                StreamWriter OutputFile = new StreamWriter(DocFileName);
                OutputFile.WriteLine("<html>");
                OutputFile.WriteLine("<head>");
                OutputFile.WriteLine("<meta http-equiv=\"content-type\"");
                OutputFile.WriteLine("content=\"text/html; charset=ISO-8859-1\">");
                OutputFile.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"Plant2.css\" >");
                OutputFile.WriteLine("</head>");
                OutputFile.WriteLine("<body>");
                string TitleText = "The APSIM " + Utility.Xml.Value(XML.DocumentElement, "Name") + " Module";
                OutputFile.WriteLine(Title(TitleText));
                OutputFile.WriteLine(Center(Header(TitleText, 1)));
                XmlNode image = Utility.Xml.FindByType(XML.DocumentElement, "MetaData");
                List<XmlNode> children = Utility.Xml.ChildNodes(image, "");

                OutputFile.WriteLine("<table style=\"text-align: left; width: 100%;\" border=\"1\" cellpadding=\"2\"\ncellspacing=\"2\">\n<tbody>\n<tr>\n<td id=\"toc\"style=\"vertical-align: top;\">");
                OutputFile.WriteLine("<A NAME=\"toc\"></A>");
                OutputFile.WriteLine("Table of Contents<br>"); //ToC added after the rest of the file is created. See CreateTOC()
                OutputFile.WriteLine("</td>\n<td>");
                foreach (XmlNode n in children)
                {
                    if (n.Name.Contains("Image"))
                    {
                        string s = n.InnerText;
                        s = s.Replace("%apsim%", "..\\..");
                        OutputFile.WriteLine("<img src = \"{0}\" />", s);
                    }
                }
                OutputFile.WriteLine("</td></tbody></table>");

                //XmlNode PlantNode = Utility.Xml.FindByType(XML.DocumentElement, "Model/Plant");
                DocumentNodeAndChildren(OutputFile, XML.DocumentElement, 2);

                OutputFile.WriteLine("</body>");
                OutputFile.WriteLine("</html>");
                OutputFile.Close();
                //insert TOC
                StreamReader inFile = new StreamReader(DocFileName);
                StreamReader fullFile = new StreamReader(DocFileName);
                string fullText = fullFile.ReadToEnd();
                string fileText = CreateTOC(inFile, fullText);
                inFile.Close();
                fullFile.Close();

                StreamWriter outFile = new StreamWriter(DocFileName);
                outFile.WriteLine(fileText);
                outFile.Close();
                //end insert

                Directory.SetCurrentDirectory(SavedDirectory);
                Code = 0;
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
                Code = 1;
            }
            if (SavedDirectory != "")
                Directory.SetCurrentDirectory(SavedDirectory);
            return Code;
        }

        static void DocumentNodeAndChildren(StreamWriter OutputFile, XmlNode N, int Level)
        {
            string paramTable = "";
            string Indent = new string(' ', Level * 3);
            if (N.Name.Contains("Leaf") || N.Name.Contains("Root")) //Nodes to add parameter doc to
                paramTable = DocumentParams(OutputFile, N);
            OutputFile.WriteLine(Header(Utility.Xml.Value(N, "Name"), Level, Utility.Xml.Value(N.ParentNode, "Name")));
            
            WriteDescriptionForTypeName(OutputFile, N.Name);

            OutputFile.WriteLine(ClassDescription(N));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(paramTable);

            foreach (XmlNode CN in Utility.Xml.ChildNodes(N, ""))
                DocumentNode(OutputFile, CN, Level + 1);

            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentNode(StreamWriter OutputFile, XmlNode N, int NextLevel)
        {
            

            if (Utility.Xml.Attribute(N, "shortcut") != "")
            {
                OutputFile.WriteLine("<p>" + Utility.Xml.Value(N, "Name") + " uses the same value as " + Utility.Xml.Attribute(N, "shortcut"));
            }
            else if (N.Name == "Constant")
            {
                OutputFile.WriteLine(Header(Utility.Xml.Value(N, "Name"), NextLevel, Utility.Xml.Value(N.ParentNode, "Name")));

                OutputFile.WriteLine("<p>Value = " + Utility.Xml.Value(N, "Value") + "</p>");
            }
            else if (Utility.Xml.ChildNodes(N, "").Count == 0)
                DocumentProperty(OutputFile, N, NextLevel);
            else if (Utility.Xml.ChildNodes(N, "XYPairs").Count > 0)
            {
                CreateGraph(OutputFile, Utility.Xml.ChildNodes(N, "XYPairs")[0], NextLevel);
            }
            else if (Utility.Xml.Type(N) == "TemperatureFunction")
                DocumentTemperatureFunction(OutputFile, N, NextLevel);
            //else if (Utility.Xml.Type(N) == "GenericPhase")
            //   DocumentFixedPhase(OutputFile, N, NextLevel);
            // else if (Utility.Xml.Type(N) == "PhaseLookupValue")
            //   DocumentPhaseLookupValue(OutputFile, N, NextLevel);
            else if (Utility.Xml.Type(N) == "ChillingPhase")
                ChillingPhaseFunction(OutputFile, N, NextLevel);
            else
                DocumentNodeAndChildren(OutputFile, N, NextLevel);
        }

        private static void WriteDescriptionForTypeName(StreamWriter OutputFile, string typeName)
        {
            Type[] t = Utility.Reflection.GetTypeWithoutNameSpace(typeName);
            if (t.Length == 1)
            {
                DescriptionAttribute description = Utility.Reflection.GetAttribute(t[0], typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (description != null)
                {
                    OutputFile.WriteLine("<p>" + description.ToString() + "</p>");
                }
            }
        }

        private static string DocumentParams(StreamWriter OutputFile, XmlNode N)
        {
            List<string> pList = new List<string>();
            string outerXML = N.OuterXml;
            string table = "";
            pList = ParamaterList(N);
            if (pList != null && pList.Count > 1) //some nodes will add an empty string to list. Don't render these.
            {
                table += "<table>\n<tbody>\n";
                table += "<tr style=\"font-weight: bold;\"><td>Name</td>\n<td>Value</td>\n<td>Units</td>\n<td>Description</td></tr>\n";
                foreach (string s in pList)
                {
                    string[] tag = s.Split('|');
                    if (tag.Length < 2) //handle empty strings
                        continue;
                    if (tag[1].Contains("_Frgr")) //exception for non-standard variable name
                        tag[1] = "Frgr";
                    int sIndex = outerXML.IndexOf("<" + tag[1]);
                    int eIndex = outerXML.IndexOf("</" + tag[1]);
                    if (sIndex == -1 || eIndex == -1) //didn't find tag
                        continue;
                    else
                    {
                        char[] sep = { ',', '|' };
                        string[] units = s.Split(sep);
                        if (units.Length < 3) //handle no units case
                            units[2] = "&nbsp";
                        tag = outerXML.Substring(sIndex, eIndex - sIndex).Split('>');
                        string name = tag[0].Substring(1);
                        if (name.IndexOf(' ') != -1) //some parameters have extra formatting tags, strip them if found
                            name = name.Remove(name.IndexOf(' '));
                        table += "<tr><td>" + name + "</td><td>" + tag[1] + "</td><td>" + units[2] + "</td><td>" + s.Substring(0, s.IndexOf(',') != -1 ? s.IndexOf(',') : s.IndexOf('|')) + "</td></tr>\n";
                    }

                    if (sIndex == -1 || eIndex == -1) //didn't find tag
                        continue;
                }
                table += "</table>\n</tbody>\n";
            }
            return table;
        }

        private static string CreateTOC(StreamReader fileText, string fullText)
        {
            string inject;
            List<string> headers = new List<string>();
            string curLine;
            string topLevel = "";
            headers.Add("<dl>");

            while ((curLine = fileText.ReadLine()) != null)
            {
                if (curLine.Contains("<H3>"))
                {
                    headers.Add("<dt><A HREF=\"#" + curLine.Substring(4, curLine.IndexOf("</H3>") - 4) + "\">" + curLine.Substring(4, curLine.IndexOf("</H3>")) + "</A><BR></dt>");
                    topLevel = curLine.Substring(4, curLine.IndexOf("</H3>") - 4);
                }
                else if (curLine.Contains("<H4>"))
                    headers.Add("<dd><A HREF=\"#" + topLevel + "_" + curLine.Substring(4, curLine.IndexOf("</H4>") - 4) + "\">    " + curLine.Substring(4, curLine.IndexOf("</H4>")) + "</A><BR></dd>");
            }
            headers.Add("</dl>");

            fileText.Close();
            inject = "";
            foreach (String s in headers)
            {
                inject += s + "\n";
            }
            return fullText.Insert(21 + fullText.IndexOf("Table of Contents<br>"), inject); //21 length of index string
        }

        private static void ChillingPhaseFunction(StreamWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(Utility.Xml.Value(N, "Name"), Level, Utility.Xml.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            string start = Utility.Xml.FindByType(N, "Start").InnerText;
            string end = Utility.Xml.FindByType(N, "End").InnerText;
            string CDTarget = Utility.Xml.FindByType(N, "CDTarget").InnerText;
            string text = "";
            text = Utility.Xml.Value(N, "Name") + " extends from " + start + " to " + end + " with a Chilling Days Target of " + CDTarget + " days.";
            OutputFile.WriteLine(text);
            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentProperty(StreamWriter OutputFile, XmlNode N, int Level)
        {
            string[] stages = null;
            string[] values = null;
            if (N.Name != "XProperty")
            {
                if (Utility.Xml.Value(N, "Name").Contains("Stages"))
                {
                    stages = N.InnerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    values = N.NextSibling.InnerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (Utility.Xml.Value(N, "Name").Contains("Values"))
                {
                    //processed above so skip it
                }
                else if (Utility.Xml.Value(N, "Name").Contains("Memo"))
                    OutputFile.WriteLine("<i>Note: " + N.InnerText + "</i>");
                else if (!N.ParentNode.Name.Contains("Leaf") && !N.ParentNode.Name.Contains("Root"))
                {
                    OutputFile.WriteLine("<p>" + N.Name + " = " + N.InnerText);
                }
            }

            if (stages != null)
            {
                OutputFile.WriteLine("<table>\n<tr>");
                OutputFile.WriteLine("<td>Stages</td><td>Values</td></tr>");
                for (int i = 0; i < stages.Length; i++)
                {
                    OutputFile.WriteLine("<tr><td>" + stages[i] + "</td><td>" + values[i] + "</td></tr>");
                }
                OutputFile.WriteLine("</table>");
            }
        }

        private static void DocumentFixedPhase(StreamWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(Utility.Xml.Value(N, "Name"), Level, Utility.Xml.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            string start = Utility.Xml.FindByType(N, "Start").InnerText;
            string end = Utility.Xml.FindByType(N, "End").InnerText;
            string TTT = Utility.Xml.FindByType(N, "Target").InnerText;
            string text = "";
            text = Utility.Xml.Value(N, "Name") + " extends from " + start + " to " + end + " with a fixed thermal time duration of " + TTT + " degree.days.";
            OutputFile.WriteLine(text);
            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentTemperatureFunction(StreamWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(Utility.Xml.Value(N, "Name"), Level, Utility.Xml.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            CreateGraph(OutputFile, Utility.Xml.FindByType(N, "XYPairs"), Level);
            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentPhaseLookupValue(StreamWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(Utility.Xml.Value(N, "Name"), Level, Utility.Xml.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            string start = Utility.Xml.FindByType(N, "Start").InnerText;
            string end = Utility.Xml.FindByType(N, "End").InnerText;

            string text = "The value of " + Utility.Xml.Value(N, "Name") + " during the period from " + start + " to " + end + " is calculated as follows:";
            OutputFile.WriteLine(text);
            DocumentNode(OutputFile, Utility.Xml.Find(N, "Function"), Level);
            OutputFile.WriteLine("</blockquote>");
        }

        static string ClassDescription(XmlNode Node)
        {
            object P = new Plant();
            Assembly A = Assembly.GetAssembly(P.GetType());
            Type T = A.GetType(Utility.Xml.Type(Node));
            if (T != null)
            {
                object[] Attributes = T.GetCustomAttributes(true);
                if (Attributes != null)
                {
                    String atts = null;
                    foreach (object Att in Attributes)
                    {
                        // if (Att is Description)
                        atts += Att.ToString() + "\n";
                    }
                    if (atts != null)
                        return atts;
                }
            }
            return "";
        }

        static List<string> ParamaterList(XmlNode Node)
        {
            object P = new Plant();
            Assembly A = Assembly.GetAssembly(P.GetType());
            Type T = A.GetType(Utility.Xml.Type(Node));
            List<string> paramaters = new List<string>();
            if (T == null)
                return null;

            FieldInfo[] members = T.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (MemberInfo m in members)
            {
                Object[] fieldMembers = m.GetCustomAttributes(false);
                string desc = null;
                string units = null;
                foreach (Object o in fieldMembers)
                {
                    if (o is Models.Core.DescriptionAttribute)
                    {
                        desc = o.ToString();
                    }
                    else if (o is Models.Core.UnitsAttribute)
                    {
                        units = o.ToString();
                    }
                }
                string[] split = m.ToString().Split(' ');
                paramaters.Add(desc + "|" + split[1] + "|" + units); //tag the field name to the description

            }
            if (paramaters.Count == 0)
                paramaters.Add("");

            return paramaters;
        }

        static List<NameDescUnitsValue> Outputs(XmlNode N)
        {
            List<NameDescUnitsValue> OutputList = new List<NameDescUnitsValue>();
            string Type = Utility.Xml.Type(N);
            object P = new Plant();
            Assembly A = Assembly.GetAssembly(P.GetType());
            Type T = A.GetType(Type);

            FieldInfo[] FI = T.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo F in FI)
            {
                object[] Attributes = F.GetCustomAttributes(false);
                if (Attributes != null)
                {
                    foreach (object Att in Attributes)
                    {
                        //if (Att is Output)
                        {
                            NameDescUnitsValue O = new NameDescUnitsValue();
                            O.Name = F.Name;
                            OutputList.Add(O);
                        }
                    }
                }
            }
            return OutputList;
        }

        static string Header(string text, int Level)
        {
            if (Level == 3)
            {
                return "";
            }
            else
            {
                return "<H" + Level.ToString() + ">" + text + "</H" + Level.ToString() + ">";
            }
        }

        static string Header(string text, int Level, string parent)
        {
            string blah = (Level == 3 ? "\n<br>\n" :
                  Level == 4 ? "\n<A NAME=\"" + parent + "_" + text + "\"></A>\n<br>\n" : "")
                  + "<H" + Level.ToString() + ">" + text + "</H" + Level.ToString() + ">";
            return blah;
        }
        static string Title(string text)
        {
            return "<TITLE>" + text + "</TITLE>";
        }
        static string Center(string text)
        {
            return "<CENTER>" + text + "</CENTER>";
        }
        private static void CreateGraph(StreamWriter OutputFile, XmlNode N, int NextLevel)
        {

            string InstanceName = Utility.Xml.Value(N.OwnerDocument.DocumentElement, "Name");
            string GraphName;
            if (N.Name == "XYPairs")
                GraphName = Utility.Xml.Value(N.ParentNode.ParentNode, "Name") + Utility.Xml.Value(N.ParentNode, "Name") + "Graph";
            else
                GraphName = Utility.Xml.Value(N.ParentNode.ParentNode, "Name") + Utility.Xml.Value(N, "Name") + "Graph";

            OutputFile.WriteLine(Header(Utility.Xml.Value(N.ParentNode, "Name"), NextLevel, Utility.Xml.Value(N.ParentNode, "Name")));

            WriteDescriptionForTypeName(OutputFile, N.ParentNode.Name);


            Directory.CreateDirectory(InstanceName + "Graphs");
            string GifFileName = InstanceName + "Graphs\\" + GraphName + ".gif";

            // work out x and y variable names.
            string XName = Utility.Xml.Value(N.ParentNode, "XProperty");
            if (N.ParentNode.Name == "TemperatureFunction" || N.ParentNode.Name == "AirTemperatureFunction")
                XName = "Temperature (oC)";

            string YName;
            if (N.Name == "XYPairs")
                YName = Utility.Xml.Value(N.ParentNode, "Name");
            else
                YName = Utility.Xml.Value(N, "Name");
            if (YName == "Function")
                YName = Utility.Xml.Value(N.ParentNode.ParentNode, "Name");

            // Set up to write a table.
            OutputFile.WriteLine("<table border=\"0\">");
            //   OutputFile.WriteLine("<td></td><td></td>");
            //   OutputFile.WriteLine("<tr>");

            // output xy table as a nested table.
            OutputFile.WriteLine("<td>");
            OutputFile.WriteLine("<table width=\"250\">");
            OutputFile.WriteLine("<td><b>" + XName + "</b></td><td><b>" + YName + "</b></td>");
            double[] x = Utility.Math.StringsToDoubles(Utility.Xml.Values(N, "X/double"));
            double[] y = Utility.Math.StringsToDoubles(Utility.Xml.Values(N, "Y/double"));
            for (int i = 0; i < x.Length; i++)
            {
                OutputFile.WriteLine("<tr><td>" + x[i] + "</td><td>" + y[i] + "</td></tr>");
            }

            OutputFile.WriteLine("</table>");
            OutputFile.WriteLine("</td>");

            // output chart as a column to the outer table.
            OutputFile.WriteLine("<td>");
            OutputFile.WriteLine("<img src=\"" + GifFileName + "\">");
            OutputFile.WriteLine("</td>");
            OutputFile.WriteLine("</tr>");
            OutputFile.WriteLine("</table>");

            // Setup cleanish graph.
            GraphView graph = new GraphView();
            graph.Clear();

            // Create a line series.
            graph.DrawLineAndMarkers("", x, y, Models.Graph.Axis.AxisType.Bottom, Models.Graph.Axis.AxisType.Left,
                                     Color.Blue, Models.Graph.Series.LineType.Solid, Models.Graph.Series.MarkerType.FilledCircle);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, XName, false);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, YName, false);

            // Format the title
            graph.BackColor = Color.White;

            graph.Refresh();

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(400, 400);
            graph.Export(image);
            
            image.Save(GifFileName, System.Drawing.Imaging.ImageFormat.Gif);
        }

    }
}
