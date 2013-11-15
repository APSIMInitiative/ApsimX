using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Returns the Minimum value of all children functions")]
    class MinimumFunction : Function
    {
        public List<Function> Children { get; set; }

        public override double FunctionValue
        {
            get
            {
                double ReturnValue = 999999999;
                foreach (Function F in Children)
                {
                    ReturnValue = Math.Min(ReturnValue, F.FunctionValue);
                }
                return ReturnValue;
            }
        }

    }
}