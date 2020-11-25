namespace Models
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using CommandLine;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Factorial;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>Class to hold a static main entry point.</summary>
    public class Program
    {
        private static object lockObject = new object();
        private static List<string> ignorePaths = new List<string>() { "UnitTests", "UserInterface", "ApsimNG" };
        private static int exitCode = 0;
        private static List<Exception> exceptionsWrittenToConsole = new List<Exception>();

        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            ReplaceObsoleteArguments(ref args);
            new Parser(config =>
            {
                config.AutoHelp = true;
                config.HelpWriter = Console.Out;
            })
                        .ParseArguments<Options>(args)
                        .WithParsed(Run)
                        .WithNotParsed(HandleParseError);
            return 0;
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (Error error in errors)
                if (error.Tag != ErrorType.HelpRequestedError
                 && error.Tag != ErrorType.VersionRequestedError)
                    Console.Error.WriteLine($"Parse error: {error.Tag}");
        }

        private static void Run(Options options)
        {
            try
            {
                string[] files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive)).ToArray();
                if (files == null || files.Length < 1)
                    throw new ArgumentException($"No files were specified");
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
                else if (options.EditFilePath != null)
                    foreach (string file in files)
                        EditFile.Do(file, options.EditFilePath);
                else if (options.MergeDBFiles)
                {
                    string[] dbFiles = files.Select(f => Path.ChangeExtension(f, ".db")).ToArray();
                    string outFile = Path.Combine(Path.GetDirectoryName(dbFiles[0]), "merged.db");
                    DBMerger.MergeFiles(dbFiles, outFile);
                }
                else
                {
                    // Run simulations
                    var runner = new Runner(files, ignorePaths, options.RunTests, options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);
                    runner.SimulationCompleted += OnJobCompleted;
                    if (options.Verbose)
                        runner.SimulationCompleted += WriteCompleteMessage;
                    if (options.ExportToCsv)
                        runner.SimulationGroupCompleted += OnSimulationGroupCompleted;
                    runner.AllSimulationsCompleted += OnAllJobsCompleted;
                    runner.Run();

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

            //return exitCode;
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
        /// Write message to user on command line usage and switches.
        /// </summary>
        private static void WriteUsageMessage()
        {
            string usageMessage = "Usage: Models ApsimXFileSpec [/Recurse] [/SingleThreaded] [/RunTests] [/Csv] [/Version] [/Verbose] [/Upgrade] [/MultiProcess] [/NumberOfProcessors:xx] [/SimulationNameRegexPattern:xx] [/MergeDBFiles] [/Edit <PathToConfigFile>] [/ListSimulations] [/?]";
            string detailedHelpInfo = usageMessage;
            detailedHelpInfo += Environment.NewLine + Environment.NewLine;
            detailedHelpInfo += "ApsimXFileSpec:          The path to an .apsimx file. May include wildcard.";
            detailedHelpInfo += Environment.NewLine + Environment.NewLine + "Options:" + Environment.NewLine;
            detailedHelpInfo += "    /Recurse                        Recursively search subdirectories for files matching ApsimXFileSpec" + Environment.NewLine;
            detailedHelpInfo += "    /SingleThreaded                 Run all simulations in a single thread." + Environment.NewLine;
            detailedHelpInfo += "    /RunTests                       Run all tests." + Environment.NewLine;
            detailedHelpInfo += "    /Csv                            Export all reports to .csv files." + Environment.NewLine;
            detailedHelpInfo += "    /Version                        Display the version number." + Environment.NewLine;
            detailedHelpInfo += "    /Verbose                        Write messages to StdOut when a simulation starts/finishes. Only has an effect when running a directory of .apsimx files (*.apsimx)." + Environment.NewLine;
            detailedHelpInfo += "    /Upgrade                        Upgrades a file to the latest version of the .apsimx file format. Does not run the file." + Environment.NewLine;
            detailedHelpInfo += "    /MultiProcess                   Use the multi-process job runner." + Environment.NewLine;
            detailedHelpInfo += "    /NumberOfProcessors:xx          Set the number of processors to use." + Environment.NewLine;
            detailedHelpInfo += "    /SimulationNameRegexPattern:xx  Use to filter simulation names to run." + Environment.NewLine;
            detailedHelpInfo += "    /MergeDBFiles                   Merges .db files into a single .db file." + Environment.NewLine;
            detailedHelpInfo += "    /Edit <PathToConfigFile>        Edits the .apsimx file. Path to a config file must be specified which contains lines of parameters to change in the form 'path = value'" + Environment.NewLine;
            detailedHelpInfo += "    /ListSimulations                List all simulation names in the file, without running the file." + Environment.NewLine;

            detailedHelpInfo += "    /?                              Show detailed help information.";
            Console.WriteLine(detailedHelpInfo);
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
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            SimulationGroup jobFinder = new SimulationGroup(file, simulationNamePatternMatch: simulationNameRegex);
            jobFinder.FindAllSimulationNames(file, null).ForEach(name => Console.WriteLine(name));

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
            if (e.AllExceptionsThrown != null)
            {
                foreach (var exception in e.AllExceptionsThrown)
                {
                    if (!exceptionsWrittenToConsole.Contains(exception))
                    {
                        Console.WriteLine("----------------------------------------------");
                        Console.WriteLine(exception.ToString());
                        exitCode = 1;
                    }
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
    }
}