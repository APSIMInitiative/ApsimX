

namespace Models.Core.ApsimFile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// XML to JSON converter
    /// </summary>
    public class XmlToJson
    {
        private static string[] builtinTypeNames = new string[] { "string", "int", "double" };
        private static string[] arrayVariableNames = new string[] { "AcceptedStats", "Operation" };
        /// <summary>
        /// Convert APSIM Next Generation xml to json.
        /// </summary>
        /// <param name="xml">XML string to convert.</param>
        /// <returns>The equivalent JSON.</returns>
        public static string Convert(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string json = JsonConvert.SerializeXmlNode(doc);
            JObject root = JObject.Parse(json);

            JToken newRoot = CreateObject(root[doc.DocumentElement.Name]);

            json = newRoot.ToString();

            return json;
        }

        /// <summary>
        /// Create an object (or an array)
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static JToken CreateObject(JToken root)
        {
            JObject newRoot = new JObject();
            if (root.Parent != null)
                AddTypeToObject(root, newRoot);

            JToken child = root.First;
            while (child != null)
            {
                if (child is JProperty)
                {
                    JProperty property = child as JProperty;
                    if (property.Value is JArray)
                    {
                        if (builtinTypeNames.Contains(property.Name))
                        {
                            // array of string / double etc/
                            return CreateArray(property.Name, property.Value as JArray, newRoot);
                        }
                        else
                        {
                            // array of models.
                            JArray arrayOfModels = CreateArray(property.Name, property.Value as JArray, newRoot);
                            if (arrayOfModels.Count > 0)
                                newRoot[property.Name] = arrayOfModels;
                        }
                    }
                    else if (property.Value is JValue)
                    {
                        if (builtinTypeNames.Contains(property.Name))
                        {
                            // Should be treated as an array. e.g report event names are often
                            // only one element. The NewtonSoft JSON converter treats this as
                            // a normal property. We need to convert the property into an array
                            // with one element.
                            return CreateArray(property.Name, property.Value, newRoot);
                        }
                        else
                            WriteProperty(property, newRoot);
                    }
                    else if (property.Value is JObject)
                        ProcessObject(property.Name, property.Value as JObject, newRoot);
                }

                child = child.Next;
            }

            return newRoot;
        }

        private static void AddTypeToObject(JToken root, JToken newRoot)
        {
            Type t = GetModelTypeName(root.Parent.Path);
            if (t != null)
                newRoot["$type"] = t.FullName + ", Models";
        }

        private static void ProcessObject(string name, JObject obj, JObject newRoot)
        {
            // Look for an array of something e.g. variable names in report.
            if (name == "Code")
            {
                JValue childAsValue = obj.First.First as JValue;
                newRoot["Code"] = childAsValue.Value.ToString();
            }
            else if (name == "MemoText")
            {
                JValue childAsValue = obj.First.First as JValue;
                newRoot["Text"] = childAsValue.Value.ToString();
            }
            else if (name == "Script")
            {
                // manager parameters.
                JArray parameters = new JArray();
                foreach (JProperty parameter in obj.Children())
                {
                    JObject newParameter = new JObject();
                    newParameter["Key"] = parameter.Name;
                    newParameter["Value"] = parameter.Value.ToString();
                    parameters.Add(newParameter);
                }
                newRoot["Parameters"] = parameters;
            }
            else
            {
                Type modelType = GetModelTypeName(name);

                if (modelType == null || modelType.GetInterface("IModel") == null)
                {
                    var newObject = CreateObject(obj);
                    if (arrayVariableNames.Contains(name))
                    {
                        // Should be an array of objects.
                        if (newObject.First.First is JArray)
                            newObject = newObject.First.First;
                        else
                        {
                            JArray array = new JArray();
                            if (newObject.Count() == 1 && newObject.First is JProperty)
                                array.Add(newObject.First.First);
                            else
                                array.Add(newObject);
                            newObject = array;
                        }
                    }

                    newRoot[name] = newObject;
                }
                else
                    AddNewChild(obj, newRoot);
            }
        }

        private static JArray CreateArray(string name, JToken array, JObject newRoot)
        {
            // Array of non models. e.g. array of Axis.
            JArray newArray = new JArray();
            if (array is JArray)
            {
                foreach (var element in array.Children())
                {
                    Type modelType = GetModelTypeName(name);
                    if (name == "string")
                        newArray.Add(new JValue(element.ToString()));
                    else if (name == "double")
                        newArray.Add(new JValue(double.Parse(element.ToString())));
                    else if (modelType == null || modelType.GetInterface("IModel") == null)
                        newArray.Add(CreateObject(element as JObject));
                    else
                        AddNewChild(element, newRoot);
                }
            }
            else if (array is JValue)
            {
                // simply put the single property into the array. e.g. report event names
                JValue value = array as JValue;
                if (name == "string")
                    newArray.Add(new JValue(value.ToString()));
                else if (name == "double")
                    newArray.Add(new JValue(double.Parse(value.ToString())));
            }

            return newArray;
        }

        private static void AddNewChild(JToken element, JObject newRoot)
        {
            var newChild = CreateObject(element as JObject);

            if (newRoot["Children"] == null)
                newRoot["Children"] = new JArray();

            (newRoot["Children"] as JArray).Add(newChild);
        }

        private static void WriteProperty(JProperty property, JObject toObject)
        {
            string propertyName = property.Name;
            if (propertyName == "@Version")
                propertyName = "Version";
            // Old memo have #text, we don't them.
            if (propertyName == "#text")
                return;

            if (!propertyName.StartsWith("@"))
            {
                JToken valueToken = property.Value;
                if (valueToken.HasValues)
                {
                    if (property.First.First.First is JValue)
                    {
                        JValue value = property.First.First.First as JValue;
                        string elementType = (value.Parent as JProperty).Name;
                        JArray newArray = new JArray();
                        newArray.Add(new JValue(value.ToString()));
                        toObject[propertyName] = newArray;
                    }
                    else if (property.First.First.First is JArray)
                    {
                        JArray array = property.First.First.First as JArray;

                        string elementType = (array.Parent as JProperty).Name;
                        JArray newArray = new JArray();
                        foreach (var value in array.Values())
                        {
                            if (elementType == "string")
                                newArray.Add(new JValue(value.ToString()));
                            else if (elementType == "double")
                                newArray.Add(new JValue(double.Parse(value.ToString())));
                        }
                        toObject[propertyName] = newArray;
                    }
                }
                else
                {
                    string value = valueToken.Value<string>();
                    int intValue;
                    double doubleValue;
                    bool boolValue;
                    DateTime dateValue;
                    if (int.TryParse(value, out intValue))
                        toObject[propertyName] = intValue;
                    else if (double.TryParse(value, out doubleValue))
                        toObject[propertyName] = doubleValue;
                    else if (value == "-INF")
                        toObject[propertyName] = double.NaN;
                    else if (bool.TryParse(value, out boolValue))
                        toObject[propertyName] = boolValue;
                    else if (DateTime.TryParseExact(value, "MM/dd/yyyy hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateValue))
                        toObject[propertyName] = dateValue.ToString("yyyy-MM-dd");
                    else
                        toObject[propertyName] = value;
                }
            }
        }

        private static Type GetModelTypeName(string modelNameToFind)
        {
            string[] modelWords = modelNameToFind.Split(".".ToCharArray());
            string m = modelWords[modelWords.Length - 1];

            Type[] types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (type.Name == m)
                    return type;
            }
            return null;
        }
    }
}
