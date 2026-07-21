namespace APSIM.Core;

internal partial class DeleteCommand: IModelCommand
{
    private const string KEYWORD_DELETE = "delete ";
    private const string PATTERN_DELETE = $@"{KEYWORD_DELETE}(?<model>{CommandLanguage.PATTERN_MODEL_PATH})";

    /// <summary>
    /// Create a delete command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <remarks>
    /// delete [Zone].Report
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        string file = CommandLanguage.ReadSimpleCommand(command, KEYWORD_DELETE, PATTERN_DELETE, "model");
        return new DeleteCommand(file);
    }

    /// <summary>
    /// Convert an DeleteCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString() => $"{KEYWORD_DELETE}{modelName}";
}