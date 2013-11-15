using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Multiplies the values of the children of this node
    /// </summary>
    [Description("Returns the product of all childern function values")]
    public class MultiplyFunction : Function
    {
        public List<Function> Children { get; set; }
        public override double FunctionValue
        {
            get
            {
                double returnValue = 1.0;

                foreach (Function F in Children)
                    returnValue = returnValue * F.FunctionValue;
                return returnValue;
            }
        }

    }
}