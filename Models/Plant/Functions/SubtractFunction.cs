using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// From the value of the first child function, subtract the values of the subsequent children functions
    /// </summary>
    [Serializable]
    [Description("From the value of the first child function, subtract the values of the subsequent children functions")]
    public class SubtractFunction : Model, Function
    {
        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function));

                double returnValue = 0.0;
                if (ChildFunctions.Count > 0)
                {
                    Function F = ChildFunctions[0] as Function;
                    returnValue = F.Value;

                    if (ChildFunctions.Count > 1)
                        for (int i = 1; i < ChildFunctions.Count; i++)
                        {
                            F = ChildFunctions[i] as Function;
                            returnValue = returnValue - F.Value;
                        }

                }
                return returnValue;
            }
        }

    }
}