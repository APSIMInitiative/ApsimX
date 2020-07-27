using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Functions;
using Models;
using Models.Core;

namespace Models
{
    /// <summary>
    /// A template script used by CSharpExpressionFunction.
    /// </summary>
    [Serializable]
    public class Script : Model, IFunction
    {
#pragma warning disable CS0414
        [Link] Clock Clock = null;
#pragma warning restore CS0414

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            return 123456;
        }
    }
}