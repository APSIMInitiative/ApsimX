using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using APSIM.Shared.Utilities;
using Models.Core.ApsimFile;
using static Models.Core.Overrides;

namespace Models.Core.ConfigFile
{
    /// <summary>
    /// Functions used for handling Config files used in models.exe command line commands.
    /// </summary>
    public static class ConfigFile
    {
        /// <summary>
        /// Returns all commands in a config file.
        /// </summary>
        /// <param name="configFilePath">String path to config file.</param>
        /// <returns>A combined string List of override and instruction commands.</returns>
        public static List<string> GetConfigFileCommands(string configFilePath)
        {
            try
            {
                // List for all the commands, whether they are override commands or instruction commands.
                // Instructions = used for adding, creating, deleting, copying, saving, loading
                // Overrides = used for modifying existing .apsimx node values.
                List<string> configFileCommands = File.ReadAllLines(configFilePath).ToList();

                // Trim all commands and remove empty lines.
                configFileCommands = configFileCommands.Select(x => x.Trim()).ToList();
                configFileCommands.RemoveAll(string.IsNullOrEmpty);

                List<string> cleanedCommands = new List<string>();
                foreach (string commandString in configFileCommands)
                {
                    char firstChar = commandString.First();
                    if (firstChar != '#' && firstChar != '\\')
                        cleanedCommands.Add(AddQuotesAroundStringsWithSpaces(commandString));
                }
                return cleanedCommands;
            }
            catch (Exception e)
            {
                throw new Exception($"An error occurred fetching commands from config file located at {configFilePath}. Check file contents.\n {e}");
            }
        }

