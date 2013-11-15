using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Returns the maximum value of all childern functions")]
    class MaximumFunction : Function
    {
        public List<Function> Children { get; set; }

        public override double FunctionValue
        {
            get
            {
                double ReturnValue = -999999999;
                foreach (Function F in Children)
                {
                    ReturnValue = Math.Max(ReturnValue, F.FunctionValue);
                }
                return ReturnValue;
            }
        }

    }
}