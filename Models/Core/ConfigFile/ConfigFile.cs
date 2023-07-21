using System;
using System.Collections.Generic;
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
            // List for all the commands, whether they are override commands or instruction commands.
            // Instructions = used for adding, creating, deleting, copying, saving, loading
            // Overrides = used for modifying existing .apsimx node values.
            List<string> configFileCommands = File.ReadAllLines(configFilePath).ToList();
            // Used to remove any comments from list.
            List<string> commandsWithoutCommentLines = new List<string>();
            foreach (string commandString in configFileCommands)
            {
                char firstChar = commandString[0];
                if (firstChar != '#' && firstChar != '\\')
                {
                    commandsWithoutCommentLines.Add(commandString);
                }
            }
            return commandsWithoutCommentLines;
        }

        /// <summary>
        /// Runs the commands depending on command type (override/instruction).
        /// </summary>
        /// <param name="configFileCommands">A list of override or instruction commands.</param>
        /// <param name="apsimxFilePath">A file path to an .apsimx file.</param>
        public static IModel RunConfigCommands(string apsimxFilePath, IEnumerable<string> configFileCommands)
        {
            try
            {
                // TODO: Important regex for nodes needs to be updated to include dashes.
                Regex rxAddLocalCommand = new Regex(@"(add)\s(\[){1}(\w)*(\]){1}\s(\w)*");
                Regex rxAddFromOtherFileCommand = new Regex(@"(add)\s(\[){1}(\w)+([\w\s])*(\]){1}\s([a-zA-Z0-9]){1}([\:\-\w\\\/])*(\.){1}(apsimx){1}(;){1}(\[){1}([\w\s])*(\]){1}");
                Regex rxCopyCommand = new Regex(@"(copy)\s(\[){1}(\w)*(\]){1}\s(\[){1}(\w)*(\]){1}");
                Regex rxDeleteCommand = new Regex(@"(delete)\s(\[){1}([\w\s])*(\]){1}(\.){1}([\w\.])*");
                Regex rxSaveCommand = new Regex(@"(save)\s([\w\:\\])*(\.){1}(apsimx)");
                Regex rxLoadCommand = new Regex(@"(load)\s([\w\:\\])*(\.){1}(apsimx)");
                Regex rxRunCommand = new Regex(@"(run)");
                Regex rxOverride = new Regex(@"(\[){1}([\w\s])+(\]){1}([\.\w])*([\=])+([\w])+");
                Regex rxOverrideTargetNode = new Regex(@"(\[){1}([\w\s])+(\]){1}([\.\w])*");
                Regex rxNode = new Regex(@"(\[){1}([\w\s])+(\]){1}");
                Regex rxNodeComplex = new Regex(@"(\[){1}([\w\s])+(\]){1}([\.\w])*");
                Regex rxValidApsimxFilePath = new Regex(@"([a-zA-Z0-9]){1}([\:\/\\\w])*(\.){1}(apsimx)");
                Regex rxValidPathToNodeInAnotherApsimxFile = new Regex(@"([a-zA-Z0-9]){1}([\:\/\\\-\w])*(\.){1}(apsimx)(;){1}(\[){1}([\w\s])+(\]){1}");
                Regex rxBrokenNode = new Regex(@""); // TODO: Create new regex to recognise broken nodes
                Simulations sim = FileFormat.ReadFromFile<Simulations>(apsimxFilePath, e => throw e, false).NewModel as Simulations;

                List<string> newConfigFileCommands = EncodeSpacesInCommandList(configFileCommands.ToList());

                foreach (string command in newConfigFileCommands)
                {
                    // Gets command, splits it using space and = characters, then replaces any @ symbols with spaces so
                    // nodes in the commands can be used normally.
                    List<string> commandSplits = DecodeSpacesInCommandSplits(command.Split(' ', '=').ToList());

                    // If first index item is a string containing "[]" the command is an override...
                    Match firstSplitResult = rxOverrideTargetNode.Match(commandSplits[0]);
                    //if (commandSplits[0].Contains('['))
                    if (firstSplitResult.Success)
                    {
                        string[] singleLineCommandArray = { command };
                        var overrides = Overrides.ParseStrings(singleLineCommandArray);
                        sim = (Simulations)ApplyOverridesToApsimxFile(overrides, sim);
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
                        else if (keywordString.Contains("run")) { keyword = Keyword.Run; }
                        else throw new Exception($"keyword in command didn't match any recognised commands. Keyword given {keywordString}");

                        //if (commandSplits.Length >= 2)
                        if (commandSplits.Count >= 2)
                        {
                            // Determine if its a nodeToModify/SavePath/LoadPath
                            if (keyword == Keyword.Add || keyword == Keyword.Copy || keyword == Keyword.Delete)
                            {
                                Match secondSplitResult = rxNode.Match(commandSplits[1]);
                                Match secondSplitComplexResult = rxNodeComplex.Match(commandSplits[1]);
                                // Check for required format
                                //if (commandSplits[1].Contains('[') && commandSplits[1].Contains(']'))
                                if (secondSplitResult.Success || secondSplitComplexResult.Success)
                                {
                                    string modifiedNodeName = commandSplits[1];
                                    if (!string.IsNullOrEmpty(modifiedNodeName))
                                    {
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
                                    //if (commandSplits[2].Contains('\\') || commandSplits[2].Contains('/'))
                                    if (!doRegexValuesMatch && thirdCommandMatchesApsimxFilePath.Success)
                                    {
                                        string[] filePathAndNodeName = commandSplits[2].Split(';');
                                        if (filePathAndNodeName.Length == 2)
                                        {
                                            fileContainingNode = filePathAndNodeName[0];
                                            string reformattedNode = filePathAndNodeName[1];
                                            nodeForAction = reformattedNode;
                                        }
                                        else throw new Exception("Add command missing either file or node name.");
                                    }
                                    // If third command split string is a [NodeName]...
                                    //else if (commandSplits[2].Contains('[') && commandSplits[2].Contains(']'))
                                    else if (!doRegexValuesMatch && thirdCommandMatchesNode.Success)
                                    {
                                        nodeForAction = commandSplits[2];
                                    }
                                    else
                                    {
                                        //string reformattedNode = "{\"$type\": \"Models." + commandSplits[2] + ", Models\"}";
                                        Type[] typeArray = ReflectionUtilities.GetTypeWithoutNameSpace(commandSplits[2], Assembly.GetExecutingAssembly());
                                        string reformattedNode = "{\"$type\":\"" + typeArray[0].ToString() + ", Models\"}";
                                        nodeForAction = reformattedNode;
                                    }
                                }
                            }
                            // Ignore the command as these cases are handled outside of this method.
                            else if (keyword == Keyword.Load || keyword == Keyword.Save)
                                return sim;
                        }

                        Instruction instruction = new Instruction(keyword, nodeToModify, fileContainingNode, savePath, loadPath, nodeForAction);
                        // Run the instruction.
                        if (string.IsNullOrEmpty(instruction.FileContainingNode))
                        {
                            sim = (Simulations)RunInstructionOnApsimxFile(sim, instruction);
                        }
                        else
                        {
                            sim = (Simulations)RunInstructionOnApsimxFile(sim, instruction, instruction.FileContainingNode);
                        }

                    }
                }
                return sim;
            }
            catch (Exception e)
            {
                throw new Exception("Error occured while running config file commands.", e);
            }

        }
        /// <summary>
        /// Runs overrides on the file before returning the modified file.
        /// </summary>
        /// <param name="overrides"> A single Override in a list.</param>
        /// <param name="simulation">A Simulations type to make changes to.</param>
        /// <returns></returns>
        private static IModel ApplyOverridesToApsimxFile(IEnumerable<Override> overrides, IModel simulation)
        {
            Overrides.Apply(simulation, overrides);
            return simulation;
        }

        /// <summary>
        /// Runs config file instruction on .apsimx file then returns the modified file.
        /// </summary>
        private static IModel RunInstructionOnApsimxFile(IModel simulation, Instruction instruction)
        {
            try
            {
                Locator locator = new Locator(simulation);
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
                    case "Copy":
                        IModel nodeToBeCopied = locator.Get(instruction.NodeToModify) as IModel;
                        IModel nodeToBeCopiedsParent = nodeToBeCopied.Parent;
                        IModel nodeClone = nodeToBeCopied.Clone();
                        string newNodeName = instruction.NodeForAction.ToString().Substring(1).Trim(']');
                        nodeClone.Name = newNodeName;
                        Structure.Add(nodeClone, nodeToBeCopiedsParent);
                        break;
                }
                return simulation;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Runs config file instruction on .apsimx file then returns the modified file.
        /// </summary>
        private static IModel RunInstructionOnApsimxFile(IModel simulation, Instruction instruction, string pathOfSimWithNode)
        {
            try
            {

                //Check for add keyword in instruction.
                if (instruction.keyword == Keyword.Add)
                {
                    // Process for adding an existing node from another file.
                    {
                        Simulations simToCopyFrom = FileFormat.ReadFromFile<Simulations>(pathOfSimWithNode, e => throw e, false).NewModel as Simulations;
                        Locator simToCopyFromLocator = new Locator(simToCopyFrom);
                        IModel nodeToCopy = simToCopyFromLocator.Get(instruction.NodeForAction) as IModel;
                        Locator simToCopyToLocator = new Locator(simulation);
                        IModel parentNode = simToCopyToLocator.Get(instruction.NodeToModify) as IModel;
                        Structure.Add(nodeToCopy, parentNode);
                    }
                }
                return simulation;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Takes List of strings and removes spaces from broken nodes and reconstructs them.
        /// </summary>
        /// <param name="commandsList"></param>
        /// <returns>A valid string List of commands and overrides.</returns>
        private static List<string> EncodeSpacesInCommandList(List<string> commandsList)
        {
            Regex rxKeywordSubstring = new Regex(@"(add|copy|delete|save|load|run)");
            Regex rxBrokenNodeStart = new Regex(@"(\[){1}([\w])+");
            Regex rxBrokenNodeEnd = new Regex(@"([\w])+(\]){1}");
            Regex rxNode = new Regex(@"(\[){1}([\w\s])+(\]){1}");

            List<string> normalizedList = new();
            //string firstBrokenNodeString = "";
            //string secondBrokenNodeString = "";
            StringBuilder correctedLineString = new();
            //string commandKeyWordSubString = "";
            //string firstValidNodeString = "";
            //string secondValidNodeString = "";

            foreach (string lineString in commandsList)
            {
                List<string> lineSections;
                // if the line is an override...
                if (lineString.Contains('='))
                {
                    string correctedLine = "";
                    lineSections = lineString.Split('=').ToList();
                    foreach (string section in lineSections)
                    {
                        string fixedSection;
                        if (section.Contains(' '))
                        {
                            int indexOfSpace = section.IndexOf(' ');
                            string tempSection = section.Insert(indexOfSpace - 1, "@");
                            int newIndexOfSpace = tempSection.IndexOf(" ");
                            fixedSection = tempSection.Remove(indexOfSpace, 1);
                            correctedLine += fixedSection;
                        }
                    }
                    normalizedList.Add(correctedLine);
                }
                // if the line is a command...
                else
                {
                    lineSections = lineString.Split(' ').ToList();
                    if (lineSections[0] != "load" && lineSections[0] != "save")
                    {
                        foreach (string section in lineSections)
                        {
                            if (rxKeywordSubstring.IsMatch(section))
                            {
                                if (section == "load" || section == "save")
                                    break;
                                //else commandKeyWordSubString = section;
                                else correctedLineString.Append(section + " ");
                            }
                            else if (rxBrokenNodeStart.IsMatch(section) && !rxNode.IsMatch(section))
                            {
                                //// Make sure that if a broken node start has been encountered that it starts a new brokenNodeString.
                                //if (string.IsNullOrEmpty(firstBrokenNodeString))
                                //    firstBrokenNodeString = section;
                                //else secondBrokenNodeString = section;
                                correctedLineString.Append(section + '@');
                            }
                            else if (rxBrokenNodeEnd.IsMatch(section) && !rxNode.IsMatch(section))
                            {
                                //if (!string.IsNullOrEmpty(firstBrokenNodeString) && string.IsNullOrEmpty(secondBrokenNodeString))
                                //    string.Concat(firstBrokenNodeString, section);
                                //else if (!string.IsNullOrEmpty(firstBrokenNodeString) && !string.IsNullOrEmpty(secondBrokenNodeString))
                                //    string.Concat(secondBrokenNodeString, section);
                                correctedLineString.Append(section + " ");
                            }
                            else
                            {
                                //if (string.IsNullOrEmpty(firstValidNodeString))
                                //    string.Concat(firstValidNodeString, section);
                                //else string.Concat(secondValidNodeString, section);
                                correctedLineString.Append(section + " ");
                            }

                        }

                        //correctedLineString = string.Concat(commandKeyWordSubString, firstBrokenNodeString);
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
        /// Takes <paramref name="commandSplitList"/> (a line from a configFile split into sections seperated by space) and removes the 
        /// '@' symbols so Nodes with spaces can be located correctly. 
        /// </summary>
        /// <param name="commandSplitList"></param>
        /// <returns>The list with splits that do not contain '@' characters.</returns>
        private static List<string> DecodeSpacesInCommandSplits(List<string> commandSplitList)
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
                throw new Exception($"An error occured trying to replace '@' characters from commandSplit. {e}");
            }
        }
    }
}
