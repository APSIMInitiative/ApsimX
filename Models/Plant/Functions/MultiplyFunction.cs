using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    /// <summary>
    /// Multiplies the values of the children of this node
    /// </summary>
    [Description("Returns the product of all childern function values")]
    public class MultiplyFunction : Function
    {
        
        public override double FunctionValue
        {
            get
            {
                double returnValue = 1.0;

                foreach (Function F in this.Models)
                    returnValue = returnValue * F.FunctionValue;
                return returnValue;
            }
        }

    }
}