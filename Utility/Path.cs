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
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetAbsolutePath(string path, string SimPath)
        {
            //try to find a path relative to the ApsimX\bin directory
            string NewPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                        .Substring(0, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 3), path);
            if (File.Exists(NewPath))
                return NewPath;

            //try to find a path relative to the .apsimx directory
            NewPath = Path.Combine(Path.GetDirectoryName(SimPath), path);
            if (File.Exists(NewPath))
                return NewPath;
            else
                return string.Empty;
        }

        public static string GetAbsolutePath(string path)
        {
            return GetAbsolutePath(path, string.Empty);
        }

    }
}
