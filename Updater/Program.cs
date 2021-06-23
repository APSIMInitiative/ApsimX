using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Updater
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
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
                Console.WriteLine(err);
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

            // Run the user interface.
            string userInterface = Path.Combine(newInstallDirectory, "Bin", "ApsimNG.exe");
            if (!File.Exists(userInterface))
            {
                string progFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string testPath = Path.Combine(progFilesDir, Path.GetFileName(newInstallDirectory), "Bin", "ApsimNG.exe");
                if (File.Exists(testPath))
                    userInterface = testPath;
                else
                {
                    progFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    testPath = Path.Combine(progFilesDir, Path.GetFileName(newInstallDirectory), "Bin", "ApsimNG.exe");
                    if (File.Exists(testPath))
                        userInterface = testPath;

                }
                if (!File.Exists(userInterface))
                    throw new Exception("Cannot find user interface: " + userInterface);
            }
            Process.Start(userInterface);
        }

        private static void DoMacUpdate(string uninstallDirectory, string newInstallDirectory)
        {
            // Write the updater script to a temporary file.
            // We assume that we are in the same folder as the .deb file, and that
            // we have write-access to that folder.

            using (StreamWriter outputFile = new StreamWriter("updater.sh", false))
            {
                outputFile.WriteLine("#!/bin/sh");
                outputFile.WriteLine("if [ $# -ge 1 ] && [ -e $1 ]");
                outputFile.WriteLine("then");
                outputFile.WriteLine("  APSIMDMG=`hdiutil attach $1`");
                outputFile.WriteLine("  if [ $? -ne 0 ]");
                outputFile.WriteLine("  then");
                outputFile.WriteLine("    echo \"Unable to mount $1.\"");
                outputFile.WriteLine("    exit 1");
                outputFile.WriteLine("  fi");
                outputFile.WriteLine("  DMGDevice=$(echo $APSIMDMG | cut -f1 -d' ')");
                outputFile.WriteLine("  DMGPath=$(echo $APSIMDMG | cut -f2 -d' ')");
                outputFile.WriteLine("  appPath=$(echo $DMGPath)/`ls $DMGPath`");
                outputFile.WriteLine("  if [ $# -eq 2 ] && [ -d $2 ]");
                outputFile.WriteLine("  then");
                outputFile.WriteLine("    rm -rf $2");
                outputFile.WriteLine("  fi");
                outputFile.WriteLine("  cp -R $appPath /Applications");
                outputFile.WriteLine("  hdiutil detach -quiet $DMGDevice");
                outputFile.WriteLine("fi");
            }

            int exitCode;
            // If we're in a application bundle, go up two more levels to get the main folder
            if (Path.GetFileName(uninstallDirectory) == "Resources")
            {
                uninstallDirectory = Path.GetDirectoryName(uninstallDirectory);
                uninstallDirectory = Path.GetDirectoryName(uninstallDirectory);
            }
            string newInstallName = Path.Combine("/Applications", Path.GetFileName(newInstallDirectory) + ".app");

            ReadProcessOutput("/bin/sh", "./updater.sh APSIMSetup.dmg " + uninstallDirectory, out exitCode);
            if (exitCode == 0)
            {
                File.Delete("updater.sh");
                // Run the user interface.
                if (!Directory.Exists(newInstallName))
                    throw new Exception("Cannot find apsim at: " + newInstallName);
                Process.Start("/usr/bin/open", "-a " + newInstallName);
            }
            else
                Console.WriteLine("Update failed");
        }

        private static void DoLinuxUpdate(string uninstallDirectory, string newInstallDirectory)
        {
            // Write the updater script to a temporary file.
            // We assume that we are in the same folder as the .deb file, and that
            // we have write-access to that folder.

            using (StreamWriter outputFile = new StreamWriter("updater.sh", false))
            {
                outputFile.WriteLine("#!/bin/sh");
                outputFile.WriteLine("zenity --password --title \"sudo access required\" --timeout 30 | sudo -v -S");
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
            ReadProcessOutput("/bin/sh", "./updater.sh APSIMSetup.deb", out exitCode);
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
                Console.WriteLine("Update failed");
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
    }
}
