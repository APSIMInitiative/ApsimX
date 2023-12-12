using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.ConfigFile;
using Models.Core.Run;
using Models.Storage;

namespace Models
{

    /// <summary>Class to hold a static main entry point.</summary>
    public class Program
    {
        private static object lockObject = new object();
        private static int exitCode = 0;
        private static List<Exception> exceptionsWrittenToConsole = new List<Exception>();

        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            bool isApplyOptionPresent = false;
            // Required to allow the --apply switch functionality of not including
            // an apsimx file path on the command line.
            if (args.Length > 0 && args[0].Equals("--apply"))
            {
                isApplyOptionPresent = true;
                string[] empty = { " " };
                empty = empty.Concat(args).ToArray();
                args = empty;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ReplaceObsoleteArguments(ref args);

            Parser parser = new Parser(config =>
            {
                config.AutoHelp = true;
                config.HelpWriter = Console.Out;
            });

            // Holds the switch(es) used in the command line call.
            ParserResult<Options> result = parser.ParseArguments<Options>(args);

            if (isApplyOptionPresent)
            {
                if (args.Length > 2)
                {
                    result.Value.Apply = args[2];
                }
                else
                {
                    string argsListString = string.Join(" ", args.ToList());
                    throw new Exception($"No config file was given with the --apply switch. Arguments given: {argsListString}");
                }
            }

            result.WithParsed(Run).WithNotParsed(HandleParseError);
            return exitCode;
        }

        /// <summary>
        /// Handles parser errors to ensure that a non-zero exit code
        /// is returned when parse errors are encountered.
        /// </summary>
        /// <param name="errors">Parse errors.</param>
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            if (!(errors.IsHelp() || errors.IsVersion()))
                exitCode = 1;
        }

