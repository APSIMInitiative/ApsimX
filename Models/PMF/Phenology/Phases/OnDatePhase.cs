using System;
using Models.Climate;
using Models.Core;
using Newtonsoft.Json;


namespace Models.PMF.Phen
{
    /// <summary>
    /// Proceeds On a specific day of the year.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class OnDatePhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IClock clock = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        private int StartDOY = 0;
        private bool First = true;
        
        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Days after winter solstice to progress from phase</summary>
        [Description("DAWStoProgress")]
        public int DOYtoProgress { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                return Math.Min(1.0, ((double)clock.Today.DayOfYear - (double)StartDOY) / ((double)DOYtoProgress - (double)StartDOY));
            }
        }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            if (First)
            {
                StartDOY = clock.Today.DayOfYear;
                First = false;
            }

            if (clock.Today.DayOfYear >= DOYtoProgress) 
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            First = true;
            StartDOY = 0;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { ResetPhase(); }
    }
}



