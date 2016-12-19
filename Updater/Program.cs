using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Updater
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try {
                if (args.Length != 2)
                    throw new Exception("Usage: Updater uninstalldir newinstalldir");

                string uninstallDirectory = args[0];
                string newInstallDirectory = args[1];
                uint platformID = (uint)System.Environment.OSVersion.Platform;
                if (platformID <= 3)
                    DoWindowsUpdate(uninstallDirectory, newInstallDirectory);
                else if (platformID == 4 || platformID == 6 || platformID == 128)
                // Distinguishing Unix from OS X is a bit harder.
                // We have code for doing that in APSIM.Shared, but we don't want this
                // application to depend on any of our DLLs.
                {
                    int exitCode;
                    if (ReadProcessOutput("uname", null, out exitCode).Contains("Darwin"))
                        DoMacUpdate(uninstallDirectory, newInstallDirectory);
                    else
                        DoLinuxUpdate(uninstallDirectory, newInstallDirectory);
                }
                else
                    throw new Exception("Could not determine execution platform!");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error",  MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Perform the update by first uninstalling the old version and then installing the new.
        /// </summary>
        /// <param name="uninstallDirectory"></param>
        /// <param name="newInstallDirectory"></param>
        private static void DoWindowsUpdate(string uninstallDirectory, string newInstallDirectory)
        {
            WaitForProcess("ApsimNG");

            // Uninstall old version.
            string uninstaller = Path.Combine(uninstallDirectory, "unins000.exe");
            if (!File.Exists(uninstaller))
                throw new Exception("Cannot find uninstaller: " + uninstaller);
            Process uninstallProcess = Process.Start(uninstaller, "/SILENT");
            uninstallProcess.WaitForExit();

            // Install new version.
            Process installProcess = Process.Start(Path.Combine(Path.GetTempPath(), "ApsimSetup.exe"), "/SILENT");
            installProcess.WaitForExit();

            WaitForProcess("APSIMSetup");

            // Write to the downloads database.

            // Run the user interface.
            string userInterface = Path.Combine(newInstallDirectory, "Bin", "ApsimNG.exe");
            if (!File.Exists(userInterface))
                throw new Exception("Cannot find user interface: " + userInterface);
            Process.Start(userInterface);
        }

        private static void DoMacUpdate(string uninstallDirectory, string newInstallDirectory)
        {
        }

        private static void DoLinuxUpdate(string uninstallDirectory, string newInstallDirectory)
        {
            // Write the updater script to a temporary file.
            // We assume that we are in the same folder as the .deb file, and that
            // we have write-access to that folder.

            using (StreamWriter outputFile = new StreamWriter("updater.sh", false))
            {
                outputFile.WriteLine("#!/bin/sh");
                outputFile.WriteLine("sudo -v");
                outputFile.WriteLine("if [ $? -eq 0 ]");
                outputFile.WriteLine("then");
                outputFile.WriteLine("  if dpkg-query -Wf'${db:Status-abbrev}' apsim 2>/dev/null | grep -q '^i'; then");
                outputFile.WriteLine("    sudo dpkg -r apsim");
                outputFile.WriteLine("  fi");
                outputFile.WriteLine("  sudo dpkg -i $1");
                outputFile.WriteLine("  if [ $? -ne 0 ]");
                outputFile.WriteLine("  then");
                outputFile.WriteLine("    sudo apt install -f");
                outputFile.WriteLine("    sudo dpkg -i $1");
                outputFile.WriteLine("  fi");
                outputFile.WriteLine("else");
                outputFile.WriteLine("  echo \"This update script requires superuser privileges.\"");
                outputFile.WriteLine("  exit 1");
                outputFile.WriteLine("fi");
            }

            int exitCode;
            string output = ReadProcessOutput("/bin/sh", "./updater.sh APSIMSetup.deb", out exitCode);
            if (exitCode == 0)
            {
                File.Delete("updater.sh");
                // Run the user interface.
                string apsimCmd = "/usr/local/bin/apsim";
                if (!File.Exists(apsimCmd))
                    throw new Exception("Cannot find apsim at: " + apsimCmd);
                Process.Start(apsimCmd);
            }
            else
                MessageBox.Show("Update failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static string ReadProcessOutput(string name, string args, out int exitCode)
        {
            exitCode = -1;
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                if (args != null && args != "") p.StartInfo.Arguments = " " + args;
                p.StartInfo.FileName = name;
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.HasExited)
                    exitCode = p.ExitCode;
                if (output == null) output = "";
                output = output.Trim();
                return output;
            }
            catch
            {
                return "";
            }
        }

        private static int WaitForProcess(string exeName)
        {
            // Wait for the user interface to close - just in case.
            int i = 0;
            while (i < 10 && Process.GetProcessesByName(exeName).Count() > 0)
            {
                Thread.Sleep(1000);
                i++;
            }

            // If user interface didn't shut down then abort.
            if (i == 10)
                throw new Exception(exeName + " is still running. Aborting upgrade.");
            return i;
        }

        private static void UpdateDB()
        {

        }
    }
}
