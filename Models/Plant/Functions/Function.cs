using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Base class from which other functions inherit")]
    abstract public class Function: Model
    {
        abstract public double Value { get; }
        virtual public string ValueString { get { return Value.ToString(); } }
        virtual public double[] Values { get { return new double[1] { Value }; } }

        virtual public void UpdateVariables(string initial) { }

        [Link]
        public WeatherFile MetData = null;

        [Link]
        public Clock Clock = null;

    }
}