using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace RunAllSims
{
    /// <summary>
    /// Control program for running simulations. Ensure that errors are correctly returned to Jenkins.
    /// Very basic, doesn't run sims individually, just enumerates the test directory and runs them all in parallel.
    /// **Note: disabled multithreading as it was causing the first sim to fail.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string[] Files = Directory.GetFiles("Tests", "*.apsimx", SearchOption.AllDirectories);
            bool AllRun = true;
            string ErrorReport = "";

            //     Parallel.ForEach(Files, file =>
            foreach (string file in Files)
            {
                // skip sims in UnitTest directory.
                if (file.Contains("UnitTest"))
                    continue;

                Process p = new Process();
                p.StartInfo.Arguments = file;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = "Bin" + Path.DirectorySeparatorChar + "model.exe";
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
                Console.WriteLine("Running " + file);
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                    AllRun = false;
                string output = p.StandardOutput.ReadToEnd();
                if (output != "")
                    Console.WriteLine(output);
                Console.WriteLine("Completed " + file + " [" + (p.ExitTime - p.StartTime).Seconds + "sec]");
                ErrorReport += p.StandardError.ReadToEnd();
            }
            //   });

            Console.WriteLine(ErrorReport);
            if (AllRun)
                Environment.Exit(0);
            else
                Environment.Exit(1);
        }
    }
}