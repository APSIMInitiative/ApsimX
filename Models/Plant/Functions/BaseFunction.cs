// ----------------------------------------------------------------------
// <copyright file="BaseFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using System;

    /// <summary>An abstract base class for performing a mathematic operation (e.g. multiply, divide)</summary>
    [Serializable]
    public abstract class BaseFunction : Model, IFunction
    {
        /// <summary>Gets the value of the function.</summary>
        public double Value()
        {
            return Values()[0];
        }

        /// <summary>Gets the values of the function.</summary>
        public abstract double[] Values();

    }
}