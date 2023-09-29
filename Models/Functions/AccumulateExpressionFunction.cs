﻿using Models.Core;
using System;

namespace Models.Functions
{
    /// <summary>
    /// Adds the value of all children functions to the previous day's accumulation between when an expression evaluates to true
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateExpressionFunction : Model, IFunction
    {
        private IBooleanFunction expressionFunction;
        private double accumulatedValue = 0;


        [Link]
        readonly ScriptCompiler compiler = null;

        [Link(Type = LinkType.Child)]
        private readonly IFunction[] childFunctions = null;


        /// <summary>Expression for when to accumulate.</summary>
        [Separator("Expression for when to accumulate child functions e.g. Canola.Phenology.Stage &lt; 4 &amp;&amp; Canola.Phenology.Stage &gt; 6")]
        [Description("Expression")]
        public string Expression { get; set; }


        /// <summary>Gets the current accumulated value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return accumulatedValue;
        }

        /// <summary>Called when simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (CSharpExpressionFunction.Compile(Expression, this, compiler, out IBooleanFunction f, out string errors))
                expressionFunction = f;
            else
                throw new Exception(errors);

            accumulatedValue = 0;
        }

        /// <summary>Called at the start of each day</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if (expressionFunction.Value())
            {
                double dailyIncrement = 0.0;
                foreach (IFunction function in childFunctions)
                    dailyIncrement += function.Value();

                accumulatedValue += dailyIncrement;
            }
        }
    }
}
