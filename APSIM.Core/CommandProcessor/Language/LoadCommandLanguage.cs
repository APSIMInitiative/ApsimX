namespace APSIM.Core;

internal partial class LoadCommand: IModelCommand
{
    private const string KEYWORD_LOAD = "load ";
    private const string PATTERN_LOAD = $@"{KEYWORD_LOAD}(?<file>{CommandLanguage.PATTERN_FILE_PATH})";
    
    /// <summary>
    /// Create a load command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns></returns>
    /// <remarks>
    /// load base.apsimx
    /// </remarks>
    public static IModelCommand Create(string command, string relativeToDirectory)
    {
        string file = CommandLanguage.ReadSimpleCommand(command, KEYWORD_LOAD, PATTERN_LOAD, "file");

        if (relativeToDirectory != null)
            file = Path.GetFullPath(file, relativeToDirectory);

        return new LoadCommand(file);      
    }

    /// <summary>
    /// Convert an LoadCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString() => $"{KEYWORD_LOAD}{fileName}";
}