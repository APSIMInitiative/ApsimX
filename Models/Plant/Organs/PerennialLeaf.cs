using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using Models.PMF.Phen;
using Models.PMF.Struct;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// # [Name]
    /// This organ is parameterised using a simple leaf organ type which provides the core functions of intercepting radiation, providing a photosynthesis supply and a transpiration demand.  It also calculates the growth, senescence and detachment of leaves.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PerennialLeaf : Model, IOrgan, ICanopy, IArbitration, IHasWaterDemand, IRemovableBiomass
    {
        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;


        /// <summary>Carbon concentration</summary>
        /// [Units("-")]
        [Link]
        IFunction CarbonConcentration = null;

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

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

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

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get;  set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get;  set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        public double MaintenanceRespiration { get; set; }

        /// <summary>Growth Respiration</summary>
        public double GrowthRespiration { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Removed = new Biomass();

        /// <summary>Gets the DM amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax
        {
            get { return Gsmax350 * FRGR * StomatalConductanceCO2Modifier.Value(); }
        }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Maximum stomatal conductance at CO2 concentration of 350 ppm (m/s)")]
        public double Gsmax350 { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50")]
        public double R50 { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                double lai = 0;
                foreach (PerrenialLeafCohort L in Leaves)
                    lai = lai + L.Area;
                return lai;
            }
        }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }
        
        /// <summary>Gets the SLA</summary>
        public double SpecificLeafArea { get { return MathUtilities.Divide(LAI, Live.Wt, 0.0); } }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen
        {
            get
            {
                if (Plant.IsAlive)
                {
                    double greenCover = 1.0 - Math.Exp(-ExtinctionCoefficient.Value() * LAI);
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
        public double Depth { get { return Height; } }
        
        /// <summary>Gets the width of the canopy (mm).</summary>
        public double Width { get { return 0; } }

        /// <summary>Gets or sets the FRGR.</summary>
        [Units("mm")]
        public double FRGR { get; set; }
         
        private double _PotentialEP = 0;
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP
        {
            get { return _PotentialEP; }
            set { _PotentialEP = value;}
        }

        /// <summary>Sets the actual water demand.</summary>
        [Units("mm")]
        public double WaterDemand { get; set; }


        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        #endregion

        #region Parameters
        /// <summary>The FRGR function</summary>
        [Link]
        IFunction FRGRFunction = null;   
        /// <summary>The effect of CO2 on stomatal conductance</summary>
        [Link]
        IFunction StomatalConductanceCO2Modifier = null;



        /// <summary>The DM demand function</summary>
        [ChildLinkByName]
        [Units("g/m2/d")]
        private BiomassDemand dmDemands = null;

        /// <summary>The N demand function</summary>
        [ChildLinkByName]
        [Units("g/m2/d")]
        private BiomassDemand nDemands = null;

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
        /// <summary>Leaf Residence Time</summary>
        [Link]
        IFunction LeafResidenceTime = null;
        /// <summary>Leaf Development Rate</summary>
        [Link]
        IFunction LeafDevelopmentRate = null;
        /// <summary>Leaf Death</summary>
        [Link]
        IFunction LeafKillFraction = null;
        /// <summary>Minimum LAI</summary>
        [Link]
        IFunction MinimumLAI = null;
        /// <summary>Leaf Detachment Time</summary>
        [Link]
        IFunction LeafDetachmentTime = null;
        /// <summary>SpecificLeafArea</summary>
        [Link]
        IFunction SpecificLeafAreaFunction = null;

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
            return WaterDemand;
        }
        /// <summary>Gets the transpiration.</summary>
        public double Transpiration { get { return WaterAllocation; } }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public double NFixationCost { get { return 0; } }
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
        public double Fn
        {
            get
            {
                double value = MathUtilities.Divide(Live.NConc-MinimumNConc.Value(), MaximumNConc.Value()-MinimumNConc.Value(), 1);
                value = MathUtilities.Bound(value, 0, 1);
                return value;
            }
        }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get
            {
                double lai = 0;
                foreach (PerrenialLeafCohort L in Leaves)
                    lai = lai + L.AreaDead;
                return lai;
            }
        }

        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get { return 1.0 - Math.Exp(-ExtinctionCoefficientDead.Value() * LAIDead); } }

        /// <summary>Gets the RAD int tot.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                 double TotalRadn = 0;
                 for (int i = 0; i < LightProfile.Length; i++)
                     TotalRadn += LightProfile[i].amount;
                 return TotalRadn;
            }
        }

        /// <summary>Apex number by age</summary>
        /// <param name="age">Threshold age</param>
        public double ApexNumByAge(double age) { return 0; }
        #endregion

        #region Arbitrator Methods

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        private void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.Fixation = Photosynthesis.Value();
            DMSupply.Retranslocation = StartLive.StorageWt * DMRetranslocationFactor.Value();
            DMSupply.Reallocation = 0.0;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        private void SetNSupply(object sender, EventArgs e)
        {
            double LabileN = Math.Max(0, StartLive.StorageN - StartLive.StorageWt * MinimumNConc.Value());
            Biomass Senescing = new Biomass();
            GetSenescingLeafBiomass(out Senescing);

            NSupply.Reallocation = Senescing.StorageN * NReallocationFactor.Value();
            NSupply.Retranslocation = (LabileN - StartNReallocationSupply) * NRetranslocationFactor.Value();
            NSupply.Uptake = 0.0;        
        }
        

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        private void SetDMDemand(object sender, EventArgs e)
        {
            DMDemand.Structural = dmDemands.Structural.Value();
            DMDemand.Storage = 0;
            DMDemand.Metabolic = 0;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        private void SetNDemand(object sender, EventArgs e)
        {
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = 0.0; // nDemands.Metabolic.Value();
            NDemand.Storage =  nDemands.Storage.Value();
        }


        /// <summary>Gets or sets the water allocation.</summary>
        [XmlIgnore]
        public double WaterAllocation { get; set; }
        #endregion

        #region Events


        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Phenology != null)
                if (Phenology.OnStartDayOf("Emergence"))
                    if (Structure != null)
                        Structure.LeafTipsAppeared = 1.0;
        }
        #endregion

        #region Component Process Functions

        /// <summary>Clears this instance.</summary>
        protected void Clear()
        {
            Height = 0;
            StartNRetranslocationSupply = 0;
            StartNReallocationSupply = 0;
            LiveFWt = 0;
            DMDemand.Clear();
            DMSupply.Clear();
            NDemand.Clear();
            NSupply.Clear();
            Detached.Clear();
            Leaves.Clear();
            if (Structure != null)
                Structure.LeafTipsAppeared = 0;

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
        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }
        
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
            public double Area = 0;
            public double AreaDead = 0;
            public Biomass Live = new Biomass();
            public Biomass Dead = new Biomass();
        }

        private List<PerrenialLeafCohort> Leaves = new List<PerrenialLeafCohort>();

        private void AddNewLeafMaterial(double StructuralWt, double StorageWt, double StructuralN, double StorageN, double SLA)
        {
            Leaves[Leaves.Count - 1].Live.StructuralWt += StructuralWt;
            Leaves[Leaves.Count - 1].Live.StorageWt += StorageWt;
            Leaves[Leaves.Count - 1].Live.StructuralN += StructuralN;
            Leaves[Leaves.Count - 1].Live.StorageN += StorageN;
            Leaves[Leaves.Count - 1].Area += (StructuralWt + StorageWt) * SLA;
        }

        private void ReduceLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Live.Multiply(fraction);
                L.Area *= fraction;
            }
        }
        private void ReduceDeadLeavesUniformly(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Dead.Multiply(fraction);
                L.AreaDead *= fraction;
            }
        }
        private void RespireLeafFraction(double fraction)
        {
            foreach (PerrenialLeafCohort L in Leaves)
            {
                L.Live.StorageWt *= (1 - fraction);
                L.Live.MetabolicWt *= (1 - fraction);
            }
        }

        private void GetSenescingLeafBiomass(out Biomass Senescing)
        {
            Senescing = new Biomass();
            double LRT = LeafResidenceTime.Value();
            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= LRT)
                    Senescing.Add(L.Live);
        }

        private void SenesceLeaves()
        {
            double LRT = LeafResidenceTime.Value();

            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= LRT)
                {
                    L.Dead.Add(L.Live);
                    L.AreaDead += L.Area;
                    L.Live.Clear();
                    L.Area = 0;
                }
        }

        private void KillLeavesUniformly(double fraction)
        {
            Biomass Loss = new Biomass();

            foreach (PerrenialLeafCohort L in Leaves)
            {            
                Loss.SetTo(L.Live);
                Loss.Multiply(fraction);
                L.Dead.Add(Loss);
                L.Live.Subtract(Loss);
                L.AreaDead += L.Area * fraction;
                L.Area *= (1 - fraction);
            }
        }
        private Biomass DetachLeaves()
        {
            Detached = new Biomass();
            double LRT = LeafResidenceTime.Value();
            double LDT = LeafDetachmentTime.Value();

            foreach (PerrenialLeafCohort L in Leaves)
                if (L.Age >= (LRT + LDT))
                    Detached.Add(L.Dead);
            Leaves.RemoveAll(L => L.Age >= (LRT + LDT));
            return Detached;
        }

        #endregion

        #region Organ functions

        #endregion

        #region Arbitrator methods

        /// <summary>Sets the dry matter potential allocation.</summary>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Structural = dryMatter.Structural;
        }

        /// <summary>Sets the dry matter allocation.</summary>
        public void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double growthRespFactor = ((1 / DMConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * CarbonConcentration.Value()) * 44.0 / 12.0;
            GrowthRespiration = (dryMatter.Structural + dryMatter.Storage) * growthRespFactor;
            
            AddNewLeafMaterial(StructuralWt: Math.Min(dryMatter.Structural * DMConversionEfficiency.Value(), DMDemand.Structural),
                               StorageWt: dryMatter.Storage * DMConversionEfficiency.Value(),
                               StructuralN: 0,
                               StorageN: 0,
                               SLA: SpecificLeafAreaFunction.Value());

            double Removal = dryMatter.Retranslocation;
            foreach (PerrenialLeafCohort L in Leaves)
            {
                double Delta = Math.Min(L.Live.StorageWt, Removal);
                L.Live.StorageWt -= Delta;
                Removal -= Delta;
            }
            if (MathUtilities.IsGreaterThan(Removal, 0))
                throw new Exception("Insufficient Storage DM to account for Retranslocation and Reallocation in Perrenial Leaf");
        }

        /// <summary>Sets the n allocation.</summary>
        public void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            AddNewLeafMaterial(StructuralWt: 0,
                StorageWt: 0,
                StructuralN: nitrogen.Structural,
                StorageN: nitrogen.Storage,
                SLA: SpecificLeafAreaFunction.Value());

            double Removal = nitrogen.Retranslocation + nitrogen.Reallocation;
            foreach (PerrenialLeafCohort L in Leaves)
            {
                double Delta = Math.Min(L.Live.StorageN, Removal);
                L.Live.StorageN -= Delta;
                Removal -= Delta;
            }
            if (MathUtilities.IsGreaterThan(Removal, 0))
                throw new Exception("Insufficient Storage N to account for Retranslocation and Reallocation in Perrenial Leaf");
        }

        /// <summary>Remove maintenance respiration from live component of organs.</summary>
        /// <param name="respiration">The respiration to remove</param>
        public virtual void RemoveMaintenanceRespiration(double respiration)
        {
            double total = Live.MetabolicWt + Live.StorageWt;
            if (respiration > total)
            {
                throw new Exception("Respiration is more than total biomass of metabolic and storage in live component.");
            }
            Live.MetabolicWt = Live.MetabolicWt - (respiration * Live.MetabolicWt / total);
            Live.StorageWt = Live.StorageWt - (respiration * Live.StorageWt / total);
        }


        /// <summary>Gets or sets the maximum nconc.</summary>
        public double MaxNconc { get { return MaximumNConc.Value(); } }

        /// <summary>Gets or sets the minimum nconc.</summary>
        public double MinNconc { get { return MinimumNConc.Value(); } }

        /// <summary>Gets the total biomass</summary>
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the total grain weight</summary>
        [Units("g/m2")]
        public double Wt { get { return Total.Wt; } }

        /// <summary>Gets the total grain N</summary>
        [Units("g/m2")]
        public double N { get { return Total.N; } }

        #endregion

        #region Events and Event Handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// 
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            DMDemand = new BiomassPoolType();
            NDemand = new BiomassPoolType();
            DMSupply = new BiomassSupplyType();
            NSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            Detached = new Biomass();
            Clear();
        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
                Clear();
        }

        /// <summary>Kill a fraction of the green leaf</summary>
        /// <param name="fraction">The fraction of leaf to kill</param>
        public void Kill(double fraction)
        {
            Summary.WriteMessage(this, "Killing " + fraction + " of live leaf on plant");
            KillLeavesUniformly(fraction);
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                Detached.Clear();
                FRGR = FRGRFunction.Value();
                Height = HeightFunction.Value();
                //Initialise biomass and nitrogen

                Leaves.Add(new PerrenialLeafCohort());
                if (Leaves.Count == 1)
                    AddNewLeafMaterial(StructuralWt: InitialWtFunction.Value(),
                                       StorageWt: 0,
                                       StructuralN: InitialWtFunction.Value() * MinimumNConc.Value(),
                                       StorageN: InitialWtFunction.Value() * (MaximumNConc.Value() - MinimumNConc.Value()),
                                       SLA: SpecificLeafAreaFunction.Value());

                double LDR = LeafDevelopmentRate.Value();
                foreach (PerrenialLeafCohort L in Leaves)
                    L.Age+=LDR;

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
                double LKF = Math.Max(0.0, Math.Min(LeafKillFraction.Value(), (1 - MinimumLAI.Value() / LAI)));
                if (LKF>0)
                   KillLeavesUniformly(LKF);
                Detached = DetachLeaves();
                
                if (Detached.Wt > 0.0)
                    SurfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, Plant.CropType, Name);

                MaintenanceRespiration = 0;
                //Do Maintenance respiration
                if (MaintenanceRespirationFunction != null && (Live.MetabolicWt + Live.StorageWt) > 0)
                {
                    MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value();
                    RespireLeafFraction(MaintenanceRespirationFunction.Value());
                    MaintenanceRespiration += Live.StorageWt * MaintenanceRespirationFunction.Value();
                }

                if (DryMatterContent != null)
                    LiveFWt = Live.Wt / DryMatterContent.Value();
            }
        }

        #endregion

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
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
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        virtual public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            Biomass liveAfterRemoval = Live;
            Biomass deadAfterRemoval = Dead;
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, liveAfterRemoval, deadAfterRemoval, Removed, Detached);

            double remainingLiveFraction = MathUtilities.Divide(liveAfterRemoval.Wt, Live.Wt, 0);
            double remainingDeadFraction = MathUtilities.Divide(deadAfterRemoval.Wt, Dead.Wt, 0);

            ReduceLeavesUniformly(remainingLiveFraction);
            ReduceDeadLeavesUniformly(remainingDeadFraction);
        }
    }
}