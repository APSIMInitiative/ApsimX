using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Surface;

namespace Models.AgPasture
{

    /// <summary>Describes a generic tissue of a pasture species.</summary>
    [Serializable]
    public class GenericTissue : Model
    {
        /// <summary>Name of species.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected PastureSpecies species = null;

        /// <summary>The surface organic matter model.</summary>
        [Link]
        private SurfaceOrganicMatter surfaceOrganicMatter = null;

        //----------------------- Constants -----------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        private const double CarbonConcentration = 0.4;

        /// <summary>Carbon to nitrogen ratio of proteins (kg/kg).</summary>
        private const double CNratioProtein = 3.5;

        /// <summary>Carbon to nitrogen ratio of cell walls (kg/kg).</summary>
        private const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        //---------------------------- Parameters -----------------------

        /// <summary>Fraction of excess N, above optimum N for live tissues and minimum for dead tissue, that is remobilisable per day (0-1).</summary>
        public double FractionNRemobilisable { get; set; }

        /// <summary>Sugar fraction on new growth, i.e. soluble carbohydrate (0-1).</summary>
        public double FractionSugarNewGrowth { get; set; } = 0.0;

        /// <summary>Digestibility of cell walls (0-1).</summary>
        public double DigestibilityCellWall { get; set; }

        /// <summary>Digestibility of proteins (0-1).</summary>
        public double DigestibilityProtein { get; set; } = 1.0;

        //----------------------- Daily Deltas -----------------------
        // These get applied once each day during Update()

        /// <summary>DM transferred into this tissue (kg/ha).</summary>
        public double DMTransferredIn { get; set; }

        /// <summary>DM transferred out of this tissue (kg/ha).</summary>
        public double DMTransferredOut { get; private set; }

        /// <summary>N transferred into this tissue (kg/ha).</summary>
        public double NTransferredIn { get; set; }

        /// <summary>N transferred out of this tissue (kg/ha).</summary>
        public double NTransferredOut { get; private set; }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved { get; private set; }

        /// <summary>The fraction of DM removed from this tissue.</summary>
        public double FractionRemoved { get; private set; }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        public double NRemoved { get; private set; }

        /// <summary>N available for remobilisation (kg/ha).</summary>
        public double NRemobilisable { get; set; }

        /// <summary>N remobilised into new growth (kg/ha).</summary>
        public double NRemobilised { get; set; }

        //----------------------- States -----------------------

        /// <summary>Tissue dry matter biomass.</summary>
        private AGPBiomass biomass = new AGPBiomass();

        /// <summary>Dry matter biomass.</summary>
        public IAGPBiomass DM { get { return biomass; } }

        /// <summary>Digestibility of this tissue (kg/kg).</summary>
        public double Digestibility { get; private set; }

        //----------------------- Public methods -----------------------

        /// <summary>Sets the biomass of this tissue.</summary>
        /// <param name="dmAmount">The DM amount to set to (kg/ha).</param>
        /// <param name="nAmount">The amount of N to set to (kg/ha).</param>
        public void SetBiomass(double dmAmount, double nAmount)
        {
            biomass.Wt = dmAmount;
            biomass.N = nAmount;
            calculateDigestibility();
        }

        /// <summary>Adds an amount of biomass to this tissue.</summary>
        /// <param name="dmAmount">The amount of dry matter to add (kg/ha).</param>
        /// <param name="nAmount">The amount of nitrogen to add (kg/ha).</param>
        public void AddBiomass(double dmAmount, double nAmount)
        {
            biomass.Wt += dmAmount;
            biomass.N += nAmount;

            calculateDigestibility();
        }

