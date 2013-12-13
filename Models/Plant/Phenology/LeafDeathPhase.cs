using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;

namespace Models.PMF.Phen
{
    [Serializable]
    public class LeafDeathPhase : Phase
    {
        [Link]
        private Leaf Leaf = null;

        [Link]
        Structure Structure = null;

        private double DeadNodeNoAtStart = 0;
        private bool First = true;

        public override void ResetPhase()
        {
            base.ResetPhase();
            DeadNodeNoAtStart = 0;
            First = true;
        }
        /// <summary>
        /// Do our timestep development
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            base.DoTimeStep(PropOfDayToUse);

            if (First)
            {
                DeadNodeNoAtStart = Leaf.DeadCohortNo;
                First = false;
            }

            if ((Leaf.DeadCohortNo >= Structure.MainStemFinalNodeNo) || (Leaf.CohortsInitialised == false))
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
                double F = (Leaf.DeadCohortNo - DeadNodeNoAtStart) / (Structure.MainStemFinalNodeNo - DeadNodeNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return F;
            }
        }

    }
}

      
      
