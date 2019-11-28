namespace Models.Functions
{
    using Models.Core;

    /// <summary>Interface for a function</summary>
    public interface IFunction
    {
        /// <summary>Gets the value of the function.</summary>
        double Value(int arrayIndex = -1);
    }
}