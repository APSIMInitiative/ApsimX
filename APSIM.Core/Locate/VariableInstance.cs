using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// This class encapsulates a single property of a model. Has properties for getting the value
/// of the property, the value in the base model and the default value as definned in the
/// source code.
/// </summary>
[Serializable]
internal class VariableObject : IVariable
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public VariableObject(object model)
    {
        Object = model;
    }

    /// <summary>
    /// Gets or sets the object this variable is relative to
    /// </summary>
    public object Object { get; set; }

    /// <summary>
    /// Return the name of the property.
    /// </summary>
    public string Name
    {
        get
        {
            return ReflectionUtilities.GetValueOfFieldOrProperty("Name", Object) as string;
        }
    }

    /// <summary>
    /// Returns the value of the property.
    /// </summary>
    public object Value
    {
        get
        {
            return Object;
        }
        set
        {
            Object = value;
        }
    }

    /// <summary>
    /// Gets the data type of the property
    /// </summary>
    public Type DataType { get { return Value?.GetType(); } }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable { get { return true; } }
}

