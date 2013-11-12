using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Raises the value of the child to the power of the exponent specified")]
    public class PowerFunction : Function
    {
        public double Exponent = 1.0;

        
        public override double Value
        {
            get
            {
                object[] Children = this.Models;
                if (Children.Length == 1)
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
