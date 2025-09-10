using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>This class calculates the minimum of all child functions.</summary>
    [Serializable]
    [Description("Returns the Minimum value of all children functions")]
    public class MinimumFunction : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Structure.FindChildren<IFunction>().ToList();

            double ReturnValue = 999999999;
            foreach (IFunction F in ChildFunctions)
            {
                ReturnValue = Math.Min(ReturnValue, F.Value(arrayIndex));
            }
            return ReturnValue;
        }
    }
}