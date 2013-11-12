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
        public string k = "0";
        
        public override double Value { get { return Convert.ToDouble(k); } }
        public override string ValueString { get { return k; } }
    }
}