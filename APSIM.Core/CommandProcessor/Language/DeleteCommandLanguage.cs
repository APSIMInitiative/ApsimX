namespace APSIM.Core;

internal partial class DeleteCommand: IModelCommand
{
    private const string KEYWORD_DELETE = "delete ";
    private const string PATTERN_DELETE = $@"{KEYWORD_DELETE}(?<all>all )*(?<model>{CommandLanguage.PATTERN_MODEL_PATH})";

    /// <summary>
    /// Create a delete command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <remarks>
    /// delete [Zone].Report
    /// delete [Report]
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        CommandSegment[] segments = CommandLanguage.ReadCommand(command, [KEYWORD_DELETE], [PATTERN_DELETE]);
        string model = CommandSegment.GetValue(segments, "model");
        bool usesAll = CommandSegment.ContainsKey(segments, "all");
        if (string.IsNullOrEmpty(model))
            throw new Exception($"Invalid command: {command}");
        return new DeleteCommand(model, usesAll);
    }

    /// <summary>
    /// Convert an DeleteCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        if (_multiple)
            return $"{KEYWORD_DELETE}all {_modelName}";
        else
            return $"{KEYWORD_DELETE}{_modelName}";
    } 
}