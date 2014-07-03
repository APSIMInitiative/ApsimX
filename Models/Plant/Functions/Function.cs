using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Base class from which other functions inherit")]
    abstract public class Function: Model
    {
        abstract public double Value { get; }
        virtual public double[] Values { get { return new double[1] { Value }; } }

        virtual public void UpdateVariables(string initial) { }

        [Link]
        protected WeatherFile MetData = null;

        [Link]
        protected Clock Clock = null;

    }
}