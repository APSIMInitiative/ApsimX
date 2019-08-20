namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Library;
    using System;
    using System.Collections.Generic;
    using Models.PMF.Phen;
    using Newtonsoft.Json;

    /// <summary>
    /// This organ is simulated using a SimpleLeaf organ type.  It provides the core functions of intercepting radiation, producing biomass
    ///  through photosynthesis, and determining the plant's transpiration demand.  The model also calculates the growth, senescence, and
    ///  detachment of leaves.  SimpleLeaf does not distinguish leaf cohorts by age or position in the canopy.
    /// 
    /// Radiation interception and transpiration demand are computed by the MicroClimate model.  This model takes into account
    ///  competition between different plants when more than one is present in the simulation.  The values of canopy Cover, LAI, and plant
    ///  Height (as defined below) are passed daily by SimpleLeaf to the MicroClimate model.  MicroClimate uses an implementation of the
    ///  Beer-Lambert equation to compute light interception and the Penman-Monteith equation to calculate potential evapotranspiration.  
    ///  These values are then given back to SimpleLeaf which uses them to calculate photosynthesis and soil water demand.
    /// </summary>
    /// <remarks>
    /// NOTE: the summary above is used in the Apsim's autodoc.
    /// 
    /// SimpleLeaf has two options to define the canopy: the user can either supply a function describing LAI or a function describing canopy cover directly.  From either of these functions SimpleLeaf can obtain the other property using the Beer-Lambert equation with the specified value of extinction coefficient.
    /// The effect of growth rate on transpiration is captured by the Fractional Growth Rate (FRGR) function, which is passed to the MicroClimate model.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class SimpleLeaf : Model, ICanopy, IHasWaterDemand,  IOrgan, IArbitration, ICustomDocumentation, IRemovableBiomass
    {
        /// <summary>
        /// The met data
        /// </summary>
        [Link]
        public IWeather MetData = null;

        /// <summary>
        /// The plant
        /// </summary>
        [Link]
        private Plant plant = null;

        /// <summary>
        /// The surface organic matter model.
        /// </summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>
        /// Relative growth rate factor.
        /// </summary>
        [Link]
        private IFunction frgr = null;

        /// <summary>
        /// The effect of CO2 on stomatal conductance.
        /// </summary>
        [Link]
        private IFunction stomatalConductanceCO2Modifier = null;

        /// <summary>
        /// The photosynthesis function.
        /// </summary>
        [Link]
        private IFunction photosynthesis = null;

        /// <summary>
        /// The height function.
        /// </summary>
        [Link]
        private IFunction tallness = null;

        /// <summary>
        /// The lai dead function.
        /// </summary>
        [Link]
        private IFunction laiDead = null;

        /// <summary>
        /// Carbon concentration.
        /// </summary>
        [Link]
        private IFunction carbonConcentration = null;

        /// <summary>
        /// Water Demand Function.
        /// </summary>
        [Link(IsOptional = true)]
        private IFunction waterDemand = null;

        /// <summary>
        /// The cover function.
        /// </summary>
        [Link(IsOptional = true)]
        private IFunction cover = null;

        /// <summary>
        /// The lai function.
        /// </summary>
        [Link(IsOptional = true)]
        private IFunction area = null;

        /// <summary>
        /// The extinction coefficient function.
        /// </summary>
        [Link(IsOptional = true)]
        private IFunction extinctionCoefficient = null;

        /// <summary>
        /// The height of the base of the canopy.
        /// </summary>
        [Link(IsOptional = true)]
        private IFunction baseHeight = null;

        /// <summary>
        /// The with of a single plant.
        /// </summary>
        [Link(IsOptional = true)]
        private IFunction wideness = null;

        /// <summary>
        /// Link to biomass removal model.
        /// </summary>
        [ChildLink]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>
        /// The senescence rate function.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        protected IFunction senescenceRate = null;

        /// <summary>
        /// The detachment rate function.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction detachmentRate = null;

        /// <summary>
        /// The N retranslocation factor.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        protected IFunction nRetranslocationFactor = null;

        /// <summary>
        /// The N reallocation factor.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        protected IFunction nReallocationFactor = null;

        /// <summary>
        /// The DM retranslocation factor.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction dmRetranslocationFactor = null;

        /// <summary>
        /// The DM reallocation factor.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>
        /// The DM demand function.
        /// </summary>
        [ChildLinkByName]
        [Units("g/m2/d")]
        private BiomassDemand dmDemands = null;

        /// <summary>
        /// The N demand function.
        /// </summary>
        [ChildLinkByName]
        [Units("g/m2/d")]
        private BiomassDemand nDemands = null;

        /// <summary>
        /// The initial biomass dry matter weight.
        /// </summary>
        [ChildLinkByName]
        [Units("g/m2")]
        private IFunction initialWt = null;

        /// <summary>
        /// The maximum N concentration.
        /// </summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction maximumNConc = null;

        /// <summary>
        /// The minimum N concentration.
        /// </summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction minimumNConc = null;

        /// <summary>
        /// The critical N concentration.
        /// </summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction criticalNConc = null;

        /// <summary>
        /// The proportion of biomass respired each day.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction maintenanceRespiration = null;

        /// <summary>
        /// Dry matter conversion efficiency.
        /// </summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction dmConversionEfficiency = null;

        /// <summary>
        /// The cost for remobilisation.
        /// </summary>
        [ChildLinkByName]
        [Units("")]
        private IFunction remobilisationCost = null;

        /// <summary>
        /// Tolerance for biomass comparisons.
        /// </summary>
        private const double biomassToleranceValue = 0.0000000001;

        /// <summary>
        /// The live biomass state at start of the computation round.
        /// </summary>
        private Biomass startLive = null;

        /// <summary>
        /// Is leaf initialised?
        /// </summary>
        private bool leafInitialised = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SimpleLeaf()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>
        /// Albedo.
        /// </summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>
        /// Maximum stomatal conductance at CO2 concentration of 350 ppm (m/s).
        /// </summary>
        [Units("m/s")]
        [Description("Maximum stomatal conductance at CO2 concentration of 350 ppm")]
        public double Gsmax350 { get; set; }

        /// <summary>
        /// Radiation level at which stomatal conductance is half the maximum value.
        /// </summary>
        [Description("Radiation level at which stomatal conductance is half the maximum value")]
        public double R50 { get; set; }

        /// <summary>
        /// The Stage that leaves are initialised on.
        /// </summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";

        /// <summary>
        /// Leaf area index.
        /// </summary>
        [JsonIgnore]
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [Units("mm")]
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [Units("mm")]
        public double BaseHeight { get; set; }

        /// <summary>
        /// The width of an individual plant
        /// </summary>
        [Units("mm")]
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the FRGR.
        /// </summary>
        [Units("mm")]
        public double FRGR { get; set; }

        /// <summary>
        /// Sets the potential evapotranspiration. Set by micro cliamte.
        /// </summary>
        [Units("mm")]
        public double PotentialEP { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        [Units("mm")]
        public double WaterDemand { get; set; }

        /// <summary>
        /// Sets the light profile. Set by MICROCLIMATE.
        /// </summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>
        /// Extinction coefficient (dead).
        /// </summary>
        public double KDead { get; set; }

        /// <summary>
        /// Gets or sets the lai dead.
        /// </summary>
        public double LAIDead { get; set; }

        /// <summary>
        /// The dry matter supply.
        /// </summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>
        /// The nitrogen supply.
        /// </summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>
        /// The dry matter demand.
        /// </summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>
        /// Structural nitrogen demand.
        /// </summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>
        /// The dry matter potentially being allocated.
        /// </summary>
        public BiomassPoolType potentialDMAllocation { get; set; }

        /// <summary>
        /// The live biomass.
        /// </summary>
        [JsonIgnore]
        public Biomass Live { get; private set; }

        /// <summary>
        /// The dead biomass.
        /// </summary>
        [JsonIgnore]
        public Biomass Dead { get; private set; }

        /// <summary>
        /// Gets the total biomass.
        /// </summary>
        [JsonIgnore]
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>
        /// Gets the biomass allocated (represented actual growth).
        /// </summary>
        [JsonIgnore]
        public Biomass Allocated { get; private set; }

        /// <summary>
        /// Gets the biomass senesced (transferred from live to dead material).
        /// </summary>
        [JsonIgnore]
        public Biomass Senesced { get; private set; }

        /// <summary>
        /// Gets the biomass detached (sent to soil/surface organic matter).
        /// </summary>
        [JsonIgnore]
        public Biomass Detached { get; private set; }

        /// <summary>
        /// Gets the biomass removed from the system (harvested, grazed, etc.).
        /// </summary>
        [JsonIgnore]
        public Biomass Removed { get; private set; }

        /// <summary>
        /// The amount of mass lost each day from maintenance respiration.
        /// </summary>
        [JsonIgnore]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>
        /// Growth Respiration.
        /// </summary>
        [JsonIgnore]
        public double GrowthRespiration { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the biomass is above ground or not.
        /// </summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>
        /// Gets the potential DM allocation for this computation round.
        /// </summary>
        public BiomassPoolType DMPotentialAllocation
        {
            get
            {
                return potentialDMAllocation;
            }
        }

        /// <summary>
        /// Gets or sets the n fixation cost.
        /// </summary>
        [JsonIgnore]
        public virtual double NFixationCost
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the maximum N concentration.
        /// </summary>
        [JsonIgnore]
        public double MaxNconc
        {
            get
            {
                return maximumNConc.Value();
            }
        }

        /// <summary>
        /// Gets the minimum N concentration.
        /// </summary>
        [JsonIgnore]
        public double MinNconc
        {
            get
            {
                return minimumNConc.Value();
            }
        }

        /// <summary>
        /// Gets the minimum N concentration.
        /// </summary>
        [JsonIgnore]
        public double CritNconc
        {
            get
            {
                return criticalNConc.Value();
            }
        }

        /// <summary>
        /// Gets the total (live + dead) dry matter weight.
        /// </summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double Wt
        {
            get
            {
                return Live.Wt + Dead.Wt;
            }
        }

        /// <summary>
        /// Gets the total (live + dead) N amount.
        /// </summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double N
        {
            get
            {
                return Live.N + Dead.N;
            }
        }

        /// <summary>
        /// Gets the total (live + dead) N concentration.
        /// </summary>
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

        /// <summary>
        /// Gets the transpiration.
        /// </summary>
        public double Transpiration
        {
            get
            {
                return WaterAllocation;
            }
        }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        [Units("mm")]
        public double Depth
        {
            get
            {
                return Math.Max(0, Height - BaseHeight);
            }
        }

        /// <summary>
        /// Gets the canopy. Should return null if no canopy present.
        /// </summary>
        public string CanopyType
        {
            get
            {
                return plant.CropType;
            }
        }

        /// <summary>
        /// Gets the LAI live + dead.
        /// </summary>
        [Units("m^2/m^2")]
        public double LAITotal
        {
            get
            {
                return LAI + LAIDead;
            }
        }

        /// <summary>
        /// Water stress factor.
        /// </summary>
        public double Fw
        {
            get
            {
                return MathUtilities.Divide(WaterAllocation, PotentialEP, 1);
            }
        }

        /// <summary>
        /// Gets the cover total.
        /// </summary>
        [Units("0-1")]
        public double CoverTotal
        {
            get
            {
                return 1.0 - (1 - CoverGreen) * (1 - CoverDead);
            }
        }

        /// <summary>
        /// Gets the cover green.
        /// </summary>
        [Units("0-1")]
        public double CoverGreen
        {
            get
            {
                if (plant.IsAlive)
                {
                    double greenCover = 0.0;
                    if (cover == null)
                        greenCover = 1.0 - Math.Exp(-extinctionCoefficient.Value() * LAI);
                    else
                        greenCover = cover.Value();
                    return MathUtilities.Bound(greenCover, 0.0, 0.999999999); // limiting to within 10^-9, so MicroClimate doesn't complain
                }
                else
                    return 0.0;

            }
        }

        /// <summary>
        /// Gets the nitrogen factor.
        /// </summary>
        public double Fn
        {
            get
            {
                if (Live != null)
                    return MathUtilities.Divide(Live.N, Live.Wt * MaxNconc, 1);
                return 0;
            }
        }

        /// <summary>
        /// Gets the metabolic N concentration factor.
        /// </summary>
        public double FNmetabolic
        {
            get
            {
                double factor = 0.0;
                if (Live != null)
                    factor = MathUtilities.Divide(Live.N - Live.StructuralN, Live.Wt * (CritNconc - MinNconc), 1.0);
                return Math.Min(1.0, factor);
            }
        }

        /// <summary>
        /// Gets the cover dead.
        /// </summary>
        public double CoverDead
        {
            get
            {
                return 1.0 - Math.Exp(-KDead * LAIDead);
            }
        }

        /// <summary>
        /// Intercepted radiation value that is passed to the RUE class to calculate DM supply.
        /// </summary>
        [Units("MJ/m^2/day")]
        public double RadIntTot
        {
            get
            {
                if (LightProfile == null)
                    return 0;

                double totalRadn = 0;
                for (int i = 0; i < LightProfile.Length; i++)
                    totalRadn += LightProfile[i].amount;
                return totalRadn;
            }
        }

        /// <summary>
        /// Daily maximum stomatal conductance.
        /// </summary>
        [Units("m/s")]
        public double Gsmax
        {
            get
            {
                return Gsmax350 * FRGR * stomatalConductanceCO2Modifier.Value();
            }
        }

        /// <summary>
        /// Gets or sets the water allocation.
        /// </summary>
        [JsonIgnore]
        public double WaterAllocation { get; set; }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        private void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
            DMSupply.Clear();
            NSupply.Clear();
            DMDemand.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
            Height = 0;
            LAI = 0;
            leafInitialised = false;
        }

        /// <summary>
        /// Clears the transferring biomass amounts.
        /// </summary>
        private void ClearBiomassFlows()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>
        /// Calculate and return the dry matter supply (g/m2).
        /// </summary>
        [EventSubscribe("SetDMSupply")]
        private void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.Reallocation = AvailableDMReallocation();
            DMSupply.Retranslocation = AvailableDMRetranslocation();
            DMSupply.Uptake = 0;
            DMSupply.Fixation = photosynthesis.Value();
        }

        /// <summary>
        /// Called when [phase changed].
        /// </summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == LeafInitialisationStage)
                leafInitialised = true;
        }

        /// <summary>
        /// Event from sequencer telling us to do our potential growth.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (plant.IsEmerged)
                startLive = Live;
            if (leafInitialised)
            {
                FRGR = frgr.Value();
                if (cover == null && extinctionCoefficient == null)
                    throw new Exception("\"CoverFunction\" or \"ExtinctionCoefficientFunction\" should be defined in " + this.Name);
                if (cover != null)
                    LAI = (Math.Log(1 - CoverGreen) / (extinctionCoefficient.Value() * -1)) * plant.populationFactor;
                if (area != null)
                    LAI = area.Value() * plant.populationFactor;

                Height = tallness.Value();
                if (baseHeight == null)
                    BaseHeight = 0;
                else
                    BaseHeight = baseHeight.Value();
                if (wideness == null)
                    Width = 0;
                else
                    Width = wideness.Value();
                LAIDead = laiDead.Value();
            }
        }

        /// <summary>
        /// Calculate and return the nitrogen supply (g/m2).
        /// </summary>
        [EventSubscribe("SetNSupply")]
        protected virtual void SetNSupply(object sender, EventArgs e)
        {
            NSupply.Reallocation = Math.Max(0, (startLive.StorageN + startLive.MetabolicN) * senescenceRate.Value() * nReallocationFactor.Value());
            if (MathUtilities.IsNegative(NSupply.Reallocation))
                throw new Exception("Negative N reallocation value computed for " + Name);

            NSupply.Retranslocation = Math.Max(0, (startLive.StorageN + startLive.MetabolicN) * (1 - senescenceRate.Value()) * nRetranslocationFactor.Value());
            if (MathUtilities.IsNegative(NSupply.Retranslocation))
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>
        /// Calculate and return the dry matter demand (g/m2).
        /// </summary>
        [EventSubscribe("SetDMDemand")]
        protected virtual void SetDMDemand(object sender, EventArgs e)
        {
            if (MathUtilities.IsPositive(dmConversionEfficiency.Value()))
            {
                DMDemand.Structural = (dmDemands.Structural.Value() / dmConversionEfficiency.Value() + remobilisationCost.Value()) * plant.populationFactor;
                DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value() / dmConversionEfficiency.Value()) * plant.populationFactor;
                DMDemand.Metabolic = 0;
            }
            else
            {
                // Conversion efficiency is zero!!!!
                DMDemand.Structural = 0;
                DMDemand.Storage = 0;
                DMDemand.Metabolic = 0;
            }
        }

        /// <summary>
        /// Calculate and return the nitrogen demand (g/m2).
        /// </summary>
        [EventSubscribe("SetNDemand")]
        protected virtual void SetNDemand(object sender, EventArgs e)
        {
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = nDemands.Metabolic.Value();
            NDemand.Storage = nDemands.Storage.Value();
        }

        /// <summary>
        /// Called when [simulation commencing].
        /// </summary>
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
            Height = 0.0;
            LAI = 0.0;
            leafInitialised = false;
        }

        /// <summary>
        /// Called when [do daily initialisation].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (plant.IsAlive || plant.IsEnding)
                ClearBiomassFlows();
        }

        /// <summary>
        /// Called when crop is ending.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == plant)
            {
                Clear();
                ClearBiomassFlows();
                Live.StructuralWt = initialWt.Value();
                Live.StorageWt = 0.0;
                Live.StructuralN = Live.StructuralWt * minimumNConc.Value();
                Live.StorageN = (initialWt.Value() * maximumNConc.Value()) - Live.StructuralN;
            }
        }

        /// <summary>
        /// Does the nutrient allocations.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (plant.IsAlive)
            {
                // Do senescence
                double senescedFrac = senescenceRate.Value();
                if (Live.Wt * (1.0 - senescedFrac) < biomassToleranceValue)
                    senescedFrac = 1.0;  // remaining amount too small, senesce all
                Biomass Loss = Live * senescedFrac;
                Live.Subtract(Loss);
                Dead.Add(Loss);
                Senesced.Add(Loss);

                // Do detachment
                double detachedFrac = detachmentRate.Value();
                if (Dead.Wt * (1.0 - detachedFrac) < biomassToleranceValue)
                    detachedFrac = 1.0;  // remaining amount too small, detach all
                Biomass detaching = Dead * detachedFrac;
                Dead.Multiply(1.0 - detachedFrac);
                if (detaching.Wt > 0.0)
                {
                    Detached.Add(detaching);
                    surfaceOrganicMatter.Add(detaching.Wt * 10, detaching.N * 10, 0, plant.CropType, Name);
                }

                // Do maintenance respiration
                MaintenanceRespiration = 0;
                if (maintenanceRespiration != null && (Live.MetabolicWt + Live.StorageWt) > 0)
                {
                    MaintenanceRespiration += Live.MetabolicWt * maintenanceRespiration.Value();
                    MaintenanceRespiration += Live.StorageWt * maintenanceRespiration.Value();
                }
            }
        }

        /// <summary>
        /// Called when crop is ending.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            if (MathUtilities.IsPositive(Wt))
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, plant.CropType, Name);
            }

            Clear();
        }

        /// <summary>
        /// Removes biomass from organs when harvest, graze or cut events are called.
        /// </summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public virtual void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }
        
        /// <summary>
        /// Calculates the water demand.
        /// </summary>
        public double CalculateWaterDemand()
        {
            if (waterDemand != null)
                return waterDemand.Value();

            return WaterDemand;
        }

        /// <summary>
        /// Computes the amount of DM available for retranslocation.
        /// </summary>
        public double AvailableDMRetranslocation()
        {
            double availableDM = Math.Max(0.0, startLive.StorageWt - DMSupply.Reallocation) * dmRetranslocationFactor.Value();
            if (MathUtilities.IsNegative(availableDM))
                throw new Exception("Negative DM retranslocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>
        /// Computes the amount of DM available for reallocation.
        /// </summary>
        public double AvailableDMReallocation()
        {
            double availableDM = startLive.StorageWt * senescenceRate.Value() * dmReallocationFactor.Value();
            if (MathUtilities.IsNegative(availableDM))
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>
        /// Sets the dry matter potential allocation.
        /// </summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>
        /// Sets the dry matter allocation.
        /// </summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public virtual void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // Check retranslocation
            if (MathUtilities.IsGreaterThan(dryMatter.Retranslocation, startLive.StorageWt))
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            // get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double growthRespFactor = ((1.0 / dmConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * carbonConcentration.Value()) * 44.0 / 12.0;
            GrowthRespiration = 0.0;

            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural * dmConversionEfficiency.Value(), DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            GrowthRespiration += Allocated.StructuralWt * growthRespFactor;

            // allocate non structural DM
            if (MathUtilities.IsGreaterThan(dryMatter.Storage * dmConversionEfficiency.Value(), DMDemand.Storage))
                throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");

            // Allocated.StorageWt = dryMatter.Storage * dmConversionEfficiency.Value();
            double diffWt = dryMatter.Storage - dryMatter.Retranslocation;
            if (MathUtilities.IsPositive(diffWt))
            {
                diffWt *= dmConversionEfficiency.Value();
                GrowthRespiration += diffWt * growthRespFactor;
            }
            Allocated.StorageWt = diffWt;
            Live.StorageWt += diffWt;

            // allocate metabolic DM
            Allocated.MetabolicWt = dryMatter.Metabolic * dmConversionEfficiency.Value();
            GrowthRespiration += Allocated.MetabolicWt * growthRespFactor;

        }

        /// <summary>
        /// Sets the n allocation.
        /// </summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public virtual void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, startLive.StorageN + startLive.MetabolicN - NSupply.Retranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double storageNRetranslocation = Math.Min(nitrogen.Retranslocation, startLive.StorageN * (1 - senescenceRate.Value()) * nRetranslocationFactor.Value());
            Live.StorageN -= storageNRetranslocation;
            Live.MetabolicN -= (nitrogen.Retranslocation - storageNRetranslocation);
            Allocated.StorageN -= nitrogen.Retranslocation;

            // Reallocation
            if (MathUtilities.IsGreaterThan(nitrogen.Reallocation, startLive.StorageN + startLive.MetabolicN))
                throw new Exception("N reallocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double storageNReallocation = Math.Min(nitrogen.Reallocation, startLive.StorageN * senescenceRate.Value() * nReallocationFactor.Value());
            Live.StorageN -= storageNReallocation;
            Live.MetabolicN -= (nitrogen.Reallocation - storageNReallocation);
            Allocated.StorageN -= nitrogen.Reallocation;
        }

        /// <summary>
        /// Remove maintenance respiration from live component of organs.
        /// </summary>
        /// <param name="respiration">The respiration to remove</param>
        public virtual void RemoveMaintenanceRespiration(double respiration)
        {
            double total = Live.MetabolicWt + Live.StorageWt;
            if (respiration > total)
                throw new Exception("Respiration is more than total biomass of metabolic and storage in live component.");
            Live.MetabolicWt = Live.MetabolicWt - MathUtilities.Divide(respiration * Live.MetabolicWt , total, 0);
            Live.StorageWt = Live.StorageWt - MathUtilities.Divide(respiration * Live.StorageWt , total, 0);
        }

        /// <summary>
        /// Writes documentation for this function by adding to the list of documentation tags.
        /// </summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading, the name of this organ
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write the basic description of this class, given in the <summary>
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write the memos
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                //// List the parameters, properties, and processes from this organ that need to be documented:

                // document initial DM weight
                IModel iniWt = Apsim.Child(this, "initialWt");
                AutoDocumentation.DocumentModel(iniWt, tags, headingLevel + 1, indent);

                // document DM demands
                tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", indent));
                IModel DMDemand = Apsim.Child(this, "dmDemands");
                AutoDocumentation.DocumentModel(DMDemand, tags, headingLevel + 2, indent);

                // document N demands
                tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", indent));
                IModel NDemand = Apsim.Child(this, "nDemands");
                AutoDocumentation.DocumentModel(NDemand, tags, headingLevel + 2, indent);

                // document N concentration thresholds
                IModel MinN = Apsim.Child(this, "MinimumNConc");
                AutoDocumentation.DocumentModel(MinN, tags, headingLevel + 2, indent);
                IModel CritN = Apsim.Child(this, "CriticalNConc");
                AutoDocumentation.DocumentModel(CritN, tags, headingLevel + 2, indent);
                IModel MaxN = Apsim.Child(this, "MaximumNConc");
                AutoDocumentation.DocumentModel(MaxN, tags, headingLevel + 2, indent);
                IModel NDemSwitch = Apsim.Child(this, "NitrogenDemandSwitch");
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
                IModel DMReallocFac = Apsim.Child(this, "DMReallocationFactor");
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
                IModel DMRetransFac = Apsim.Child(this, "DMRetranslocationFactor");
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

                // document photosynthesis
                IModel PhotosynthesisModel = Apsim.Child(this, "Photosynthesis");
                AutoDocumentation.DocumentModel(PhotosynthesisModel, tags, headingLevel + 2, indent);

                // document N supplies
                tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
                IModel NReallocFac = Apsim.Child(this, "NReallocationFactor");
                if (NReallocFac is Constant)
                {
                    if ((NReallocFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NReallocFac, tags, headingLevel + 2, indent);
                }
                IModel NRetransFac = Apsim.Child(this, "NRetranslocationFactor");
                if (NRetransFac is Constant)
                {
                    if ((NRetransFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NRetransFac, tags, headingLevel + 2, indent);
                }

                // document canopy
                tags.Add(new AutoDocumentation.Heading("Canopy Properties", headingLevel + 1));
                IModel laiF = Apsim.Child(this, "Area");
                IModel coverF = Apsim.Child(this, "Cover");
                if (laiF != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has been defined with a LAIFunction, cover is calculated using the Beer-Lambert equation.", indent));
                    AutoDocumentation.DocumentModel(laiF, tags, headingLevel + 2, indent);
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has been defined with a CoverFunction. LAI is calculated using an inverted Beer-Lambert equation", indent));
                    AutoDocumentation.DocumentModel(coverF, tags, headingLevel + 2, indent);
                }
                IModel exctF = Apsim.Child(this, "ExtinctionCoefficient");
                AutoDocumentation.DocumentModel(exctF, tags, headingLevel + 2, indent);
                IModel heightF = Apsim.Child(this, "Tallness");
                AutoDocumentation.DocumentModel(heightF, tags, headingLevel + 2, indent);

                // document senescence and detachment
                tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
                IModel SenRate = Apsim.Child(this, "SenescenceRate");
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

                IModel DetRate = Apsim.Child(this, "DetachmentRate");
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

                if (biomassRemovalModel != null)
                    biomassRemovalModel.Document(tags, headingLevel + 1, indent);
            }
        }
    }
}