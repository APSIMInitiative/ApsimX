using System.Text;
using System.Text.RegularExpressions;

namespace APSIM.Core;

public class CommandSegment
{
    public string Name {get; set;}
    public string Value {get; set;}

    public CommandSegment(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public static bool ContainsKey(IEnumerable<CommandSegment> segments, string name)
    {
        foreach(CommandSegment segment in segments)
            if (segment.Name.ToLower() == name.ToLower())
                return true;
        return false;
    }

    public static string GetValue(IEnumerable<CommandSegment> segments, string name)
    {
        foreach(CommandSegment segment in segments)
            if (segment.Name.ToLower() == name.ToLower())
                return segment.Value.ToString().Trim();
        return null;
    }
}

/// <summary>
/// Implements the command language
/// </summary>
/// <remarks>
/// add Report to [Zone]
/// add child Report to [Zone] name MyReport
/// add Report to all [Zone]
/// add [Report] from anotherfile.apsimx to [Zone]
/// replace all [Wheat] with child NewWheat
/// replace all [Wheat] with NewWheat from anotherfile.apsimx
/// replace all [Wheat] with child NewWheat
/// delete [Zone].Report
/// duplicate [Simulation] name SimulationCopy
/// [Simulation].Name=NewName
/// run APSIM
/// load base.apsimx
/// save modifiedSim.apsimx
/// </remarks>
public class CommandLanguage
{
    /// <summary>Regex pattern for getting a model path</summary>
    public const string PATTERN_MODEL_PATH = @"[\w\-\[\]\.\: ]+";

    /// <summary>Regex pattern for getting a file path</summary>
    public const string PATTERN_FILE_PATH = @"[\w\-_\.\\:/ ]+";

    /// <summary>Regex pattern for a model name</summary>
    public const string PATTERN_NAME_TEXT = @"[\w\- ]+";

    /// <summary>Regex pattern for a generic value</summary>
    public const string PATTERN_VALUE = @"[^\<]+";

    /// <summary>Regex pattern for an operator on a value setting command</summary>
    public const string PATTERN_OPERATOR = @"=|\+=|-=|=<";

    /// <summary>
    /// Parse a collection of lines into a collection of model commands.
    /// </summary>
    /// <param name="lines">Collection of strings, one for each line.</param>
    /// <param name="relativeTo">The node owning the collection of strings.</param>
    /// <param name="relativeToDirectory">Directory that file names are relative to.</param>
    /// <returns></returns>
    public static IEnumerable<IModelCommand> StringToCommands(IEnumerable<string> lines, INodeModel relativeTo, string relativeToDirectory)
    {
        List<IModelCommand> commands = [];

        // Combine indented lines with above line. e.g. convert lines like:
        //    define factor sow
        //       [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
        // into:
        //    define factor sow [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
        // The command and parameters are then converted into a IModelCommand

        StringBuilder command = null;
        foreach (var line in lines)
        {
            // Strip commented characters.
            string sanitisedLine = line.Replace("//", "#");
            int posComment = sanitisedLine.IndexOf('#');
            if (posComment != -1)
                sanitisedLine = sanitisedLine.Remove(posComment);

            sanitisedLine = sanitisedLine.TrimEnd();

            if (sanitisedLine.StartsWith(' '))
            {
                // Add to existing command.
                command.Append(sanitisedLine.Trim());
            }
            else if (sanitisedLine != string.Empty)
            {
                // New command. If there is an existing command then add it to the commands collection.
                if (command == null)
                    command = new();
                else
                    commands.Add(ConvertCommandParameterToModelCommand(command.ToString(), relativeTo, relativeToDirectory));

                command.Clear();
                command.Append(sanitisedLine.Trim());
            }
        }
        if (command != null)
            commands.Add(ConvertCommandParameterToModelCommand(command.ToString(), relativeTo, relativeToDirectory));

        return commands;
    }

