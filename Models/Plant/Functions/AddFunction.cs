using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Add the values of all child functions")]
    public class AddFunction : Function
    {
        
        public override double Value
        {
            get
            {
                double returnValue = 0.0;

                foreach (Function F in this.Models)
                {
                    returnValue = returnValue + F.Value;
                }

                return returnValue;
            }
        }

    }

}