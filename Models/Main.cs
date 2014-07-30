using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using Models.Core;

namespace Models
{
    public class Program
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        static int Main(string[] args)
        {
            try
            {
                string fileName = null;
                string commandLineSwitch = null;

                // Check the command line arguments.
                if (args.Length >= 1)
                    fileName = args[0];
                if (args.Length == 2)
                    commandLineSwitch = args[1];
                if (args.Length < 1 || args.Length > 3)
                    throw new Exception("Usage: ApsimX ApsimXFileSpec [/Recurse] [/Network]");

                // Create a instance of a job that will go find .apsimx files. Then
                // pass the job to a job runner.
                RunDirectoryOfApsimFiles runApsim = new RunDirectoryOfApsimFiles(fileName, commandLineSwitch);

                Stopwatch timer = new Stopwatch();
                timer.Start();

                int numSimulations = 0;
                if (commandLineSwitch == "/SingleThreaded")
                    numSimulations = RunSingleThreaded(fileName);
                else if (args.Contains("/Network"))
                {
                    try
                    {
                        DoNetworkRun(fileName);// send files over network
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Connection to server terminated.");
                    }
                }
                else
                {
                    Utility.JobManager jobManager = new Utility.JobManager();
                    jobManager.OnComplete += OnError;
                    jobManager.AddJob(runApsim);
                    jobManager.Start(waitUntilFinished: true);
                    if (jobManager.SomeHadErrors) return 1;

                    // Write out the number of simulations run to the console.
                    numSimulations = jobManager.NumberOfJobs - 1;
                }
                timer.Stop();
                Console.WriteLine("Finished running " + numSimulations.ToString() + " simulations. Duration " + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec.");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Run all simulations in the specified 'fileName' single threaded i.e.
        /// don't use the JobManager. Useful for profilling.
        /// </summary>
        private static int RunSingleThreaded(string fileName)
        {
            Simulations simulations = Simulations.Read(fileName);
            // Don't use JobManager - just run the simulations.
            Simulation[] simulationsToRun = Simulations.FindAllSimulationsToRun(simulations);
            foreach (Simulation simulation in simulationsToRun)
                simulation.Run(null, null);
            return simulationsToRun.Length;
        }

        /// <summary>
        /// Get a list of files (weather, input) to send to remote computer.
        /// </summary>
        /// <param name="fileName">The .apsimx file to parse.</param>
        /// <returns>A list of weather file names.</returns>
        private static List<string> GetFileList(string fileName)
        {
            List<string> FileList = new List<string>();
            XmlNodeList nodes;
            XmlDocument doc = new XmlDocument();

            doc.Load(fileName);
            XmlNode root = doc.DocumentElement;

            nodes = root.SelectNodes("//WeatherFile/FileName");
            foreach (XmlNode node in nodes)
                FileList.Add(node.InnerText);

            nodes = root.SelectNodes("//Input/FileNames/string");
            foreach (XmlNode node in nodes)
                FileList.Add(node.InnerText);

            // resolve relative file name
            for (int i = 0; i < FileList.Count; i++)
            {
                if (!File.Exists(FileList[i]))
                {
                    string TryFileName = Utility.PathUtils.GetAbsolutePath(FileList[i]);
                    if (File.Exists(TryFileName))
                        FileList[i] = TryFileName;
                }
            }
                return FileList;
        }

        /// <summary>
        /// Send our .apsimx and associated weather files over the network
        /// </summary>
        /// <param name="FileName">The .apsimx file to send.</param>
        private static void DoNetworkRun(string FileName)
        {
            string ServerIP = "150.229.142.16";
            //ServerIP = "127.0.0.1";
            //hold server acknowledge
            byte[] ack = new byte[1];

            // get a list of files to send
            List<string> FileList = GetFileList(FileName);
            FileList.Add(FileName);

            Console.WriteLine("Attempting connection to " + ServerIP);
            using (TcpClient client = new TcpClient(ServerIP, 50000))
            using (NetworkStream ns = client.GetStream())
            {
                Console.WriteLine("Connected to " + client.Client.RemoteEndPoint);
                // send the number of files to be sent
                byte[] dataLength = BitConverter.GetBytes(FileList.Count);
                ns.Write(dataLength, 0, 4);
                ns.Flush();

                foreach (string fileName in FileList)
                {
                    // send the length of the file name
                    dataLength = BitConverter.GetBytes(Path.GetFileName(fileName).Length);
                    ns.Write(dataLength, 0, 4);
                    ns.Flush();

                    // read the file and send the length;
                    byte[] FileStream = File.ReadAllBytes(fileName);
                    dataLength = BitConverter.GetBytes(FileStream.Length);
                    ns.Write(dataLength, 0, 4);
                    ns.Flush();

                    // then send the file name
                    Console.WriteLine("Sending file: " + fileName);
                    StreamWriter writer = new StreamWriter(ns, Encoding.ASCII);
                    writer.Write(Path.GetFileName(fileName));
                    writer.Flush();

                    //wait for acknowledgement from server
                    ns.Read(ack, 0, 1);

                    // next, send the file data
                    ns.Write(FileStream, 0, FileStream.Length);
                    ns.Flush();

                    //wait for acknowledgement from server
                    ns.Read(ack, 0, 1);
                }

                //get run updates from server
                byte[] MsgLength = new byte[4];
                byte[] Msg;
                while(true)
                {
                    ns.Read(MsgLength, 0, 4);
                    if (BitConverter.ToInt32(MsgLength, 0) == Int32.MaxValue)
                        break;

                    Msg = new byte[BitConverter.ToInt32(MsgLength, 0)];
                    ns.Read(Msg, 0, Msg.Length);

                    Console.WriteLine(Encoding.ASCII.GetString(Msg));
                }

                GetNetworkRun(ns, FileName);
            }
        }

        /// <summary>
        /// When an error is encountered, this handler will be called.
        /// </summary>
        static void OnError(object sender, Utility.JobManager.JobCompleteArgs e)
        {
            if (e.ErrorMessage != null)
                Console.WriteLine(e.ErrorMessage);
        }

        static void GetNetworkRun(NetworkStream stream, string FileName)
        {
            // the acknowledge byte
            byte[] ack = { 1 };

            byte[] data = new byte[4096];
            byte[] nameLength = new byte[4]; //hold the length of the next incoming file name
            byte[] dataLength = new byte[4];
            byte[] fileListLength = new byte[4];
            int bytesRead;
            string fileName;
            MemoryStream ms;

            //get the length of the file name
            bytesRead = stream.Read(fileListLength, 0, 4);
            if (bytesRead == 0)
                return;

            for (int i = 0; i < BitConverter.ToInt32(fileListLength, 0);i++ )
            {
                ms = new MemoryStream();
                bytesRead = 0;
                //get the length of the file name
                bytesRead = stream.Read(nameLength, 0, 4);
                if (bytesRead == 0)
                    break;

                //get the length of the file
                bytesRead = stream.Read(dataLength, 0, 4);
                if (bytesRead == 0)
                    break;

                // receive the file name
                byte[] nameData = new byte[BitConverter.ToInt32(nameLength, 0)];
                bytesRead = stream.Read(nameData, 0, nameData.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Could not read file name.");
                    break;
                }
                fileName = Encoding.ASCII.GetString(nameData);
                Console.WriteLine("Receiving " + fileName);

                // send an acknowledgement
                stream.Write(ack, 0, 1);

                // then the file itself.
                int totalBytes = 0;
                while (totalBytes < BitConverter.ToInt32(dataLength, 0))
                {
                    bytesRead = stream.Read(data, 0, 4096);
                    ms.Write(data, 0, bytesRead);
                    totalBytes += bytesRead;
                }

                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(FileName), fileName), ApServer.Convert2.ToByteArray(ms));

                Console.WriteLine(Path.Combine(Path.GetDirectoryName(FileName), fileName) + " successfully written.");

                // send an acknowledgement
                stream.Write(ack, 0, 1);
            }
        }

        /// <summary>
        /// This runnable class finds .apsimx files on the 'fileSpec' passed into
        /// the constructor. If 'recurse' is true then it will also recursively
        /// look for files in sub directories.
        /// </summary>
        [Serializable]
        class RunDirectoryOfApsimFiles : Utility.JobManager.IRunnable
        {
            private string FileSpec;
            private bool Recurse;

            /// <summary>
            /// Constructor
            /// </summary>
            public RunDirectoryOfApsimFiles(string fileSpec, string commandLineSwitch)
            {
                FileSpec = fileSpec;
                Recurse = commandLineSwitch == "/Recurse";
            }

            /// <summary>
            /// Run this job.
            /// </summary>
            public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
            {
                // Extract the path from the filespec. If non specified then assume
                // current working directory.
                string path = Path.GetDirectoryName(FileSpec);
                if (path == null)
                    path = Directory.GetCurrentDirectory();

                string fileSpecNoPath = Path.GetFileName(FileSpec);

                List<string> Files;
                if (Recurse)
                    Files = Directory.GetFiles(path, fileSpecNoPath, SearchOption.AllDirectories).ToList();
                else
                    Files = Directory.GetFiles(path, fileSpecNoPath, SearchOption.TopDirectoryOnly).ToList();

                Files.RemoveAll(s => s.Contains("UnitTests"));

                // Get a reference to the JobManager so that we can add jobs to it.
                Utility.JobManager jobManager = e.Argument as Utility.JobManager;

                // For each .apsimx file - read it in and create a job for each simulation it contains.
                foreach (string apsimxFileName in Files)
                {
                    Simulations simulations = Simulations.Read(apsimxFileName);
                    jobManager.AddJob(simulations);
                }
            }
        }


    }
}