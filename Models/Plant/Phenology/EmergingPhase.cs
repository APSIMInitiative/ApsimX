using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Phen
{
    public class EmergingPhase : GenericPhase
    {
        public double ShootLag = 0;
        public double ShootRate = 0;

        private double SowDepth;

        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            SowDepth = Sow.Depth;
        }

        /// <summary>
        /// Return the target to caller. Can be overridden by derived classes.
        /// </summary>
        protected override double CalcTarget()
        {
            return ShootLag + SowDepth * ShootRate;
        }


    }
}