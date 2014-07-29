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
 *          *** EXPERIMENTAL ***
 *            NOT FOR RELEASE
 *  AUTHOR: Justin Fainges
 *  TCP Server adapted from http://tech.pro/tutorial/704/csharp-tutorial-simple-threaded-tcp-server
 *  
 * http://www.codeproject.com/Articles/1505/Create-your-own-Web-Server-using-C
 * http://tech.pro/tutorial/704/csharp-tutorial-simple-threaded-tcp-server
 ***************************************************/

namespace ApServer
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private static string dataDir = @"c:\temp\ApServer";

        public static void Main(string[] args)
        {
            Server server = new Server();
        }

        public Server()
        {
            Console.WriteLine("Server started.");

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

            // the acknowledge byte
            byte[] ack = { 1 };

            byte[] data = new byte[4096];
            byte[] nameLength = new byte[4]; //hold the length of the next incoming file name
            byte[] dataLength = new byte[4];
            byte[] fileListLength = new byte[4];
            int bytesRead;
            string fileName;
            MemoryStream ms;

            bytesRead = 0;

            try
            {
                //get the length of the file name
                bytesRead = clientStream.Read(fileListLength, 0, 4);
                Console.WriteLine("Connection accepted from " + tcpClient.Client.RemoteEndPoint);
                for (int i = 0; i < BitConverter.ToInt32(fileListLength, 0); i++)
                {
                    ms = new MemoryStream();
                    //get the length of the file name
                    bytesRead = clientStream.Read(nameLength, 0, 4);
                    if (bytesRead == 0)
                        break;

                    //get the length of the file
                    bytesRead = clientStream.Read(dataLength, 0, 4);
                    if (bytesRead == 0)
                        break;

                    // next item to arrive is the .apsimx file name
                    byte[] nameData = new byte[BitConverter.ToInt32(nameLength, 0)];
                    bytesRead = clientStream.Read(nameData, 0, nameData.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Could not read file name.");
                        break;
                    }
                    fileName = Encoding.ASCII.GetString(nameData);

                    // send an acknowledgement
                    clientStream.Write(ack, 0, 1);

                    // then the .apsimx file itself.
                    int totalBytes = 0;
                    while (totalBytes < BitConverter.ToInt32(dataLength, 0))
                    {
                        bytesRead = clientStream.Read(data, 0, 4096);
                        ms.Write(data, 0, bytesRead);
                        totalBytes += bytesRead;
                    }

                    File.WriteAllBytes(Path.Combine(dataDir, fileName), Convert2.ToByteArray(ms));

                    Console.WriteLine(Path.Combine(dataDir, fileName) + " successfully written.");

                    // send an acknowledgement
                    clientStream.Write(ack, 0, 1);
                }
            }
            catch
            {
                //a socket error has occured
                //   break;
            }

            //Do apsim run


            //run the simulations
            RunSimulations("", clientStream);

            tcpClient.Close();
        }

        private void RunSimulations(string dir, NetworkStream stream)
        {
            // First, remove all paths from file names
            foreach (string s in Directory.GetFiles(Path.Combine(dataDir, dir), "*.apsimx"))
            {
                XmlDocument doc = new XmlDocument();
                XmlNode root;
                XmlNodeList nodes;
                char SplitChar = '\\';

                doc.Load(s);
                root = doc.DocumentElement;

                nodes = root.SelectNodes("//WeatherFile/FileName");

                foreach (XmlNode node in nodes)
                {
                    node.InnerText = Path.GetFileName(node.InnerText);
                }
                doc.Save(s);
            }

            Process process = new Process();
            process.StartInfo.FileName = "model.exe";
            process.StartInfo.Arguments = Path.Combine(dataDir, dir, "*.apsimx");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler((sender, args) => OutputEventHandler(stream, args));
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            stream.Write(BitConverter.GetBytes(Int32.MaxValue), 0, 4);

            // Send completed runs back to user.
            
            Console.WriteLine("Run complete.");
        }

        /// <summary>
        /// Send completed runs back to the user.
        /// </summary>
        /// <param name="dir">The users remote directory.</param>
        /// <param name="stream">The users connection.</param>
        private void SendCompletedData(string dir, NetworkStream stream)
        {
            List<string> DatabaseFiles = Directory.GetFiles(Path.Combine(dataDir, dir), "*.db").ToList();
            //hold client acknowledge
            byte[] ack = new byte[1];
            // send the number of files to be sent
            byte[] dataLength = BitConverter.GetBytes(DatabaseFiles.Count);
            stream.Write(dataLength, 0, 4);
            stream.Flush();
            
            foreach(string s in DatabaseFiles)
            {
                // send the length of the file name
                dataLength = BitConverter.GetBytes(Path.GetFileName(s).Length);
                stream.Write(dataLength, 0, 4);
                stream.Flush();

                // read the file and send the length;
                byte[] FileStream = File.ReadAllBytes(s);
                dataLength = BitConverter.GetBytes(FileStream.Length);
                stream.Write(dataLength, 0, 4);
                stream.Flush();

                // then send the file name
                Console.WriteLine("Sending file name: " + s);
                StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);
                writer.Write(Path.GetFileName(s));
                writer.Flush();

                //wait for acknowledgement from client
                stream.Read(ack, 0, 1);

                // next, send the file data
                Console.WriteLine("sending file data");
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

            NetworkStream stream = (NetworkStream)SendingProcess;
            StreamWriter writer = new StreamWriter(stream, Encoding.ASCII);
            stream.Write(BitConverter.GetBytes(DataArgs.Data.Length), 0, 4);
            writer.Write(DataArgs.Data);
            writer.Flush();
        }
    }

    public static class Convert2
    {
        public static byte[] ToByteArray(Stream stream)
        {
            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            for (int totalBytesCopied = 0; totalBytesCopied < stream.Length; )
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
            return buffer;
        }
    }
}