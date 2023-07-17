using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
    public class Script : Model, IBooleanFunction
    {
        [Link] IClock Clock = null;

        /// <summary>Gets the value of the function.</summary>
        public bool Value()
        {
            return Clock.FractionComplete;
        }
    }
}