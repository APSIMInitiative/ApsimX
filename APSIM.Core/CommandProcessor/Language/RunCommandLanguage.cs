namespace APSIM.Core;

internal partial class RunCommand: IModelCommand
{
    private const string KEYWORD_RUN = "run";

    /// <summary>
    /// Create a run command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    public static IModelCommand Create(string command)
    {
        if (command.ToLower().Trim() != KEYWORD_RUN)
            throw new Exception($"Invalid run command: {command}");
        else
            return new RunCommand();
    }

    /// <summary>
    /// Convert an save command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> KEYWORD_RUN;

}