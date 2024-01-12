using System;
using Models.Climate;
using Models.Core;
using Models.Functions;
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
    public class StartGrowthPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------
        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MovingAverageTemp = null;

        [Link]
        private Weather weather = null;

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

        /// <summary>Moving Average temperature required to start growth</summary>
        [Description("Moving average temperature to Progress")]
        public int TemptoProgress { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                double TOYfrac = (double)weather.DaysSinceWinterSolstice < 160 ? 1.0 : 0.0;
                double daysFrac = Math.Min(1.0, (double)weather.DaysSinceWinterSolstice / (double)DOYtoProgress);
                double tempFrac = Math.Min(1.0, (MovingAverageTemp.Value()/TemptoProgress));
                return Math.Min(Math.Min(TOYfrac, daysFrac), tempFrac);
            }
        }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
               // in the first half of the growth year   && Past the ealiest date to start growth              && Warm enough               
            if ((weather.DaysSinceWinterSolstice <= 160) && (weather.DaysSinceWinterSolstice >= DOYtoProgress) && (MovingAverageTemp.Value() >= TemptoProgress))
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { }
    }
}



