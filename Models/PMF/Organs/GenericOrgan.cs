﻿using System;
using System.Collections.Generic;
using System.Linq;
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
    /// This organ is simulated using a GenericOrgan type.  It is parameterised to calculate the growth, senescence, and detachment of any organ that does not have specific functions.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class GenericOrgan : Model, IOrgan, IArbitration, IOrganDamage, IHasDamageableBiomass
    {
        /// <summary>Tolerance for biomass comparisons</summary>
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
        public IFunction SenescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction detachmentRateFunction = null;

        /// <summary>The N retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction NRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        protected IFunction nReallocationFactor = null;

        /// <summary>The DM retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction DMRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private NutrientDemandFunctions dmDemands = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private NutrientDemandFunctions nDemands = null;

        /// <summary>Wt in each pool when plant is initialised</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/plant")]
        public NutrientPoolFunctions InitialWt = null;

        /// <summary>The initial N Concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction initialNConcFunction = null;

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
        public IFunction DMConversionEfficiency = null;

        /// <summary>The cost for remobilisation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m^2")]
        private IFunction remobilisationCost = null;

        /// <summary>Carbon concentration</summary>
        [Units("g/g")]
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction CarbonConcentration = null;

        /// <summary>The photosynthesis</summary>
        [Units("g/m2")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Photosynthesis = null;

        /// <summary>The RetranslocationMethod</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IRetranslocateMethod RetranslocateNitrogen = null;

        /// <summary>The live biomass state at start of the computation round</summary>
        public Biomass StartLive = null;

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
        public GenericOrgan()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; } = true;

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

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double GrowthRespiration { get; set; }

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation { get { return potentialDMAllocation; } }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [JsonIgnore]
        [Units("g DM/g N")]
        public virtual double NFixationCost { get { return 0; } }

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
            return biomassRemovalModel.RemoveBiomass(liveToRemove, deadToRemove, liveToResidue, deadToResidue, 
                                                     Live, Dead, Removed, Detached);
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
            return RetranslocateNitrogen.CalculateBiomass(this);
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            double availableDM = StartLive.StorageWt * SenescenceRate.Value() * dmReallocationFactor.Value();
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
            DMSupply.Fixation = Photosynthesis.Value();
            DMSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        protected virtual void SetNSupply(object sender, EventArgs e)
        {
            NSupply.ReAllocation = Math.Max(0, (StartLive.StorageN + StartLive.MetabolicN) * SenescenceRate.Value() * nReallocationFactor.Value());
            if (NSupply.ReAllocation < -BiomassToleranceValue)
                throw new Exception("Negative N reallocation value computed for " + Name);

            NSupply.ReTranslocation = RetranslocateNitrogen.Calculate(this);
            if (NSupply.ReTranslocation < -BiomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);


            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        protected virtual void SetDMDemand(object sender, EventArgs e)
        {
            double dMCE = DMConversionEfficiency.Value();
            if (dMCE > 0.0)
            {
                DMDemand.Structural = MathUtilities.Divide(dmDemands.Structural.Value() , dMCE,0) + remobilisationCost.Value();
                DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value() / dMCE);
                DMDemand.Metabolic = Math.Max(0, dmDemands.Metabolic.Value() / dMCE);
                DMDemand.QStructuralPriority = dmDemands.QStructuralPriority.Value();
                DMDemand.QMetabolicPriority = dmDemands.QMetabolicPriority.Value();
                DMDemand.QStoragePriority = dmDemands.QStoragePriority.Value();
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
            NDemand.QStructuralPriority = nDemands.QStructuralPriority.Value();
            NDemand.QStoragePriority = nDemands.QStoragePriority.Value();
            NDemand.QMetabolicPriority = nDemands.QMetabolicPriority.Value();
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Gets the biomass retranslocation.</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double RetranslocationWt { get; private set; }

        /// <summary>Sets the dry matter allocation.</summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public virtual void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            
            
            // get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double dMCE = DMConversionEfficiency.Value();
            double growthRespFactor = ((1.0 / dMCE) * (12.0 / 30.0) - 1.0 * CarbonConcentration.Value()) * 44.0 / 12.0;

            RetranslocationWt = dryMatter.Retranslocation;

            GrowthRespiration = 0.0;
            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural * dMCE, DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            GrowthRespiration += Allocated.StructuralWt * growthRespFactor;


            // allocate non structural DM
            if ((dryMatter.Storage * dMCE - DMDemand.Storage) > BiomassToleranceValue)
                throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");
            // Allocated.StorageWt = dryMatter.Storage * dmConversionEfficiency.Value();

            RetranslocateNitrogen.AllocateBiomass(this, dryMatter);
        }

        /// <summary>Sets the n allocation.</summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public virtual void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            RetranslocateNitrogen.Allocate(this, nitrogen);

            // Reallocation
            double senescedFrac = SenescenceRate.Value();
            if (StartLive.Wt * (1.0 - senescedFrac) < BiomassToleranceValue)
                senescedFrac = 1.0;  // remaining amount too small, senesce all

            if (MathUtilities.IsGreaterThan(nitrogen.Reallocation, StartLive.StorageN + StartLive.MetabolicN))
                throw new Exception("N reallocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double StorageNReallocation = Math.Min(nitrogen.Reallocation, StartLive.StorageN * senescedFrac * nReallocationFactor.Value());
            Live.StorageN -= StorageNReallocation;
            Live.MetabolicN -= (nitrogen.Reallocation - StorageNReallocation);
            Allocated.StorageN -= nitrogen.Reallocation;

            // now move the remaining senescing material to the dead pool
            Biomass Loss = new Biomass();
            Loss.StructuralN = StartLive.StructuralN * senescedFrac;
            Loss.StorageN = StartLive.StorageN * senescedFrac - StorageNReallocation;
            Loss.MetabolicN = StartLive.MetabolicN * senescedFrac - (nitrogen.Reallocation - StorageNReallocation);
            Loss.StructuralWt = StartLive.StructuralWt * senescedFrac;
            Loss.MetabolicWt = StartLive.MetabolicWt * senescedFrac;
            Loss.StorageWt = StartLive.StorageWt * senescedFrac;
            Live.Subtract(Loss);
            Dead.Add(Loss);
            Senesced.Add(Loss);
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
            StartLive = new Biomass();
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
            if (parentPlant.IsAlive)
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
                Live.StructuralWt = InitialWt.Structural.Value();
                Live.MetabolicWt = InitialWt.Metabolic.Value();
                Live.StorageWt = InitialWt.Storage.Value();
                Live.StructuralN = Live.StructuralWt * initialNConcFunction.Value();
                Live.StorageN = Live.StorageWt * initialNConcFunction.Value();
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
                StartLive.SetTo(Live);
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
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
                double mRrate = Math.Min(1.0, Math.Max(0.0, maintenanceRespirationFunction.Value()));

                MaintenanceRespiration = (Live.MetabolicWt + Live.StorageWt) * mRrate;
                Live.MetabolicWt *= (1 - mRrate);
                Live.StorageWt *= (1 - mRrate);
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
            // Add heading and description.
            foreach (ITag tag in base.Document())
                yield return tag;

            // List the parameters, properties, and processes from this organ that need to be documented:

            // Document DM demands.
            yield return new Section("Dry Matter Demand", DocumentDMDemand(dmDemands));

            // Document N demands.
            yield return new Section("Nitrogen Demand", DocumentNDemand(FindChild("nDemands")));

            // Document N concentration thresholds.
            yield return new Section("N Concentration Thresholds", DocumentNConcentrationThresholds());

            // todo: should this be in its own section?
            IModel nDemandSwitch = FindChild("NitrogenDemandSwitch");
            if (nDemandSwitch is Constant nDemandConst)
            {
                if (nDemandConst.Value() == 1)
                {
                    // Don't bother documenting as it does nothing
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

            // Document DM supplies.
            yield return new Section("Dry Matter Supply", DocumentDMSupply());

            // Document N supplies.
            yield return new Section("Nitrogen Supply", DocumentNSupply());

            // Document senescence and detachment.
            yield return new Section("Senescence and Detachment", DocumentSenescence());

            if (biomassRemovalModel != null)
                foreach (ITag tag in biomassRemovalModel.Document())
                    yield return tag;
        }

        private IEnumerable<ITag> DocumentDMDemand(IModel dmDemands)
        {
            yield return new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.");
            foreach (ITag tag in dmDemands.Document())
                yield return tag;
        }

        private IEnumerable<ITag> DocumentNDemand(IModel nDemands)
        {
            yield return new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.");
            foreach (ITag tag in nDemands.Document())
                yield return tag;
        }

        private IEnumerable<ITag> DocumentNConcentrationThresholds()
        {
            IModel minNConc = FindChild("MinimumNConc");
            foreach (ITag tag in minNConc.Document())
                yield return tag;
            IModel critNConc = FindChild("CriticalNConc");
            foreach (ITag tag in critNConc.Document())
                yield return tag;
            IModel maxNConc = FindChild("MaximumNConc");
            foreach (ITag tag in maxNConc.Document())
                yield return tag;
        }

        private IEnumerable<ITag> DocumentDMSupply()
        {
            IModel dmReallocFactor = FindChild("DMReallocationFactor");
            if (dmReallocFactor is Constant dmReallocConst)
            {
                if (dmReallocConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not reallocate DM when senescence of the organ occurs.");
                else
                    yield return new Paragraph($"{Name} will reallocate {dmReallocConst.Value() * 100}% of DM that senesces each day.");
            }
            else
            {
                yield return new Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.");
                foreach (ITag tag in dmReallocFactor.Document())
                    yield return tag;
            }
            IModel dmRetransFactor = FindChild("DMRetranslocationFactor");
            if (dmRetransFactor is Constant dmRetransConst)
            {
                if (dmRetransConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not retranslocate non-structural DM.");
                else
                    yield return new Paragraph($"{Name} will retranslocate {dmRetransConst.Value() * 100}% of non-structural DM each day.");
            }
            else
            {
                yield return new Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.");
                foreach (ITag tag in dmRetransFactor.Document())
                    yield return tag;
            }
        }

        private IEnumerable<ITag> DocumentNSupply()
        {
            IModel nReallocFactor = FindChild("NReallocationFactor");
            if (nReallocFactor is Constant nReallocConst)
            {
                if (nReallocConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not reallocate N when senescence of the organ occurs.");
                else
                    yield return new Paragraph($"{Name} can reallocate up to {nReallocConst.Value() * 100}% of N that senesces each day if required by the plant arbitrator to meet N demands.");
            }
            else
            {
                yield return new Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.");
                foreach (ITag tag in nReallocFactor.Document())
                    yield return tag;
            }
            IModel nRetransFactor = FindChild("NRetranslocationFactor");
            if (nRetransFactor is Constant nRetransConst)
            {
                if (nRetransConst.Value() == 0)
                    yield return new Paragraph($"{Name} does not retranslocate non-structural N.");
                else
                    yield return new Paragraph($"{Name} can retranslocate up to {nRetransConst.Value() * 100}% of non-structural N each day if required by the plant arbitrator to meet N demands.");
            }
            else
            {
                yield return new Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.");
                foreach (ITag tag in nRetransFactor.Document())
                    yield return tag;
            }
        }

        private IEnumerable<ITag> DocumentSenescence()
        {
            IModel senescenceRate = FindChild("SenescenceRate");
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

            IModel detachmentRate = FindChild("DetachmentRateFunction");
            if (detachmentRate is Constant detachmentConst)
            {
                if (detachmentConst.Value() == 0)
                    yield return new Paragraph($"{Name} has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.");
                else
                    yield return new Paragraph($"{Name} detaches {detachmentConst.Value() * 100}% of its live biomass each day, passing it to the surface organic matter model for decomposition.");
            }
            else
            {
                yield return new Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.");
                foreach (ITag tag in detachmentRate.Document())
                    yield return tag;
            }
        }
    }
}
