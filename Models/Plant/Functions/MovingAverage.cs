using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;
using APSIM.Shared.Utilities;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Maintains a moving average of a given value for a user-specified number of simulation days")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class MovingAverageFunction : Model, IFunction
    {
        /// <summary>The number of days over which to calculate the moving average</summary>
        [Description("Number of Days")]
        public int NumberOfDays { get; set; }

        /// <summary>Initial value of the moving average</summary>
        [Description("Initial Value")]
        public double InitialValue { get; set; }

        /// <summary>The accumulated value</summary>
        private List<double> AccumulatedValues = new List<double>();

        /// <summary>The child functions</summary>
        private IFunction ChildFunction
        {
            get
            {
                IFunction value;
                List<IModel> ChildFunctions = Apsim.Children(this, typeof(IFunction));
                if (ChildFunctions.Count == 1)
                    value = ChildFunctions[0] as IFunction;
                else
                    throw new ApsimXException(this, "Moving average function " + this.Name + " must only have one child node.");
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
                AccumulatedValues.Add(InitialValue);
         }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("EndOfDay")]
        private void EndOfDay(object sender, EventArgs e)
        {
            AccumulatedValues.RemoveAt(0);
            AccumulatedValues.Add(ChildFunction.Value);
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (NumberOfDays == 0)
                    throw new ApsimXException(this, "Number of days for moving average cannot be zero in function " + this.Name);
                return MathUtilities.Sum(AccumulatedValues)/NumberOfDays;
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            Name = this.Name;
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
            tags.Add(new AutoDocumentation.Paragraph(Name + " is calculated from a moving average of " + (ChildFunction as IModel).Name + " over a series of " + NumberOfDays.ToString()+" days.", indent));

            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                child.Document(tags, headingLevel + 1, indent + 1);
        }

    }
}
