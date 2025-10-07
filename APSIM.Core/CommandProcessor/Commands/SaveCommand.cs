using NetTopologySuite.Operation.Relate;

namespace APSIM.Core;

/// <summary>A save command</summary>
internal partial class SaveCommand : IModelCommand
{
    /// <summary>The name of a .apsimx file to get new model from.</summary>
    private readonly string fileName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileName">The name of a .apsimx file to get new model from.</param>
    public SaveCommand(string fileName)
    {
        this.fileName = fileName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo)
    {
        // Write to file.
        string json = FileFormat.WriteToString(relativeTo.Node);
        File.WriteAllText(fileName, json);
        return relativeTo;
    }
}