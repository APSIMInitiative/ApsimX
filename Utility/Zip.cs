using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Utility
{
    public class Zip
    {
        /// <summary>
        /// Zip all the specified files into the specified ZipFileName.
        /// </summary>
        public static string ZipFiles(IEnumerable<string> FilesToZip, string ZipFileName, string Password)
        {
            if (!File.Exists(ZipFileName))
                File.Delete(ZipFileName);

            ZipOutputStream Zip = new ZipOutputStream(File.Create(ZipFileName));
            if (Password != "")
                Zip.Password = Password;
            try
            {
                Zip.SetLevel(5); // 0 - store only to 9 - means best compression
                foreach (string FileName in FilesToZip)
                {
                    FileStream fs = File.OpenRead(FileName);

                    byte[] Buffer = new byte[fs.Length];
                    fs.Read(Buffer, 0, Buffer.Length);
                    fs.Close();

                    ZipEntry Entry = new ZipEntry(Path.GetFileName(FileName));
                    Zip.PutNextEntry(Entry);
                    Zip.Write(Buffer, 0, Buffer.Length);
                }
                Zip.Finish();
                Zip.Close();
                return ZipFileName;
            }
            catch (System.Exception)
            {
                Zip.Finish();
                Zip.Close();
                File.Delete(ZipFileName);
                throw;
            }
        }
        /// <summary>
        /// Zip all the specified files into the specified ZipFileName.
        /// </summary>
        public static string ZipFilesWithDirectories(IEnumerable<string> FilesToZip, string ZipFileName, string Password)
        {
            if (File.Exists(ZipFileName))
                File.Delete(ZipFileName);

            ZipOutputStream Zip = new ZipOutputStream(File.Create(ZipFileName));
            if (Password != "")
                Zip.Password = Password;
            try
            {
                Zip.SetLevel(5); // 0 - store only to 9 - means best compression
                foreach (string FileName in FilesToZip)
                {
                    ZipEntry Entry = new ZipEntry(FileName);
                    Zip.PutNextEntry(Entry);
                    if (File.Exists(FileName))
                    {
                        FileStream fs = File.OpenRead(FileName);

                        byte[] Buffer = new byte[fs.Length];
                        fs.Read(Buffer, 0, Buffer.Length);
                        fs.Close();
                        Zip.Write(Buffer, 0, Buffer.Length);
                    }
                }
                Zip.Finish();
                Zip.Close();
                return ZipFileName;
            }
            catch (System.Exception)
            {
                Zip.Finish();
                Zip.Close();
                File.Delete(ZipFileName);
                throw;
            }
        }
        /// <summary>
        /// Unzips the specified zip file into the specified destination folder. Will use the 
        /// specified password. Returns a list of filenames that were created.
        /// </summary>
        public static string[] UnZipFiles(string ZipFile, string DestFolder, string Password)
        {
            StreamReader s = new StreamReader(ZipFile);
            string[] files = UnZipFiles(s.BaseStream, DestFolder, Password);
            s.Close();
            return files;
        }

        /// <summary>
        /// Unzips the specified zip file into the specified destination folder. Will use the 
        /// specified password. Returns a list of filenames that were created.
        /// </summary>
        public static string[] UnZipFiles(Stream s, string DestFolder, string Password)
        {
            List<string> FilesCreated = new List<string>();
            ZipInputStream Zip = new ZipInputStream(s);
            if (Password != "" && Password != null)
                Zip.Password = Password;
            ZipEntry Entry;
            while ((Entry = Zip.GetNextEntry()) != null)
            {
                // Convert either '/' or '\' to the local directory separator
                string DestFileName = DestFolder + Path.DirectorySeparatorChar +
                       Entry.Name.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

                // Make sure the destination folder exists.
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(DestFileName));

                BinaryWriter FileOut = new BinaryWriter(new FileStream(DestFileName, FileMode.Create));

                int size = 2048;
                byte[] data = new byte[2048];
                while (true)
                {
                    size = Zip.Read(data, 0, data.Length);
                    if (size > 0)
                        FileOut.Write(data, 0, size);
                    else
                        break;
                }
                FileOut.Close();
                FileOut = null;
                FilesCreated.Add(DestFileName);
            }
            Zip.Close();
            return FilesCreated.ToArray();
        }


        /// <summary>
        /// Unzips the specified zip to a memory stream. Returns the stream or null if not found.
        /// </summary>
        public static Stream UnZipFile(string ZipFile, string FileToExtract, string Password)
        {
            MemoryStream MemStream = null;

            List<string> FilesCreated = new List<string>();
            ZipInputStream Zip = new ZipInputStream(File.Open(ZipFile, FileMode.Open, FileAccess.Read));
            if (Password != "" && Password != null)
                Zip.Password = Password;
            ZipEntry Entry;
            while ((Entry = Zip.GetNextEntry()) != null)
            {
                if (FileToExtract == Entry.Name)
                {
                    MemStream = new MemoryStream();
                    BinaryWriter FileOut = new BinaryWriter(MemStream);

                    int size = 2048;
                    byte[] data = new byte[2048];
                    while (true)
                    {
                        size = Zip.Read(data, 0, data.Length);
                        if (size > 0)
                            FileOut.Write(data, 0, size);
                        else
                            break;
                    }
                    break;
                }
            }
            Zip.Close();
            return MemStream;
        }

        public static string[] FileNamesInZip(string ZipFile, string Password)
        {
            ZipInputStream Zip = new ZipInputStream(File.Open(ZipFile, FileMode.Open, FileAccess.Read));
            if (Password != "" && Password != null)
                Zip.Password = Password;
            List<string> FileNames = new List<string>();
            ZipEntry Entry;
            while ((Entry = Zip.GetNextEntry()) != null)
            {
                FileNames.Add(Entry.Name);
            }
            Zip.Close();
            return FileNames.ToArray();
        }
    }
}
