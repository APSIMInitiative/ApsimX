
namespace APSIM.Shared.Documentation;

/// <summary>
/// Tag for handling embedded videos.
/// </summary>
public class Video: ITag
{
    /// <summary>
    /// The URL where the video is hosted.
    /// </summary>
    public string Source {get;set;}

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="source"></param>
    public Video(string source)
    {
        Source = source;
    }
}