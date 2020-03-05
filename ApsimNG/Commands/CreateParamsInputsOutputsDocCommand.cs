namespace UserInterface.Commands
{
    using APSIM.Shared.Utilities;
    using ApsimNG.Classes;
    using Models.Core;
    using Presenters;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    public class CreateParamsInputsOutputsDocCommand : ICommand
    {
        /// <summary>The maximum length of a description.</summary>
        private const int maxDescriptionLength = 50;

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

        /// <summary>List of parameter names for the model being documented.</summary>
        private List<string> parameterNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFileDocumentationCommand"/> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        /// <param name="model">The model to document.</param>
        /// <param name="destinationFolder">Name of directory to put pdf file into.</param>
        /// <param name="outputFileName">Output file name. Can be null.</param>
        public CreateParamsInputsOutputsDocCommand(ExplorerPresenter explorerPresenter, IModel model, string destinationFolder, string outputFileName)
        {
            this.explorerPresenter = explorerPresenter;
            this.modelToDocument = model;

            namespaceToDocument = modelToDocument.GetType().Namespace;
            var modelNameToDocument = Path.GetFileNameWithoutExtension(explorerPresenter.ApsimXFile.FileName.Replace("Validation", string.Empty));
            if (outputFileName == null)
                FileNameWritten = Path.Combine(destinationFolder, modelNameToDocument + ".description.pdf");
            else
                FileNameWritten = Path.Combine(destinationFolder, outputFileName);
        }

        /// <summary>The name of the file written.</summary>
        public string FileNameWritten { get; }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory commandHistory)
        {
            if (modelToDocument is ModelCollectionFromResource)
                parameterNames = (modelToDocument as ModelCollectionFromResource).GetModelParameterNames();

            // Get a list of tags for each type.
            var tags = new List<AutoDocumentation.ITag>();

            // Document the model.
            tags.AddRange(DocumentObject(modelToDocument));

            // Document any other referenced types.
            for (int i = 0; i < typesToDocument.Count; i++)
                tags.AddRange(DocumentType(typesToDocument[i]));

            // Convert the list of models into a list of tags.
            var pdfWriter = new PDFWriter(explorerPresenter, portraitOrientation:false);
            pdfWriter.CreatePDF(tags, FileNameWritten);
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="objectToDocument">The type to document.</param>
        private List<AutoDocumentation.ITag> DocumentObject(IModel objectToDocument)
        {
            var tags = new List<AutoDocumentation.ITag>();

            tags.Add(new AutoDocumentation.Heading((objectToDocument as IModel).Name, 1));
            AutoDocumentation.ParseTextForTags(AutoDocumentation.GetSummary(objectToDocument.GetType()), modelToDocument, tags, 1, 0,false);
            AutoDocumentation.ParseTextForTags(AutoDocumentation.GetRemarks(objectToDocument.GetType()), modelToDocument, tags, 1, 0,false);

            // If there are parameters then write them to the tags.
            if (parameterNames != null && !(objectToDocument is Models.PMF.Plant))
            {
                var parameters = GetParameters(objectToDocument);
                var parameterTable = PropertiesToTable(parameters, objectToDocument);
                tags.Add(new AutoDocumentation.Paragraph("**Parameters (Inputs)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(parameterTable) { Sort = "Name asc" }, 2, width:30));
            }

            var outputs = GetOutputs(objectToDocument.GetType());

            if (outputs != null && outputs.Count > 0)
            {
                var outputTable = PropertiesToTable(outputs);
                tags.Add(new AutoDocumentation.Paragraph("**Properties (Outputs)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(outputTable) { Sort = "Name asc" }, 2, width: 30));
            }

            DocumentLinksEventsMethods(objectToDocument.GetType(), tags);

            // Clear the parameter names as we've used them.
            parameterNames?.Clear();

            return tags;
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="typeToDocument">The type to document.</param>
        private List<AutoDocumentation.ITag> DocumentType(Type typeToDocument)
        {
            var tags = new List<AutoDocumentation.ITag>();

            tags.Add(new AutoDocumentation.Heading(typeToDocument.Name, 1));
            AutoDocumentation.ParseTextForTags(AutoDocumentation.GetSummary(typeToDocument), modelToDocument, tags, 1, 0, false);
            AutoDocumentation.ParseTextForTags(AutoDocumentation.GetRemarks(typeToDocument), modelToDocument, tags, 1, 0, false);

            var outputs = GetOutputs(typeToDocument);
            if (outputs != null && outputs.Count > 0)
            {
                var outputTable = PropertiesToTable(outputs);
                tags.Add(new AutoDocumentation.Paragraph("**Properties (Outputs)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(outputTable) { Sort = "Name asc" }, 2));
            }

            DocumentLinksEventsMethods(typeToDocument, tags);

            return tags;
        }

        private void DocumentLinksEventsMethods(Type typeToDocument, List<AutoDocumentation.ITag> tags)
        {
            var links = GetLinks(typeToDocument);
            if (links != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Links (Dependencies)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(links) { Sort = "Name asc" }, 1));
            }

            var events = GetEvents(typeToDocument);
            if (events != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Events published**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(events) { Sort = "Name asc" }, 1));
            }

            var methods = GetMethods(typeToDocument);
            if (methods != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("**Methods (callable from manager)**", 0));
                tags.Add(new AutoDocumentation.Table(new DataView(methods) { Sort = "Name asc" }, 1));
            }
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="properties">The list of properties to put into table.</param>
        /// <param name="objectToDocument">The object to use for getting property values. If null, then no value column will be added.</param>
        private DataTable PropertiesToTable(List<IVariable> properties, object objectToDocument = null)
        {
            var outputs = new DataTable("Properties");
            outputs.Columns.Add("Name", typeof(string));
            outputs.Columns.Add("Description", typeof(string));
            outputs.Columns.Add("Units", typeof(string));
            outputs.Columns.Add("Type", typeof(string));
            if (objectToDocument == null)
                outputs.Columns.Add("Settable?", typeof(bool));
            else
                outputs.Columns.Add("Value", typeof(string));
            foreach (var property in properties)
            {
                var row = outputs.NewRow();

                string typeName = GetTypeName(property.DataType);
                var summary = property.Summary + property.Description;
                string remarks = property.Remarks;
                if (!string.IsNullOrEmpty(remarks))
                    summary += Environment.NewLine + Environment.NewLine + remarks;

                row["Name"] = property.Name;
                row["Type"] = typeName;
                row["Units"] = property.Units;
                row["Description"] = summary;
                if (objectToDocument == null)
                    row["Settable?"] = property.Writable;
                else
                {
                    try
                    {
                        row["Value"] = property.Value;
                    }
                    catch (Exception)
                    { }
                }
                outputs.Rows.Add(row);  
            }
            if (outputs.Rows.Count > 0)
                return outputs;
            else
                return null;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="typeToDocument">The type of object to inspect.</param>
        /// <param name="typeofProperties">The type of properties to include in the return table.</param>
        private List<IVariable> GetParameters(object objectToDocument)
        {
            var parameters = new List<IVariable>();

            foreach (var parameterName in parameterNames)
            {
                var parameter = Apsim.GetVariableObject(objectToDocument as IModel, parameterName);
                if (parameter != null)
                    parameters.Add(parameter);
            }

            return parameters;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="typeToDocument">The type of object to inspect.</param>
        /// <param name="typeofProperties">The type of properties to include in the return table.</param>
        private List<IVariable> GetOutputs(Type typeToDocument)
        {
            var outputs = new List<IVariable>();
            foreach (var property in typeToDocument.GetProperties(BindingFlags.Public |
                                                                  BindingFlags.Instance |
                                                                  BindingFlags.FlattenHierarchy |
                                                                  BindingFlags.DeclaredOnly))
            {
                if (property.DeclaringType != typeof(Model))
                {
                    // See if property is a parameter. If so then don't put it into
                    // the outputs table.
                    bool isParameter = parameterNames != null && parameterNames.Contains(property.Name);

                    if (!isParameter)
                        outputs.Add(new VariableProperty(null, property));
                }
            }
            return outputs;
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

            if (type.IsClass && type.Namespace != null && type.Namespace.StartsWith(namespaceToDocument))
            {
                if (type != modelToDocument.GetType() && !typesToDocument.Contains(type))
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

                MethodInfo invokeMethod = eventMember.EventHandlerType.GetMethod("Invoke");
                string parameterString = null;
                foreach (ParameterInfo param in invokeMethod.GetParameters())
                {
                    if (parameterString != null)
                        parameterString += ", ";

                    parameterString += GetTypeName(param.ParameterType) + " " + param.Name;
                }
                string typeString = invokeMethod.ReturnType.Name + " " + 
                                    eventMember.Name + " ("  +
                                    parameterString +
                                    ")";

                row["Type"] = typeString;

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
                    string remarks = AutoDocumentation.GetRemarks(method);
                    if (!string.IsNullOrEmpty(remarks))
                        description += Environment.NewLine + Environment.NewLine + remarks;

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

