namespace APSIM.Core;

/// <summary>A set properties command</summary>
internal partial class SetPropertiesCommand : IModelCommand
{
    /// <summary>Collection of property name/value pairs.</summary>
    private readonly string name;
    private readonly string value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="properties">name/value pair.</param>
    public SetPropertiesCommand(string name, string value)
    {
        this.name = name;
        this.value = value;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunAPSIM runner)
    {
        // Convert value into correct type.
        var obj = relativeTo.Node.GetObject(name);
        var valueOfCorrentType = ApsimConvert.ToType(value, obj.DataType);
        obj.Value = valueOfCorrentType;
        return relativeTo;
    }
}