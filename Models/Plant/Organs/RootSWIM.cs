using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Organs
{
    class RootSWIM : BaseOrgan, BelowGround
    {
        [Link]
        Plant Plant = null;

        private double[] Uptake = null;
        public double[] rlv = null;

        
        [Units("mm")]
        public override double WaterUptake
        {
            get { return -Utility.Math.Sum(Uptake); }
        }


        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(WaterUptakesCalculatedType Uptakes)
        {
            for (int i = 0; i != Uptakes.Uptakes.Length; i++)
            {
                if (Uptakes.Uptakes[i].Name == Plant.Name)
                    Uptake = Uptakes.Uptakes[i].Amount;
            }
        }
    }
}
