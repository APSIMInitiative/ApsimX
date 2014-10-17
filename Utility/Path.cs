// -----------------------------------------------------------------------
// <copyright file="Path.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// A collection of path utilities.
    /// </summary>
    public class PathUtils
    {
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
            // Remove any %root% macro.
            string rootDirectory = System.IO.Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
            path = path.Replace("%root%", rootDirectory);
            
            // Make sure we have a relative directory 
            string relativeDirectory = Path.GetDirectoryName(relativePath);
            if (relativeDirectory != null)
            {
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(relativeDirectory, path);
            }
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
            if (System.String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            // Make sure we have a relative directory 
            string relativeDirectory = Path.GetDirectoryName(relativePath);
            if (relativeDirectory != null)
            {
                // Try putting in a %root%.
                string rootDirectory = System.IO.Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;
                path = path.Replace(rootDirectory, "%root%");

                // Try getting rid of the relative directory.
                path = path.Replace(relativeDirectory + Path.DirectorySeparatorChar, "");  // the relative path should not have a preceding \
            }

            return path;
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
    }
}
