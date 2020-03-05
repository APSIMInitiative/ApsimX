namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Models.Functions;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
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
            FieldInfo field = model.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(field, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return unitsAttribute.ToString();
            }

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
            else
                return GetSummary(fullName, 'M');
        }

        /// <summary>
        /// Get the summary of a type.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummary(Type t)
        {
            return GetSummary(t.FullName, 'T');
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
            else
                return GetRemarks(fullName, 'M');
        }

        /// <summary>
        /// Get the summary of a member (class, field, property)
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        private static string GetSummary(string path, char typeLetter)
        {
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
                return summaryNode.InnerXml;
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
                return summaryNode.InnerXml;
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
                    IModel child = Apsim.Get(model, childName) as IModel;
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
                    foreach (IModel child in Apsim.Children(model, childType))
                    {
                        DocumentModel(child, tags, targetHeadingLevel + 1, indent);
                        childrenDocumented.Add(child);
                    }
                }
                else if (line == "[DocumentView]")
                    tags.Add(new ModelView(model));
                else
                    paragraphSoFar += line + "\r\n";

                line = reader.ReadLine();
            }

            StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

            if (documentAllChildren)
            {
                // write children.
                foreach (IModel child in Apsim.Children(model, typeof(IModel)))
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
                        object value = Apsim.Get(model, macro, true);
                        if (value != null)
                        {
                            line = line.Remove(posMacro, posEndMacro - posMacro + 1);
                            line = line.Insert(posMacro, value.ToString());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                posMacro = line.IndexOf('[', posMacro + 1);
            }

            return line;
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

            /// <summary>
            /// Initializes a new instance of the <see cref="Table"/> class.
            /// </summary>
            /// <param name="data">The column / row data.</param>
            /// <param name="indent">The indentation.</param>
            /// <param name="width">Max width of each column (in terms of number of characters).</param>
            public Table(DataTable data, int indent, int width = 50)
            {
                this.data = new DataView(data);
                this.indent = indent;
                this.ColumnWidth = width;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Table"/> class.
            /// </summary>
            /// <param name="data">The column / row data.</param>
            /// <param name="indent">The indentation.</param>
            /// <param name="width">Max width of each column (in terms of number of characters).</param>
            public Table(DataView data, int indent, int width = 50)
            {
                this.data = data;
                this.indent = indent;
                this.ColumnWidth = width;
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
