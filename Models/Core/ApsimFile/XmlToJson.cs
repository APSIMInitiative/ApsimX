

namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// XML to JSON converter
    /// </summary>
    public class XmlToJson
    {
        private static string[] builtinTypeNames = new string[] { "string", "int", "double", "dateTime", "ArrayOfString" };
        private static string[] arrayVariableNames = new string[] { "AcceptedStats", "Operation", "Parameters", "cultivars", "Nodes", "Stores", "PaddockList" };
        private static string[] arrayVariables = new[] { "Command", "Alias", "Leaves", "ZoneNamesToGrowRootsIn", "ZoneRootDepths", "ZoneInitialDM" };
        private static string[] propertiesToIgnore = new[] { "ParameterValues", "Nodes", "Arcs", "Weirdo" };

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

            // The order of child nodes can be wrong. Newtonsoft XML to JSON will
            // group child nodes of the same type into an array. This alters the
            // order of children. Need to reorder children.
            ReorderChildren(newRoot, doc.DocumentElement);

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
                JProperty property = child as JProperty;
                if (property != null && !propertiesToIgnore.Contains(property.Name))
                {
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
                        else if (GetModelTypeName(property.Name) != null && 
                                 property.Name != "Parameter" &&
                                 GetModelTypeName(property.Name).GetInterface("IModel") != null)  // CLEM.LabourFilter has a Parameter property.
                        {
                            // a model without any child nodes.
                            AddNewChild(property, newRoot);
                        }
                        else if (arrayVariables.Contains(property.Name))
                        {
                            JArray arrayOfModels = CreateArray(property.Name, property.Value, newRoot);
                            if (arrayOfModels.Count > 0)
                                newRoot[property.Name] = arrayOfModels;
                        }
                        else 
                            WriteProperty(property, newRoot);
                    }
                    else if (property.Value is JObject)
                        ProcessObject(property.Name, property.Value, newRoot);
                }

                child = child.Next;
            }

            return newRoot;
        }

        private static void AddTypeToObject(JToken root, JToken newRoot)
        {
            string modelType;
            if (root["@xsi:type"] != null)
                modelType = root["@xsi:type"].ToString();
            else
                modelType = root.Parent.Path;

            Type t = GetModelTypeName(modelType);
            if (t != null)
                newRoot["$type"] = t.FullName + ", Models";
        }

        private static void ProcessObject(string name, JToken obj, JObject newRoot)
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
            else if (name.Equals("Script", StringComparison.CurrentCultureIgnoreCase))
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
            else if (name.Equals("PaddockList", StringComparison.CurrentCultureIgnoreCase))
            {
                // manager parameters.
                JArray values = new JArray();
                foreach (var child in obj.Children())
                {
                    var newObject = CreateObject(child.First);
                    values.Add(newObject);
                }
                newRoot[name] = values;
            }
            else
            {
                Type modelType = GetModelTypeName(name);
                if (modelType == null || modelType.GetInterface("IModel") == null)
                {
                    modelType = GetModelTypeName(JsonUtilities.Type(newRoot));
                    var property = modelType?.GetProperty(name);
                    var newObject = CreateObject(obj);
                    // If the new obejct is NOT a JArray, and this object is supposed to be an array...
                    if (!(newObject is JArray) && (arrayVariableNames.Contains(name) || (property != null && property.PropertyType.IsArray)))
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

                    if (newObject.Children().Count() == 1 && newObject.First.Path == "#text")
                        newRoot[name] = newObject.First.First;
                    else
                        newRoot[name] = newObject;
                }
                else
                    AddNewChild(obj, newRoot);
            }
        }

        private static JArray CreateArray(string name, JToken array, JObject newRoot)
        {
            JArray newArray = new JArray();
            if (array is JArray)
            {
                // Array of non models. e.g. array of Axis.
                foreach (var element in array.Children())
                {
                    Type modelType = GetModelTypeName(name);
                    if (name == "string" || name == "Command")
                        newArray.Add(new JValue(element.ToString()));
                    else if (name == "double")
                        newArray.Add(new JValue(double.Parse(element.ToString())));
                    else if (name == "int")
                        newArray.Add(new JValue(int.Parse(element.ToString())));
                    else if (name == "dateTime")
                        newArray.Add(new JValue(DateTime.Parse(element.ToString())));
                    else if (name == "ArrayOfString")
                    {
                        JArray nestedArray = new JArray();
                        foreach (var value in element.First.Values<JArray>())
                            newArray.Add(value);
                        //newArray.Add(nestedArray);
                    }
                    else if (modelType == null || modelType.GetInterface("IModel") == null)
                        newArray.Add(CreateObject(element));
                    else
                        AddNewChild(element, newRoot);
                }
            }
            else if (array is JValue)
            {
                // Simply put the single property into the array. e.g. report event names
                JValue value = array as JValue;
                if (name == "string" || name == "Command" || name == "Alias")
                    newArray.Add(new JValue(value.ToString()));
                else if (name == "double")
                    newArray.Add(new JValue(double.Parse(value.ToString())));
            }

            return newArray;
        }

        private static void AddNewChild(JToken element, JObject newRoot)
        {
            JToken newChild;
            if (element is JProperty)
            {
                newChild = new JObject();
                Type t = GetModelTypeName((element as JProperty).Name);
                if (t != null)
                    newChild["$type"] = t.FullName + ", Models";

                if (newChild["Name"] == null)
                    newChild["Name"] = (element as JProperty).Name;
            }
            else
            {
                newChild = CreateObject(element as JObject);
                if (newChild["Name"] == null && newChild["$type"] != null)
                {
                    string type = newChild["$type"].Value<string>().Replace(", Models", "");
                    string[] words = type.Split(".".ToCharArray());
                    newChild["Name"] = words.Last();
                }
            }


            if (newRoot["Children"] == null)
                newRoot["Children"] = new JArray();

            (newRoot["Children"] as JArray).Add(newChild);
        }

        private static void WriteProperty(JProperty property, JObject toObject)
        {
            string propertyName = property.Name;
            if (propertyName == "@Version")
                propertyName = "Version";
            if (propertyName == "#text" && property.Path.Contains("Memo"))
                return; // Old memo have #text, we don't want them.

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
                    if (property.Name == "Name")
                    {
                        if (JsonUtilities.Type(toObject) == "SoilCrop")
                            toObject["Name"] = GetSoilCropName(property.Value.ToString());
                        else
                            toObject[propertyName] = value;
                    }
                    else if (int.TryParse(value, out intValue))
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
            else if (propertyName == "@name") // Name attribute.
            {
                // SoilCrops copied from Apsim classic need to be renamed to CropNameSoil e.g. WheatSoil.
                if (toObject["$type"]?.ToString() == "Models.Soils.SoilCrop, Models")
                    toObject["Name"] = property.Value.ToString() + "Soil";
                else if (toObject["Name"] == null)
                    toObject["Name"] = property.Value;
            }
        }

        private static Type GetModelTypeName(string modelNameToFind)
        {
            if (modelNameToFind == null)
                return null;

            string[] modelWords = modelNameToFind.Split(".".ToCharArray());
            string m = modelWords[modelWords.Length - 1];

            Type[] types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                if (type.Name == m)
                {
                    return type;
                }
            }
            return null;
        }

        /// <summary>
        /// Make sure the child nodes of JToken are the same as for the original XML document.
        /// Do this recursively.
        /// </summary>
        /// <param name="jsonNode">The JSON node.</param>
        /// <param name="xmlNode">The XML node.</param>
        private static void ReorderChildren(JToken jsonNode, XmlNode xmlNode)
        {
            if (jsonNode["Children"] != null)
            {
                JArray newArray = new JArray();

                // Some simulations can have a 2 child models with same name.
                List<string> childNamesDone = new List<string>();

                JArray children = jsonNode["Children"] as JArray;
                foreach (var childXmlNode in XmlUtilities.ChildNodes(xmlNode, null))
                {
                    string childXmlName = XmlUtilities.Value(childXmlNode, "Name");

                    if (!childNamesDone.Contains(childXmlName))
                    {
                        if (childXmlName == string.Empty)
                        {
                            string nameAttribute = XmlUtilities.NameAttr(childXmlNode);
                            if (nameAttribute != null)
                                childXmlName = nameAttribute;
                            else if (GetModelTypeName(childXmlNode.Name) != null)
                                childXmlName = childXmlNode.Name;
                        }
                        if (childXmlName != string.Empty || GetModelTypeName(childXmlNode.Name) != null)
                        {
                            int i = 1;
                            foreach (var childJsonNode in children.Where(c => !(c is JArray) && c["Name"].ToString() == childXmlName || (c["$type"].ToString().Contains("SoilCrop") && c["Name"].ToString() == GetSoilCropName(childXmlName))))
                            {
                                bool alreadyAdded = newArray.FirstOrDefault(c => c["Name"].ToString() == childXmlName) != null;

                                if (childJsonNode != null)
                                {
                                    if (alreadyAdded)
                                    {
                                        string name = childJsonNode["Name"].ToString();
                                        string newName = name + i.ToString();
                                        childJsonNode["Name"] = newName;
                                        i++;
                                    }

                                    ReorderChildren(childJsonNode, childXmlNode);
                                    newArray.Add(childJsonNode);
                                }
                            }
                            childNamesDone.Add(childXmlName);
                        }
                    }
                }

                jsonNode["Children"] = newArray;
            }
        }

        /// <summary>
        /// Gets the name of a SoilCrop. This should start with an upper case
        /// letter and end with "Soil". e.g. WheatSoil.
        /// </summary>
        /// <param name="name">Name of the crop.</param>
        /// <returns></returns>
        /// <remarks>
        /// todo: rework the SoilCrop class so that this isn't necessary?
        /// </remarks>
        private static string GetSoilCropName(string name)
        {
            name = name.First().ToString().ToUpper() + name.Substring(1);
            if (!name.EndsWith("Soil"))
                name += "Soil";
            return name;
        }
    }
}
