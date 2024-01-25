﻿namespace APSIM.Interop.Documentation.Formats
{
    using APSIM.Shared.Documentation;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using APSIM.Shared.Extensions;
    using Newtonsoft.Json.Linq;
    using Models.Core.ApsimFile;

    /// <summary>
    /// This class documents a model's parameters, inputs, and outputs.
    /// </summary>
    public class ParamsInputsOutputs
    {
        /// <summary>The maximum length of a description.</summary>
        private const int maxDescriptionLength = 50;

        /// <summary>The maximum length of a type name.</summary>
        private const int maxTypeLength = 30;

        /// <summary>The model to document.</summary>
        private IModel modelToDocument;

        /// <summary>A list of types to document.</summary>
        private IEnumerable<Type> typesToDocument = Enumerable.Empty<Type>();

        /// <summary>Only document types in this namespace.</summary>
        private string namespaceToDocument;

        /// <summary>List of parameter names for the model being documented.</summary>
        private IEnumerable<string> parameterNames = Enumerable.Empty<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFileDocumentationCommand"/> class.
        /// </summary>
        /// <param name="model">The model to document.</param>
        public ParamsInputsOutputs(IModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            modelToDocument = model;
            namespaceToDocument = model.GetType().Namespace;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public IEnumerable<ITag> Document()
        {
            if (string.IsNullOrEmpty(modelToDocument.ResourceName))
            {
                var modelAsJson = FileFormat.WriteToString(modelToDocument);
                parameterNames = Resource.GetModelParameterNamesFromJSON(modelAsJson);
            }
            else
                parameterNames = Resource.GetModelParameterNames(modelToDocument.ResourceName);

            // Get a list of tags for each type.
            List<ITag> tags = new List<ITag>();

            // Document the model.
            tags.AddRange(DocumentObject(modelToDocument));

            // Document any other referenced types.
            foreach (Type type in typesToDocument)
                tags.AddRange(DocumentType(type));

            return tags;
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="objectToDocument">The type to document.</param>
        private IEnumerable<ITag> DocumentObject(IModel objectToDocument)
        {
            List<ITag> tags = new List<ITag>();

            // If there are parameters then write them to the tags.
            if (parameterNames != null && !(objectToDocument is Models.PMF.Plant))
            {
                IEnumerable<IVariable> parameters = GetParameters(objectToDocument);
                DataTable parameterTable = PropertiesToTable(parameters, objectToDocument);
                tags.AddRange(DocumentTable("**Parameters (Inputs)**", parameterTable));
            }

            IEnumerable<IVariable> outputs = GetOutputs(objectToDocument.GetType());

            if (outputs != null && outputs.Any())
            {
                DataTable outputTable = PropertiesToTable(outputs);
                tags.AddRange(DocumentTable("**Properties (Outputs)**", outputTable));
            }

            tags.AddRange(DocumentLinksEventsMethods(objectToDocument.GetType()));

            yield return new Section(objectToDocument.Name, tags);
        }

        private IEnumerable<ITag> DocumentTable(string sectionName, DataTable parameterTable)
        {
            if (parameterTable != null && parameterTable.Rows.Count > 0)
            {
                yield return new Paragraph(sectionName);
                yield return new Table(new DataView(parameterTable) { Sort = "Name asc" });
            }
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="typeToDocument">The type to document.</param>
        private IEnumerable<ITag> DocumentType(Type typeToDocument)
        {
            List<ITag> tags = new List<ITag>();
            tags.Add(new Paragraph(CodeDocumentation.GetSummary(typeToDocument)));
            tags.Add(new Paragraph(CodeDocumentation.GetRemarks(typeToDocument)));

            IEnumerable<IVariable> outputs = GetOutputs(typeToDocument);
            if (outputs != null && outputs.Any())
            {
                DataTable outputTable = PropertiesToTable(outputs);
                tags.AddRange(DocumentTable("**Properties (Outputs)**", outputTable));
            }

            tags.AddRange(DocumentLinksEventsMethods(typeToDocument));

            yield return new Section(typeToDocument.GetFriendlyName(), tags);
        }

        private IEnumerable<ITag> DocumentLinksEventsMethods(Type typeToDocument)
        {
            DataTable links = GetLinks(typeToDocument);
            foreach (ITag tag in DocumentTable("**Links (Dependencies)**", links))
                yield return tag;

            DataTable events = GetEvents(typeToDocument);
            foreach (ITag tag in DocumentTable("**Events published**", events))
                yield return tag;

            DataTable methods = GetMethods(typeToDocument);
            foreach (ITag tag in DocumentTable("**Methods (callable from manager)**", methods))
                yield return tag;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="properties">The list of properties to put into table.</param>
        /// <param name="objectToDocument">The object to use for getting property values. If null, then no value column will be added.</param>
        private DataTable PropertiesToTable(IEnumerable<IVariable> properties, object objectToDocument = null)
        {
            DataTable outputs = new DataTable("Properties");
            outputs.Columns.Add("Name", typeof(string));
            outputs.Columns.Add("Description", typeof(string));
            outputs.Columns.Add("Units", typeof(string));
            outputs.Columns.Add("Type", typeof(string));
            if (objectToDocument == null)
                outputs.Columns.Add("Settable?", typeof(bool));
            else
                outputs.Columns.Add("Value", typeof(string));
            foreach (IVariable property in properties)
            {
                DataRow row = outputs.NewRow();

                string typeName = GetTypeName(property.DataType);
                string summary = property.Summary;
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
        /// <param name="objectToDocument">Object to be documented.</param>
        private IEnumerable<IVariable> GetParameters(object objectToDocument)
        {
            List<IVariable> parameters = new List<IVariable>();

            foreach (string parameterName in parameterNames)
            {
                IVariable parameter = (objectToDocument as IModel).FindByPath(parameterName);
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
        private IEnumerable<IVariable> GetOutputs(Type typeToDocument)
        {
            List<IVariable> outputs = new List<IVariable>();
            foreach (PropertyInfo property in typeToDocument.GetProperties(BindingFlags.Public |
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
        /// <remarks>
        /// todo: consider a way to phase out this function by making use of
        /// <see cref="TypeExtensions.GetFriendlyName(Type)"/>. The problem is
        /// we need some way of keeping track of which user-defiend (aka apsim-)
        /// types are referenced by this type.
        /// </remarks>
        private string GetTypeName(Type memberType)
        {
            Type type = null;
            bool isList = false;
            bool isArray = false;
            bool isEnumerable = false;
            if (memberType.IsByRef)
                return GetTypeName(memberType.GetElementType());
            if (memberType.GetInterface("IList") != null)
            {
                if (memberType.IsGenericType)
                    type = memberType.GenericTypeArguments[0];
                else
                    type = memberType.GetElementType();
                isList = true;
            }
            else if (memberType.GetInterface("IEnumerable") != null)
            {
                if (memberType.IsGenericType)
                    type = memberType.GenericTypeArguments[0];
                else
                    type = memberType.GetElementType();
                isEnumerable = true;
            }
            else if (memberType.IsArray)
            {
                type = memberType.GetElementType();
                isArray = true;
            }

            if (type == null)
                type = memberType;

            // Truncate descriptions so they fit onto the page.
            string typeName = type.Name;
            if (typeName?.Length > maxTypeLength)
                typeName = typeName.Remove(maxTypeLength) + "...";

            // Convert value types e.g. Double into alias names e.g. double - looks better.
            if (type.IsValueType && type.Namespace.StartsWith("System"))
                typeName = typeName.ToLower();

            if (isArray)
                typeName += "[]";

            if (type.IsClass && type.Namespace != null && type.Namespace.StartsWith(namespaceToDocument))
            {
                if (type != modelToDocument.GetType() && !typesToDocument.Contains(type))
                    typesToDocument = typesToDocument.Append(type);
                typeName = $"[{typeName}](#{type.Name})";

                if (isList)
                    typeName = $"List&lt;{typeName}&gt;";
                else if (isEnumerable)
                    typeName = $"IEnumerable&lt;{typeName}&gt;";
            }

            return typeName;
        }

        /// <summary>Return a datatable of links for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private DataTable GetLinks(Type type)
        {
            DataTable links = new DataTable("Links");
            links.Columns.Add("Name", typeof(string));
            links.Columns.Add("Type", typeof(string));
            links.Columns.Add("IsOptional?", typeof(bool));
            foreach (FieldInfo field in type.GetFields(System.Reflection.BindingFlags.Public |
                                                    System.Reflection.BindingFlags.NonPublic |
                                                    System.Reflection.BindingFlags.Instance |
                                                    System.Reflection.BindingFlags.FlattenHierarchy))
            {
                LinkAttribute linkAttribute = field.GetCustomAttribute<LinkAttribute>();
                if (linkAttribute != null)
                {
                    DataRow row = links.NewRow();

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
            DataTable events = new DataTable("Links");
            events.Columns.Add("Name", typeof(string));
            events.Columns.Add("Type", typeof(string));

            foreach (EventInfo eventMember in type.GetEvents(System.Reflection.BindingFlags.Public |
                                                       System.Reflection.BindingFlags.Instance |
                                                       System.Reflection.BindingFlags.FlattenHierarchy))
            {
                DataRow row = events.NewRow();

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
            DataTable methods = new DataTable("Methods");
            methods.Columns.Add("Name", typeof(string));
            methods.Columns.Add("Description", typeof(string));

            foreach (MethodInfo method in type.GetMethods(System.Reflection.BindingFlags.Public |
                                                   System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (!method.IsSpecialName)
                {
                    DataRow row = methods.NewRow();
                    string parameters = null;
                    foreach (ParameterInfo argument in method.GetParameters())
                    {
                        if (parameters != null)
                            parameters += ", ";
                        parameters += GetTypeName(argument.ParameterType) + " " + argument.Name;
                    }
                    string description = CodeDocumentation.GetSummary(method);
                    string remarks = CodeDocumentation.GetRemarks(method);
                    if (!string.IsNullOrEmpty(remarks))
                        description += Environment.NewLine + Environment.NewLine + remarks;
                    string methodName = method.Name;
                    // Italicise the method description.
                    if (!string.IsNullOrEmpty(description))
                        description = $"*{description}*";
                    StringBuilder st = new StringBuilder();
                    string returnType = GetTypeName(method.ReturnType);
                    st.Append(returnType);
                    st.Append(" ");
                    st.Append(method.Name);
                    st.Append("(");
                    st.Append(parameters);
                    st.AppendLine(")");
                    st.AppendLine();
                    st.Append(description);

                    row["Name"] = method.Name;
                    row["Description"] = st.ToString();//.Replace("\r\n", " ");

                    methods.Rows.Add(row);
                }
            }

            if (methods.Rows.Count > 0)
                return methods;
            else
                return null;
        }
    }
}
