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
                DoUpate(uninstallDirectory, newInstallDirectory);
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
        private static void DoUpate(string uninstallDirectory, string newInstallDirectory)
        {
            int i = WaitForProcess("UserInterface");

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
            string userInterface = Path.Combine(newInstallDirectory, "Bin", "UserInterface.exe");
            if (!File.Exists(userInterface))
                throw new Exception("Cannot find user interface: " + userInterface);
            Process.Start(userInterface);
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
