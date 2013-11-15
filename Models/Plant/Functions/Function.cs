using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Base class from which other functions inherit")]
    abstract public class Function: Model
    {
        abstract public double FunctionValue { get; }
        virtual public string ValueString { get { return FunctionValue.ToString(); } }
        virtual public double[] Values { get { return new double[1] { FunctionValue }; } }

        virtual public void UpdateVariables(string initial) { }

        [Link]
        protected WeatherFile MetData = null;

        [Link]
        protected Clock Clock = null;

    }
}