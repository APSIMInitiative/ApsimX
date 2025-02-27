using APSIM.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// Contains utility functions for reading xml documentation comments
    /// in the source code.
    /// </summary>
    public static class CodeDocumentation
    {
        private const string summaryTagName = "summary";
        private const string remarksTagName = "remarks";

        private static Dictionary<Assembly, XmlDocument> documentCache = new Dictionary<Assembly, XmlDocument>();

        private static Dictionary<string, string> modelDescriptionCache = new Dictionary<string, string>();

        /// <summary>
        /// Get the summary of a type removing CRLF.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummary(Type t)
        {
            string text = GetCustomTag(t, summaryTagName);
            if (text != null)
                return text.Replace("-", " ");
            else
                return "";
        }

        /// <summary>
        /// Get the remarks tag of a type (if it exists).
        /// </summary>
        /// <param name="t">The type.</param>
        public static string GetRemarks(Type t)
        {
            string text = GetCustomTag(t, remarksTagName);
            if (text != null)
                return text.Replace("-", " ");
            else
                return "";
        }

        /// <summary>
        /// Get the contents of a given xml element from the documentation of a type
        /// if it exists.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <param name="tagName">The name of the xml element in the documentation to be reaed. E.g. "summary".</param>
        public static string GetCustomTag(Type t, string tagName)
        {
            return GetDocumentationElement(LoadDocument(t.Assembly), t.FullName, tagName, 'T');
        }

        /// <summary>
        /// Get the summary of a member (field, property)
        /// </summary>
        /// <param name="member">The member to get the summary for.</param>
        public static string GetSummary(MemberInfo member)
        {
            return GetCustomTag(member, summaryTagName);
        }

        /// <summary>
        /// Get the remarks of a member (field, property) if it exists.
        /// </summary>
        /// <param name="member">The member.</param>
        public static string GetRemarks(MemberInfo member)
        {
            return GetCustomTag(member, remarksTagName);
        }

        /// <summary>
        /// Get the contents of a given xml element from the documentation of a member
        /// (field, property) if it exists.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="tagName">The name of the xml element in the documentation to be reaed. E.g. "summary".</param>
        public static string GetCustomTag(MemberInfo member, string tagName)
        {
            XmlDocument document = LoadDocument(member.DeclaringType.Assembly);
            
            //make a list of types to check
            Type type = member.ReflectedType.BaseType;
            List<Type> types = new List<Type>();
            while(type != null)
            {
                types.Add(type);
                type = type.BaseType;
            }
            types.Add(member.ReflectedType);
            //Then reverse the list to do this type first, then work upwards from model for preformance
            types.Reverse();

            foreach(Type t in types)
            {
                string result = GetCustomTagDetails(member, tagName, document, t);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="tagName">The name of the xml element in the documentation to be reaed. E.g. "summary".</param>
        /// <param name="document"></param>
        /// <param name="type"></param>
        public static string GetCustomTagDetails(MemberInfo member, string tagName, XmlDocument document, Type type)
        {
            string fullName = type.FullName + "." + member.Name;

            char typeLetter = ' ';
            if (member is PropertyInfo)
                typeLetter = 'P';
            else if (member is FieldInfo)
                typeLetter = 'F';
            else if (member is EventInfo)
                typeLetter = 'E';
            else if (member is MethodInfo method)
                typeLetter = 'M';

            if (typeLetter == 'P' || typeLetter == 'F' || typeLetter == 'E')
            {
                string cacheName = fullName+tagName;
                string description = "";
                if (modelDescriptionCache.TryGetValue(cacheName, out description))
                    return description;

                description = GetDocumentationElement(document, fullName, tagName, typeLetter);
                if (!string.IsNullOrEmpty(description))
                {
                    modelDescriptionCache.Add(cacheName, description);
                    return description;
                }
            }
            else if (typeLetter == 'M')
            {
                MethodInfo method = member as MethodInfo;
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                string name = $"{fullName}({args})";

                string cacheName = name+tagName;
                string description = "";
                if (modelDescriptionCache.TryGetValue(cacheName, out description))
                    return description;

                description = GetDocumentationElement(document, name, tagName, typeLetter);
                if (!string.IsNullOrEmpty(description))
                {
                    modelDescriptionCache.Add(cacheName, description);
                    return description;
                }
            }
            else
            {
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
            }
            return "";
        }

        /// <summary>
        /// Get the Events of function of the given type.
        /// Model source file must be included as embedded resource in project.
        /// A string is return with a Event Handle Name and Summary Comment seperated by a tab character
        /// and each Event sperated by a newline character.
        /// </summary>
        /// <param name="type">The type of the model being documented</param>
        /// <param name="functionName">Function name with Arguements as a string</param>
        public static List<string[]> GetEventsInvokedInOrder(Type type, string functionName)
        {
            //load the source file as a string form the binary resources
            Assembly assembly = type.Assembly;
            string fullName = type.FullName;
            string filename = $"{fullName}.cs";
            string raw = ReflectionUtilities.GetResourceAsString(assembly, filename);
            if (raw == null)
                throw new Exception($"Documentation Error: {fullName} could not be found in {filename}. Has it been included as an Embedded Resource?");

            string functionString = GetFunctionStringFromRawFile(filename, raw, functionName);

            List<string[]> eventsNamesInOrder = new List<string[]>();
            //get all the event handles that are invoked in the function
            MatchCollection matches = Regex.Matches(functionString, @"(\w+)\??\.Invoke\(.+");
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    //name will be in group 0 if it worked.
                    string handleString = match.Groups[1].Value;
                    //use the name to get the summary notes
                    string summary = "";
                    MemberInfo[] member = type.GetMember(handleString);
                    if (member.Length > 0)
                        summary += GetSummary(member[0]);

                    //store as string array of two parts
                    string[] parts = new string[2];
                    parts[0] = $"{handleString}";
                    parts[1] = $"{summary}\n";

                    eventsNamesInOrder.Add(parts);
                }
                else
                {
                    throw new Exception($"Documentation Error: Regex failed on \"{match.Value}\" Event Handle Name was found.");
                }
            }
            return eventsNamesInOrder;
        }

        private static string GetDocumentationElement(XmlDocument document, string path, string element, char typeLetter)
        {
            path = path.Replace("+", ".");

            string xpath = $"/doc/members/member[@name='{typeLetter}:{path}']/{element}";
            XmlNode summaryNode = document.SelectSingleNode(xpath);
            if (summaryNode != null)
            {
                string raw = summaryNode.InnerXml.Trim();
                // Need to fix multiline comments - remove newlines and consecutive spaces.
                return Regex.Replace(raw, @"\n[ \t]+", "\n");
            }
            return null;
        }

        private static XmlDocument LoadDocument(Assembly assembly)
        {
            //This returns an empty XML if we tried to load a private system library that won't have an xml.
            //Just stops error throws in debugging mode
            if (assembly.FullName.StartsWith("System.Private"))
                return new XmlDocument();

            if (documentCache.ContainsKey(assembly))
            {
                XmlDocument result = documentCache[assembly];
                if (result != null)
                    return result;
            }
            string fileName = Path.ChangeExtension(assembly.Location, ".xml");

            if (File.Exists(fileName))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);
                documentCache[assembly] = doc;
                return doc;
            }
            throw new FileNotFoundException($"XML Documentation could not be located for assembly {assembly.FullName}");
        }

        private static string GetFunctionStringFromRawFile(string fileName, string fileContents, string functionName)
        {
            //find the name of the function we are searching for and
            //move to the next curly brace
            int functionPos = fileContents.IndexOf(functionName);
            if (functionPos == -1)
                throw new Exception($"Documentation Error: {functionName} does not exist in {fileName}.");

            int braceStart = fileContents.IndexOf("{", functionPos);

            //Move through the file until we find where that curly brace is closed
            int braceCount = 1;
            int i = braceStart + 1; //so we've already counted the first brace.
            while (braceCount > 0 && i < fileContents.Length)
            {
                if (fileContents[i] == '{')
                    braceCount += 1;
                else if (fileContents[i] == '}')
                    braceCount -= 1;

                i += 1;
            }

            if (i > fileContents.Length)
                throw new Exception("Documentation Error: Uneven number of curly braces { } in " + fileName);

            //remove the last closing curly brace and return all code between start and end
            return fileContents.Substring(braceStart, i - braceStart);
        }
    }
}