    /// <summary>
    /// Convert a collection of commands into strings.
    /// </summary>
    /// <param name="commands">The model commands.</param>
    /// <returns>A collection of lines.</returns>
    public static IEnumerable<string> CommandsToStrings(IEnumerable<IModelCommand> commands)
    {
        List<string> lines = [];
        foreach (var command in commands)
            lines.Add(command.ToString());
        return lines;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="keyword"></param>
    /// <param name="pattern"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public static string ReadCommand(string command, string keyword, string pattern)
    {
        IEnumerable<CommandSegment> segments = ReadCommand(command, [keyword], [pattern]);
        if (segments.Count() != 1)
            throw new Exception($"Invalid command: {command}");

        string value = segments.First().Value;
        if (string.IsNullOrEmpty(value))
            throw new Exception($"Invalid command: {command}");

        return value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="keywords"></param>
    /// <param name="patterns"></param>
    public static CommandSegment[] ReadCommand(string command, string[] keywords, string[] patterns)
    {
        if (keywords.Length != patterns.Length)
            throw new Exception($"Invalid command: {command}");

        IEnumerable<string> segments = BreakCommandIntoSegements(command, keywords);
        
        List<CommandSegment> commandSegments = new List<CommandSegment>();
        foreach(string segment in segments)
        {
            for(int i = 0; i < keywords.Length; i++)
            {
                if (segment.StartsWith(keywords[i]))
                {
                    Match match = Regex.Match(segment, patterns[i]);
                    if (!match.Success)
                        throw new Exception($"Invalid command: {command}");

                    foreach(string key in match.Groups.Keys)
                        if (key != "0" && !string.IsNullOrEmpty(match.Groups[key].ToString())) //the first group is the entire segment and should be skipped
                            commandSegments.Add(new CommandSegment(key, match.Groups[key].ToString()));
                }
            }
        }
        return commandSegments.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="keywords"></param>
    /// <returns></returns>
    private static IEnumerable<string> BreakCommandIntoSegements(string command, string[] keywords)
    {
        //remove quoted sections prior to searching for keywords
        List<(int, string)> quotes = new List<(int, string)>();
        string commandWithoutQuotedSections = "";
        string quotedSection = "";
        bool inQuotes = false;
        foreach(char character in command)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                if (inQuotes)
                    quotedSection = "";
                else
                    quotes.Add((commandWithoutQuotedSections.Length, quotedSection));
            }
            else
            {
                if (!inQuotes)
                    commandWithoutQuotedSections += character;
                else
                    quotedSection += character;
            }
        }
        quotes.Reverse();

        //work out where the keywords are in the quoteless command
        int[] positions = new int[keywords.Length];
        int lastPosition = 0;
        for(int i = 0; i < keywords.Length; i++)
        {
            positions[i] = commandWithoutQuotedSections.IndexOf(keywords[i], lastPosition);
            if (positions[i] > 0)
                lastPosition = positions[i];
        }
        
        //remove quote characters from original command as our positions are created based on them
        //not being there
        string commandWithoutQuoteMarkers = command.Replace("\"", "");

        //adjust positions based on if text was quoted out
        foreach((int, string) quote in quotes)
        {
            for(int i = positions.Length-1; i >= 0; i--)
            {
                if (positions[i] >= quote.Item1)
                    positions[i] += quote.Item2.Length;
            }
        }

        //Break the command into parts based on the keywords
        List<string> segments = new List<string>();
        for(int i = 0; i < positions.Length; i++)
        {
            int startIndex = positions[i];
            if (startIndex >= 0)
            {
                int endIndex = -1;
                for(int j = i+1; j < positions.Length && endIndex < 0; j++)
                    if (positions[j] >= 0)
                        endIndex = positions[j];
                string segment;
                if (endIndex >= 0)
                    segment = commandWithoutQuoteMarkers.Substring(startIndex, endIndex-startIndex);
                else
                    segment = commandWithoutQuoteMarkers.Substring(startIndex);
                segments.Add(segment);
            }
        }

        return segments;
    }

    /// <summary>
    /// Create a model command from a string.
    /// </summary>
    /// <param name="command">Command string.</param>
    /// <param name="relativeTo">The node that owns the command string.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>An instance of a model command.</returns>
    private static IModelCommand ConvertCommandParameterToModelCommand(string command, INodeModel relativeTo, string relativeToDirectory)
    {
        command = command.Trim();
        if (command.StartsWith("add", StringComparison.InvariantCultureIgnoreCase))
            return AddCommand.Create(command, relativeTo, relativeToDirectory);
        else if (command.StartsWith("delete", StringComparison.InvariantCultureIgnoreCase))
            return DeleteCommand.Create(command);
        else if (command.StartsWith("duplicate", StringComparison.InvariantCultureIgnoreCase))
            return DuplicateCommand.Create(command);
        else if (command.StartsWith("save", StringComparison.InvariantCultureIgnoreCase))
            return SaveCommand.Create(command, relativeToDirectory);
        else if (command.StartsWith("load", StringComparison.InvariantCultureIgnoreCase))
            return LoadCommand.Create(command, relativeToDirectory);
        else if (command.StartsWith("run", StringComparison.InvariantCultureIgnoreCase))
            return RunCommand.Create(command);
        else if (command.StartsWith("replace", StringComparison.InvariantCultureIgnoreCase))
            return ReplaceCommand.Create(command, relativeTo, relativeToDirectory);
        else if (command.StartsWith("move", StringComparison.InvariantCultureIgnoreCase))
            return MoveCommand.Create(command);
        else if (command.Contains('='))
            return SetPropertyCommand.Create(command, relativeToDirectory);

        throw new Exception($"Unknown command: {command}");
    }
}