        /// <summary>Removes a fraction of the biomass from this tissue.</summary>
        /// <param name="fractionToRemove">The fraction of biomass to remove.</param>
        /// <param name="fractionToSoil">The fraction of removed biomass to send to soil surface.</param>
        public void RemoveBiomass(double fractionToRemove, double fractionToSoil)
        {
            var dmToSoil = fractionToSoil * biomass.Wt;
            var nToSoil = fractionToSoil * biomass.N;
            var totalFraction = fractionToRemove + fractionToSoil;

            DMRemoved = totalFraction * biomass.Wt;
            NRemoved = totalFraction * biomass.N;
            FractionRemoved = totalFraction;

            if (totalFraction > 0.0)
            {
                biomass.Wt *= (1.0 - totalFraction);
                biomass.N *= (1.0 - totalFraction);
                NRemobilisable *= (1.0 - totalFraction);
            }

            if (dmToSoil > 0.0)
            {
                surfaceOrganicMatter.Add(dmToSoil, nToSoil, 0.0, species.Name, species.Name);
            }

            calculateDigestibility();
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        public void Update()
        {
            // update values
            biomass.Wt += DMTransferredIn - DMTransferredOut;
            biomass.N += NTransferredIn - (NTransferredOut + NRemobilised);

            // ensure values near zero are zeroed (prevent small negatives)
            if (MathUtilities.FloatsAreEqual(biomass.Wt, 0.0, Epsilon))
            {
                biomass.Wt = 0.0;
            }
            if (MathUtilities.FloatsAreEqual(biomass.N, 0.0, Epsilon))
            {
                biomass.N = 0.0;
            }

            // check that biomass doesn't go negative
            if (biomass.Wt < 0.0)
            {
                throw new Exception($"{species.Name} {Name} tissue has negative dry matter");
            }
            if (biomass.N < 0.0)
            {
                throw new Exception($"{species.Name} {Name} tissue has negative N content");
            }

            calculateDigestibility();
        }

        /// <summary>Computes the DM and N amounts turned over for this tissue, also estimates remobilisable N.</summary>
        /// <param name="turnoverRate">The turnover rate for the tissue today.</param>
        /// <param name="receivingTissue">The tissue to move the turned over biomass to.</param>
        /// <param name="nConcThreshold">The N concentration threshold, below which no remobilisation will occur.</param>
        /// <remarks>For live tissues, potential N remobilisable is above optimum concentration, for dead is all above minimum.</remarks>
        public void DoTissueTurnover(double turnoverRate, GenericTissue receivingTissue, double nConcThreshold)
        {
            if (biomass.Wt > 0.0 && turnoverRate > 0.0)
            {
                // get the amounts turned over
                var turnedoverDM = biomass.Wt * turnoverRate;
                var turnedoverN = biomass.N * turnoverRate;
                DMTransferredOut += turnedoverDM;
                NTransferredOut += turnedoverN;

                // pass the amounts from this to the receiving tissue
                if ((receivingTissue != null) && (turnedoverDM > 0.0))
                {
                    receivingTissue.SetBiomassTransferIn(turnedoverDM, turnedoverN);
                }

                // get the N amount remobilisable (all N in this tissue above the given nConc threshold)
                double potentialRemobilisableN = 0.0;
                // first, get the available N in the tissue
                if (Name != "DeadTissue")
                {
                    potentialRemobilisableN = (biomass.Wt - DMTransferredOut) * Math.Max(0.0, biomass.NConc - nConcThreshold);
                    // NOTE: N already in dead tissue is no longer available for remobilisation
                }

                // then get the N that is available in the material being transferred in (includes N into dead, i.e. senesced)
                potentialRemobilisableN += Math.Max(0.0, NTransferredIn - DMTransferredIn * nConcThreshold);

                // only a fraction of the above calculated potential remobilisable N can be remobilised each day
                NRemobilisable = Math.Max(0.0, potentialRemobilisableN * FractionNRemobilisable);
            }
        }

        /// <summary>Set the biomass moving into the tissue.</summary>
        /// <param name="dm">Dry matter to add (kg/ha).</param>
        /// <param name="n">The nitrogen to add (kg/ha).</param>
        public void SetBiomassTransferIn(double dm, double n)
        {
            DMTransferredIn += dm;
            NTransferredIn += n;
        }

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        public void DoRemobiliseN(double fraction)
        {
            if (fraction > 1.0)
            {
                throw new Exception($"{species.Name} {Name} fraction of N remobilised is > 1");
            }

            NRemobilised = NRemobilisable * fraction;
        }

        /// <summary>Clear the daily flows of DM and N.</summary>
        public void ClearDailyTransferredAmounts()
        {
            DMTransferredIn = 0.0;
            DMTransferredOut = 0.0;
            NTransferredIn = 0.0;
            NTransferredOut = 0.0;
            NRemobilisable = 0.0;
            NRemobilised = 0.0;
            DMRemoved = 0.0;
            NRemoved = 0.0;
            FractionRemoved = 0.0;
        }

        //----------------------- Private methods -----------------------

        /// <summary>Calculate the values for calculated states.</summary>
        /// <remarks>Digestibility of sugars is assumed to be 100%.</remarks>
        private void calculateDigestibility()
        {
            Digestibility = 0.0;
            if (biomass.Wt > 0.0)
            {
                double cnTissue = biomass.Wt * CarbonConcentration / biomass.N;
                double ratio1 = CNratioCellWall / cnTissue;
                double ratio2 = CNratioCellWall / CNratioProtein;
                double fractionSugar = DMTransferredIn * FractionSugarNewGrowth / biomass.Wt;
                double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                Digestibility = fractionSugar + (fractionProtein * DigestibilityProtein) + (fractionCellWall * DigestibilityCellWall);

                if (Digestibility < 0.0)
                {
                    throw new Exception($"{species.Name} {Name} digestibility is negative");
                }
            }
        }
    }
}
