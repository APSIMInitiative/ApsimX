using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class AddCommand: IModelCommand
{
    //Keywords for the command, in order they can appear in the command
    private const string KEYWORD_ADD = "add ";
    private const string KEYWORD_FROM = " from ";
    private const string KEYWORD_TO = " to ";
    private const string KEYWORD_NAME = " name ";

    //Regex patterns to read the text between keywords
    private const string PATTERN_ADD = $@"{KEYWORD_ADD}(?<new>new )*(?<model>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_FROM = $@"{KEYWORD_FROM}(?<file>{CommandLanguage.PATTERN_FILE_PATH})";
    private const string PATTERN_TO = $@"{KEYWORD_TO}(?<all>all )*(?<model>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_NAME = $@"{KEYWORD_NAME}(?<name>{CommandLanguage.PATTERN_NAME_TEXT})";

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
        if (!command.ToLower().Trim().StartsWith("add"))
            throw new Exception($"Invalid command: {command}");

        //Determine what required and optional keywords are in the command
        //Order of keywords is important
        string[] keywords = [KEYWORD_ADD, KEYWORD_FROM, KEYWORD_TO, KEYWORD_NAME];
        IEnumerable<string> segments = CommandLanguage.BreakCommandIntoSegements(command, keywords);

        bool usesNew = false;
        bool usesAll = false;
        string source = "";
        string filepath = "";
        string destination = "";
        string name = "";
        //Use regex to quality check the inputs
        foreach(string segment in segments)
        {
            if (segment.StartsWith(KEYWORD_ADD))
            {
                Match match = Regex.Match(segment, PATTERN_ADD);
                if (!match.Success)
                    throw new Exception($"Invalid command: {command}");
                if (!string.IsNullOrEmpty(match.Groups["new"].ToString()))
                    usesNew = true;
                source = match.Groups["model"].ToString();
            }
            else if (segment.StartsWith(KEYWORD_FROM))
            {
                Match match = Regex.Match(segment, PATTERN_FROM);
                if (!match.Success)
                    throw new Exception($"Invalid command: {command}");
                filepath = match.Groups["file"].ToString();
            }
            else if (segment.StartsWith(KEYWORD_TO))
            {
                Match match = Regex.Match(segment, PATTERN_TO);
                if (!match.Success)
                    throw new Exception($"Invalid command: {command}");
                if (!string.IsNullOrEmpty(match.Groups["all"].ToString()))
                    usesAll = true;
                destination = match.Groups["model"].ToString();
            }
            else if (segment.StartsWith(KEYWORD_NAME))
            {
                Match match = Regex.Match(segment, PATTERN_NAME);
                if (!match.Success)
                    throw new Exception($"Invalid command: {command}");
                name = match.Groups["name"].ToString().Trim();
                if (string.IsNullOrEmpty(name))
                    throw new Exception($"Invalid command: {command}");
            }
        }

        if (string.IsNullOrEmpty(source))
            throw new Exception($"Invalid command: {command}");
        if (string.IsNullOrEmpty(destination))
            throw new Exception($"Invalid command: {command}");

        //find or create the model being added
        IModelReference modelReference;
        if (usesNew)
        {
            modelReference = new NewModelReference(source);
        }
        else if (!string.IsNullOrEmpty(filepath))
        {
            // If filename is relative, make it absolute
            if (relativeToDirectory != null)
                filepath = Path.GetFullPath(filepath, relativeToDirectory);
            modelReference = new ModelInFileReference(filepath, source);
        }
        else
        {
            if (!source.StartsWith('[') && !source.EndsWith(']'))
                source = $"[{source}]";
            modelReference = new ModelLocatorReference(relativeTo, source);
        }

        //make the command
        return new AddCommand(modelReference, toPath: destination, multiple: usesAll, newName: name);
    }

    /// <summary>
    /// Convert an AddCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        List<string> parts = ["add"];

        if (modelReference is ModelLocatorReference childModelReference)
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

        if (_multiple)
            parts.Add("all");

        parts.Add(_toPath);

        if (!string.IsNullOrEmpty(_newName))
        {
            parts.Add("name");
            parts.Add(_newName);
        }

        return string.Join(" ", parts);
    }
}