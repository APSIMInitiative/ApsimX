using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a exponential of the form y = A * B * exp(x * C)")]
    public class ExponentialFunction : Function
    {
        public double A = 1.0;
        public double B = 1.0;
        public double C = 1.0;
        private Model[] ChildFunctions;

        
        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Children.MatchingMultiple(typeof(Function));

                if (ChildFunctions.Length == 1)
                {
                    Function F = ChildFunctions[0] as Function;

                    return A + B * Math.Exp(C * F.Value);
                }
                else
                {
                    throw new Exception("Sigmoid function must have only one argument");
                }
            }
        }

    }
}
