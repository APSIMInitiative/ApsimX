using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using APSIM.Shared.Utilities;
using System.Data;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Text;

namespace Models.Utilities
{
    /// <summary>
    /// Class for executing arbitrary R code through APSIM.
    /// </summary>
    public class R
    {
        /// <summary>
        /// Stable link which always points to the latest Windows release.
        /// </summary>
        private const string windowsDownloadUrl = "https://cran.r-project.org/bin/windows/base/release.htm";

        /// <summary>
        /// Path to a temporary working directory for the script.
        /// </summary>
        private string workingDirectory;

        /// <summary>
        /// Takes care of initialising and starting the process, reading output, etc.
        /// </summary>
        private ProcessUtilities.ProcessWithRedirectedOutput proc;

        /// <summary>
        /// Holds the path to the Rscript executable.
        /// </summary>
        private string rScript;

        /// <summary>
        /// Directory to which packages will be installed.
        /// On Windows, this is %appdata%\ApsimInitiative\ApsimX\rpackages.
        /// On Linux, this is ~/.config/ApsimInitiative/ApsimX/rpackages.
        /// </summary>
        public static string PackagesDirectory { get; } = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "ApsimInitiative",
                                    "ApsimX",
                                    "rpackages").Replace("\\", "/");

        /// <summary>
        /// Default constructor. Checks if R is installed.
        /// On Windows, prompts user to install if necessasry.
        /// Will throw on 'Nix systems if R is not installed.
        /// </summary>
        public R()
        {
            rScript = GetRExePath();
            // Create a temporary working directory.
            workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDirectory)) // I would be very suprised if it did already exist
                Directory.CreateDirectory(workingDirectory);

            string startupCommand = $".libPaths(c(.libPaths(), '{PackagesDirectory}'))";
            string startupFile = Path.Combine(workingDirectory, ".Rprofile");
            if (!File.Exists(startupFile))
                File.WriteAllText(startupFile, startupCommand);
        }

        /// <summary>
        /// Destructor - deletes working directory.
        /// </summary>
        ~R()
        {
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, true);
        }

        /// <summary>
        /// Invoked when the R script is finished.
        /// </summary>
        public EventHandler Finished;

        /// <summary>
        /// Method to be run before downloading R and installing R.
        /// R will not be downloaded if this returns false.
        /// </summary>
        public Func<bool> OnDownload;

        /// <summary>
        /// Method to run once R has finished downloading.
        /// </summary>
        public Action OnDownloadCompleted;

        /// <summary>
        /// Starts the execution of an R script.
        /// </summary>
        /// <param name="fileName">Path to an R script. May be a file on disk, or an embedded resource.</param>
        /// <param name="arguments">Command line arguments to pass to the script.</param>
        public void RunAsync(string fileName, params string[] arguments)
        {
            string scriptName = fileName;
            if (!File.Exists(scriptName) && Assembly.GetExecutingAssembly().GetManifestResourceInfo(scriptName) != null)
            {
                // If the file doesn't exist, we check the list of resources.
                string script = string.Empty;
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(scriptName))
                    using (StreamReader reader = new StreamReader(s))
                        script = reader.ReadToEnd();
                scriptName = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), ".r");
                File.WriteAllText(scriptName, script);
            }

            // Each command line argument must be quoted in case it contains spaces.
            string args = "";
            if (arguments.Length > 0)
                args = arguments.Aggregate((x, y) => $"\"{x}\" \"{y}\"");

            proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Exited += OnExited;
            proc.Start(rScript, "\"" + scriptName + "\" " + args, workingDirectory, true);
        }

        /// <summary>
        /// Runs an R script and blocks the current thread until the script finishes its execution.
        /// </summary>
        /// <param name="fileName">Path to an R script. May be a file on disk, or an embedded resource.</param>
        /// <param name="arguments">Command line arguments to pass to the script.</param>
        /// <param name="throwOnError">Throw on error from R?</param>
        /// <returns>Standard output generated by the R script.</returns>
        public string Run(string fileName, bool throwOnError = true, params string[] arguments)
        {
            RunAsync(fileName, arguments);
            proc.WaitForExit();
            string message;
            if (proc.ExitCode != 0)
            {
                StringBuilder error = new StringBuilder("Error from R:");
                error.AppendLine($"Script path: '{fileName}'");
                error.AppendLine("Script contents:");
                error.AppendLine(File.ReadAllText(fileName));
                error.AppendLine("StdErr:");
                error.AppendLine(proc.StdErr);
                error.AppendLine("StdOut:");
                error.AppendLine(proc.StdOut);

                message = error.ToString();
                if (throwOnError)
                    throw new Exception(message);
            }
            else
                message = proc.StdOut;
            return message;
        }

        /// <summary>
        /// Runs an R script (synchronously) and returns the stdout as a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="fileName">Path to an R script. May be a file on disk, or an embedded resource.</param>
        /// <param name="arguments">Command line arguments to pass to the script.</param>
        /// <returns>Output formatted as a <see cref="DataTable"/>.</returns>
        /// <remarks>Not sure that this method really belongs in this class, but it can stay here for now.</remarks>
        public DataTable RunToTable(string fileName, params string[] arguments)
        {
            string result = Run(fileName, true, arguments);
            string tempFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), "csv");
            if (!File.Exists(tempFile))
                File.Create(tempFile).Close();
            
            File.WriteAllText(tempFile, result);

            DataTable table = null;
            try
            {
                table = ApsimTextFile.ToTable(tempFile);
            }
            catch (Exception)
            {
                throw new Exception(File.ReadAllText(tempFile));
            }
            finally
            {
                Thread.Sleep(200);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            return table;
        }

        /// <summary>
        /// Kills the process running the R script.
        /// </summary>
        public void Kill()
        {
            proc.Kill();
        }

        /// <summary>
        /// Installs an R package if it is not already installed.
        /// </summary>
        /// <param name="package">List of packages to be installed.</param>
        /// <returns>Path to the library's install location.</returns>
        public string InstallPackage(string package)
        {
            string script;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Models.Resources.GetPackage.R"))
                using (StreamReader reader = new StreamReader(s))
                    script = reader.ReadToEnd();
            string tempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "-getPackage.r");
            string rFileName = Path.ChangeExtension(tempFileName, ".R");
            File.WriteAllText(rFileName, script);

            Run(rFileName, true, package, PackagesDirectory);
            try
            {
                File.Delete(rFileName);
            }
            catch
            {
                // Ignore errors
            }
            return PackagesDirectory;
        }

        /// <summary>
        /// Runs when the script has finished running.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExited(object sender, EventArgs e)
        {
            Finished?.Invoke(sender, e);
        }

        /// <summary>
        /// Get path to RScript.exe
        /// By default we try to use the 64-bit version.
        /// </summary>
        /// <returns>Path to RScript.exe</returns>
        private string GetRExePath()
        {
            string rScriptPath;
            if (ProcessUtilities.CurrentOS.IsWindows)
            {
                // On Windows we search the registry for the install path
                string installDirectory = GetRInstallDirectoryFromRegistry();
                string rScript32 = Path.Combine(installDirectory, "bin", "Rscript.exe");
                string rScript64 = Path.Combine(installDirectory, "bin", "x64", "Rscript.exe");
                if (File.Exists(rScript64))
                    rScriptPath = rScript64;
                else if (File.Exists(rScript32))
                    rScriptPath = rScript32;
                else
                    throw new Exception("Unable to find path to R binaries.");
            }
            else if (ProcessUtilities.CurrentOS.IsUnix)
            {
                // On Unix systems, we use the which utility
                // First, make sure R is installed.
                if (string.IsNullOrEmpty(GetPathToPackage("R")))
                    throw new Exception("You need to install R to use this feature. https://cran.csiro.au/bin/ is a good place to start.");

                // On Linux, the Rscript executable comes with the package littler. This package is available on 
                // Ubuntu, Debian, Arch, Fedora, Redhat, CentOS, openSUSE, Kali, and Mint (among others).
                // It's not available on Gentoo, but anyone using Gentoo will be capable of finding a workaround.
                rScriptPath = GetPathToPackage("Rscript");
                if (string.IsNullOrEmpty(rScriptPath))
                    throw new Exception("Unable to find RScript binary. You need to install the littler package.");
                else
                    rScriptPath = rScriptPath.Trim(Environment.NewLine.ToCharArray());
            }
            else
                // This is unlikely.
                throw new NotImplementedException("Your OS is not supported.");
            return rScriptPath;
        }

        /// <summary>
        /// Gets the directory that the latest version of R is installed to.
        /// </summary>
        private string GetRInstallDirectoryFromRegistry()
        {
            string registryKey = @"SOFTWARE\R-core";
            List<string> subKeyNames = GetSubKeys(registryKey);
            if (subKeyNames == null)
            {
                DownloadR();
                subKeyNames = GetSubKeys(registryKey);
            }

            string rKey;
            if (subKeyNames == null)
                throw new Exception("Unable to find R entry in Registry - is R installed?.");
            if (subKeyNames.Contains("R64"))
                rKey = registryKey + @"\R64";
            else if (subKeyNames.Contains("R"))
                rKey = registryKey + @"\R";
            else
                throw new Exception("Unable to find R entry in Registry - is R installed?.");

            List<string> versions = GetSubKeys(rKey);
            if (versions == null)
                throw new Exception("Unable to find R entry in Registry - is R installed?.");
            else
            {
                // Ignore Microsoft R client. 
                string latestVersionKeyName = rKey + @"\" + versions.Where(v => !v.Contains("Microsoft R Client")).OrderByDescending(i => i).First();
                string installDirectory = null; 
                using (RegistryKey latestVersionKey = Registry.LocalMachine.OpenSubKey(latestVersionKeyName))
                {
                    if (latestVersionKey != null)
                        installDirectory = Registry.GetValue(latestVersionKey.ToString(), "InstallPath", null) as string;
                }
                if (installDirectory == null)
                    using (RegistryKey latestVersionKey = Registry.CurrentUser.OpenSubKey(latestVersionKeyName))
                    {
                        if (latestVersionKey != null)
                            installDirectory = Registry.GetValue(latestVersionKey.ToString(), "InstallPath", null) as string;
                    }
                return installDirectory;
            }

        }

        /// <summary>
        /// Gets all sub keys of a given key name in the registry.
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private List<string> GetSubKeys(string keyName)
        {
            try
            {
                List<string> keys;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName))
                {
                    keys = key?.GetSubKeyNames().ToList();
                }

                if (keys == null)
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName))
                    {
                        keys = key?.GetSubKeyNames().ToList();
                    }


                return keys;
            }
            catch
            {
                return null;
            }
            
        }

        /// <summary>
        /// Downloads R to the user's machine.
        /// </summary>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="IOException" />
        /// <exception cref="NotSupportedException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="WebException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="Exception" />
        private void DownloadR()
        {
            if (OnDownload != null && !OnDownload())
                return;
            if (ProcessUtilities.CurrentOS.IsWindows)
            {
                string fileName = Path.Combine(Path.GetTempPath(), "RSetup.exe");

                // Delete the installer if it already exists.
                if (File.Exists(fileName))
                    File.Delete(fileName);
                WebClient web = new WebClient();
                web.DownloadFileCompleted += (sender, e) =>
                {
                    try
                    {
                        OnDownloadCompleted?.Invoke();
                        InstallR(fileName);
                    }
                    catch
                    {
                    }
                };
                web.DownloadFileAsync(new Uri(windowsDownloadUrl), fileName);
            }
            else if (ProcessUtilities.CurrentOS.IsMac)
            {
                throw new NotImplementedException("R auto download not yet available on macOS.");
            }
            else if (ProcessUtilities.CurrentOS.IsLinux)
            {
                throw new Exception("R auto download not yet available on Linux.");
            }
            else
            {
                throw new Exception("Target running unknown OS.");
            }
        }

        /// <summary>
        /// Runs the R installer.
        /// </summary>
        /// <param name="installerPath">Path to the installer.</param>
        private void InstallR(string installerPath)
        {
            if (installerPath == null)
                return;

            // Setup a working directory.
            string workingDirectory = Path.Combine(Path.GetTempPath(), "RSetup");
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);

            // Copy installer to working directory.
            try
            {
                string newInstallerPath = Path.Combine(workingDirectory, Path.GetFileName(installerPath));
                File.Copy(installerPath, newInstallerPath, true);
                installerPath = newInstallerPath;
            }
            catch
            {
            }
            // Check to see if installer is already running for whatever reason.
            // Kill them if found.
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(installerPath)))
                process.Kill();

            // Run the installer.
            var installer = new ProcessUtilities.ProcessWithRedirectedOutput();
            installer.Start(installerPath, "", workingDirectory, false);
            installer.WaitForExit();
        }

        /// <summary>
        /// Gets the path to an executable (uses the Unix which utility). 
        /// Throws if the package does not exist. Obviously this will not
        /// work on Windows.
        /// </summary>
        private string GetPathToPackage(string package)
        {
            ProcessUtilities.ProcessWithRedirectedOutput findR = new ProcessUtilities.ProcessWithRedirectedOutput();
            findR.Start("/usr/bin/which", package, Path.GetTempPath(), true);
            findR.WaitForExit();
            if (string.IsNullOrEmpty(findR.StdOut) && !string.IsNullOrEmpty(findR.StdErr))
            {
                // If the shell command generated anything in StdErr, we display that message.
                throw new Exception("Encountered an error while searching for " + package + " installation: " + findR.StdErr);
            }
            return findR.StdOut;
        }
    }
}
