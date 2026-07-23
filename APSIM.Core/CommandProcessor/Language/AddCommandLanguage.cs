namespace APSIM.Core;

internal partial class AddCommand: IModelCommand
{
    //Keywords for the command, in order they can appear in the command
    private const string KEYWORD_ADD = "add ";
    private const string KEYWORD_FROM = " from ";
    private const string KEYWORD_TO = " to ";
    private const string KEYWORD_NAME = " name ";

    //Regex patterns to read the text between keywords
    private const string PATTERN_ADD = $"{KEYWORD_ADD}(?<new>new )*(?<source>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_FROM = $"{KEYWORD_FROM}(?<file>{CommandLanguage.PATTERN_FILE_PATH})";
    private const string PATTERN_TO = $"{KEYWORD_TO}(?<all>all )*(?<destination>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_NAME = $"{KEYWORD_NAME}(?<name>{CommandLanguage.PATTERN_NAME_TEXT})";

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

        string[] keywords = [KEYWORD_ADD, KEYWORD_FROM, KEYWORD_TO, KEYWORD_NAME];
        string[] patterns = [PATTERN_ADD, PATTERN_FROM, PATTERN_TO, PATTERN_NAME];
        CommandSegment[] segments = CommandLanguage.ReadCommand(command, keywords, patterns);

        bool usesNew = CommandSegment.ContainsKey(segments, "new");
        bool usesAll = CommandSegment.ContainsKey(segments, "all");
        string source = CommandSegment.GetValue(segments, "source");
        string filepath = CommandSegment.GetValue(segments, "file");
        string destination = CommandSegment.GetValue(segments, "destination");
        string name = CommandSegment.GetValue(segments, "name");

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination))
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