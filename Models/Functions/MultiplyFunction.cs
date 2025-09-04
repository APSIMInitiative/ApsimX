using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>A class that returns the product of its child functions.  Performance note: This function returns zero as soon as any of its child functions return zero.  Therefore, speed gains can be achieved by placing children that are likely to return zero values at the top of the list of children</summary>
    [Serializable]
    public class MultiplyFunction : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Structure.FindChildren<IFunction>().ToList();

            double returnValue = 1.0;

            foreach (IFunction F in ChildFunctions)
            {
                returnValue = returnValue * F.Value(arrayIndex);
                if (returnValue == 0.0)
                    return 0.0;
            }

            return returnValue;
        }
    }
}