using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// 
    /// </summary>
    [Description("From the value of the first child function, subtract the values of the subsequent children functions")]
    public class SubtractFunction : Function
    {
        public List<Function> Children { get; set; }
        public override double FunctionValue
        {
            get
            {
                double returnValue = 0.0;
                if (Children.Count > 0)
                {
                    Function F = Children[0] as Function;
                    returnValue = F.FunctionValue;

                    if (Children.Count > 1)
                        for (int i = 1; i < Children.Count; i++)
                        {
                            F = Children[i] as Function;
                            returnValue = returnValue - F.FunctionValue;
                        }

                }
                return returnValue;
            }
        }

    }
}