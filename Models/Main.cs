using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using APSIM.Core;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;
using Models.Utilities.Extensions;
using Newtonsoft.Json.Linq;

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
            // Resets exitCode to help with unit testing.
            exitCode = 0;
            // Required to allow the --apply switch functionality of not including
            // an apsimx file path on the command line.
            if (args.Length > 0 &&
                args.Contains("--apply") &&
                !args.First().Equals(""))
            {
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

            if (args.Contains("--apply"))
            {
                if (string.IsNullOrEmpty(result.Value.Apply))
                    throw new Exception("No config file was given with the --apply switch." +
                        $"Arguments given: {string.Join(" ", args.ToList())}");
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
            // To help with Jenkins only errors.
            foreach (var error in errors)
            {
                //We need to exclude these as the nuget package has a bug that causes them to appear even if there is no error.
                if (error as VersionRequestedError == null && error as HelpRequestedError == null && error as MissingRequiredOptionError == null)
                {
                    Console.WriteLine("Console error output: " + error.ToString());
                    Trace.WriteLine("Trace error output: " + error.ToString());
                }
            }

            if (!(errors.IsHelp() || errors.IsVersion() || errors.Any(e => e is MissingRequiredOptionError)))
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
                if (files == null || files.Length < 1 && string.IsNullOrEmpty(options.Apply) && string.IsNullOrEmpty(options.Batch))
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
                else if (options.FileVersionNumber)
                {
                    if (files.Length > 1)
                        throw new ArgumentException("The file version number switch cannot be run with more than one file.");
                    string file = files.First();
                    JObject simsJObject = JObject.Parse(File.ReadAllText(file));
                    string fileVersionNumber = simsJObject["Version"].ToString();
                    Console.WriteLine(fileVersionNumber);
                }
                else if (options.ListSimulationNames)
                    foreach (string file in files)
                        ListSimulationNames(file, options.SimulationNameRegex);
                else if (options.ListEnabledSimulationNames)
                {
                    foreach (string file in files)
                    {
                        ListSimulationNames(file, options.SimulationNameRegex, true);
                    }
                }
                else if (options.ListReferencedFileNames)
                {
                    foreach (string file in files)
                        ListReferencedFileNames(file);
                }
                else if (options.ListReferencedFileNamesUnmodified)
                {
                    foreach (string file in files)
                        ListReferencedFileNames(file, false);
                }
                else if (options.MergeDBFiles)
                {
                    string[] dbFiles = files.Select(f => Path.ChangeExtension(f, ".db")).ToArray();
                    string outFile = Path.Combine(Path.GetDirectoryName(dbFiles[0]), "merged.db");
                    DBMerger.MergeFiles(dbFiles, outFile);
                }
                else if (!string.IsNullOrWhiteSpace(options.Log))
                {
                    bool result = MessageType.TryParse(options.Log, true, out MessageType msgType);
                    if (!result)
                        throw new ArgumentException("log option was not set to one of the following: error, warning, information, diagnostic or all.");
                    int verbosityFileChangeCount = 0;
                    foreach (string file in files)
                    {
                        Simulations sims = FileFormat.ReadFromFile<Simulations>(file).Model as Simulations;
                        List<Summary> summaryModels = sims.Node.FindChildren<Summary>(recurse: true).ToList();
                        foreach (Summary summaryModel in summaryModels)
                        {
                            summaryModel.Verbosity = msgType;
                            verbosityFileChangeCount++;
                        }
                        sims.Write(sims.FileName);
                    }
                    Console.WriteLine($"{verbosityFileChangeCount} summary nodes changed to {options.Log} level.");
                }
                // --apply switch functionality.
                else if (!string.IsNullOrWhiteSpace(options.Apply))
                {
                    string configFileAbsolutePath = Path.GetFullPath(options.Apply);
                    string configFileDirectory = Directory.GetParent(configFileAbsolutePath).FullName;
                    DoCommands(options, files, options.Apply);
                    CleanUpTempFiles(configFileDirectory);
                }
                else
                {
                    Runner runner = null;
                    if (!string.IsNullOrWhiteSpace(options.Playlist))
                    {
                        runner = CreateRunnerForPlaylistOption(options, files);
                    }
                    else
                    {
                        if (options.InMemoryDB)
                        {
                            List<Simulations> sims = CreateSimsList(files);
                            foreach (Simulations sim in sims)
                                sim.Node.FindChild<DataStore>().UseInMemoryDB = true;
                            runner = new Runner(sims,
                                            options.RunTests,
                                            runType: options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);
                        }
                        else
                        {
                            runner = new Runner(files,
                                            options.RunTests,
                                            runType: options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);
                        }
                    }
                    RunSimulations(runner, options);

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

        /// <summary>
        /// Takes an array of commands and runs them in sequence.
        /// </summary>
        /// <param name="options">The flags/switches given when calling models.exe.</param>
        /// <param name="files">the file name strings provided.</param>
        /// <param name="commandFileName">Name of command file.</param>
        /// <exception cref="Exception"></exception>
        private static void DoCommands(Options options, string[] files, string commandFileName)
        {
            // Ensure there is always a files array with at least one element so that ExecuteCommands
            // always gets called.
            if (files.Length == 0)
                files = [null];

            // Calculate the directory relative to the command file.
            string relativeToDirectory = Path.GetDirectoryName(commandFileName);
            if (string.IsNullOrEmpty(relativeToDirectory))
                relativeToDirectory = Directory.GetCurrentDirectory();

            List<string> commandsList = File.ReadAllLines(commandFileName).ToList();

            if (options.Batch != null)
            {
                if (File.Exists(options.Batch) && Path.GetExtension(options.Batch).Equals(".csv"))
                {
                    using var streamReader = new StreamReader(options.Batch);
                    var dataTable = DataTableUtilities.FromCSV(options.Batch, streamReader.ReadToEnd());

                    foreach (DataRow row in dataTable.Rows)
                    {
                        var dict = row.Table.Columns
                                        .Cast<DataColumn>()
                                        .ToDictionary(c => c.ColumnName, c => row[c].ToString());

                        for (int i = 0; i < commandsList.Count; i++)
                            commandsList[i] = Macro.Replace(commandsList[i], dict);

                        foreach (string file in files)
                            ExecuteCommands(options, commandsList, file, relativeToDirectory, row);
                    }
                }
            }
            else
            {
                foreach (string file in files)
                    ExecuteCommands(options, commandsList, file, relativeToDirectory);
            }
        }

        /// <summary>
        /// Executes the list of commands. Used when files are included as an argument in Models call.
        /// </summary>
        /// <param name="options">Arguments from Models call.</param>
        /// <param name="commandsList">A list of commands.</param>
        /// <param name="file">The name of the file.</param>
        /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
        /// <param name="row"></param>
        private static void ExecuteCommands(Options options, List<string> commandsList, string file, string relativeToDirectory, DataRow row = null)
        {
            // Create am APSIM runner for a commands.
            var runner = new Runner(relativeTo: null as IModel,
                                    runSimulations: true,
                                    runPostSimulationTools: true,
                                    options.RunTests,
                                    runType: options.RunType,
                                    numberOfProcessors: options.NumProcessors,
                                    simulationNamePatternMatch: options.SimulationNameRegex);
            runner.Playlist = options.Playlist;
            runner.UseInMemoryDB = options.InMemoryDB;

            runner.SimulationCompleted += OnJobCompleted;
            if (options.Verbose)
                runner.SimulationCompleted += WriteCompleteMessage;
            if (options.ExportToCsv)
                runner.SimulationGroupCompleted += OnSimulationGroupCompleted;
            runner.AllSimulationsCompleted += OnAllJobsCompleted;

            // Get a node that the commands are relative to. If a file wasn't specified on the command line
            // then create an empty Simulations node and use that as the node. Otherwise use the root node
            // in the specified file.
            INodeModel relativeTo = null;
            if (file == null)
            {
                relativeTo = new Simulations()
                {
                    Children = [ new DataStore() ]
                };
                Node.Create(relativeTo);
            }
            else
            {
                Node rootNode = FileFormat.ReadFromFile<Simulations>(file);
                relativeTo = rootNode.Model;
            }

            // Convert all command strings into commands and run them.
            var commands = CommandLanguage.StringToCommands(commandsList, relativeTo, relativeToDirectory);
            CommandProcessor.Run(commands, relativeTo, runner);
        }

        /// <summary>
        /// Creates a runner specifically for the playlist option/switch.
        /// </summary>
        /// <param name="options">Switches used on the command line</param>
        /// <param name="files">The files included when models is called. Can be null.</param>
        /// <returns>Runner that uses the Playlist model as the parameter 'relativeTo'.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static Runner CreateRunnerForPlaylistOption(Options options, string[] files)
        {
            Runner runner;
            if (files != null)
            {
                if (files.Length > 1)
                    throw new ArgumentException("The playlist switch cannot be run with more than one file.");
            }
            Simulations file = FileFormat.ReadFromFile<Simulations>(files.First()).Model as Simulations;
            Playlist playlistModel = file.Node.FindChild<Playlist>();
            if (playlistModel.Enabled == false)
                throw new ArgumentException("The specified playlist is disabled and cannot be run.");
            IEnumerable<Playlist> playlists = new List<Playlist> { file.Node.FindChild<Playlist>(options.Playlist) };
            if (playlists.Any() && playlists.First() == null)
                throw new ArgumentException($"A playlist named {options.Playlist} could not be found in the {file.FileName}.");
            runner = new Runner(playlists,
                                runSimulations: true,
                                runPostSimulationTools: true,
                                options.RunTests,
                                runType: options.RunType,
                                numberOfProcessors: options.NumProcessors,
                                simulationNamePatternMatch: options.SimulationNameRegex);
            return runner;
        }

        private static IModel ApplyConfigToApsimFile(string fileName, string configFilePath)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName).Model as Simulations;
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
            var response = FileFormat.ReadFromFileAndReturnConvertState<Simulations>(file);
            if (response.didConvert)
                File.WriteAllText(file, response.head.ToJSONString());
        }

        private static void ListSimulationNames(string fileName, string simulationNameRegex, bool showEnabledOnly = false)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName).Model as Simulations;

            if (showEnabledOnly)
            {
                List<string> sims = GetAllSimulationAndFactorialNameList(file, true);
                sims.ForEach(Console.WriteLine);
            }
            else
            {
                List<string> sims = GetAllSimulationAndFactorialNameList(file);
                if (string.IsNullOrEmpty(simulationNameRegex))
                {
                    sims.ForEach(Console.WriteLine);
                }
                else
                {
                    PrintMatchingStrings(simulationNameRegex, sims);
                }
            }
        }

        /// <summary>
        /// Get all sSimulation and Factorial names from the given file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="onlyEnabled"></param>
        /// <returns></returns>
        private static List<string> GetAllSimulationAndFactorialNameList(Simulations file, bool onlyEnabled = false)
        {
            List<string> sims = [];
            if (onlyEnabled)
            {
                sims = file.Node.FindChildren<Simulation>().Where(sim => sim.Enabled == true).Select(sim => sim.Name).ToList();
                List<string> allExperimentCombinations = file.Node.FindChildren<Experiment>(recurse: true).SelectMany(experiment => experiment.GetSimulationDescriptions(false).Select(sim => sim.Name)).ToList();
                sims.AddRange(allExperimentCombinations);
            }
            else
            {
                sims = file.Node.FindChildren<Simulation>().Select(sim => sim.Name).ToList();
                List<string> allExperimentCombinations = file.Node.FindChildren<Experiment>(recurse: true).SelectMany(experiment => experiment.GetSimulationDescriptions().Select(sim => sim.Name)).ToList();
                sims.AddRange(allExperimentCombinations);
            }
            return sims;
        }

        /// <summary>
        /// Print the simulation names that match the given regex.
        /// </summary>
        /// <param name="simulationNameRegex"></param>
        /// <param name="sims"></param>
        private static void PrintMatchingStrings(string simulationNameRegex, List<string> sims)
        {
            Regex r = new Regex(simulationNameRegex);
            foreach (var sim in sims)
            {
                if (r.IsMatch(sim))
                    Console.WriteLine(sim);
            }
        }

        private static void ListReferencedFileNames(string fileName, bool isAbsolute = true)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName).Model as Simulations;

            foreach (var referencedFileName in file.FindAllReferencedFiles(isAbsolute))
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
                string csvFilePattern = $"{Path.GetFileNameWithoutExtension(fileName)}.*.csv";
                if(Directory.GetFiles(Path.GetDirectoryName(fileName), csvFilePattern).Length > 0)
                    Console.WriteLine("Successfully created csv file(s) " + fileName);
                else Console.WriteLine("Unable to make csv file(s) for " + fileName);
            }
        }

        /// <summary>All jobs have completed</summary>
        private static void OnAllJobsCompleted(object sender, IRunner.AllJobsCompletedArgs e)
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

        /// <summary>
        /// Creates an apsimx file that has a 'Simulations' model with a child 'DataStore'.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
                Node.Create(sims);

                sims.Write("NewSimulation");
                return sims;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occured when trying to create a new minimal simulation." + ex.Message);
            }

        }


        private static void CleanUpTempFiles(string configFileDirectoryPath)
        {
            // IMPORTANT: The order of these is important. Do not modify.
            string[] tempFileList = {
            "*temp.apsimx.temp.bak",
            @"*.temp",
            "*temp.apsimx.db-shm",
            "*temp.apsimx.db-wal",
            "*temp.apsimx.db",
            };

            bool isFileInUse = false;
            List<string> matchingTempFiles = new();
            foreach (string file in tempFileList)
                foreach (string match in Directory.GetFiles(configFileDirectoryPath, file))
                    if (match != null)
                        matchingTempFiles.Add(match);

            //give up trying to to delete the files if they are blocked for some reason.
            int breakout = 100;
            while (matchingTempFiles.Count > 0 && breakout > 0)
            {
                for(int i = matchingTempFiles.Count-1; i >= 0; i --)
                {
                    isFileInUse = (new FileInfo(matchingTempFiles[i])).IsLocked();
                    if (!isFileInUse)
                    {
                        File.Delete(matchingTempFiles[i]);
                        matchingTempFiles.Remove(matchingTempFiles[i]);
                    }
                }
                breakout -= 1;
            }
        }

        /// <summary>
        /// Takes file strings and returns a list of Simulations
        /// </summary>
        /// <param name="files">a list of file names</param>
        /// <returns>A list of Simulations</returns>
        private static List<Simulations> CreateSimsList(IEnumerable<string> files)
        {
            List<Simulations> sims = new();
            foreach (string file in files)
                sims.Add(FileFormat.ReadFromFile<Simulations>(file, e => throw e, true).Model as Simulations);
            return sims;
        }
    }


}