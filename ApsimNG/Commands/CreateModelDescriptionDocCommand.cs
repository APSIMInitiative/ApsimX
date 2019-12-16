

namespace UserInterface.Commands
{
    using Models.Core;
    using System.IO;
    using System;
    using APSIM.Shared.Utilities;
    using System.Reflection;
    using System.Data;
    using System.Xml;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System.Linq;
    using Presenters;
    using System.Xml.Xsl;
    using System.Diagnostics;
    using Models.Core.Run;
    using ApsimNG.Classes;

    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    public class CreateModelDescriptionDocCommand : ICommand
    {
        /// <summary>The maximum length of a description.</summary>
        private const int maxDescriptionLength = 60;

        /// <summary>The maximum length of a type name.</summary>
        private const int maxTypeLength = 30;

        /// <summary>The main form.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The model to document.</summary>
        private IModel modelToDocument;

        /// <summary>A list of types to document.</summary>
        private List<Type> typesToDocument = new List<Type>();

        /// <summary>Only document types in this namespace.</summary>
        private string namespaceToDocument;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocCommand"/> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        /// <param name="model">The model to document.</param>
        public CreateModelDescriptionDocCommand(ExplorerPresenter explorerPresenter, IModel model)
        {
            this.explorerPresenter = explorerPresenter;
            this.modelToDocument = model;

            typesToDocument.Add(modelToDocument.GetType());
            namespaceToDocument = modelToDocument.GetType().Namespace;

            FileNameWritten = Path.ChangeExtension(explorerPresenter.ApsimXFile.FileName, ".description.pdf");
        }

        /// <summary>The name of the file written.</summary>
        public string FileNameWritten { get; }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory commandHistory)
        {
            // Get a list of tags for each type.
            var tags = new List<AutoDocumentation.ITag>();
            for (int i = 0; i < typesToDocument.Count; i++)
                tags.AddRange(DocumentType(typesToDocument[i]));

            // Convert the list of models into a list of tags.
            var pdfWriter = new PDFWriter(explorerPresenter, portraitOrientation:false);
            pdfWriter.CreatePDF(tags, FileNameWritten);
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="type">The type to document.</param>
        private List<AutoDocumentation.ITag> DocumentType(Type type)
        {
            var tags = new List<AutoDocumentation.ITag>();

            tags.Add(new AutoDocumentation.Heading(type.Name, 1));

            AutoDocumentation.ParseTextForTags(AutoDocumentation.GetSummary(type), modelToDocument, tags, 1, 0,false);

            var outputs = GetOutputs(type);
            if (outputs != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Properties (Outputs)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(outputs) { Sort = "Name asc" }, 2));
            }
            
