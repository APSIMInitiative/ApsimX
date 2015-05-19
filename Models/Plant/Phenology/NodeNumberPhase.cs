using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Functions;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Leaf appearance phenological phase
    /// </summary>
    [Serializable]
    [Description("This phase extends from the end of the previous phase until the Completion Leaf Number is achieved.  The duration of this phase is determined by leaf appearance rate and the completion leaf number target.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PhasePresenter")]
    public class NodeNumberPhase : Phase
    {
        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The Number of main-stem leave appeared when the phase ends</summary>
        [Link]
        private IFunction CompletionNodeNumber = null;

        /// <summary>The cohort no at start</summary>
        private double CohortNoAtStart;
        /// <summary>The first</summary>
        private bool First = true;
        /// <summary>The fraction complete yesterday</summary>
        private double FractionCompleteYesterday = 0;

        /// <summary>Reset phase</summary>
        public override void ResetPhase()
        {
            base.ResetPhase();
            CohortNoAtStart = 0;
            FractionCompleteYesterday = 0;
            First = true;
        }

        /// <summary>Do our timestep development</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            base.DoTimeStep(PropOfDayToUse);

            if (First)
            {
                CohortNoAtStart = Structure.MainStemNodeNo;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Structure.MainStemNodeNo >= CompletionNodeNumber.Value)
                return 0.00001;
            else
                return 0;
        }

        // Return proportion of TT unused
        /// <summary>Adds the tt.</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double AddTT(double PropOfDayToUse)
        {
            base.AddTT(PropOfDayToUse);
            if (First)
            {
                CohortNoAtStart = Structure.MainStemNodeNo;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Structure.MainStemNodeNo >= CompletionNodeNumber.Value)
                return 0.00001;
            else
                return 0;
        }

        /// <summary>Return a fraction of phase complete.</summary>
        /// <value>The fraction complete.</value>
        public override double FractionComplete
        {
            get
            {
                double F = (Structure.MainStemNodeNo - CohortNoAtStart) / (CompletionNodeNumber.Value - CohortNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
        }


    }
}

      
      
