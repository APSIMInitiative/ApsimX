// <copyright file="Program.cs" company="Apsim"> The APSIM Initiative 2014. </copyright>
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Xml;
    using Models.Core;
    using System.Threading;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Class to hold a static main entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "ApsimX");
            Directory.CreateDirectory(tempFolder);
            Environment.SetEnvironmentVariable("TMP", tempFolder, EnvironmentVariableTarget.Process);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Manager.ResolveManagerAssembliesEventHandler);

            int exitCode = 0 ;
            try
            {
                string fileName = null;
                string commandLineSwitch = null;

                // Check the command line arguments.
                if (args.Length >= 1)
                {
                    fileName = args[0];
                }

                if (args.Length == 2)
                {
                    commandLineSwitch = args[1];
                }

                if (args.Length < 1 || args.Length > 4)
                {
                    throw new Exception("Usage: ApsimX ApsimXFileSpec [/Recurse] [/Network] [/IP:<server IP>]");
                }

                Stopwatch timer = new Stopwatch();
                timer.Start();

                if (commandLineSwitch == "/SingleThreaded")
                    RunSingleThreaded(fileName);
                
                else if (args.Contains("/Network"))
                {
                    try
                    {
                        int indexOfIPArgument = -1;
                        for (int i = 0; i < args.Length; i++)
                        {
                            if (args[i].Contains("IP"))
                            {
                                indexOfIPArgument = i;
                                break;
                            }
                        }

                        if (indexOfIPArgument == -1)
                        {
                            throw new Exception("/Network specified, but no IP given (/IP:<server IP>]");
                        }

                        DoNetworkRun(fileName, args[indexOfIPArgument].Split(':')[1], args.Contains("/Recurse")); // send files over network
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Connection to server terminated.");
                    }
                }
                else
                {
                    // Create a instance of a job that will go find .apsimx files. Then
                    // pass the job to a job runner.
                    JobManager.IRunnable job;
                    if (fileName.Contains('*') || fileName.Contains('?'))
                        job = Runner.ForFolder(fileName, args.Contains("/Recurse"), args.Contains("/RunTests"));
                    else
                        job = Runner.ForFile(fileName, args.Contains("/RunTests"));
                    JobManager jobManager = new JobManager();
                    jobManager.AddJob(job);
                    jobManager.Start(waitUntilFinished: true);

                    List<Exception> errors = jobManager.Errors(job);
                    if (errors.Count > 0)
                    {
                        errors.ForEach(e => Console.WriteLine(e.ToString() + Environment.NewLine));
                        exitCode = 1;
                    }
                    else
                        exitCode = 0;
                }

                timer.Stop();
                Console.WriteLine("Finished running simulations. Duration " + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec.");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                exitCode = 1;
            }

            if (exitCode != 0)
                Console.WriteLine("ERRORS FOUND!!");
            return exitCode;
        }

        /// <summary>
        /// Run all simulations in the specified 'fileName' in a serial mode.
        /// i.e. don't use the multi-threaded JobManager. Useful for profiling.
        /// </summary>
        /// <param name="fileName"> the name of file to run </param>
        private static void RunSingleThreaded(string fileName)
        {
            Simulations simulations = Simulations.Read(fileName);

            // Don't use JobManager - just run the simulations.
            JobManager.IRunnable simulationsToRun = Runner.ForSimulations(simulations, simulations, false);
            JobManager jobManager = new JobManager();
            jobManager.AddJob(simulationsToRun);
            jobManager.Run();
        }

        /// <summary>
        /// Get a list of files (apsimx, weather, input, etc) to send to remote computer.
        /// </summary>
        /// <param name="fileName">The file name to parse (Can include wildcard *).</param>
        /// <param name="doRecurse">Recurse through sub directories?</param>
        /// <returns>A list of file names.</returns>
        private static List<string> GetFileList(string fileName, bool doRecurse)
        {
            string[] files;
            List<string> fileList = new List<string>();

            if (fileName.Contains('*')) 
            {
                files = Directory.GetFiles(Path.GetDirectoryName(fileName), Path.GetFileName(fileName), doRecurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            } 
            else 
            {
                files = new string[] { fileName };
            }

            foreach (string s in files)
            {
                List<string> tempList = new List<string>();

                fileList.Add(s);
                XmlNodeList nodes;
                XmlDocument doc = new XmlDocument();

                doc.Load(s);
                XmlNode root = doc.DocumentElement;

                nodes = root.SelectNodes("//WeatherFile/FileName");
                foreach (XmlNode node in nodes) 
                {
                    tempList.Add(node.InnerText);
                }

                nodes = root.SelectNodes("//Model/FileName");
                foreach (XmlNode node in nodes) 
                {
                    tempList.Add(node.InnerText);
                }

                nodes = root.SelectNodes("//Input/FileNames/string");
                foreach (XmlNode node in nodes)
                {
                    tempList.Add(node.InnerText);
                }

                // resolve relative file name
                for (int i = 0; i < tempList.Count; i++)
                {
                    if (!File.Exists(tempList[i])) 
                    {
                        // Some of the unit tests are designed to fail so have invalid data
                        // They probably should go in a different directory.
                        // WHAT A SILLY IDEA! FIXME!!!!
                        if (s.Contains("UnitTests")) 
                        {
                            continue;
                        }

                        string tryFileName = PathUtilities.GetAbsolutePath(tempList[i], s);
                        if (!File.Exists(tryFileName)) 
                        {
                            throw new ApsimXException(null, "Could not construct absolute path for " + tempList[i]);
                        }
                        else 
                        {
                            fileList.Add(tryFileName);
                        }
                    }
                }
            }
                
            return fileList;
        }

        /// <summary>
        /// Send our .apsimx and associated weather files over the network
        /// </summary>
        /// <param name="fileName">The .apsimx file to send.</param>
        /// <param name="serverIP">IP address of server to talk to </param>
        /// <param name="doRecurse">Recurse through sub directories?</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "Reviewed.")] 
        private static void DoNetworkRun(string fileName, string serverIP, bool doRecurse)
        {
            // get a list of files to send
            List<string> fileList = GetFileList(fileName, doRecurse);

            Console.WriteLine("Attempting connection to " + serverIP);
            using (TcpClient client = new TcpClient(serverIP, 50000))
            using (NetworkStream ns = client.GetStream())
            {
                Console.WriteLine("Connected to " + client.Client.RemoteEndPoint);

                // send the number of files to be sent
                byte[] dataLength = BitConverter.GetBytes(fileList.Count);
                ns.Write(dataLength, 0, 4);
                ns.Flush();

                foreach (string name in fileList)
                {
                    ApServer.Utility.SendNetworkFile(name, ns);
                }

                // get run updates from server
                byte[] msgLength = new byte[4];
                byte[] msg;
                while (true)
                {
                    ns.Read(msgLength, 0, 4);

                    // This test is the reason for the above warning suppression
                    if (BitConverter.ToInt32(msgLength, 0) == Int32.MaxValue) 
                    {
                        break;
                    }
                
                    msg = new byte[BitConverter.ToInt32(msgLength, 0)];
                    ns.Read(msg, 0, msg.Length);

                    Console.WriteLine(Encoding.ASCII.GetString(msg));
                }

                ApServer.Utility.ReceiveNetworkFiles(ns, client, Path.GetDirectoryName(fileName));
            }
        }

    }
}