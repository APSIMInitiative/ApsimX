using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("returns the specified value")]
    public class Constant : Function
    {
        public string Value { get; set; }
        
        public override double FunctionValue { get { return Convert.ToDouble(Value); } }
        public override string ValueString { get { return Value; } }
    }
}