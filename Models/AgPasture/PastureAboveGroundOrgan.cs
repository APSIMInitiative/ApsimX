namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using System;
    using System.Collections.Generic;

    /// <summary>Describes a generic above ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureAboveGroundOrgan : Model, IOrganDamage
    {
        /// <summary>The collection of tissues for this organ.</summary>
        [Link(Type = LinkType.Child)]
        public GenericTissue[] Tissue;

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassToRemove">Biomass to remove</param>
        public void RemoveBiomass(OrganBiomassRemovalType biomassToRemove)
        {
            // Live removal
            for (int t = 0; t < Tissue.Length - 1; t++)
                Tissue[t].RemoveBiomass(biomassToRemove.FractionLiveToRemove, biomassToRemove.FractionLiveToResidue);

            // Dead removal
            Tissue[Tissue.Length - 1].RemoveBiomass(biomassToRemove.FractionDeadToRemove, biomassToRemove.FractionDeadToResidue);
        }

        #region Organ specific characteristics  ----------------------------------------------------------------------------

        /// <summary>Gets or sets the N concentration for optimum growth (kg/kg).</summary>
        internal double NConcOptimum = 0.04;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake (kg/kg).</summary>
        internal double NConcMaximum = 0.05;

        /// <summary>Gets or sets the minimum N concentration, structural N (kg/kg).</summary>
        internal double NConcMinimum = 0.012;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Proportion of organ DM that is standing, available to harvest (0-1).</summary>
        internal double FractionStanding = 1.0;

        /// <summary>List of BiomassRemovalTypes with default biomass removal fractions for given removal types.</summary>
        private Dictionary<string, OrganBiomassRemovalType> defaultRemovalFractions = new Dictionary<string, OrganBiomassRemovalType>();

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ properties (summary of tissues)  ---------------------------------------------------------------------

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>Return live biomass. Used by STOCK (g/m2).</summary>
        public Biomass Live
        {
            get
            {
                Biomass live = new Biomass();
                live.StructuralWt = DMLive * 0.10 * FractionStanding;
                live.StructuralN = NLive * 0.10 * FractionStanding;
                live.DMDOfStructural = DigestibilityLive;
                return live;
            }
        }

        /// <summary>Return dead biomass. Used by STOCK (g/m2).</summary>
        public Biomass Dead
        {
            get
            {
                Biomass dead = new Biomass();
                dead.StructuralWt = DMDead * 0.10 * FractionStanding;
                dead.StructuralN = NDead * 0.10 * FractionStanding;
                dead.DMDOfStructural = DigestibilityDead;
                return dead;
            }
        }

        /// <summary>Gets the total dry matter in this organ (kg/ha).</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues (kg/ha).</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DMDead
        {
            get { return Tissue[Tissue.Length - 1].DM; }
        }

        /// <summary>The dry matter in the live (green) tissues available to harvest (kg/ha).</summary>
        internal double DMLiveHarvestable
        {
            get { return Math.Max(0.0, Math.Min(DMLive - MinimumLiveDM, DMLive * FractionStanding)); }
        }

        /// <summary>The dry matter in the dead tissues available to harvest (kg/ha).</summary>
        internal virtual double DMDeadHarvestable
        {
            get { return DMDead * FractionStanding; }
        }

        /// <summary>The total N amount in this tissue (kg/ha).</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues (kg/ha).</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead
        {
            get { return Tissue[Tissue.Length - 1].Namount; }
        }

        /// <summary>Gets the average N concentration in this organ (kg/kg).</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues (kg/kg).</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues (kg/kg).</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation (kg/ha).</summary>
        internal double NSenescedRemobilisable
        {
            get { return Tissue[Tissue.Length - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        internal double NSenescedRemobilised
        {
            get { return Tissue[Tissue.Length - 1].NRemobilised; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation (kg/ha).</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Gets the amount of luxury N remobilised into new growth (kg/ha).</summary>
        internal double NLuxuryRemobilised
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].NRemobilised;

                return result;
            }
        }

        /// <summary>Gets the DM amount added to this organ via growth (kg/ha).</summary>
        internal double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth (kg/ha).</summary>
        internal double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ (kg/ha).</summary>
        internal double DMSenesced
        {
            get { return Tissue[Tissue.Length - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ (kg/ha).</summary>
        internal double NSenesced
        {
            get { return Tissue[Tissue.Length - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ (kg/ha).</summary>
        internal double DMDetached
        {
            get { return Tissue[Tissue.Length - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ (kg/ha).</summary>
        internal double NDetached
        {
            get { return Tissue[Tissue.Length - 1].NTransferedOut; }
        }

        /// <summary>Gets the average digestibility of all biomass for this organ (kg/kg).</summary>
        internal double DigestibilityTotal
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;

                return MathUtilities.Divide(digestableDM, DMTotal, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of live biomass for this organ (kg/kg).</summary>
        internal double DigestibilityLive
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;

                return MathUtilities.Divide(digestableDM, DMLive, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of dead biomass for this organ (kg/kg).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DigestibilityDead
        {
            get { return Tissue[Tissue.Length - 1].Digestibility; }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ methods  ---------------------------------------------------------------------------------------------

        /// <summary>Reset all amounts to zero in all tissues of this organ.</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
                Tissue[t].Pamount = 0.0;
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].DMTransferedIn = 0.0;
                Tissue[t].DMTransferedOut = 0.0;
                Tissue[t].NTransferedIn = 0.0;
                Tissue[t].NTransferedOut = 0.0;
                Tissue[t].NRemobilisable = 0.0;
                Tissue[t].NRemobilised = 0.0;
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fraction">The fraction to kill in each tissue</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (1.0 - fraction > Epsilon)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    Tissue[Tissue.Length - 1].DM += Tissue[t].DM * fraction;
                    Tissue[Tissue.Length - 1].Namount += Tissue[t].Namount * fraction;
                    Tissue[t].DM *= fractionRemaining;
                    Tissue[t].Namount *= fractionRemaining;
                }
            }
            else
            {
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    Tissue[Tissue.Length - 1].DM += Tissue[t].DM;
                    Tissue[Tissue.Length - 1].Namount += Tissue[t].Namount;
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal void DoTissueTurnover(double[] turnoverRate)
        {
            double turnedoverDM;
            double turnedoverN;

            // get amounts turned over
            for (int t = 0; t < Tissue.Length; t++)
            {
                if (turnoverRate[t] > 0.0)
                {
                    turnedoverDM = Tissue[t].DM * turnoverRate[t];
                    turnedoverN = Tissue[t].Namount * turnoverRate[t];
                    Tissue[t].DMTransferedOut += turnedoverDM;
                    Tissue[t].NTransferedOut += turnedoverN;

                    if (t < Tissue.Length - 1)
                    {
                        // pass amounts turned over from this tissue to the next (except last one)
                        Tissue[t + 1].DMTransferedIn += turnedoverDM;
                        Tissue[t + 1].NTransferedIn += turnedoverN;

                        // get the amounts remobilisable (luxury N)
                        double totalLuxuryN = (Tissue[t].DM + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t].FractionNLuxuryRemobilisable);
                    }
                    else
                    {
                        // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                        double remobilisableN = Tissue[t].DMTransferedIn * (NconcLive - NConcMinimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                    }
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        /// <returns>A flag whether mass balance was maintained or not</returns>
        internal bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal) <= Epsilon;
            bool nIsOk = Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal) <= Epsilon;
            return (dmIsOk || nIsOk);
        }

        /// <summary>Adds a removal type to the defaultRemovalFractions.</summary>
        /// <param name="typeName">The name of the removal type</param>
        /// <param name="removalFractions">The default removal fractions</param>
        internal void SetRemovalFractions(string typeName, OrganBiomassRemovalType removalFractions)
        {
            defaultRemovalFractions.Add(typeName, removalFractions);
        }

        /// <summary>Gets the default removal fractions for a given removal type.</summary>
        /// <param name="typeName">The type of removal</param>
        /// <returns>The default removal fractions</returns>
        internal OrganBiomassRemovalType GetRemovalFractions(string typeName)
        {
            if (defaultRemovalFractions.ContainsKey(typeName))
                return defaultRemovalFractions[typeName];
            else
                return null;
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        const double CarbonFractionInDM = 0.4;

        /// <summary>Minimum significant difference between two values.</summary>
        const double Epsilon = 0.000000001;
    }
}
