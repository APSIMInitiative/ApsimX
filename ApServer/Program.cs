using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Xml;

/***************************************************
 * This project is an attempt to try to get an Apsim
 * run working across multiple machines.
 *
 *  AUTHOR: Justin Fainges
 *  TCP Server adapted from http://tech.pro/tutorial/704/csharp-tutorial-simple-threaded-tcp-server
 *  
 ***************************************************/

namespace ApServer
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private static string dataDir = "";

        public static void Main(string[] args)
        {
            Server server = new Server();
        }

        public Server()
        {
            Console.WriteLine("Server started.");

            if (string.IsNullOrEmpty(dataDir))
            {
                // Create a temporary working directory.
                dataDir = Path.Combine(Path.GetTempPath(), "ApServer");
                if (Directory.Exists(dataDir))
                    Directory.Delete(dataDir, true);
                Directory.CreateDirectory(dataDir);
            }

            this.tcpListener = new TcpListener(IPAddress.Any, 50000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            string userDir = Path.Combine(dataDir, tcpClient.Client.RemoteEndPoint.ToString().Replace(':', '-'));
            userDir = userDir.Replace(".", "");

            // Old userDir can stick around if client connection was terminated during a runl; if so, delete it.
            // Note users port will change constantly, so this won't clean up everything but it will stop conflicts
            // when a port is recycled.
            if (Directory.Exists(userDir))
                Directory.Delete(userDir, true);

            Directory.CreateDirectory(userDir);

            try
            {
                Utility.ReceiveNetworkFiles(clientStream, tcpClient, userDir);
                RunSimulations(userDir, clientStream);
            }
            catch
            {
                //a socket error has occured
                Console.WriteLine("Connection to " + tcpClient.Client.RemoteEndPoint + " terminated");
            }
            finally
            {
                tcpClient.Close();
              //  Directory.Delete(userDir, true);
            }
        }

        private void RunSimulations(string dir, NetworkStream stream)
        {
            // First, change paths to external files
            foreach (string s in Directory.GetFiles(dir, "*.apsimx", SearchOption.AllDirectories))
            {
                XmlDocument doc = new XmlDocument();
                XmlNode root;
                XmlNodeList nodes;

                doc.Load(s);
                root = doc.DocumentElement;

                nodes = root.SelectNodes("//WeatherFile/FileName");
                foreach (XmlNode node in nodes)
                    node.InnerText = GetAbsolutePath(node.InnerText, s);

                nodes = root.SelectNodes("//Model/FileName");
                foreach (XmlNode node in nodes)
                    node.InnerText = GetAbsolutePath(node.InnerText, s);

                nodes = root.SelectNodes("//Input/FileNames/string");
                foreach (XmlNode node in nodes)
                    node.InnerText = GetAbsolutePath(node.InnerText, s);
                doc.Save(s);
            }

            Process process = new Process();
            process.StartInfo.FileName = "model.exe";
            process.StartInfo.Arguments = Path.Combine(dataDir, dir, "*.apsimx") + " /Recurse";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler((sender, args) => OutputEventHandler(stream, args));
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            stream.Write(BitConverter.GetBytes(Int32.MaxValue), 0, 4); //using Int32.MaxValue as an EOS designator.

            // Send completed runs back to user.
            SendCompletedData(dir, stream);
            
            Console.WriteLine("Run complete.");
        }

        private string GetAbsolutePath(string path, string SimPath)
        {

            //try to find a path relative to the .apsimx directory
            string NewPath = Path.Combine(Path.GetDirectoryName(SimPath), path);
            if (File.Exists(NewPath))
                return Path.GetFullPath(NewPath); //use this to strip any relative path leftovers.

            //try to remove any overlapping path data
            while (path.IndexOf(Path.DirectorySeparatorChar) != -1)
            {
                path = ReducePath(path);
                NewPath = Path.Combine(Path.GetDirectoryName(SimPath), path);

                if (File.Exists(NewPath))
                    return NewPath;
            }

            Console.WriteLine("Could not find path to file: " + path + "using: " + SimPath);
            return "";
        }

        private string ReducePath(string path)
        {
            return path.Substring(path.IndexOf(Path.DirectorySeparatorChar) + 1, path.Length - path.IndexOf(Path.DirectorySeparatorChar) - 1);
        }

        /// <summary>
        /// Send completed runs back to the user.
        /// </summary>
        /// <param name="dir">The users remote directory.</param>
        /// <param name="stream">The users connection.</param>
        private void SendCompletedData(string dir, NetworkStream stream)
        {
            List<string> DatabaseFiles = Directory.GetFiles(dir, "*.db", SearchOption.AllDirectories).ToList();

            //remove the local directory
            for (int i = 0; i < DatabaseFiles.Count; i++)
                DatabaseFiles[i] = DatabaseFiles[i].Replace(dir + Path.DirectorySeparatorChar, "");

            //hold client acknowledge
            byte[] ack = new byte[1];
            // send the number of files to be sent
            byte[] dataLength = BitConverter.GetBytes(DatabaseFiles.Count);
            stream.Write(dataLength, 0, 4);
            stream.Flush();
            
            foreach(string s in DatabaseFiles)
            {
                // send the length of the path
                dataLength = BitConverter.GetBytes(s.Length);
                stream.Write(dataLength, 0, 4);
                stream.Flush();

                // read the file and send the length;
                byte[] FileStream = File.ReadAllBytes(Path.Combine(dir, s));
                dataLength = BitConverter.GetBytes(FileStream.Length);
                stream.Write(dataLength, 0, 4);
                stream.Flush();

                // then send the path
                Console.WriteLine("Sending file: " + s);
                StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);
                writer.Write(s);
                writer.Flush();

                //wait for acknowledgement from client
                stream.Read(ack, 0, 1);

                // next, send the file data
                stream.Write(FileStream, 0, FileStream.Length);
                stream.Flush();

                //wait for acknowledgement from client
                stream.Read(ack, 0, 1);
            }
        }

        private void OutputEventHandler(object SendingProcess, DataReceivedEventArgs DataArgs)
        {
            if (DataArgs.Data == null)
                return;

            try
            {
                NetworkStream stream = (NetworkStream)SendingProcess;
                StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);
                stream.Write(BitConverter.GetBytes(DataArgs.Data.Length), 0, 4);
                writer.Write(DataArgs.Data);
                writer.Flush();
            }
            catch (Exception e)
            {
                if (e is SocketException || e is IOException)
                {
                    Console.WriteLine("connection to client lost");
                    return;
                }
                throw;
            }
        }
    }
}