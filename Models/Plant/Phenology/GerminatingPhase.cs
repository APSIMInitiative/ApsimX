using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;


namespace Models.Plant.Phen
{
    class GerminatingPhase : Phase
    {
        [Link]
        Soils.SoilWater SoilWater = null;

        /// <summary>
        /// Do our timestep development
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {

            bool CanGerminate = !Phenology.OnDayOf("Sowing") && SoilWater.esw > 0;

            if (CanGerminate)
                return 0.999;
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
                return 0.999;
            }
        }
    }
}