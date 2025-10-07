using System.Text.RegularExpressions;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

internal partial class AddCommand: IModelCommand
{
    /// <summary>
    /// Create an add command.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <remarks>
    /// add new Report to [Zone]
    /// add child Report to [Zone] name MyReport
    /// add child Report to all [Zone]
    /// add [Report] from anotherfile.apsimx to [Zone]
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel parent)
    {
        string modelNameWithBrackets = @"[\w\d\[\]]+";
        string modelName = @"[\w\d]+";
        string fileName = @"[\w\d\.]+";

        string pattern = $@"(?<childnew>child|new)*" + @"\s*" +
                         $@"(?<modelname>{modelNameWithBrackets})" + @"\s+" +
                         $@"(?:from\s+(?<filename>{fileName})\s+)*" +
                         $@"to\s+" +
                         $@"(?<all>all)*\s*" +
                         $@"(?<topath>{modelNameWithBrackets})\s*" +
                         $@"(?:name\s+(?<name>{modelName}))*";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null)
            throw new Exception($"Invalid add command: {command}");

        IModelReference modelReference;
        if (match.Groups["childnew"]?.ToString() == "child")
            modelReference = new ChildModelReference(parent, match.Groups["modelname"]?.ToString());
        else if (!string.IsNullOrEmpty(match.Groups["filename"]?.ToString()))
            modelReference = new ModelInFileReference(match.Groups["filename"]?.ToString(), match.Groups["modelname"]?.ToString());
        else
            modelReference = new NewModelReference(match.Groups["modelname"]?.ToString());


        return new AddCommand(modelReference,
                              toPath: match.Groups["topath"]?.ToString(),
                              multiple: match.Groups["all"].Success,
                              newName: match.Groups["name"]?.ToString());
    }

    /// <summary>
    /// Convert an AddCommand instance to a string.
    /// </summary>
    /// <param name="command">The AddCommand instance.</param>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        List<string> parts = ["add"];

        if (modelReference is ChildModelReference childModelReference)
        {
            parts.Add("child");
            parts.Add(childModelReference.childModelName);
        }
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