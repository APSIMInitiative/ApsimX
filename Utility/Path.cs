using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utility
{
    public class PathUtils
    {

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

            return string.Empty;
        }

        private static string ReducePath(string path)
        {
            return path.Substring(path.IndexOf(Path.DirectorySeparatorChar) + 1, path.Length - path.IndexOf(Path.DirectorySeparatorChar)  - 1);
        }
    }
}
