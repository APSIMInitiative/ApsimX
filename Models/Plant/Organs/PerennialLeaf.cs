using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Library;
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
    public class PerennialLeaf : Model, IOrgan, ICanopy, ILeaf, IArbitration, IHasWaterDemand
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

        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        public double MaintenanceRespiration { get; set; }

        /// <summary>Growth Respiration</summary>
        public double GrowthRespiration { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>Gets the total (live + dead) dm (g/m2)</summary>
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) n (g/m2)</summary>
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>The amount of biomass detached every day</summary>
        [XmlIgnore]
        public Biomass Detached = new Biomass();

        #region Leaf Interface
        /// <summary></summary>
        public bool CohortsInitialised { get; set; }
        /// <summary></summary>
        public int TipsAtEmergence { get; set; }
        /// <summary></summary>
        public int CohortsAtInitialisation { get; set; }
        /// <summary></summary>
        public double InitialisedCohortNo { get; set; }
        /// <summary></summary>
        public double AppearedCohortNo { get; set; }
        /// <summary></summary>
        public double PlantAppearedLeafNo { get; set; }
        /// <summary></summary>
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

        private double _PotentialEP = 0;
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP
        {
            get { return _PotentialEP; }
            set
            {
                _PotentialEP = value;
                MicroClimatePresent = true;
            }
        }
        /// <summary>
        /// Flag to test if Microclimate is present
        /// </summary>
        public bool MicroClimatePresent { get; set; }

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
        /// <summary>The extinction coefficient function for dead leaves</summary>
        [Link]
        IFunction ExtinctionCoefficientDead = null;
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
        /// <summary>Leaf Death</summary>
        [Link]
        IFunction LeafKillFraction = null;
        /// <summary>Minimum LAI</summary>
        [Link]
        IFunction MinimumLAI = null;
        /// <summary>Leaf Detachment Time</summary>
        [Link]
        IFunction LeafDetachmentTime = null;
        /// <summary>The structure</summary>
        [Link(IsOptional = true)]
        public Structure Structure = null;
        /// <summary>The phenology</summary>
        [Link(IsOptional = true)]
        public Phenology Phenology = null;

        #endregion

        #region States and variables

        /// <summary>Calculate the water demand.</summary>
        public double CalculateWaterDemand()
        {
            return PotentialEP;
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
        public double Fw { get { return MathUtilities.Divide(WaterAllocation, PotentialEP, 1); } }

        /// <summary>Gets the function.</summary>
        public double Fn { get { return MathUtilities.Divide(Live.N, Live.Wt * MaximumNConc.Value, 1); } }

        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }

        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get { return 1.0 - Math.Exp(-ExtinctionCoefficientDead.Value * LAIDead); } }

        /// <summary>Gets the RAD int tot.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot { get { return CoverGreen * MetData.Radn; } }

        #endregion

        #region Arbitrator Methods
        /// <summary>Gets or sets the water allocation.</summary>
        [XmlIgnore]
        public double WaterAllocation { get; private set; }

        /// <summary>Sets the organs water allocation.</summary>
        /// <param name="allocation">The water allocation (mm)</param>
        public void SetWaterAllocation(double allocation)
        {
            WaterAllocation = allocation;
        }

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
                double NDeficit = Math.Max(0.0, MaximumNConc.Value * (Live.Wt + PotentialDMAllocation) - Live.N - StructuralDemand);

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
        /// <summary>The n reallocation factor</summary>
        [Link]
        [Units("/d")]
        IFunction NReallocationFactor = null;

        /// <summary>The n retranslocation factor</summary>
        [Link]
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

        private void GetSenescingLeafBiomass(out Biomass Senescing)
        {
            Senescing = new Biomass();
            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= LeafResidenceTime.Value)
                    Senescing.Add(L.Live);
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

        private void KillLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                Biomass Loss = new Biomass();
                Loss.SetTo(L.Live);
                Loss.Multiply(fraction);
                L.Dead.Add(Loss);
                L.Live.Subtract(Loss);
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
                double LabileN = Math.Max(0, StartLive.NonStructuralN - StartLive.NonStructuralWt * MinimumNConc.Value);
                Biomass Senescing = new Biomass();
                GetSenescingLeafBiomass(out Senescing);
                
                return new BiomassSupplyType()
                {
                    Reallocation = Senescing.NonStructuralN * NReallocationFactor.Value,
                    Retranslocation = (LabileN - StartNReallocationSupply) * NRetranslocationFactor.Value,
                    Uptake = 0.0
                };
            }
            set { }
        }


        /// <summary>Sets the dm allocation.</summary>
        public BiomassAllocationType DMAllocation
        {
            set
            {
                GrowthRespiration = value.Structural * (1 - DMConversionEfficiency.Value)
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

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                MicroClimatePresent = false;
                Clear();
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                if (MicroClimatePresent == false)
                    throw new Exception(this.Name + " is trying to calculate water demand but no MicroClimate module is present.  Include a microclimate node in your zone");

                Detached.Clear();
                FRGR = FRGRFunction.Value;
                LAI = LAIFunction.Value;
                Height = HeightFunction.Value;
                LAIDead = LaiDeadFunction.Value;

                //Initialise biomass and nitrogen

                Leaves.Add(new PerrenialLeafCohort());
                if (Leaves.Count == 1)
                    AddNewLeafMaterial(StructuralWt: InitialWtFunction.Value,
                                       NonStructuralWt: 0, 
                                       StructuralN: InitialWtFunction.Value * MinimumNConc.Value, 
                                       NonStructuralN: InitialWtFunction.Value * (MaximumNConc.Value - MinimumNConc.Value));
             
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
                double LKF = Math.Max(0.0,Math.Min(LeafKillFraction.Value, (1-MinimumLAI.Value/LAI)));
                KillLeavesUniformly(LKF);
                DetachLeaves(out Detached);

                if (Detached.Wt > 0.0)
                    SurfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, Plant.CropType, Name);

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
        [EventSubscribe("PlantEnding")]
        protected void DoPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                SurfaceOrganicMatter.Add(Wt * 10, N * 10, 0, Plant.CropType, Name);
            }
            Clear();
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="value">The fractions of biomass to remove</param>
        virtual public void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType value)
        {
            Biomass liveAfterRemoval = Live;
            Biomass deadAfterRemoval = Dead;
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, value, liveAfterRemoval, deadAfterRemoval, Removed, Detached);

            double remainingLiveFraction = MathUtilities.Divide(liveAfterRemoval.Wt, Live.Wt, 0);
            double remainingDeadFraction = MathUtilities.Divide(deadAfterRemoval.Wt,  Dead.Wt, 0);

            ReduceLeavesUniformly(remainingLiveFraction);
            ReduceDeadLeavesUniformly(remainingDeadFraction);
        }
    }
}
