namespace APSIM.Core;

/// <summary>A set properties command</summary>
public class SetPropertiesCommand : IModelCommand
{
    /// <summary>Collection of property name/value pairs.</summary>
    public readonly IEnumerable<(string name, string value)> properties;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="properties">Collection of property name/value pairs.</param>
    public SetPropertiesCommand(IEnumerable<(string name, string value)> properties)
    {
        this.properties = properties;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    void IModelCommand.Run(INodeModel relativeTo)
    {
        foreach (var property in properties)
            relativeTo.Node.Set(property.name, property.value, relativeTo);
    }
}