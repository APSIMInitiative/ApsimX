using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Organs;

namespace Models.Plant.Phen
{
    public class LeafAppearancePhase : Phase
    {
        [Link]
        Leaf Leaf = null;

        [Link]
        Structure Structure = null;

        private double CohortNoAtStart;
        bool First = true;

        public double RemainingLeaves = 0;

        private double FractionCompleteYesterday = 0;

        /// <summary>
        /// Reset phase
        /// </summary>
        public override void ResetPhase()
        {
            _TTinPhase = 0;
            CohortNoAtStart = 0;
        }

        /// <summary>
        /// Do our timestep development
        /// </summary>
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

        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
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

      
      
