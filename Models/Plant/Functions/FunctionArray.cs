// -----------------------------------------------------------------------
// <copyright file="FunctionArray.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using System;

    /// <summary>
    /// Interface for all array functions i.e. functions that return
    /// multiple values.
    /// </summary>
    public interface FunctionArray
    {
        /// <summary>Gets the values.</summary>
        /// <value>The values.</value>
        double[] Values { get; }
    }
}
