namespace Models
{
    using APSIM.Shared.JobRunning;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
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
                else
                {
                    // Run simulations
                    var runner = new Runner(fileName, ignorePaths, recurse, runTests, runType, numberOfProcessors:numberOfProcessors);
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
            detailedHelpInfo += "    /Recurse               Recursively search subdirectories for files matching ApsimXFileSpec" + Environment.NewLine;
            detailedHelpInfo += "    /SingleThreaded        Run all simulations in a single thread." + Environment.NewLine;
            detailedHelpInfo += "    /RunTests              Run all tests." + Environment.NewLine;
            detailedHelpInfo += "    /Csv                   Export all reports to .csv files." + Environment.NewLine;
            detailedHelpInfo += "    /Version               Display the version number." + Environment.NewLine;
            detailedHelpInfo += "    /Verbose               Write messages to StdOut when a simulation starts/finishes. Only has an effect when running a directory of .apsimx files (*.apsimx)." + Environment.NewLine;
            detailedHelpInfo += "    /Upgrade               Upgrades a file to the latest version of the .apsimx file format. Does not run the file." + Environment.NewLine;
            detailedHelpInfo += "    /MultiProcess          Use the multi-process job runner." + Environment.NewLine;
            detailedHelpInfo += "    /NumberOfProcessors:xx Set the number of processors to use.";
            detailedHelpInfo += "    /?                     Show detailed help information.";
            Console.WriteLine(detailedHelpInfo);
        }

        /// <summary>
        /// Write the APSIM version to the console.
        /// </summary>
        private static void WriteVersion()
        {
            Model m = new Model();
            Console.WriteLine(m.ApsimVersion);
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
                Report.Report.WriteAllTables(storage, fileName);
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