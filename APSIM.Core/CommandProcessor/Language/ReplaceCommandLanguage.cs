namespace APSIM.Core;

public partial class ReplaceCommand: IModelCommand
{
    //Keywords for the command, in order they can appear in the command
    private const string KEYWORD_REPLACE = "replace ";
    private const string KEYWORD_WITH = " with ";
    private const string KEYWORD_FROM = " from ";
    private const string KEYWORD_NAME = " name ";

    //Regex patterns to read the text between keywords
    private const string PATTERN_REPLACE = $"{KEYWORD_REPLACE}(?<all>all )*(?<existingmodel>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_WITH = $"{KEYWORD_WITH}(?<replacingmodel>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_FROM = $"{KEYWORD_FROM}(?<file>{CommandLanguage.PATTERN_FILE_PATH})";
    private const string PATTERN_NAME = $"{KEYWORD_NAME}(?<name>{CommandLanguage.PATTERN_NAME_TEXT})";

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
        string[] keywords = [KEYWORD_REPLACE, KEYWORD_WITH, KEYWORD_FROM, KEYWORD_NAME];
        string[] patterns = [PATTERN_REPLACE, PATTERN_WITH, PATTERN_FROM, PATTERN_NAME];
        CommandSegment[] segments = CommandLanguage.ReadComplexCommand(command, keywords, patterns);

        bool usesAll = CommandSegment.ContainsKey(segments, "all");
        string existingmodel = CommandSegment.GetValue(segments, "existingmodel");
        string replacingmodel = CommandSegment.GetValue(segments, "replacingmodel");
        string file = CommandSegment.GetValue(segments, "file");
        string name = CommandSegment.GetValue(segments, "name");

        IModelReference modelReference;
        if (!string.IsNullOrEmpty(file))
        {
            if (relativeToDirectory != null)
                file = Path.GetFullPath(file, relativeToDirectory);
            modelReference = new ModelInFileReference(file, replacingmodel);
        }
        else
        {
            if (!replacingmodel.StartsWith('[') && !replacingmodel.EndsWith(']'))
                replacingmodel = $"[{replacingmodel}]";
            modelReference = new ModelLocatorReference(relativeTo, replacingmodel);
        }

        return new ReplaceCommand(modelReference,
                                  replacementPath: existingmodel,
                                  multiple: usesAll,
                                  MatchType.Name,
                                  newName: name);
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