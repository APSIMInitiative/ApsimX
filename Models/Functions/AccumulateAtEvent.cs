using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous day's accumulation between start and end phases")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateAtEvent : Model, IFunction, ICustomDocumentation
    {
        ///Links
        /// -----------------------------------------------------------------------------------------------------------
        
        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology phenology = null;

        /// Private fields
        /// -----------------------------------------------------------------------------------------------------------

        private double accumulatedValue = 0;

        private IEnumerable<IFunction> childFunctions;

        private int startStageIndex;

        private int endStageIndex;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>The start stage name</summary>
        [Description("Stage name to start accumulation")]
        public string StartStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("Stage name to stop accumulation")]
        public string EndStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("Event name to accumulate")]
        public string AccumulateEventName { get; set; }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return accumulatedValue;
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
                tags.Add(new AutoDocumentation.Paragraph("**" + this.Name + "** is a daily accumulation of the values of functions listed below between the " + StartStageName + " and "
                                                            + EndStageName + " stages.  Function values added to the accumulate total each day are:", indent));

                // write children.
                foreach (IModel child in this.FindAllChildren<IModel>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }

        ///7. Private methods
        /// -----------------------------------------------------------------------------------------------------------
        
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            accumulatedValue = 0;

            events.Subscribe(AccumulateEventName, OnCalcEvent);

            startStageIndex = phenology.StartStagePhaseIndex(StartStageName);
            endStageIndex = phenology.EndStagePhaseIndex(EndStageName);
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        private void OnCalcEvent(object sender, EventArgs e)
        {
            if (childFunctions == null)
                childFunctions = FindAllChildren<IFunction>().ToList();

            if (phenology.Between(startStageIndex, endStageIndex))
            {
                double DailyIncrement = 0.0;
                foreach (IFunction function in childFunctions)
                {
                    DailyIncrement += function.Value();
                }

                accumulatedValue += DailyIncrement;
            }
        }



        
    }

}
