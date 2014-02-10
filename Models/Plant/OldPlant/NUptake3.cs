using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;


namespace Models.PMF.OldPlant
{
    class NUptake3
    {
        [Link]
        Root1 Root = null;

        [Link]
        Function NStressPeriod = null;

        public double kno3 = 0;

        public double knh4 = 0;

        public double no3ppm_min = 0;

        public double nh4ppm_min = 0;

        public double total_n_uptake_max = 0;

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
                    no3ppm = no3gsm[layer] * Utility.Math.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);
                    nh4ppm = nh4gsm[layer] * Utility.Math.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);

                    swfac = Utility.Math.Divide(sw_avail[layer], sw_avail_pot[layer], 0.0); //**2
                    swfac = Utility.Math.Constrain(swfac, 0.0, 1.0);

                    no3gsm_uptake_pot[layer] = no3gsm[layer]
                                               * kno3 * (no3ppm - no3ppm_min) * swfac;
                    no3gsm_uptake_pot[layer] = Utility.Math.Constrain(no3gsm_uptake_pot[layer], double.MinValue
                                                        , no3gsm[layer] - no3gsm_min[layer]);
                    no3gsm_uptake_pot[layer] = Utility.Math.Constrain(no3gsm_uptake_pot[layer], 0.0, double.MaxValue);

                    nh4gsm_uptake_pot[layer] = nh4gsm[layer] * knh4 * (nh4ppm - nh4ppm_min) * swfac;
                    nh4gsm_uptake_pot[layer] = Utility.Math.Constrain(nh4gsm_uptake_pot[layer], double.MinValue
                                                        , nh4gsm[layer] - nh4gsm_min[layer]);
                    nh4gsm_uptake_pot[layer] = Utility.Math.Constrain(nh4gsm_uptake_pot[layer], 0.0, double.MaxValue);
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
                    no3ppm = no3gsm[layer] * Utility.Math.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);
                    nh4ppm = nh4gsm[layer] * Utility.Math.Divide(1000.0, bd[layer] * dlayer[layer], 0.0);


                    if (kno3 > 0 && no3ppm > no3ppm_min)
                        no3gsm_uptake_pot[layer] = Utility.Math.Constrain(no3gsm[layer] - no3gsm_min[layer], 0.0, double.MaxValue);
                    else
                        no3gsm_uptake_pot[layer] = 0.0;

                    if (knh4 > 0 && nh4ppm > nh4ppm_min)
                        nh4gsm_uptake_pot[layer] = Utility.Math.Constrain(nh4gsm[layer] - nh4gsm_min[layer], 0.0, double.MaxValue);
                    else
                        nh4gsm_uptake_pot[layer] = 0.0;
                }
            }
            double total_n_uptake_pot = Utility.Math.Sum(no3gsm_uptake_pot, 0, deepest_layer + 1, 0.0)
                       + Utility.Math.Sum(nh4gsm_uptake_pot, 0, deepest_layer + 1, 0.0);
            double scalef = Utility.Math.Divide(total_n_uptake_max, total_n_uptake_pot, 0.0);
            scalef = Utility.Math.Constrain(scalef, 0.0, 1.0);
            for (int layer = 0; layer <= deepest_layer; layer++)
            {
                no3gsm_uptake_pot[layer] = scalef * no3gsm_uptake_pot[layer];
                nh4gsm_uptake_pot[layer] = scalef * nh4gsm_uptake_pot[layer]; ;
            }
            Util.Debug("Root.no3gsm_uptake_pot=%f", Utility.Math.Sum(no3gsm_uptake_pot));
            Util.Debug("Root.nh4gsm_uptake_pot=%f", Utility.Math.Sum(nh4gsm_uptake_pot));

        }
    }
}
