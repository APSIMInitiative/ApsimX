namespace APSIM.Shared.Utilities
{
    using System;
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

            // Convert slashes.
            path = ConvertSlashes(path);

            return Path.GetFullPath(path);
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

            // Make sure we have a relative directory 
            string relativeDirectory = Path.GetDirectoryName(relativePath);
            if (relativeDirectory != null)
            {
                // Try getting rid of the relative directory.
                path = path.Replace(relativeDirectory + Path.DirectorySeparatorChar, "");  // the relative path should not have a preceding \
            }

            return ConvertSlashes(path);
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
            if (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32Windows)
                return path.Replace("/", @"\");

            return path.Replace(@"\", "/");
        }
    }
}
