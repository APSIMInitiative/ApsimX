// -----------------------------------------------------------------------
// <copyright file="IFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    /// <summary>Interface for a function</summary>
    public interface IFunction
    {
        /// <summary>Gets the value of the function.</summary>
        double Value();

        /// <summary>Gets the values of the function.</summary>
        double[] Values();

    }
}