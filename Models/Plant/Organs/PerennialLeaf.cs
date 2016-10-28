using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// This plant organ is parameterised using a simple leaf organ type which provides the core functions of intercepting radiation, providing a photosynthesis supply and a transpiration demand.  It also calculates the growth, senescence and detachment of leaves.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PerennialLeaf : Model, IOrgan, ICanopy, ILeaf, IArbitration
    {
        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;

        /// <summary>Gets the cohort live.</summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass Live
        {
            get
            {
                Biomass live = new Biomass();
                foreach (PerrenialLeafCohort L in Leaves)
                    live.Add(L.Live);
                return live;
            }
        }

        /// <summary>Gets the cohort live.</summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass Dead
        {
            get
            {
                Biomass dead = new Biomass();
                foreach (PerrenialLeafCohort L in Leaves)
                    dead.Add(L.Dead);
                return dead;
            }
        }


        /// <summary>The plant</summary>
        [Link]
        protected Plant Plant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        protected ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The summary</summary>
        [Link]
        protected ISummary Summary = null;

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        virtual public double MaintenanceRespiration { get { return 0; } set { } }

        /// <summary>Growth Respiration</summary>
        public double GrowthRespiration { get; set; }


        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public double RemovedWt { get; set; }

        /// <summary>Gets the N amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public double RemovedN { get; set; }


        /// <summary>Gets the total (live + dead) dm (g/m2)</summary>
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) n (g/m2)</summary>
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the dm amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public double DetachedWt { get; set; }

        /// <summary>Gets the N amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public double DetachedN { get; set; }


        #region Leaf Interface
        /// <summary>
        /// 
        /// </summary>
        public bool CohortsInitialised { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int TipsAtEmergence { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int CohortsAtInitialisation { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double InitialisedCohortNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double AppearedCohortNo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double PlantAppearedLeafNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="proprtionRemoved"></param>
        public void DoThin(double proprtionRemoved) { }
        #endregion

        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("GSMAX")]
        public double Gsmax { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50")]
        public double R50 { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen
        {
            get
            {
                if (Plant.IsAlive)
                {
                    double greenCover = 1.0 - Math.Exp(-ExtinctionCoefficient.Value * LAI);
                    return Math.Min(Math.Max(greenCover, 0.0), 0.999999999); // limiting to within 10^-9, so MicroClimate doesn't complain
                }
                else
                    return 0.0;
            }
        }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height { get; set; }
        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth { get { return Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Gets or sets the FRGR.</summary>
        [Units("mm")]
        public double FRGR { get; set; }

        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        public double PotentialEP { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        #endregion

        #region Parameters
        /// <summary>The FRGR function</summary>
        [Link]
        IFunction FRGRFunction = null;   // VPD effect on Growth Interpolation Set
        /// <summary>The dm demand function</summary>
        [Link]
        IFunction DMDemandFunction = null;

        /// <summary>The lai function</summary>
        [Link]
        IFunction LAIFunction = null;
        /// <summary>The extinction coefficient function</summary>
        [Link]
        IFunction ExtinctionCoefficient = null;
        /// <summary>The photosynthesis</summary>
        [Link]
        IFunction Photosynthesis = null;
        /// <summary>The height function</summary>
        [Link]
        IFunction HeightFunction = null;
        /// <summary>The lai dead function</summary>
        [Link]
        IFunction LaiDeadFunction = null;
        /// <summary>The structural fraction</summary>
        [Link]
        IFunction StructuralFraction = null;
        /// <summary>Leaf Residence Time</summary>
        [Link]
        IFunction LeafResidenceTime = null;

        /// <summary>Leaf Detachment Time</summary>
        [Link]
        IFunction LeafDetachmentTime = null;

        /// <summary>The structure</summary>
        [Link(IsOptional = true)]
        public Structure Structure = null;
        /// <summary>The phenology</summary>
        [Link(IsOptional = true)]
        public Phenology Phenology = null;
        /// <summary>Water Demand Function</summary>
        [Link(IsOptional = true)]
        IFunction WaterDemandFunction = null;


        #endregion

        #region States and variables

        /// <summary>Gets or sets the k dead.</summary>
        public double KDead { get; set; }                  // Extinction Coefficient (Dead)
        /// <summary>Gets or sets the water demand.</summary>
        [Units("mm")]
        public double WaterDemand
        {
            get
            {
                if (WaterDemandFunction != null)
                    return WaterDemandFunction.Value;
                else
                    return PotentialEP;
            }
            set { }
        }
        /// <summary>Gets the transpiration.</summary>
        public double Transpiration { get { return WaterAllocation; } }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public double NFixationCost { get { return 0; } set { } }

        /// <summary>Gets or sets the water supply.</summary>
        /// <param name="zone">The zone.</param>
         public double[] WaterSupply(ZoneWaterAndN zone) { return null; }
        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
         public void DoWaterUptake(double[] Amount, string zoneName) { }
        /// <summary>Gets the nitrogen supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        /// <param name="NO3Supply">The returned NO3 supply</param>
        /// <param name="NH4Supply">The returned NH4 supply</param>
         public void CalcNSupply(ZoneWaterAndN zone, out double[] NO3Supply, out double[] NH4Supply)
        {
            NO3Supply = null;
            NH4Supply = null;
        }

        /// <summary>Does the Nitrogen uptake.</summary>
        /// <param name="zonesFromSoilArbitrator">List of zones from soil arbitrator</param>
         public void DoNitrogenUptake(List<ZoneWaterAndN> zonesFromSoilArbitrator) { }
        /// <summary>Gets the fw.</summary>
        public double Fw { get { return MathUtilities.Divide(WaterAllocation, WaterDemand, 1); } }

        /// <summary>Gets the function.</summary>
        public double Fn { get { return MathUtilities.Divide(Live.N, Live.Wt * MaximumNConc.Value, 1); } }

        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }


        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get { return 1.0 - Math.Exp(-KDead * LAIDead); } }

        /// <summary>Gets the RAD int tot.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot { get { return CoverGreen * MetData.Radn; } }

        #endregion

        #region Arbitrator Methods
        /// <summary>Gets or sets the water allocation.</summary>
        public double WaterAllocation { get; set; }
        
        /// <summary>Gets or sets the dm demand.</summary>
        [XmlIgnore]
        public BiomassPoolType DMDemand
        {
            get
            {
                StructuralDMDemand = DMDemandFunction.Value;
                NonStructuralDMDemand = 0;
                return new BiomassPoolType { Structural = StructuralDMDemand , NonStructural = NonStructuralDMDemand };
            }
            set { }
        }

        /// <summary>Gets or sets the dm supply.</summary>
        [XmlIgnore]
        public BiomassSupplyType DMSupply
        {
            get
            {
                return new BiomassSupplyType
                {
                    Fixation = Photosynthesis.Value,
                    Retranslocation = StartLive.NonStructuralWt * DMRetranslocationFactor.Value,
                    Reallocation = 0.0
                };
            }
            set { }
        }

        /// <summary>Gets or sets the n demand.</summary>
        [XmlIgnore]
        public BiomassPoolType NDemand
        {
            get
            {
                double StructuralDemand = MaximumNConc.Value * PotentialDMAllocation * StructuralFraction.Value;
                double NDeficit = Math.Max(0.0, MaximumNConc.Value * (Live.Wt + PotentialDMAllocation) - Live.N) - StructuralDemand;
                return new BiomassPoolType { Structural = StructuralDemand, NonStructural = NDeficit };
            }
            set { }
        }

        #endregion

        #region Events


        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Phenology != null)
                if (Phenology.OnDayOf("Emergence"))
                    if (Structure != null)
                        Structure.LeafTipsAppeared = 1.0;
        }
        #endregion

        #region Component Process Functions

        /// <summary>Clears this instance.</summary>
        protected void Clear()
        {
            Height = 0;
            LAI = 0;
            StartNRetranslocationSupply = 0;
            StartNReallocationSupply = 0;
            PotentialDMAllocation = 0;
            PotentialStructuralDMAllocation = 0;
            PotentialMetabolicDMAllocation = 0;
            StructuralDMDemand = 0;
            NonStructuralDMDemand = 0;
            LiveFWt = 0;
        }
        #endregion

        #region Top Level time step functions


        #endregion

        // ============================================================
        #region Class Structures
        /// <summary>The start live</summary>
        private Biomass StartLive = new Biomass();
        #endregion

        #region Class Parameter Function Links
        /// <summary>The senescence rate function</summary>
        [Link]
        [Units("/d")]
        IFunction SenescenceRate = null;

        /// <summary>The n reallocation factor</summary>
        [Link]
        [Units("/d")]
        IFunction NReallocationFactor = null;

        /// <summary>The n retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction NRetranslocationFactor = null;

        /// <summary>The dm retranslocation factor</summary>
        [Link]
        [Units("/d")]
        IFunction DMRetranslocationFactor = null;

        /// <summary>The initial wt function</summary>
        [Link]
        [Units("g/m2")]
        IFunction InitialWtFunction = null;
        /// <summary>The dry matter content</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        IFunction DryMatterContent = null;
        /// <summary>The maximum n conc</summary>
        [Link]
        [Units("g/g")]
        public IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Units("g/g")]
        [Link]
        public IFunction MinimumNConc = null;
        /// <summary>The proportion of biomass repired each day</summary>
        [Link(IsOptional = true)]
        public IFunction MaintenanceRespirationFunction = null;
        /// <summary>Dry matter conversion efficiency</summary>
        [Link]
        public IFunction DMConversionEfficiency = null;
        #endregion

        #region States
        /// <summary>The start n retranslocation supply</summary>
        private double StartNRetranslocationSupply = 0;
        /// <summary>The start n reallocation supply</summary>
        private double StartNReallocationSupply = 0;
        /// <summary>The potential dm allocation</summary>
        protected double PotentialDMAllocation = 0;
        /// <summary>The potential structural dm allocation</summary>
        protected double PotentialStructuralDMAllocation = 0;
        /// <summary>The potential metabolic dm allocation</summary>
        protected double PotentialMetabolicDMAllocation = 0;
        /// <summary>The structural dm demand</summary>
        protected double StructuralDMDemand = 0;
        /// <summary>The non structural dm demand</summary>
        protected double NonStructuralDMDemand = 0;


        #endregion




        #region Class properties

        /// <summary>Gets or sets the live f wt.</summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public double LiveFWt { get; set; }

        [Serializable]
        private class PerrenialLeafCohort
        {
            public double Age = 0;
            public Biomass Live = new Biomass();
            public Biomass Dead = new Biomass();
        }

        private List<PerrenialLeafCohort> Leaves = new List<PerrenialLeafCohort>();
        private void AddNewLeafMaterial(double StructuralWt, double NonStructuralWt, double StructuralN, double NonStructuralN)
        {
            Leaves[Leaves.Count - 1].Live.StructuralWt += StructuralWt;
            Leaves[Leaves.Count - 1].Live.NonStructuralWt += NonStructuralWt;
            Leaves[Leaves.Count - 1].Live.StructuralN += StructuralN;
            Leaves[Leaves.Count - 1].Live.NonStructuralN += NonStructuralN;
        }

        private void ReduceLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
                L.Live.Multiply(fraction);
        }
        private void ReduceDeadLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
                L.Dead.Multiply(fraction);
        }
        private void RespireLeafFraction(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Live.NonStructuralWt *= (1-fraction);
                L.Live.MetabolicWt *= (1-fraction);
            }
        }
        private void SenesceLeaves()
        {
            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= LeafResidenceTime.Value)
                {
                    L.Dead.Add(L.Live);
                    L.Live.Clear();
                }
        }

        private void DetachLeaves(out Biomass Detached)
        {
            Detached = new Biomass();
            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= (LeafResidenceTime.Value+ LeafDetachmentTime.Value))
                    Detached.Add(L.Dead);
            Leaves.RemoveAll(L => L.Age >= (LeafResidenceTime.Value + LeafDetachmentTime.Value));
        }


        #endregion

        #region Organ functions

        #endregion

        #region Arbitrator methods


        /// <summary>Sets the dm potential allocation.</summary>
        public BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in " + Name);
                PotentialMetabolicDMAllocation = value.Metabolic;
                PotentialStructuralDMAllocation = value.Structural;
                PotentialDMAllocation = value.Structural + value.Metabolic;
            }
        }


        /// <summary>Gets or sets the N supply.</summary>
        [XmlIgnore]
        public BiomassSupplyType NSupply
        {
            get
            {
                return new BiomassSupplyType()
                {
                    Reallocation = AvailableNReallocation(),
                    Retranslocation = AvailableNRetranslocation(),
                    Uptake = 0.0
                };
            }
            set { }
        }

        /// <summary>Gets the N amount available for retranslocation</summary>
        /// <returns>N available to retranslocate</returns>
        public double AvailableNRetranslocation()
        {
            if (NRetranslocationFactor != null)
            {
                double LabileN = Math.Max(0, StartLive.NonStructuralN - StartLive.NonStructuralWt * MinimumNConc.Value);
                return (LabileN - StartNReallocationSupply) * NRetranslocationFactor.Value;
            }
            else
            {
                //Default of 0 means retranslocation is always turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Gets the N amount available for reallocation</summary>
        /// <returns>DM available to reallocate</returns>
        public double AvailableNReallocation()
        {
            return SenescenceRate.Value * StartLive.NonStructuralN * NReallocationFactor.Value;
        }

        /// <summary>Sets the dm allocation.</summary>
        public BiomassAllocationType DMAllocation
        {
            set
            {
                GrowthRespiration = 0;
                GrowthRespiration += value.Structural * (1 - DMConversionEfficiency.Value)
                                   + value.NonStructural * (1 - DMConversionEfficiency.Value);

                AddNewLeafMaterial(StructuralWt: Math.Min(value.Structural * DMConversionEfficiency.Value, StructuralDMDemand),
                                   NonStructuralWt: value.NonStructural * DMConversionEfficiency.Value - value.Retranslocation,
                                   StructuralN: 0,
                                   NonStructuralN: 0);

            }
        }
        /// <summary>Sets the n allocation.</summary>
        public BiomassAllocationType NAllocation
        {
            set
            {
               AddNewLeafMaterial(StructuralWt: 0,
                   NonStructuralWt: 0,
                   StructuralN: value.Structural,
                   NonStructuralN: value.NonStructural- value.Retranslocation- value.Reallocation);
            }
        }

        /// <summary>Gets or sets the maximum nconc.</summary>
        public double MaxNconc { get { return MaximumNConc.Value; } }
        /// <summary>Gets or sets the minimum nconc.</summary>
        public double MinNconc { get { return MinimumNConc.Value; } }
        #endregion

        #region Events and Event Handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// 
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
                Clear();
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                FRGR = FRGRFunction.Value;
                LAI = LAIFunction.Value;
                Height = HeightFunction.Value;
                LAIDead = LaiDeadFunction.Value;

                //Initialise biomass and nitrogen

                Leaves.Add(new PerrenialLeafCohort());
                if (Leaves.Count == 1)
                {
                    AddNewLeafMaterial(StructuralWt: InitialWtFunction.Value,
                                       NonStructuralWt: 0, 
                                       StructuralN: InitialWtFunction.Value * MinimumNConc.Value, 
                                       NonStructuralN: InitialWtFunction.Value * (MaximumNConc.Value - MinimumNConc.Value));
             
                }
                foreach (PerrenialLeafCohort L in Leaves)
                    L.Age++;

                StartLive = Live;
                StartNReallocationSupply = NSupply.Reallocation;
                StartNRetranslocationSupply = NSupply.Retranslocation;
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
            {
                SenesceLeaves();
                Biomass Detached = new Biomass();
                DetachLeaves(out Detached);

                if (Detached.Wt > 0.0)
                {
                    DetachedWt += Detached.Wt;
                    DetachedN += Detached.N;
                    SurfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, Plant.CropType, Name);
                }


                MaintenanceRespiration = 0;
                //Do Maintenance respiration
                if (MaintenanceRespirationFunction != null)
                {
                    MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value;
                    RespireLeafFraction(MaintenanceRespirationFunction.Value);

                    MaintenanceRespiration += Live.NonStructuralWt * MaintenanceRespirationFunction.Value;

                }

                if (DryMatterContent != null)
                    LiveFWt = Live.Wt / DryMatterContent.Value;
            }
        }

        #endregion


        /// <summary>Called when crop is ending</summary>
        ///[EventSubscribe("PlantEnding")]
        virtual public void DoPlantEnding()
        {
            if (Wt > 0.0)
            {
                DetachedWt += Wt;
                DetachedN += N;
                SurfaceOrganicMatter.Add(Wt * 10, N * 10, 0, Plant.CropType, Name);
            }

            Clear();
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="value">The fractions of biomass to remove</param>
        virtual public void DoRemoveBiomass(OrganBiomassRemovalType value)
        {
            double totalFractionToRemove = value.FractionLiveToRemove + value.FractionDeadToRemove
                                           + value.FractionLiveToResidue + value.FractionDeadToResidue;

            double totalLiveFractionToRemove = value.FractionLiveToRemove + value.FractionLiveToResidue;
            double totalDeadFractionToRemove = value.FractionDeadToRemove + value.FractionDeadToResidue;

            if (totalLiveFractionToRemove > 1.0)
            {
                throw new Exception("The sum of FractionToResidue and FractionToRemove for "
                                    + value.Name
                                    + " is greater than 1 for live biomass.  Had this execption not triggered you would be removing more biomass from "
                                    + Name + " than there is to remove");
            }
            if (totalDeadFractionToRemove > 1.0)
            {
                throw new Exception("The sum of FractionToResidue and FractionToRemove for "
                                    + value.Name
                                    + " is greater than 1 for dead biomass.  Had this execption not triggered you would be removing more biomass from "
                                    + Name + " than there is to remove");
            }
            if (totalFractionToRemove > 0.0)
            {
                double RemainingLiveFraction = 1.0 - (value.FractionLiveToResidue + value.FractionLiveToRemove);
                double RemainingDeadFraction = 1.0 - (value.FractionDeadToResidue + value.FractionDeadToRemove);

                double detachingWt = Live.Wt * value.FractionLiveToResidue + Dead.Wt * value.FractionDeadToResidue;
                double detachingN = Live.N * value.FractionLiveToResidue + Dead.N * value.FractionDeadToResidue;
                RemovedWt += Live.Wt * value.FractionLiveToRemove + Dead.Wt * value.FractionDeadToRemove;
                RemovedN += Live.N * value.FractionLiveToRemove + Dead.N * value.FractionDeadToRemove;
                DetachedWt += detachingWt;
                DetachedN += detachingN;

                ReduceLeavesUniformly(RemainingLiveFraction);

                Dead.StructuralWt *= RemainingDeadFraction;
                Dead.NonStructuralWt *= RemainingDeadFraction;
                Dead.MetabolicWt *= RemainingDeadFraction;

                Dead.StructuralN *= RemainingDeadFraction;
                Dead.NonStructuralN *= RemainingDeadFraction;
                Dead.MetabolicN *= RemainingDeadFraction;

                SurfaceOrganicMatter.Add(detachingWt * 10, detachingN * 10, 0.0, Plant.CropType, Name);
                //TODO: theoretically the dead material is different from the live, so it should be added as a separate pool to SurfaceOM

                double toResidue = (value.FractionLiveToResidue + value.FractionDeadToResidue) / totalFractionToRemove * 100;
                double removedOff = (value.FractionLiveToRemove + value.FractionDeadToRemove) / totalFractionToRemove * 100;
                Summary.WriteMessage(this, "Removing " + (totalFractionToRemove * 100).ToString("0.0")
                                         + "% of " + Name + " Biomass from " + Plant.Name
                                         + ".  Of this " + removedOff.ToString("0.0") + "% is removed from the system and "
                                         + toResidue.ToString("0.0") + "% is returned to the surface organic matter");
            }
        }


    }
}
