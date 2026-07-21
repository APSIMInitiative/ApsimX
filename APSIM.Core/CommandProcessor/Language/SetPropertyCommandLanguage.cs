namespace APSIM.Core;

public partial class SetPropertyCommand: IModelCommand
{
    private const string KEYWORD_FROM = " from ";
    private const string PATTERN_VALUE = $@"(?<model>{CommandLanguage.PATTERN_MODEL_PATH}) ?(?<operator>{CommandLanguage.PATTERN_OPERATOR}) ?(?<value>{CommandLanguage.PATTERN_VALUE})?";
    private const string PATTERN_FROM = $"{KEYWORD_FROM}(?<file>{CommandLanguage.PATTERN_FILE_PATH})";

    /// <summary>
    /// Create a set properties command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// [Simulation].Name=NewName
    /// </remarks>
    public static IModelCommand Create(string command, string relativeToDirectory)
    {
        string[] keywords = ["[", KEYWORD_FROM];
        string[] patterns = [PATTERN_VALUE, PATTERN_FROM];
        CommandSegment[] segments = CommandLanguage.ReadCommand(command, keywords, patterns);

        string model = CommandSegment.GetValue(segments, "model");
        string operate = CommandSegment.GetValue(segments, "operator");
        string value = CommandSegment.GetValue(segments, "value");
        string file = CommandSegment.GetValue(segments, "file");

        if (string.IsNullOrEmpty(model) || string.IsNullOrEmpty(operate))
            throw new Exception($"Invalid command: {command}");

        if (!string.IsNullOrEmpty(file))
            if (relativeToDirectory != null)
                file = Path.GetFullPath(file, relativeToDirectory);

        return new SetPropertyCommand(model, operate, value, file);
    }

    /// <summary>
    /// Convert an set properties command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        if (_fileName == null)
            return $"{_name}{_oper}{_value}";
        else
            return $"{_name}= from {_fileName}";
    }

}