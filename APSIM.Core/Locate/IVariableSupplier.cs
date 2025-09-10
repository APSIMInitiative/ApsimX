namespace APSIM.Core;

public interface IVariableSupplier
{
    /// <summary>
    /// Get the value of a variable.
    /// </summary>
    /// <param name="name">Name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool Get(string name, out object value);
}