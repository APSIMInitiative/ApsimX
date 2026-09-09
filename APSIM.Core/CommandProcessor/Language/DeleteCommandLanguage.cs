using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class DeleteCommand: IModelCommand
{
    private const string KEYWORD_DELETE = "delete";
    private const string KEYWORD_FROM = " from ";
    private const string KEYWORD_ALL = " all ";
    private const string PATTERN_DELETE = $@"{KEYWORD_DELETE}(?<all>{KEYWORD_ALL})*(?<model>{CommandLanguage.PATTERN_MODEL_PATH})";
    private const string PATTERN_FROM = $@"{KEYWORD_FROM}(?<parent>{CommandLanguage.PATTERN_MODEL_PATH})";

    /// <summary>
    /// Create a delete command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <remarks>
    /// delete [Zone].Report
    /// delete [Report]
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        string[] keywords = [KEYWORD_DELETE, KEYWORD_FROM];
        string[] patterns = [PATTERN_DELETE, PATTERN_FROM];
        CommandSegment[] segments = CommandLanguage.ReadCommand(command, keywords, patterns);
        string model = CommandSegment.GetValue(segments, "model");
        bool usesAll = CommandSegment.ContainsKey(segments, "all");
        string parentModel = CommandSegment.GetValue(segments, "parent");
        if (string.IsNullOrEmpty(model))
            throw new Exception($"Invalid command: {command}");
        return new DeleteCommand(model, usesAll, parentModel);
    }

    /// <summary>
    /// Convert an DeleteCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        string all = _multiple ? KEYWORD_ALL : " ";
        string from = string.IsNullOrEmpty(_parentModelName) ? "" : $"{KEYWORD_FROM}{_parentModelName}";
        return $"{KEYWORD_DELETE}{all}{_modelName}{from}";
    } 
}