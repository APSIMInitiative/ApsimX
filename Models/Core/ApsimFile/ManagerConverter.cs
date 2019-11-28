// -----------------------------------------------------------------------
// <copyright file="ManagerConverter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System;
    using Newtonsoft.Json.Linq;
    using System.Text;

    /// <summary>
    /// Provides helper methods to read and manipulate manager scripts.
    /// </summary>
    public class ManagerConverter
    {
        private List<string> lines = new List<string>();
        private JObject manager;

        /// <summary>Default constructor.</summary>
        public ManagerConverter() { }

        /// <summary>
        /// Parameters (public properties with a display attribute) of the manager script.
        /// </summary>
        public Dictionary<string, string> Parameters
        {
            get
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                if (manager["Parameters"] == null)
                    return parameters;

                foreach (var parameter in manager["Parameters"])
                    parameters.Add(parameter["Key"].ToString(), parameter["Value"].ToString());
                return parameters;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="manager">The JSON manager object.</param>
        public ManagerConverter(JObject manager)
        {
            this.manager = manager;
            if (manager["Code"] != null)
                Read(manager["Code"].ToString());
        }

        /// <summary>Load script</summary>
        /// <param name="script">The manager script to work on</param>
        public void Read(string script)
        {
            lines.Clear();
            using (StringReader reader = new StringReader(script))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    line = line.Replace("\t", "    ");
                    lines.Add(line);
                    line = reader.ReadLine();
                }
            }
        }

        /// <summary>Load script</summary>
        /// <param name="node">The manager node to read from</param>
        public void Read(XmlNode node)
        {
            XmlCDataSection codeNode = XmlUtilities.Find(node, "Code").ChildNodes[0] as XmlCDataSection;
            Read(codeNode.InnerText);
        }

        /// <summary>Write script</summary>
        /// <param name="node">The manager node to write to</param>
        public void Write(XmlNode node)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            lines.ForEach(line => builder.AppendLine(line));

            XmlCDataSection codeNode = XmlUtilities.Find(node, "Code").ChildNodes[0] as XmlCDataSection;
            codeNode.InnerText = builder.ToString();
        }

        /// <summary>
        /// Save the manager object code back to the manager JSON object.
        /// </summary>
        public void Save()
        {
            manager["Code"] = ToString();
        }

        /// <summary>Write script</summary>
        public new string ToString()
        {
            if (lines.Count == 0)
                return null;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            lines.ForEach(line => builder.AppendLine(line));
            return builder.ToString();
        }

        /// <summary>Get all using statements.</summary>
        public IEnumerable<string> GetUsingStatements()
        {
            int startUsing;
            int endUsing;
            FindUsingBlock(out startUsing, out endUsing);

            List<string> usings = new List<string>();
            if (startUsing != -1 && endUsing != -1)
            {
                for (int i = startUsing; i <= endUsing; i++)
                {
                    string cleanLine = Clean(lines[i]);

                    if (cleanLine != string.Empty)
                    {
                        string[] words = cleanLine.Split(' ');
                        usings.Add(words[1].Trim().Replace(";", ""));
                    }
                }
            }

            return usings;
        }

        /// <summary>Set using statements.</summary>
        /// <param name="usings">Using statements to write</param>
        public void SetUsingStatements(IEnumerable<string> usings)
        {
            int startUsing;
            int endUsing;
            FindUsingBlock(out startUsing, out endUsing);

            if (startUsing != -1 && endUsing != -1)
            {
                // Remove old using statements
                lines.RemoveRange(startUsing, endUsing - startUsing + 1);

                foreach (string usingAssembly in usings)
                    lines.Insert(startUsing, "using " + usingAssembly + ";");
            }
        }

        /// <summary>Gets a collection of declarations.</summary>
        public List<Declaration> GetDeclarations()
        {
            List<Declaration> foundDeclarations = new List<Declaration>();

            string pattern = @"(?<Link>\[.+\])?\s+(?<Access>public\s+|private\s+)?(?<TypeName>\w+)\s+(?<InstanceName>\w+)\s*(=\s*null)?;";
            for (int i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], pattern);
                if (match.Groups["TypeName"].Value != string.Empty &&
                    match.Groups["TypeName"].Value != "as" &&
                    match.Groups["InstanceName"].Value != string.Empty &&
                    match.Groups["TypeName"].Value != "using")
                {
                    Declaration decl = new Declaration();
                    decl.LineIndex = i;
                    decl.TypeName = match.Groups["TypeName"].Value;
                    decl.InstanceName = match.Groups["InstanceName"].Value;
                    decl.Attributes = new List<string>();
                    decl.IsEvent = lines[i].Contains("event");
                    decl.IsPrivate = !decl.IsEvent && match.Groups["Access"].Value.TrimEnd() != "public";
                    if (match.Groups["Link"].Success)
                    {
                        decl.Attributes.Add(match.Groups["Link"].Value);
                        decl.AttributesOnPreviousLines = false;
                    }

                    // Look on previous lines for attributes.
                    string linkPattern = @"(?<Link>\[.+\])\s*$";
                    for (int j = i - 1; j >= 0; j--)
                    {
                        Match linkMatch = Regex.Match(Clean(lines[j]), linkPattern);
                        if (linkMatch.Success)
                            decl.Attributes.Add(linkMatch.Groups["Link"].Value);
                        else
                            break;
                    }

                    foundDeclarations.Add(decl);
                }
            }
            return foundDeclarations;
        }

        /// <summary>
        /// Set the complete list of declarations.
        /// </summary>
        /// <param name="newDeclarations">A list of declarations for the manager model.</param>
        public void SetDeclarations(List<Declaration> newDeclarations)
        {
            int lineNumberStartDeclarations;
            
            var existingDeclarations = GetDeclarations();
            if (existingDeclarations.Count == 0)
            {
                lineNumberStartDeclarations = FindStartOfClass();
                if (lineNumberStartDeclarations == -1)
                {
                    lines.AddRange(new string[]
                    {
                        "namespace Models",
                        "{",
                        "    [Serializable]",
                        "    public class Script : Model",
                        "    {",
                        "    }",
                        "}"
                    });
                }
            }
            else
            {
                // Remove existing declarations
                for (int i = existingDeclarations.Count-1; i >= 0; i--)
                {
                    int beginLineIndex = existingDeclarations[i].LineIndex;
                    if (existingDeclarations[i].AttributesOnPreviousLines)
                        beginLineIndex -= existingDeclarations[i].Attributes.Count;

                    int numLinesToRemove = existingDeclarations[i].LineIndex - beginLineIndex + 1;
                    lines.RemoveRange(beginLineIndex, numLinesToRemove);
                }
            }

            lineNumberStartDeclarations = FindStartOfClass();

            foreach (var newDeclaration in newDeclarations)
            {
                var declarationLineBuilder = new StringBuilder();
                declarationLineBuilder.Append("        ");
                if (newDeclaration.AttributesOnPreviousLines)
                {
                    // Write attributes
                    foreach (var attribute in newDeclaration.Attributes)
                    {
                        lines.Insert(lineNumberStartDeclarations, "        " + attribute + "");
                        lineNumberStartDeclarations++;
                    }
                }
                else
                {
                    // Write attributes
                    foreach (var attribute in newDeclaration.Attributes)
                        declarationLineBuilder.Append(attribute);
                    declarationLineBuilder.Append(' ');
                }

                // Write declaration
                if (newDeclaration.IsPrivate)
                    declarationLineBuilder.Append("private ");
                else
                    declarationLineBuilder.Append("public ");
                if (newDeclaration.IsEvent)
                    declarationLineBuilder.Append(" event ");
                declarationLineBuilder.Append(newDeclaration.TypeName);
                declarationLineBuilder.Append(' ');
                declarationLineBuilder.Append(newDeclaration.InstanceName);
                declarationLineBuilder.Append(';');
                lines.Insert(lineNumberStartDeclarations, declarationLineBuilder.ToString());
                lineNumberStartDeclarations++;
            }
        }

        /// <summary>
        /// Find 0 or more method calls that match the instanceType/methodName
        /// </summary>
        /// <param name="instanceType">The instance type (from manager field declaration)</param>
        /// <returns></returns>
        /// <param name="methodName">The name of the method</param>
        public List<MethodCall> FindMethodCalls(string instanceType, string methodName)
        {
            List<MethodCall> methods = new List<MethodCall>();
            int lineIndex = FindString("." + methodName);
            while (lineIndex != -1)
            {
                // Process method line.
                string pattern = @"(?<InstanceName>\w+)." + methodName + @"\s*\((?<Arguments>.*)\)";
                Match match = Regex.Match(lines[lineIndex], pattern);

                if (match != null)
                {
                    string instanceName = match.Groups["InstanceName"].Value;
                    Declaration decl = GetDeclarations().Find(d => d.InstanceName == instanceName);
                    if (decl != null && decl.TypeName == instanceType)
                    {
                        MethodCall method = new MethodCall();
                        method.LineIndex = lineIndex;
                        method.MethodName = methodName;
                        method.InstanceName = instanceName;
                        method.Arguments = new List<string>();
                        string arguments = match.Groups["Arguments"].Value;
                        foreach (string argument in arguments.Split(','))
                            method.Arguments.Add(argument.Trim());
                        methods.Add(method);
                    }
                }

                lineIndex = FindString("." + methodName, lineIndex+1);
            }
            return methods;
        }

        /// <summary>
        /// Store the the specified method call, replacing the line. 
        /// </summary>
        /// <param name="method">Details of the method call</param>
        public void SetMethodCall(MethodCall method)
        {
            int indent = lines[method.LineIndex].Length - lines[method.LineIndex].TrimStart().Length;

            string newLine = new string(' ', indent);
            newLine += method.InstanceName + "." + method.MethodName;
            newLine += "(";
            newLine += StringUtilities.BuildString(method.Arguments.ToArray(), ", ");
            newLine += ");";
            lines[method.LineIndex] = newLine;
        }

        /// <summary>
        /// Search for the old string and replace with the new string.
        /// </summary>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        /// <param name="caseSensitive">Case sensitive?</param>
        /// <returns></returns>
        public bool Replace(string searchPattern, string replacePattern, bool caseSensitive = false)
        {
            if (searchPattern == null)
                return false;

            bool replacementDone = false;
            for (int i = 0; i < lines.Count; i++)
            {
                StringComparison comparison = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                int pos = lines[i].IndexOf(searchPattern, comparison);
                while (pos != -1)
                {
                    lines[i] = lines[i].Remove(pos, searchPattern.Length);
                    lines[i] = lines[i].Insert(pos, replacePattern);
                    replacementDone = true;
                    pos = lines[i].IndexOf(searchPattern, pos+1);
                }
            }
            return replacementDone;
        }

        /// <summary>
        /// Perform a search and replace in manager script.
        /// </summary>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        /// <param name="options">Regular expression options to use. Default value is none.</param>
        public bool ReplaceRegex(string searchPattern, string replacePattern, RegexOptions options = RegexOptions.None)
        {
            bool replacementDone = false;
            string oldCode = ToString();
            if (oldCode == null || searchPattern == null)
                return false;
            var newCode = Regex.Replace(oldCode, searchPattern, replacePattern, options);
            if (newCode != oldCode)
            {
                Read(newCode);
                replacementDone = true;
            }
            return replacementDone;
        }

        /// <summary>
        /// Add a declaration if it doesn't exist.
        /// </summary>
        /// <param name="typeName">The type name of the declaration.</param>
        /// <param name="instanceName">The instance name of the declaration.</param>
        /// <param name="attributes">The attributes of the declaration e.g. [Link].</param>
        /// <returns>true if link was inserted.</returns>
        public bool AddDeclaration(string typeName, string instanceName, IEnumerable<string> attributes = null)
        {
            var declarations = GetDeclarations();
            var declaration = declarations.Find(d => d.InstanceName == instanceName);

            if (declaration == null)
            {
                declarations.Add(new Declaration()
                {
                    TypeName = typeName,
                    InstanceName = instanceName,
                    Attributes = attributes.ToList()
                });

                SetDeclarations(declarations);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove a declaration.
        /// </summary>
        /// <param name="instanceName">The instance name of the declaration.</param>
        /// <returns>true if link was inserted.</returns>
        public bool RemoveDeclaration(string instanceName)
        {
            var declarations = GetDeclarations();
            var declaration = declarations.Find(d => d.InstanceName == instanceName);
            if (declaration != null)
            {
                declarations.Remove(declaration);
                SetDeclarations(declarations);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Find a line with the matching string
        /// </summary>
        /// <param name="stringToFind">String to find.</param>
        /// <param name="startIndex">LineNumber to start search from</param>
        /// <returns>The index of the line of the match or -1 if not found</returns>
        private int FindString(string stringToFind, int startIndex = 0)
        {
            for (int i = startIndex; i < lines.Count; i++)
            {
                if (lines[i].Contains(stringToFind))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Find the Using block of statements.
        /// </summary>
        /// <param name="startIndex">The starting index of using block. -1 if not found</param>
        /// <param name="endIndex">The ending index of using block. -1 if not found</param>
        private void FindUsingBlock(out int startIndex, out int endIndex)
        {
            startIndex = -1;
            endIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                string cleanLine = Clean(lines[i]);
                if (cleanLine.StartsWith("using "))
                {
                    if (startIndex == -1)
                        startIndex = i;
                    endIndex = i;
                }
                else if (cleanLine != string.Empty && startIndex != -1)
                    break;
            }
        }

        /// <summary>
        /// Find the start of the manager class
        /// </summary>
        /// <returns>The line after the classes curly bracket.</returns>
        private int FindStartOfClass()
        {
            int lineNumberClass = FindString("public class Script");
            if (lineNumberClass != -1)
            {
                while (!lines[lineNumberClass].Contains('{'))
                    lineNumberClass++;
                return lineNumberClass + 1; // The line after the curly bracket.
            }
            else
                return -1;
        }

        /// <summary>Trim the line of spaces and remove comments.</summary>
        /// <param name="line">Line to clean</param>
        /// <returns>A new string without leading / trailing spaces and comments</returns>
        private string Clean(string line)
        {
            string cleanLine = line.Trim();
            int posComment = cleanLine.IndexOf("//");
            if (posComment != -1)
                cleanLine = cleanLine.Remove(posComment);

            return cleanLine;
        }

        /// <summary>
        /// Changes the value of a parameter with a given key.
        /// </summary>
        /// <param name="key">Key of the paramter.</param>
        /// <param name="newParam">New value of the parameter.</param>
        public void UpdateParameter(string key, string newParam)
        {
            foreach (var parameter in manager["Parameters"].Children())
                if (parameter["Key"].ToString() == key)
                    parameter["Value"] = newParam;
                    //return;
        }
    }

    /// <summary>A manager declaration</summary>
    public class Declaration
    {
        /// <summary>The index of the line starting the declaration</summary>
        public int LineIndex { get; set; } = -1;

        /// <summary>Was the declaration all on one line?</summary>
        public bool AttributesOnPreviousLines { get; set; } = true;

        /// <summary>The attributes of the declaration</summary>
        public List<string> Attributes { get; set; } = new List<string>();

        /// <summary>The type name of the declaration</summary>
        public string TypeName { get; set; }

        /// <summary>The instance name of the declaration</summary>
        public string InstanceName { get; set; }

        /// <summary>Is declaration private?</summary>
        public bool IsPrivate { get; set; } = true;

        /// <summary>Is declaration an event?</summary>
        public bool IsEvent { get; set; }
    }
        

    /// <summary>Encapsulates a manager method call</summary>
    public class MethodCall
    {
        // e.g. mySolutes.Add("NO3", myUrineDeposition);
        //      InstanceName.MethodName(Arguments);

        /// <summary>The index of the line with the method</summary>
        public int LineIndex { get; set; }

        /// <summary>The instance name that the method is being called on</summary>
        public string InstanceName { get; set; }

        /// <summary>The name of the method</summary>
        public string MethodName { get; set; }

        /// <summary>The method arguments</summary>
        public List<string> Arguments { get; set; }
    }
}


