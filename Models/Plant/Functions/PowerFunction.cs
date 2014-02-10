using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Raises the value of the child to the power of the exponent specified")]
    public class PowerFunction : Function
    {
        public double Exponent = 1.0;

        private List<Function> Children { get { return ModelsMatching<Function>(); } }
        public override double Value
        {
            get
            {
                if (Children.Count == 1)
                {
                    Function F = Children[0] as Function;
                    return Math.Pow(F.Value, Exponent);
                }
                else
                {
                    throw new Exception("Power function must have only one argument");
                }
            }
        }

    }
}
