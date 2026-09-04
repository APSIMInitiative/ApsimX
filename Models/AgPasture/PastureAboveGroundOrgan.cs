using System;
using System.Linq;
using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;
using System.Collections.Generic;
using Models.PMF.Organs;
using APSIM.Numerics;

namespace Models.AgPasture
{

    /// <summary>Describes a generic above ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureAboveGroundOrgan : Model, IOrganDamage, IOrganDigestibility, IHasDamageableBiomass
    {
        /// <summary>Plant model.</summary>
        [Link(Type = LinkType.Ancestor)]
        private PastureSpecies species = null;

        /// <summary>Collection of tissues for this organ.</summary>
        [Link(Type = LinkType.Child)]
        public GenericTissue[] Tissue;

        /// <summary>Emerging aboveground organ tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue EmergingTissue { get; private set; }
        /// <summary>Developing aboveground organ tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue DevelopingTissue { get; private set; }

        /// <summary>Mature aboveground organ tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue MatureTissue { get; private set; }

        /// <summary>Dead aboveground organ tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public GenericTissue DeadTissue { get; private set; }

        /// <summary>Collection of live aboveground organ tissues.</summary>
        public GenericTissue[] LiveTissue { get; private set; }

        //---------------------------- Parameters -----------------------

        /// <summary>N concentration for optimum growth (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcOptimum { get; set; }

        /// <summary>Minimum N concentration, structural N (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcMinimum { get; set; }

        /// <summary>Maximum N concentration, for luxury uptake (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcMaximum { get; set; }

        /// <summary>Proportion of organ DM that is standing, available to harvest (0-1).</summary>
        [Units("kg/kg")]
        public double FractionStanding { get; set; } = 1.0;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double MinimumLiveDM { get; set; }

        //----------------------- Constants -----------------------

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        //----------------------- States -----------------------

        /// <summary>Flag indicating whether the biomass is above ground or not.</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>A list of material (biomass) that can be damaged.</summary>
        public IEnumerable<DamageableBiomass> Material
        {
            get
            {
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", Live, true, LiveDigestibility);
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", Dead, false, DeadDigestibility);
            }
        }

        /// <summary>Live biomass. Used by STOCK (g/m2).</summary>
        public Biomass Live { get; private set; } = new Biomass();

        /// <summary>Dead biomass. Used by STOCK (g/m2).</summary>
        public Biomass Dead { get; private set; } = new Biomass();

        /// <summary>Digestibility of live biomass. Used by STOCK (g/g).</summary>
        [Units("g/g")]
        public double LiveDigestibility { get; private set; }

        /// <summary>Digestibility of dead biomass. Used by STOCK (g/g).</summary>
        [Units("g/g")]
        public double DeadDigestibility { get; private set; }

        /// <summary>Total dry matter in this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMTotal { get { return DMLive + DMDead; } }

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
        [Units("kg/kg")]
        public double StandingLiveDigestibility { get { return DigestibilityLive; } }

        /// <summary>Standing live digestibility (0-1).</summary>
        [Units("kg/kg")]
        public double StandingDeadDigestibility { get { return DigestibilityDead; } }

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
        public double DMTotalHarvestable { get { return DMLiveHarvestable + DMDeadHarvestable; } }

        /// <summary>Harvestable dry matter in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMLiveHarvestable { get { return Math.Max(0.0, DMLive * FractionStanding - MinimumLiveDM); } }

        /// <summary>Dry matter in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMDeadHarvestable { get { return DMDead * FractionStanding; } }

        /// <summary>N in the total harvestable dry matter (kg/ha).</summary>
        [Units("kg/ha")]
        public double NTotalHarvestable { get { return NLiveHarvestable + NDeadHarvestable; } }

        /// <summary>N in the harvestable dry matter in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLiveHarvestable { get { return NLive * MathUtilities.Divide(DMLiveHarvestable, DMLive, 0.0); } }

        /// <summary>N in the harvestable dry matter in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDeadHarvestable { get { return NDead * MathUtilities.Divide(DMDeadHarvestable, DMDead, 0.0); } }

        /// <summary>Total N amount in this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double NTotal { get { return NLive + NDead; } }

        /// <summary>N amount in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLive { get; private set; }

        /// <summary>N amount in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDead { get; private set; }

        /// <summary>Average total N concentration in this organ (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcTotal { get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); } }

        /// <summary>Average N concentration in the live tissues (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcLive { get { return MathUtilities.Divide(NLive, DMLive, 0.0); } }

        /// <summary>Average N concentration in dead tissues (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcDead { get { return MathUtilities.Divide(NDead, DMDead, 0.0); } }

        /// <summary>Luxury N available for remobilisation (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLuxuryRemobilisable { get { return LiveTissue.Sum(tissue => tissue.NRemobilisable); } }

        /// <summary>Luxury N remobilised into new growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLuxuryRemobilised { get { return LiveTissue.Sum(tissue => tissue.NRemobilised); } }

        /// <summary>Senesced N available for remobilisation (kg/ha).</summary>
        [Units("kg/ha")]
        public double NSenescedRemobilisable { get { return DeadTissue.NRemobilisable; } }

        /// <summary>Senesced N remobilised into new growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double NSenescedRemobilised { get { return DeadTissue.NRemobilised; } }

        /// <summary>DM senescing from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMSenesced { get { return MatureTissue.DMTransferredOut; } }

        /// <summary>N senescing from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double NSenesced { get { return MatureTissue.NTransferredOut; } }

        /// <summary>DM detached from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMDetached { get { return DeadTissue.DMTransferredOut; } }

        /// <summary>N detached from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDetached { get { return DeadTissue.NTransferredOut; } }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMRemoved { get { return LiveTissue.Sum(tissue => tissue.DMRemoved) + DeadTissue.DMRemoved; } }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        [Units("kg/ha")]
        public double NRemoved { get { return LiveTissue.Sum(tissue => tissue.NRemoved) + DeadTissue.NRemoved; } }

        /// <summary>Fraction of DM removed from organ.</summary>
        [Units("kg/kg")]
        public double FractionRemoved { get { return removedFraction; } }

        /// <summary>DM added to this organ via growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMGrowth { get { return EmergingTissue.DMTransferredIn; } }

        /// <summary>N added to this organ via growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double NGrowth { get { return EmergingTissue.NTransferredIn; } }

        /// <summary>Average digestibility of all biomass.</summary>
        [Units("kg/kg")]
        public double DigestibilityTotal
        {
            get
            {
                if (LiveTissue == null)
                    return 1.0;

                return MathUtilities.Divide(LiveTissue.Sum(tissue => tissue.Digestibility * tissue.DM.Wt)
                                            + DeadTissue.Digestibility * DeadTissue.DM.Wt,
                                            DMTotal, 0.0);
            }
        }

        /// <summary>Average digestibility of live biomass.</summary>
        [Units("kg/kg")]
        public double DigestibilityLive
        {
            get
            {
                if (LiveTissue == null)
                    return 1.0;

                return MathUtilities.Divide(LiveTissue.Sum(tissue => tissue.Digestibility * tissue.DM.Wt),
                                            DMLive, 0.0);
            }
        }

        /// <summary>Average digestibility of dead biomass.</summary>
        [Units("kg/kg")]
        public double DigestibilityDead { get { return DeadTissue.Digestibility; } }

        /// <summary>Digestibility of standing herbage.</summary>
        [Units("kg/kg")]
        public double StandingDigestibility { get { return DigestibilityTotal; } }

        /// <summary>Fraction of dry matter removed (0-1).</summary>
        private double removedFraction = 0.0;


        //----------------------- Public methods -----------------------

        /// <summary>Initialise this organ instance (and tissues).</summary>
        /// <param name="minimumLiveWt">Minimum live DM biomass for this organ (kg/ha).</param>
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

            // tissue states have changed so recalculate our states.
            CalculateStates();
        }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <param name="fractionStanding">Fraction of biomass that remains standing when passed to surfaceOM (0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0, double fractionStanding = 0)
        {
            // the fractions passed in are based on the total biomass
            var previousDM = Tissue.Sum(tissue => tissue.DM.Wt);

            // remove live tissue
            for (int t = 0; t < Tissue.Length - 1; t++)
            {
                Tissue[t].RemoveBiomass(liveToRemove, liveToResidue);
            }

            // remove dead tissue
            Tissue[Tissue.Length - 1].RemoveBiomass(deadToRemove, deadToResidue);

            // calculate the fraction of DM removed from this organ
            double removedDM = Tissue.Sum(tissue => tissue.DMRemoved);
            removedFraction = MathUtilities.Divide(removedDM, previousDM, 0.0);

            // tissue states have changed so recalculate our states.
            CalculateStates();

            // update LAI and herbage digestibility
            species.EvaluateLAI();
            species.EvaluateDigestibility();

            return removedDM;
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        public void ClearDailyTransferredAmounts()
        {
            removedFraction = 0.0;
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
                    Tissue[t].AddBiomass(-Tissue[t].DM.Wt * fractionToRemove, -Tissue[t].DM.N * fractionToRemove);
                }
            }

