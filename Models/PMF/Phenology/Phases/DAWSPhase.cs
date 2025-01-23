using System;
using Models.Climate;
using Models.Core;
using Newtonsoft.Json;


namespace Models.PMF.Phen
{
    /// <summary>
    /// It proceeds until the last leaf on the main-stem has fully senessced.  Therefore its duration depends on the number of main-stem leaves that are produced and the rate at which they seness following final leaf appearance.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class DAWSPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Weather met = null;

        [Link]
        private IClock clock = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        private int StartDAWS = 0;
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
        public int DAWStoProgress { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                return Math.Min(1.0, ((double)met.DaysSinceWinterSolstice - (double)StartDAWS) / ((double)DAWStoProgress - (double)StartDAWS));
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
                
                if (clock.Today.DayOfYear < met.WinterSolsticeDOY)
                {
                    if (DateTime.IsLeapYear(clock.Today.Year))
                        StartDAWS = 366 - met.WinterSolsticeDOY + clock.Today.DayOfYear - 1;
                    else
                        StartDAWS = 365 - met.WinterSolsticeDOY + clock.Today.DayOfYear - 1;
                }
                else
                    StartDAWS = clock.Today.DayOfYear - met.WinterSolsticeDOY;

                First = false;
            }
            
            if (((met.DaysSinceWinterSolstice >= DAWStoProgress) && (met.DaysSinceWinterSolstice < 365))||
                ((met.DaysSinceWinterSolstice == 0) && (DAWStoProgress >= 365)))
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
            StartDAWS = 0;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { ResetPhase(); }
    }
}



