using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class AddCommand: IModelCommand
{
    /// <summary>
    /// Create an add command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeTo">The node that owns the command string.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns></returns>
    /// <remarks>
    /// add new Report to [Zone]
    /// add Report to [Zone] name MyReport
    /// add Report to all [Zone]
    /// add [Report] from anotherfile.apsimx to [Zone]
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel relativeTo, string relativeToDirectory)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";
        string modelNamePattern = @"[\w\d]+";
        string fileNamePattern = @"[\w\d-_\.\\:/]+";

        string pattern = $@"add (?<new>new)*" + @"\s*" +
                         $@"(?<modelname>{modelNameWithBrackets})" + @"\s+" +
                         $@"(?:from\s+(?<filename>{fileNamePattern})\s+)*" +
                         $@"to\s+" +
                         $@"(?<all>all)*\s*" +
                         $@"(?<topath>{modelNameWithBrackets})\s*" +
                         $@"(?:name\s+(?<name>{modelNamePattern}))*";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null || !match.Success ||
             command.Length != match.Length)
            throw new Exception($"Invalid command: {command}");

        IModelReference modelReference;
        if (match.Groups["new"]?.ToString() == "new")
            modelReference = new NewModelReference(match.Groups["modelname"]?.ToString());
        else if (!string.IsNullOrEmpty(match.Groups["filename"]?.ToString()))
        {
            // If filename is relative, make it absolute
            string fileName = match.Groups["filename"].ToString();
            if (relativeToDirectory != null)
                fileName = Path.GetFullPath(fileName, relativeToDirectory);
            modelReference = new ModelInFileReference(fileName, match.Groups["modelname"]?.ToString());
        }
        else
        {
            string modelName = match.Groups["modelname"].ToString().Trim();
            if (!modelName.StartsWith('[') && !modelName.EndsWith(']'))
                modelName = $"[{modelName}]";
            modelReference = new ModelReference(relativeTo, modelName);
        }

        return new AddCommand(modelReference,
                              toPath: match.Groups["topath"]?.ToString(),
                              multiple: match.Groups["all"].Success,
                              newName: match.Groups["name"]?.ToString());
    }

    /// <summary>
    /// Convert an AddCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        List<string> parts = ["add"];

        if (modelReference is ModelReference childModelReference)
            parts.Add(childModelReference.modelName);
        else if (modelReference is NewModelReference newModelReference)
        {
            parts.Add("new");
            parts.Add(newModelReference.newModelType);
        }
        else if (modelReference is ModelInFileReference modelInFileReference)
        {
            parts.Add(modelInFileReference.modelName);
            parts.Add("from");
            parts.Add(modelInFileReference.fileName);
        }

        parts.Add("to");

        if (multiple)
            parts.Add("all");

        parts.Add(toPath);

        if (!string.IsNullOrEmpty(newName))
        {
            parts.Add("name");
            parts.Add(newName);
        }

        return string.Join(" ", parts);
    }
}