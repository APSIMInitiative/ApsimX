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

    /// <summary>
    /// Provides helper methods to read and manipulate manager scripts.
    /// </summary>
    public class ManagerConverter
    {
        private List<string> lines = new List<string>();

        /// <summary>Load script</summary>
        /// <param name="script">The manager script to work on</param>
        public void Read(string script)
        {
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

        /// <summary>Write script</summary>
        public new string ToString()
        {
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
        /// Find a declaration
        /// </summary>
        /// <param name="instanceName"></param>
        /// <returns>The declaration or null if not found</returns>
        public Declaration FindDeclaration(string instanceName)
        {
            string pattern = @"(?<Link>\[.+\])?\s+(?<TypeName>\w+)\s+(?<InstanceName>\w+);";
            for (int i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], pattern);
                if (match.Success && match.Groups["InstanceName"].Value == instanceName)
                {
                    Declaration decl = new Declaration();
                    decl.LineIndex = i;
                    decl.TypeName = match.Groups["TypeName"].Value;
                    decl.InstanceName = match.Groups["InstanceName"].Value;
                    decl.Attributes = new List<string>();
                    if (match.Groups["Link"].Success)
                        decl.Attributes.Add(match.Groups["Link"].Value);

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

                    return decl;
                }
            }
            return null;
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
                    Declaration decl = FindDeclaration(instanceName);
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
        /// Find a line with the matching string
        /// </summary>
        /// <param name="stringToFind"></param>
        /// <param name="startIndex">Index to start search from</param>
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

    }

    /// <summary>A manager declaration</summary>
    public class Declaration
    {
        /// <summary>The index of the line starting the declaration</summary>
        public int LineIndex { get; set; }

        /// <summary>The attributes of the declaration</summary>
        public List<string> Attributes { get; set; }

        /// <summary>The type name of the declaration</summary>
        public string TypeName { get; set; }

        /// <summary>The instance name of the declaration</summary>
        public string InstanceName { get; set; }
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


