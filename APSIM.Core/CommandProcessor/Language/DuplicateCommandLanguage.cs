namespace APSIM.Core;

internal partial class DuplicateCommand: IModelCommand
{
    //Keywords for the command, in order they can appear in the command
    private const string KEYWORD_DUPLICATE = "duplicate ";
    private const string KEYWORD_NAME = " name ";

    //Regex patterns to read the text between keywords
    private const string PATTERN_DUPLICATE = $"{KEYWORD_DUPLICATE}(?<model>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_NAME = $"{KEYWORD_NAME}(?<name>{CommandLanguage.PATTERN_NAME_TEXT})";

    /// <summary>
    /// Create a duplicate command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// duplicate [Simulation] name SimulationCopy
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        string[] keywords = [KEYWORD_DUPLICATE, KEYWORD_NAME];
        string[] patterns = [PATTERN_DUPLICATE, PATTERN_NAME];
        CommandSegment[] segments = CommandLanguage.ReadCommand(command, keywords, patterns);

        string model = CommandSegment.GetValue(segments, "model");
        string name = CommandSegment.GetValue(segments, "name");

        if (string.IsNullOrEmpty(model))
            throw new Exception($"Invalid command: {command}");

        return new DuplicateCommand(model, name);
    }

    /// <summary>
    /// Convert an Duplicate command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        string command = $"{KEYWORD_DUPLICATE}{modelName}";
        if (!string.IsNullOrEmpty(newName))
            command += $"{KEYWORD_NAME}{newName}";
        return command;
    }

}