        /// <summary>
        /// Run Models with the given set of options.
        /// </summary>
        /// <param name="options"></param>
        public static void Run(Options options)
        {
            try
            {
                string[] files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive)).ToArray();
                // Required to warn user that file was not found even though --apply switch used.
                if (options.Files.ToList().Count > 0
                    && options.Files.ToList().First() != " "
                    && !string.IsNullOrEmpty(options.Apply)
                    && files.Length < 1)
                {
                    throw new ArgumentException($"One or more files included before the --apply switch where not found. The files are: {options.Files.ToList()}.");
                }
                if (files == null || files.Length < 1 && string.IsNullOrEmpty(options.Apply))
                    throw new ArgumentException($"No files were specified");
                if (options.NumProcessors == 0)
                    throw new ArgumentException($"Number of processors cannot be 0");
                if (options.Upgrade)
                {
                    foreach (string file in files)
                    {
                        UpgradeFile(file);
                        if (options.Verbose)
                            Console.WriteLine("Successfully upgraded " + file);
                    }
                }
                else if (options.ListSimulationNames)
                    foreach (string file in files)
                        ListSimulationNames(file, options.SimulationNameRegex);
                else if (options.ListReferencedFileNames)
                {
                    foreach (string file in files)
                        ListReferencedFileNames(file);
                }
                else if (options.MergeDBFiles)
                {
                    string[] dbFiles = files.Select(f => Path.ChangeExtension(f, ".db")).ToArray();
                    string outFile = Path.Combine(Path.GetDirectoryName(dbFiles[0]), "merged.db");
                    DBMerger.MergeFiles(dbFiles, outFile);
                }
                // --apply switch functionality.
                else if (!string.IsNullOrWhiteSpace(options.Apply))
                {
                    bool isSimToBeRun = false;
                    string configFileAbsolutePath = Path.GetFullPath(options.Apply);
                    string configFileDirectory = Directory.GetParent(configFileAbsolutePath).FullName;
                    List<string> commands = ConfigFile.GetConfigFileCommands(options.Apply);
                    List<string> commandsWithoutNulls = ConfigFile.GetListWithoutNullCommands(commands);
                    List<string> commandsWithSpacesRemoved = ConfigFile.RemoveConfigFileWhitespace(commandsWithoutNulls.ToList());
                    string[] commandsArray = ConfigFile.EncodeSpacesInCommandList(commandsWithSpacesRemoved).ToArray();
                    string savePath = "";
                    string loadPath = "";

                    if (files.Length > 0)
                    {
                        string temporarySimLoadPath = "";
                        foreach (string file in files)
                        {

                            for (int i = 0; i < commandsArray.Length; i++)
                            {
                                string[] splitCommand = commandsArray[i].Split(' ', '=');
                                if (splitCommand[0] == "save")
                                {
                                    savePath = configFileDirectory + Path.DirectorySeparatorChar + splitCommand[1];
                                }
                                else if (splitCommand[0] == "load")
                                {
                                    loadPath = splitCommand[1];
                                    string fullLoadPath = configFileDirectory + Path.DirectorySeparatorChar + loadPath;
                                    List<string> filePathSplits = fullLoadPath.Split('.', '/', '\\').ToList();
                                    if (filePathSplits.Count >= 2)
                                    {
                                        // Creates a path string to a temp copy of the loadPath
                                        // apsimx file in the configFile's directory.
                                        temporarySimLoadPath = configFileDirectory +
                                                               Path.DirectorySeparatorChar +
                                                               filePathSplits[filePathSplits.Count - 2] +
                                                               "temp." +
                                                               filePathSplits.Last<string>() +
                                                               ".temp";
                                        // Create the file if new, back it up and write over it if it isn't.
                                        CreateApsimxFile(temporarySimLoadPath);
                                        File.Copy(fullLoadPath, temporarySimLoadPath, true);
                                    }
                                    else
                                        throw new Exception($"There was an error creating a new temporary file. The path causing issues was: {loadPath}");
                                }
                                else if (splitCommand[0] == "run")
                                {
                                    isSimToBeRun = true;
                                }
                                // Set and create if not already a temporary sim to make changes to.
                                // You should never make changes to the original unless specified in save command.
                                else
                                {
                                    if (string.IsNullOrEmpty(temporarySimLoadPath))
                                    {
                                        int indexOfLastDir = file.LastIndexOf(Path.DirectorySeparatorChar);
                                        int differenceOfLastDirAndLastPeriod = file.LastIndexOf('.') - indexOfLastDir;
                                        string fileName = file.Substring(indexOfLastDir + 1, differenceOfLastDirAndLastPeriod - 1);
                                        temporarySimLoadPath = configFileDirectory +
                                                               Path.DirectorySeparatorChar +
                                                               fileName +
                                                               "temp" +
                                                               ".apsimx.temp";
                                        // Create the file if new, back it up and write over it if it isn't.
                                        CreateApsimxFile(temporarySimLoadPath);
                                        File.Copy(file, temporarySimLoadPath, true);
                                    }

                                }

                                // Required as RunConfigCommands() requires list, not just a string.
                                List<string> commandWrapper = new()
                                {
                                    commandsArray[i]
                                };

                                Simulations sim;
                                // If loadPath is set, file should no longer be used.
                                if (!string.IsNullOrWhiteSpace(loadPath))
                                    sim = ConfigFile.RunConfigCommands(loadPath, commandWrapper, configFileDirectory) as Simulations;
                                else
                                    sim = ConfigFile.RunConfigCommands(temporarySimLoadPath, commandWrapper, configFileDirectory) as Simulations;

                                // Write to a specific file, if savePath is set use this instead of the file passed in through arguments.
                                // Otherwise 
                                if (!string.IsNullOrWhiteSpace(savePath))
                                    sim.Write(savePath);
                                else sim.Write(temporarySimLoadPath);

                                if (isSimToBeRun)
                                {
                                    // TODO: Needs to overwrite original and run it as this is expected behaviour.
                                    File.Copy(temporarySimLoadPath, file, true);
                                    sim = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false).NewModel as Simulations;

                                    Runner runner = new Runner(sim,
                                                                true,
                                                                true,
                                                                options.RunTests,
                                                                runType: options.RunType,
                                                                numberOfProcessors: options.NumProcessors,
                                                                simulationNamePatternMatch: options.SimulationNameRegex);
                                    RunSimulations(runner, options);
                                    isSimToBeRun = false;
                                    //// An assumption is made here that once a simulation is run a temp file is no longer needed.
                                    //// Release database files and clean up.
                                    //runner.DisposeStorage();
                                    //CleanUpTempFiles(configFileDirectory);
                                }
                            }
                        }
                    }
                    // If no apsimx file path included proceeding --apply switch...              
                    else if (files.Length < 1)
                    {
                        savePath = "";
                        loadPath = "";
                        string temporarySimLoadPath = "";
                        List<string> commandWrapper = new List<string>();

                        for (int i = 0; i < commandsArray.Length; i++)
                        {
                            List<string> tempCommandSplits = commandsArray.ToArray()[i].Split(' ', '=').ToList();
                            string[] splitCommand = ConfigFile.DecodeSpacesInCommandSplits(tempCommandSplits).ToArray();
                            if (splitCommand[0] == "save")
                            {
                                savePath = configFileDirectory + Path.DirectorySeparatorChar + splitCommand[1];
                            }
                            else if (splitCommand[0] == "load")
                            {
                                loadPath = splitCommand[1];
                                string fullLoadPath = configFileDirectory + Path.DirectorySeparatorChar + loadPath;
                                List<string> filePathSplits = fullLoadPath.Split('.', '/', '\\').ToList();
                                if (filePathSplits.Count >= 2)
                                {
                                    // Creates a path string to a temp copy of the loadPath
                                    // apsimx file in the configFile's directory.
                                    temporarySimLoadPath = configFileDirectory +
                                                           Path.DirectorySeparatorChar +
                                                           filePathSplits[filePathSplits.Count - 2] +
                                                           "temp." +
                                                           filePathSplits.Last<string>() +
                                                           ".temp";
                                    // Create the file if new, back it up and write over it if it isn't.
                                    CreateApsimxFile(temporarySimLoadPath);
                                    File.Copy(fullLoadPath, temporarySimLoadPath, true);
                                }
                                else
                                    throw new Exception($"There was an error creating a new temporary file. The path causing issues was: {loadPath}");
                            }
                            else if (splitCommand[0] == "run")
                            {
                                isSimToBeRun = true;
                            }

                            // Throw if the first command is not a save or load command.
                            if (i == 0 && String.IsNullOrEmpty(loadPath) && String.IsNullOrEmpty(savePath))
                            {
                                throw new Exception("First command in a config file can only be either a save or load command if no apsimx file is included.");
                            }

                            // Required as RunConfigCommands() requires list, not just a string.
                            commandWrapper = new List<string>()
                            {
                                commandsArray[i]
                            };

                            // As long as a file can be loaded any other command can be run.
                            if (!String.IsNullOrEmpty(loadPath))
                            {
                                // Temporary sim for holding changes.
                                Simulations sim;
                                // Makes sure that loadPath file is not overwritten.
                                if (!string.IsNullOrEmpty(temporarySimLoadPath))
                                    sim = ConfigFile.RunConfigCommands(temporarySimLoadPath, commandWrapper, configFileDirectory) as Simulations;
                                else
                                    sim = ConfigFile.RunConfigCommands(loadPath, commandWrapper, configFileDirectory) as Simulations;

                                if (!String.IsNullOrEmpty(loadPath) && String.IsNullOrEmpty(savePath))
                                    sim.Write(temporarySimLoadPath);
                                else if (!String.IsNullOrEmpty(loadPath) && !String.IsNullOrEmpty(savePath))
                                    sim.Write(savePath);
                                if (isSimToBeRun)
                                {
                                    // TODO: Needs to overwrite original and run it as this is expected behaviour.
                                    File.Copy(temporarySimLoadPath, loadPath, true);
                                    sim = FileFormat.ReadFromFile<Simulations>(loadPath, e => throw e, false).NewModel as Simulations;
                                    Runner runner = new Runner(sim,
                                                            true,
                                                            true,
                                                            options.RunTests,
                                                            runType: options.RunType,
                                                            numberOfProcessors: options.NumProcessors,
                                                            simulationNamePatternMatch: options.SimulationNameRegex);
                                    RunSimulations(runner, options);
                                    isSimToBeRun = false;
                                    //// An assumption is made here that once a simulation is run a temp file is no longer needed.
                                    //// Release database files and clean up. 
                                    //runner.DisposeStorage();
                                    //CleanUpTempFiles(configFileDirectory);
                                }
                            }
                            else if (!string.IsNullOrEmpty(savePath))
                            {
                                // Create a new simulation as an existing apsimx file was not included.
                                Simulations sim = CreateMinimalSimulation();
                                sim.Write(sim.FileName, savePath);
                                savePath = "";
                            }
                            else throw new Exception("--apply switch used without apsimx file and no load command. Include a load command in the config file.");

                        }

                    }

                }
                else
                {
                    Runner runner = null;
                    if (string.IsNullOrEmpty(options.EditFilePath))
                    {
                        runner = new Runner(files,
                                            options.RunTests,
                                            options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);

                        RunSimulations(runner, options);

                    }
                    else if (!string.IsNullOrEmpty(options.EditFilePath))
                    {

                        runner = new Runner(files.Select(f => ApplyConfigToApsimFile(f, options.EditFilePath)),
                                            true,
                                            true,
                                            options.RunTests,
                                            runType: options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);

                        RunSimulations(runner, options);
                    }

                    // If errors occurred, write them to the console.
                    if (exitCode != 0)
                        Console.WriteLine("ERRORS FOUND!!");
                    if (options.Verbose)
                        Console.WriteLine("Elapsed time was " + runner.ElapsedTime.TotalSeconds.ToString("F1") + " seconds");

                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                exitCode = 1;
            }
        }

        private static IModel ApplyConfigToApsimFile(string fileName, string configFilePath)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, e => throw e, false).NewModel as Simulations;
            var overrides = Overrides.ParseStrings(File.ReadAllLines(configFilePath));
            Overrides.Apply(file, overrides);
            return file;
        }

        private static void ReplaceObsoleteArguments(ref string[] args)
        {
            if (args == null)
                return;
            List<KeyValuePair<string, string>> replacements = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("/Recurse", "--recursive"),
                new KeyValuePair<string, string>("/SingleThreaded", "--single-threaded"),
                new KeyValuePair<string, string>("/RunTests", "--run-tests"),
                new KeyValuePair<string, string>("/Csv", "--csv"),
                new KeyValuePair<string, string>("/Version", "--version"),
                new KeyValuePair<string, string>("/Verbose", "--verbose"),
                new KeyValuePair<string, string>("/Upgrade", "--upgrade"),
                new KeyValuePair<string, string>("/MultiProcess", "--multi-process"),
                new KeyValuePair<string, string>("/NumberOfProcessors:", "--cpu-count="),
                new KeyValuePair<string, string>("/SimulationNameRegexPattern:", "--simulation-names="),
                new KeyValuePair<string, string>("/MergeDBFiles", "--merge-db-files"),
                new KeyValuePair<string, string>("/Edit", "--edit"),
                new KeyValuePair<string, string>("/ListSimulations", "--list-simulations"),
                new KeyValuePair<string, string>("/?", "--help"),
            };
            for (int i = 0; i < args.Length; i++)
                foreach (KeyValuePair<string, string> replacement in replacements)
                    args[i] = args[i].Replace(replacement.Key, replacement.Value);
        }

        /// <summary>
        /// Write the APSIM version to the console.
        /// </summary>
        private static void WriteVersion()
        {
            Console.WriteLine(Simulations.GetApsimVersion());
        }

        /// <summary>
        /// Upgrade a file to the latest APSIM version.
        /// </summary>
        /// <param name="file">The name of the file to upgrade.</param>
        private static void UpgradeFile(string file)
        {
            string contents = File.ReadAllText(file);
            ConverterReturnType converter = Converter.DoConvert(contents, fileName: file);
            if (converter.DidConvert)
                File.WriteAllText(file, converter.Root.ToString());
        }

        private static void ListSimulationNames(string fileName, string simulationNameRegex)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, e => throw e, false).NewModel as Simulations;

            SimulationGroup jobFinder = new SimulationGroup(file, simulationNamePatternMatch: simulationNameRegex);
            jobFinder.FindAllSimulationNames(file, null).ForEach(name => Console.WriteLine(name));

        }

        private static void ListReferencedFileNames(string fileName)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, e => throw e, false).NewModel as Simulations;

            foreach (var referencedFileName in file.FindAllReferencedFiles())
                Console.WriteLine(referencedFileName);
        }

        /// <summary>Job has completed</summary>
        private static void OnJobCompleted(object sender, JobCompleteArguments e)
        {
            if (e.ExceptionThrowByJob != null)
            {
                lock (lockObject)
                {
                    exceptionsWrittenToConsole.Add(e.ExceptionThrowByJob);
                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine(e.ExceptionThrowByJob.ToString());
                    exitCode = 1;
                }
            }
        }

        /// <summary>All jobs for a file have completed</summary>
        private static void OnSimulationGroupCompleted(object sender, EventArgs e)
        {
            if (sender is SimulationGroup group)
            {
                string fileName = Path.ChangeExtension(group.FileName, ".db");
                var storage = new Storage.DataStore(fileName);
                Report.WriteAllTables(storage, fileName);
                Console.WriteLine("Successfully created csv file " + Path.ChangeExtension(fileName, ".csv"));
            }
        }

        /// <summary>All jobs have completed</summary>
        private static void OnAllJobsCompleted(object sender, Runner.AllJobsCompletedArgs e)
        {
            if (sender is Runner runner)
                (sender as Runner).DisposeStorage();

            if (e.AllExceptionsThrown == null)
                return;

            foreach (Exception error in e.AllExceptionsThrown)
            {
                if (!exceptionsWrittenToConsole.Contains(error))
                {
                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine(error.ToString());
                    exitCode = 1;
                }
            }
        }

        /// <summary>
        /// Write a complete message to the console.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">The event arguments of the completed job.</param>
        private static void WriteCompleteMessage(object sender, JobCompleteArguments e)
        {
            if (e.Job == null)
                return;

            var message = new StringBuilder(e.Job.Name);
            if (e.Job is SimulationDescription sim && !string.IsNullOrEmpty(sim.SimulationToRun?.FileName))
                message.Append($" ({sim.SimulationToRun.FileName})");
            string duration = e.ElapsedTime.TotalSeconds.ToString("F1");
            message.Append($" has finished. Elapsed time was {duration} seconds.");
            Console.WriteLine(message);
        }

        /// <summary>
        /// Runs a specified runner.
        /// </summary>
        /// <param cref="Runner" name="runner">The runner to be ran.</param>
        /// <param cref="Options" name="options">The command line switches/flags.</param>
        private static void RunSimulations(Runner runner, Options options)
        {
            runner.SimulationCompleted += OnJobCompleted;
            if (options.Verbose)
                runner.SimulationCompleted += WriteCompleteMessage;
            if (options.ExportToCsv)
                runner.SimulationGroupCompleted += OnSimulationGroupCompleted;
            runner.AllSimulationsCompleted += OnAllJobsCompleted;
            // Run simulations.
            runner.Run();
        }

        private static Simulations CreateMinimalSimulation()
        {
            try
            {
                Simulations sims = new Simulations()
                {
                    Children = new List<IModel>()
                    {
                        new DataStore()
                    }
                };

                sims.Write("NewSimulation");
                return sims;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occured when trying to create a new minimal simulation." + ex.Message);
            }

        }

        /// <summary>
        /// Creates a backup of a file.
        /// </summary>
        /// <param name="existingFilePath">An apsimx file path string</param>
        private static void CreateApsimxFile(string existingFilePath)
        {
            if (File.Exists(existingFilePath))
            {
                string backupFilePath = existingFilePath + ".bak";
                File.Copy(existingFilePath, backupFilePath, true);
            }
            else
                File.Create(existingFilePath)?.Close();
        }

        // TODO: clean up function needs further work. Currently has trouble removing files due to other processes using the \
        // files.
        private static void CleanUpTempFiles(string configFileDirectoryPath)
        {
            string[] tempFileList = {
                "*temp.apsimx.temp.bak",
                "*temp.apsimx.dh-shm",
                "*temp.apsimx.db-wal",
                "*temp.apsimx.db",
                @"*.temp"
            };

            bool isFileInUse = false;
            List<string> matchingTempFiles = new();
            foreach (string file in tempFileList)
                foreach (string match in Directory.GetFiles(configFileDirectoryPath, file))
                    matchingTempFiles.Add(match);
            foreach (string matchingFile in matchingTempFiles)
            {
                while (isFileInUse == true)
                    isFileInUse = IsFileLocked(matchingFile);
                File.Delete(matchingFile);
            }

        }

        /// <summary>
        /// Closes file if in use and returns true otherwise returns false.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>bool</returns>
        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}