namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System.Linq;

    /// <summary>Describes a generic above ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureAboveGroundOrgan : Model, IOrganDamage
    {
        /// <summary>The collection of tissues for this organ.</summary>
        [Link(Type = LinkType.Child)]
        public GenericTissue[] Tissue;

        /// <summary>The emerging tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue EmergingTissue { get; private set; }

        /// <summary>The developing tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue DevelopingTissue { get; private set; }

        /// <summary>The mature tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue MatureTissue { get; private set; }

        /// <summary>The mature tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue DeadTissue { get; private set; }

        /// <summary>List of BiomassRemovalTypes with default biomass removal fractions for given removal types.</summary>
        private Dictionary<string, OrganBiomassRemovalType> defaultRemovalFractions = new Dictionary<string, OrganBiomassRemovalType>();

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        public double MinimumLiveDM = 0.0;

        /// <summary>Gets or sets the N concentration for optimum growth (kg/kg).</summary>
        [XmlIgnore]
        public double NConcOptimum { get; set; } = 0.04;

        /// <summary>Gets or sets the minimum N concentration, structural N (kg/kg).</summary>
        [XmlIgnore]
        public double NConcMinimum { get; set; } = 0.012;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake (kg/kg).</summary>
        [XmlIgnore]
        public double NConcMaximum { get; set; } = 0.05;

        /// <summary>Proportion of organ DM that is standing, available to harvest (0-1).</summary>
        [XmlIgnore]
        public double FractionStanding { get; set; } = 1.0;

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>Return live biomass. Used by STOCK (g/m2).</summary>
        public Biomass Live
        {
            get
            {
                return new Biomass()
                {
                    StructuralWt = DMLiveHarvestable / 10,  // to g/m2
                    StructuralN = NLiveHarvestable / 10,    // to g/m2
                    DMDOfStructural = DigestibilityLive
                };
            }
        }

        /// <summary>Return dead biomass. Used by STOCK (g/m2).</summary>
        public Biomass Dead
        {
            get
            {
                return new Biomass()
                {
                    StructuralWt = DMDeadHarvestable / 10,  // to g/m2
                    StructuralN = NDeadHarvestable / 10,    // to g/m2
                    DMDOfStructural = DigestibilityDead
                };
            }
        }

        /// <summary>Gets the total dry matter in this organ (kg/ha).</summary>
        public double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].DM.Wt;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues (kg/ha).</summary>
        public double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].DM.Wt;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        public double DMDead
        {
            get { return Tissue[Tissue.Length - 1].DM.Wt; }
        }

        /// <summary>The standing herbage weight (kg/ha).</summary>
        public double StandingHerbageWt { get { return DMTotal * FractionStanding; } }

        /// <summary>The standing live herbage weight (kg/ha).</summary>
        public double StandingLiveHerbageWt { get { return DMLive * FractionStanding; } }

        /// <summary>The standing live digestibility (0-1).</summary>
        public double StandingLiveDigestibility { get { return 0; } }  // Todo: need to fix

        /// <summary>The standing live digestibility (0-1).</summary>
        public double StandingDeadDigestibility { get { return 0; } }  // Todo: need to fix

        /// <summary>The standing dead herbage weight (kg/ha).</summary>
        public double StandingDeadHerbageWt { get { return DMDead * FractionStanding; } }

        /// <summary>The standing live herbage weight (kg/ha).</summary>
        public double StandingLiveHerbageN { get { return NLive * FractionStanding; } }

        /// <summary>The standing dead herbage weight (kg/ha).</summary>
        public double StandingDeadHerbageN { get { return NDead * FractionStanding; } }

        /// <summary>The standing herbage nitrogen (kg/ha).</summary>
        public double StandingHerbageN { get { return NTotal * FractionStanding; } }

        /// <summary>The total harvestable dry matter (kg/ha).</summary>
        public double DMTotalHarvestable { get { return DMLiveHarvestable + DMDeadHarvestable; } }

        /// <summary>The harvestable dry matter in the live (green) tissues (kg/ha).</summary>
        public double DMLiveHarvestable { get { return Math.Max(0, DMLive * FractionStanding - MinimumLiveDM); } }

        /// <summary>The harvestable dry matter in the dead tissues (kg/ha).</summary>
        public virtual double DMDeadHarvestable { get { return DMDead * FractionStanding; } }

        /// <summary>The amount of N in the total harvestable dry matter (kg/ha).</summary>
        public double NTotalHarvestable { get { return NLiveHarvestable + NDeadHarvestable; } }

        /// <summary>The amount of N in the harvestable dry matter in the live (green) tissues (kg/ha).</summary>
        public double NLiveHarvestable { get { return DMLiveHarvestable / DMLive * NLive; } }

        /// <summary>The amount of N in the harvestable dry matter in the dead tissues (kg/ha).</summary>
        public virtual double NDeadHarvestable { get { return DMDeadHarvestable / DMDead * NDead; } }

        /// <summary>The total N amount in this tissue (kg/ha).</summary>
        public double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].DM.N;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues (kg/ha).</summary>
        public double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].DM.N;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        public double NDead
        {
            get { return Tissue[Tissue.Length - 1].DM.N; }
        }

        /// <summary>Gets the average N concentration in this organ (kg/kg).</summary>
        public double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues (kg/kg).</summary>
        public double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues (kg/kg).</summary>
        public double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation (kg/ha).</summary>
        public double NSenescedRemobilisable
        {
            get { return Tissue[Tissue.Length - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        public double NSenescedRemobilised
        {
            get { return Tissue[Tissue.Length - 1].NRemobilised; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation (kg/ha).</summary>
        public double NLuxuryRemobilisable
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
        public double NLuxuryRemobilised
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
        public double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth (kg/ha).</summary>
        public double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ (kg/ha).</summary>
        public double DMSenesced
        {
            get { return Tissue[Tissue.Length - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ (kg/ha).</summary>
        public double NSenesced
        {
            get { return Tissue[Tissue.Length - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ (kg/ha).</summary>
        public double DMDetached
        {
            get { return Tissue[Tissue.Length - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ (kg/ha).</summary>
        public double NDetached
        {
            get { return Tissue[Tissue.Length - 1].NTransferedOut; }
        }

        /// <summary>The amount of DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved { get { return Tissue.Sum(t => t.DMRemoved); } }

        /// <summary>The amount of N removed from this tissue (kg/ha).</summary>
        public double NRemoved { get { return Tissue.Sum(t => t.NRemoved); } }

        /// <summary>Gets the average digestibility of all biomass for this organ (kg/kg).</summary>
        public double DigestibilityTotal
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM.Wt;

                return MathUtilities.Divide(digestableDM, DMTotal, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of live biomass for this organ (kg/kg).</summary>
        public double DigestibilityLive
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM.Wt;

                return MathUtilities.Divide(digestableDM, DMLive, 0.0);
            }
        }

        /// <summary>Gets the average digestibility of dead biomass for this organ (kg/kg).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        public double DigestibilityDead
        {
            get { return Tissue[Tissue.Length - 1].Digestibility; }
        }

        /// <summary>Digestibility of standing herbage.</summary>
        [Units("kg/kg")]
        public double StandingDigestibility { get { return DigestibilityTotal * FractionStanding; } }

        /// <summary>
        /// Reset this organ's state.
        /// </summary>
        /// <param name="emergingWt">The amount of emerging biomass (kg/ha).</param>
        /// <param name="developingWt">The amount of developing biomass (kg/ha).</param>
        /// <param name="matureWt">The amount of developing biomass (kg/ha).</param>
        /// <param name="deadWt">The amount of developing biomass (kg/ha).</param>
        public void Reset(double emergingWt, double developingWt, double matureWt, double deadWt)
        {
            EmergingTissue.ResetTo(emergingWt, emergingWt * NConcOptimum);
            DevelopingTissue.ResetTo(developingWt, developingWt * NConcOptimum);
            MatureTissue.ResetTo(matureWt, matureWt * NConcOptimum);
            DeadTissue.ResetTo(deadWt, deadWt * NConcMinimum);
        }

        /// <summary>
        /// Reset this organ's state at emergence.
        /// </summary>
        /// <param name="emergingWt">The amount of emerging biomass (kg/ha).</param>
        /// <param name="developingWt">The amount of developing biomass (kg/ha).</param>
        /// <param name="matureWt">The amount of developing biomass (kg/ha).</param>
        /// <param name="deadWt">The amount of developing biomass (kg/ha).</param>
        public void ResetEmergence(double emergingWt, double developingWt, double matureWt, double deadWt)
        {
            EmergingTissue.ResetTo(emergingWt, emergingWt * NConcOptimum);
            DevelopingTissue.ResetTo(developingWt, developingWt * NConcOptimum);
            MatureTissue.ResetTo(matureWt, matureWt * NConcOptimum);
            DeadTissue.ResetTo(deadWt, deadWt * NConcOptimum);
        }

        /// <summary>
        /// Remove biomass from organ
        /// </summary>
        /// <param name="biomassToRemove">The fraction of the harvestable biomass to remove</param>
        public void RemoveBiomass(OrganBiomassRemovalType biomassToRemove)
        {
            // The fractions passed in are based on the harvestable biomass. Convert these to
            // fractions of total biomass so that we can pass these to the tissue RemoveBiomass methods.
            biomassToRemove.FractionLiveToRemove = MathUtilities.Divide(biomassToRemove.FractionLiveToRemove * DMLiveHarvestable, DMLive, 0);
            biomassToRemove.FractionDeadToRemove = MathUtilities.Divide(biomassToRemove.FractionDeadToRemove * DMDeadHarvestable, DMDead, 0);
            biomassToRemove.FractionLiveToResidue  = MathUtilities.Divide(biomassToRemove.FractionLiveToResidue * DMLiveHarvestable, DMLive, 0);
            biomassToRemove.FractionDeadToResidue  = MathUtilities.Divide(biomassToRemove.FractionDeadToResidue * DMDeadHarvestable, DMDead, 0);

            // Live removal
            for (int t = 0; t < Tissue.Length - 1; t++)
                Tissue[t].RemoveBiomass(biomassToRemove.FractionLiveToRemove, biomassToRemove.FractionLiveToResidue);

            // Dead removal
            Tissue[Tissue.Length - 1].RemoveBiomass(biomassToRemove.FractionDeadToRemove, biomassToRemove.FractionDeadToResidue);
        }

        /// <summary>Reset all amounts to zero in all tissues of this organ.</summary>
        public void DoResetOrgan()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].Reset();
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        public void DoCleanTransferAmounts()
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
        /// <param name="fractionToRemove">The fraction to kill in each tissue</param>
        public void DoKillOrgan(double fractionToRemove = 1.0)
        {
            if (MathUtilities.IsGreaterThan(1.0 - fractionToRemove, 0))
            {
                double fractionRemaining = 1.0 - fractionToRemove;
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    DeadTissue.AddBiomass(Tissue[t].DM.Wt * fractionToRemove, Tissue[t].DM.N * fractionToRemove);
                    Tissue[t].RemoveBiomass(fractionToRemove, 0.0);
                }
            }
            else
            {
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    DeadTissue.AddBiomass(Tissue[t].DM.Wt, Tissue[t].DM.N);
                    Tissue[t].Reset();
                }
            }
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        public void DoTissueTurnover(double[] turnoverRate)
        {
            double turnedoverDM;
            double turnedoverN;

            // get amounts turned over
            for (int t = 0; t < Tissue.Length; t++)
            {
                if (turnoverRate[t] > 0.0)
                {
                    turnedoverDM = Tissue[t].DM.Wt * turnoverRate[t];
                    turnedoverN = Tissue[t].DM.N * turnoverRate[t];
                    Tissue[t].DMTransferedOut += turnedoverDM;
                    Tissue[t].NTransferedOut += turnedoverN;

                    if (t < Tissue.Length - 1)
                    {
                        // pass amounts turned over from this tissue to the next (except last one)
                        Tissue[t + 1].DMTransferedIn += turnedoverDM;
                        Tissue[t + 1].NTransferedIn += turnedoverN;

                        // get the amounts remobilisable (luxury N)
                        double totalLuxuryN = (Tissue[t].DM.Wt + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
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
        public bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = MathUtilities.FloatsAreEqual(Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal), 0);
            bool nIsOk = MathUtilities.FloatsAreEqual(Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal), 0);
            return (dmIsOk || nIsOk);
        }

        /// <summary>Adds a removal type to the defaultRemovalFractions.</summary>
        /// <param name="typeName">The name of the removal type</param>
        /// <param name="removalFractions">The default removal fractions</param>
        public void SetRemovalFractions(string typeName, OrganBiomassRemovalType removalFractions)
        {
            defaultRemovalFractions.Add(typeName, removalFractions);
        }

        /// <summary>Gets the default removal fractions for a given removal type.</summary>
        /// <param name="typeName">The type of removal</param>
        /// <returns>The default removal fractions</returns>
        public OrganBiomassRemovalType GetRemovalFractions(string typeName)
        {
            if (defaultRemovalFractions.ContainsKey(typeName))
                return defaultRemovalFractions[typeName];
            else
                return null;
        }
    }
}