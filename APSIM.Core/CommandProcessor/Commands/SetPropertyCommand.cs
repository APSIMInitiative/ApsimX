namespace APSIM.Core;

/// <summary>A command that can set the property of a model.</summary>
internal partial class SetPropertyCommand : IModelCommand
{
    private readonly string name;
    private readonly string value;
    private readonly string fileName;
    private readonly string oper;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="name">Property value</param>
    /// <param name="filename">Optional file name.</param>
    public SetPropertyCommand(string name, string oper, string value, string fileName)
    {
        this.name = name;
        this.oper = oper;
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

        var obj = relativeTo.Node.GetObject(name) ?? throw new Exception($"Cannot find property {name}");
        if (oper == "=")
        {
            // Convert value into correct type.
            obj.Value = ApsimConvert.ToType(propertyValue, obj.DataType);
        }
        else
        {
            // Throw if trying to add or remove from a scalar.
            if (!ApsimConvert.IsIList(obj.DataType))
                throw new Exception($"Property {name} is not an array type. Cannot use += or -= operators.");

            // Throw if trying to add or remove from an array that isn't a string array.
            if (DataAccessor.GetElementTypeOfIList(obj.DataType) != typeof(string))
                throw new Exception($"Property {name} is not a string array. Cannot use += or -= operators.");

            // Get the current array as a list of strings.
            object valueAsObject = obj.Value ?? throw new Exception($" Cannot use += or -= operators on a null array.");
            List<string> strings = ApsimConvert.ToType(valueAsObject, typeof(List<string>)) as List<string>;

            // Add or remove a string.
            if (oper == "+=")
                strings.Add(value);
            else
                strings.Remove(value);

            // Give the modifed array back to the object.
            var objValue = ApsimConvert.ToType(strings, obj.DataType);
            obj.Value = objValue;
        }

        return relativeTo;
    }
}