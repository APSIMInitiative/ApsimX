namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF;
    using Models.Surface;
    using System;

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

        #region Basic properties  ------------------------------------------------------------------------------------------

        ////- Characteristics (parameters) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the fraction of luxury N remobilisable per day (0-1).</summary>
        internal double FractionNLuxuryRemobilisable = 0.0;

        /// <summary>Gets or sets the sugar fraction on new growth, i.e. soluble carbohydrate (0-1).</summary>
        internal double FractionSugarNewGrowth = 0.0;

        /// <summary>Gets or sets the digestibility of cell walls (0-1).</summary>
        internal double DigestibilityCellWall = 0.5;

        /// <summary>Gets or sets the digestibility of proteins (0-1).</summary>
        internal double DigestibilityProtein = 1.0;

        ////- State properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the dry matter weight (kg/ha).</summary>
        internal virtual double DM { get; set; }

        /// <summary>Gets or sets the nitrogen content (kg/ha).</summary>
        internal virtual double Namount { get; set; }

        /// <summary>Gets or sets the phosphorus content (kg/ha).</summary>
        internal virtual double Pamount { get; set; }

        ////- Amounts in and out >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

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

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Derived properties (outputs)  ------------------------------------------------------------------------------

        /// <summary>Gets the nitrogen concentration (kg/kg).</summary>
        internal double Nconc
        {
            get { return MathUtilities.Divide(Namount, DM, 0.0); }
            set { Namount = value * DM; }
        }

        /// <summary>Gets the phosphorus concentration (kg/kg).</summary>
        internal double Pconc
        {
            get { return MathUtilities.Divide(Pamount, DM, 0.0); }
            set { Pamount = value * DM; }
        }

        /// <summary>Gets the digestibility of this tissue (kg/kg).</summary>
        /// <remarks>Digestibility of sugars is assumed to be 100%.</remarks>
        internal double Digestibility
        {
            get
            {
                double tissueDigestibility = 0.0;
                if (DM > 0.0)
                {
                    double cnTissue = DM * CarbonFractionInDM / Namount;
                    double ratio1 = CNratioCellWall / cnTissue;
                    double ratio2 = CNratioCellWall / CNratioProtein;
                    double fractionSugar = DMTransferedIn * FractionSugarNewGrowth / DM;
                    double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                    double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                    tissueDigestibility = fractionSugar + (fractionProtein * DigestibilityProtein) + (fractionCellWall * DigestibilityCellWall);
                }

                return tissueDigestibility;
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Tissue methods  --------------------------------------------------------------------------------------------

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        internal void DoRemobiliseN(double fraction)
        {
            NRemobilised = NRemobilisable * fraction;
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        internal virtual void DoUpdateTissue()
        {
            DM += DMTransferedIn - DMTransferedOut;
            Namount += NTransferedIn - (NTransferedOut + NRemobilised);
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Constants  -------------------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        const double CarbonFractionInDM = 0.4;

        /// <summary>Carbon to nitrogen ratio of proteins (kg/kg).</summary>
        const double CNratioProtein = 3.5;

        /// <summary>Carbon to nitrogen ratio of cell walls (kg/kg).</summary>
        const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double MyPrecision = 0.0000000001;

        #endregion ---------------------------------------------------------------------------------------------------------

        /// <summary>Removes biomass from tissue.</summary>
        /// <param name="fractionToRemove">The fraction of biomass to remove from the simulation.</param>
        /// <param name="fractionToSoil">The fraction of biomass to send to soil.</param>
        public void RemoveBiomass(double fractionToRemove, double fractionToSoil)
        {
            var dmToSoil = fractionToSoil * DM;
            var nToSoil = fractionToSoil * Namount;
            var totalFraction = fractionToRemove + fractionToSoil;
            if (totalFraction > 0)
            {
                DM *= (1 - totalFraction);
                Namount *= (1 - totalFraction);
                NRemobilisable *= (1 - totalFraction);
            }

            if (dmToSoil > 0)
                DetachBiomass(dmToSoil, nToSoil);
        }
    
        /// <summary>Adds a given amount of detached root material (DM and N) to the surface organic matter pool.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        public virtual void DetachBiomass(double amountDM, double amountN)
        { 
            if (amountDM > 0.0)
                surfaceOrganicMatter.Add(amountDM, amountN, 0.0, species.Name, species.Name);
        }
    }
}
