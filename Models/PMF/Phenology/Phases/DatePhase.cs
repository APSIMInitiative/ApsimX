using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using NetTopologySuite.Mathematics;
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
    public class DatePhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IClock clock = null;

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

        /// <summary>Date to progress (dd-MMM)</summary>
        [Description("Date to Progress")]
        public string DateToProgress { get; set; }

        private DateTime StartDate { get; set; }
        private DateTime EndDate { get; set; }
        private int duration { get; set; }
        private int progress { get; set; }
        private bool FirstDay = true;

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                return MathUtilities.Divide(progress , duration,0);
            }
        }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            if (FirstDay)
            {
                StartDate = clock.Today;
                int yearToProgress = clock.Today.Year;
                if (DateUtilities.CompareDates(DateToProgress + "-" + clock.Today.Year.ToString(), clock.Today) > 0)
                    yearToProgress += 1;
                EndDate = DateUtilities.GetDate(DateToProgress, yearToProgress);
                if (EndDate < StartDate)
                    throw new Exception("End date before start date");
                duration = (EndDate - StartDate).Days;
                FirstDay = false;
            }
            progress = (clock.Today - StartDate).Days;
            bool progressToday = DateUtilities.DatesAreEqual(DateToProgress, clock.Today);
            if (progressToday)
            {
                propOfDayToUse = 0.00001;
                FirstDay = true;
            }
            return progressToday;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            FirstDay = true;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { ResetPhase(); }
    }
}



