using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Returns the maximum value of all childern functions")]
    class MaximumFunction : Function
    {
        
        public override double Value
        {
            get
            {
                double ReturnValue = -999999999;
                foreach (Function F in this.Models)
                {
                    ReturnValue = Math.Max(ReturnValue, F.Value);
                }
                return ReturnValue;
            }
        }

    }
}