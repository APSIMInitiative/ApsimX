using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Functions
{
    /// <summary>
    /// Sets return to the value of the first child function then recurses through each subsequent child setting return to return/child[n].value.
    /// Protected so does not return null values.  
    /// Returns NumeratorErrValue if the value of the first child function is zero
    /// Returns DemonimatorErrValue if any of the subsequent children functions have a value of zero.
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    [Description("Recursively divide child functions with configurable protection against zero numerators and denominators")]
    public class ProtectedDivideFunction : Model, IFunction
    {
        /// <summary>Value to return if the running numerator is zero.</summary>
        [Description("Value to return if numerator is 0:")]
        public double NumeratorErrVal { get; set; } = 0.0;

        /// <summary>Value to return if a denominator is zero.</summary>
        [Description("Value to return if denominator is 0:")]
        public double DenominatorErrVal { get; set; } = 0;

        private List<IFunction> ChildFunctions;

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

            if (ChildFunctions.Count <= 1)
                throw new Exception("Protected divide function must have at least two child functions");

            double returnValue = ChildFunctions[0].Value(arrayIndex);

            // Protect initial numerator
            if (MathUtilities.FloatsAreEqual(returnValue, 0.0))
                return NumeratorErrVal;

            for (int i = 1; i < ChildFunctions.Count; i++)
            {
                double denominator = ChildFunctions[i].Value(arrayIndex);

                if (MathUtilities.FloatsAreEqual(denominator, 0.0))
                    return DenominatorErrVal;

                returnValue /= denominator;
            }

            return returnValue;
        }
    }
}
