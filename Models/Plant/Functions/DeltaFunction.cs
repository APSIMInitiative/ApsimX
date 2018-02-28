// -----------------------------------------------------------------------
// <copyright file="DeltaFunction.cs" company="APSIM Initiative">
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
    /// This function returns the daily delta for its child function
    /// </summary>
    [Serializable]
    [Description("Stores the value of its child function (called Integral) from yesterday and returns the difference between that and todays value of the child function")]
    public class DeltaFunction : BaseFunction, ICustomDocumentation
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The child function to return a delta for</summary>
        [ChildLinkByName]
        private IFunction Integral = null;

        /// <summary>The phenology</summary>
        [Link]
        private Phenology Phenology = null;
        
        /// <summary>The accumulated value</summary>
        private double yesterdaysValue = 0;

        /// <summary>The start stage name</summary>
        [Description("StartStageName")]
        public string StartStageName { get; set; }

        [EventSubscribe("SimulationCommencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            yesterdaysValue = 0;
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (StartStageName != null) //For functions that don't start giving values on the first day of simulation and don't have zero as their first value we need to set a start stage so the first values is picked up on the correct day
            {
                if (Phenology.Beyond(StartStageName))
                {
                    yesterdaysValue = Integral.Value();
                }
            }
            else
                yesterdaysValue = Integral.Value();
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            returnValue[0] = Integral.Value() - yesterdaysValue;
            return returnValue;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            yesterdaysValue = 0;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PhaseRewind")]
        private void OnPhaseRewind(object sender, EventArgs e)
        {
            yesterdaysValue = Integral.Value();
        }
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                //Write what the function is returning
                tags.Add(new AutoDocumentation.Paragraph("*" + this.Name + "* is the daily differential of", indent));

                // write a description of the child it is returning the differential of.
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }
    }
}
