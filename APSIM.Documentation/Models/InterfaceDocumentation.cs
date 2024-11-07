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
        /// <summary>The maximum length of a type name.</summary>
        private const int maxTypeLength = 30;

        /// <summary>Binding Flags used by reflection to get Properties, Fields, Events, Methods etc from types.</summary>
        private static BindingFlags FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly;

        /// <summary>Document a Model Interface into ITags</summary>
        /// <param name="model"></param>
        /// <returns>A list of ITags with the documentation for this interface</returns>
        public static List<ITag> Document(IModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            
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

            // Document any other referenced types.
            foreach (Type typeDoc in GetTypes(model))
                tags.AddRange(DocumentType(typeDoc));
                
            return tags;
        }

        /// <summary>Document the specified model.</summary>
        /// <param name="type"></param>
        /// <returns>An ITag section with the summary, remarks, outputs, links, events and methods for that type</returns>
        private static List<ITag> DocumentType(Type type)
        {
            List<ITag> tags = new List<ITag>();
            tags.Add(new Paragraph(CodeDocumentation.GetSummary(type)));
            tags.Add(new Paragraph(CodeDocumentation.GetRemarks(type)));

            tags.AddRange(GetOutputs(type));
            tags.AddRange(DocumentLinksEventsMethods(type));

            return new List<ITag>() {new Section(type.GetFriendlyName(), tags)};
        }

        /// <summary>Get all the types that should be documented for this model. This includes Properties and Fields within the same namespace.</summary>
        /// <param name="model"></param>
        /// <returns>An array of types to be documented</returns>
        private static Type[] GetTypes(IModel model)
        {
            string namespaceToDocument = model.GetType().Namespace;
            Type modelType = model.GetType();
            List<Type> types = new List<Type>();

            PropertyInfo[] properties = modelType.GetProperties(FLAGS);
            foreach(PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;
                if (propertyType.IsClass && propertyType.Namespace != null && propertyType.Namespace.StartsWith(namespaceToDocument))
                {
                    if (propertyType != modelType && !types.Contains(propertyType))
                        types.Add(propertyType);
                }
            }

            FieldInfo[] fields = modelType.GetFields(FLAGS);
            foreach(FieldInfo field in fields)
            {
                Type propertyType = field.FieldType;
                if (propertyType.IsClass && propertyType.Namespace != null && propertyType.Namespace.StartsWith(namespaceToDocument))
                {
                    if (propertyType != modelType && !types.Contains(propertyType))
                        types.Add(propertyType);
                }
            }

            return types.ToArray();
        }

        /// <summary>Gets the list of parameter names for a model. If the model is a resource, it will load them from the resource.</summary>
        /// <param name="model"></param>
        /// <returns>An array of string name for each of the parameters.</returns>
        private static string[] GetParameterNames(IModel model)
        {
            if (string.IsNullOrEmpty(model.ResourceName))
            {
                var modelAsJson = FileFormat.WriteToString(model);
                return Resource.GetModelParameterNamesFromJSON(modelAsJson).ToArray();
            }
            else
                return Resource.GetModelParameterNames(model.ResourceName).ToArray();
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

            DataTable parameterTable = ConvertPropertiesToDataTable(parameters, model);
            if (parameterTable != null)
            {
                parameterTable.TableName = "Parameters (Inputs)";
                tags.AddRange(ConvertToITags(parameterTable));
            }
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
            PropertyInfo[] properties = type.GetProperties(FLAGS);
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

            DataTable table = ConvertPropertiesToDataTable(outputs);
            if (table != null)
            {
                table.TableName = "Properties (Outputs)";
                return ConvertToITags(table);
            }
            else
                return new List<ITag>();
        }

        /// <summary>Gets documentation tags for Links, Events and Methods for a type.</summary>
        /// <param name="type"></param>
        /// <returns>An array of ITag section for each of the Links, Events and Methods to be documented</returns>
        private static List<ITag> DocumentLinksEventsMethods(Type type)
        {
            List<ITag> tags = new List<ITag>();

            DataTable links = GetLinks(type);
            if (links != null)
            {
                links.TableName = "Links (Dependencies)";
                tags.AddRange(ConvertToITags(links));
            }
            
            DataTable events = GetEvents(type);
            if (events != null)
            {
                events.TableName = "Events published";
                tags.AddRange(ConvertToITags(events));
            }

            DataTable methods = GetMethods(type);
            if (methods != null)
            {
                methods.TableName = "Methods (callable from manager)";
                tags.AddRange(ConvertToITags(methods));
            }

            return tags;
        }

        /// <summary>Return a datatable of links for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private static DataTable GetLinks(Type type)
        {
            DataTable table = new DataTable("Links");
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("IsOptional?", typeof(bool));

            FieldInfo[] fields = type.GetFields(FLAGS | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                LinkAttribute linkAttribute = field.GetCustomAttribute<LinkAttribute>();
                if (linkAttribute != null)
                {
                    DataRow row = table.NewRow();
                    row["Name"] = field.Name;
                    row["IsOptional?"] = linkAttribute.IsOptional;
                    row["Type"] = GetTypeName(field.FieldType);
                    table.Rows.Add(row);
                }
            }

            if (table.Rows.Count > 0)
                return table;
            else
                return null;
        }

        /// <summary>Return a datatable of links for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private static DataTable GetEvents(Type type)
        {
            DataTable table = new DataTable("Links");
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Type", typeof(string));

            EventInfo[] events = type.GetEvents(FLAGS);

            foreach (EventInfo eventMember in events)
            {
                DataRow row = table.NewRow();

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

                table.Rows.Add(row);
            }

            if (table.Rows.Count > 0)
                return table;
            else
                return null;
        }

        /// <summary>Return a datatable of methods for the specified type.</summary>
        /// <param name="type">The type to document.</param>
        private static DataTable GetMethods(Type type)
        {
            DataTable table = new DataTable("Methods");
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));

            MethodInfo[] methods = type.GetMethods(FLAGS);

            foreach (MethodInfo method in methods)
            {
                if (!method.IsSpecialName)
                {
                    DataRow row = table.NewRow();
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
                    row["Description"] = st.ToString();

                    table.Rows.Add(row);
                }
            }

            if (table.Rows.Count > 0)
                return table;
            else
                return null;
        }

        /// <summary>Get a type name for the specified class member.</summary>
        /// <param name="memberType">The type to get a name for.</param>
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

        /// <summary>
        /// Convert a list of properties in a DataTable
        /// </summary>
        /// <param name="properties">The list of properties to put into table.</param>
        /// <param name="objectToDocument">The object to use for getting property values. If null, then no value column will be added.</param>
        /// <returns>A datatable containing the content for the properties</returns>
        private static DataTable ConvertPropertiesToDataTable(List<IVariable> properties, object objectToDocument = null)
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

        /// <summary>Convert a DataTable into an ITag table</summary>
        /// <param name="table"></param>
        /// <returns>An ITag list with the table name in a paragraph and table as a sibling.</returns>
        private static List<ITag> ConvertToITags(DataTable table)
        {
            List<ITag> tags = new List<ITag>();
            
            if (table == null || table.Rows.Count == 0)
                return tags;

            tags.Add(new Paragraph("**" + table.TableName + "**"));
            tags.Add(new Table(new DataView(table) { Sort = "Name asc" }));
            return tags;
        }
    }
}
