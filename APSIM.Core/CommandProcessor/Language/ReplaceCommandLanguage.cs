using System.Text.RegularExpressions;

namespace APSIM.Core;

public partial class ReplaceCommand: IModelCommand
{
    /// <summary>
    /// Create a replace command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeTo">The node that owns the command string.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns></returns>
    /// <remarks>
    /// replace [Report] with NewReport name ReportWithNewName
    /// replace all [Report] with NewReport name ReportWithNewName
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel relativeTo, string relativeToDirectory)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";
        string fileNamePattern = @"[\w\d-_\.\\:/]+";
        string modelNamePattern = @"[\w\d]+";

        string pattern = $@"replace (?<all>all)*\s*" +
                         $@"(?<oldmodelpath>{modelNameWithBrackets})" + @"\s+" +
                         $@"with\s+" +
                         $@"(?<newmodelpath>{modelNameWithBrackets})\s*" +
                         $@"(?:from\s+(?<filename>{fileNamePattern}))*\s*" +
                         $@"(?:name\s+(?<name>{modelNamePattern}))*";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null || !match.Success ||
             command.Length != match.Length)
            throw new Exception($"Invalid command: {command}");

        IModelReference modelReference;
        if (!string.IsNullOrEmpty(match.Groups["filename"]?.ToString()))
        {
            // If filename is relative, make it absolute
            string fileName = match.Groups["filename"].ToString();
            if (relativeToDirectory != null)
                fileName = Path.GetFullPath(fileName, relativeToDirectory);
            modelReference = new ModelInFileReference(fileName, match.Groups["newmodelpath"]?.ToString());
        }
        else
        {
            string newModelPath = match.Groups["newmodelpath"].ToString().Trim();
            if (!newModelPath.StartsWith('[') && !newModelPath.EndsWith(']'))
                newModelPath = $"[{newModelPath}]";
            modelReference = new ModelLocatorReference(relativeTo, newModelPath);
        }

        return new ReplaceCommand(modelReference,
                                  replacementPath: match.Groups["oldmodelpath"]?.ToString(),
                                  multiple: match.Groups["all"].Success,
                                  MatchType.Name,
                                  newName: match.Groups["name"].ToString());
    }

    /// <summary>
    /// Convert an ReplaceCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        List<string> parts = ["replace"];

        if (multiple)
            parts.Add("all");

        parts.Add(replacementPath);

        parts.Add("with");

        if (modelReference is ModelLocatorReference childModelReference)
            parts.Add(childModelReference.modelName);
        else if (modelReference is ModelInFileReference modelInFileReference)
        {
            parts.Add(modelInFileReference.modelName);
            parts.Add("from");
            parts.Add(modelInFileReference.fileName);
        }
        if (!string.IsNullOrEmpty(newName))
        {
            parts.Add("name");
            parts.Add(newName);
        }

        return string.Join(" ", parts);
    }
}