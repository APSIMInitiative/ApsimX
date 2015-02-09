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
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        [XmlElement("Value")]
        public double value { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value { get { return value; } }
    }
}