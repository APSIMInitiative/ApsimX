using System;
using System.IO;

namespace Models.Utilities.Extensions;

/// <summary>
/// Class for extensions to FileInfo class.
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Determines if a file is locked or not.
    /// </summary>
    /// <param name="f">FileInfo object</param>
    /// <returns></returns>
    public static bool IsLocked(this FileInfo f)
    {
        try
        {
            string fpath = f.FullName;
            FileStream fs = File.OpenWrite(fpath);
            fs.Close();
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}