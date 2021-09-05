namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Surface;
    using System;
    using Newtonsoft.Json;

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
        private const double CarbonFractionInDM = 0.4;

        /// <summary>Carbon to nitrogen ratio of proteins (kg/kg).</summary>
        private const double CNratioProtein = 3.5;

        /// <summary>Carbon to nitrogen ratio of cell walls (kg/kg).</summary>
        private const double CNratioCellWall = 100.0;

        //----------------------- Backing fields for states -----------------------

        private AGPBiomass dryMatter = new AGPBiomass();

        //---------------------------- Parameters -----------------------

        /// <summary>The fraction of luxury N remobilisable per day (0-1).</summary>
        public double FractionNLuxuryRemobilisable { get; set; } = 0.1;

        /// <summary>The sugar fraction on new growth, i.e. soluble carbohydrate (0-1).</summary>
        public double FractionSugarNewGrowth { get; set; } = 0.0;

        /// <summary>The digestibility of cell walls (0-1).</summary>
        public double DigestibilityCellWall { get; set; } = 0.5;

        /// <summary>The digestibility of proteins (0-1).</summary>
        public double DigestibilityProtein { get; set; } = 1.0;

        //----------------------- Daily Deltas -----------------------
        // These get applied once each day during Update()

        /// <summary>DM transferred into this tissue (kg/ha).</summary>
        public double DMTransferedIn { get; set; }

        /// <summary>DM transferred out of this tissue (kg/ha).</summary>
        public double DMTransferedOut { get; set; }

        /// <summary>N transferred into this tissue (kg/ha).</summary>
        public double NTransferedIn { get; set; }

        /// <summary>N transferred out of this tissue (kg/ha).</summary>
        public double NTransferedOut { get; set; }

        /// <summary>N available for remobilisation (kg/ha).</summary>
        public double NRemobilisable { get; set; }

        /// <summary>N remobilised into new growth (kg/ha).</summary>
        public double NRemobilised { get; set; }

        //----------------------- States -----------------------

        /// <summary>Dry matter.</summary>
        public IAGPBiomass DM { get { return dryMatter; } }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved { get; private set; }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        public double NRemoved { get; private set; }

        /// <summary>Digestibility of this tissue (kg/kg).</summary>
        /// <remarks>Digestibility of sugars is assumed to be 100%.</remarks>
        public double Digestibility { get; private set; }

        //----------------------- Public methods -----------------------

        /// <summary>Preparation before the main daily processes.</summary>
        public void OnDoDailyInitialisation()
        {
            DMRemoved = 0;
            NRemoved = 0;
            ClearDailyTransferredAmounts();
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        public void Update()
        {
            dryMatter.Wt += DMTransferedIn - DMTransferedOut;
            dryMatter.N += NTransferedIn - (NTransferedOut + NRemobilised);
            CalculateStates();
        }

        /// <summary>Removes biomass from tissue.</summary>
        /// <param name="fractionToRemove">The fraction of the total biomass to remove from the simulation.</param>
        /// <param name="fractionToSoil">The fraction of the total biomass to send to soil.</param>
        public void RemoveBiomass(double fractionToRemove, double fractionToSoil)
        {
            var dmToSoil = fractionToSoil * dryMatter.Wt;
            var nToSoil = fractionToSoil * dryMatter.N;
            var totalFraction = fractionToRemove + fractionToSoil;

            DMRemoved = totalFraction * dryMatter.Wt;
            NRemoved = totalFraction * dryMatter.N;

            if (totalFraction > 0)
            {
                dryMatter.Wt *= (1 - totalFraction);
                dryMatter.N *= (1 - totalFraction);
                NRemobilisable *= (1 - totalFraction);
            }

            if (dmToSoil > 0)
                    surfaceOrganicMatter.Add(dmToSoil, nToSoil, 0.0, species.Name, species.Name);

            CalculateStates();
        }

        /// <summary>
        /// Add biomass.
        /// </summary>
        /// <param name="dmAmount">The amount of dry matter to add (kg/ha).</param>
        /// <param name="nAmount">The amount of nitrogen to add (kg/ha).</param>
        public void AddBiomass(double dmAmount, double nAmount)
        {
            dryMatter.Wt += dmAmount;
            dryMatter.N += nAmount;

            CalculateStates();
        }

        /// <summary>Reset this tissue to the specified amount.</summary>
        /// <param name="dmAmount">The amount of dry matter to reset to (kg/ha).</param>
        /// <param name="nAmount">The amount of nitrogen to reset to (kg/ha).</param>
        public void Reset(double dmAmount, double nAmount)
        {
            dryMatter.Wt = dmAmount;
            dryMatter.N = nAmount;
            CalculateStates();
            ClearDailyTransferredAmounts();
        }

        /// <summary>Clear the daily flows of DM and N.</summary>
        public void ClearDailyTransferredAmounts()
        {
            DMTransferedIn = 0.0;
            DMTransferedOut = 0.0;
            NTransferedIn = 0.0;
            NTransferedOut = 0.0;
            NRemobilisable = 0.0;
            NRemobilised = 0.0;
        }

        //----------------------- Private methods -----------------------

        /// <summary>Calculate the values for calculated states.</summary>
        private void CalculateStates()
        {
            Digestibility = 0.0;
            if (DM.Wt > 0.0)
            {
                double cnTissue = DM.Wt * CarbonFractionInDM / DM.N;
                double ratio1 = CNratioCellWall / cnTissue;
                double ratio2 = CNratioCellWall / CNratioProtein;
                double fractionSugar = DMTransferedIn * FractionSugarNewGrowth / DM.Wt;
                double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                Digestibility = fractionSugar + (fractionProtein * DigestibilityProtein) + (fractionCellWall * DigestibilityCellWall);
            }
        }

        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs args)
        {
            ClearDailyTransferredAmounts();
        }
    }
}