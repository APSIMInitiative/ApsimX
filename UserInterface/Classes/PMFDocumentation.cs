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
    using Models.PMF.Functions;
    using Models.PMF.Phen;

    /// <summary>
    /// Document a PMF model.
    /// </summary>
    public class PMFDocumentation
    {
        /// <summary>
        /// Top level model we're documenting.
        /// </summary>
        private IModel model;

        /// <summary>
        /// Have we written the cultivar heading.
        /// </summary>
        private bool writtenCultivarHeading = false;

        public class NameDescUnitsValue
        {
            public string Name = "";
            public string Description = "";
            public string Units = "";
            public string Value = "";
        }

        public int Go(TextWriter OutputFile, Model parentModel)
        {
            model = parentModel;
            string xml = XmlUtilities.Serialise(parentModel, true);

            XmlDocument XML = new XmlDocument();
            XML.LoadXml(xml);

            int Code;
            
            try
            {
                DocumentNodeAndChildren(OutputFile, XML.DocumentElement, 1);
                DocumentVariables(OutputFile);

                Code = 0;
            }
            catch (Exception E)
            {
                Console.WriteLine(E.Message);
                Code = 1;
            }

            return Code;
        }

        /// <summary>
        /// Document the specified node and all children.
        /// </summary>
        /// <param name="writer">The text writer to write to.</param>
        /// <param name="node">The XML node to write.</param>
        /// <param name="level">The HTML level e.g. 2 for H2</param>
        private void DocumentNodeAndChildren(TextWriter writer, XmlNode node, int level)
        {
            string paramTable = "";
            if (level > 1)
                writer.WriteLine(Header(XmlUtilities.Value(node, "Name"), level, XmlUtilities.Value(node.ParentNode, "Name")));

            WriteDescriptionForTypeName(writer, node);

            writer.WriteLine(ClassDescription(node));
            writer.WriteLine(paramTable);

            // Document all constants.
            //foreach (XmlNode constant in XmlUtilities.ChildNodes(node, "Constant"))
            //    DocumentConstant(writer, constant,level+1);
            
            // Document all other child nodes.
            foreach (XmlNode CN in XmlUtilities.ChildNodes(node, ""))
            {
                if (CN.Name == "Cultivar")
                {
                    if (!writtenCultivarHeading)
                    {
                        writtenCultivarHeading = true;
                        writer.WriteLine("<h2>Cultivars</h2>");
                    }
                    DocumentNode(writer, CN, 3);
                }
                else 
                    DocumentNode(writer, CN, level + 1);
            }
        }

        private void DocumentNode(TextWriter writer, XmlNode node, int NextLevel)
        {
            IModel ourModel = GetModelForNode(node);

            if (node.Name != "Name" && 
                (ourModel == null || ShouldDocument(ourModel.Parent, ourModel.Name)))
            {
                if (node.Name == "Constant")
                    DocumentConstant(writer, node, NextLevel);
                else if (XmlUtilities.ChildNodes(node, "").Count == 0)
                {
                    WriteDescriptionForTypeName(writer, node);

                    DocumentProperty(writer, node, NextLevel);
                }
                else if (XmlUtilities.ChildNodes(node, "XYPairs").Count > 0)
                    CreateGraph(writer, XmlUtilities.ChildNodes(node, "XYPairs")[0], NextLevel);
                else if (XmlUtilities.Type(node) == "TemperatureFunction")
                    DocumentTemperatureFunction(writer, node, NextLevel);
                //else if (XmlUtilities.Type(N) == "GenericPhase")
                //   DocumentFixedPhase(OutputFile, N, NextLevel);
                else if (XmlUtilities.Type(node) == "PhaseLookupValue")
                    DocumentPhaseLookupValue(writer, node, NextLevel);
                else if (XmlUtilities.Type(node) == "ChillingPhase")
                    ChillingPhaseFunction(writer, node, NextLevel);
                else if (node.Name == "Memo")
                {
                    writer.WriteLine(MemoToHTML(node, NextLevel));
                }
                else if (node.Name == "MultiplyFunction")
                    DocumentFunction(writer, node, NextLevel, "x");
                else if (node.Name == "AddFunction")
                    DocumentFunction(writer, node, NextLevel, "+");
                else if (node.Name == "DivideFunction")
                    DocumentFunction(writer, node, NextLevel, "/");
                else if (node.Name == "SubtractFunction")
                    DocumentFunction(writer, node, NextLevel, "-");
                else if (node.Name == "VariableReference")
                    DocumentVariableReference(writer, node, NextLevel);
                else if (node.Name == "OnEventFunction")
                    DocumentOnEventFunction(writer, node, NextLevel);
                else if (ourModel is Phase)
                    DocumentPhase(writer, node, NextLevel);

                else if (ourModel is IFunction)
                {
                    //writer.WriteLine(XmlUtilities.Value(node, "Name") + " is calculated as follows:</br>");
                    DocumentNodeAndChildren(writer, node, NextLevel);
                }
                else
                    DocumentNodeAndChildren(writer, node, NextLevel);
            }
        }

        private void DocumentPhase(TextWriter writer, XmlNode node, int NextLevel)
        {
            string name = XmlUtilities.Value(node, "Name");
            string start = XmlUtilities.Value(node, "Start");
            string end = XmlUtilities.Value(node, "End");

            // Look for memo
            string memo = string.Empty;
            XmlNode memoNode = XmlUtilities.Find(node, "memo");
            if (memoNode != null)
                memo = MemoToHTML(memoNode, 0);

            // Get the corresponding model.
            string desc = string.Empty;
            IModel modelForNode = GetModelForNode(node);
            DescriptionAttribute Description = ReflectionUtilities.GetAttribute(modelForNode.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;
            if (Description != null)
                desc = Description.ToString();

            writer.Write(Header(name + " Phase", NextLevel, null));
            writer.Write("<p>");
            writer.Write("The "+ name + " phase extends between the " + start + " and " + end+" stages.  ");
            writer.Write(memo);
            writer.Write(desc);
            
            writer.WriteLine("</p>");
            foreach (XmlNode child in XmlUtilities.ChildNodes(node, ""))
            {
                if (child.Name != "Start" && child.Name!= "End")
                {
                    DocumentNode(writer, child, NextLevel + 1);
                }
            }

        }

        /// <summary>Should the specified field in the specified model be documented?</summary>
        /// <param name="model">The model.</param>
        /// <param name="fieldName">Name of the field or property.</param>
        /// <returns></returns>
        private bool ShouldDocument(IModel model, string fieldName)
        {
            if (model != null)
            {
                MemberInfo field = model.GetType().GetField(fieldName);
                if (field == null)
                    field = model.GetType().GetProperty(fieldName);

                if (field != null)
                {
                    object[] attributes = field.GetCustomAttributes(typeof(DoNotDocumentAttribute), true);
                    if (attributes.Length >= 1)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Document a add, multiply, divide, subtract function.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="N"></param>
        /// <param name="NextLevel"></param>
        /// <param name="parentModel"></param>
        /// <param name="oper"></param>
        private void DocumentFunction(TextWriter writer, XmlNode N, int NextLevel, string oper)
        {
            string msg = string.Empty;
            foreach (XmlNode child in XmlUtilities.ChildNodes(N, ""))
            {
                if (msg != string.Empty)
                    msg += " " + oper +" ";
                msg += XmlUtilities.Value(child, "Name");
            }

            string name = XmlUtilities.Value(N, "Name");

            writer.Write(Header(name, NextLevel, null));
            writer.WriteLine("<p><i>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + name + " = " + msg + "</i></p>");
            
            foreach (XmlNode child in XmlUtilities.ChildNodes(N, ""))
            {
                string childName = XmlUtilities.Value(child, "Name");
                if (childName != string.Empty)
                {
                    DocumentNode(writer, child, NextLevel + 1);
                }
            }
        }

        /// <summary>
        /// Document a constant node.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="constantNode"></param>
        private void DocumentConstant(TextWriter writer, XmlNode constantNode, int NextLevel)
        {
            string name = XmlUtilities.Value(constantNode, "Name");
            string value = XmlUtilities.Value(constantNode, "Value");

            // Get the corresponding model.
            IModel modelForNode = GetModelForNode(constantNode);

            // Look for units.
            string units = string.Empty;
            FieldInfo property = modelForNode.Parent.GetType().GetField(name, BindingFlags.NonPublic| BindingFlags.Instance);
            if (property != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    units = "(" + unitsAttribute.ToString() + ")";
            }
            else
            {
                units = XmlUtilities.Value(constantNode, "Units");
            }

            // Look for memo
            string memo = string.Empty;
            XmlNode memoNode = XmlUtilities.Find(constantNode, "memo");
            if (memoNode != null)
                memo = MemoToHTML(memoNode, 0);

            
            writer.Write(Header(name,NextLevel,null));
            writer.Write("<p>");
            writer.Write(memo);
            writer.Write(name + " is given a constant value of " + value + " "+ units);
            writer.WriteLine("</p>");
        }
        /// <summary>
        /// Document a variable reference.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="constantNode"></param>
        /// <param name="NextLevel""></param>
        private void DocumentVariableReference(TextWriter writer, XmlNode constantNode, int NextLevel)
        {
            string name = XmlUtilities.Value(constantNode, "Name");
            string variablename = XmlUtilities.Value(constantNode, "VariableName");

            // Get the corresponding model.
            IModel modelForNode = GetModelForNode(constantNode);

            // Look for units.
            string units = string.Empty;
            PropertyInfo property = modelForNode.GetType().GetProperty(XmlUtilities.Value(constantNode, "Name"));
            if (property != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    units = "(" + unitsAttribute.ToString() + ")";
            }

            // Look for memo
            string memo = string.Empty;
            XmlNode memoNode = XmlUtilities.Find(constantNode, "memo");
            if (memoNode != null)
                memo = MemoToHTML(memoNode, 0);


            writer.Write(Header(name, NextLevel, null));
            writer.Write("<p>");
            writer.Write(memo);
            writer.Write(name + units + " in this function uses the value given by " + variablename);
            writer.WriteLine("</p>");
        }
        /// <summary>
        /// Document an OnEvent Function.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="constantNode"></param>
        /// <param name="NextLevel""></param>
        private void DocumentOnEventFunction(TextWriter writer, XmlNode Node, int NextLevel)
        {
            string name = XmlUtilities.Value(Node, "Name");
            string setevent = XmlUtilities.Value(Node, "SetEvent");
            string resetevent = XmlUtilities.Value(Node, "ResetEvent");

            // Get the corresponding model.
            IModel modelForNode = GetModelForNode(Node);

            // Look for units.
            string units = string.Empty;
            PropertyInfo property = modelForNode.GetType().GetProperty(XmlUtilities.Value(Node, "Name"));
            if (property != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    units = "(" + unitsAttribute.ToString() + ")";
            }

            // Look for memo
            string memo = string.Empty;
            XmlNode memoNode = XmlUtilities.Find(Node, "memo");
            if (memoNode != null)
                memo = MemoToHTML(memoNode, 0);


            writer.Write(Header(name, NextLevel, null));
            writer.Write("<p>");
            writer.Write(memo);
            writer.Write(name + units + " is set to <i>PostEventValue</i> in response to a "+setevent+" event.  ");
            writer.Write("Prior to this it is set to a <i>PreEventValue</i>.");
            if (resetevent!="never")
                writer.Write("The value is reset in response to a " + resetevent + " event.</br>");
            
            writer.WriteLine("</p>");
            foreach (XmlNode child in XmlUtilities.ChildNodes(Node, ""))
            {
                string childName = XmlUtilities.Value(child, "Name");
                if (childName != string.Empty)
                {
                    DocumentNode(writer, child, NextLevel + 1);
                }
            }

        }

        /// <summary>
        /// Get a model for a given xml node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The model or null if not found.</returns>
        private IModel GetModelForNode(XmlNode node)
        {
            string path = XmlUtilities.FullPathUsingName(node).Replace("/", ".");

            // remove trailing slash.
            if (path.Length > 0 || path[path.Length - 1] == '.')
                path = path.Remove(path.Length - 1);

            // remove the crop name at the beginning e.g. 'Maize.'
            string prefix = model.Name + ".";
            if (path.StartsWith(prefix))
                path = path.Remove(0, prefix.Length);
            return Apsim.Get(model, path) as IModel;
            //return Apsim.Find(model, path);
        }

        private void TryDocumentMemo(TextWriter OutputFile, XmlNode N, int NextLevel)
        {
            XmlNode memo = XmlUtilities.Find(N, "Memo");
            if (memo != null)
                OutputFile.WriteLine(MemoToHTML(memo, NextLevel));
        }

        private string MemoToHTML(XmlNode N, int NextLevel)
        {
            string html = string.Empty;
            if (XmlUtilities.Value(N, "Name") != "Memo")
            {
                html += Header(XmlUtilities.Value(N, "Name"), NextLevel, XmlUtilities.Value(N.ParentNode, "Name"));
            }
            string contents = XmlUtilities.Value(N, "MemoText");
            if (contents.Contains('<'))
            {
                html = contents.Replace("<html><body>", "");
                html = html.Replace("</body></html>", "");
            }
            else
            {
                // Maybe not xml - assume plain text.
                html = contents;
            }

            //html = html.Replace("\r\n", "");
            
            return html;
        }

        private void WriteDescriptionForTypeName(TextWriter OutputFile, XmlNode node)
        {
            // Get the corresponding model.
            IModel modelForNode = GetModelForNode(node);

            if (modelForNode != null)
            {
                PropertyInfo property = modelForNode.GetType().GetProperty(XmlUtilities.Value(node, "Name"));
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
                    FieldInfo field = modelForNode.GetType().GetField(XmlUtilities.Value(node, "Name"), BindingFlags.NonPublic | BindingFlags.Instance);
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

        private string DocumentParams(TextWriter OutputFile, XmlNode N)
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


        private void ChillingPhaseFunction(TextWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine(ClassDescription(N));
            string start = XmlUtilities.FindByType(N, "Start").InnerText;
            string end = XmlUtilities.FindByType(N, "End").InnerText;
            string CDTarget = XmlUtilities.FindByType(N, "CDTarget").InnerText;
            string text = "";
            text = XmlUtilities.Value(N, "Name") + " extends from " + start + " to " + end + " with a Chilling Days Target of " + CDTarget + " days.";
            OutputFile.WriteLine(text);
        }

        private void DocumentProperty(TextWriter OutputFile, XmlNode N, int Level)
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

        private void DocumentFixedPhase(TextWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine(ClassDescription(N));
            string start = XmlUtilities.FindByType(N, "Start").InnerText;
            string end = XmlUtilities.FindByType(N, "End").InnerText;
            string TTT = XmlUtilities.FindByType(N, "Target").InnerText;
            string text = "";
            text = XmlUtilities.Value(N, "Name") + " extends from " + start + " to " + end + " with a fixed thermal time duration of " + TTT + " degree.days.";
            OutputFile.WriteLine(text);
        }

        private void DocumentTemperatureFunction(TextWriter OutputFile, XmlNode node, int Level)
        {
             OutputFile.WriteLine(Header(XmlUtilities.Value(node, "Name"), Level, XmlUtilities.Value(node.ParentNode, "Name")));
            OutputFile.WriteLine(ClassDescription(node));
            CreateGraph(OutputFile, XmlUtilities.FindByType(node, "XYPairs"), Level);
        }

        private void DocumentPhaseLookupValue(TextWriter OutputFile, XmlNode N, int Level)
        {
            OutputFile.WriteLine(Header(XmlUtilities.Value(N, "Name"), Level, XmlUtilities.Value(N.ParentNode, "Name")));
            OutputFile.WriteLine(ClassDescription(N));
            string start = XmlUtilities.FindByType(N, "Start").InnerText;
            string end = XmlUtilities.FindByType(N, "End").InnerText;

            string text = "The value for "+ XmlUtilities.Value(N, "Name") + " (<i>i.e.</i> from " + start + " to " + end + ") is calculated as follows:";
            OutputFile.WriteLine(text);
            foreach (XmlNode child in XmlUtilities.ChildNodes(N, ""))
            {
                if (XmlUtilities.ChildNodes(child, "").Count > 0)
                {
                    string childName = XmlUtilities.Value(child, "Name");
                    DocumentNode(OutputFile, child, Level);
                }
            }
        }

        private string ClassDescription(XmlNode Node)
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

        private List<string> ParamaterList(XmlNode Node)
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

        private List<NameDescUnitsValue> Outputs(XmlNode N)
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

        private string Header(string text, int Level, string parent)
        {
            return "<H" + Level.ToString() + ">" + text + "</H" + Level.ToString() + ">";
        }
        private string Title(string text)
        {
            return "<TITLE>" + text + "</TITLE>";
        }
        private string Center(string text)
        {
            return "<CENTER>" + text + "</CENTER>";
        }
        private void CreateGraph(TextWriter OutputFile, XmlNode node, int NextLevel)
        {
            string name = XmlUtilities.Value(node.ParentNode, "Name");
            OutputFile.WriteLine(name + " is calculated as follows:");
            OutputFile.Write("</br>");
            string InstanceName = XmlUtilities.Value(node.OwnerDocument.DocumentElement, "Name");
            string GraphName;
            if (node.Name == "XYPairs")
                GraphName = XmlUtilities.Value(node.ParentNode.ParentNode, "Name") + XmlUtilities.Value(node.ParentNode, "Name") + "Graph";
            else
                GraphName = XmlUtilities.Value(node.ParentNode.ParentNode, "Name") + XmlUtilities.Value(node, "Name") + "Graph";

            OutputFile.WriteLine(Header(XmlUtilities.Value(node.ParentNode, "Name"), NextLevel, XmlUtilities.Value(node.ParentNode.ParentNode, "Name")));

            WriteDescriptionForTypeName(OutputFile, node.ParentNode);
            TryDocumentMemo(OutputFile, node.ParentNode, NextLevel);


            Directory.CreateDirectory(InstanceName + "Graphs");
            string GifFileName = InstanceName + "Graphs\\" + GraphName + ".gif";

            // work out x and y variable names.
            string XName = XmlUtilities.Value(node.ParentNode, "XProperty");
            if (node.ParentNode.Name == "TemperatureFunction" || node.ParentNode.Name == "AirTemperatureFunction")
                XName = "Temperature (oC)";

            string YName;
            if (node.Name == "XYPairs")
                YName = XmlUtilities.Value(node.ParentNode, "Name");
            else
                YName = XmlUtilities.Value(node, "Name");
            if (YName == "Function")
                YName = XmlUtilities.Value(node.ParentNode.ParentNode, "Name");

            // Set up to write a table.
            OutputFile.WriteLine("<table border=\"0\">");

            // output xy table as a nested table.
            OutputFile.WriteLine("<td>");
            OutputFile.WriteLine("<table width=\"250\">");
            OutputFile.WriteLine("<td><b>" + XName + "</b></td><td><b>" + YName + "</b></td>");
            double[] x = MathUtilities.StringsToDoubles(XmlUtilities.Values(node, "X/double"));
            double[] y = MathUtilities.StringsToDoubles(XmlUtilities.Values(node, "Y/double"));
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
                                     Color.Blue, Models.Graph.Series.LineType.Solid, Models.Graph.Series.MarkerType.None, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, XName, false, double.NaN, double.NaN, double.NaN);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, YName, false, double.NaN, double.NaN, double.NaN);

            // Format the title
            graph.BackColor = Color.White;

            graph.Refresh();

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(350, 350);
            graph.Export(image);

            image.Save(GifFileName, System.Drawing.Imaging.ImageFormat.Gif);
        }


        private void DocumentVariables(TextWriter OutputFile)
        {
            OutputFile.WriteLine(Header("Public properties", 3, null));

            OutputFile.WriteLine("<table style=\"text-align: left; valign: top;\"  border=\"1\"  >");
            OutputFile.WriteLine("<th>Name</th><th>Units</th><th>Data type</th><th>Description</th>");
            foreach (PropertyInfo property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