            var links = GetLinks(type);
            if (links != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Links (Dependencies)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(links) { Sort = "Name asc" }, 1));
            }

            var events = GetEvents(type);
            if (events != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Events published**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(events) { Sort = "Name asc" }, 1));
            }

            var methods = GetMethods(type);
            if (methods != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Methods (callable from manager)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(methods) { Sort = "Name asc" }, 1));
            }

            return tags;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="type">The type</param>
        private DataTable GetOutputs(Type type)
        {
            var outputs = new DataTable("Outputs");
            outputs.Columns.Add("Name", typeof(string));
            outputs.Columns.Add("Description", typeof(string));
            outputs.Columns.Add("Units", typeof(string));
            outputs.Columns.Add("Type", typeof(string));
            outputs.Columns.Add("Settable?", typeof(bool));
            foreach (var property in type.GetProperties(System.Reflection.BindingFlags.Public |
                                                        System.Reflection.BindingFlags.Instance |
                                                        System.Reflection.BindingFlags.FlattenHierarchy))
            {
                if (property.DeclaringType != typeof(Model))
                {
                    var row = outputs.NewRow();

                    string typeName = GetTypeName(property.PropertyType);
                    string units = property.GetCustomAttribute<UnitsAttribute>()?.ToString();
                    var description = AutoDocumentation.GetSummary(property);
                    if (description == null)
                    {
                        var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                        description = descriptionAttribute?.ToString();
                    }
                    // Truncate descriptions so they fit onto the page.
                    if (description?.Length > maxDescriptionLength)
                        description = description.Remove(maxDescriptionLength) + "...";

                    row["Name"] = property.Name;
                    row["Type"] = typeName;
                    row["Units"] = units;
                    row["Description"] = description;
                    row["Settable?"] = property.CanWrite;

                    outputs.Rows.Add(row);
                }
            }
            if (outputs.Rows.Count > 0)
                return outputs;
            else
                return null;
        }

        /// <summary>Get a type name for the specified class member.</summary>
        /// <param name="memberType">The type to get a name for.</param>
        private string GetTypeName(Type memberType)
        {
            Type type;
            bool isList = false;
            bool isArray = false;
            if (memberType.IsGenericType && memberType.GetInterface("IList") != null)
            {
                type = memberType.GenericTypeArguments[0];
                isList = true;
            }
            else if (memberType.IsArray)
            {
                type = memberType.GetElementType();
                isArray = true;
            }
            else
                type = memberType;

            // Truncate descriptions so they fit onto the page.
            var typeName = type.Name;
            if (typeName?.Length > maxTypeLength)
                typeName = typeName.Remove(maxTypeLength) + "...";

            // Convert value types e.g. Double into alias names e.g. double - looks better.
            if (type.IsValueType && type.Namespace.StartsWith("System"))
                typeName = typeName.ToLower();

            if (isList)
                typeName += "List<" + typeName + ">";
            else if (isArray)
                typeName += "[]";

            if (type.IsClass && type.Namespace.StartsWith(namespaceToDocument))
            {
                if (!typesToDocument.Contains(type))
                    typesToDocument.Add(type);
                typeName = string.Format("<a href=\"#{0}\">{1}</a>", type.Name, typeName);
            }

            return typeName;
        }

        /// <summary>Return a datatable of links for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private DataTable GetLinks(Type type)
        {
            var links = new DataTable("Links");
            links.Columns.Add("Name", typeof(string));
            links.Columns.Add("Type", typeof(string));
            links.Columns.Add("IsOptional?", typeof(bool));
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public |
                                                    System.Reflection.BindingFlags.NonPublic |
                                                    System.Reflection.BindingFlags.Instance |
                                                    System.Reflection.BindingFlags.FlattenHierarchy))
            {
                var linkAttribute = field.GetCustomAttribute<LinkAttribute>();
                if (linkAttribute != null)
                {
                    var row = links.NewRow();

                    row["Name"] = field.Name;
                    row["IsOptional?"] = linkAttribute.IsOptional;
                    row["Type"] = GetTypeName(field.FieldType);

                    links.Rows.Add(row);
                }
            }

            if (links.Rows.Count == 0)
                return null;
            else
                return links;
        }

        /// <summary>Return a datatable of links for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private DataTable GetEvents(Type type)
        {
            var events = new DataTable("Links");
            events.Columns.Add("Name", typeof(string));
            events.Columns.Add("Type", typeof(string));

            foreach (var eventMember in type.GetEvents(System.Reflection.BindingFlags.Public |
                                                       System.Reflection.BindingFlags.Instance |
                                                       System.Reflection.BindingFlags.FlattenHierarchy))
            {
                var row = events.NewRow();

                row["Name"] = eventMember.Name;
                row["Type"] = GetTypeName(eventMember.EventHandlerType);

                events.Rows.Add(row);
            }

            if (events.Rows.Count > 0)
                return events;
            else
                return null;
        }

        /// <summary>Return a datatable of methods for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private DataTable GetMethods(Type type)
        {
            var methods = new DataTable("Methods");
            methods.Columns.Add("Name", typeof(string));
            methods.Columns.Add("Description", typeof(string));

            foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public |
                                                   System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.FlattenHierarchy))
            {
                if (!method.IsSpecialName)
                {
                    var row = methods.NewRow();
                    string parameters = null;
                    foreach (var argument in method.GetParameters())
                    {
                        if (parameters != null)
                            parameters += ", ";
                        parameters += GetTypeName(argument.ParameterType) + " " + argument.Name;
                    }
                    string description = AutoDocumentation.GetSummary(method);
                    if (description != null)
                        description = "<i>" + description + "</i>"; // italics
                    var st = string.Format("<p>{0} {1}({2})</p>{3}",
                                           GetTypeName(method.ReturnType), 
                                           method.Name, 
                                           parameters, 
                                           description);


                    row["Name"] = method.Name;
                    row["Description"] = st.Replace("\r\n", " ");

                    methods.Rows.Add(row);
                }
            }

            if (methods.Rows.Count > 0)
                return methods;
            else
                return null;
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory commandHistory)
        {

        }
    }
}

