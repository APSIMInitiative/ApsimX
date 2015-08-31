using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// This function returns the daily delta for its child function
    /// </summary>
    [Serializable]
    [Description("Stores the value of its child function (called Integral) from yesterday and returns the difference between that and todays value of the child function")]
    public class DeltaFunction : Model, IFunction
    {
        //Class members
        /// <summary>The accumulated value</summary>
        private double YesterdaysValue = 0;

        /// <summary>The start stage name</summary>
        [Description("StartStageName")]
        public string StartStageName { get; set; }

        /// <summary>The child function to return a delta for</summary>
        [Link]
        IFunction Integral = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            YesterdaysValue = 0;
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (StartStageName != null) //For functions that don't start giving values on the first day of simulation and don't have zero as their first value we need to set a start stage so the first values is picked up on the correct day
            {
                if (Phenology.Beyond(StartStageName))
                {
                    YesterdaysValue = Integral.Value;
                }
            }
            else
                YesterdaysValue = Integral.Value;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return Integral.Value - YesterdaysValue;
            }
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            YesterdaysValue = 0;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            //Write what the function is returning
            tags.Add(new AutoDocumentation.Paragraph("*" + this.Name + "* is the daily differential of", indent));

            // write a description of the child it is returning the differential of.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                    child.Document(tags, headingLevel + 1, indent+1);
            }
        }
    }
}
