using Models.Core;

namespace Models.Functions
{
    /// <summary>Interface for a function</summary>
    public interface IIndexedFunction : IModel
    {
        /// <summary>Gets the value of the function.</summary>
        double ValueIndexed(double dX);
    }
}