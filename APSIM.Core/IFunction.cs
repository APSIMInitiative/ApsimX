namespace APSIM.Core;

public interface IFunction
{
    /// <summary>Gets the value of the function.</summary>
    double Value(int arrayIndex = -1);
}