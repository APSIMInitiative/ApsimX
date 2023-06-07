using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
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
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Integral = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        [EventSubscribe("Commencing")]
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
                    YesterdaysValue = Integral.Value();
                }
            }
            else
                YesterdaysValue = Integral.Value();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return Integral.Value(arrayIndex) - YesterdaysValue;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            YesterdaysValue = 0;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StageWasReset")]
        private void OnStageReset(object sender, EventArgs e)
        {
            YesterdaysValue = Integral.Value();
        }
        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"*{Name}* is the daily differential of");
        }
    }
}
