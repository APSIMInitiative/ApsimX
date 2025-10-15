namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// A collection of path utilities.
    /// </summary>
    public class PathUtilities
    {
        /// <summary>
        /// Return the path to the ApsimX directory.
        /// </summary>
        /// <remarks>
        /// When using the installed version, this is usually the
        /// parent of the bin directory, but we also account for custom
        /// builds, in which the assemblies may be located somewhere
        /// like ApsimX/bin/Debug/net472/.
        /// </remarks>
        public static string GetApsimXDirectory()
        {
            return GetAbsolutePath("%root%", null);
        }

        /// <summary>
        /// Convert the specified URL to a path.
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static string ConvertURLToPath(string Url)
        {
            Uri uri = new Uri(Url);
            return uri.LocalPath;
        }

        /// <summary>
        /// Takes a file path, and attempts to assure it's in the
        /// right form for the current OS. For now, it just looks
        /// at the path separators.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string OSFilePath(string path)
        {
            if (Path.DirectorySeparatorChar != '\\')
                return path.Replace('\\', Path.DirectorySeparatorChar);
            if (Path.DirectorySeparatorChar != '/')
                return path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }

        /// <summary>
        /// Check for valid characters allowed in component names
        /// </summary>
        /// <param name="s">Test string</param>
        /// <returns>True if an invalid character is found</returns>
        public static bool CheckForInvalidChars(string s)
        {
            if ((s.Contains(",")) || (s.Contains(".")) || (s.Contains("/")) || (s.Contains("\\")) || (s.Contains("<")) || (s.Contains(">")) || (s.Contains("\"")) || (s.Contains("\'")) || (s.Contains("`")) || (s.Contains(":")) || (s.Contains("?")) || (s.Contains("|")) || (s.Contains("*")) || (s.Contains("&")) || (s.Contains("=")) || (s.Contains("!")))
            {
                return true;
            }
            else { return false; }


        }

        /// <summary>
        /// Try to find an absolute path from a relative one.
        /// The %root% macro will be expanded if it exists. This macro
        /// represents the parent of the directory containing the executing assembly.
        /// </summary>
        /// <param name="path">The relative path to find an abolsute for</param>
        /// <param name="relativePath">The relative path to use</param>
        /// <returns>The absolute path</returns>
        public static string GetAbsolutePath(string path, string relativePath)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Remove any %root% macro (even if relative path is null).
            string bin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo directory = new DirectoryInfo(bin).Parent;
            while (directory.Name == "Debug" || directory.Name == "Release" || directory.Name == "bin")
                directory = directory.Parent;
            string apsimxDirectory = directory.FullName;
            if (apsimxDirectory == "/")
                apsimxDirectory = "/wd/"; // Special condition for running on azure compute nodes. Required for new build system.
            path = path.Replace("%root%", apsimxDirectory);

            if (string.IsNullOrEmpty(relativePath))
                return ConvertSlashes(path);

            // Make sure we have a relative directory
            string relativeDirectory;
            if (Directory.Exists(relativePath))
                relativeDirectory = relativePath;
            else
                relativeDirectory = Path.GetDirectoryName(relativePath);
            if (relativeDirectory != null && !Path.IsPathRooted(path))
                    path = Path.Combine(relativeDirectory, path);

            return ConvertSlashes(Path.GetFullPath(path));
        }

        /// <summary>
        /// Creates a relative path from the given path.
        /// </summary>
        /// <param name="path">The path to make relative</param>
        /// <param name="relativePath">The relative path to use</param>
        /// <returns>The relative path</returns>
        public static string GetRelativePath(string path, string relativePath)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(relativePath))
                return path;

            //Convert both paths to using linux style slashes
            string correctedPath = ConvertSlashes(path);
            string correctedRelativePath = ConvertSlashes(relativePath);

            //if path has backtracking in path, convert to absolute
            if (correctedPath.Contains("../"))
                correctedPath = GetAbsolutePath(correctedPath, correctedRelativePath);

            //Get the program path to replace %root%
            string programPath = GetAbsolutePath("%root%", null);
            correctedPath = correctedPath.Replace("%root%", programPath);

            //check if our path is the same as the absolute path, if it is, see if it contains the relative path to shorten it
            string absolutePath = GetAbsolutePath(correctedPath, correctedRelativePath);
            if (absolutePath == correctedPath)
            {
                string relativeDirectory = ConvertSlashes(Path.GetDirectoryName(correctedRelativePath));
                if (!string.IsNullOrEmpty(relativeDirectory))
                {
                    if (!relativeDirectory.EndsWith("/"))
                        relativeDirectory = relativeDirectory + "/";
                    correctedPath = correctedPath.Replace(relativeDirectory, "");
                }
            }

            //check now if the path matches the absolute path. if it is, see if we can shorten it with %root%
            if (absolutePath == correctedPath)
                correctedPath = correctedPath.Replace(programPath, "%root%");

            //We should now have the best version of this path, in order of relative, %root% and absolute
            return correctedPath;
        }

        /// <summary>
        /// Creates a relative path from the given path and uses %root% replacement if path is under the %root%/Examples folder.
        /// </summary>
        /// <param name="path">The path to make relative</param>
        /// <param name="relativePath">The relative path to use</param>
        /// <returns>The relative path</returns>
        public static string GetRelativePathAndRootExamples(string path, string relativePath)
        {
            string examplesPath = GetAbsolutePath("%root%/Examples/", null);
            if (path.Contains(examplesPath))
                return ConvertSlashes("%root%/Examples/" + path.Remove(path.IndexOf(examplesPath), examplesPath.Length));
            else
                return GetRelativePath(path, relativePath);
        }

        /// <summary>
        /// Try and reduce the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string ReducePath(string path)
        {
            return path.Substring(path.IndexOf(Path.DirectorySeparatorChar) + 1, path.Length - path.IndexOf(Path.DirectorySeparatorChar) - 1);
        }

        /// <summary>
        /// Convert all slashes to the correct directory separator character.
        /// </summary>
        private static string ConvertSlashes(string path)
        {
            return path.Replace("\\", "/");
        }


        /// <summary>
        /// Get all the absolute paths of files in a directory and subdirectories with the .apsimx extension.
        /// </summary>
        /// <param name="directory">The directory to search.</param>
        /// <returns>A list of absolute paths to files with the .apsimx extension.</returns>
        public static List<string> GetAllApsimXFilePaths(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"The directory '{directory}' does not exist.");

            List<string> apsimxFiles = new List<string>();
            foreach (string file in Directory.EnumerateFiles(directory, "*.apsimx", SearchOption.AllDirectories))
            {
                apsimxFiles.Add(Path.GetFullPath(file));
            }

            return apsimxFiles;
        }

        /// <summary>
        /// Compares two filepaths and determines if they are the same. Can compare relative against full path.
        /// </summary>
        /// <returns>True if the file paths match</returns>
        public static bool ComparePaths(string path1, string path2, string apsimxFilepath)
        {
            string fullpath1 = PathUtilities.GetAbsolutePath(path1, apsimxFilepath);
            string fullpath2 = PathUtilities.GetAbsolutePath(path2, apsimxFilepath);

            return fullpath1 == fullpath2;
        }

    }

}
