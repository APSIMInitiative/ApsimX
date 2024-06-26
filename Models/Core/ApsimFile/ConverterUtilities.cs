using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using APSIM.Shared.Utilities;
using Newtonsoft.Json.Linq;

namespace Models.Core.ApsimFile
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class ConverterUtilities
    {
        /// <summary>
        /// Perform a search and replace in manager script. Also optionally insert a using statement.
        /// </summary>
        /// <param name="manager">The manager model.</param>
        /// <param name="searchPattern">The pattern to search for</param>
        /// <param name="replacePattern">The string to replace</param>
        /// <param name="usingStatement">An optional using statement to insert at top of the script.</param>
        internal static void SearchReplaceManagerCodeUsingRegEx(XmlNode manager, string searchPattern, string replacePattern, string usingStatement = null)
        {
            XmlCDataSection codeNode = XmlUtilities.Find(manager, "Code").ChildNodes[0] as XmlCDataSection;
            string newCode = Regex.Replace(codeNode.InnerText, searchPattern, replacePattern, RegexOptions.None);
            if (codeNode.InnerText != newCode)
            {
                // Replacement was done so need to add using statement.
                if (usingStatement != null)
                    newCode = InsertUsingStatementInManagerCode(newCode, usingStatement);
                codeNode.InnerText = newCode;
            }
        }

        /// <summary>
        /// Perform a search and replace in report variables.
        /// </summary>
        /// <param name="report">The reportr model.</param>
        /// <param name="searchPattern">The pattern to search for</param>
        /// <param name="replacePattern">The string to replace</param>
        internal static void SearchReplaceReportCodeUsingRegEx(XmlNode report, string searchPattern, string replacePattern)
        {
            List<string> variableNames = XmlUtilities.Values(report, "VariableNames/string");
            for (int i = 0; i < variableNames.Count; i++)
                variableNames[i] = Regex.Replace(variableNames[i], searchPattern, replacePattern, RegexOptions.None);
            XmlUtilities.SetValues(report, "VariableNames/string", variableNames);
        }

        /// <summary>
        /// Perform a search and replace in report variables.
        /// </summary>
        /// <param name="report">The reportr model.</param>
        /// <param name="searchPattern">The pattern to search for</param>
        /// <param name="replacePattern">The string to replace</param>
        internal static void SearchReplaceReportCode(XmlNode report, string searchPattern, string replacePattern)
        {
            List<string> variableNames = XmlUtilities.Values(report, "VariableNames/string");
            for (int i = 0; i < variableNames.Count; i++)
                variableNames[i] = variableNames[i].Replace(searchPattern, replacePattern);
            XmlUtilities.SetValues(report, "VariableNames/string", variableNames);
        }

        /// <summary>
        /// Add the specified 'using' statement to the specified code.
        /// </summary>
        /// <param name="code">The code to modifiy</param>
        /// <param name="usingStatement">The using statement to insert at the correct location</param>
        internal static string InsertUsingStatementInManagerCode(string code, string usingStatement)
        {
            // Find all using statements
            StringReader reader = new StringReader(code);
            StringWriter writer = new StringWriter();
            bool foundUsingStatement = false;
            bool foundEndOfUsingSection = false;
            string line = reader.ReadLine();
            do
            {
                if (!line.Trim().StartsWith("using ") && !foundEndOfUsingSection)
                {
                    foundEndOfUsingSection = true;
                    if (!foundUsingStatement)
                        writer.WriteLine(usingStatement);
                }
                else
                    foundUsingStatement = foundUsingStatement || line.Trim() == usingStatement;
                writer.WriteLine(line);
                line = reader.ReadLine();
            }
            while (line != null);
            return writer.ToString();
        }


        /// <summary>
        /// Add the specified 'using' statement to the specified code.
        /// </summary>
        /// <param name="manager">The manager to modifiy</param>
        /// <param name="linkStatement">The link statement to insert at the correct location</param>
        internal static void InsertLink(XmlNode manager, string linkStatement)
        {
            XmlCDataSection codeNode = XmlUtilities.Find(manager, "Code").ChildNodes[0] as XmlCDataSection;
            string code = codeNode.InnerText;

            string returnCode = code;
            if (!code.Contains(linkStatement))
            {

                int curlyIndex = code.IndexOf('{');
                curlyIndex = code.IndexOf('{', curlyIndex + 1); // look for second curly bracket.
                if (curlyIndex >= 0)
                {
                    returnCode = code.Substring(0, curlyIndex + 1);
                    returnCode += Environment.NewLine + "        " + linkStatement;
                    returnCode += code.Substring(curlyIndex + 2);
                }
            }
            codeNode.InnerText = returnCode;
        }


        /// <summary>
        /// Find a PMF node, as a direct child under the specified node, that has the specified name element.
        /// </summary>
        /// <param name="node">The XML Nnde to search</param>
        /// <param name="name">The name of the element to search for</param>
        /// <returns>The node or null if not found</returns>
        internal static XmlNode FindModelNode(XmlNode node, string name)
        {
            foreach (XmlNode child in node.ChildNodes)
                if (XmlUtilities.Value(child, "Name") == name)
                    return child;
            return null;
        }

        /// <summary>
        /// Find model nodes of the specified type and name
        /// </summary>
        /// <param name="node">The node to search under</param>
        /// <param name="modelType">The type name of the model to look for</param>
        /// <param name="modelName">The name of the model to look for</param>
        internal static List<XmlNode> FindModelNodes(XmlNode node, string modelType, string modelName)
        {
            List<XmlNode> modelNodes = new List<XmlNode>();
            foreach (XmlNode child in XmlUtilities.FindAllRecursivelyByType(node, modelType))
                if (XmlUtilities.Value(child, "Name") == modelName)
                    modelNodes.Add(child);
            return modelNodes;
        }

        /// <summary>
        /// Rename a variable or fragment.
        /// </summary>
        /// <param name="node">The node to modifiy</param>
        /// <param name="searchFor">The pattern to search for</param>
        /// <param name="replaceWith">The string to replace</param>
        internal static void RenameVariable(XmlNode node, string searchFor, string replaceWith)
        {
            foreach (XmlNode manager in XmlUtilities.FindAllRecursivelyByType(node, "Manager"))
                SearchReplaceManagerCode(manager, searchFor, replaceWith);
            foreach (XmlNode report in XmlUtilities.FindAllRecursivelyByType(node, "Report"))
                SearchReplaceReportCode(report, searchFor, replaceWith);
            foreach (XmlNode graph in XmlUtilities.FindAllRecursivelyByType(node, "Graph"))
                SearchReplaceGraphCode(graph, searchFor, replaceWith);
            foreach (XmlNode cultivar in XmlUtilities.FindAllRecursivelyByType(node, "Cultivar"))
                SearchReplaceCultivarOverrides(cultivar, searchFor, replaceWith);
            foreach (XmlNode variableName in XmlUtilities.FindAllRecursivelyByType(node, "VariableName"))
                variableName.InnerText = variableName.InnerText.Replace(searchFor, replaceWith);
            foreach (XmlNode xproperty in XmlUtilities.FindAllRecursivelyByType(node, "XProperty"))
                xproperty.InnerText = xproperty.InnerText.Replace(searchFor, replaceWith);
            foreach (XmlNode yproperty in XmlUtilities.FindAllRecursivelyByType(node, "YProperty"))
                yproperty.InnerText = yproperty.InnerText.Replace(searchFor, replaceWith);
            foreach (XmlNode expression in XmlUtilities.FindAllRecursivelyByType(node, "Expression"))
                expression.InnerText = expression.InnerText.Replace(searchFor, replaceWith);
        }

        /// <summary>
        /// Perform a search and replace in cultivar commands
        /// </summary>
        /// <param name="cultivar">Cultivar node</param>
        /// <param name="searchFor">The pattern to search for</param>
        /// <param name="replaceWith">The string to replace</param>
        public static void SearchReplaceCultivarOverrides(XmlNode cultivar, string searchFor, string replaceWith)
        {
            List<string> commands = XmlUtilities.Values(cultivar, "Command");
            for (int i = 0; i < commands.Count; i++)
                commands[i] = commands[i].Replace(searchFor, replaceWith);
            XmlUtilities.SetValues(cultivar, "Command", commands);
        }

        /// <summary>
        /// Perform a search and replace in manager script. 
        /// </summary>
        /// <param name="manager">The manager model.</param>
        /// <param name="searchFor">The pattern to search for</param>
        /// <param name="replaceWith">The string to replace</param>
        internal static bool SearchReplaceManagerCode(XmlNode manager, string searchFor, string replaceWith)
        {
            XmlCDataSection codeNode = XmlUtilities.Find(manager, "Code").ChildNodes[0] as XmlCDataSection;
            string newCode = codeNode.InnerText.Replace(searchFor, replaceWith);
            bool wasChanged = newCode != codeNode.InnerText;
            codeNode.InnerText = newCode;

            // Now look under the script node.
            XmlNode script = XmlUtilities.Find(manager, "Script");
            if (script != null)
                foreach (XmlNode scriptChild in script.ChildNodes)
                    scriptChild.InnerText = scriptChild.InnerText.Replace(searchFor, replaceWith);
            return wasChanged;
        }

        /// <summary>
        /// Perform a search and replace in graph x/y variables.
        /// </summary>
        /// <param name="graph">The graph model.</param>
        /// <param name="searchFor">The pattern to search for</param>
        /// <param name="replaceWith">The string to replace</param>
        internal static void SearchReplaceGraphCode(XmlNode graph, string searchFor, string replaceWith)
        {
            foreach (XmlNode series in XmlUtilities.FindAllRecursivelyByType(graph, "Series"))
            {
                XmlNode variable = XmlUtilities.Find(series, "XFieldName");
                if (variable != null)
                    variable.InnerText = variable.InnerText.Replace(searchFor, replaceWith);
                variable = XmlUtilities.Find(series, "YFieldName");
                if (variable != null)
                    variable.InnerText = variable.InnerText.Replace(searchFor, replaceWith);
                variable = XmlUtilities.Find(series, "X2FieldName");
                if (variable != null)
                    variable.InnerText = variable.InnerText.Replace(searchFor, replaceWith);
                variable = XmlUtilities.Find(series, "Y2FieldName");
                if (variable != null)
                    variable.InnerText = variable.InnerText.Replace(searchFor, replaceWith);
            }
        }

        /// <summary>
        /// Add a constant function to the specified xml node.
        /// </summary>
        /// <param name="node">The xml node to add constant to</param>
        /// <param name="name">The name of the constant function</param>
        /// <param name="fixedValue">The fixed value of the constant function</param>
        internal static void AddConstantFuntionIfNotExists(XmlNode node, string name, string fixedValue)
        {
            if (FindModelNode(node, name) == null)
            {
                XmlNode constant = node.AppendChild(node.OwnerDocument.CreateElement("Constant"));
                XmlUtilities.SetValue(constant, "Name", name);
                XmlUtilities.SetValue(constant, "FixedValue", fixedValue);
            }
        }

        /// <summary>
        /// Add a variable reference function to the specified xml node.
        /// </summary>
        /// <param name="node">The xml node to add constant to</param>
        /// <param name="name">The name of the constant function</param>
        /// <param name="reference">The reference to put into the function</param>
        internal static void AddVariableReferenceFuntionIfNotExists(XmlNode node, string name, string reference)
        {
            if (FindModelNode(node, name) == null)
            {
                XmlNode critNConc = node.AppendChild(node.OwnerDocument.CreateElement("VariableReference"));
                XmlUtilities.SetValue(critNConc, "Name", name);
                XmlUtilities.SetValue(critNConc, "VariableName", reference);
            }
        }

        /// <summary>
        /// Rename a XML node.
        /// </summary>
        /// <param name="node">The xml node to add constant to</param>
        /// <param name="oldName">The name to look for</param>
        /// <param name="newName">The new name</param>
        internal static void RenameNode(XmlNode node, string oldName, string newName)
        {
            List<XmlNode> nodesToRename = XmlUtilities.FindAllRecursivelyByType(node, oldName);
            foreach (XmlNode nodeToRename in nodesToRename)
                XmlUtilities.Rename(nodeToRename.ParentNode, oldName, newName);
        }

        /// <summary>
        /// Rename a PMF function
        /// </summary>
        /// <param name="node">The node to search under</param>
        /// <param name="parentName">The name of the parent node to look for</param>
        /// <param name="oldName">The old name of the function to replace</param>
        /// <param name="newName">The new replacement name</param>
        internal static void RenamePMFFunction(XmlNode node, string parentName, string oldName, string newName)
        {
            foreach (XmlNode child in XmlUtilities.FindAllRecursivelyByType(node, parentName))
            {
                XmlNode pmfNode = FindModelNode(child, oldName);
                if (pmfNode != null)
                    XmlUtilities.SetValue(pmfNode, "Name", newName);
            }
        }

        /// <summary>Get a list of model names under the specified node</summary>
        /// <param name="node">Root node</param>
        internal static List<string> GetAllModelNames(XmlNode node)
        {
            SortedSet<string> childNames = new SortedSet<string>();

            foreach (XmlNode child in XmlUtilities.ChildNodesRecursively(node, typeFilter: null))
            {
                string name = XmlUtilities.Value(child, "Name");
                if (name != string.Empty)
                    childNames.Add(name);
            }
            return childNames.ToList();
        }

        /// <summary></summary>
        /// <param name="node1">Node1</param>
        /// <param name="node2">Node2</param>
        internal static bool NodesInSameSimulation(JContainer node1, JContainer node2)
        {
            JContainer sim1 = node1;
            while(sim1 != null && sim1["$type"].ToString() != "Models.Core.Simulation, Models")
            {
                sim1 = sim1.Parent;
                if (sim1 != null)
                    sim1 = sim1.Parent;
                if (sim1 != null)
                    sim1 = sim1.Parent;
            }

            JContainer sim2 = node2;
            while(sim2 != null && sim2["$type"].ToString() != "Models.Core.Simulation, Models")
            {
                sim2 = sim2.Parent;
                if (sim2 != null)
                    sim2 = sim2.Parent;
                if (sim2 != null)
                    sim2 = sim2.Parent;
            }

            if (sim1.Path.CompareTo(sim2.Path) == 0)
                return true;
            else
                return false;
        }
    }
}


