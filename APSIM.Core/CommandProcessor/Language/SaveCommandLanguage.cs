namespace APSIM.Core;

internal partial class SaveCommand: IModelCommand
{
    private const string KEYWORD_SAVE = "save ";
    private const string PATTERN_SAVE = $@"{KEYWORD_SAVE}(?<file>{CommandLanguage.PATTERN_FILE_PATH})";

    /// <summary>
    /// Create a save command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// save modifiedSim.apsimx
    /// </remarks>
    public static IModelCommand Create(string command, string relativeToDirectory)
    {
        string file = CommandLanguage.ReadCommand(command, KEYWORD_SAVE, PATTERN_SAVE);

        if (relativeToDirectory != null)
            file = Path.GetFullPath(file, relativeToDirectory);

        return new SaveCommand(file);
    }

    /// <summary>
    /// Convert an save command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> $"{KEYWORD_SAVE}{fileName}";

}