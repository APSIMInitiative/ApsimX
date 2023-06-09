using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static IEnumerable<string> GetConfigFileCommands(string configFilePath)
        {
            // List for all the commands, whether they are override commands or instruction commands.
            // Instructions = used for adding, creating, deleting, copying, saving, loading
            // Overrides = used for modifying existing .apsimx node values.
            List<string> configFileCommands = File.ReadAllLines(configFilePath).ToList();
            return configFileCommands;
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
                Simulations sim = FileFormat.ReadFromFile<Simulations>(apsimxFilePath, e => throw e, false).NewModel as Simulations;

                foreach (string command in configFileCommands)
                {
                    string[] splitCommands = command.Split(' ', '=');
                    // If first index item is a string containing "[]" the command is an override...
                    if (splitCommands[0].Contains('['))
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
                        string keywordString = splitCommands[0].ToLower();
                        if (keywordString.Contains("add")) { keyword = Keyword.Add; }
                        else if (keywordString.Contains("delete")) { keyword = Keyword.Delete; }
                        else if (keywordString.Contains("copy")) { keyword = Keyword.Copy; }
                        else if (keywordString.Contains("save")) { keyword = Keyword.Save; }
                        else if (keywordString.Contains("load")) { keyword = Keyword.Load; }
                        else if (keywordString.Contains("run")) { keyword = Keyword.Run; }

                        if (splitCommands.Length > 1)
                        {
                            // Determine if its a nodeToModify/SavePath/LoadPath
                            if (keyword == Keyword.Add || keyword == Keyword.Copy || keyword == Keyword.Delete)
                            {
                                // Check for required format
                                if (splitCommands[1].Contains('[') && splitCommands[1].Contains(']'))
                                {
                                    string modifiedNodeName = splitCommands[1].Substring(1).Trim(']');
                                    nodeToModify = modifiedNodeName;
                                }
                                else throw new Exception("Format of parent model type does not match required format: [ModelName]");

                            }
                            else if (keyword == Keyword.Load)
                                loadPath = splitCommands[1];
                            else
                                savePath = splitCommands[1];
                        }

                        // Determine 3rd split string usage.
                        if (splitCommands.Length > 2)
                        {
                            switch (keywordString)
                            {
                                case "add":
                                    // Checks for a node in a 
                                    // Check for back-slash...
                                    if (splitCommands[2].Contains("\\"))
                                    {
                                        string[] filePathAndNodeName = splitCommands[2].Split(';');
                                        if (filePathAndNodeName.Length == 2)
                                        {
                                            fileContainingNode = filePathAndNodeName[0];
                                            string reformattedNode = filePathAndNodeName[1].Substring(1).Trim(']');
                                            nodeForAction = reformattedNode;
                                        }
                                        else throw new Exception("Add command missing either file or node name.");
                                    }
                                    else
                                    {
                                        string reformattedNode = "{\"$type\": \"Models." + splitCommands[2] + ", Models\"}";
                                        nodeForAction = reformattedNode;
                                    }
                                    break;
                                case "copy":
                                    break;
                                case "delete":
                                    break;
                            }
                        }
                        // Instruction to be used with Structure.cs.
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
                //Check for add keyword in instruction.
                if (instruction.keyword == Keyword.Add)
                {

                    IModel simulationNode = simulation.FindAllChildren().First(m => m.Name == "Simulation");
                    IModel parentNode = simulationNode.FindAllChildren().First(m => m.Name == instruction.NodeToModify);
                    Structure.Add(instruction.NodeForAction, parentNode);
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
                        IModel nodeToCopy = simToCopyFrom.FindInScope(instruction.NodeForAction); // TODO: needs testing.
                        IModel simToCopyTo = simulation.FindAllChildren().First(m => m.Name == "Simulation");
                        IModel parentNode = simToCopyTo.FindAllChildren().First(m => m.Name == instruction.NodeToModify);
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


    }
}
