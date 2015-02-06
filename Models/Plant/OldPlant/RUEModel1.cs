using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// An RUE model for old plant
    /// </summary>
    [Serializable]
    public class RUEModel1 : Model
    {
        /// <summary>The p stress</summary>
        [Link]
        PStress PStress = null;

        /// <summary>The n stress</summary>
        [Link]
        NStress NStress = null;

        /// <summary>The sw stress</summary>
        [Link]
        SWStress SWStress = null;

        /// <summary>The temporary stress</summary>
        [Link]
        IFunction TempStress = null;
        /// <summary>The rue</summary>
        [Link]
        IFunction RUE = null;
        /// <summary>The rue modifier</summary>
        [Link]
        IFunction RUEModifier = null;   // used for CO2

        /// <summary>Potentials the dm.</summary>
        /// <param name="radiationInterceptedGreen">The radiation intercepted green.</param>
        /// <returns></returns>
        public double PotentialDM(double radiationInterceptedGreen)
        {
            double RUEFactor = 1.0;
            double stress_factor = Math.Min(Math.Min(Math.Min(Math.Min(TempStress.Value, NStress.Photo),
                                                              SWStress.OxygenDeficitPhoto),
                                                     PStress.Photo),
                                            RUEFactor);

            return radiationInterceptedGreen * RUE.Value * stress_factor * RUEModifier.Value;
        }

        /// <summary>Gets the FRGR.</summary>
        /// <value>The FRGR.</value>
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
