using Models.Core;

namespace Models.Functions
{
    /// <summary>Interface for a boolean function</summary>
    public interface IBooleanFunction : IModel
    {
        /// <summary>Gets the value of the function.</summary>
        bool Value();
    }
}