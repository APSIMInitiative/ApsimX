using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Models.Core.ApsimFile
{
    /// <summary>
    /// A collection of json utilities.
    /// </summary>
    /// <remarks>
    /// If you write a new utility for this class, please write a unit test
    /// for it. See JsonUtilitiesTests.cs in the UnitTests project.
    /// </remarks>
    public static class JsonUtilities
    {
        /// <summary>
        /// Returns the name of a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <remarks>
        /// This actually fetches the 'Apsim' name of the node.
        /// e.g. For a Report called HarvestReport this will return 
        /// HarvestReport.
        /// </remarks>
        public static string Name(JToken node)
        {
            if (node == null)
                return null;

            if (node is JObject)
            {
                JProperty nameProperty = (node as JObject).Property("Name");
                if (nameProperty == null)
                    throw new Exception(string.Format("Attempted to fetch the name property of json node {0}.", node.ToString()));
                return (string)nameProperty.Value;
            }

            if (node is JProperty)
                return (node as JProperty).Name;

            return string.Empty;
        }

        /// <summary>
        /// Returns the type of an apsim model node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="withNamespace">
        /// If true, the namespace will be included in the returned type name.
        /// e.g. Models.Core.Simulations
        /// </param>
        public static string Type(JToken node, bool withNamespace = true)
        {
            // If the node is not a JObject, it is not an apsim model.
            if ( !(node is JObject) )
                return null;

            JProperty typeProperty = (node as JObject).Property("$type");

            if (typeProperty == null)
                return null;

            string typeName = (string)typeProperty.Value;

            // Type is written as "Namespace.TypeName, Assembly"
            // e.g. Models.Core.Simulations, Models
            int indexOfComma = typeName.IndexOf(',');
            if (indexOfComma >= 0)
                typeName = typeName.Substring(0, indexOfComma);

            if (!withNamespace)
            {
                int indexOfLastPeriod = typeName.LastIndexOf('.');
                if (indexOfLastPeriod >= 0)
                    typeName = typeName.Substring(indexOfLastPeriod + 1);
            }

            return typeName;
        }

        /// <summary>
        /// Returns the child models of a given node.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        public static List<JObject> Children(JObject node)
        {
            if (node == null)
                return new List<JObject>();

            JProperty childrenProperty = node.Property("Children");

            if (childrenProperty == null)
                return new List<JObject>();

            IEnumerable<JToken> children = childrenProperty.Values();

            if (children == null)
                return new List<JObject>();

            return children.Cast<JObject>().ToList();
        }

        /// <summary>
        /// Returns all descendants of a given node.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        public static List<JObject> ChildrenRecursively(JObject node)
        {
            List<JObject> descendants = new List<JObject>();
            Descendants(node, ref descendants);
            return descendants;
        }

        /// <summary>
        /// Returns a all descendants of a node, which are of a given type.
        /// Will never return null;
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">Type type name, with or without the namespace.</param>
        /// <returns></returns>
        public static List<JObject> ChildrenRecursively(JObject node, string typeFilter)
        {
            List<JObject> descendants = new List<JObject>();
            Descendants(node, ref descendants, typeFilter);
            return descendants;
        }

        /// <summary>
        /// Perform a search and replace in manager script.
        /// </summary>
        /// <param name="manager">The manager model.</param>
        /// <param name="searchPattern">The string to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        public static void ReplaceManagerCode(JObject manager, string searchPattern, string replacePattern)
        {
            string code = manager["Code"]?.ToString();
            if (code == null || searchPattern == null)
                return;
            manager["Code"] = code.Replace(searchPattern, replacePattern);
        }

        /// <summary>
        /// Perform a search and replace in manager script.
        /// </summary>
        /// <param name="manager">The manager model.</param>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        /// <param name="options">Regular expression options to use. Default value is none.</param>
        public static void ReplaceManagerCodeUsingRegex(JObject manager, string searchPattern, string replacePattern, RegexOptions options = RegexOptions.None)
        {
            string code = manager["Code"]?.ToString();
            if (code == null || searchPattern == null)
                return;
            manager["Code"] = Regex.Replace(code, searchPattern, replacePattern, options);
        }

        /// <summary>
        /// Helper method for <see cref="ChildrenRecursively(JObject)"/>.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="descendants">List of descendants.</param>
        /// <param name="typeFilter">Type name by which to filter.</param>
        private static void Descendants(JObject node, ref List<JObject> descendants, string typeFilter = null)
        {
            if (node == null)
                return;

            List<JObject> children = Children(node);
            if (children == null)
                return;

            if (descendants == null)
                descendants = new List<JObject>();

            foreach (JObject child in children)
            {
                if (string.IsNullOrEmpty(typeFilter) || Type(child, typeFilter.Contains('.')) == typeFilter)
                    descendants.Add(child);
                Descendants(child, ref descendants, typeFilter);
            }
        }
    }
}
