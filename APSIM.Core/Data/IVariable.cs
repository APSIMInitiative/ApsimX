namespace APSIM.Core;

/// <summary>
/// This abstract base class encapsulates the interface for a variable from a Model.
/// source code.
/// </summary>
[Serializable]
public abstract class IVariable
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets or sets the object this variable is relative to
    /// </summary>
    public abstract object Object { get; set; }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    public abstract object Value { get; set; }

    /// <summary>
    /// Gets the data type of the property
    /// </summary>
    public abstract Type DataType { get; }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public abstract bool Writable { get; }
}
