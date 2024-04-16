using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Maintains a moving sum of a given value for a user-specified number of simulation days")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class MovingSumFunction : Model, IFunction
    {
        /// <summary>The number of days over which to calculate the moving sum</summary>
        [Description("Number of Days")]
        public int NumberOfDays { get; set; }


        /// <summary>The accumulated value</summary>
        private List<double> AccumulatedValues = new List<double>();

        /// <summary>The child functions</summary>
        private IFunction ChildFunction
        {
            get
            {
                IFunction value;
                IEnumerable<IFunction> ChildFunctions = FindAllChildren<IFunction>();
                if (ChildFunctions.Count() == 1)
                    value = ChildFunctions.First();
                else
                    throw new ApsimXException(this, "Moving sum function " + this.Name + " must only have one child node.");
                return value;
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            for (int i = 0; i < NumberOfDays; i++)
                AccumulatedValues.Add(0);
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("EndOfDay")]
        private void EndOfDay(object sender, EventArgs e)
        {
            AccumulatedValues.RemoveAt(0);
            AccumulatedValues.Add(ChildFunction.Value());
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (NumberOfDays == 0)
                throw new ApsimXException(this, "Number of days for moving sum cannot be zero in function " + this.Name);
            return MathUtilities.Sum(AccumulatedValues);
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            if (FindAllChildren<IFunction>().Count() == 1)
                yield return new Paragraph($"{Name} is calculated from a moving sum of {ChildFunction}.Name over a series of {NumberOfDays} days.");
        }
    }
}
