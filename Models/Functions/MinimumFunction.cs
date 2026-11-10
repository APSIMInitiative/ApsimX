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
    public class MinimumFunction : Model, IFunction
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

            double ReturnValue = 999999999;
            foreach (IFunction F in ChildFunctions)
            {
                ReturnValue = Math.Min(ReturnValue, F.Value(arrayIndex));
            }
            return ReturnValue;
        }
    }
}