namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Models.Functions;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// A class of auto-documentation methods and HTML building widgets.
    /// </summary>
    public class AutoDocumentation
    {
        private static XmlDocument doc = null;

        /// <summary>Gets the units from a declaraion.</summary>
        /// <param name="model">The model containing the declaration field.</param>
        /// <param name="fieldName">The declaration field name.</param>
        /// <returns>The units (no brackets) or any empty string.</returns>
        public static string GetUnits(IModel model, string fieldName)
        {
            if (model == null || string.IsNullOrEmpty(fieldName))
                return string.Empty;
            FieldInfo field = model.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(field, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return unitsAttribute.ToString();
            }
            
            PropertyInfo property = model.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return unitsAttribute.ToString();
            }
            // Didn't find untis - try parent.
            if (model.Parent != null)
                return GetUnits(model.Parent, model.Name);
            else
                return string.Empty;
        }

        /// <summary>Gets the description from a declaraion.</summary>
        /// <param name="model">The model containing the declaration field.</param>
        /// <param name="fieldName">The declaration field name.</param>
        /// <returns>The description or any empty string.</returns>
        public static string GetDescription(IModel model, string fieldName)
        {
            if (model == null || string.IsNullOrEmpty(fieldName))
                return string.Empty;
            FieldInfo field = model.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                DescriptionAttribute descriptionAttribute = ReflectionUtilities.GetAttribute(field, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descriptionAttribute != null)
                    return descriptionAttribute.ToString();
            }

            return string.Empty;
        }


        /// <summary>Writes the description of a class to the tags.</summary>
        /// <param name="model">The model to get documentation for.</param>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="headingLevel">The heading level to use.</param>
        /// <param name="indent">The indentation level.</param>
        /// <param name="documentAllChildren">Document all children?</param>
        /// <param name="force">
        /// Whether or not to force the generation of documentation, 
        /// regardless of the model's IncludeInDocumentation status.
        /// </param>
        public static void DocumentModel(IModel model, List<ITag> tags, int headingLevel, int indent, bool documentAllChildren = true, bool force = false)
        {
            if (model == null)
                return;
            if (force || (model.IncludeInDocumentation && model.Enabled) )
            {
                if (model is ICustomDocumentation)
                    (model as ICustomDocumentation).Document(tags, headingLevel, indent);
                else
                    DocumentModelSummary(model, tags, headingLevel, indent, documentAllChildren);
            }
        }

        /// <summary>
        /// Document the summary description of a model.
        /// </summary>
        /// <param name="model">The model to get documentation for.</param>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="headingLevel">The heading level to use.</param>
        /// <param name="indent">The indentation level.</param>
        /// <param name="documentAllChildren">Document all children?</param>
        public static void DocumentModelSummary(IModel model, List<ITag> tags, int headingLevel, int indent, bool documentAllChildren)
        {
            if (model == null)
                return;
            if (doc == null)
            {
                string fileName = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
                doc = new XmlDocument();
                doc.Load(fileName);
            }

            string nameToFindInSummary = "members/T:" + model.GetType().FullName.Replace("+", ".") + "/summary";
            XmlNode summaryNode = XmlUtilities.Find(doc.DocumentElement, nameToFindInSummary);
            if (summaryNode != null)
                ParseTextForTags(summaryNode.InnerXml, model, tags, headingLevel, indent, documentAllChildren);
        }

        /// <summary>
        /// Get the summary of a member (field, property)
        /// </summary>
        /// <param name="member">The member to get the summary for.</param>
        public static string GetSummary(MemberInfo member)
        {
            var fullName = member.ReflectedType + "." + member.Name;
            if (member is PropertyInfo)
                return GetSummary(fullName, 'P');
            else if (member is FieldInfo)
                return GetSummary(fullName, 'F');
            else if (member is EventInfo)
                return GetSummary(fullName, 'E');
            else if (member is MethodInfo method)
            {
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                return GetSummary($"{fullName}({args})", 'M');
            }
            else
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
        }

        /// <summary>
        /// Get the summary of a type removing CRLF.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummary(Type t)
        {
            return GetSummary(t.FullName, 'T');
        }

        /// <summary>
        /// Get the summary of a type without removing CRLF.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummaryRaw(Type t)
        {
            return GetSummaryRaw(t.FullName, 'T');
        }

        /// <summary>
        /// Get the remarks tag of a type (if it exists).
        /// </summary>
        /// <param name="t">The type.</param>
        public static string GetRemarks(Type t)
        {
            return GetRemarks(t.FullName, 'T');
        }

        /// <summary>
        /// Get the remarks of a member (field, property) if it exists.
        /// </summary>
        /// <param name="member">The member.</param>
        public static string GetRemarks(MemberInfo member)
        {
            var fullName = member.ReflectedType + "." + member.Name;
            if (member is PropertyInfo)
                return GetRemarks(fullName, 'P');
            else if (member is FieldInfo)
                return GetRemarks(fullName, 'F');
            else if (member is EventInfo)
                return GetRemarks(fullName, 'E');
            else if (member is MethodInfo method)
            {
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                return GetRemarks($"{fullName}({args})", 'M');
            }
            else
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
        }

        /// <summary>
        /// Get the summary of a member (class, field, property)
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        private static string GetSummary(string path, char typeLetter)
        {
            var rawSummary = GetSummaryRaw(path, typeLetter);
            if (rawSummary != null)
            {
                // Need to fix multiline comments - remove newlines and consecutive spaces.
                return Regex.Replace(rawSummary, @"\n\s+", " ");
            }
            return null;
        }

        /// <summary>
        /// Get the summary of a member (class, field, property)
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        private static string GetSummaryRaw(string path, char typeLetter)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (doc == null)
            {
                string fileName = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
                doc = new XmlDocument();
                doc.Load(fileName);
            }

            path = path.Replace("+", ".");

            string nameToFindInSummary = string.Format("members/{0}:{1}/summary", typeLetter, path);
            XmlNode summaryNode = XmlUtilities.Find(doc.DocumentElement, nameToFindInSummary);
            if (summaryNode != null)
                return summaryNode.InnerXml.Trim();
            return null;
        }

        /// <summary>
        /// Get the remarks of a member (class, field, property).
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        /// <returns></returns>
        private static string GetRemarks(string path, char typeLetter)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (doc == null)
            {
                string fileName = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
                doc = new XmlDocument();
                doc.Load(fileName);
            }

            path = path.Replace("+", ".");

            string nameToFindInSummary = string.Format("members/{0}:{1}/remarks", typeLetter, path);
            XmlNode summaryNode = XmlUtilities.Find(doc.DocumentElement, nameToFindInSummary);
            if (summaryNode != null)
            {
                // Need to fix multiline remarks - trim newlines and consecutive spaces.
                string remarks = summaryNode.InnerXml.Trim();
                return Regex.Replace(remarks, @"\n\s+", " ");
            }
            return null;
        }

        /// <summary>
        /// Parse a string into documentation tags
        /// </summary>
        /// <param name="stringToParse">The string to parse</param>
        /// <param name="model">The associated model where the string came from</param>
        /// <param name="tags">The list of tags to add to</param>
        /// <param name="headingLevel">The current heading level</param>
        /// <param name="indent">The current indent level</param>
        /// <param name="doNotTrim">If true, don't trim the lines</param>
        /// <param name="documentAllChildren">Ensure all children are documented?</param>
        public static void ParseTextForTags(string stringToParse, IModel model, List<ITag> tags, int headingLevel, int indent, bool documentAllChildren, bool doNotTrim=false)
        {
            if (string.IsNullOrEmpty(stringToParse) || model == null)
                return;
            List<IModel> childrenDocumented = new List<Core.IModel>();
            int numSpacesStartOfLine = -1;
            string paragraphSoFar = string.Empty;
            if (stringToParse.StartsWith("\r\n"))
                stringToParse = stringToParse.Remove(0, 2);
            StringReader reader = new StringReader(stringToParse);
            string line = reader.ReadLine();
            int targetHeadingLevel = headingLevel;
            while (line != null)
            {
                if (!doNotTrim)
                    line = line.Trim();

                // Adjust heading levels.
                if (line.StartsWith("#"))
                {
                    int currentHeadingLevel = line.Count(c => c == '#');
                    targetHeadingLevel = headingLevel + currentHeadingLevel - 1; // assumes models start numbering headings at 1 '#' character
                    string hashString = new string('#', targetHeadingLevel);
                    line = hashString + line.Replace("#", "") + hashString;
                }

                if (line != string.Empty && !doNotTrim)
                {
                    {
                        if (numSpacesStartOfLine == -1)
                        {
                            int preLineLength = line.Length;
                            line = line.TrimStart();
                            numSpacesStartOfLine = preLineLength - line.Length - 1;
                        }
                        else
                            line = line.Remove(0, numSpacesStartOfLine);
                    }
                }

                // Remove expression macros and replace with values.
                line = RemoveMacros(model, line);

                string heading;
                int thisHeadingLevel;
                if (GetHeadingFromLine(line, out heading, out thisHeadingLevel))
                {
                    StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);
                    tags.Add(new Heading(heading, thisHeadingLevel));
                }
                else if (line.StartsWith("[Document "))
                {
                    StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

                    // Find child
                    string childName = line.Replace("[Document ", "").Replace("]", "");
                    IModel child = model.FindByPath(childName)?.Value as IModel;
                    if (child == null)
                        paragraphSoFar += "<b>Unknown child name: " + childName + " </b>\r\n";
                    else
                    {
                        DocumentModel(child, tags, targetHeadingLevel + 1, indent);
                        childrenDocumented.Add(child);
                    }
                }
                else if (line.StartsWith("[DocumentType "))
                {
                    StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

                    // Find children
                    string childTypeName = line.Replace("[DocumentType ", "").Replace("]", "");
                    Type childType = ReflectionUtilities.GetTypeFromUnqualifiedName(childTypeName);
                    foreach (IModel child in model.FindAllChildren().Where(c => childType.IsAssignableFrom(c.GetType())))
                    {
                        DocumentModel(child, tags, targetHeadingLevel + 1, indent);
                        childrenDocumented.Add(child);
                    }
                }
                else if (line == "[DocumentView]")
                    tags.Add(new ModelView(model));
                else if (line.StartsWith("[DocumentChart "))
                {
                    StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);
                    var words = line.Replace("[DocumentChart ", "").Replace("]", "").Split(',');
                    if (words.Length == 4)
                    {
                        var xypairs = model.FindByPath(words[0])?.Value as XYPairs;
                        if (xypairs != null)
                        {
                            childrenDocumented.Add(xypairs);
                            var xName = words[2];
                            var yName = words[3];
                            tags.Add(new GraphAndTable(xypairs, words[1], xName, yName, indent));
                        }
                    }
                }
                else if (line.StartsWith("[DocumentMathFunction"))
                {
                    StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);
                    var operatorChar = line["[DocumentMathFunction".Length + 1];
                    childrenDocumented.AddRange(DocumentMathFunction(model, operatorChar, tags, headingLevel, indent));
                }

                else
                    paragraphSoFar += line + "\r\n";

                line = reader.ReadLine();
            }

            StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

            if (documentAllChildren)
            {
                // write children.
                foreach (IModel child in model.FindAllChildren<IModel>())
                {
                    if (!childrenDocumented.Contains(child))
                        DocumentModel(child, tags, headingLevel + 1, indent, documentAllChildren);
                }
            }
        }

        private static string RemoveMacros(IModel model, string line)
        {
            if (model == null || string.IsNullOrEmpty(line))
                return string.Empty;
            int posMacro = line.IndexOf('[');
            while (posMacro != -1)
            {
                int posEndMacro = line.IndexOf(']', posMacro);
                if (posEndMacro != -1)
                {
                    string macro = line.Substring(posMacro + 1, posEndMacro - posMacro - 1);
                    try
                    {
                        object value = EvaluateModelPath(model, macro);
                        if (value != null)
                        {
                            if (value is Array)
                                value = StringUtilities.Build(value as Array, Environment.NewLine);
                            
                            line = line.Remove(posMacro, posEndMacro - posMacro + 1);
                            line = line.Insert(posMacro, value.ToString());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (posMacro < line.Length)
                    posMacro = line.IndexOf('[', posMacro + 1);

                if (string.IsNullOrEmpty(line))
                    break;
            }

            return line;
        }

        /// <summary>
        /// Evaluate a path that can include child models, properties or method calls.
        /// </summary>
        /// <param name="model">The reference model.</param>
        /// <param name="path">The path to locate</param>
        private static object EvaluateModelPath(IModel model, string path)
        {
            object obj = model;
            foreach (var word in path.Split('.'))
            {
                if (obj == null)
                    return null;
                if (word.EndsWith("()"))
                {
                    // Process a method (with no arguments) call.
                    // e.g. GetType()
                    var methodName = word.Replace("()", "");
                    var method = obj.GetType().GetMethod(methodName);
                    if (method != null)
                        obj = method.Invoke(obj, null);
                }
                else if (obj is IModel && word == "Units")
                    obj = GetUnits(model, model.Name);
                else if (obj is IModel)
                {
                    // Process a child or property of a model.
                    obj = (obj as IModel).FindByPath(word, true)?.Value;
                }
                else
                {
                    // Process properties / fields of an object (not an IModel)
                    obj = ReflectionUtilities.GetValueOfFieldOrProperty(word, obj);
                }
            }

            return obj;
        }

        private static void StoreParagraphSoFarIntoTags(List<ITag> tags, int indent, ref string paragraphSoFar)
        {
            if (paragraphSoFar.Trim() != string.Empty) 
                tags.Add(new Paragraph(paragraphSoFar, indent));
            paragraphSoFar = string.Empty;
        }

        /// <summary>Look at a string and return true if it is a heading.</summary>
        /// <param name="st">The string to look at.</param>
        /// <param name="heading">The returned heading.</param>
        /// <param name="headingLevel">The returned heading level.</param>
        /// <returns></returns>
        private static bool GetHeadingFromLine(string st, out string heading, out int headingLevel)
        {
            if (string.IsNullOrEmpty(st))
            {
                heading = string.Empty;
                headingLevel = 0;
                return false;
            }

            heading = st.Replace("#", string.Empty);
            headingLevel = 0;
            if (st.StartsWith("####"))
            {
                headingLevel = 4;
                return true;
            }
            if (st.StartsWith("###"))
            {
                headingLevel = 3;
                return true;
            }
            if (st.StartsWith("##"))
            {
                headingLevel = 2;
                return true;
            }
            if (st.StartsWith("#"))
            {
                headingLevel = 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Document all child members of the specified model.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <param name="tags">Documentation elements</param>
        /// <param name="headingLevel">Heading level</param>
        /// <param name="indent">Indent level</param>
        /// <param name="childTypesToExclude">An optional list of Types to exclude from documentation.</param>
        public static void DocumentChildren(IModel model, List<AutoDocumentation.ITag> tags, int headingLevel, int indent, Type[] childTypesToExclude = null)
        {
            if (model == null)
                return;
            foreach (IModel child in model.Children)
                if (child.IncludeInDocumentation && 
                    (childTypesToExclude == null || Array.IndexOf(childTypesToExclude, child.GetType()) == -1))
                    DocumentModel(child, tags, headingLevel + 1, indent);
        }

        /// <summary>
        /// Document the mathematical function.
        /// </summary>
        /// <param name="function">The IModel function.</param>
        /// <param name="op">The operator</param>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        private static List<IModel> DocumentMathFunction(IModel function, char op,
                                                         List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // create a string to display 'child1 - child2 - child3...'
            string msg = string.Empty;
            List<IModel> childrenToDocument = new List<IModel>();
            foreach (IModel child in function.FindAllChildren<IFunction>())
            {
                if (msg != string.Empty)
                    msg += " " + op + " ";

                if (!AddChildToMsg(child, ref msg))
                    childrenToDocument.Add(child);
            }

            tags.Add(new AutoDocumentation.Paragraph("<i>" + function.Name + " = " + msg + "</i>", indent));

            // write children
            if (childrenToDocument.Count > 0)
            {
                tags.Add(new AutoDocumentation.Paragraph("Where:", indent));

                foreach (IModel child in childrenToDocument)
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }

            return childrenToDocument;
        }

        /// <summary>
        /// Return the name of the child or it's value if the name of the child is equal to 
        /// the written value of the child. i.e. if the value is 1 and the name is 'one' then
        /// return the value, instead of the name.
        /// </summary>
        /// <param name="child">The child model.</param>
        /// <param name="msg">The message to add to.</param>
        /// <returns>True if child's value was added to msg.</returns>
        private static bool AddChildToMsg(IModel child, ref string msg)
        {
            if (child is Constant)
            {
                double doubleValue = (child as Constant).FixedValue;
                if (Math.IEEERemainder(doubleValue, doubleValue) == 0)
                {
                    int intValue = Convert.ToInt32(doubleValue, CultureInfo.InvariantCulture);
                    string writtenInteger = Integer.ToWritten(intValue);
                    writtenInteger = writtenInteger.Replace(" ", "");  // don't want spaces.
                    if (writtenInteger.Equals(child.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        msg += intValue.ToString();
                        return true;
                    }
                }
            }
            else if (child is VariableReference)
            {
                msg += StringUtilities.RemoveTrailingString((child as VariableReference).VariableName, ".Value()");
                return true;
            }

            msg += child.Name;
            return false;
        }

        /// <summary>
        /// Describes an interface for a auto-doc command.
        /// </summary>
        public interface ITag
        {
        }

        /// <summary>
        /// Describes an auto-doc heading command.
        /// </summary>
        public class Heading : ITag
        {
            /// <summary>The heading text</summary>
            public string text;

            /// <summary>The heading level</summary>
            public int headingLevel;

            /// <summary>
            /// Initializes a new instance of the <see cref="Heading"/> class.
            /// </summary>
            /// <param name="text">The heading text.</param>
            /// <param name="headingLevel">The heading level.</param>
            public Heading(string text, int headingLevel)
            {
                this.text = text;
                this.headingLevel = headingLevel;
            }
        }

        /// <summary>
        /// Describes an auto-doc paragraph command.
        /// </summary>
        public class Paragraph : ITag
        {
            /// <summary>The paragraph text.</summary>
            public string text;

            /// <summary>The indent level.</summary>
            public int indent;

            /// <summary>The bookmark name (optional)</summary>
            public string bookmarkName;

            /// <summary>Should the paragraph indent all lines except the first?</summary>
            public bool handingIndent;

            /// <summary>
            /// Initializes a new instance of the <see cref="Paragraph"/> class.
            /// </summary>
            /// <param name="text">The paragraph text.</param>
            /// <param name="indent">The paragraph indent.</param>
            public Paragraph(string text, int indent)
            {
                this.text = text;
                this.indent = indent;
            }
        }

        /// <summary>Describes an auto-doc graph and table command.</summary>
        public class GraphAndTable : ITag
        {
            /// <summary>The data to show in graph and table.</summary>
            public XYPairs xyPairs;

            /// <summary>The graph title</summary>
            public string title;

            /// <summary>The x axis title.</summary>
            public string xName;

            /// <summary>The y axis title</summary>
            public string yName;

            /// <summary>The indent level.</summary>
            public int indent;

            /// <summary>
            /// Initializes a new instance of the <see cref="GraphAndTable"/> class.
            /// </summary>
            /// <param name="xyPairs">The xy pairs.</param>
            /// <param name="title">Graph title.</param>
            /// <param name="xName">The x axis title.</param>
            /// <param name="yName">The y axis title.</param>
            /// <param name="indent">The indentation.</param>
            public GraphAndTable(XYPairs xyPairs, string title, string xName, string yName, int indent)
            {
                this.title = title;
                this.xyPairs = xyPairs;
                this.xName = xName;
                this.yName = yName;
                this.indent = indent;
            }
        }

        /// <summary>Describes an auto-doc table command.</summary>
        public class Table : ITag
        {
            /// <summary>The data to show in the table.</summary>
            public DataView data;

            /// <summary>The indent level.</summary>
            public int indent;

            /// <summary>Max width of each column (in terms of number of characters).</summary>
            public int ColumnWidth { get; private set; }

            /// <summary>Max width of each column (in terms of number of characters).</summary>
            public string Style { get; private set; } = "Table";

            /// <summary>
            /// Initializes a new instance of the <see cref="Table"/> class.
            /// </summary>
            /// <param name="data">The column / row data.</param>
            /// <param name="indent">The indentation.</param>
            /// <param name="width">Max width of each column (in terms of number of characters).</param>
            /// <param name="style">The style to use for the table.</param>
            public Table(DataTable data, int indent, int width = 50, string style = "Table")
            {
                this.data = new DataView(data);
                this.indent = indent;
                this.ColumnWidth = width;
                Style = style;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Table"/> class.
            /// </summary>
            /// <param name="data">The column / row data.</param>
            /// <param name="indent">The indentation.</param>
            /// <param name="width">Max width of each column (in terms of number of characters).</param>
            /// <param name="style">The style to use for the table.</param>
            public Table(DataView data, int indent, int width = 50, string style = "Table")
            {
                this.data = data;
                this.indent = indent;
                this.ColumnWidth = width;
                Style = style;
            }
        }

        /// <summary>Descibes an image for the tags system.</summary>
        public class Image : ITag
        {
            /// <summary>The image to put into the doc.</summary>
            public System.Drawing.Image image;

            /// <summary>Unique name for image. Used to save image to temp folder.</summary>
            public string name;
        }

        /// <summary>Describes a new page for the tags system.</summary>
        public class NewPage : ITag
        {
            /// <summary>Is new page portrait?</summary>
            public bool Portrait { get; set; } = true;
        }

        /// <summary>Page setup tag.</summary>
        public class PageSetup : ITag
        {
            /// <summary>Is new page portrait?</summary>
            public bool Portrait { get; set; } = true;
        }

        /// <summary>Describes a model view for the tags system.</summary>
        public class ModelView : ITag
        {
            /// <summary>Model</summary>
            public IModel model;

            /// <summary>Constructor</summary>
            /// <param name="modelToDocument">The model to document</param>
            public ModelView(IModel modelToDocument)
            {
                model = modelToDocument;
            }
        }

    }
}
