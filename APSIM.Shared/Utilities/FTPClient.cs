namespace APSIM.Shared.Utilities
{
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// An FTP client
    /// </summary>
    public class FTPClient
        {

        /// <summary>
        /// Uploads the specified file.
        /// </summary>
        /// <param name="fullFileName">Full name of the file.</param>
        /// <param name="fTPFullFileName">Name of the FTP full file.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public static void Upload(string fullFileName, string fTPFullFileName, string userName, string password)
            {
            FileInfo File = new FileInfo(fullFileName);

            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPFullFileName);
            FTP.Credentials = new NetworkCredential(userName, password);
            FTP.KeepAlive = false;
            FTP.Method = WebRequestMethods.Ftp.UploadFile;
            FTP.UseBinary = true;
            FTP.ContentLength = File.Length;
            FTP.UsePassive = false;

            // The buffer size is set to 2kb
            int BuffLength = 2048;
            byte[] Buffer = new byte[BuffLength];
            int contentLen;

            // Opens a file stream (System.IO.FileStream) to read the file to be uploaded
            FileStream fs = File.OpenRead();
           
            // Stream to which the file to be upload is written
            Stream strm = FTP.GetRequestStream();
            
            // Read from the file stream 2kb at a time
            contentLen = fs.Read(Buffer, 0, BuffLength);

            // Till Stream content ends
            while (contentLen != 0)
                {
                // Write Content from the file stream to the FTP Upload Stream
                strm.Write(Buffer, 0, contentLen);
                contentLen = fs.Read(Buffer, 0, BuffLength);
                }

            // Close the file stream and the Request Stream
            strm.Close();
            fs.Close();
            }

        /// <summary>
        /// Downloads the specified file.
        /// </summary>
        /// <param name="fTPFullFileName">Name of the FTP full file.</param>
        /// <param name="destFullFileName">Name of the dest full file.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public static void Download(string fTPFullFileName, string destFullFileName,
                                    string userName, string password)
            {
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPFullFileName);
            FTP.Method = WebRequestMethods.Ftp.DownloadFile;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(userName, password);

            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();
            int BufferSize = 2048;
            byte[] Buffer = new byte[BufferSize];

            FileStream OutputStream = new FileStream(destFullFileName, FileMode.Create);
            int ReadCount = FtpStream.Read(Buffer, 0, BufferSize);
            while (ReadCount > 0)
                {
                OutputStream.Write(Buffer, 0, ReadCount);
                ReadCount = FtpStream.Read(Buffer, 0, BufferSize);
                }

            FtpStream.Close();
            OutputStream.Close();
            Response.Close();
            }

        /// <summary>
        /// Deletes the specified FTP file.
        /// </summary>
        /// <param name="fTPFullFileName">Name of the FTP full file.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public static void Delete(string fTPFullFileName, string userName, string password)
            {
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPFullFileName);

            FTP.Credentials = new NetworkCredential(userName, password);
            FTP.KeepAlive = false;
            FTP.Method = WebRequestMethods.Ftp.DeleteFile;

            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream Datastream = Response.GetResponseStream();
            StreamReader Reader = new StreamReader(Datastream);
            Reader.ReadToEnd();
            Reader.Close();
            Datastream.Close();
            Response.Close();
            }

        /// <summary>
        /// Gets a directory listing.
        /// </summary>
        /// <param name="fTPDirectory">The FTP directory.</param>
        /// <param name="detailed">if set to <c>true</c> [detailed].</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static string[] DirectoryListing(string fTPDirectory, bool detailed, string userName, string password)
            {
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPDirectory);
            FTP.Credentials = new NetworkCredential(userName, password);
            FTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            if (!detailed)
                FTP.Method = WebRequestMethods.Ftp.ListDirectory;

            WebResponse Response = FTP.GetResponse();
            StreamReader Reader = new StreamReader(Response.GetResponseStream());

            StringBuilder Result = new StringBuilder();
            string Line = Reader.ReadLine();
            while (Line != null)
                {
                Result.Append(Line);
                Result.Append("\n");
                Line = Reader.ReadLine();
                }
            
            Result.Remove(Result.ToString().LastIndexOf("\n"), 1);
            Reader.Close();
            Response.Close();
            return Result.ToString().Split('\n');
            }

        /// <summary>
        /// Gets the size of a file.
        /// </summary>
        /// <param name="fTPFullFileName">Name of the FTP full file.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static long FileSize(string fTPFullFileName, string userName, string password)
            {
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPFullFileName);
            FTP.Method = WebRequestMethods.Ftp.GetFileSize;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(userName, password);
            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();
            long FileSize = Response.ContentLength;
            
            FtpStream.Close();
            Response.Close();
            return FileSize;
            }

        /// <summary>
        /// Renames the specified FTP file.
        /// </summary>
        /// <param name="fTPFullFileName">Name of the FTP full file.</param>
        /// <param name="newFileName">New name of the file.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public static void Rename(string fTPFullFileName, string newFileName, string userName, string password)
            {
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPFullFileName);
            FTP.Method = WebRequestMethods.Ftp.Rename;
            FTP.RenameTo = newFileName;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(userName, password);
            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();
            FtpStream.Close();
            Response.Close();
            }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="fTPDirectory">The FTP directory.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public static void MakeDirectory(string fTPDirectory, string userName, string password)
            {
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(fTPDirectory);
            FTP.Method = WebRequestMethods.Ftp.MakeDirectory;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(userName, password);
            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();

            FtpStream.Close();
            Response.Close();
            }
     
    }
}