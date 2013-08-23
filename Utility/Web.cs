using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Utility
{
    class Web
    {
        /// <summary>
        ///  Upload a file via ftp
        /// </summary>
        /// <param name="localFileName">Name of the file to be uploaded</param>
        /// <param name="username">remote username</param>
        /// <param name="password">remote password</param>
        /// <param name="hostname">remote hostname</param>
        /// <param name="remoteFileName">Full path and name of where the file goes</param>
        /// <returns></returns>
        public static bool UploadFTP(string localFileName, string username, string password, string hostname, string remoteFileName)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + hostname + remoteFileName);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential();

            // Copy the contents of the file to the request stream.
            StreamReader sourceStream = new StreamReader(localFileName);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            string retVal = response.StatusDescription;
            response.Close();

            return retVal != "200";
        }

        /// <summary>
        /// Send a string to the specified socket server. Returns the response string. Will throw
        /// if cannot connect.
        /// </summary>
        public static string SocketSend(string ServerName, int Port, string Data)
        {
            string Response = null;
            TcpClient Server = null;
            try
            {
                Server = new TcpClient(ServerName, Convert.ToInt32(Port));
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(Data);
                Server.GetStream().Write(data, 0, data.Length);

                Byte[] bytes = new Byte[8192];

                // Wait for data to become available.
                while (!Server.GetStream().DataAvailable)
                    Thread.Sleep(10);

                // Loop to receive all the data sent by the client.
                while (Server.GetStream().DataAvailable)
                {
                    int NumBytesRead = Server.GetStream().Read(bytes, 0, bytes.Length);
                    Response += System.Text.Encoding.ASCII.GetString(bytes, 0, NumBytesRead);
                }
            }
            finally
            {
                if (Server != null) Server.Close();
            }
            return Response;
        }



    }
}
