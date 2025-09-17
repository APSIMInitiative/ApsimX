using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Functions
{
    /// <summary>A class that divides all child functions.</summary>
    /// <remarks>Returns zero if nominator is zero, returns double.maxValue if denominator is zero.</remarks>
    [Serializable]
    [Description("Starting with the first child function, recursively divide by the values of the subsequent child functions")]
    public class DivideFunction : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The child functions</summary>
        private List<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Structure.FindChildren<IFunction>().ToList();

            double returnValue = 0.0;
            if (ChildFunctions.Count > 0)
            {
                IFunction F = ChildFunctions[0];
                returnValue = F.Value(arrayIndex);

                if ((returnValue != 0.0) && (ChildFunctions.Count > 1))
                {
                    for (int i = 1; i < ChildFunctions.Count; i++)
                    {
                        F = ChildFunctions[i];
                        double denominator = F.Value(arrayIndex);
                        if (denominator == 0.0)
                        {
                            if (returnValue < 0.0)
                            {
                                returnValue = double.NegativeInfinity;
                            }
                            else
                            {
                                returnValue = double.PositiveInfinity;
                            }
                        }
                        else
                        {
                            returnValue = MathUtilities.Divide(returnValue, denominator, 0.0);
                        }
                    }
                }
            }

            return returnValue;
        }
    }
}
