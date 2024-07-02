using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.CLEM.Timers;
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

    public class Organ : Model, IOrgan, IHasDamageableBiomass
    {
        ///0. Redundant satisification of IOrgan
        ///--------------------------------------------------------------------------------------------------
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
            Clear();
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
        public OrganNutrientsState Total { get { return OrganNutrientsState.Add(Live, Dead, Cconc); } }

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
        public double MaxNconc { get; private set; }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MinNconc { get; private set; }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double CritNconc { get; private set; }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double Wt
        {
            get
            {
                return Live.Wt + Dead.Wt;
            }
        }

        /// <summary>Gets the total (live + dead) carbon weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double C
        {
            get
            {
                return Live.Carbon.Total + Dead.Carbon.Total;
            }
        }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double N
        {
            get
            {
                return Live.Nitrogen.Total + Dead.Nitrogen.Total;
            }
        }
        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double Nconc
        {
            get
            {
                return Wt > 0.0 ? N / Wt : 0.0;
            }
        }

        /// <summary>
        /// Gets the nitrogen factor.
        /// </summary>
        public double Fn
        {
            get
            {
                return Live != null ? MathUtilities.Divide(Live.Nitrogen.Total, Live.Wt * MaxNconc, 1) : 0;
            }
        }

        /// <summary>
        /// Gets the metabolic N concentration factor.
        /// </summary>
        public double FNmetabolic
        {
            get
            {
                return (Live != null) ? Math.Min(1.0, MathUtilities.Divide(Nconc - MinNconc, CritNconc - MinNconc, 0)) : 0;
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
            OrganNutrientsState liveExported = OrganNutrientsState.Multiply(Live, liveToRemove, Cconc);
            OrganNutrientsState liveRetained = OrganNutrientsState.Multiply(Live, liveToResidue, Cconc);
            LiveRemoved = OrganNutrientsState.Add(liveExported, liveRetained, Cconc);

            OrganNutrientsState deadExported = OrganNutrientsState.Multiply(Dead, deadToRemove, Cconc);
            OrganNutrientsState deadRetained = OrganNutrientsState.Multiply(Dead, deadToResidue, Cconc);
            DeadRemoved = OrganNutrientsState.Add(deadExported, deadRetained, Cconc);

            double fracLiveToResidue = MathUtilities.Divide(liveToResidue, (liveToResidue + liveToRemove), 0);
            double fracDeadToResidue = MathUtilities.Divide(deadToResidue, (deadToResidue + deadToRemove), 0);

            if (fracDeadToResidue + fracLiveToResidue > 0)
            {
                OrganNutrientsState totalToResidues = OrganNutrientsState.Add(liveRetained, deadRetained, Cconc);
                Biomass toResidues = totalToResidues.ToBiomass;
                surfaceOrganicMatter.Add(toResidues.Wt * 10.0, toResidues.N * 10.0, 0.0, parentPlant.PlantType, Name);
            }
            removeBiomass = true;
            return LiveRemoved.Wt + DeadRemoved.Wt;
        }

        /// <summary>Clears this instance.</summary>
        protected virtual void Clear()
        {
            Live = new OrganNutrientsState();
            Dead = new OrganNutrientsState();
            ReAllocated = new OrganNutrientsState();
            ReTranslocated = new OrganNutrientsState();
            Allocated = new OrganNutrientsState();
            Senesced = new OrganNutrientsState();
            Detached = new OrganNutrientsState();
            LiveRemoved = new OrganNutrientsState();
            DeadRemoved = new OrganNutrientsState();
            removeBiomass = false;
            resetOrganTomorrow = false;
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            ReAllocated = new OrganNutrientsState();
            ReTranslocated = new OrganNutrientsState();
            Allocated = new OrganNutrientsState();
            Senesced = new OrganNutrientsState();
            Detached = new OrganNutrientsState();
            LiveRemoved = new OrganNutrientsState();
            DeadRemoved = new OrganNutrientsState();
            removeBiomass = false;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            RootNetworkObject = this.FindChild<RootNetwork>();
            Clear();
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

        /// <summary>
        /// set initial biomass for organ
        /// </summary>
        public void initialiseBiomass()
        {
            Clear();
            ClearBiomassFlows();
            setNconcs();
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

            Live = new OrganNutrientsState(initC, initN, new NutrientPoolsState(), new NutrientPoolsState(), Cconc);
            Dead = new OrganNutrientsState();
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
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
                    Live = OrganNutrientsState.Subtract(Live, LiveRemoved, Cconc);
                    Dead = OrganNutrientsState.Subtract(Dead, DeadRemoved, Cconc);
                    removeBiomass = false;
                }

                //Do initial calculations
                SenescenceRate = senescenceRate.Value();
                DetachmentRate = detachmentRate.Value();
                setNconcs();
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
                    Senesced = OrganNutrientsState.Multiply(Live, SenescenceRate, Cconc);
                    Live = OrganNutrientsState.Subtract(Live, Senesced, Cconc);

                    //Catch the bits that were reallocated and add the bits that wernt into dead.
                    NutrientPoolsState ReAllocatedC = new NutrientPoolsState(Carbon.SuppliesAllocated.ReAllocation);
                    NutrientPoolsState ReAllocatedN = new NutrientPoolsState(Nitrogen.SuppliesAllocated.ReAllocation);
                    ReAllocated = new OrganNutrientsState(ReAllocatedC, ReAllocatedN, new NutrientPoolsState(), new NutrientPoolsState(), Cconc);
                    Senesced = OrganNutrientsState.Subtract(Senesced, ReAllocated, Cconc);
                    Dead = OrganNutrientsState.Add(Dead, Senesced, Cconc);
                }

                //Retranslocate from live pools
                NutrientPoolsState ReTranslocatedC = new NutrientPoolsState(Carbon.SuppliesAllocated.ReTranslocation);
                NutrientPoolsState ReTranslocatedN = new NutrientPoolsState(Nitrogen.SuppliesAllocated.ReTranslocation);
                ReTranslocated = new OrganNutrientsState(ReTranslocatedC, ReTranslocatedN, new NutrientPoolsState(), new NutrientPoolsState(), Cconc);
                Live = OrganNutrientsState.Subtract(Live, ReTranslocated, Cconc);

                //Add in todays fresh allocation
                NutrientPoolsState AllocatedC = new NutrientPoolsState(Carbon.DemandsAllocated);
                NutrientPoolsState AllocatedN = new NutrientPoolsState(Nitrogen.DemandsAllocated);
                Allocated = new OrganNutrientsState(AllocatedC, AllocatedN, new NutrientPoolsState(), new NutrientPoolsState(), Cconc);
                Live = OrganNutrientsState.Add(Live, Allocated, Cconc);

                // Do detachment
                if ((DetachmentRate > 0) && (Dead.Wt > 0))
                {
                    if (Dead.Weight.Total * (1.0 - DetachmentRate) < 0.00000001)
                        DetachmentRate = 1.0;  // remaining amount too small, detach all
                    Detached = OrganNutrientsState.Multiply(Dead, DetachmentRate, Cconc);
                    Dead = OrganNutrientsState.Subtract(Dead, Detached, Cconc);
                    if (RootNetworkObject == null)
                        surfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, parentPlant.PlantType, Name);
                }

                // Remove respiration
                Respired = new OrganNutrientsState(new NutrientPoolsState(respiration.CalculateLosses()),
                    new NutrientPoolsState(), new NutrientPoolsState(), new NutrientPoolsState(), Cconc);
                Live = OrganNutrientsState.Subtract(Live, Respired, Cconc);

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
                ClearBiomassFlows();
            }
        }

        private void checkMassBalance(double startLive, double startDead, string element)
        {
            double live = (double)(this.FindByPath("Live." + element).Value);
            double dead = (double)(this.FindByPath("Dead." + element).Value);
            double allocated = (double)(this.FindByPath("Allocated." + element).Value);
            double senesced = (double)(this.FindByPath("Senesced." + element).Value);
            double reAllocated = (double)(this.FindByPath("ReAllocated." + element).Value);
            double reTranslocated = (double)(this.FindByPath("ReTranslocated." + element).Value);
            double liveRemoved = (double)(this.FindByPath("LiveRemoved." + element).Value);
            double deadRemoved = (double)(this.FindByPath("DeadRemoved." + element).Value);
            double respired = (double)(this.FindByPath("Respired." + element).Value);
            double detached = (double)(this.FindByPath("Detached." + element).Value);

            double liveBal = Math.Abs(live - (startLive + allocated - senesced - reAllocated
                                                        - reTranslocated - liveRemoved - respired));
            if (liveBal > tolerence)
                throw new Exception(element + " mass balance violation in live biomass of " + this.Name);

            double deadBal = Math.Abs(dead - (startDead + senesced - deadRemoved - detached));
            if (deadBal > tolerence)
                throw new Exception(element + " mass balance violation in dead biomass of " + this.Name);

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
                Senesced = OrganNutrientsState.Add(Detached, Live, Cconc);
                Detached = OrganNutrientsState.Add(Detached,Live, Cconc);
                Detached = OrganNutrientsState.Add(Detached, Dead, Cconc);
                Live = new OrganNutrientsState();
                Dead = new OrganNutrientsState();
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

        private void setNconcs()
        {
            MaxNconc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Storage : 0;
            MinNconc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Structural : 0;
            CritNconc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Metabolic : 0;
        }


        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {

            // add a heading, the name of this organ
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            // write the basic description of this class, given in the <summary>
            AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

            // write the memos
            foreach (IModel memo in this.FindAllChildren<Memo>())
                AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

            //// List the parameters, properties, and processes from this organ that need to be documented:

            // document DM demands
            tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
            tags.Add(new AutoDocumentation.Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", indent));
            IModel DMDemand = this.FindChild("dmDemands");
            AutoDocumentation.DocumentModel(DMDemand, tags, headingLevel + 2, indent);

            // document N demands
            tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
            tags.Add(new AutoDocumentation.Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", indent));
            IModel NDemand = this.FindChild("nDemands");
            AutoDocumentation.DocumentModel(NDemand, tags, headingLevel + 2, indent);

            // document N concentration thresholds
            IModel MinN = this.FindChild("MinimumNConc");
            AutoDocumentation.DocumentModel(MinN, tags, headingLevel + 2, indent);
            IModel CritN = this.FindChild("CriticalNConc");
            AutoDocumentation.DocumentModel(CritN, tags, headingLevel + 2, indent);
            IModel MaxN = this.FindChild("MaximumNConc");
            AutoDocumentation.DocumentModel(MaxN, tags, headingLevel + 2, indent);
            IModel NDemSwitch = this.FindChild("NitrogenDemandSwitch");
            if (NDemSwitch is Constant)
            {
                if ((NDemSwitch as Constant).Value() == 1.0)
                {
                    //Don't bother documenting as is does nothing
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor of " + (NDemSwitch as Constant).Value() + " as specified by the NitrogenDemandSwitch", indent));
                }
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch.", indent));
                AutoDocumentation.DocumentModel(NDemSwitch, tags, headingLevel + 2, indent);
            }

            // document DM supplies
            tags.Add(new AutoDocumentation.Heading("Dry Matter Supply", headingLevel + 1));
            IModel DMReallocFac = this.FindChild("DMReallocationFactor");
            if (DMReallocFac is Constant)
            {
                if ((DMReallocFac as Constant).Value() == 0)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate DM when senescence of the organ occurs.", indent));
                else
                    tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + (DMReallocFac as Constant).Value() * 100 + "% of DM that senesces each day.", indent));
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                AutoDocumentation.DocumentModel(DMReallocFac, tags, headingLevel + 2, indent);
            }
            IModel DMRetransFac = this.FindChild("DMRetranslocationFactor");
            if (DMRetransFac is Constant)
            {
                if ((DMRetransFac as Constant).Value() == 0)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural DM.", indent));
                else
                    tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + (DMRetransFac as Constant).Value() * 100 + "% of non-structural DM each day.", indent));
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                AutoDocumentation.DocumentModel(DMRetransFac, tags, headingLevel + 2, indent);
            }

            // document N supplies
            tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
            IModel NReallocFac = this.FindChild("NReallocationFactor");
            if (NReallocFac is Constant)
            {
                if ((NReallocFac as Constant).Value() == 0)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs.", indent));
                else
                    tags.Add(new AutoDocumentation.Paragraph(Name + " can reallocate up to " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day if required by the plant arbitrator to meet N demands.", indent));
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", indent));
                AutoDocumentation.DocumentModel(NReallocFac, tags, headingLevel + 2, indent);
            }
            IModel NRetransFac = this.FindChild("NRetranslocationFactor");
            if (NRetransFac is Constant)
            {
                if ((NRetransFac as Constant).Value() == 0)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N.", indent));
                else
                    tags.Add(new AutoDocumentation.Paragraph(Name + " can retranslocate up to " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day if required by the plant arbitrator to meet N demands.", indent));
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                AutoDocumentation.DocumentModel(NRetransFac, tags, headingLevel + 2, indent);
            }

            // document senescence and detachment
            tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
            IModel SenRate = this.FindChild("SenescenceRate");
            if (SenRate is Constant)
            {
                if ((SenRate as Constant).Value() == 0)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has senescence parameterised to zero so all biomass in this organ will remain alive.", indent));
                else
                    tags.Add(new AutoDocumentation.Paragraph(Name + " senesces " + (SenRate as Constant).Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.", indent));
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate.", indent));
                AutoDocumentation.DocumentModel(SenRate, tags, headingLevel + 2, indent);
            }

            IModel DetRate = this.FindChild("DetachmentRateFunction");
            if (DetRate is Constant)
            {
                if ((DetRate as Constant).Value() == 0)
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.", indent));
                else
                    tags.Add(new AutoDocumentation.Paragraph(Name + " detaches " + (DetRate as Constant).Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition.", indent));
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.", indent));
                AutoDocumentation.DocumentModel(DetRate, tags, headingLevel + 2, indent);
            }

        }
    }

}
