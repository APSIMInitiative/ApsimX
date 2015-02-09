
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Utility
    {
    public class FTPClient
        {

        public static void Upload(string FullFileName, string FTPFullFileName, string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Method to upload the specified FullFileName to the specified 
            // FTPDirectory (e.g. ftp://www.apsim.info/apsim/temp)
            // ------------------------------------------------------------------
            FileInfo File = new FileInfo(FullFileName);

            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPFullFileName);
            FTP.Credentials = new NetworkCredential(UserName, Password);
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

        public static void Download(string FTPFullFileName, string DestFullFileName,
                                    string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Download the specified FTPFullFileName 
            // (e.g. ftp://www.apsim.info/apsim/temp/temp.xml) to the 
            // DestFileName (full path).
            // ------------------------------------------------------------------

            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPFullFileName);
            FTP.Method = WebRequestMethods.Ftp.DownloadFile;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(UserName, Password);

            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();
            int BufferSize = 2048;
            byte[] Buffer = new byte[BufferSize];

            FileStream OutputStream = new FileStream(DestFullFileName, FileMode.Create);
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

        public static void Delete(string FTPFullFileName, string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Delete the specified FTPFullFileName 
            // (e.g. ftp://www.apsim.info/apsim/temp/temp.xml)
            // ------------------------------------------------------------------

            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPFullFileName);

            FTP.Credentials = new NetworkCredential(UserName, Password);
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

        public static string[] DirectoryListing(string FTPDirectory, bool Detailed, string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Return a directory listing to caller for the specified 
            // FTPDirectory (e.g. ftp://www.apsim.info/apsim/temp)
            // If detailed = true then a detailed listing will be returned.
            // ------------------------------------------------------------------
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPDirectory);
            FTP.Credentials = new NetworkCredential(UserName, Password);
            FTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            if (!Detailed)
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

        public static long FileSize(string FTPFullFileName, string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Get the size of the specified FTPFullFileName 
            // (e.g. ftp://www.apsim.info/apsim/temp/test.xml)
            // ------------------------------------------------------------------

            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPFullFileName);
            FTP.Method = WebRequestMethods.Ftp.GetFileSize;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(UserName, Password);
            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();
            long FileSize = Response.ContentLength;
            
            FtpStream.Close();
            Response.Close();
            return FileSize;
            }

        public static void Rename(string FTPFullFileName, string NewFileName, string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Rename the specified FTPFileName 
            // (ftp://www.apsim.info/apsim/temp/test.xml) to the specified 
            // NewFileName (no path)
            // ------------------------------------------------------------------
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPFullFileName);
            FTP.Method = WebRequestMethods.Ftp.Rename;
            FTP.RenameTo = NewFileName;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(UserName, Password);
            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();
            FtpStream.Close();
            Response.Close();
            }

        public static void MakeDirectory(string FTPDirectory, string UserName, string Password)
            {
            // ------------------------------------------------------------------
            // Make the specified FTPDirectory (ftp://www.apsim.info/apsim/temp)
            // ------------------------------------------------------------------
            FtpWebRequest FTP = (FtpWebRequest)FtpWebRequest.Create(FTPDirectory);
            FTP.Method = WebRequestMethods.Ftp.MakeDirectory;
            FTP.UseBinary = true;
            FTP.Credentials = new NetworkCredential(UserName, Password);
            FtpWebResponse Response = (FtpWebResponse)FTP.GetResponse();
            Stream FtpStream = Response.GetResponseStream();

            FtpStream.Close();
            Response.Close();
            }
     
    }
}