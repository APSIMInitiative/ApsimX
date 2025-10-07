using System;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage and
    /// it continues until the final main-stem leaf has finished expansion.
    /// The duration of this phase is determined by leaf appearance rate (Structure.Phyllochron)
    /// and the number of leaves produced on the mainstem (Structure.FinalLeafNumber).
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class LeafAppearancePhase : Model, IPhase, IPhaseWithSetableCompletionDate
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FinalLeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction LeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FullyExpandedLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialisedLeafNumber = null;

        [Link]
        private IClock clock = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        private double leafNoAtStart;
        /// <summary>Flag for the first day of this phase</summary>
        private bool first = true;
        private double fractionCompleteYesterday = 0;
        private double targetLeafForCompletion = 0;
        /// <summary>First date in this phase</summary>
        private DateTime firstDate { get; set; }

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                if (String.IsNullOrEmpty(DateToProgress))
                {
                    double F = 0;
                    F = (LeafNumber.Value() - leafNoAtStart) / targetLeafForCompletion;
                    F = MathUtilities.Bound(F, 0, 1);
                    return Math.Max(F, fractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
                }
                else 
                {
                    double dayDurationOfPhase = (DateUtilities.GetDate(DateToProgress) - firstDate).Days;
                    double daysInPhase = (clock.Today - firstDate).Days;
                    return daysInPhase / dayDurationOfPhase;
                }
            }
        }

        /// <summary>Data to progress.  Is empty by default.  If set by external model, phase will ignore its mechanisum and wait for the specified date to progress</summary>
        [JsonIgnore]
        public string DateToProgress { get; set; } = "";

        //6. Public method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (String.IsNullOrEmpty(DateToProgress))
            {
                if (first)
                {
                    leafNoAtStart = LeafNumber.Value();
                    targetLeafForCompletion = FinalLeafNumber.Value() - leafNoAtStart;
                    first = false;
                }

                fractionCompleteYesterday = FractionComplete;

                //if (leaf.ExpandedCohortNo >= (leaf.InitialisedCohortNo))
                if (FullyExpandedLeafNo.Value() >= InitialisedLeafNumber.Value())
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = 0.00001;  //assumes we use most of the Tt today to get to final leaf.  Should be calculated as a function of the phyllochron
                }
            }
            else
            {
                if (first)
                {
                    firstDate = clock.Today;
                    first = false;
                }
                if (DateUtilities.DatesAreEqual(DateToProgress, clock.Today))
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = 1;
                }
            }

            return proceedToNextPhase;
        }

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            leafNoAtStart = 0;
            fractionCompleteYesterday = 0;
            targetLeafForCompletion = 0;
            first = true;
            DateToProgress = "";
        }

        //7. Private methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { ResetPhase(); }
    }
}