        /// <summary>
        /// Runs the commands depending on command type (override/instruction).
        /// </summary>
        /// <param name="command">An override or instruction command.</param>
        /// <param name="configFileDirectory">A path to the config file's directory</param>
        /// <param name="tempSim">A file path to an .apsimx file.</param>
        public static IModel RunConfigCommands(Simulations tempSim, string command, string configFileDirectory)
        {
            try
            {
                Regex rxOverrideTargetNode = new Regex(@"(\[){1}([\w\s])+(\]){1}([\.\w])*");
                Regex rxNode = new Regex(@"(\[){1}([\w\s])+(\]){1}");
                Regex rxNodeComplex = new Regex(@"(\[){1}([\w\s])+(\]){1}([\.\w])*");
                Regex rxValidPathToNodeInAnotherApsimxFile = new Regex(@"([a-zA-Z0-9]){1}([\:\/\\\-\w])*(\.){1}(apsimx)(;){1}(\[){1}([\w\s])+(\]){1}");
                Regex rxBrokenNode = new Regex(@"");

                // Gets command, splits it using space and = characters, then replaces any @ symbols with spaces so
                // nodes in the commands can be used normally.
                List<string> commandSplits = StringUtilities.SplitStringHonouringQuotes(command, " =");

                // Get the first part to see what kind of command it is
                string part1 = commandSplits[0].Trim();

                // If first index item is a string starting with ".", or containing "[]", the command is an override
                if (part1.StartsWith('.') || (part1.StartsWith('[') && part1.Contains(']')))
                {
                    string property = part1;
                    string value = "";
                    for(int i = 1; i < commandSplits.Count; i++)
                    {
                        value += commandSplits[i];
                        if (i < commandSplits.Count-1)
                            value += "=";
                    }

                    //check if second part is a filename or value (ends in ; and file exists)
                    //if so, read contents of that file in as the value
                    string potentialFilepath = configFileDirectory + "/" + value.Substring(0, value.Length-1);
                    if (value.Trim().EndsWith(';') && File.Exists(potentialFilepath)) 
                        value = File.ReadAllText(potentialFilepath);

                    string[] singleLineCommandArray = { property + "=" + value };
                    var overrides = Overrides.ParseStrings(singleLineCommandArray);
                    tempSim = (Simulations)ApplyOverridesToApsimxFile(overrides, tempSim);
                }
                // else its an instruction.
                else
                {
                    // Note: 4 items max per instruction command. 1 item minimum (run instruction).
                    Keyword keyword = Keyword.None;
                    string path = "";
                    string activeNode = "";
                    string newNode = "";
                    List<string> parameters = new List<string>();

                    // Determine instruction type.
                    string keywordString = commandSplits[0].ToLower();
                    if (keywordString.Contains("add")) { keyword = Keyword.Add; }
                    else if (keywordString.Contains("copy")) { keyword = Keyword.Copy; }
                    else if (keywordString.Contains("delete")) { keyword = Keyword.Delete; }
                    else if (keywordString.Contains("duplicate")) { keyword = Keyword.Duplicate; }
                    else if (keywordString.Contains("save")) { keyword = Keyword.Save; }
                    else if (keywordString.Contains("load")) { keyword = Keyword.Load; }
                    else if (keywordString.Contains("run"))  { return tempSim; }
                    else throw new Exception($"keyword in command didn't match any recognised commands. Keyword given {keywordString}");

                    // Ignore the command as these cases are handled outside of this method.
                    if (keyword == Keyword.Load || keyword == Keyword.Save || keyword == Keyword.Run)
                        return tempSim;

                    if (commandSplits.Count >= 2)
                    {
                        string part2 = commandSplits[1].Trim();

                        // Check for required format
                        bool isNode = part2.StartsWith('[') && part2.Contains(']');
                        if (!isNode)
                            throw new Exception($"Format of parent model type does not match required format: [ModelName]. The command given was: {command}");

                        // Special check to see if the modifiedNodeName = [Simulations] as Simulations node should never be deleted.
                        if (keyword == Keyword.Delete && part2 == "[Simulations]")
                            throw new InvalidOperationException($"Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file.");

                        // part is good, assign to variable
                        if (!string.IsNullOrEmpty(part2))
                            activeNode = part2;
                    }

                    if (commandSplits.Count >= 3)
                    {
                        string part3 = commandSplits[2].Trim();
                        
                        bool isNode = part3.StartsWith('[') && part3.Contains(']');
                        bool isPathWithNode = rxValidPathToNodeInAnotherApsimxFile.Match(part3).Success;

                        if (!isNode && !isPathWithNode)
                        {
                            if (keyword == Keyword.Duplicate) 
                                parameters.Add(part3);
                            else
                            {
                                Type[] typeArray = ReflectionUtilities.GetTypeWithoutNameSpace(part3, Assembly.GetExecutingAssembly());
                                if (typeArray.Length > 0)
                                    newNode = "{\"$type\":\"" + typeArray[0].ToString() + ", Models\"}";
                                else
                                    throw new Exception($"Unable to find a model for action by the name of: {part3}");
                            }
                        }
                        // If third command split string is a file path with node name
                        else if (isPathWithNode)
                        {
                            string[] filePathAndNodeName = part3.Split(';');
                            if (filePathAndNodeName.Length == 2)
                            {
                                path = filePathAndNodeName[0].Split('\\', '/').Last();
                                newNode = filePathAndNodeName[1];
                            }
                            else throw new Exception($"Path with Node {part3} missing ; symbol");
                        }
                        // If third command split string is a [NodeName]
                        else if (isNode)
                        {
                            newNode = part3;
                        }
                    }

                    if (commandSplits.Count > 3)
                    {
                        for (int i = 3; i < commandSplits.Count; i++)
                            parameters.Add(commandSplits[i].Trim());
                    }

                    Instruction instruction = new Instruction(keyword, activeNode, newNode, path, parameters);
                    // Run the instruction.
                    if (string.IsNullOrEmpty(instruction.Path))
                        tempSim = (Simulations)RunInstructionOnApsimxFile(tempSim, instruction);
                    else
                        tempSim = (Simulations)RunInstructionOnApsimxFile(tempSim, instruction, instruction.Path, configFileDirectory);

                }
                return tempSim;
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while running config file commands.", e);
            }

        }
        /// <summary>
        /// Runs overrides on the file before returning the modified file.
        /// </summary>
        /// <param name="overrides"> A single Override in a list.</param>
        /// <param name="simulations">A Simulations type to make changes to.</param>
        /// <returns></returns>
        private static IModel ApplyOverridesToApsimxFile(IEnumerable<Override> overrides, IModel simulations)
        {
            Overrides.Apply(simulations, overrides);
            return simulations;
        }

