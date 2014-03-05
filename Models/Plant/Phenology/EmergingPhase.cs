using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Phen
{
    [Serializable]
    public class EmergingPhase : GenericPhase
    {
        [Link]
        Plant Plant = null;

        public double ShootLag { get; set; }
        public double ShootRate { get; set; }

        /// <summary>
        /// Return the target to caller. Can be overridden by derived classes.
        /// </summary>
        protected override double CalcTarget()
        {
            return ShootLag + Plant.SowingData.Depth * ShootRate;
        }

    }

    /// <summary>
    /// The class below is for Plant15. Need to get rid of this eventually.
    /// </summary>
    [Serializable]
    public class EmergingPhase15 : GenericPhase
    {
        [Link]
        Models.PMF.OldPlant.Plant15 Plant = null;

        public double ShootLag { get; set; }
        public double ShootRate { get; set; }

        /// <summary>
        /// Return the target to caller. Can be overridden by derived classes.
        /// </summary>
        protected override double CalcTarget()
        {
            return ShootLag + Plant.SowingData.Depth * ShootRate;
        }

    }
}