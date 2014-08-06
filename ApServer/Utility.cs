using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ApServer
{
    public static class Utility
    {
        public static byte[] ToByteArray(Stream stream)
        {
            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            for (int totalBytesCopied = 0; totalBytesCopied < stream.Length; )
                totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
            return buffer;
        }

        public static void SendNetworkFile(string fileName, NetworkStream ns)
        {
            byte[] dataLength;
            byte[] ack = new byte[1];
            string RemoteFileName = fileName.Substring(fileName.IndexOf(Path.DirectorySeparatorChar) + 1, fileName.Length - fileName.IndexOf(Path.DirectorySeparatorChar) - 1);
            // send the length of the path (with drive designator removed)
            dataLength = BitConverter.GetBytes(RemoteFileName.Length);
            ns.Write(dataLength, 0, 4);
            ns.Flush();

            // read the file and send the length;
            byte[] FileStream = File.ReadAllBytes(fileName);
            dataLength = BitConverter.GetBytes(FileStream.Length);
            ns.Write(dataLength, 0, 4);
            ns.Flush();

            // then send the path
            Console.WriteLine("Sending file: " + fileName);
            StreamWriter writer = new StreamWriter(ns, Encoding.ASCII);
            writer.Write(RemoteFileName);
            writer.Flush();

            //wait for acknowledgement from server
            ns.Read(ack, 0, 1);

            // next, send the file data
            ns.Write(FileStream, 0, FileStream.Length);
            ns.Flush();

            //wait for acknowledgement from server
            ns.Read(ack, 0, 1);
        }

        public static void ReceiveNetworkFiles(NetworkStream clientStream, TcpClient tcpClient, string userDir)
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

            //get the number of files to receive
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

                // next item to arrive is the file name
                byte[] nameData = new byte[BitConverter.ToInt32(nameLength, 0)];
                bytesRead = clientStream.Read(nameData, 0, nameData.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Could not read file name.");
                    break;
                }
                fileName = Path.Combine(userDir, Encoding.ASCII.GetString(nameData));

                //remove any overlapping path data
                List<string> components = new List<string>(userDir.Split(Path.DirectorySeparatorChar));
                foreach (string s in fileName.Split(Path.DirectorySeparatorChar))
                    if (!components.Contains(s))
                        components.Add(s);

                fileName = Path.Combine(components.ToArray());
                // on windows, add the dir seperator after the volume seperator
                fileName = fileName.Replace(":", ":\\");

                // send an acknowledgement
                clientStream.Write(ack, 0, 1);

                // then the file itself.
                int totalBytes = 0;
                while (totalBytes < BitConverter.ToInt32(dataLength, 0))
                {
                    bytesRead = clientStream.Read(data, 0, 4096);
                    ms.Write(data, 0, bytesRead);
                    totalBytes += bytesRead;
                }

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                File.WriteAllBytes(fileName, Utility.ToByteArray(ms));

                Console.WriteLine(fileName + " successfully written.");

                // send an acknowledgement
                clientStream.Write(ack, 0, 1);
            }
        }
    }
}
