using System.Text.Json.Serialization;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// Class for handling filepaths within APSIM, handles storing the path so that 
/// it can consitantly be retrieved as either a relative or absolute path.
/// </summary>
public class APSIMFilePath
{
    private string _relativeFilePath = "";

    /// <summary>
    /// The start directory of the .apsimx file to know where to make the path 
    /// relative to.
    /// </summary>
    [JsonIgnore]
    public string StartDirectory { get; private set; } = "";

    /// <summary>
    /// The relative filepath based on the location of the file, and the 
    /// currently open .apsimx file.
    /// </summary>
    public string RelativeFilePath 
    { 
        get 
        {
            return _relativeFilePath;
        }
        set
        {
            if (string.IsNullOrEmpty(StartDirectory))
                _relativeFilePath = value;
            else
                _relativeFilePath = PathUtilities.GetRelativePath(value, StartDirectory);
        }
    }

    /// <summary>
    /// Absolute file path for the stored path, starts with drive letter.
    /// </summary>
    [JsonIgnore]
    public string AbsoluteFilePath 
    { 
        get 
        {
            return PathUtilities.GetAbsolutePath(_relativeFilePath, StartDirectory);
        }
    }

    /// <summary>
    /// Set the starting directory
    /// </summary>
    /// <param name="directory">Starting directory</param>
    public void SetStartDirectory(string directory)
    {
        StartDirectory = directory;
    }
}