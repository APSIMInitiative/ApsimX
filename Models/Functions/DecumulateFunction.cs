using Models.Core;
using Models.PMF.Phen;
using System;

namespace Models.Functions
{
    /// <summary>
    /// Starts with an initial value and subtracts the value of a child
    /// function each day.
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class DecumulateFunction : Model, IFunction
    {
        /// <summary>
        /// The initial value.
        /// </summary>
        [Description("Initial value")]
        public double InitialValue { get; set; }

        /// <summary>
        /// Minimum value.
        /// </summary>
        [Description("Minimum value")]
        public double MinValue { get; set; }

        /// <summary>
        /// Optional: Name of the stage at which to start decumulation.
        /// </summary>
        [Description("Optional: Name of the stage at which to start decumulation.")]
        public string StartStageName { get; set; }

        /// <summary>
        /// Optional: Name of the phenological stage at which the value should be reset.
        /// </summary>
        [Description("Optional: Name of the phenological stage at which the value should be reset.")]
        public string ResetStageName { get; set; }

        /// <summary>
        /// Optional: Name of the phenological stage at which to stop decumulation.
        /// </summary>
        [Description("Optional: Name of the phenological stage at which to stop decumulation.")]
        public string StopStageName { get; set; }

        /// <summary>
        /// The daily value we want to track.
        /// </summary>
        private double accumulator = 0;

        /// <summary>
        /// Child function whose value will be successively subtracted from the
        /// accumulator.
        /// </summary>
        [Link(Type = LinkType.Child)]
        private IFunction child = null;

        /// <summary>
        /// Phenology.
        /// </summary>
        [Link]
        private Phenology phenology = null;

        /// <summary>
        /// Invoked at the start of simulation. Sets the accumulator to its
        /// initial value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            accumulator = InitialValue;
        }

        /// <summary>
        /// Invoked after daily phenology. Updates the accumulator.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PostPhenology")]
        private void OnPostPhenology(object sender, EventArgs e)
        {
            if (string.Equals(StartStageName, phenology.CurrentStageName, StringComparison.InvariantCultureIgnoreCase))
                accumulator = InitialValue;

            if (!string.IsNullOrWhiteSpace(ResetStageName) && string.Equals(ResetStageName, phenology.CurrentStageName, StringComparison.InvariantCultureIgnoreCase))
                accumulator = InitialValue;

            // Only decumulate if:
            //     1. Start stage is not provided or it is provided and we're past it
            // AND 2. Stop stage is not provided or it is provided and we're not past it.
            if (( string.IsNullOrWhiteSpace(StartStageName) || phenology.Stage >= phenology.StartStagePhaseIndex(StartStageName) + 1 ) &&
                ( string.IsNullOrWhiteSpace(StopStageName)  || phenology.Stage <= phenology.StartStagePhaseIndex(StopStageName) + 1 ))
            {
                accumulator -= child.Value();
                accumulator = Math.Max(accumulator, MinValue);
            }
        }

        /// <summary>Gets the value.</summary>
        /// <param name="arrayIndex">The array index (unused).</param>
        public double Value(int arrayIndex = -1)
        {
            return accumulator;
        }
    }
}
