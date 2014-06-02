using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.OldPlant
{
    [Serializable]
    public class RUEModel1 : Model
    {
        [Link]
        Plant15 Plant = null;

        [Link]
        PStress PStress = null;

        [Link]
        NStress NStress = null;

        [Link]
        SWStress SWStress = null;

        [Link] Function TempStress = null;
        [Link] Function RUE = null;
        [Link] Function RUEModifier = null;   // used for CO2

        public double PotentialDM(double radiationInterceptedGreen)
        {
            double RUEFactor = 1.0;
            double stress_factor = Math.Min(Math.Min(Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                                              SWStress.OxygenDeficitPhoto),
                                                     PStress.Photo),
                                            RUEFactor);

            return radiationInterceptedGreen * RUE.Value * stress_factor * RUEModifier.Value;
        }

        public double FRGR
        {
            get
            {
                return Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                Math.Min(SWStress.OxygenDeficitPhoto, PStress.Photo));

            }
        }
    }
}
