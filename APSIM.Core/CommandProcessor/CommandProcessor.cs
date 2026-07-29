namespace APSIM.Core;

/// <summary>
/// A command processor for running commands.
/// </summary>
public class CommandProcessor
{
    /// <summary>
    /// Run all commands.
    /// </summary>
    /// <param name="relativeTo">The commands will be run relative to this argument.</param>
    public static void Run(IEnumerable<IModelCommand> commands, INodeModel relativeTo, IRunner runner)
    {
        var localRelativeTo = relativeTo;

        List<string> filePaths = new List<string>();
        foreach (IModelCommand command in commands)
        {
            if (command is ICommandReferenceExternalFile file)
            {
                string filePath = file.GetFilePath();
                if (!string.IsNullOrEmpty(filePath) && !filePaths.Contains(filePath))
                    if (File.Exists(filePath))
                        filePaths.Add(filePath);
            }
        }
        List<Node> externalFileCache = new List<Node>();
        foreach (string filePath in filePaths)
        {
            Type simulationsType = ModelRegistry.ModelNameToType("Simulations");
            Node externalRootNode = FileFormat.ReadFromFile(filePath, simulationsType);
            externalFileCache.Add(externalRootNode);
        }

        foreach (IModelCommand command in commands)
            localRelativeTo = command.Run(localRelativeTo, runner, externalFileCache);
    }
}