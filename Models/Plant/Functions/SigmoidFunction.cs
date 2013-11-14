using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)")]
    public class SigmoidFunction : Function
    {
        public double Xmax = 1.0;
        public double Xo = 1.0;
        public double b = 1.0;


        
        public override double FunctionValue
        {
            get
            {
                object[] Children = this.Models;
                if (Children.Length == 1)
                {
                    Function F = Children[0] as Function;

                    return Xmax * 1 / (1 + Math.Exp(-(F.FunctionValue - Xo) / b));
                }
                else
                {
                    throw new Exception("Sigmoid function must have only one argument");
                }
            }
        }

    }
}
