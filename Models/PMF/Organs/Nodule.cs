using System;
using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using Newtonsoft.Json;

namespace Models.PMF.Organs
{

    /// <summary>
    /// This organ simulates the root structure associate with symbiotic N-fixing bacteria.  It provides the core functions of determining 
    ///  N fixation supply and related costs.  It also calculates the growth, senescence and detachment of nodules.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Nodule : Model, IOrgan, IArbitration, IOrganDamage, IHasDamageableBiomass
    {
        /// <summary>The fixation metabolic cost</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g DM/g N")]
        IFunction FixationMetabolicCost = null;

        /// <summary>The specific nitrogenase activity</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m^2")]
        IFunction FixationRate = null;

        /// <summary>The respired wt</summary>
        [Units("g/m2")]
        [JsonIgnore]
        public double RespiredWt { get; set; }
        /// <summary>Gets the n fixed.</summary>
        [Units("g/m2")]
        [JsonIgnore]
        public double NFixed { get; set; }


        /// <summary>Gets or sets the n fixation cost.</summary>
        [Units("g DM/g N")]
        public double NFixationCost { get { return FixationMetabolicCost.Value(); } }

        /// <summary>Sets the n allocation.</summary>
        public void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, startLive.StorageN + startLive.MetabolicN - NSupply.ReTranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double StorageNRetranslocation = Math.Min(nitrogen.Retranslocation, startLive.StorageN * (1 - senescenceRate.Value()) * nRetranslocationFactor.Value());
            Live.StorageN -= StorageNRetranslocation;
            Live.MetabolicN -= (nitrogen.Retranslocation - StorageNRetranslocation);
            Allocated.StorageN -= nitrogen.Retranslocation;

            // Reallocation
            if (MathUtilities.IsGreaterThan(nitrogen.Reallocation, startLive.StorageN + startLive.MetabolicN))
                throw new Exception("N reallocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double StorageNReallocation = Math.Min(nitrogen.Reallocation, startLive.StorageN * senescenceRate.Value() * nReallocationFactor.Value());
            Live.StorageN -= StorageNReallocation;
            Live.MetabolicN -= (nitrogen.Reallocation - StorageNReallocation);
            Allocated.StorageN -= nitrogen.Reallocation;
            NFixed = nitrogen.Fixation;    // now get our fixation value.
        }

        /// <summary>Gets the respired wt fixation.</summary>
        [Units("g/m^2")]
        public double RespiredWtFixation { get { return RespiredWt; } }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        private void SetNSupply(object sender, EventArgs e)
        {
            NSupply.ReAllocation = Math.Max(0, (startLive.StorageN + startLive.MetabolicN) * senescenceRate.Value() * nReallocationFactor.Value());
            if (NSupply.ReAllocation < -BiomassToleranceValue)
                throw new Exception("Negative N reallocation value computed for " + Name);

            NSupply.ReTranslocation = Math.Max(0, (startLive.StorageN + startLive.MetabolicN) * (1 - senescenceRate.Value()) * nRetranslocationFactor.Value());
            if (NSupply.ReTranslocation < -BiomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Uptake = 0;
            NSupply.Fixation = FixationRate.Value();
        }

        /// <summary>Sets the dry matter allocation.</summary>
        public void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // Check retranslocation
            if (dryMatter.Retranslocation - startLive.StorageWt > BiomassToleranceValue)
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            // get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double growthRespFactor = ((1.0 / dmConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * CarbonConcentration.Value()) * 44.0 / 12.0;

            GrowthRespiration = 0.0;
            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural * dmConversionEfficiency.Value(), DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            GrowthRespiration += Allocated.StructuralWt * growthRespFactor;

            // allocate non structural DM
            if ((dryMatter.Storage * dmConversionEfficiency.Value() - DMDemand.Storage) > BiomassToleranceValue)
                throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");
            // Allocated.StorageWt = dryMatter.Storage * dmConversionEfficiency.Value();
            double diffWt = dryMatter.Storage - dryMatter.Retranslocation;
            if (diffWt > 0)
            {
                diffWt *= dmConversionEfficiency.Value();
                GrowthRespiration += diffWt * growthRespFactor;
            }
            Allocated.StorageWt = diffWt;
            Live.StorageWt += diffWt;
            // allocate metabolic DM
            Allocated.MetabolicWt = dryMatter.Metabolic * dmConversionEfficiency.Value();
            GrowthRespiration += Allocated.MetabolicWt * growthRespFactor;
            //This is the DM that is consumed to fix N.  this is calculated by the arbitrator and passed to the nodule to report
            RespiredWt = dryMatter.Respired;    // Now get the respired value for ourselves.
        }


        /// <summary>Event from sequencer telling us to do phenology events.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        protected void OnStartOfDay(object sender, EventArgs e)
        {
            NFixed = 0;
            RespiredWt = 0;
        }


        /// <summary>Tolerance for biomass comparisons</summary>
        [Units("g/m^2")]
        protected double BiomassToleranceValue = 0.0000000001;

        /// <summary>The parent plant</summary>
        [Link]
        private Plant parentPlant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The senescence rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        protected IFunction senescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction detachmentRateFunction = null;

        /// <summary>The N retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        protected IFunction nRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        protected IFunction nReallocationFactor = null;

        /// <summary>The DM retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction dmRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private NutrientPoolFunctions dmDemands = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private NutrientPoolFunctions nDemands = null;

        /// <summary>The initial biomass dry matter weight</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        private IFunction initialWtFunction = null;

        /// <summary>The maximum N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction maximumNConc = null;

        /// <summary>The minimum N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction minimumNConc = null;

        /// <summary>The critical N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction criticalNConc = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction maintenanceRespirationFunction = null;

        /// <summary>Dry matter conversion efficiency</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction dmConversionEfficiency = null;

        /// <summary>The cost for remobilisation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m^2")]
        private IFunction remobilisationCost = null;

        /// <summary>Carbon concentration</summary>
        /// [Units("g/g")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction CarbonConcentration = null;


        /// <summary>The live biomass state at start of the computation round</summary>
        protected Biomass startLive = null;

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }

