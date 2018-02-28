// -----------------------------------------------------------------------
// <copyright file="HoldFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using Models.PMF.Phen;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Returns the a value which is updated daily until a given stage is reached, beyond which it is held constant
    /// </summary>
    [Serializable]
    [Description("Returns the ValueToHold which is updated daily until the WhenToHold stage is reached, beyond which it is held constant")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class HoldFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>Value to return</summary>
        private double[] returnValue;

        /// <summary>The value to hold after event</summary>
        [ChildLink]
        private IFunction valueToHold = null;

        /// <summary>The phenology</summary>
        [Link]
        private Phenology phenologyModel = null;

        /// <summary>The set event</summary>
        [Description("Phenological stage at which value stops updating and is held constant")]
        public string WhenToHold { get; set; }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            returnValue = valueToHold.Values();
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("DoUpdate")]
        private void OnDoUpdate(object sender, EventArgs e)
        {
            if (phenologyModel.Beyond(WhenToHold))
            {
                //Do nothing, hold value constant
            }
            else
                returnValue = valueToHold.Values();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double[] Values()
        {
            return returnValue;
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
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
                // Describe the function
                if (valueToHold != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " is the same as " + (valueToHold as IModel).Name + " until it reaches " + WhenToHold + " stage when it fixes its value", indent));
                }
                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }
    }

}