using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)")]
    public class SigmoidFunction : Function
    {
        public double Xmax = 1.0;
        public double Xo = 1.0;
        public double b = 1.0;
        private List<IModel> ChildFunctions;

        
        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function));

                if (ChildFunctions.Count == 1)
                {
                    Function F = ChildFunctions[0] as Function;

                    return Xmax * 1 / (1 + Math.Exp(-(F.Value - Xo) / b));
                }
                else
                {
                    throw new Exception("Sigmoid function must have only one argument");
                }
            }
        }

    }
}
