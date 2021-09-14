using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// Contains utility functions for reading xml documentation comments
    /// in the source code.
    /// </summary>
    public static class CodeDocumentation
    {
        private static Dictionary<Assembly, XmlDocument> documentCache = new Dictionary<Assembly, XmlDocument>();

        /// <summary>
        /// Get the summary of a type removing CRLF.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummary(Type t)
        {
            XmlDocument document = LoadDocument(t.Assembly);
            return GetDocumentationElement(document, t.FullName, "summary", 'T');
        }

        /// <summary>
        /// Get the remarks tag of a type (if it exists).
        /// </summary>
        /// <param name="t">The type.</param>
        public static string GetRemarks(Type t)
        {
            return GetDocumentationElement(LoadDocument(t.Assembly), t.FullName, "remarks", 'T');
        }

        /// <summary>
        /// Get the summary of a member (field, property)
        /// </summary>
        /// <param name="member">The member to get the summary for.</param>
        public static string GetSummary(MemberInfo member)
        {
            var fullName = member.ReflectedType + "." + member.Name;
            XmlDocument document = LoadDocument(member.DeclaringType.Assembly);
            if (member is PropertyInfo)
                return GetDocumentationElement(document, fullName, "summary", 'P');
            else if (member is FieldInfo)
                return GetDocumentationElement(document, fullName, "summary", 'F');
            else if (member is EventInfo)
                return GetDocumentationElement(document, fullName, "summary", 'E');
            else if (member is MethodInfo method)
            {
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                return GetDocumentationElement(document, $"{fullName}({args})", "summary", 'M');
            }
            else
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
        }

        /// <summary>
        /// Get the remarks of a member (field, property) if it exists.
        /// </summary>
        /// <param name="member">The member.</param>
        public static string GetRemarks(MemberInfo member)
        {
            var fullName = member.ReflectedType + "." + member.Name;
            XmlDocument document = LoadDocument(member.DeclaringType.Assembly);
            if (member is PropertyInfo)
                return GetDocumentationElement(document, fullName, "remarks", 'P');
            else if (member is FieldInfo)
                return GetDocumentationElement(document, fullName, "remarks", 'F');
            else if (member is EventInfo)
                return GetDocumentationElement(document, fullName, "remarks", 'E');
            else if (member is MethodInfo method)
            {
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                return GetDocumentationElement(document, $"{fullName}({args})", "remarks", 'M');
            }
            else
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
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
    }
}
