using Newtonsoft.Json;

namespace APSIM.Core;

/// <summary>A command that can set the property of a model.</summary>
/// <remarks>
/// The JsonProperty attributes below are needed for JSON serialisation which the APSIM.Server uses.
/// </remarks>
public partial class SetPropertyCommand : IModelCommand
{
    [JsonProperty]
    private readonly string name;
    [JsonProperty]
    private readonly string value;
    [JsonProperty]
    private readonly string fileName;
    [JsonProperty]
    private readonly string oper;
    [JsonProperty]
    private readonly bool multiple;
    private readonly List<VariableComposite> obj = [];
    private readonly List<object> oldValues = [];

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="oper">Operation to perform (e.g., "=", "+=", "-=").</param>
    /// <param name="value">Property value.</param>
    /// <param name="fileName">Optional file name.</param>
    /// <param name="multiple">Perform property set for multiple instances?</param>
    public SetPropertyCommand(string name, string oper, string value, string fileName = null, bool multiple = false)
    {
        this.name = name;
        this.oper = oper;
        this.value = value;
        this.fileName = fileName;
        this.multiple = multiple;
    }

    /// <summary>Property value.</summary>
    internal object Value => value;

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        // Read the property value from an external file if necessary.
        string propertyValue = value;
        if (fileName != null)
        {
            if (!File.Exists(fileName))
                throw new Exception($"Cannot find file : {fileName}");
            propertyValue = File.ReadAllText(fileName);
        }

        // Get all model instances that need a property set.
        IEnumerable<VariableComposite> instances = null;
        if (multiple)
            instances = relativeTo.Node.GetAllObjects(name, flags: LocatorFlags.PropertiesOnly | LocatorFlags.CaseSensitive | LocatorFlags.IncludeDisabled);
        else
        {
            var instance = relativeTo.Node.GetObject(name);
            if (instance != null)
                instances = [instance];
        }

        if (instances == null || !instances.Any())
            throw new Exception($"Cannot find property {name}");

        // Perform multiple property sets.
        foreach (var instance in instances)
        {
            // Capture the old value so that we can perform an undo if necessary.
            obj.Add(instance);
            oldValues.Add(instance.Value);

            if (oper == "=")
            {
                // If "null" was specified then set the object value to null. Otherwise convert
                // the value into correct type.
                if (propertyValue == "null")
                    instance.Value = null;
                else
                    instance.Value = ApsimConvert.ToType(propertyValue, instance.DataType);
            }
            else
            {
                // Throw if trying to add or remove from a scalar.
                if (!ApsimConvert.IsIList(instance.DataType))
                    throw new Exception($"Property {name} is not an array type. Cannot use += or -= operators.");

                // Throw if trying to add or remove from an array that isn't a string array.
                if (DataAccessor.GetElementTypeOfIList(instance.DataType) != typeof(string))
                    throw new Exception($"Property {name} is not a string array. Cannot use += or -= operators.");

                // Get the current array as a list of strings.
                object valueAsObject = instance.Value ?? throw new Exception($" Cannot use += or -= operators on a null array.");
                List<string> strings = ApsimConvert.ToType(valueAsObject, typeof(List<string>)) as List<string>;
                if (strings == null || strings.Count == 0)
                    throw new Exception("Cannot use += or -= operators on a null or empty array.");

                string[] tokens = [.. value.Split('=').Select(part => part.Trim())];

                // Add or remove a string.
                if (oper == "+=")
                {
                    int index = strings.FindIndex(s => s.Split('=')[0].Trim() == tokens.First());
                    if (index >= 0)
                        strings[index] = value;
                    else
                        strings.Add(value);

                }
                else
                    strings.RemoveAll(s => s.Split('=')[0].Trim() == tokens.First());

                // Give the modifed array back to the object.
                var objValue = ApsimConvert.ToType(strings, instance.DataType);
                instance.Value = objValue;
            }
        }

        return relativeTo;
    }

    /// <summary>
    /// Revert the value of the property to its original value.
    /// </summary>
    public void Undo()
    {
        for (int i = 0; i < obj.Count; i++)
            obj[i].Value = oldValues[i];
    }

    /// <summary>
    /// Return a hash code - useful for unit testing.
    /// </summary>
    public override int GetHashCode()
    {
        return (name, value, fileName, oper, multiple).GetHashCode();
    }

}