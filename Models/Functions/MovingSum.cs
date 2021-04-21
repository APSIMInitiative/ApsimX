using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;
using APSIM.Shared.Utilities;
using System.Linq;

namespace Models.Functions
{
    /// <summary>
    /// # [Name]
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Maintains a moving sum of a given value for a user-specified number of simulation days")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class MovingSumFunction : Model, IFunction, ICustomDocumentation
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

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                Name = this.Name;
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
                if (FindAllChildren<IFunction>().Count() == 1)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " is calculated from a moving sum of " + (ChildFunction as IModel).Name + " over a series of " + NumberOfDays.ToString() + " days.", indent));

                // write children.
                foreach (IModel child in this.FindAllChildren<IModel>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }
    }
}
