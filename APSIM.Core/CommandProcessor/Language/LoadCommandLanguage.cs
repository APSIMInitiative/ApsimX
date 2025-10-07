using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class LoadCommand: IModelCommand
{
    /// <summary>
    /// Create a load command.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <remarks>
    /// load base.apsimx
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel parent)
    {
        string fileName = @"[\w\d\.\\:]+";

        string pattern = $@"load\s+" +
                         $@"(?<filename>{fileName})";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null)
            throw new Exception($"Invalid load command: {command}");

        return new LoadCommand(match.Groups["filename"]?.ToString());
    }

    /// <summary>s
    /// Convert an LoadCommand instance to a string.
    /// </summary>
    /// <param name="command">The LoadCommand instance.</param>
    /// <returns>A command language string.</returns>
    public override string ToString() => $"load {fileName}";
}