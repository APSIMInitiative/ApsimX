using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Phen;
using System.Linq;

namespace Models.Functions
{
    /// <summary>Accumulates a child function between a start and end stage.
    /// </summary>
    [Serializable]
    [Description("Joins/concats the value of all children functions between start and end phases using ','")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ConcatFunction : Model, IFunction
    {
        ///Links
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>The phenology</summary>
        [Link]
        Phenology phenology = null;

        /// Private class members
        /// -----------------------------------------------------------------------------------------------------------

        private int startStageIndex;

        private int endStageIndex;

        private string AccumulatedValue = "";

        private IEnumerable<IFunction> ChildFunctions;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>The start stage name</summary>
        [Description("Stage name to start accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string StartStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("Stage name to stop accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string EndStageName { get; set; }

        /// <summary>The reset stage name</summary>
        [Description("(optional) Stage name to reset accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string ResetStageName { get; set; }

        /// <summary>The reset stage name</summary>
        [Description("(optional) Number of digits to round to")]
        public int RoundDigits { get; set; } = 4;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = "";
            startStageIndex = phenology.StartStagePhaseIndex(StartStageName);
            endStageIndex = phenology.EndStagePhaseIndex(EndStageName);
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            if (phenology.Between(startStageIndex, endStageIndex))
            {
                string DailyIncrement = "";
                foreach (IFunction function in ChildFunctions)
                {
                    if (DailyIncrement.Length > 0) 
                        DailyIncrement += ",";
                    DailyIncrement += Math.Round(function.Value(), RoundDigits).ToString();
                }

                if (AccumulatedValue.Length > 0)
                    AccumulatedValue += ",";
                AccumulatedValue += DailyIncrement;
            }
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == ResetStageName)
                AccumulatedValue = "";
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public string ValueString()
        {
            return AccumulatedValue;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return 0;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            AccumulatedValue = "";
        }
    }
}
