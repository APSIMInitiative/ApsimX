namespace Models.AgPasture
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.PMF;
    using Models.PMF.Interfaces;

    /// <summary>Describes a generic above ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureAboveGroundOrgan : Model, IOrganDamage, IOrganDigestibility
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

        //---------------------------- Parameters -----------------------

        /// <summary>N concentration for optimum growth (kg/kg).</summary>
        public double NConcOptimum { get; set; } = 0.04;

        /// <summary>Minimum N concentration, structural N (kg/kg).</summary>
        public double NConcMinimum { get; set; } = 0.012;

        /// <summary>Maximum N concentration, for luxury uptake (kg/kg).</summary>
        public double NConcMaximum { get; set; } = 0.05;

        /// <summary>Proportion of organ DM that is standing, available to harvest (0-1).</summary>
        public double FractionStanding { get; set; } = 1.0;

        //----------------------- States -----------------------

        /// <summary>Array of live tissue.</summary>
        public GenericTissue[] LiveTissue { get; private set; }

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        public double MinimumLiveDM { get; set; }

        /// <summary>Flag indicating whether the biomass is above ground or not.</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>Return live biomass. Used by STOCK (g/m2).</summary>
        public Biomass Live { get; private set; } = new Biomass();

        /// <summary>Digestibility of live biomass. Used by STOCK (g/m2).</summary>
        public double LiveDigestibility { get; private set; }

        /// <summary>Dead biomass. Used by STOCK (g/m2).</summary>
        public Biomass Dead { get; private set; } = new Biomass();

        /// <summary>Digestibility of dead biomass. Used by STOCK (g/m2).</summary>
        public double DeadDigestibility { get; private set; }

        /// <summary>Total dry matter in this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMTotal { get; private set; }

        /// <summary>Dry matter in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMLive { get; private set; }

        /// <summary>Dry matter in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMDead { get; private set; }

        /// <summary>Standing herbage weight (kg/ha).</summary>
        [Units("kg/ha")]
        public double StandingHerbageWt { get { return DMTotal * FractionStanding; } }

        /// <summary>Standing live herbage weight (kg/ha).</summary>
        [Units("kg/ha")]
        public double StandingLiveHerbageWt { get { return DMLive * FractionStanding; } }

        /// <summary>Standing live digestibility (0-1).</summary>
        public double StandingLiveDigestibility { get { return 0; } }  // Todo: need to fix

        /// <summary>Standing live digestibility (0-1).</summary>
        public double StandingDeadDigestibility { get { return 0; } }  // Todo: need to fix

        /// <summary>Standing dead herbage weight (kg/ha).</summary>
        [Units("kg/ha")]
        public double StandingDeadHerbageWt { get { return DMDead * FractionStanding; } }

        /// <summary>Standing live herbage weight (kg/ha).</summary>
        [Units("kg/ha")]
        public double StandingLiveHerbageN { get { return NLive * FractionStanding; } }

        /// <summary>Standing dead herbage weight (kg/ha).</summary>
        [Units("kg/ha")]
        public double StandingDeadHerbageN { get { return NDead * FractionStanding; } }

        /// <summary>Standing herbage nitrogen (kg/ha).</summary>
        [Units("kg/ha")]
        public double StandingHerbageN { get { return NTotal * FractionStanding; } }

        /// <summary>Total harvestable dry matter (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMTotalHarvestable { get; private set; }

        /// <summary>Harvestable dry matter in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMLiveHarvestable { get; private set; }

        /// <summary>Dry matter in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMDeadHarvestable { get; private set; }

        /// <summary>N in the total harvestable dry matter (kg/ha).</summary>
        [Units("kg/ha")]
        public double NTotalHarvestable { get; private set; }

        /// <summary>N in the harvestable dry matter in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLiveHarvestable { get; private set; }

        /// <summary>N in the harvestable dry matter in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDeadHarvestable { get; private set; }

        /// <summary>Total N in this tissue (kg/ha).</summary>
        [Units("kg/ha")]
        public double NTotal { get; private set; }

        /// <summary>N in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLive { get; private set; }

        /// <summary>N amount in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDead { get; private set; }

        /// <summary>Average N concentration.</summary>
        [Units("kg/kg")]
        public double NConcTotal { get; private set; }

        /// <summary>Average N concentration in the live tissues (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcLive { get; private set; }

        /// <summary>Average N concentration in dead tissues (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcDead { get; private set; }

        /// <summary>Luxury N available for remobilisation (kg/ha).</summary>
        public double NLuxuryRemobilisable => LiveTissue.Sum(tissue => tissue.NRemobilisable);

        /// <summary>Luxury N remobilised into new growth (kg/ha).</summary>
        public double NLuxuryRemobilised => LiveTissue.Sum(tissue => tissue.NRemobilised);

        /// <summary>DM added to this organ via growth (kg/ha).</summary>
        public double DMGrowth { get { return EmergingTissue.DMTransferedIn; } }

        /// <summary>N added to this organ via growth (kg/ha).</summary>
        public double NGrowth { get { return EmergingTissue.NTransferedIn; } }

        /// <summary>DM senescing from this organ (kg/ha).</summary>
        public double DMSenesced { get { return MatureTissue.DMTransferedOut; } }

        /// <summary>N senescing from this organ (kg/ha).</summary>
        public double NSenesced { get { return MatureTissue.NTransferedOut; } }

        /// <summary>DM detached from this organ (kg/ha).</summary>
        public double DMDetached { get { return DeadTissue.DMTransferedOut; } }

        /// <summary>N detached from this organ (kg/ha).</summary>
        public double NDetached { get { return DeadTissue.NTransferedOut; } }

        /// <summary>Senesced N available for remobilisation (kg/ha).</summary>
        public double NSenescedRemobilisable { get { return DeadTissue.NRemobilisable; } }

        /// <summary>Senesced N remobilised into new growth (kg/ha).</summary>
        public double NSenescedRemobilised { get { return DeadTissue.NRemobilised; } }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved => Tissue.Sum(t => t.DMRemoved);

        /// <summary>N removed from this tissue (kg/ha).</summary>
        public double NRemoved => Tissue.Sum(t => t.NRemoved);

        /// <summary>Fraction of DM removed from organ.</summary>
        public double FractionRemoved
        {
            get
            {
                double preDM = 0.0;
                foreach (var tissue in Tissue)
                    preDM += MathUtilities.Divide(tissue.DMRemoved, tissue.FractionRemoved, 0.0);
                return MathUtilities.Divide(DMRemoved, preDM, 0.0);
            }
        }

        /// <summary>Average digestibility of all biomass.</summary>
        [Units("kg/kg")]
        public double DigestibilityTotal { get; private set; }

        /// <summary>Average digestibility of live biomass.</summary>
        [Units("kg/kg")]
        public double DigestibilityLive { get; private set; }

        /// <summary>Average digestibility of dead biomass.</summary>
        [Units("kg/kg")]
        public double DigestibilityDead { get; private set; }

        /// <summary>Digestibility of standing herbage.</summary>
        [Units("kg/kg")]
        public double StandingDigestibility { get; private set; }

        //----------------------- Public methods -----------------------

        /// <summary>Initialisation</summary>
        /// <param name="minimumLiveWt">Minimum live dry matter (kg/ha)</param>
        public void Initialise(double minimumLiveWt)
        {
            LiveTissue = new GenericTissue[] { EmergingTissue, DevelopingTissue, MatureTissue };
            MinimumLiveDM = minimumLiveWt;
        }

        /// <summary>Set this organ's biomass state.</summary>
        /// <param name="emergingWt">The DM amount of emerging biomass (kg/ha).</param>
        /// <param name="emergingN">The amount of N in emerging biomass (kg/ha).</param>
        /// <param name="developingWt">The DM amount of developing biomass (kg/ha).</param>
        /// <param name="developingN">The amount of N in developing biomass (kg/ha).</param>
        /// <param name="matureWt">The DM amount of developing biomass (kg/ha).</param>
        /// <param name="matureN">The amount of N in developing biomass (kg/ha).</param>
        /// <param name="deadWt">The DM amount of developing biomass (kg/ha).</param>
        /// <param name="deadN">The amount of N in developing biomass (kg/ha).</param>
        public void SetBiomassState(double emergingWt, double emergingN,
                                    double developingWt, double developingN,
                                    double matureWt, double matureN,
                                    double deadWt, double deadN)
        {
            EmergingTissue.SetBiomass(emergingWt, emergingN);
            DevelopingTissue.SetBiomass(developingWt, developingN);
            MatureTissue.SetBiomass(matureWt, matureN);
            DeadTissue.SetBiomass(deadWt, deadN);

            // Tissue states have changed so recalculate our states.
            CalculateStates();
        }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="biomassToRemove">The fraction of the harvestable biomass to remove</param>
        public void RemoveBiomass(OrganBiomassRemovalType biomassToRemove)
        {
            // The fractions passed in are based on the total biomass
            var dm = Tissue.Sum(t => t.DM.Wt);

            // Live removal
            for (int t = 0; t < Tissue.Length - 1; t++)
            {
                Tissue[t].RemoveBiomass(biomassToRemove.FractionLiveToRemove, biomassToRemove.FractionLiveToResidue);
            }

            // Dead removal
            Tissue[Tissue.Length - 1].RemoveBiomass(biomassToRemove.FractionDeadToRemove, biomassToRemove.FractionDeadToResidue);

            // Tissue states have changed so recalculate our states.
            CalculateStates();
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        public void ClearDailyTransferredAmounts()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].ClearDailyTransferredAmounts();
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fractionToRemove">The fraction to kill in each tissue</param>
        public void KillOrgan(double fractionToRemove)
        {
            if (MathUtilities.IsGreaterThan(1.0 - fractionToRemove, 0))
            {
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    DeadTissue.AddBiomass(Tissue[t].DM.Wt * fractionToRemove, Tissue[t].DM.N * fractionToRemove);
                    Tissue[t].RemoveBiomass(fractionToRemove, 0.0);
                }
            }

            // Tissue states have changed so recalculate our states.
            CalculateStates();
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        public void CalculateTissueTurnover(double[] turnoverRate)
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
                        double totalLuxuryN = (Tissue[t].DM.Wt + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NConcLive - NConcOptimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t].FractionNLuxuryRemobilisable);
                    }
                    else
                    {
                        // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                        double remobilisableN = Tissue[t].DMTransferedIn * (NConcLive - NConcMinimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                    }
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        /// <returns>A flag whether mass balance was maintained or not</returns>
        public bool Update()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t].Update();

            CalculateStates();

            // check mass balance
            bool dmIsOk = MathUtilities.FloatsAreEqual(Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal), 0);
            bool nIsOk = MathUtilities.FloatsAreEqual(Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal), 0);
            return (dmIsOk || nIsOk);
        }

        /// <summary>Calculate the values for calculated states.</summary>
        private void CalculateStates()
        {
            DMLive = LiveTissue.Sum(tissue => tissue.DM.Wt);
            DMDead = DeadTissue.DM.Wt;
            DMTotal = DMLive + DMDead;
            DMLiveHarvestable = Math.Max(0, DMLive * FractionStanding - MinimumLiveDM);
            DMDeadHarvestable = DMDead * FractionStanding;
            DMTotalHarvestable = DMLiveHarvestable + DMDeadHarvestable;

            NLive = LiveTissue.Sum(tissue => tissue.DM.N);
            NDead = DeadTissue.DM.N;
            NTotal = Tissue.Sum(tissue => tissue.DM.N);
            NConcLive = MathUtilities.Divide(NLive, DMLive, 0.0);
            NConcDead = MathUtilities.Divide(NDead, DMDead, 0.0);
            NConcTotal = MathUtilities.Divide(NTotal, DMTotal, 0.0);
            NLiveHarvestable = MathUtilities.Divide(DMLiveHarvestable, DMLive * NLive, 0);
            NDeadHarvestable = MathUtilities.Divide(DMDeadHarvestable, DMDead * NDead, 0);
            NTotalHarvestable = NLiveHarvestable + NDeadHarvestable;

            double liveDigestableDM = LiveTissue.Sum(tissue => tissue.Digestibility * tissue.DM.Wt);
            double totalDigestableDM = Tissue.Sum(tissue => tissue.Digestibility * tissue.DM.Wt);
            
            DigestibilityLive = MathUtilities.Divide(liveDigestableDM, DMLive, 0.0);
            DigestibilityDead = DeadTissue.Digestibility;
            DigestibilityTotal = MathUtilities.Divide(totalDigestableDM, DMTotal, 0.0);
            StandingDigestibility = DigestibilityTotal * FractionStanding;

            Live.StructuralWt = DMLive / 10.0;  // to g/m2
            Live.StructuralN = NLive / 10.0;    // to g/m2
            LiveDigestibility = DigestibilityLive;

            Dead.StructuralWt = DMDead / 10.0;  // to g/m2
            Dead.StructuralN = NDead / 10.0;    // to g/m2
            DeadDigestibility = DigestibilityDead;
        }
    }
}
