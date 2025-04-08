using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using APSIM.Shared.Utilities;
using Newtonsoft.Json.Linq;

namespace Models.Core.ApsimFile
{


    /// <summary>
    /// Provides helper methods to read and manipulate manager scripts.
    /// </summary>
    public class ManagerConverter
    {
        private List<string> lines = new List<string>();

        /// <summary>
        /// The Json token.
        /// </summary>
        public JObject Token { get; private set; }

        /// <summary>Name of manager model - useful for debugging.</summary>
        public string Name => Token["Name"].ToString();

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
                if (Token["Parameters"] == null)
                    return parameters;

                foreach (var parameter in Token["Parameters"])
                    parameters.Add(parameter["Key"].ToString(), parameter["Value"].ToString());
                return parameters;
            }
        }

        /// <summary>
        /// Change the value of a manager parameter
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="value">Parameter value</param>
        public void ChangeParameterValue(string key, string value)
        {
            foreach (var parameter in Token["Parameters"])
            {
                if (parameter["Key"].ToString() == key)
                    parameter["Value"] = value;
            }
        }

        /// <summary>Returns true if manager is empty.</summary>
        public bool IsEmpty => lines.Count == 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="manager">The JSON manager object.</param>
        public ManagerConverter(JObject manager)
        {
            this.Token = manager;
            if (manager["Code"] != null)
                Read(manager["Code"].ToString());
            else if (manager["CodeArray"] != null)
            {
                var codeArray = manager["CodeArray"] as JArray;
                lines = codeArray.Values<string>().ToList();
            }
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
        /// Changed how Manager Code is stored in version 163, stored as array of lines instead of single string
        /// </summary>
        public void Save()
        {
            if (Token["CodeArray"] != null)
            {
                Token["CodeArray"] = new JArray(lines);
                if (Token["Code"] != null)
                    Token.Remove("Code");
            }
            else
            {
                Token["Code"] = ToString();
            }
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

        /// <summary>
        /// Add a using statement if it doesn't already exist.
        /// </summary>
        /// <param name="statement"></param>
        public void AddUsingStatement(string statement)
        {
            List<string> usings = GetUsingStatements().ToList();
            usings.Add(statement);
            SetUsingStatements(usings.Distinct());
        }

        /// <summary>Gets a collection of declarations.</summary>
        public List<Declaration> GetDeclarations()
        {
            List<Declaration> foundDeclarations = new List<Declaration>();

            string pattern = @"(?<Link>\[.+\]\s+)?(?<Access>public\s+|private\s+)?(?<TypeName>[\w\.]+)\s+(?<InstanceName>\w+)\s*(=\s*null)?;";
            for (int i = 0; i < lines.Count; i++)
            {
                var line = Clean(lines[i]);
                Match match = Regex.Match(line, pattern);
                if (match.Groups["TypeName"].Value != string.Empty &&
                    match.Groups["TypeName"].Value != "as" &&
                    match.Groups["TypeName"].Value != "return" &&
                    match.Groups["InstanceName"].Value != string.Empty &&
                    match.Groups["InstanceName"].Value != "get" &&
                    match.Groups["InstanceName"].Value != "set" &&
                    match.Groups["TypeName"].Value != "using")
                {
                    Declaration decl = new Declaration();
                    decl.LineIndex = i;
                    decl.TypeName = match.Groups["TypeName"].Value;
                    decl.InstanceName = match.Groups["InstanceName"].Value;
                    decl.Attributes = new List<string>();
                    decl.IsEvent = line.Contains("event");
                    decl.IsPrivate = !decl.IsEvent && match.Groups["Access"].Value.TrimEnd() != "public";
                    if (match.Groups["Link"].Success)
                    {
                        decl.Attributes.Add(match.Groups["Link"].Value.Trim());
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
                for (int i = existingDeclarations.Count - 1; i >= 0; i--)
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

                lineIndex = FindString("." + methodName, lineIndex + 1);
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
                    pos = lines[i].IndexOf(searchPattern, pos + 1);
                }
            }
            return replacementDone;
        }

        /// <summary>
        /// Search for a string and return the line index it is on or -1 if not found.
        /// </summary>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="caseSensitive">Case sensitive?</param>
        /// <returns></returns>
        public int LineIndexOf(string searchPattern, bool caseSensitive = false)
        {
            if (searchPattern != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    StringComparison comparison = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    if (lines[i].IndexOf(searchPattern, comparison) != -1)
                        return i;
                }
            }
            return -1;
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
        /// Perform a search and replace in manager script.
        /// </summary>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacer">Delegate that returns a custom replacment string depending on the match..</param>
        /// <param name="options">Regular expression options to use. Default value is none.</param>
        public bool ReplaceRegex(string searchPattern, MatchEvaluator replacer, RegexOptions options = RegexOptions.None)
        {
            bool replacementDone = false;
            string oldCode = ToString();
            if (oldCode == null || searchPattern == null)
                return false;
            var newCode = Regex.Replace(oldCode, searchPattern, replacer, options);
            if (newCode != oldCode)
            {
                Read(newCode);
                replacementDone = true;
            }
            return replacementDone;
        }

        /// <summary>
        /// Find a string using a regular expression.
        /// </summary>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <returns>The match.</returns>
        public MatchCollection FindRegexMatches(string searchPattern)
        {
            string oldCode = ToString();
            if (oldCode == null || searchPattern == null)
                return null;
            return Regex.Matches(oldCode, searchPattern);
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
        public int FindString(string stringToFind, int startIndex = 0)
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
            int lineNumberClass = FindString("public class ");
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
            foreach (var parameter in Token["Parameters"].Children())
                if (parameter["Key"].ToString() == key)
                    parameter["Value"] = newParam;
            //return;
        }

        /// <summary>
        /// Change manager to reflect moving of variables from one object to another e.g. from Soil to IPhysical.
        /// </summary>
        /// <param name="variablesToMove">The names of variables to move.</param>
        /// <returns>True if changes were made.</returns>
        public bool MoveVariables(ManagerReplacement[] variablesToMove)
        {
            var declarations = GetDeclarations();

            bool replacementMade = false;
            foreach (var variableToMove in variablesToMove)
            {
                var tokens = variableToMove.OldName.Split('.');
                if (tokens.Length != 2)
                    throw new Exception($"Invalid old variale name found {variableToMove.OldName}");
                var oldTypeName = tokens[0];
                var oldInstanceName = tokens[1];

                var pattern = $@"(\w+)\.{oldInstanceName}(\W+)";
                ReplaceRegex(pattern, match =>
                {
                    // Check the type of the variable to see if it is soil.
                    var soilInstanceName = match.Groups[1].Value;
                    var matchDeclaration = declarations.Find(decl => decl.InstanceName == soilInstanceName);
                    if (matchDeclaration == null || (matchDeclaration.TypeName != oldTypeName && !matchDeclaration.TypeName.EndsWith($".{oldTypeName}")))
                        return match.Groups[0].Value; // Don't change anything as the type isn't a match.

                    replacementMade = true;

                    tokens = variableToMove.NewName.Split('.');
                    string newInstanceName = null;
                    string newVariableName = null;
                    if (tokens.Length >= 1)
                        newInstanceName = tokens[0];
                    if (tokens.Length == 2)
                        newVariableName = tokens[1];

                    // Found a variable that needs renaming.
                    // See if there is an instance varialbe of the correct type.If not add one.
                    Declaration declaration = declarations.Find(decl => decl.TypeName == variableToMove.NewInstanceTypeName);
                    if (declaration == null)
                    {
                        declaration = new Declaration()
                        {
                            TypeName = variableToMove.NewInstanceTypeName,
                            InstanceName = newInstanceName,
                            IsPrivate = true
                        };
                        declarations.Add(declaration);
                    }

                    if (!declaration.Attributes.Contains("[Link]"))
                        declaration.Attributes.Add("[Link]");

                    if (newVariableName == null)
                        return $"{declaration.InstanceName}{match.Groups[2].Value}";
                    else
                        return $"{declaration.InstanceName}.{newVariableName}{match.Groups[2].Value}";
                });
            }
            if (replacementMade)
                SetDeclarations(declarations);
            return replacementMade;
        }

        /// <summary>
        /// Return true if the specified position is commented out in the code.
        /// </summary>
        /// <param name="pos"></param>
        public bool PositionIsCommented(int pos)
        {
            string code = ToString();
            // Search backwards for either '/*'
            int posOpenComment = code.LastIndexOf("/*", pos);
            int posCloseComment = code.LastIndexOf("*/", pos);
            return posOpenComment > posCloseComment;
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

    /// <summary>
    /// Encapsulates a management replacement.
    /// </summary>
    public class ManagerReplacement
    {
        /// <summary>The old variable name.</summary>
        public string OldName { get; set; }

        /// <summary>The new variable name.</summary>
        public string NewName { get; set; }

        /// <summary>The type of the new instance variable..</summary>
        public string NewInstanceTypeName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="searchFor"></param>
        /// <param name="replaceWith"></param>
        /// <param name="typeName"></param>
        public ManagerReplacement(string searchFor, string replaceWith, string typeName)
        {
            OldName = searchFor;
            NewName = replaceWith;
            NewInstanceTypeName = typeName;
        }
    }
}


