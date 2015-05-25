using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A constant value function
    /// </summary>
    [Serializable]
    public class Constant : Model, IFunction
    {
        /// <summary>Gets the value.</summary>
        public double Value { get; set; }
    }
}