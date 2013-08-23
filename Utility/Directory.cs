using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Utility
{
    class Directory
    {
        /// <summary>
        /// Ensure the specified filename is unique (by appending a number). 
        /// Returns the updated filename to caller. 
        /// </summary>
        public static string EnsureFileNameIsUnique(string FileName)
        {
            string BaseName = System.IO.Path.GetFileNameWithoutExtension(FileName);
            int Number = 1;
            while (File.Exists(FileName))
            {
                FileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FileName), BaseName + Number.ToString() + System.IO.Path.GetExtension(FileName));
                Number++;
            }
            if (File.Exists(FileName))
                throw new Exception("Cannot find a unique filename for file: " + BaseName);

            return FileName;
        }

        /// <summary>
        /// Delete files that match the specified filespec (e.g. *.out). If Recurse is true
        /// then it will look for matching files to delete in all sub directories.
        /// </summary>
        public static void DeleteFiles(string FileSpec, bool Recurse)
        {
            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(FileSpec)))
            {
                foreach (string FileName in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(FileSpec),
                                                               System.IO.Path.GetFileName(FileSpec)))
                    File.Delete(FileName);
                if (Recurse)
                {
                    foreach (string SubDirectory in System.IO.Directory.GetDirectories(System.IO.Path.GetDirectoryName(FileSpec)))
                        DeleteFiles(System.IO.Path.Combine(SubDirectory, System.IO.Path.GetFileName(FileSpec)), true);
                }
            }
        }

        /// <summary>
        /// Return a list of files that match the specified filespec (e.g. *.out). If Recurse is true
        /// then it will look for matching files in all sub directories. If SearchHiddenFolders is
        /// true then it will look in hidden folders as well.
        /// </summary>
        public static void FindFiles(string DirectoryName, string FileSpec, ref List<string> FileNames,
                                     bool Recurse = false, bool SearchHiddenFolders = false)
        {
            foreach (string FileName in System.IO.Directory.GetFiles(DirectoryName, FileSpec))
                FileNames.Add(FileName);
            if (Recurse)
                foreach (string ChildDirectoryName in System.IO.Directory.GetDirectories(DirectoryName))
                    if (SearchHiddenFolders || (File.GetAttributes(ChildDirectoryName) & FileAttributes.Hidden) != FileAttributes.Hidden)
                        FindFiles(ChildDirectoryName, FileSpec, ref FileNames, Recurse, SearchHiddenFolders);
        }

        /// <summary>
        /// Find the specified file (using the environment PATH variable) and return its full path.
        /// </summary>
        public static string FindFileOnPath(string FileName)
        {
            string PathVariable = Environment.GetEnvironmentVariable("PATH");
            if (PathVariable == null)
                throw new Exception("Cannot find PATH environment variable");
            string[] Paths;
            string PathSeparator;

            if (System.IO.Path.VolumeSeparatorChar == '/')
                PathSeparator = ":";
            else
                PathSeparator = ";";

            Paths = PathVariable.Split(PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (string DirectoryName in Paths)
            {
                string FullPath = System.IO.Path.Combine(DirectoryName, FileName);
                if (File.Exists(FullPath))
                    return FullPath;
            }
            return "";
        }

    }
}
