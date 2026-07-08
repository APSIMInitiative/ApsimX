using System.Text.Json.Serialization;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>

/// </summary>
public class APSIMFilePath
{
    private string _relativeFilePath = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonIgnore]
    public string StartDirectory { get; private set; } = "";

    /// <summary>
    /// 
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
    /// 
    /// </summary>
    [JsonIgnore]
    public string AbsoluteFilePath 
    { 
        get 
        {
            return PathUtilities.GetAbsolutePath(_relativeFilePath, StartDirectory);
        }
    }

    public void SetStartDirectory(string directory)
    {
        StartDirectory = directory;
    }
}