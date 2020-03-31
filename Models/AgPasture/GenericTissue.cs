namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Surface;
    using System;
    using System.Xml.Serialization;

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

        /// <summary>Gets or sets the fraction of luxury N remobilisable per day (0-1).</summary>
        [XmlIgnore]
        public double FractionNLuxuryRemobilisable { get; set; } = 0.1;

        /// <summary>Gets or sets the sugar fraction on new growth, i.e. soluble carbohydrate (0-1).</summary>
        [XmlIgnore]
        public double FractionSugarNewGrowth { get; set; } = 0.0;

        /// <summary>Gets or sets the digestibility of cell walls (0-1).</summary>
        [XmlIgnore]
        public double DigestibilityCellWall { get; set; } = 0.5;

        /// <summary>Gets or sets the digestibility of proteins (0-1).</summary>
        [XmlIgnore]
        public double DigestibilityProtein { get; set; } = 1.0;

        /// <summary>Amount of dry matter.</summary>
        [XmlIgnore]
        public AGPBiomass DM { get; set; } = new AGPBiomass();

        /// <summary>Gets or sets the DM amount transferred into this tissue (kg/ha).</summary>
        internal double DMTransferedIn { get; set; }

        /// <summary>Gets or sets the DM amount transferred out of this tissue (kg/ha).</summary>
        internal double DMTransferedOut { get; set; }

        /// <summary>Gets or sets the amount of N transferred into this tissue (kg/ha).</summary>
        internal double NTransferedIn { get; set; }

        /// <summary>Gets or sets the amount of N transferred out of this tissue (kg/ha).</summary>
        internal double NTransferedOut { get; set; }

        /// <summary>Gets or sets the amount of N available for remobilisation (kg/ha).</summary>
        internal double NRemobilisable { get; set; }

        /// <summary>Gets or sets the amount of N remobilised into new growth (kg/ha).</summary>
        internal double NRemobilised { get; set; }

        /// <summary>The amount of DM removed from this tissue (kg/ha).</summary>
        internal double DMRemoved { get; private set; }

        /// <summary>The amount of N removed from this tissue (kg/ha).</summary>
        internal double NRemoved { get; private set; }

        /// <summary>Gets the digestibility of this tissue (kg/kg).</summary>
        /// <remarks>Digestibility of sugars is assumed to be 100%.</remarks>
        internal double Digestibility
        {
            get
            {
                double tissueDigestibility = 0.0;
                if (DM.Wt > 0.0)
                {
                    double cnTissue = DM.Wt * CarbonFractionInDM / DM.N;
                    double ratio1 = CNratioCellWall / cnTissue;
                    double ratio2 = CNratioCellWall / CNratioProtein;
                    double fractionSugar = DMTransferedIn * FractionSugarNewGrowth / DM.Wt;
                    double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                    double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                    tissueDigestibility = fractionSugar + (fractionProtein * DigestibilityProtein) + (fractionCellWall * DigestibilityCellWall);
                }

                return tissueDigestibility;
            }
        }

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        internal void DoRemobiliseN(double fraction)
        {
            NRemobilised = NRemobilisable * fraction;
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        internal virtual void DoUpdateTissue()
        {
            DM.Wt += DMTransferedIn - DMTransferedOut;
            DM.N += NTransferedIn - (NTransferedOut + NRemobilised);
        }

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        const double CarbonFractionInDM = 0.4;

        /// <summary>Carbon to nitrogen ratio of proteins (kg/kg).</summary>
        const double CNratioProtein = 3.5;

        /// <summary>Carbon to nitrogen ratio of cell walls (kg/kg).</summary>
        const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double MyPrecision = 0.0000000001;

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            DMRemoved = 0;
            NRemoved = 0;
        }

        /// <summary>Removes biomass from tissue.</summary>
        /// <param name="fractionToRemove">The fraction of the total biomass to remove from the simulation.</param>
        /// <param name="fractionToSoil">The fraction of the total biomass to send to soil.</param>
        public void RemoveBiomass(double fractionToRemove, double fractionToSoil)
        {
            var dmToSoil = fractionToSoil * DM.Wt;
            var nToSoil = fractionToSoil * DM.N;
            var totalFraction = fractionToRemove + fractionToSoil;

            DMRemoved = totalFraction * DM.Wt;
            NRemoved = totalFraction * DM.N;

            if (totalFraction > 0)
            {
                DM.Wt *= (1 - totalFraction);
                DM.N *= (1 - totalFraction);
                NRemobilisable *= (1 - totalFraction);
            }

            if (dmToSoil > 0)
                DetachBiomass(dmToSoil, nToSoil);
        }

        /// <summary>
        /// Add biomass.
        /// </summary>
        /// <param name="dmAmount">The amount of dry matter to add (kg/ha).</param>
        /// <param name="nAmount">The amount of nitrogen to add (kg/ha).</param>
        public void AddBiomass(double dmAmount, double nAmount)
        {
            DM.Wt += dmAmount;
            DM.N += nAmount;
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the surface organic matter pool.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        public virtual void DetachBiomass(double amountDM, double amountN)
        { 
            if (amountDM > 0.0)
                surfaceOrganicMatter.Add(amountDM, amountN, 0.0, species.Name, species.Name);
        }

        /// <summary>
        /// Reset tissue to the specified amount.
        /// </summary>
        /// <param name="dmAmount">The amount of dry matter to reset to (kg/ha).</param>
        /// <param name="nAmount">The amount of nitrogen to reset to (kg/ha).</param>
        public void ResetTo(double dmAmount, double nAmount)
        {
            DM.Wt = dmAmount;
            DM.N = nAmount;
        }

        /// <summary>Reset tissue to zero.</summary>
        public virtual void Reset()
        {
            DM.Wt = 0;
            DM.N = 0;
        }
    }
}
