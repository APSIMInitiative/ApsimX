using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Returns the value of a nominated internal Plant2 numerical variable")]
    public class VariableReference : Function
    {
        public string VariableName = "";

        
        public override double FunctionValue
        {
            get
            {
                return Convert.ToDouble(ExpressionFunction.Evaluate(VariableName.Trim(), this));
            }
        }

    }
}