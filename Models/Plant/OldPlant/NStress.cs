using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using APSIM.Shared.Utilities;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// Nitrogen stress model for plant
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant15))]
    public class NStress : Model
    {
        /// <summary>The leaf</summary>
        [Link]
        Leaf1 Leaf = null;
        /// <summary>The stem</summary>
        [Link]
        Stem1 Stem = null;
        /// <summary>Gets or sets the n_fact_photo.</summary>
        /// <value>The n_fact_photo.</value>
        public double N_fact_photo { get; set; }
        /// <summary>Gets or sets the n_fact_pheno.</summary>
        /// <value>The n_fact_pheno.</value>
        public double N_fact_pheno { get; set; }
        /// <summary>Gets or sets the n_fact_expansion.</summary>
        /// <value>The n_fact_expansion.</value>
        public double N_fact_expansion { get; set; }
        /// <summary>Gets or sets the n_fact_grain.</summary>
        /// <value>The n_fact_grain.</value>
        public double N_fact_grain { get; set; }

        /// <summary>Gets the photo stress.</summary>
        /// <value>The photo stress.</value>
        public double PhotoStress { get { return 1 - photo; } }
        /// <summary>Gets the pheno stress.</summary>
        /// <value>The pheno stress.</value>
        public double PhenoStress { get { return 1 - pheno; } }
        /// <summary>Gets the expansion stress.</summary>
        /// <value>The expansion stress.</value>
        public double ExpansionStress { get { return 1 - expansion; } }
        /// <summary>Gets the grain stress.</summary>
        /// <value>The grain stress.</value>
        public double GrainStress { get { return 1 - grain; } }

        /// <summary>The expansion</summary>
        private double expansion;
        /// <summary>The pheno</summary>
        private double pheno;
        /// <summary>The photo</summary>
        private double photo;
        /// <summary>The grain</summary>
        private double grain;
        /// <summary>The _ photo sum</summary>
        private double _PhotoSum;
        /// <summary>The _ photo count</summary>
        private int _PhotoCount;
        /// <summary>The _ grain sum</summary>
        private double _GrainSum;
        /// <summary>The _ grain count</summary>
        private int _GrainCount;
        /// <summary>Gets the photo.</summary>
        /// <value>The photo.</value>
        public double Photo { get { return photo; } }
        /// <summary>Gets the pheno.</summary>
        /// <value>The pheno.</value>
        public double Pheno { get { return pheno; } }
        /// <summary>Gets the photo average.</summary>
        /// <value>The photo average.</value>
        public double PhotoAverage { get { return MathUtilities.Divide(_PhotoSum, _PhotoCount, 0); } }
        /// <summary>Gets the expansion.</summary>
        /// <value>The expansion.</value>
        public double Expansion { get { return expansion; } }
        /// <summary>Gets the grain.</summary>
        /// <value>The grain.</value>
        public double Grain { get { return grain; } }
        /// <summary>Gets the grain average.</summary>
        /// <value>The grain average.</value>
        public double GrainAverage { get { return MathUtilities.Divide(_GrainSum, _GrainCount, 0); } }

        /// <summary>Does the plant n stress.</summary>
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

        /// <summary>Updates this instance.</summary>
        internal void Update()
        {
            _PhotoSum += Photo;
            _PhotoCount++;

            _GrainSum += Photo;
            _GrainCount++;
        }

        /// <summary>Resets the average.</summary>
        internal void ResetAverage()
        {
            _PhotoSum = 0;
            _PhotoCount = 0;
            _GrainSum = 0;
            _GrainCount = 0;
        }

        /// <summary>Crits the n factor.</summary>
        /// <param name="Leaf">The leaf.</param>
        /// <param name="Stem">The stem.</param>
        /// <param name="multiplier">The multiplier.</param>
        /// <returns></returns>
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
                    return MathUtilities.Bound(result, 0.0, 1.0);
                }
            }
            return (1.0);
        }





    }
}
