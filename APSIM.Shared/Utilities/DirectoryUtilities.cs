namespace APSIM.Shared.Utilities
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// A collection of directory utilities.
    /// </summary>
    public class DirectoryUtilities
    {
        /// <summary>
        /// Ensure the specified filename is unique (by appending a number). 
        /// Returns the updated filename to caller. 
        /// </summary>
        public static string EnsureFileNameIsUnique(string fileName)
        {
            string BaseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            int Number = 1;
            while (File.Exists(fileName))
            {
                fileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fileName), BaseName + Number.ToString() + System.IO.Path.GetExtension(fileName));
                Number++;
            }
            if (File.Exists(fileName))
                throw new Exception("Cannot find a unique filename for file: " + BaseName);

            return fileName;
        }

        /// <summary>
        /// Delete files that match the specified filespec (e.g. *.out). If Recurse is true
        /// then it will look for matching files to delete in all sub directories.
        /// </summary>
        public static void DeleteFiles(string fileSpec, bool recurse)
        {
            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(fileSpec)))
            {
                foreach (string FileName in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(fileSpec),
                                                               System.IO.Path.GetFileName(fileSpec)))
                    File.Delete(FileName);
                if (recurse)
                {
                    foreach (string SubDirectory in System.IO.Directory.GetDirectories(System.IO.Path.GetDirectoryName(fileSpec)))
                        DeleteFiles(System.IO.Path.Combine(SubDirectory, System.IO.Path.GetFileName(fileSpec)), true);
                }
            }
        }

        /// <summary>
        /// Return a list of files that match the specified filespec (e.g. *.out). If Recurse is true
        /// then it will look for matching files in all sub directories. If SearchHiddenFolders is
        /// true then it will look in hidden folders as well.
        /// </summary>
        /// <param name="fileSpec">
        /// File specification - e.g. "*.apsimx". If a path/directory name
        /// is ommitted, search will start from current working directory.
        /// </param>
        /// <param name="recurse">Search child directories?</param>
        public static string[] FindFiles(string fileSpec, bool recurse = false)
        {
            SearchOption searchType = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string fileName = Path.GetFileName(fileSpec);
            string path = Path.GetDirectoryName(fileSpec);
            if (string.IsNullOrEmpty(path))
                path = Directory.GetCurrentDirectory();

            return Directory.EnumerateFiles(path, fileName, searchType).ToArray();
        }

        /// <summary>
        /// Find the specified file (using the environment PATH variable) and return its full path.
        /// </summary>
        public static string FindFileOnPath(string fileName)
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
                string FullPath = System.IO.Path.Combine(DirectoryName, fileName);
                if (File.Exists(FullPath))
                    return FullPath;
            }
            return "";
        }

        /// <summary>
        /// Find the specified file in the specified directory structure. If not found
        /// in the specified directory it will recursively look under parent directories.
        /// </summary>
        /// <param name="fileName">The file name to look for</param>
        /// <param name="directory">The directory to search.</param>
        /// <returns></returns>
        public static string FindFileInDirectoryStructure(string fileName, string directory)
        {
            string path = directory;
            string[] files = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
            while (Path.GetDirectoryName(path) != null && files.Length == 0)
            {
                path = Path.GetFullPath(Path.Combine(path, ".."));
                files = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
            }
            if (files.Length >= 1)
                return files[0];
            else
                throw new Exception("Cannot find: " + fileName);
        }
    }
}
