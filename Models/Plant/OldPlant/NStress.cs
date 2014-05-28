using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.PMF.OldPlant
{
    [Serializable]
    public class NStress : Model
    {
        [Link]
        Leaf1 Leaf = null;
        [Link]
        Stem1 Stem = null;
        public double N_fact_photo { get; set; }
        public double N_fact_pheno { get; set; }
        public double N_fact_expansion { get; set; }
        public double N_fact_grain { get; set; }

        public double PhotoStress { get { return 1 - photo; } }
        public double PhenoStress { get { return 1 - pheno; } }
        public double ExpansionStress { get { return 1 - expansion; } }
        public double GrainStress { get { return 1 - grain; } }

        private double expansion;
        private double pheno;
        private double photo;
        private double grain;
        private double _PhotoSum;
        private int _PhotoCount;
        private double _GrainSum;
        private int _GrainCount;
        public double Photo { get { return photo; } }
        public double Pheno { get { return pheno; } }
        public double PhotoAverage { get { return Utility.Math.Divide(_PhotoSum, _PhotoCount, 0); } }
        public double Expansion { get { return expansion; } }
        public double Grain { get { return grain; } }
        public double GrainAverage { get { return Utility.Math.Divide(_GrainSum, _GrainCount, 0); } }

        public void DoPlantNStress()
        {
            // Expansion uses leaves only
            expansion = critNFactor(Leaf, null, N_fact_expansion);

            // Rest have leaf & stem
            pheno = critNFactor(Leaf, Stem, N_fact_pheno);
            photo = critNFactor(Leaf, Stem, N_fact_photo);
            grain = critNFactor(Leaf, Stem, N_fact_grain);
            Util.Debug("NStress.Photo=%f", photo);
            Util.Debug("NStress.Pheno=%f", pheno);
            Util.Debug("NStress.Grain=%f", grain);
            Util.Debug("NStress.Expansion=%f", expansion);
        }

        internal void Update()
        {
            _PhotoSum += Photo;
            _PhotoCount++;

            _GrainSum += Photo;
            _GrainCount++;
        }

        internal void ResetAverage()
        {
            _PhotoSum = 0;
            _PhotoCount = 0;
            _GrainSum = 0;
            _GrainCount = 0;
        }

        private static double critNFactor(Leaf1 Leaf, Stem1 Stem, double multiplier)
        //=======================================================================================
        //   Calculate Nitrogen stress factor from a bunch of parts
        /*  Purpose
        *   The concentration of Nitrogen in plant parts is used to derive a Nitrogen stress index
        *   for many processes. This stress index is calculated from today's relative nutitional
        *   status between a critical and minimum Nitrogen concentration.
        */
        {
            double dm = Leaf.Live.Wt;
            double N = Leaf.Live.N;
            if (Stem != null)
            {
                dm += Stem.Live.Wt;
                N += Stem.Live.N;
            }
            
            if (dm > 0.0)
            {
                double N_conc = N / dm;

                // calculate critical and minimum N concentrations
                double N_crit = Leaf.NCrit;
                double N_min = Leaf.NMin;
                if (Stem != null)
                {
                    N_crit += Stem.NCrit;
                    N_min += Stem.NMin;
                }

                double N_conc_crit = N_crit / dm;
                double N_conc_min = N_min / dm;

                //calculate shortfall in N concentrations
                double dividend = N_conc - N_conc_min;
                double divisor = N_conc_crit - N_conc_min;
                if (divisor != 0)
                {
                    double result = multiplier * dividend / divisor;
                    return Utility.Math.Bound(result, 0.0, 1.0);
                }
            }
            return (1.0);
        }





    }
}
