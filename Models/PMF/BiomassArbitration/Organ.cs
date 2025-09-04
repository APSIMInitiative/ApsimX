using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Zones;
using Newtonsoft.Json;
using Zone = Models.Core.Zone;

namespace Models.PMF
{

    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]

    public class Organ : Model, IOrgan, IHasDamageableBiomass, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


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
        [Link(Type = LinkType.Ancestor)]
        public Plant parentPlant = null;

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
        protected double tolerence = 3e-11;

        private double startLiveC { get; set; }
        private double startDeadC { get; set; }
        private double startLiveN { get; set; }
        private double startDeadN { get; set; }
        private double startLiveWt { get; set; }
        private double startDeadWt { get; set; }

        private bool removeBiomass { get; set; }
        private bool resetOrganTomorrow { get; set; }

        private double simArea { get; set; }


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
                return Structure.FindChild<IWaterNitrogenUptake>();
            }
        }

        /// <summary> The canopy object </summary>
        public IHasWaterDemand CanopyObjects
        {
            get
            {
                return Structure.FindChild<IHasWaterDemand>();
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

        /// <summary>Gets the total (live + dead) dry matter weight (g)</summary>
        [JsonIgnore]
        [Units("g")]
        public double Wt
        {
            get
            {
                return Live != null ? Live.Wt + Dead.Wt : 0.0;
            }
        }

        /// <summary>Gets the total (live + dead) carbon weight (g)</summary>
        [JsonIgnore]
        [Units("g")]
        public double C
        {
            get
            {
                return Live != null ? Live.Carbon.Total + Dead.Carbon.Total : 0.0;
            }
        }

        /// <summary>Gets the total (live + dead) N amount (g)</summary>
        [JsonIgnore]
        [Units("g")]
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

        /// <summary>
        /// The width of the organ is assumed to be the width of the parent plant.  
        /// If parent plant does not have width model it is set as the width of the parent zone
        /// </summary>
        private double PlantWidth
        {
            get
            {
                IFunction width = Structure.FindChild<IFunction>("Width",relativeTo: parentPlant) as IFunction;
                if (width != null)
                    return width.Value() / 1000; //Convert from mm to m
                else
                {
                    RectangularZone parentZone = Structure.FindParent<RectangularZone>(recurse: true);

                    if (parentZone != null)
                        return parentZone.Width;
                    else
                        return 1.0;
                }
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
                addSOMtoZones(toResidues.Wt, toResidues.N);
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
            RootNetworkObject = Structure.FindChild<RootNetwork>();
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
                else
                    initialiseSOMZones();
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
                        addSOMtoZones(Detached.Wt, Detached.N);
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
            double live = (double)(Structure.GetObject("Live." + element).Value);
            double dead = (double)(Structure.GetObject("Dead." + element).Value);
            double allocated = (double)(Structure.GetObject("Allocated." + element).Value);
            double senesced = (double)(Structure.GetObject("Senesced." + element).Value);
            double reAllocated = (double)(Structure.GetObject("ReAllocated." + element).Value);
            double reTranslocated = (double)(Structure.GetObject("ReTranslocated." + element).Value);
            double liveRemoved = (double)(Structure.GetObject("LiveRemoved." + element).Value);
            double deadRemoved = (double)(Structure.GetObject("DeadRemoved." + element).Value);
            double respired = (double)(Structure.GetObject("Respired." + element).Value);
            double detached = (double)(Structure.GetObject("Detached." + element).Value);

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
                    addSOMtoZones(Wt, N);
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

        /// <summary>
        /// Method to allocate detached plant biomass over zones
        /// </summary>
        /// <param name="wt"></param>
        /// <param name="n"></param>
        private void addSOMtoZones(double wt, double n)
        {
            Simulation sim = Structure.FindParent<Simulation>();
            List<Zone> zones = Structure.FindAll<Zone>(relativeTo: sim).ToList();
            Zone parentZone = Structure.FindParent<Zone>(recurse: true);
            double totalWidth = 0;
            double[] zoneWidths = new double[zones.Count];
            int zi = 0;
            foreach (Zone z in zones)
            {
                if (z is RectangularZone)
                {
                    totalWidth += (z as RectangularZone).Width;
                }
                else
                {
                    totalWidth = 1.0;
                }
                zi += 1;
            }
            double plantWidth = Math.Min(PlantWidth,totalWidth);
            double zoneLength = 1.0;
            zi = 0;
            foreach (Zone z in zones)
            {
                if (z is RectangularZone)
                {
                    if (z.Name == parentZone.Name)
                    {
                        zoneWidths[zi] = (z as RectangularZone).Width;
                        zoneLength = (z as RectangularZone).Length;
                    }
                    else
                    {
                        double overlap = plantWidth - (parentZone as RectangularZone).Width;
                        zoneWidths[zi] = overlap;
                    }
                }
                else
                {
                    zoneWidths[zi] = 1.0;
                }
                zi += 1;
            }
            double plantLength = Math.Min(zoneLength, PlantWidth); //Assume plant is square, length represents spaciing so will not exceed zone length as plants start touching
            double plantArea = plantWidth * plantLength;
            zi = 0;
            foreach (Zone z in zones)
            {
                ISurfaceOrganicMatter somZone = Structure.FindChild<ISurfaceOrganicMatter>(relativeTo: z);
                double rza = zoneWidths[zi] / totalWidth;
                somZone.Add((wt/plantArea) * 10 * rza, (n /plantArea) * 10 * rza, 0, parentPlant.PlantType, Name);
                zi += 1; 
            }
        }

        /// <summary>
        /// set initial biomass for organ
        /// </summary>
        private void initialiseSOMZones()
        {
            Simulation sim = Structure.FindParent<Simulation>();
            List<Zone> zones = Structure.FindAll<Zone>(relativeTo: sim).ToList();
            foreach (Zone z in zones)
            {
                simArea += z.Area;
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
