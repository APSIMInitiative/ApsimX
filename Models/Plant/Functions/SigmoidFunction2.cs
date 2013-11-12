using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)")]
    public class SigmoidFunction2 : Function
    {
        [Link]
        public Function Ymax = null;
        [Link]
        public Function XValue = null;

        public double Xo = 1.0;
        public double b = 1.0;


        
        public override double Value
        {
            get
            {

                try
                {
                    return Ymax.Value * 1 / (1 + Math.Exp(-(XValue.Value - Xo) / b));
                }
                catch (Exception E)
                {
                    throw new Exception("Error with values to Sigmoid function");
                }
            }
        }

    }
}
