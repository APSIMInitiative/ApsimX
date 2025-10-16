namespace APSIM.Core;

/// <summary>A set properties command</summary>
internal partial class SetPropertiesCommand : IModelCommand
{
    private readonly string name;
    private readonly string value;
    private readonly string fileName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="name">Property value</param>
    /// <param name="filename">Optional file name.</param>
    public SetPropertiesCommand(string name, string value, string fileName)
    {
        this.name = name;
        this.value = value;
        this.fileName = fileName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        string propertyValue = value;
        if (fileName != null)
        {
            if (!File.Exists(fileName))
                throw new Exception($"Cannot find file : {fileName}");
            propertyValue = File.ReadAllText(fileName);
        }

        // Convert value into correct type.
        var obj = relativeTo.Node.GetObject(name) ?? throw new Exception($"Cannot find property {name}");
        obj.Value = ApsimConvert.ToType(propertyValue, obj.DataType);
        return relativeTo;
    }
}