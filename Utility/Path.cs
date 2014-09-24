// -----------------------------------------------------------------------
// <copyright file="Path.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.IO;

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
        /// Try to find an absolute path from a relative one
        /// </summary>
        /// <param name="path">The relative path to find an abolsute for.</param>
        /// <param name="SimPath">The file system path of a .apsimx file.</param>
        /// <returns></returns>
        public static string GetAbsolutePath(string path, string SimPath)
        {
            if (Path.GetDirectoryName(SimPath) == null)
                return path;
            else
            {
                //try to find a path relative to the .apsimx directory
                string NewPath = Path.Combine(Path.GetDirectoryName(SimPath), path);
                if (File.Exists(NewPath))
                    return Path.GetFullPath(NewPath); //use this to strip any relative path leftovers.

                //try to remove any overlapping path data
                while (path.IndexOf(Path.DirectorySeparatorChar) != -1)
                {
                    path = ReducePath(path);
                    NewPath = Path.Combine(Path.GetDirectoryName(SimPath), path);

                    if (File.Exists(NewPath))
                        return NewPath;
                }

                return path;
            }
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string toPath, string fromPath)
        {
            if (System.String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (System.String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = null;
            Uri toUri = null;

            //catch if path is already relative
            try
            {
                fromUri = new Uri(fromPath);
                toUri = new Uri(toPath);
            }
            catch (Exception)
            {
                return toPath;
            }

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
        
        /// <summary>
        /// Try and reduce the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string ReducePath(string path)
        {
            return path.Substring(path.IndexOf(Path.DirectorySeparatorChar) + 1, path.Length - path.IndexOf(Path.DirectorySeparatorChar)  - 1);
        }
    }
}
