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
    using APSIM.Shared.Utilities;

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

        public static int Go(string DocFileName, Model parentModel)
        {
            string xml = XmlUtilities.Serialise(parentModel, true);

            XmlDocument XML = new XmlDocument();
            XML.LoadXml(xml);

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
                string TitleText = "The APSIM " + XmlUtilities.Value(XML.DocumentElement, "Name") + " Module";
                OutputFile.WriteLine(Title(TitleText));
                OutputFile.WriteLine(Center(Header(TitleText, 1)));
                XmlNode image = XmlUtilities.FindByType(XML.DocumentElement, "MetaData");

                List<XmlNode> children = XmlUtilities.ChildNodes(image, "");

                OutputFile.WriteLine("<h2>Validation</h2>");
                OutputFile.WriteLine("<A HREF=\"Index.html\">Model validation can be found here.<A>");

                OutputFile.WriteLine("<table style=\"text-align: left; width: 100%;\" border=\"1\" cellpadding=\"2\"\ncellspacing=\"2\">\n<tbody>\n<tr>\n<td id=\"toc\"style=\"vertical-align: top;\">");
                OutputFile.WriteLine("<A NAME=\"toc\"></A>");
                OutputFile.WriteLine("<h2>Model Documentation</h2><br>"); //ToC added after the rest of the file is created. See CreateTOC()
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

                //XmlNode PlantNode = XmlUtilities.FindByType(XML.DocumentElement, "Model/Plant");
                DocumentNodeAndChildren(OutputFile, XML.DocumentElement, 2, parentModel);

                DocumentVariables(OutputFile, parentModel);

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

        static void DocumentNodeAndChildren(StreamWriter OutputFile, XmlNode N, int Level, Model parentModel)
        {
            string paramTable = "";
            string Indent = new string(' ', Level * 3);
            if (N.Name.Contains("Leaf") || N.Name.Contains("Root")) //Nodes to add parameter doc to
                paramTable = DocumentParams(OutputFile, N);
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));

            WriteDescriptionForTypeName(OutputFile, N, parentModel);

            OutputFile.WriteLine(ClassDescription(N));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(paramTable);

            foreach (XmlNode CN in XmlUtilities.ChildNodes(N, ""))
                DocumentNode(OutputFile, CN, Level + 1, parentModel);

            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentNode(StreamWriter OutputFile, XmlNode N, int NextLevel, Model parentModel)
        {

            if (N.Name == "Name")
                return;

            if (XmlUtilities.Attribute(N, "shortcut") != "")
            {
                OutputFile.WriteLine("<p>" + XmlUtilities.Value(N, "Name") + " uses the same value as " + XmlUtilities.Attribute(N, "shortcut"));
            }
            else if (N.Name == "Constant")
            {
                OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), NextLevel, XmlUtilities.Value(N.ParentNode, "Name")));

                WriteDescriptionForTypeName(OutputFile, N, parentModel);
                TryDocumentMemo(OutputFile, N, NextLevel);

                OutputFile.WriteLine("<p>Value = " + XmlUtilities.Value(N, "Value") + "</p>");
            }
            else if (XmlUtilities.ChildNodes(N, "").Count == 0)
            {
                WriteDescriptionForTypeName(OutputFile, N, parentModel);

                DocumentProperty(OutputFile, N, NextLevel);
            }
            else if (XmlUtilities.ChildNodes(N, "XYPairs").Count > 0)
            {
                CreateGraph(OutputFile, XmlUtilities.ChildNodes(N, "XYPairs")[0], NextLevel, parentModel);
            }
            else if (XmlUtilities.Type(N) == "TemperatureFunction")
                DocumentTemperatureFunction(OutputFile, N, NextLevel, parentModel);
            //else if (XmlUtilities.Type(N) == "GenericPhase")
            //   DocumentFixedPhase(OutputFile, N, NextLevel);
            // else if (XmlUtilities.Type(N) == "PhaseLookupValue")
            //   DocumentPhaseLookupValue(OutputFile, N, NextLevel);
            else if (XmlUtilities.Type(N) == "ChillingPhase")
                ChillingPhaseFunction(OutputFile, N, NextLevel);
            else if (N.Name == "Memo")
            {
                DocumentMemo(OutputFile, N, NextLevel);
            }
            else
            {
                string childName = XmlUtilities.Value(N, "Name");
                Model childModel = null;
                if (parentModel != null)
                    childModel = Apsim.Child(parentModel, childName) as Model;
                DocumentNodeAndChildren(OutputFile, N, NextLevel, childModel);
            }
        }

        private static void TryDocumentMemo(StreamWriter OutputFile, XmlNode N, int NextLevel)
        {
            XmlNode memo = XmlUtilities.Find(N, "Memo");
            if (memo != null)
                DocumentMemo(OutputFile, memo, NextLevel);
        }

        private static void DocumentMemo(StreamWriter OutputFile, XmlNode N, int NextLevel)
        {
            if (XmlUtilities.Value(N, "Name") != "Memo")
            {
                OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), NextLevel, XmlUtilities.Value(N.ParentNode, "Name")));
            }
            string contents = XmlUtilities.Value(N, "MemoText");
            string line;
            if (contents.Contains('<'))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(contents);
                line = XmlUtilities.Value(doc.DocumentElement, "/html/body");
            }
            else
            {
                // Maybe not xml - assume plain text.
                line = contents;
            }

            line = line.Replace("\r\n", "<br/><br/>");
            OutputFile.WriteLine(line);
        }

        private static void WriteDescriptionForTypeName(StreamWriter OutputFile, XmlNode node, Model parentModel)
        {
            if (parentModel != null)
            {
                PropertyInfo property = parentModel.GetType().GetProperty(XmlUtilities.Value(node, "Name"));
                if (property != null)
                {
                    UnitsAttribute units = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                    if (units != null)
                    {
                        OutputFile.WriteLine("<p>Units: " + units.ToString() + "</p>");
                    }

                    DescriptionAttribute description = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                    if (description != null)
                    {
                        OutputFile.WriteLine("<p>" + description.ToString() + "</p>");
                    }
                }
                else
                {
                    FieldInfo field = parentModel.GetType().GetField(XmlUtilities.Value(node, "Name"), BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        UnitsAttribute units = ReflectionUtilities.GetAttribute(field, typeof(UnitsAttribute), false) as UnitsAttribute;
                        if (units != null)
                        {
                            OutputFile.WriteLine("<p>Units: " + units.ToString() + "</p>");
                        }

                        DescriptionAttribute description = ReflectionUtilities.GetAttribute(field, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                        if (description != null)
                        {
                            OutputFile.WriteLine("<p>" + description.ToString() + "</p>");
                        }
                    }
                }
            }

            Type[] t = ReflectionUtilities.GetTypeWithoutNameSpace(node.Name, Assembly.GetExecutingAssembly());
            if (t.Length == 1)
            {
                DescriptionAttribute description = ReflectionUtilities.GetAttribute(t[0], typeof(DescriptionAttribute), false) as DescriptionAttribute;
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

            const string placeMarker = "<h2>Model Documentation</h2><br>";
            int insertPoint = placeMarker.Length + fullText.IndexOf(placeMarker);
            return fullText.Insert(insertPoint, inject); 
        }

        private static void ChillingPhaseFunction(StreamWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            string start = XmlUtilities.FindByType(N, "Start").InnerText;
            string end = XmlUtilities.FindByType(N, "End").InnerText;
            string CDTarget = XmlUtilities.FindByType(N, "CDTarget").InnerText;
            string text = "";
            text = XmlUtilities.Value(N, "Name") + " extends from " + start + " to " + end + " with a Chilling Days Target of " + CDTarget + " days.";
            OutputFile.WriteLine(text);
            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentProperty(StreamWriter OutputFile, XmlNode N, int Level)
        {
            string[] stages = null;
            string[] values = null;
            if (N.Name != "XProperty")
            {
                if (XmlUtilities.Value(N, "Name").Contains("Stages"))
                {
                    stages = N.InnerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    values = N.NextSibling.InnerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (XmlUtilities.Value(N, "Name").Contains("Values"))
                {
                    //processed above so skip it
                }
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
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            string start = XmlUtilities.FindByType(N, "Start").InnerText;
            string end = XmlUtilities.FindByType(N, "End").InnerText;
            string TTT = XmlUtilities.FindByType(N, "Target").InnerText;
            string text = "";
            text = XmlUtilities.Value(N, "Name") + " extends from " + start + " to " + end + " with a fixed thermal time duration of " + TTT + " degree.days.";
            OutputFile.WriteLine(text);
            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentTemperatureFunction(StreamWriter OutputFile, XmlNode N, int Level, Model parentModel)
        {
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            CreateGraph(OutputFile, XmlUtilities.FindByType(N, "XYPairs"), Level, parentModel);
            OutputFile.WriteLine("</blockquote>");
        }

        private static void DocumentPhaseLookupValue(StreamWriter OutputFile, XmlNode N, int Level, Model parentModel)
        {
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine("<blockquote>");
            OutputFile.WriteLine(ClassDescription(N));
            string start = XmlUtilities.FindByType(N, "Start").InnerText;
            string end = XmlUtilities.FindByType(N, "End").InnerText;

            string text = "The value of " + XmlUtilities.Value(N, "Name") + " during the period from " + start + " to " + end + " is calculated as follows:";
            OutputFile.WriteLine(text);
            DocumentNode(OutputFile, XmlUtilities.Find(N, "Function"), Level, parentModel);
            OutputFile.WriteLine("</blockquote>");
        }

        static string ClassDescription(XmlNode Node)
        {
            object P = new Plant();
            Assembly A = Assembly.GetAssembly(P.GetType());
            Type T = A.GetType(XmlUtilities.Type(Node));
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
            Type T = A.GetType(XmlUtilities.Type(Node));
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
            string Type = XmlUtilities.Type(N);
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
            string blah = (Level == 3 ? "\n<br><A NAME=\"" + text + "\"></A>\n" :
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
        private static void CreateGraph(StreamWriter OutputFile, XmlNode N, int NextLevel, Model parentModel)
        {

            string InstanceName = XmlUtilities.Value(N.OwnerDocument.DocumentElement, "Name");
            string GraphName;
            if (N.Name == "XYPairs")
                GraphName = XmlUtilities.Value(N.ParentNode.ParentNode, "Name") + XmlUtilities.Value(N.ParentNode, "Name") + "Graph";
            else
                GraphName = XmlUtilities.Value(N.ParentNode.ParentNode, "Name") + XmlUtilities.Value(N, "Name") + "Graph";

            OutputFile.WriteLine(Header(XmlUtilities.Value(N.ParentNode, "Name"), NextLevel, XmlUtilities.Value(N.ParentNode.ParentNode, "Name")));

            WriteDescriptionForTypeName(OutputFile, N.ParentNode, parentModel);
            TryDocumentMemo(OutputFile, N.ParentNode, NextLevel);


            Directory.CreateDirectory(InstanceName + "Graphs");
            string GifFileName = InstanceName + "Graphs\\" + GraphName + ".gif";

            // work out x and y variable names.
            string XName = XmlUtilities.Value(N.ParentNode, "XProperty");
            if (N.ParentNode.Name == "TemperatureFunction" || N.ParentNode.Name == "AirTemperatureFunction")
                XName = "Temperature (oC)";

            string YName;
            if (N.Name == "XYPairs")
                YName = XmlUtilities.Value(N.ParentNode, "Name");
            else
                YName = XmlUtilities.Value(N, "Name");
            if (YName == "Function")
                YName = XmlUtilities.Value(N.ParentNode.ParentNode, "Name");

            // Set up to write a table.
            OutputFile.WriteLine("<table border=\"0\">");
            //   OutputFile.WriteLine("<td></td><td></td>");
            //   OutputFile.WriteLine("<tr>");

            // output xy table as a nested table.
            OutputFile.WriteLine("<td>");
            OutputFile.WriteLine("<table width=\"250\">");
            OutputFile.WriteLine("<td><b>" + XName + "</b></td><td><b>" + YName + "</b></td>");
            double[] x = MathUtilities.StringsToDoubles(XmlUtilities.Values(N, "X/double"));
            double[] y = MathUtilities.StringsToDoubles(XmlUtilities.Values(N, "Y/double"));
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
                                     Color.Blue, Models.Graph.Series.LineType.Solid, Models.Graph.Series.MarkerType.FilledCircle, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, XName, false, double.NaN, double.NaN, double.NaN);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, YName, false, double.NaN, double.NaN, double.NaN);

            // Format the title
            graph.BackColor = Color.White;

            graph.Refresh();

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(400, 400);
            graph.Export(image);

            image.Save(GifFileName, System.Drawing.Imaging.ImageFormat.Gif);
        }


        private static void DocumentVariables(StreamWriter OutputFile, Model parentModel)
        {
            OutputFile.WriteLine(Header("Public properties", 3, null));

            OutputFile.WriteLine("<table style=\"text-align: left; valign: top;\"  border=\"1\"  >");
            OutputFile.WriteLine("<th>Name</th><th>Units</th><th>Data type</th><th>Description</th>");
            foreach (PropertyInfo property in parentModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {

                string unitsString = string.Empty;
                UnitsAttribute units = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (units != null)
                {
                    unitsString = units.ToString();
                }

                string descriptionString = string.Empty;
                DescriptionAttribute description = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (description != null)
                {
                    descriptionString = description.ToString();
                }

                OutputFile.Write("<tr>");

                OutputFile.Write("<td id=\"properties\" >" + property.Name + "</td>");
                OutputFile.Write("<td id=\"properties\" >" + unitsString + "</td>");
                OutputFile.Write("<td id=\"properties\" >" + property.PropertyType.Name + "</td>");
                OutputFile.Write("<td id=\"properties\" >" + descriptionString + "</td>");


                OutputFile.WriteLine("</tr>");
            }

            OutputFile.WriteLine("</table>");
        }

    }
}
