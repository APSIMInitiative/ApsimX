using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
                // Used to remove any comments from list.
                List<string> commandsWithoutCommentLines = new List<string>();
                foreach (string commandString in configFileCommands)
                {
                    if (!string.IsNullOrEmpty(commandString))
                    {
                        char firstChar = commandString[0];
                        if (firstChar != '#' && firstChar != '\\')
                        {
                            commandsWithoutCommentLines.Add(commandString);
                        }
                    }
                }
                return commandsWithoutCommentLines;
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
                List<string> commandSplits = DecodeSpacesInCommandSplits(command.Split(' ', '=').ToList());

                // If first index item is a string containing "[]" the command is an override...
                Match firstSplitResult = rxOverrideTargetNode.Match(commandSplits[0]);
                if (firstSplitResult.Success)
                {
                    string property = commandSplits[0];
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

                    // TODO: Needs fixing to make sure overrides with encoded spaces (@) are handled correctly.
                    string[] singleLineCommandArray = { property + "=" + value };
                    var overrides = Overrides.ParseStrings(singleLineCommandArray);
                    tempSim = (Simulations)ApplyOverridesToApsimxFile(overrides, tempSim);
                }
                // ... else its an instruction.
                else
                {
                    // Note: 4 items max per instruction command. 1 item minimum (run instruction).
                    Keyword keyword = Keyword.None;
                    string nodeToModify = "";
                    string fileContainingNode = "";
                    string nodeForAction = "";
                    string savePath = "";
                    string loadPath = "";

                    // Determine instruction type.
                    string keywordString = commandSplits[0].ToLower();
                    if (keywordString.Contains("add")) { keyword = Keyword.Add; }
                    else if (keywordString.Contains("delete")) { keyword = Keyword.Delete; }
                    else if (keywordString.Contains("copy")) { keyword = Keyword.Copy; }
                    else if (keywordString.Contains("save")) { keyword = Keyword.Save; }
                    else if (keywordString.Contains("load")) { keyword = Keyword.Load; }
                    else if (keywordString.Contains("run"))
                    {
                        keyword = Keyword.Run;
                        return tempSim;
                    }
                    else if (keywordString.Contains("duplicate")) { keyword = Keyword.Duplicate; }
                    else throw new Exception($"keyword in command didn't match any recognised commands. Keyword given {keywordString}");

                    //if (commandSplits.Length >= 2)
                    if (commandSplits.Count >= 2)
                    {
                        // Determine if its a nodeToModify/SavePath/LoadPath
                        if (keyword == Keyword.Add || keyword == Keyword.Copy || keyword == Keyword.Delete || keyword == Keyword.Duplicate)
                        {
                            Match secondSplitResult = rxNode.Match(commandSplits[1]);
                            Match secondSplitComplexResult = rxNodeComplex.Match(commandSplits[1]);
                            // Check for required format
                            if (secondSplitResult.Success || secondSplitComplexResult.Success)
                            {
                                string modifiedNodeName = commandSplits[1];
                                if (!string.IsNullOrEmpty(modifiedNodeName))
                                {
                                    // Special check to see if the modifiedNodeName = [Simulations]
                                    // Simulations node should never be deleted.
                                    if (modifiedNodeName == "[Simulations]" && keyword == Keyword.Delete)
                                    {
                                        throw new InvalidOperationException($"Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file.");
                                    }
                                    nodeToModify = modifiedNodeName;
                                }
                            }
                            else
                            {
                                throw new Exception($"Format of parent model type does not match required format: [ModelName]. The command given was: {command}");
                            }
                            if (commandSplits.Count > 2)
                            {
                                Match thirdCommandMatchesApsimxFilePath = rxValidPathToNodeInAnotherApsimxFile.Match(commandSplits[2]);
                                Match thirdCommandMatchesNode = rxNode.Match(commandSplits[2]);
                                bool doRegexValuesMatch = false;
                                if (thirdCommandMatchesApsimxFilePath.Value.Equals(thirdCommandMatchesNode))
                                    doRegexValuesMatch = true;
                                // If third command split string is a file path...
                                if (!doRegexValuesMatch && thirdCommandMatchesApsimxFilePath.Success)
                                {
                                    string[] filePathAndNodeName = commandSplits[2].Split(';');
                                    if (filePathAndNodeName.Length == 2)
                                    {
                                        fileContainingNode = filePathAndNodeName[0].Split('\\', '/').Last();
                                        string reformattedNode = filePathAndNodeName[1];
                                        nodeForAction = reformattedNode;
                                    }
                                    else throw new Exception("Add command missing either file or node name.");
                                }
                                // If third command split string is a [NodeName]...
                                else if (!doRegexValuesMatch && thirdCommandMatchesNode.Success)
                                {
                                    nodeForAction = commandSplits[2];
                                }
                                else
                                {
                                    Type[] typeArray = ReflectionUtilities.GetTypeWithoutNameSpace(commandSplits[2], Assembly.GetExecutingAssembly());
                                    if (typeArray.Length == 0)
                                    {
                                        if (commandSplits[0].Equals("duplicate"))
                                        {
                                            // Makes the nodeForAction function as a new name for the cloned node.
                                            nodeForAction = commandSplits[2];
                                        }
                                        else
                                        {
                                            throw new Exception($"Unable to find a model for action by the name of: {commandSplits[2]}");
                                        }
                                    }
                                    else
                                    {
                                        string reformattedNode = "{\"$type\":\"" + typeArray[0].ToString() + ", Models\"}";
                                        nodeForAction = reformattedNode;
                                    }
                                }
                            }
                        }
                        // Ignore the command as these cases are handled outside of this method.
                        else if (keyword == Keyword.Load || keyword == Keyword.Save || keyword == Keyword.Run)
                            return tempSim;
                    }

                    Instruction instruction = new Instruction(keyword, nodeToModify, fileContainingNode, savePath, loadPath, nodeForAction);
                    // Run the instruction.
                    if (string.IsNullOrEmpty(instruction.FileContainingNode))
                    {
                        tempSim = (Simulations)RunInstructionOnApsimxFile(tempSim, instruction);
                    }
                    else
                    {
                        tempSim = (Simulations)RunInstructionOnApsimxFile(tempSim, instruction, instruction.FileContainingNode, configFileDirectory);
                    }

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

                string keyword = instruction.keyword.ToString();
                switch (keyword)
                {
                    case "Add":
                        IModel parentNode = locator.Get(instruction.NodeToModify) as IModel;
                        Structure.Add(instruction.NodeForAction, parentNode);
                        break;
                    case "Delete":
                        IModel nodeToBeDeleted = locator.Get(instruction.NodeToModify) as IModel;
                        Structure.Delete(nodeToBeDeleted);
                        break;
                    case "Duplicate":
                        IModel nodeToBeCopied = locator.Get(instruction.NodeToModify) as IModel;
                        IModel nodeToBeCopiedsParent = nodeToBeCopied.Parent;
                        IModel nodeClone = nodeToBeCopied.Clone();
                        string newNodeName = instruction.NodeForAction.ToString();
                        if (!string.IsNullOrWhiteSpace(newNodeName))
                            nodeClone.Name = newNodeName;
                        Structure.Add(nodeClone, nodeToBeCopiedsParent);
                        break;
                    case "Copy":
                        nodeToBeCopied = locator.Get(instruction.NodeToModify) as IModel;
                        IModel nodeToCopyTo = locator.Get(instruction.NodeForAction) as IModel;
                        IModel parentNodeForCopiedNode = nodeToBeCopied.Parent;
                        nodeClone = nodeToBeCopied.Clone();
                        if (nodeToCopyTo != null)
                            Structure.Add(nodeClone, nodeToCopyTo);
                        else throw new Exception($"Unable to copy {instruction.NodeToModify} to node {instruction.NodeForAction}. Make sure {instruction.NodeForAction} is in the APSIM file.");
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
                if (instruction.keyword == Keyword.Add)
                {
                    // Process for adding an existing node from another file.
                    {
                        string pathOfSimWithNodeAbsoluteDirectory = configFileDirectory + Path.DirectorySeparatorChar + pathOfSimWithNode;
                        Simulations simToCopyFrom = FileFormat.ReadFromFile<Simulations>(pathOfSimWithNodeAbsoluteDirectory, e => throw e, false).NewModel as Simulations;
                        Locator simToCopyFromLocator = new Locator(simToCopyFrom);
                        IModel nodeToCopy = simToCopyFromLocator.Get(instruction.NodeForAction) as IModel;
                        Locator simToCopyToLocator = new Locator(simulations);
                        IModel parentNode = simToCopyToLocator.Get(instruction.NodeToModify) as IModel;
                        Structure.Add(nodeToCopy, parentNode);
                    }
                }
                return simulations;
            }
            catch (Exception e)
            {
                string message = e.Message + " : " + instruction.keyword + " " +  instruction.NodeToModify + " " + instruction.NodeForAction;
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Takes List of strings and removes spaces from broken nodes and reconstructs them.
        /// </summary>
        /// <param name="commandsList"></param>
        /// <returns>A valid string List of commands and overrides.</returns>
        public static List<string> EncodeSpacesInCommandList(List<string> commandsList)
        {
            // TODO: Needs to encode nodes that have multiple spaces as well.
            Regex rxKeywordSubstring = new Regex(@"(add|copy|delete|save|load|run|duplicate)");
            Regex rxBrokenNodeStart = new Regex(@"(\[){1}([\w])+");
            Regex rxBrokenNodeEnd = new Regex(@"([\w])+(\]){1}");
            Regex rxNode = new Regex(@"(\[){1}([\w\s])+(\]){1}");
            Regex rxFactorSpecification = new Regex(@"(.)*(\=)(.)*(\=)(.)*");
            Regex rxNodeWithChild = new Regex(@"(\[{1})([\w\d\s])*(\]){1}(\.){1}([\w\d\s])*");

            List<string> normalizedList = new();
            StringBuilder correctedLineString = new();

            foreach (string lineString in commandsList)
            {
                List<string> lineSections;
                // if the line is an override...
                if (lineString.Contains('='))
                {
                    // Check for factor specification override
                    Match factorSpecification = rxFactorSpecification.Match(lineString);
                    string correctedLine = "";
                    char[] delimiters = new char[] { '=' };
                    lineSections = lineString.Split(delimiters).ToList();
                    foreach (string section in lineSections)
                    {
                        string tempString = null;
                        if (section == lineSections.Last())
                            tempString += section;
                        else
                        {
                            string nonEndSection = section + "=";
                            tempString += nonEndSection;
                        }

                        if (tempString.Contains(' '))
                            tempString = EncodeCommandSubString(tempString);
                        correctedLine += tempString;
                    }
                    normalizedList.Add(correctedLine);
                }
                // if the line is a command...
                else
                {
                    correctedLineString.Clear();
                    string trimmedLineString = lineString.Trim();
                    lineSections = trimmedLineString.Split(' ').ToList();
                    if (lineSections[0] != "load" && lineSections[0] != "save")
                    {
                        foreach (string section in lineSections)
                        {
                            if (rxKeywordSubstring.IsMatch(section))
                            {
                                if (section == "load" || section == "save")
                                    break;
                                else if (section == "run")
                                    correctedLineString.Append(section);
                                else correctedLineString.Append(section + " ");
                            }
                            else if (rxBrokenNodeStart.IsMatch(section) && !rxNode.IsMatch(section))
                                correctedLineString.Append(section + '@');
                            else if (rxBrokenNodeEnd.IsMatch(section) && !rxNode.IsMatch(section))
                                if (section != lineSections.Last())
                                    correctedLineString.Append(section + " ");
                                else correctedLineString.Append(section);
                            else if (rxNode.IsMatch(section))
                            {
                                if (section.Contains('.'))
                                {
                                    if (rxNodeWithChild.IsMatch(section) && section != lineSections.Last())
                                        correctedLineString.Append(section + " ");
                                    else
                                        correctedLineString.Append(section);
                                }
                                else if (section == lineSections.Last())
                                    correctedLineString.Append(section);
                                else correctedLineString.Append(section + " ");
                            }
                            else if (string.IsNullOrEmpty(section))
                                continue;
                            else
                            {
                                if (section != lineSections.Last())
                                    correctedLineString.Append(section + "@");
                                else
                                    correctedLineString.Append(section);
                            }
                        }
                        normalizedList.Add(correctedLineString.ToString());
                    }
                    else
                    {
                        normalizedList.Add(lineString);
                    }
                }
            }
            return normalizedList;
        }

        /// <summary>
        /// Takes <paramref name="commandSplitList"/> (a line from a configFile split into sections separated by space) and removes the 
        /// '@' symbols so Nodes with spaces can be located correctly. 
        /// </summary>
        /// <param name="commandSplitList"></param>
        /// <returns>The list with splits that do not contain '@' characters.</returns>
        public static List<string> DecodeSpacesInCommandSplits(List<string> commandSplitList)
        {
            try
            {
                List<string> decodedCommandList = new();
                foreach (string split in commandSplitList)
                {
                    if (split.Contains('@'))
                    {
                        string modifiedSplit = split.Replace("@", "\u0020");
                        decodedCommandList.Add(modifiedSplit);
                    }
                    else decodedCommandList.Add(split);
                }
                return decodedCommandList;
            }
            catch (Exception e)
            {
                throw new Exception($"An error occurred trying to replace '@' characters from commandSplit. {e}");
            }
        }

        /// <summary>
        /// Takes a list of command strings and removes unnecessary whitespace trailing a command.
        /// </summary>
        /// <param name="commandSplitList"></param>
        /// <returns>A List&lt;string&gt;with trailing whitespace removed.</returns>
        public static List<string> RemoveConfigFileWhitespace(List<string> commandSplitList)
        {
            try
            {
                List<string> modifiedList = new List<string>();
                List<string> tempModifiedList = new List<string>();
                foreach (string commandString in commandSplitList)
                {
                    string fixedString = RemoveInternalOverrideCommandSpaces(commandString);
                    tempModifiedList.Add(fixedString);
                }
                List<string> encodedCommandList = EncodeSpacesInCommandList(tempModifiedList);
                foreach (string command in tempModifiedList)
                {
                    List<string> splitCommands = command.Split(" ").ToList();
                    splitCommands.RemoveAll(split => split == "");
                    string trimmedCommand = "";
                    foreach (string split in splitCommands)
                    {
                        trimmedCommand += split;
                        if (split != splitCommands.Last<string>())
                            trimmedCommand += " ";
                    }
                    modifiedList.Add(trimmedCommand);
                }
                List<string> decodedModifiedList = DecodeSpacesInCommandSplits(modifiedList);
                modifiedList = decodedModifiedList;
                return modifiedList;
            }
            catch (Exception e)
            {
                throw new Exception($"An error occurred removing whitespace from a command list.\n {e}");
            }

        }

        /// <summary>
        /// Takes a command string from a config file and removes internal whitespace.
        /// </summary>
        /// <param name="commandString">A line from config file.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string RemoveInternalOverrideCommandSpaces(string commandString)
        {
            try
            {
                // Removes spaces before and after override '=' symbol.
                if (commandString.Contains("="))
                {
                    // Need temp string for storing command if changed.
                    string tempCommandString;
                    // Get index of '=' and check index of char either side.
                    int equalsIndex = commandString.IndexOf('=');
                    if (commandString[equalsIndex - 1] == ' ')
                    {
                        tempCommandString = commandString.Remove(equalsIndex - 1, 1);
                        commandString = tempCommandString;
                    }
                    // Reset equalsIndex.
                    equalsIndex = commandString.IndexOf("=");
                    if (commandString[equalsIndex + 1] == ' ')
                    {
                        tempCommandString = commandString.Remove(equalsIndex + 1, 1);
                        commandString = tempCommandString;
                    }
                }
                return commandString;

            }
            catch (Exception e)
            {
                throw new Exception($"An occurred removing extra internal whitespace in a command. The command was: {commandString}.\n{e}");
            }

        }

        /// <summary>
        /// Takes a command list and returns new List with no null values.
        /// </summary>
        /// <param name="commandStrings">List of strings from a config file.</param>
        /// <returns>A List of command strings without null values.</returns>
        /// <exception cref="Exception"></exception>
        public static List<string> GetListWithoutNullCommands(List<string> commandStrings)
        {
            try
            {
                List<string> results = new();
                foreach (string line in commandStrings)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        results.Add(line);
                    }
                }
                return results;
            }
            catch (Exception e)
            {
                throw new Exception($"An error occurred creating new list without null values.\n {e}");
            }
        }

        /// <summary>
        /// Returns a section of command with '@' encoded.
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static string EncodeCommandSubString(string section)
        {
            string trimmedSection = section.TrimEnd();
            return trimmedSection.Replace(' ', '@');

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
