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
    public class PhaseLookup : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            FunctionUtilities.ValidateFunctionChildren(Node);
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Node.FindChildren<IFunction>().ToList();

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
