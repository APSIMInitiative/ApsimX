using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.OldPlant
{
    class RUEModel1
    {
        [Link]
        Plant15 Plant = null;

        [Link]
        PStress PStress = null;

        [Link]
        NStress NStress = null;

        [Link]
        SWStress SWStress = null;

        public Function TempStress { get; set; }

        public Function RUE { get; set; }

        public Function RUEModifier { get; set; }   // used for CO2

        
        public event NewPotentialGrowthDelegate NewPotentialGrowth;

        public double PotentialDM(double radiationInterceptedGreen)
        {
            double RUEFactor = 1.0;
            double stress_factor = Math.Min(Math.Min(Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                                              SWStress.OxygenDeficitPhoto),
                                                     PStress.Photo),
                                            RUEFactor);

            return radiationInterceptedGreen * RUE.Value * stress_factor * RUEModifier.Value;
        }

        private void PublishNewPotentialGrowth()
        {
            // Send out a NewPotentialGrowthEvent.
            if (NewPotentialGrowth != null)
            {
                NewPotentialGrowthType GrowthType = new NewPotentialGrowthType();
                GrowthType.sender = Plant.Name;
                GrowthType.frgr = (float)Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                                   Math.Min(SWStress.OxygenDeficitPhoto, PStress.Photo));
                NewPotentialGrowth.Invoke(GrowthType);
            }
        }
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            PublishNewPotentialGrowth();
        }
    }
}
