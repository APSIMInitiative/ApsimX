using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>A class that returns the sum of its child functions.</summary>

    [Serializable]
    [Description("Add the values of all child functions")]
    public class AddFunction : Model, IFunction, IStructureDependency
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

            double returnValue = 0.0;

            foreach (IFunction F in ChildFunctions)
                returnValue = returnValue + F.Value(arrayIndex);

            return returnValue;
        }
    }

}