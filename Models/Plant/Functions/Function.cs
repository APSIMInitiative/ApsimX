using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A base function model
    /// </summary>
    [Serializable]
    [Description("Base class from which other functions inherit")]
    abstract public class Function: Model
    {
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        abstract public double Value { get; }
        /// <summary>Gets the values.</summary>
        /// <value>The values.</value>
        virtual public double[] Values { get { return new double[1] { Value }; } }

        /// <summary>Updates the variables.</summary>
        /// <param name="initial">The initial.</param>
        virtual public void UpdateVariables(string initial) { }

        /// <summary>The met data</summary>
        [Link]
        protected WeatherFile MetData = null;

        /// <summary>The clock</summary>
        [Link]
        protected Clock Clock = null;

    }
}