namespace Models
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
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
        private static string[] arguments;
        private static int exitCode = 0;
        private static List<Exception> exceptionsWrittenToConsole = new List<Exception>();

        private static string fileName { get { return arguments[0]; } }
        private static bool recurse { get { return arguments.Contains("/Recurse"); } }
        private static bool version { get { return arguments.Contains("/Version"); } }
        private static bool upgrade { get { return arguments.Contains("/Upgrade"); } }
        private static bool runTests { get { return arguments.Contains("/RunTests"); } }
        private static bool verbose { get { return arguments.Contains("/Verbose"); } }
        private static bool csv { get { return arguments.Contains("/Csv"); } }
        private static bool mergeDBFiles { get { return arguments.Contains("/MergeDBFiles"); } }
        private static bool edit { get { return arguments.Contains("/Edit"); } }

        private static Runner.RunTypeEnum runType
        {
            get
            {
                if (arguments.Contains("/SingleThreaded"))
                    return Runner.RunTypeEnum.SingleThreaded;
                else if (arguments.Contains("/MultiProcess"))
                    return Runner.RunTypeEnum.MultiProcess;
                else
                    return Runner.RunTypeEnum.MultiThreaded;
            }
        }
        private static int numberOfProcessors
        {
            get
            {
                foreach (var argument in arguments)
                {
                    var index = argument.IndexOf("/NumberOfProcessors:");
                    if (index != -1)
                        return Convert.ToInt32(argument.Substring("/NumberOfProcessors:".Length));
                }
                return -1;
            }
        }
        private static string simulationNameRegex
        {
            get
            {
                foreach (var argument in arguments)
                {
                    var index = argument.IndexOf("/SimulationNameRegexPattern:");
                    if (index != -1)
                        return argument.Substring("/SimulationNameRegexPattern:".Length);
                }
                return "";
            }
        }

        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            if (args.Contains("/?") || args.Length < 1 || args.Length > 10)
            {
                WriteUsageMessage();
                return 1;
            }

            arguments = args;
            try
            {
                if (version)
                    WriteVersion();
                else if (upgrade)
                    UpgradeFile(fileName, recurse);
                else if (edit)
                    EditFile(fileName, recurse);
                else if (mergeDBFiles)
                    DBMerger.MergeFiles(fileName, recurse, Path.Combine(Path.GetDirectoryName(fileName), "merged.db"));
                else
                {
                    // Run simulations
                    var runner = new Runner(fileName, ignorePaths, recurse, runTests, runType,
                                            numberOfProcessors: numberOfProcessors,
                                            simulationNamePatternMatch: simulationNameRegex);
                    runner.SimulationCompleted += OnJobCompleted;
                    runner.SimulationGroupCompleted += OnSimulationGroupCompleted;
                    runner.AllSimulationsCompleted += OnAllJobsCompleted;
                    runner.Run();

                    // If errors occurred, write them to the console.
                    if (exitCode != 0)
                        Console.WriteLine("ERRORS FOUND!!");
                    if (verbose)
                        Console.WriteLine("Elapsed time was " + runner.ElapsedTime.TotalSeconds.ToString("F1") + " seconds");
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                exitCode = 1;
            }

            return exitCode;
        }

        /// <summary>
        /// Write message to user on command line usage and switches.
        /// </summary>
        private static void WriteUsageMessage()
        {
            string usageMessage = "Usage: Models ApsimXFileSpec [/Recurse] [/SingleThreaded] [/RunTests] [/Csv] [/Version] [/Verbose] [/Upgrade] [/m] [/?]";
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
        /// <param name="fileName">The name of the file to upgrade.</param>
        /// <param name="recurse">Recurse though child folders?</param>
        private static void UpgradeFile(string fileName, bool recurse)
        {
            string dir = Path.GetDirectoryName(fileName);
            if (string.IsNullOrWhiteSpace(dir))
                dir = Directory.GetCurrentDirectory();
            string[] files = Directory.EnumerateFiles(dir, Path.GetFileName(fileName), recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToArray();
            foreach (string file in files)
            {
                List<Exception> errors;
                IModel sims = FileFormat.ReadFromFile<Model>(file, out errors);
                if (errors != null && errors.Count > 0)
                    foreach (Exception error in errors)
                        Console.Error.WriteLine(error.ToString());
                File.WriteAllText(file, FileFormat.WriteToString(sims));
                Console.WriteLine("Successfully upgraded " + file);
            }
        }

        /// <summary>
        /// Called when the user passes the /Edit command line switch.
        /// Performs pattern matching and edits all specified .apsimx
        /// files (e.g. *.apsimx /Recurse).
        /// </summary>
        private static void EditFile(string fileName, bool recurse)
        {
            int index = Array.IndexOf(arguments, "/Edit");
            if (index < 0)
                throw new Exception("Illegal state - this should never happen. /Edit paramter was not specified?");
            if (index + 1 >= arguments.Length)
                throw new Exception("/Edit option was provided but no config file argument was given. The config file argument must directly follow the /Edit argument. Use this syntax: Models.exe path/to/apsimXFile.apsimx /Edit path/to/configfile.txt");
            string configFileName = arguments[index + 1];
            List<CompositeFactor> factors = GetFactors(configFileName);

            string dir = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dir))
                dir = Directory.GetCurrentDirectory();

            string[] files = Directory.EnumerateFiles(dir, Path.GetFileName(fileName), recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToArray();
            foreach (string file in files)
                EditFile(file, factors);
        }

        /// <summary>
        /// Gets a list of factors from a config file.
        /// </summary>
        /// <remarks>
        /// Each line in the file must be of the form:
        /// 
        /// path = value
        /// 
        /// e.g.
        /// 
        /// [Clock].StartDate = 1/1/2019
        /// .Simulations.Simulation.Weather.FileName = asdf.met
        /// </remarks>
        /// <param name="configFileName">Path to the config file.</param>
        private static List<CompositeFactor> GetFactors(string configFileName)
        {
            List<CompositeFactor> factors = new List<CompositeFactor>();
            string[] lines = File.ReadAllLines(configFileName);
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] values = lines[i].Split('=');
                if (values.Length != 2)
                    throw new Exception($"Wrong number of values specified on line {i} of config file '{configFileName}'.");

                string path = values[0].Trim();
                string value = values[1].Trim();
                factors.Add(new CompositeFactor("factor", path, value));
            }

            return factors;
        }

        /// <summary>
        /// Edits a single apsimx file according to the changes specified in the config file.
        /// </summary>
        /// <param name="apsimxFileName">Path to an .apsimx file.</param>
        /// <param name="factors">Factors to apply to the file.</param>
        private static void EditFile(string apsimxFileName, List<CompositeFactor> factors)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw new Exception($"Error reading file ${apsimxFileName}: {errors[0].ToString()}");

            foreach (CompositeFactor factor in factors)
            {
                IVariable variable = Apsim.GetVariableObject(file, factor.Paths[0]);
                if (variable == null)
                    throw new Exception($"Invalid path: {factor.Paths[0]}");

                string value = factor.Values[0].ToString();
                string absolutePath;
                try
                {
                    absolutePath = PathUtilities.GetAbsolutePath(value, Directory.GetCurrentDirectory());
                }
                catch
                {
                    absolutePath = null;
                }

                string[] parts = value.Split(';');
                if (parts != null && parts.Length == 2)
                {
                    string fileName = parts[0];
                    string absoluteFileName = PathUtilities.GetAbsolutePath(fileName, Directory.GetCurrentDirectory());
                    string modelPath = parts[1];

                    if (File.Exists(fileName))
                        ReplaceModelFromFile(file, factor.Paths[0], fileName, modelPath);
                    else if (File.Exists(absoluteFileName))
                        ReplaceModelFromFile(file, factor.Paths[0], absoluteFileName, modelPath);
                    else
                        variable.Value = ReflectionUtilities.StringToObject(variable.DataType, value);
                }
                else if (File.Exists(value) && variable.Value is IModel)
                    ReplaceModelFromFile(file, factor.Paths[0], value, null);
                else if (File.Exists(absolutePath) && variable.Value is IModel)
                    ReplaceModelFromFile(file, factor.Paths[0], absolutePath, null);
                else
                    variable.Value = ReflectionUtilities.StringToObject(variable.DataType, value);
            }
            file.Write(apsimxFileName);
        }

        /// <summary>
        /// Replace a model with a model from another file.
        /// </summary>
        /// <param name="topLevel">The top-level model of the file being modified.</param>
        /// <param name="modelToReplace">Path to the model which is to be replaced.</param>
        /// <param name="replacementFile">Path of the .apsimx file containing the model which will be inserted.</param>
        /// <param name="replacementPath">Path to the model in replacementFile which will be used to replace a model in topLevel.</param>
        private static void ReplaceModelFromFile(Simulations topLevel, string modelToReplace, string replacementFile, string replacementPath)
        {
            IModel toBeReplaced = Apsim.Get(topLevel, modelToReplace) as IModel;
            if (toBeReplaced == null)
                throw new Exception($"Unable to find model which is to be replaced ({modelToReplace}) in file {topLevel.FileName}");

            IModel extFile = FileFormat.ReadFromFile<IModel>(replacementFile, out List<Exception> errors);
            if (errors?.Count > 0)
                throw new Exception($"Error reading replacement file {replacementFile}", errors[0]);

            IModel replacement;
            if (string.IsNullOrEmpty(replacementPath))
            {
                replacement = Apsim.ChildrenRecursively(extFile, toBeReplaced.GetType()).FirstOrDefault();
                if (replacement == null)
                    throw new Exception($"Unable to find replacement model of type {toBeReplaced.GetType().Name} in file {replacementFile}");
            }
            else
            {
                replacement = Apsim.Get(extFile, replacementPath) as IModel;
                if (replacement == null)
                    throw new Exception($"Unable to find model at path {replacementPath} in file {replacementFile}");
            }

            IModel parent = toBeReplaced.Parent;
            int index = parent.Children.IndexOf((Model)toBeReplaced);
            parent.Children.Remove((Model)toBeReplaced);

            // Need to call Structure.Add to add the model to the parent.
            Structure.Add(replacement, parent);

            // Move the new model to the index in the list at which the
            // old model previously resided.
            parent.Children.Remove((Model)replacement);
            parent.Children.Insert(index, (Model)replacement);
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
                    if (verbose)
                        WriteCompleteMessage(e);
                    exitCode = 1;
                }
            }
            else if (verbose)
                WriteCompleteMessage(e);
        }

        /// <summary>All jobs for a file have completed</summary>
        private static void OnSimulationGroupCompleted(object sender, EventArgs e)
        {
            if (csv)
            {
                string fileName = Path.ChangeExtension((sender as SimulationGroup).FileName, ".db");
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
        /// <param name="e">The event arguments of the completed job.</param>
        private static void WriteCompleteMessage(JobCompleteArguments e)
        {
            var message = new StringBuilder();
            WriteDetailsToMessage(e, message);
            if (e.Job != null)
            {
                message.Append(" has finished. Elapsed time was ");
                message.Append(e.ElapsedTime.TotalSeconds.ToString("F1"));
                message.Append(" seconds.");
            }
            Console.WriteLine(message);
        }

        /// <summary>
        /// Write part of a complete message to a string builder.
        /// </summary>
        /// <param name="e">The event arguments of the completed job.</param>
        /// <param name="message">The string builder to write to.</param>
        private static void WriteDetailsToMessage(JobCompleteArguments e, StringBuilder message)
        {
            if (e.Job is SimulationDescription)
            {
                message.Append((e.Job as SimulationDescription).Name);
                if (string.IsNullOrEmpty(fileName))
                {
                    message.Append(" (");
                    message.Append(fileName);
                    message.Append(')');
                }
            }
        }

    }
}