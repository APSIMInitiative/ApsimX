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
    /// its duration is determined by leaf appearance rate and the number of leaves to
    /// complete the phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class SimpleLeafAppearancePhase : Model, IPhase
    {

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction targetLeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction currentLeafNumber = null;

        private double LeafNoAtStart;
        private bool First = true;
        private double FractionCompleteYesterday = 0;
        private double TargetLeafForCompletion = 0;

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
                double F = 0;
                F = (currentLeafNumber.Value() - LeafNoAtStart) / TargetLeafForCompletion;
                F = MathUtilities.Bound(F, 0, 1);
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
        }

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (First)
            {
                LeafNoAtStart = currentLeafNumber.Value();
                TargetLeafForCompletion = targetLeafNumber.Value() - LeafNoAtStart;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (FractionComplete >= 1)
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;  //assumes we use most of the Tt today to get to final leaf.  Should be calculated as a function of the phyllochron
            }

            return proceedToNextPhase;
        }

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            LeafNoAtStart = 0;
            FractionCompleteYesterday = 0;
            TargetLeafForCompletion = 0;
            First = true;
        }

        //7. Private methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}
