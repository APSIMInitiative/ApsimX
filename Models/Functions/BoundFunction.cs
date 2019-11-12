using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A function that returns the value of its Variable child constrained within the specified Upper and LowerBounds
    /// </summary>
    [Serializable]
    [Description("Returns the value of the Variable function, constrained within the upper and lower bounds")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BoundFunction : Model, IFunction
    {
        /// <summary>The value to be bound within the Upper and LowerBoundry</summary>
        [Link]
        IFunction Variable = null;

        /// <summary>The upper limit to constrain the variable to</summary>
        [Link]
        IFunction UpperBoundry = null;

        /// <summary>The lower limit to constrain the variable to</summary>
        [Link]
        IFunction LowerBoundry = null;
        
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (Variable.Value > UpperBoundry.Value)
                    return UpperBoundry.Value;
                else if (Variable.Value < LowerBoundry.Value)
                    return LowerBoundry.Value;
                else return Variable.Value;
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

            tags.Add(new AutoDocumentation.Paragraph("<i>" + this.Name + "</i> returns <i>Variable.Value</i> constrained within <i>UpperBoundry.Value</i> and <i>LowerBoundry.Value</i>", indent));


            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                child.Document(tags, headingLevel + 1, indent + 1);
            }
        }

    }
}
