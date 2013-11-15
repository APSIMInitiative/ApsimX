using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)")]
    public class SigmoidFunction2 : Function
    {
        public Function Ymax { get; set; }
        public Function XValue { get; set; }

        public double Xo = 1.0;
        public double b = 1.0;


        
        public override double FunctionValue
        {
            get
            {

                try
                {
                    return Ymax.FunctionValue * 1 / (1 + Math.Exp(-(XValue.FunctionValue - Xo) / b));
                }
                catch (Exception E)
                {
                    throw new Exception("Error with values to Sigmoid function");
                }
            }
        }

    }
}
