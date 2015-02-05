
namespace Models.PMF.Functions
{
    using System;
    using Models.Core;

    /// <summary>
    /// Interface for a function
    /// </summary>
    public interface Function
    {
        /// <summary>Gets the value of the function.</summary>
        double Value { get; }
    }
}