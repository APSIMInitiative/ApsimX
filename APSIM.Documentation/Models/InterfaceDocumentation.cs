using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using APSIM.Shared.Extensions;
using Models.Core.ApsimFile;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// This class documents a model's parameters, inputs, and outputs.
    /// </summary>
    public class InterfaceDocumentation
    {
        /// <summary>The maximum length of a description.</summary>
        private const int maxDescriptionLength = 50;

        /// <summary>The maximum length of a type name.</summary>
        private const int maxTypeLength = 30;

        /// <summary>
        /// 
        /// </summary>
        public InterfaceDocumentation()
        {
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public IEnumerable<ITag> Document(IModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            string namespaceToDocument = model.GetType().Namespace;
            string[] parameterNames = GetParameterNames(model);
            Type type = model.GetType();

            // Get a list of tags for each type.
            List<ITag> tags = new List<ITag>();

            List<ITag> subtags = new List<ITag>();
            //Add Parameters
            subtags.AddRange(GetParameters(model, parameterNames));

            subtags.AddRange(GetOutputs(type, parameterNames));

            subtags.AddRange(DocumentLinksEventsMethods(type));

            tags.Add(new Section(model.Name, subtags));

            List<Type> typesToDocument = GetTypes(namespaceToDocument, model);
            // Document any other referenced types.
            foreach (Type typeDoc in typesToDocument)
            {
                tags.AddRange(DocumentType(typeDoc));
            }
                
            return tags;
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="type"></param>
        private List<ITag> DocumentType(Type type)
        {
            List<ITag> tags = new List<ITag>();
            tags.Add(new Paragraph(CodeDocumentation.GetSummary(type)));
            tags.Add(new Paragraph(CodeDocumentation.GetRemarks(type)));

            tags.AddRange(GetOutputs(type));
            tags.AddRange(DocumentLinksEventsMethods(type));

            return new List<ITag>() {new Section(type.GetFriendlyName(), tags)};
        }

        private static List<ITag> DocumentTable(string sectionName, DataTable parameterTable)
        {
            List<ITag> tags = new List<ITag>();
            
            if (parameterTable == null || parameterTable.Rows.Count == 0)
                return tags;

            tags.Add(new Paragraph(sectionName));
            tags.Add(new Table(new DataView(parameterTable) { Sort = "Name asc" }));
            return tags;
        }

        private List<ITag> DocumentLinksEventsMethods(Type type)
        {
            List<ITag> tags = new List<ITag>();

            DataTable links = GetLinks(type);
            tags.AddRange(DocumentTable("**Links (Dependencies)**", links));

            DataTable events = GetEvents(type);
            tags.AddRange(DocumentTable("**Events published**", events));

            DataTable methods = GetMethods(type);
            tags.AddRange(DocumentTable("**Methods (callable from manager)**", methods));

            return tags;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="properties">The list of properties to put into table.</param>
        /// <param name="objectToDocument">The object to use for getting property values. If null, then no value column will be added.</param>
        private static DataTable PropertiesToTable(IEnumerable<IVariable> properties, object objectToDocument = null)
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
        /// </summary>
        /// <param name="model"></param>
        private static string[] GetParameterNames(IModel model)
        {
            if (string.IsNullOrEmpty(model.ResourceName))
            {
                var modelAsJson = FileFormat.WriteToString(model);
                return Resource.GetModelParameterNamesFromJSON(modelAsJson).ToArray();
            }
            else
            {
                return Resource.GetModelParameterNames(model.ResourceName).ToArray();
            }
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="model">model to be documented.</param>
        /// <param name="parameterNames">model to be documented.</param>
        private static List<ITag> GetParameters(IModel model, string[] parameterNames)
        {
            List<ITag> tags = new List<ITag>();

            //if there are no paramters or this is a plant, don't add anything
            if (parameterNames.Length == 0 || model is Plant)
                return tags;

            List<IVariable> parameters = new List<IVariable>();
            foreach (string parameterName in parameterNames)
            {
                IVariable parameter = model.FindByPath(parameterName);
                if (parameter != null)
                    parameters.Add(parameter);
            }

            DataTable parameterTable = InterfaceDocumentation.PropertiesToTable(parameters, model);
            tags.AddRange(DocumentTable("**Parameters (Inputs)**", parameterTable));

            return tags;
        }

        /// <summary>
        /// Create and return a new Output object for member
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameterNames"></param>
        private static List<ITag> GetOutputs(Type type, string[] parameterNames = null)
        {
            List<IVariable> outputs = new List<IVariable>();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo property in properties)
            {
                if (property.DeclaringType != typeof(Model))
                {
                    // See if property is a parameter. If so then don't put it into the outputs table.
                    bool isParameter = false;
                    if (parameterNames != null)
                        isParameter = parameterNames.Contains(property.Name);

                    if (!isParameter)
                        outputs.Add(new VariableProperty(null, property));
                }
            }

            if (outputs.Count == 0)
                return new List<ITag>();
            else
                return DocumentTable("**Properties (Outputs)**", PropertiesToTable(outputs));

        }

        /// <summary>Get a type name for the specified class member.</summary>
        /// <param name="memberType">The type to get a name for.</param>
        /// <remarks>
        /// todo: consider a way to phase out this function by making use of
        ///. The problem is
        /// we need some way of keeping track of which user-defiend (aka apsim-)
        /// types are referenced by this type.
        /// </remarks>
        private static string GetTypeName(Type memberType)
        {
            Type type = null;
            bool isArray = false;
            if (memberType.IsByRef)
                return GetTypeName(memberType.GetElementType());
            if (memberType.GetInterface("IList") != null)
            {
                if (memberType.IsGenericType)
                    type = memberType.GenericTypeArguments[0];
                else
                    type = memberType.GetElementType();
            }
            else if (memberType.GetInterface("IEnumerable") != null)
            {
                if (memberType.IsGenericType)
                    type = memberType.GenericTypeArguments[0];
                else
                    type = memberType.GetElementType();
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

            return typeName;
        }

        /// <summary>Get a type name for the specified class member.</summary>
        /// <param name="namespaceToDocument"></param>
        /// <param name="modelToDocument"></param>
        private static List<Type> GetTypes(string namespaceToDocument, IModel modelToDocument)
        {
            Type modelType = modelToDocument.GetType();
            
            List<Type> typesToDocument = new List<Type>();

            PropertyInfo[] properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            foreach(PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;
                if (propertyType.IsClass && propertyType.Namespace != null && propertyType.Namespace.StartsWith(namespaceToDocument))
                {
                    if (propertyType != modelType && !typesToDocument.Contains(propertyType))
                        typesToDocument.Add(propertyType);
                }
            }

            FieldInfo[] fields = modelType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            foreach(FieldInfo field in fields)
            {
                Type propertyType = field.FieldType;
                if (propertyType.IsClass && propertyType.Namespace != null && propertyType.Namespace.StartsWith(namespaceToDocument))
                {
                    if (propertyType != modelType && !typesToDocument.Contains(propertyType))
                        typesToDocument.Add(propertyType);
                }
            }

            return typesToDocument;
        }

        /// <summary>Return a datatable of links for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private static DataTable GetLinks(Type type)
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
