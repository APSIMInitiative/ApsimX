namespace APSIM.Core;

/// <summary>
/// This abstract base class encapsulates the interface for a variable from a Model.
/// source code.
/// </summary>
public interface IVariable
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the object this variable is relative to
    /// </summary>
    public object Object { get; set; }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Gets the data type of the property
    /// </summary>
    public Type DataType { get; }

    /// <summary>
    /// Returns true if the variable is writable
    /// </summary>
    public bool Writable { get; }
}