        /// <summary>Constructor</summary>
        public Nodule()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return false; } }

        /// <summary>The live biomass</summary>
        [JsonIgnore]
        public Biomass Live { get; private set; }

        /// <summary>The dead biomass</summary>
        [JsonIgnore]
        public Biomass Dead { get; private set; }

        /// <summary>Gets the total biomass</summary>
        [JsonIgnore]
        public Biomass Total { get { return Live + Dead; } }


        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [JsonIgnore]
        public Biomass Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [JsonIgnore]
        public Biomass Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [JsonIgnore]
        public Biomass Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public Biomass Removed { get; private set; }

        /// <summary>Gets or sets the amount of mass lost each day from maintenance respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double GrowthRespiration { get; private set; }

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation { get { return potentialDMAllocation; } }

        /// <summary>Gets the maximum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MaxNconc { get { return maximumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MinNconc { get { return minimumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double CritNconc { get { return criticalNConc.Value(); } }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double Nconc
        {
            get
            {
                if (Wt > 0.0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }

        /// <summary>A list of material (biomass) that can be damaged.</summary>
        public IEnumerable<DamageableBiomass> Material
        {
            get
            {
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", Live, true);
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", Dead, false);
            }
        }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove, double deadToRemove, double liveToResidue, double deadToResidue)
        {
            return biomassRemovalModel.RemoveBiomass(liveToRemove, deadToRemove, liveToResidue, deadToResidue, Live, Dead, Removed, Detached);
        }

        /// <summary>Harvest the organ.</summary>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double Harvest()
        {
            return RemoveBiomass(biomassRemovalModel.HarvestFractionLiveToRemove, biomassRemovalModel.HarvestFractionDeadToRemove,
                                 biomassRemovalModel.HarvestFractionLiveToResidue, biomassRemovalModel.HarvestFractionDeadToResidue);
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        public double AvailableDMRetranslocation()
        {
            double availableDM = Math.Max(0.0, startLive.StorageWt - DMSupply.ReAllocation) * dmRetranslocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM retranslocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            double availableDM = startLive.StorageWt * senescenceRate.Value() * dmReallocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        protected virtual void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.ReAllocation = AvailableDMReallocation();
            DMSupply.ReTranslocation = AvailableDMRetranslocation();
            DMSupply.Fixation = 0;
            DMSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        protected virtual void SetDMDemand(object sender, EventArgs e)
        {
            if (dmConversionEfficiency.Value() > 0.0)
            {
                DMDemand.Structural = (dmDemands.Structural.Value() / dmConversionEfficiency.Value() + remobilisationCost.Value());
                DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value() / dmConversionEfficiency.Value());
                DMDemand.Metabolic = 0;
            }
            else
            { // Conversion efficiency is zero!!!!
                DMDemand.Structural = 0;
                DMDemand.Storage = 0;
                DMDemand.Metabolic = 0;
            }
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        protected virtual void SetNDemand(object sender, EventArgs e)
        {
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = nDemands.Metabolic.Value();
            NDemand.Storage = nDemands.Storage.Value();
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Clears this instance.</summary>
        protected virtual void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
            DMSupply.Clear();
            NSupply.Clear();
            DMDemand.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Live = new Biomass();
            Dead = new Biomass();
            startLive = new Biomass();
            DMDemand = new BiomassPoolType();
            NDemand = new BiomassPoolType();
            DMSupply = new BiomassSupplyType();
            NSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive || parentPlant.IsEnding)
                ClearBiomassFlows();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == parentPlant)
            {
                Clear();
                ClearBiomassFlows();
                Live.StructuralWt = initialWtFunction.Value();
                Live.StorageWt = 0.0;
                Live.StructuralN = Live.StructuralWt * minimumNConc.Value();
                Live.StorageN = (initialWtFunction.Value() * maximumNConc.Value()) - Live.StructuralN;
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (parentPlant.IsEmerged)
                startLive = ReflectionUtilities.Clone(Live) as Biomass;
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                // Do senescence
                double senescedFrac = senescenceRate.Value();
                if (Live.Wt * (1.0 - senescedFrac) < BiomassToleranceValue)
                    senescedFrac = 1.0;  // remaining amount too small, senesce all
                Biomass Loss = Live * senescedFrac;
                Live.Subtract(Loss);
                Dead.Add(Loss);
                Senesced.Add(Loss);

                // Do detachment
                double detachedFrac = detachmentRateFunction.Value();
                if (Dead.Wt * (1.0 - detachedFrac) < BiomassToleranceValue)
                    detachedFrac = 1.0;  // remaining amount too small, detach all
                Biomass detaching = Dead * detachedFrac;
                Dead.Multiply(1.0 - detachedFrac);
                if (detaching.Wt > 0.0)
                {
                    Detached.Add(detaching);
                    surfaceOrganicMatter.Add(detaching.Wt * 10, detaching.N * 10, 0, parentPlant.PlantType, Name);
                }

                // Do maintenance respiration
                MaintenanceRespiration = 0;
                if (maintenanceRespirationFunction.Value() > 0)
                {
                    MaintenanceRespiration = (Live.MetabolicWt + Live.StorageWt) * maintenanceRespirationFunction.Value();
                    Live.MetabolicWt *= (1 - maintenanceRespirationFunction.Value());
                    Live.StorageWt *= (1 - maintenanceRespirationFunction.Value());
                }
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, parentPlant.PlantType, Name);
            }

            Clear();
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (ITag tag in GetModelDescription())
                yield return tag;

            // Document DM demands.
            yield return new Section("Dry Matter Demand", DocumentDMDemands());

            // Document N demands.
            yield return new Section("Nitrogen Demand", DocumentNDemand());

            // Document N concentration thresholds.
            // todo: Should these be in their own section?
            foreach (ITag tag in minimumNConc.Document())
                yield return tag;

            foreach (ITag tag in criticalNConc.Document())
                yield return tag;

            foreach (ITag tag in maximumNConc.Document())
                yield return tag;

            IModel nDemandSwitch = FindChild("NitrogenDemandSwitch");
            if (nDemandSwitch != null)
            {
                if (nDemandSwitch is Constant nDemandConst)
                {
                    if (nDemandConst.Value() == 1)
                    {
                        //Don't bother documenting as is does nothing
                    }
                    else
                    {
                        yield return new Paragraph($"The demand for N is reduced by a factor of {nDemandConst.Value()} as specified by the NitrogenDemandSwitch");
                    }
                }
                else
                {
                    yield return new Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch.");
                    foreach (ITag tag in nDemandSwitch.Document())
                        yield return tag;
                }
            }

            // document DM supplies
            yield return new Section("Dry Matter Supply", DocumentDMSupply());


            // Document N supplies.
            yield return new Section("Nitrogen Supply", DocumentNSupply());


            // Document N fixation.
            IModel fixationRate = FindChild("FixationRate");
            if (fixationRate != null)
                foreach (ITag tag in fixationRate.Document())
                    yield return tag;

            // Document senescence and detachment.
            yield return new Section("Senescence and Detachment", DocumentSenescenceRate());

            if (biomassRemovalModel != null)
                foreach (ITag tag in biomassRemovalModel.Document())
                    yield return tag;
        }

        private IEnumerable<ITag> DocumentSenescenceRate()
        {
            if (senescenceRate is Constant senescenceConst)
            {
                if (senescenceConst.Value() == 0)
                    yield return new Paragraph($"{Name} has senescence parameterised to zero so all biomass in this organ will remain alive.");
                else
                    yield return new Paragraph($"{Name} senesces {senescenceConst.Value() * 100}% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.");
            }
            else
            {
                yield return new Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate.");
                foreach (ITag tag in senescenceRate.Document())
                    yield return tag;
            }

            if (detachmentRateFunction is Constant detachmentConst)
            {
                if (detachmentConst.Value() == 0)
                    yield return new Paragraph($"{Name} has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.");
                else
                    yield return new Paragraph($"{Name} detaches {detachmentConst.Value() * 100}% of its live biomass each day, passing it to the surface organic matter model for decomposition.");
            }
            else
            {
                yield return new Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.");
                foreach (ITag tag in detachmentRateFunction.Document())
                    yield return tag;
            }
        }

        private IEnumerable<ITag> DocumentDMDemands()
        {
            yield return new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.");
            foreach (ITag tag in dmDemands.Document())
                yield return tag;
        }

        private IEnumerable<ITag> DocumentNDemand()
        {
            yield return new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.");
            foreach (ITag tag in nDemands.Document())
                yield return tag;
        }

        private IEnumerable<ITag> DocumentDMSupply()
        {
            if (dmReallocationFactor is Constant dmReallocConst)
            {
                if (dmReallocConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not reallocate DM when senescence of the organ occurs.");
                else
                    yield return new Paragraph($"{Name} will reallocate {dmReallocConst.Value() * 100}% of DM that senesces each day.");
            }
            else
            {
                yield return new Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.");
                foreach (ITag tag in dmReallocationFactor.Document())
                    yield return tag;
            }

            if (dmRetranslocationFactor is Constant dmRetransConst)
            {
                if (dmRetransConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not retranslocate non-structural DM.");
                else
                    yield return new Paragraph($"{Name} will retranslocate {dmRetransConst.Value() * 100}% of non-structural DM each day.");
            }
            else
            {
                yield return new Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.");
                foreach (ITag tag in dmRetranslocationFactor.Document())
                    yield return tag;
            }
        }

        private IEnumerable<ITag> DocumentNSupply()
        {
            if (nReallocationFactor is Constant nReallocConst)
            {
                if (nReallocConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not reallocate N when senescence of the organ occurs.");
                else
                    yield return new Paragraph($"{Name} will reallocate {nReallocConst.Value() * 100}% of N that senesces each day.");
            }
            else
            {
                yield return new Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.");
                foreach (ITag tag in nReallocationFactor.Document())
                    yield return tag;
            }

            IModel nRetransFactor = FindChild("NRetranslocationFactor");
            if (nRetransFactor != null)
            {
                if (nRetransFactor is Constant nRetransConst)
                {
                    if (nRetransConst.Value() == 0)
                        yield return new Paragraph($"{Name} does not retranslocate non-structural N.");
                    else
                        yield return new Paragraph($"{Name} will retranslocate {nRetransConst.Value() * 100}% of non-structural N each day.");
                }
                else
                {
                    yield return new Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.");
                    foreach (ITag tag in nRetransFactor.Document())
                        yield return tag;
                }
            }
        }
    }
}
