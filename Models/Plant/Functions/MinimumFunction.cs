using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Returns the Minimum value of all children functions")]
    class MinimumFunction : Function
    {
        
        public override double Value
        {
            get
            {
                double ReturnValue = 999999999;
                foreach (Function F in this.Models)
                {
                    ReturnValue = Math.Min(ReturnValue, F.Value);
                }
                return ReturnValue;
            }
        }

    }
}