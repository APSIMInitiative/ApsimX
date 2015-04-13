using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using APSIM.Shared.Utilities;


namespace Models.PMF.OldPlant
{
    /// <summary>
    /// N uptake - version 3
    /// </summary>
    [Serializable]
    public class NUptake3 : Model
    {
        /// <summary>The root</summary>
        [Link]
        Root1 Root = null;

        /// <summary>The n stress period</summary>
        [Link]
        IFunction NStressPeriod = null;

        /// <summary>Gets or sets the kno3.</summary>
        /// <value>The kno3.</value>
        public double kno3 { get; set; }

        /// <summary>Gets or sets the KNH4.</summary>
        /// <value>The KNH4.</value>
        public double knh4 { get; set; }

        /// <summary>Gets or sets the no3ppm_min.</summary>
        /// <value>The no3ppm_min.</value>
        public double no3ppm_min { get; set; }

        /// <summary>Gets or sets the nh4ppm_min.</summary>
        /// <value>The nh4ppm_min.</value>
        public double nh4ppm_min { get; set; }

        /// <summary>Gets or sets the total_n_uptake_max.</summary>
        /// <value>The total_n_uptake_max.</value>
        public double total_n_uptake_max { get; set; }

        /// <summary>Does the n uptake.</summary>
        /// <param name="RootDepth">The root depth.</param>
        /// <param name="no3gsm">The no3gsm.</param>
        /// <param name="nh4gsm">The NH4GSM.</param>
        /// <param name="bd">The bd.</param>
        /// <param name="dlayer">The dlayer.</param>
        /// <param name="sw_avail">The sw_avail.</param>
        /// <param name="sw_avail_pot">The sw_avail_pot.</param>
        /// <param name="no3gsm_min">The no3gsm_min.</param>
        /// <param name="nh4gsm_min">The nh4gsm_min.</param>
        /// <param name="no3gsm_uptake_pot">The no3gsm_uptake_pot.</param>
        /// <param name="nh4gsm_uptake_pot">The nh4gsm_uptake_pot.</param>
        public void DoNUptake(double RootDepth, double[] no3gsm, double[] nh4gsm,
                              double[] bd, double[] dlayer, double[] sw_avail, double[] sw_avail_pot,
                              double[] no3gsm_min, double[] nh4gsm_min,
                              ref double[] no3gsm_uptake_pot, ref double[] nh4gsm_uptake_pot)
        {
            double no3ppm, nh4ppm, swfac;


            int deepest_layer = Root.FindLayerNo(RootDepth);
            if (NStressPeriod.Value == 1)
            {
                // N stress period.
                for (int layer = 0; layer <= deepest_layer; layer++)
                {
                    no3ppm = no3gsm[layer] * MathUtilities.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);
                    nh4ppm = nh4gsm[layer] * MathUtilities.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);

                    swfac = MathUtilities.Divide(sw_avail[layer], sw_avail_pot[layer], 0.0); //**2
                    swfac = MathUtilities.Constrain(swfac, 0.0, 1.0);

                    no3gsm_uptake_pot[layer] = no3gsm[layer]
                                               * kno3 * (no3ppm - no3ppm_min) * swfac;
                    no3gsm_uptake_pot[layer] = MathUtilities.Constrain(no3gsm_uptake_pot[layer], double.MinValue
                                                        , no3gsm[layer] - no3gsm_min[layer]);
                    no3gsm_uptake_pot[layer] = MathUtilities.Constrain(no3gsm_uptake_pot[layer], 0.0, double.MaxValue);

                    nh4gsm_uptake_pot[layer] = nh4gsm[layer] * knh4 * (nh4ppm - nh4ppm_min) * swfac;
                    nh4gsm_uptake_pot[layer] = MathUtilities.Constrain(nh4gsm_uptake_pot[layer], double.MinValue
                                                        , nh4gsm[layer] - nh4gsm_min[layer]);
                    nh4gsm_uptake_pot[layer] = MathUtilities.Constrain(nh4gsm_uptake_pot[layer], 0.0, double.MaxValue);
                }

            }
            else
            {
                // Not in N stress period.
                // No N stress whilst N is present in soil
                // crop has access to all that it wants early on
                // to avoid effects of small differences in N supply
                // having affect during the most sensitive part
                // of canopy development.

                for (int layer = 0; layer <= deepest_layer; layer++)
                {
                    no3ppm = no3gsm[layer] * MathUtilities.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);
                    nh4ppm = nh4gsm[layer] * MathUtilities.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);


                    if (kno3 > 0 && no3ppm > no3ppm_min)
                        no3gsm_uptake_pot[layer] = MathUtilities.Constrain(no3gsm[layer] - no3gsm_min[layer], 0.0, double.MaxValue);
                    else
                        no3gsm_uptake_pot[layer] = 0.0;

                    if (knh4 > 0 && nh4ppm > nh4ppm_min)
                        nh4gsm_uptake_pot[layer] = MathUtilities.Constrain(nh4gsm[layer] - nh4gsm_min[layer], 0.0, double.MaxValue);
                    else
                        nh4gsm_uptake_pot[layer] = 0.0;
                }
            }
            double total_n_uptake_pot = MathUtilities.Sum(no3gsm_uptake_pot, 0, deepest_layer + 1, 0.0)
                       + MathUtilities.Sum(nh4gsm_uptake_pot, 0, deepest_layer + 1, 0.0);
            double scalef = MathUtilities.Divide(total_n_uptake_max, total_n_uptake_pot, 0.0);
            scalef = MathUtilities.Constrain(scalef, 0.0, 1.0);
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                no3gsm_uptake_pot[layer] = scalef * no3gsm_uptake_pot[layer];
                nh4gsm_uptake_pot[layer] = scalef * nh4gsm_uptake_pot[layer]; ;
            }
            Util.Debug("Root.no3gsm_uptake_pot=%f", MathUtilities.Sum(no3gsm_uptake_pot));
            Util.Debug("Root.nh4gsm_uptake_pot=%f", MathUtilities.Sum(nh4gsm_uptake_pot));

        }
    }
}
