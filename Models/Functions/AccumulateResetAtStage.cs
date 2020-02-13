using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions, and reset to zero each time the specisified stage is passed.
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous day's accumulation and reset to zero each time the specisified stage is passed")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateResetAtStage : Model, IFunction, ICustomDocumentation
    {        
        /// Private class members
        /// -----------------------------------------------------------------------------------------------------------
                   
        private double AccumulatedValue = 0;

        private List<IModel> ChildFunctions;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>The reset stage name</summary>
        [Description("(optional) Stage name to reset accumulation")]
        public string ResetStageName { get; set; }

        
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = 0;          
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (ChildFunctions == null)
                ChildFunctions = Apsim.Children(this, typeof(IFunction));

                double DailyIncrement = 0.0;
                foreach (IFunction function in ChildFunctions)
                {
                    DailyIncrement += function.Value();
                }

                AccumulatedValue += DailyIncrement;           
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == ResetStageName)
                AccumulatedValue = 0.0;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return AccumulatedValue;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
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
                tags.Add(new AutoDocumentation.Paragraph("**" + this.Name + "** is a daily accumulation of the values of functions listed below " +
                    " and set to zero each time the "+ ResetStageName + " is passed.", indent));

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }
    }
}
