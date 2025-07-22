﻿using System;
using System.Collections.Generic;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Newtonsoft.Json;

namespace Models.PMF
{

    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]

    public class Organ : Model, IOrgan, IHasDamageableBiomass, ILocatorDependency
    {
        [NonSerialized] private ILocator locator;

        /// <summary>Locator supplied by APSIM kernel.</summary>
        public void SetLocator(ILocator locator) => this.locator = locator;

        /// <summary>Harvest the organ.</summary>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double Harvest()
        {
            return RemoveBiomass();
        }

        /// <summary>
        /// Maintenance respiration.
        /// </summary>
        [JsonIgnore]
        public double MaintenanceRespiration { get { return 0; } }

        /// <summary>A list of material (biomass) that can be damaged.</summary>
        public IEnumerable<DamageableBiomass> Material
        {
            get
            {
                Biomass matLive = Live.ToBiomass;
                Biomass matDead = Dead.ToBiomass;
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", matLive, true);
                yield return new DamageableBiomass($"{Parent.Name}.{Name}", matDead, false);
            }
        }

        ///1. Links
        ///--------------------------------------------------------------------------------------------------

        /// <summary>The parent plant</summary>
        [Link]
        public Plant parentPlant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>The senescence rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction senescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction detachmentRate = null;

        /// <summary>Wt in each pool when plant is initialised</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/plant")]
        public IFunction InitialWt = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction TotalCarbonDemand = null;

        ///<summary>The proportion of biomass respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private Respiration respiration = null;

        /// <summary>The list of nurtients to arbitration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public OrganNutrientDelta Carbon = null;

        /// <summary>The list of nurtients to arbitration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public OrganNutrientDelta Nitrogen = null;

        [Link]
        private Clock clock = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double tolerence = 3e-12;

        private double startLiveC { get; set; }
        private double startDeadC { get; set; }
        private double startLiveN { get; set; }
        private double startDeadN { get; set; }
        private double startLiveWt { get; set; }
        private double startDeadWt { get; set; }

        private bool removeBiomass { get; set; }
        private bool resetOrganTomorrow { get; set; }


        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Organ constructor</summary>
        public Organ()
        {
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------
        /// <summary> The organs uptake object if it has one</summary>
        ///
        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Interface to uptakes</summary>
        public IWaterNitrogenUptake WaterNitrogenUptakeObject
        {
            get
            {
                return this.FindChild<IWaterNitrogenUptake>();
            }
        }

        /// <summary> The canopy object </summary>
        public IHasWaterDemand CanopyObjects
        {
            get
            {
                return this.FindChild<IHasWaterDemand>();
            }
        }

        /// <summary>
        /// Object that contains root specific functionality.  Only present if the organ is representing a root
        /// </summary>
        ///  [JsonIgnore]
        public RootNetwork RootNetworkObject { get; set; }

        /// <summary>The Carbon concentration of the organ</summary>
        [Description("Carbon concentration")]
        [Units("g/g")]
        public double Cconc { get; set; } = 0.4;

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; } = true;

        /// <summary>The live biomass</summary>
        public OrganNutrientsState Live { get; private set; }

        /// <summary>The dead biomass</summary>
        public OrganNutrientsState Dead { get; private set; }

        /// <summary>Gets the total biomass</summary>
        [JsonIgnore]
        public OrganNutrientsState Total { get { return Live + Dead; } }

        /// <summary>Gets the biomass reallocated from senescing material</summary>
        [JsonIgnore]
        public OrganNutrientsState ReAllocated { get; private set; }

        /// <summary>Gets the biomass reallocated from senescing material</summary>
        [JsonIgnore]
        public OrganNutrientsState ReTranslocated { get; private set; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [JsonIgnore]
        public OrganNutrientsState Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [JsonIgnore]
        public OrganNutrientsState Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [JsonIgnore]
        public OrganNutrientsState Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public OrganNutrientsState LiveRemoved { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public OrganNutrientsState DeadRemoved { get; private set; }

        /// <summary>The amount of carbon respired</summary>
        [JsonIgnore]
        public OrganNutrientsState Respired { get; private set; }

        /// <summary>total demand for the day</summary>
        [JsonIgnore]
        public double totalCarbonDemand { get; private set; }

        /// <summary>Rate of senescence for the day</summary>
        [JsonIgnore]
        public double SenescenceRate { get; private set; }

        /// <summary>the detachment rate for the day</summary>
        [JsonIgnore]
        public double DetachmentRate { get; private set; }

        /// <summary>Gets the maximum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MaxNConc { get; private set; }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MinNConc { get; private set; }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double CritNConc { get; private set; }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double Wt
        {
            get
            {
                return Live != null ? Live.Wt + Dead.Wt : 0.0;
            }
        }

        /// <summary>Gets the total (live + dead) carbon weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double C
        {
            get
            {
                return Live != null ? Live.Carbon.Total + Dead.Carbon.Total : 0.0;
            }
        }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double N
        {
            get
            {
                return Live != null ? Live.Nitrogen.Total + Dead.Nitrogen.Total : 0.0;
            }
        }
        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double NConc
        {
            get
            {
                return Live != null ? MathUtilities.Divide( N , Wt,0) : 0.0;
            }
        }

        /// <summary>
        /// Gets the nitrogen factor.
        /// </summary>
        public double Fn
        {
            get
            {
                return Live != null ? MathUtilities.Divide(Live.Nitrogen.Total, Live.Wt * MaxNConc, 1) : 0;
            }
        }

        /// <summary>
        /// Gets the metabolic N concentration factor.
        /// </summary>
        public double FNmetabolic
        {
            get
            {
                return (Live != null) ? Math.Min(1.0, MathUtilities.Divide(NConc - MinNConc, CritNConc - MinNConc, 0)) : 0;
            }
        }


        ///6. Public methods
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public virtual double RemoveBiomass(double liveToRemove = 1, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0)
        {
            OrganNutrientsState liveExported = Live * liveToRemove;
            OrganNutrientsState liveRetained = Live * liveToResidue;
            LiveRemoved = liveExported + liveRetained;

            OrganNutrientsState deadExported = Dead * deadToRemove;
            OrganNutrientsState deadRetained = Dead * deadToResidue;
            DeadRemoved = deadExported + deadRetained;

            double fracLiveToResidue = MathUtilities.Divide(liveToResidue, (liveToResidue + liveToRemove), 0);
            double fracDeadToResidue = MathUtilities.Divide(deadToResidue, (deadToResidue + deadToRemove), 0);

            if (fracDeadToResidue + fracLiveToResidue > 0)
            {
                OrganNutrientsState totalToResidues = liveRetained + deadRetained;
                Biomass toResidues = totalToResidues.ToBiomass;
                surfaceOrganicMatter.Add(toResidues.Wt * 10.0, toResidues.N * 10.0, 0.0, parentPlant.PlantType, Name);
            }
            if ((liveToRemove + deadToRemove + liveToResidue + deadToResidue)>0)
            {
                removeBiomass = true;
            }

            return LiveRemoved.Wt + DeadRemoved.Wt;
        }

        /// <summary>Clears this instance.</summary>
        protected virtual void Clear()
        {
            Live.Clear();
            Dead.Clear();
            ReAllocated.Clear();
            ReTranslocated.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            LiveRemoved.Clear();
            DeadRemoved.Clear();
            removeBiomass = false;
            resetOrganTomorrow = false;
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            ReAllocated.Clear();
            ReTranslocated.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
        }

        private void ClearBiomassRemovals()
        {
            LiveRemoved.Clear();
            DeadRemoved.Clear();
            removeBiomass = false;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            RootNetworkObject = this.FindChild<RootNetwork>();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PostPhenology")]
        protected void OnPostPhenology(object sender, EventArgs e)
        {
            totalCarbonDemand = TotalCarbonDemand.Value();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == parentPlant)
            {
                initialiseBiomass();

                if (RootNetworkObject != null)
                    RootNetworkObject.InitailiseNetwork(Live);
            }
        }

        /// <summary>Called when crop is harvested</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PostHarvesting")]
        protected void OnPostHarvesting(object sender, HarvestingParameters e)
        {
            if (e.RemoveBiomass)
                Harvest();
        }

        /// <summary>
        /// set initial biomass for organ
        /// </summary>
        public void initialiseBiomass()
        {
            setNConcs();
            Nitrogen.setConcentrationsOrProportions();
            Carbon.setConcentrationsOrProportions();

            NutrientPoolsState initC = new NutrientPoolsState(
                InitialWt.Value() * Cconc * Carbon.ConcentrationOrFraction.Structural,
                InitialWt.Value() * Cconc * Carbon.ConcentrationOrFraction.Metabolic,
                InitialWt.Value() * Cconc * Carbon.ConcentrationOrFraction.Storage);

            NutrientPoolsState initN = new NutrientPoolsState(
                InitialWt.Value() * Nitrogen.ConcentrationOrFraction.Structural,
                InitialWt.Value() * (Nitrogen.ConcentrationOrFraction.Metabolic - Nitrogen.ConcentrationOrFraction.Structural),
                InitialWt.Value() * (Nitrogen.ConcentrationOrFraction.Storage - Nitrogen.ConcentrationOrFraction.Metabolic));

            Live = new OrganNutrientsState(initC, initN, Cconc);
            Dead = new OrganNutrientsState(Cconc);
            ReAllocated = new OrganNutrientsState(Cconc);
            ReTranslocated = new OrganNutrientsState(Cconc);
            Allocated = new OrganNutrientsState(Cconc);
            Senesced = new OrganNutrientsState(Cconc);
            Detached = new OrganNutrientsState(Cconc);
            LiveRemoved = new OrganNutrientsState(Cconc);
            DeadRemoved = new OrganNutrientsState(Cconc);
            Respired = new OrganNutrientsState(Cconc);

        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                ClearBiomassFlows();
                //Set start properties used for mass balance checking
                startLiveN = Live.N;
                startDeadN = Dead.N;
                startLiveC = Live.C;
                startDeadC = Dead.C;
                startLiveWt = Live.Wt;
                startDeadWt = Dead.Wt;

                //Take away any biomass that was removed by management or phenology triggered event
                if (removeBiomass)
                {
                    Live -= LiveRemoved;
                    Dead -= DeadRemoved;
                }
                removeBiomass = false;

                //Do initial calculations
                SenescenceRate = Math.Min(senescenceRate.Value(),1);
                DetachmentRate = Math.Min(detachmentRate.Value(),1);
                setNConcs();
                Carbon.SetSuppliesAndDemands();
            }
        }



        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                //Calculate biomass to be lost from senescene
                if (SenescenceRate > 0)
                {
                    Senesced = Live *SenescenceRate;
                    Live -= Senesced;

                    //Catch the bits that were reallocated and add the bits that wernt into dead.
                    ReAllocated.Set(carbon:Carbon.SuppliesAllocated.ReAllocation, nitrogen:Nitrogen.SuppliesAllocated.ReAllocation);
                    Senesced -= ReAllocated;
                    Dead += Senesced;
                }

                //Retranslocate from live pools
                ReTranslocated.Set(carbon: Carbon.SuppliesAllocated.ReTranslocation, nitrogen: Nitrogen.SuppliesAllocated.ReTranslocation);
                Live -= ReTranslocated;

                //Add in todays fresh allocation
                Allocated.Set(carbon:Carbon.DemandsAllocated, nitrogen:Nitrogen.DemandsAllocated);
                Live += Allocated;

                // Do detachment
                if ((DetachmentRate > 0) && (Dead.Wt > 0))
                {
                    if (Dead.Weight.Total * (1.0 - DetachmentRate) < 0.00000001)
                        DetachmentRate = 1.0;  // remaining amount too small, detach all
                    Detached = Dead * DetachmentRate;
                    Dead -= Detached;
                    if (RootNetworkObject == null)
                        surfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, parentPlant.PlantType, Name);
                }

                // Remove respiration
                Respired.Set(carbon:respiration.CalculateLosses(),nitrogen:new NutrientPoolsState());
                Live -= Respired;

                if (RootNetworkObject != null)
                {
                    RootNetworkObject.PartitionBiomassThroughSoil(ReAllocated, ReTranslocated, Allocated, Senesced, Detached, LiveRemoved, DeadRemoved);
                    RootNetworkObject.GrowRootDepth();
                }
            }
        }

        /// <summary>Called towards the end of proceedings each day</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoUpdate")]
        protected void OnDoUpdate(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                checkMassBalance(startLiveN, startDeadN, "N");
                checkMassBalance(startLiveC, startDeadC, "C");
                checkMassBalance(startLiveWt, startDeadWt, "Wt");
                ClearBiomassRemovals();
            }
        }

        private void checkMassBalance(double startLive, double startDead, string element)
        {
            double live = (double)(locator.GetObject("Live." + element).Value);
            double dead = (double)(locator.GetObject("Dead." + element).Value);
            double allocated = (double)(locator.GetObject("Allocated." + element).Value);
            double senesced = (double)(locator.GetObject("Senesced." + element).Value);
            double reAllocated = (double)(locator.GetObject("ReAllocated." + element).Value);
            double reTranslocated = (double)(locator.GetObject("ReTranslocated." + element).Value);
            double liveRemoved = (double)(locator.GetObject("LiveRemoved." + element).Value);
            double deadRemoved = (double)(locator.GetObject("DeadRemoved." + element).Value);
            double respired = (double)(locator.GetObject("Respired." + element).Value);
            double detached = (double)(locator.GetObject("Detached." + element).Value);

            double liveBal = Math.Abs(live - (startLive + allocated - senesced - reAllocated
                                                        - reTranslocated - liveRemoved - respired));
            if (liveBal > tolerence)
                throw new Exception(element + " mass balance violation in live biomass of " + this.Name + "on " + clock.Today.ToString());

            double deadBal = Math.Abs(dead - (startDead + senesced - deadRemoved - detached));
            if (deadBal > tolerence)
                throw new Exception(element + " mass balance violation in dead biomass of " + this.Name + "on " + clock.Today.ToString());

        }

        /// <summary>Called when plant endcrop is called</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void onPlantEnding(object sender, EventArgs e)
        {
            resetOrganTomorrow = true;
        }

        /// <summary>Called when Biomass removal event of tyep EndCrop occurs.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndCrop")]
        protected void onEndCrop(object sender, EventArgs e)
        {
            resetOrganTomorrow = true;
        }

        /// <summary>
        /// Called at the start of the day to clear up yesterdays flags.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoCatchYesterday")]
        protected void onDoCatchYesterday(object sender, EventArgs e)
        {
            if (resetOrganTomorrow == true)
                reset();
            resetOrganTomorrow = false;
        }

        /// <summary>
        /// Sends all biomass to residues and zeros variables
        /// </summary>
        private void reset()
        {
            if (Wt > 0.0)
            {
                Senesced = Detached + Live;
                Detached = Detached + Live;
                Detached = Detached + Dead;
                Live.Clear();
                Dead.Clear();
                if (RootNetworkObject == null)
                {
                    surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, parentPlant.PlantType, Name);
                }

                if (RootNetworkObject != null)
                {
                    RootNetworkObject.endRoots();
                }
            }

            Clear();
            if (RootNetworkObject != null)
            {
                RootNetworkObject.PlantZone.Clear();
                RootNetworkObject.Depth = 0;
            }
        }

        private void setNConcs()
        {
            MaxNConc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Storage : 0;
            MinNConc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Structural : 0;
            CritNConc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Metabolic : 0;
        }
    }
}