            // tissue states have changed so recalculate our states.
            CalculateStates();
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        public void CalculateTissueTurnover(double[] turnoverRate)
        {
            EmergingTissue.DoTissueTurnover(turnoverRate[0], DevelopingTissue, NConcOptimum);
            DevelopingTissue.DoTissueTurnover(turnoverRate[1], MatureTissue, NConcOptimum);
            MatureTissue.DoTissueTurnover(turnoverRate[2], DeadTissue, NConcOptimum);
            DeadTissue.DoTissueTurnover(turnoverRate[3], null, NConcMinimum);
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        /// <returns>A flag whether mass balance was maintained or not</returns>
        public bool Update()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            EmergingTissue.Update();
            DevelopingTissue.Update();
            MatureTissue.Update();
            DeadTissue.Update();

            CalculateStates();

            // check mass balance
            bool dmIsOk = MathUtilities.FloatsAreEqual(previousDM + DMGrowth - DMDetached, DMTotal, 0.000001);
            bool nIsOk = MathUtilities.FloatsAreEqual(previousN + NGrowth - NLuxuryRemobilised - NSenescedRemobilised - NDetached, NTotal, 0.000001);
            return (dmIsOk || nIsOk);
        }

        /// <summary>Calculate the values for calculated states.</summary>
        private void CalculateStates()
        {
            DMLive = LiveTissue.Sum(tissue => tissue.DM.Wt);
            DMDead = DeadTissue.DM.Wt;
            NLive = LiveTissue.Sum(tissue => tissue.DM.N);
            NDead = DeadTissue.DM.N;

            Live.StructuralWt = DMLive / 10.0;  // to g/m2
            Live.StructuralN = NLive / 10.0;    // to g/m2
            LiveDigestibility = DigestibilityLive;

            Dead.StructuralWt = DMDead / 10.0;  // to g/m2
            Dead.StructuralN = NDead / 10.0;    // to g/m2
            DeadDigestibility = DigestibilityDead;
        }
    }
}
