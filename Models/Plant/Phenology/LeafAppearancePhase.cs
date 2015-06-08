using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Leaf appearance phenological phase
    /// </summary>
    [Serializable]
    [Description("This phase extends from the end of the previous phase until the end of leaf appearance.  The duration of this phase is determined by leaf appearance and the final main stem leaf number.")]
    public class LeafAppearancePhase : Phase
    {
        /// <summary>The leaf</summary>
        [Link]
        Leaf Leaf = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The cohort no at start</summary>
        private double CohortNoAtStart;
        /// <summary>The first</summary>
        private bool First = true;
        /// <summary>The remaining leaves</summary>
        private double RemainingLeaves = 0;
        /// <summary>The fraction complete yesterday</summary>
        private double FractionCompleteYesterday = 0;

        /// <summary>Reset phase</summary>
        public override void ResetPhase()
        {
            base.ResetPhase();
            CohortNoAtStart = 0;
            RemainingLeaves = 0;
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
                CohortNoAtStart = Leaf.ExpandedCohortNo;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Leaf.ExpandedCohortNo >= (int)(Structure.MainStemFinalNodeNo - RemainingLeaves))
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
                CohortNoAtStart = Leaf.ExpandedCohortNo;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Leaf.ExpandedCohortNo >= (int)(Structure.MainStemFinalNodeNo - RemainingLeaves))
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
                double F = (Leaf.ExpandedNodeNo - CohortNoAtStart) / ((Structure.MainStemFinalNodeNo - RemainingLeaves) - CohortNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
        }


    }
}

      
      
