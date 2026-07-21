namespace APSIM.Core;

internal partial class MoveCommand: IModelCommand
{
    //Keywords for the command, in order they can appear in the command
    private const string KEYWORD_MOVE = "move ";
    private const string KEYWORD_BEFORE = " before ";
    private const string KEYWORD_AFTER = " after ";

    //Regex patterns to read the text between keywords
    private const string PATTERN_REPLACE = $"{KEYWORD_MOVE}(?<source>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_POSITION = $"(?<position>{KEYWORD_BEFORE}|{KEYWORD_AFTER})(?<destination>{CommandLanguage.PATTERN_MODEL_PATH})";

    /// <summary>
    /// Create a Move command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// move [Soil] before SurfaceOrganicMatter
    /// move [Soil] after SurfaceOrganicMatter
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        CommandSegment[] segments = null;
        //If it contains the before keyword, try parsing with BEFORE
        if (command.Contains(KEYWORD_BEFORE))
        {
            string[] keywords = [KEYWORD_MOVE, KEYWORD_BEFORE];
            string[] patterns = [PATTERN_REPLACE, PATTERN_POSITION];
            segments = CommandLanguage.ReadComplexCommand(command, keywords, patterns);
        }
        //if it didn't contain BEFORE, or after parsing it didn't fit BEFORE, 
        //try again with AFTER
        if (segments == null || segments.Length < 3)
        {
            string[] keywords = [KEYWORD_MOVE, KEYWORD_AFTER];
            string[] patterns = [PATTERN_REPLACE, PATTERN_POSITION];
            segments = CommandLanguage.ReadComplexCommand(command, keywords, patterns);
        }

        string source = CommandSegment.GetValue(segments, "source");
        string position = CommandSegment.GetValue(segments, "position");
        string destination = CommandSegment.GetValue(segments, "destination");

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(position) || string.IsNullOrEmpty(destination))
            throw new Exception($"Invalid command: {command}");

        bool placeBefore = true;
        if (position == "after")
            placeBefore = false;

        return new MoveCommand(source, destination, placeBefore);
    }

    /// <summary>
    /// Convert a Move command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        string position = KEYWORD_BEFORE;
        if (!_placeBefore)
            position = KEYWORD_AFTER;
        return $"{KEYWORD_MOVE}{_fromPath}{position}{_toPath}";
    }

}