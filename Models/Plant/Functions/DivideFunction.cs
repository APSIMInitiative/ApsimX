using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("Starting with the first child function, recursively divide by the values of the subsequent child functions")]
    public class DivideFunction : Function
    {
        
        public override double FunctionValue
        {
            get
            {
                double returnValue = 0.0;
                object[] Children = this.Models;
                if (Children.Length > 0)
                {
                    Function F = Children[0] as Function;
                    returnValue = F.FunctionValue;

                    if (Children.Length > 1)
                        for (int i = 1; i < Children.Length; i++)
                        {
                            F = Children[i] as Function;
                            returnValue = returnValue / F.FunctionValue;
                        }

                }
                return returnValue;
            }
        }

    }
}