        /// <summary>
        /// Runs config file instruction on .apsimx file then returns the modified file.
        /// </summary>
        private static IModel RunInstructionOnApsimxFile(IModel simulations, Instruction instruction)
        {
            try
            {
                (simulations as Simulations).ResetSimulationFileNames();
                Locator locator = new Locator(simulations);

                string keyword = instruction.Keyword.ToString();
                switch (keyword)
                {
                    case "Add":
                        IModel parentNode = locator.Get(instruction.ActiveNode) as IModel;
                        IModel newNode = Structure.Add(instruction.NewNode, parentNode);
                        if (instruction.Parameters.Count == 1)
                            newNode.Name = instruction.Parameters[0];
                        break;
                    case "Delete":
                        IModel nodeToBeDeleted = locator.Get(instruction.ActiveNode) as IModel;
                        Structure.Delete(nodeToBeDeleted);
                        break;
                    case "Duplicate":
                        IModel nodeToBeCopied = locator.Get(instruction.ActiveNode) as IModel;
                        IModel nodeToBeCopiedsParent = nodeToBeCopied.Parent;
                        IModel nodeClone = nodeToBeCopied.Clone();
                        if (instruction.Parameters.Count == 1)
                            nodeClone.Name = instruction.Parameters[0];
                        Structure.Add(nodeClone, nodeToBeCopiedsParent);
                        break;
                    case "Copy":
                        nodeToBeCopied = locator.Get(instruction.ActiveNode) as IModel;
                        IModel nodeToCopyTo = locator.Get(instruction.NewNode) as IModel;
                        nodeClone = nodeToBeCopied.Clone();
                        if (instruction.Parameters.Count == 1)
                            nodeClone.Name = instruction.Parameters[0];
                        Structure.Add(nodeClone, nodeToCopyTo);
                        break;
                }
                return simulations;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Runs config file instruction on .apsimx file then returns the modified file.
        /// </summary>
        private static IModel RunInstructionOnApsimxFile(IModel simulations, Instruction instruction, string pathOfSimWithNode, string configFileDirectory)
        {
            try
            {

                //Check for add keyword in instruction.
                if (instruction.Keyword == Keyword.Add)
                {
                    // Process for adding an existing node from another file.
                    {
                        string pathOfSimWithNodeAbsoluteDirectory = configFileDirectory + Path.DirectorySeparatorChar + pathOfSimWithNode;
                        Simulations simToCopyFrom = FileFormat.ReadFromFile<Simulations>(pathOfSimWithNodeAbsoluteDirectory, e => throw e, false).NewModel as Simulations;
                        Locator simToCopyFromLocator = new Locator(simToCopyFrom);
                        IModel nodeToCopy = simToCopyFromLocator.Get(instruction.NewNode) as IModel;
                        Locator simToCopyToLocator = new Locator(simulations);
                        IModel parentNode = simToCopyToLocator.Get(instruction.ActiveNode) as IModel;
                        Structure.Add(nodeToCopy, parentNode);
                    }
                }
                return simulations;
            }
            catch (Exception e)
            {
                string message = e.Message + " : " + instruction.Keyword + " " +  instruction.ActiveNode + " " + instruction.NewNode;
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Takes string and adds quotes if it has spaces in its input
        /// </summary>
        /// <param name="line"></param>
        /// <returns>The list with quotes around string inputs that have spaces</returns>
        public static string AddQuotesAroundStringsWithSpaces(string line)
        {
            int pos = line.IndexOf('=');
            if (pos > -1)
            {
                string p1 = line.Substring(0, pos);
                string p2 = line.Substring(pos+1);

                if (p2.Contains(' '))
                    p2 = '"' + p2.Trim() + '"';

                return p1 + "=" + p2;
            }
            else
                return line;
        }

        /// <summary>
        /// Replaces placeholders in a list of commands.
        /// </summary>
        /// <param name="commandString">a command string</param>
        /// <param name="dataRow">A data row from a batch csv file.</param>
        /// <param name="dataRowIndex"></param>
        /// <returns></returns>
        public static string ReplaceBatchFilePlaceholders(string commandString, DataRow dataRow, int dataRowIndex)
        {
            if (!commandString.Contains('$'))
                return commandString;
            return BatchFile.GetCommandReplacements(commandString, dataRow, dataRowIndex);
        }
    }
}
