// -----------------------------------------------------------------------
// <copyright file="IArrayFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------

namespace Models.PMF.Functions
{
    using Models.Core;

    /// <summary>Interface for a function that returns an array</summary>
    public interface IArrayFunction
    {
        /// <summary>Gets the value of the function.</summary>
        double[] Value(int arrayIndex = -1);
    }
}