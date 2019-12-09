// -----------------------------------------------------------------------
// <copyright file="WebUtilities.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Xml.Serialization;

    /// <summary>
    /// A class containing some web utilities
    /// </summary>
    public class WebUtilities
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
        public static string SocketSend(string serverName, int port, string data)
        {
            string Response = null;
            TcpClient Server = null;
            try
            {
                Server = new TcpClient(serverName, Convert.ToInt32(port, CultureInfo.InvariantCulture));
                Byte[] bData = System.Text.Encoding.ASCII.GetBytes(data);
                Server.GetStream().Write(bData, 0, bData.Length);

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

        /// <summary>Call REST web service.</summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="url">The URL of the REST service.</param>
        /// <returns>The return data</returns>
        public static T CallRESTService<T>(string url)
        {
            WebRequest wrGETURL;
            wrGETURL = WebRequest.Create(url);
            wrGETURL.Method = "GET";
            wrGETURL.ContentType = @"application/xml; charset=utf-8";
            wrGETURL.ContentLength = 0;
            using (HttpWebResponse webresponse = wrGETURL.GetResponse() as HttpWebResponse)
            {
                Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
                // read response stream from response object
                using (StreamReader loResponseStream = new StreamReader(webresponse.GetResponseStream(), enc))
                {
                    string st = loResponseStream.ReadToEnd();
                    if (typeof(T).Name == "Object")
                        return default(T);

                    XmlSerializer serializer = new XmlSerializer(typeof(T));

                    //ResponseData responseData;
                    return (T)serializer.Deserialize(new XmlUtilities.NamespaceIgnorantXmlTextReader(new StringReader(st)));
                }
            }
        }


        /// <summary>
        /// Calls a url and returns the web response in a memory stream
        /// </summary>
        /// <param name="url">The url to call</param>
        /// <returns>The data stream</returns>
        public static MemoryStream ExtractDataFromURL(string url)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            MemoryStream stream = new MemoryStream();
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                response = (HttpWebResponse)request.GetResponse();
                Stream streamResponse = response.GetResponseStream();

                // Reads 1024 characters at a time.    
                byte[] read = new byte[1024];
                int count = streamResponse.Read(read, 0, 1024);
                while (count > 0)
                {
                    // Dumps the 1024 characters into our memory stream.
                    stream.Write(read, 0, count);
                    count = streamResponse.Read(read, 0, 1024);
                }
                return stream;
            }
            catch (Exception)
            {
                throw new Exception("Cannot get data from " + url);
            }
            finally
            {
                // Releases the resources of the response.
                if (response != null)
                    response.Close();
            }
        }

    }
}
