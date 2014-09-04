using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Starting with the first child function, recursively divide by the values of the subsequent child functions
    /// </summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Starting with the first child function, recursively divide by the values of the subsequent child functions. Return 0 if no child. The value of first child if only one child.
    /// <remarks>
    /// </remarks>
    [Serializable]
    [Description("Starting with the first child function, recursively divide by the values of the subsequent child functions")]
    public class DivideFunction : Function
    {
        private Model[] ChildFunctions;

        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Children.MatchingMultiple(typeof(Function));

                double returnValue = 0.0;
                if (ChildFunctions.Length > 0)
                {
                    Function F = ChildFunctions[0] as Function;
                    returnValue = F.Value;

                    if (ChildFunctions.Length > 1)
                        for (int i = 1; i < ChildFunctions.Length; i++)
                        {
                            F = ChildFunctions[i] as Function;
                            returnValue = returnValue / F.Value;
                        }

                }
                return returnValue;
            }
        }

    }
}