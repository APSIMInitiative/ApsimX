using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Look up a value based upon the current growth phase.
    /// </summary>
    [Serializable]
    [Description("A value is chosen according to the current growth phase.")]
    public class PhaseLookup : Model, IFunction, IStructureDependency
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

            foreach (IFunction F in ChildFunctions)
            {
                PhaseLookupValue P = F as PhaseLookupValue;
                if (P.InPhase)
                    return P.Value(arrayIndex);
            }
            return 0;  // Default value is zero
        }
    }
}
