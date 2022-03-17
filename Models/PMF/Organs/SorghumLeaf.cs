using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using Models.PMF.Struct;
using Newtonsoft.Json;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Documentation;
using Models.Utilities;

namespace Models.PMF.Organs
{

    /// <summary>
    /// This organ is simulated using a SimpleLeaf organ type.  It provides the core functions of intercepting radiation, producing biomass
    /// through photosynthesis, and determining the plant's transpiration demand.  The model also calculates the growth, senescence, and
    /// detachment of leaves.  SimpleLeaf does not distinguish leaf cohorts by age or position in the canopy.
    /// 
    /// Radiation interception and transpiration demand are computed by the MicroClimate model.  This model takes into account
    /// competition between different plants when more than one is present in the simulation.  The values of canopy Cover, LAI, and plant
    /// Height (as defined below) are passed daily by SimpleLeaf to the MicroClimate model.  MicroClimate uses an implementation of the
    /// Beer-Lambert equation to compute light interception and the Penman-Monteith equation to calculate potential evapotranspiration.  
    /// These values are then given back to SimpleLeaf which uses them to calculate photosynthesis and soil water demand.
    /// </summary>
    /// <remarks>
    /// SimpleLeaf has two options to define the canopy: the user can either supply a function describing LAI or a function describing canopy cover directly.  From either of these functions SimpleLeaf can obtain the other property using the Beer-Lambert equation with the specified value of extinction coefficient.
    /// The effect of growth rate on transpiration is captured by the Fractional Growth Rate (FRGR) function, which is passed to the MicroClimate model.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SorghumLeaf : Model, IHasWaterDemand, IOrgan, IArbitration, IOrganDamage, ICanopy
    {
        /// <summary>The plant</summary>
        [Link]
        private Plant plant = null; //todo change back to private

        [Link]
        private ISummary summary = null;

        /// <summary> Culms on the leaf controls tillering</summary>
        [Link]
        public LeafCulms culms = null;

        /// <summary>Phenology</summary>
        [Link]
        public Phenology phenology = null; 

        /// <summary>The met data</summary>
        [Link]
        private IWeather metData = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        [Link(Type = LinkType.Path, Path = "[Phenology].DltTT")]
        private IFunction dltTT { get; set; }

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frgr = null;

        /// <summary>The effect of CO2 on stomatal conductance</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction stomatalConductanceCO2Modifier = null;

        /// <summary>The extinction coefficient function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction extinctionCoefficientFunction = null; //access lvl changed for LightSenescenceFunction

        /// <summary>The photosynthesis</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction photosynthesis = null; //waterSenescence

        /// <summary>The height function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction heightFunction = null;

        /// <summary>Water Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction waterDemandFunction = null;

        /// <summary>DM Fixation Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction dMSupplyFixation = null;

        /// <summary>DM Fixation Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction potentialBiomassTEFunction = null; //waterSenescence

        /// <summary>Input for TargetSLN</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction targetSLN = null;

        /// <summary>Slope for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minPlantWt = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nPhotoStressFunction = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand dmDemands = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand nDemands = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction numberOfLeaves = null;

        /// <summary>Light Senescence function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction AgeSenescence = null;

        /// <summary>Light Senescence function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction LightSenescence = null;

        /// <summary>Water Senescence function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction WaterSenescence = null;

        /// <summary>Water Senescence function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction FrostSenescence = null;

        private double potentialEP = 0;
        private bool leafInitialised = false;
        private double nDeadLeaves;
        private double dltDeadLeaves;
        
        /// <summary>Tolerance for biomass comparisons</summary>
        protected double biomassToleranceValue = 0.0000000001;

        /// <summary>Constructor</summary>
        public SorghumLeaf()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Gets the canopy type. Should return null if no canopy present.</summary>
        public string CanopyType => plant.PlantType;

        /// <summary>Gets or sets the R50.</summary>
        [Description("Tillering Method: 0 = Fixed - uses FTN, 1 = Dynamic")]
        public int TilleringMethod { get; set; } = 0;

        /// <summary>The initial biomass dry matter weight</summary>
        [Description("Initial leaf dry matter weight")]
        [Units("g/m2")]
        public double InitialDMWeight { get; set; } = 0.2;

        /// <summary>Initial LAI</summary>
        [Description("Initial LAI")]
        [Units("g/m2")]
        public double InitialLAI { get; set; } = 200.0;

        /// <summary>The initial SLN value</summary>
        [Description("Initial SLN")]
        [Units("g/m2")]
        public double InitialSLN { get; set; } = 1.5;

        /// <summary>Input for NewLeafSLN</summary>
        [Description("Input for NewLeafSLN")]
        public double NewLeafSLN { get; set; } = 1.0;

        /// <summary>Input for SenescedLeafSLN.</summary>
        [Description("Senesced Leaf SLN")]
        public double SenescedLeafSLN { get; set; } = 0.3;

        /// <summary>Intercept for N Dilutions</summary>
        [Description("Intercept for N Dilutions")]
        public double NDilutionIntercept { get; set; } = -0.0017;

        /// <summary>Slope for N Dilutions</summary>
        [Description("Slope for N Dilutions")]
        public double NDilutionSlope { get; set; } = 0.0043;

        /// <summary>Maximum canopy width - used to calcuate cover under skip row configurations</summary>
        [Description("Max Canopy Width")]
        [Units("mm")]
        public double CanopyWidth { get; set; } = 1000.0;

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax => Gsmax350 * FRGR * stomatalConductanceCO2Modifier.Value();

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Maximum stomatal conductance at CO2 concentration of 350 ppm (m/s)")]
        public double Gsmax350 { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50: solar radiation at which stomatal conductance decreases to 50 % (W / m ^ 2)")]
        public double R50 { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("Use MicroClimate: 0 = No, 1 = yes")]
        public int MicroClimateSetting { get; set; }

        /// <summary>Gets the MicroClimate setting.</summary>
        [JsonIgnore]
        public bool UseMicroClimate => MicroClimateSetting > 0;

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double BaseHeight { get; set; }

        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth => Math.Max(0, Height - BaseHeight);

        /// <summary>The width of an individual plant</summary>
        [Units("mm")]
        public double Width { get; set; }

        /// <summary>The Fractional Growth Rate (FRGR) function.</summary>
        [Units("mm")]
        public double FRGR => frgr.Value();

        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP
        {
            get { return potentialEP; }
            set
            {
                potentialEP = value;
                MicroClimatePresent = true;
            }
        }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double DltLAI { get; set; }
        
        /// <summary>Gets the Potential DltLAI</summary>
        [Units("m^2/m^2")]
        public double DltPotentialLAI { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double DltStressedLAI { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal => LAI + LAIDead;

        /// <summary>Gets the LAI</summary>
        public double SLN { get; set; }

        /// <summary>Used in metabolic ndemand calc.</summary>
        public double SLN0 { get; set; }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen { get; set; }

        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get; set; }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal => 1.0 - (1 - CoverGreen) * (1 - CoverDead); 

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        [Units("mm")]
        public double WaterDemand { get; set; }

        /// <summary> Flag to test if Microclimate is present </summary>
        public bool MicroClimatePresent { get; set; } = false;

        /// <summary>Potential Biomass via Radiation Use Efficientcy.</summary>
        public double BiomassRUE { get; set; }

        /// <summary>Potential Biomass via Radiation Use Efficientcy.</summary>
        public double BiomassTE { get; set; }

        /// <summary>Gets or sets the Extinction Coefficient (Dead).</summary>
        public double KDead { get; set; }                  // 

        /// <summary>Gets the transpiration.</summary>
        public double Transpiration => WaterAllocation;
        
        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }

        /// <summary>
        /// Intercepted radiation value that is passed to the RUE class to calculate DM supply.
        /// </summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadiationIntercepted
        {
            get
            {
                if (UseMicroClimate)
                {
                    if (LightProfile == null)
                        return 0;

                    double totalRadn = 0;
                    for (int i = 0; i < LightProfile.Length; i++)
                        totalRadn += LightProfile[i].AmountOnGreen;
                    return totalRadn;
                }
                return CoverGreen * metData.Radn;
            }
        }

        /// <summary>Nitrogen Photosynthesis Stress.</summary>
        public double NitrogenPhotoStress { get; set; }

        /// <summary>Nitrogen Phenology Stress.</summary>
        public double NitrogenPhenoStress { get; set; }

        /// <summary>Phosphorous Stress.</summary>
        public double PhosphorusStress { get; set; }

        /// <summary>Final Leaf Number.</summary>
        public double FinalLeafNo => culms?.FinalLeafNo ?? 0;

        /// <summary>Leaf number.</summary>
        public double LeafNo => culms?.LeafNo ?? 0;

        /// <summary> /// Sowing Density (Population). /// </summary>
        public double SowingDensity { get; set; }

        /// <summary>The live biomass state at start of the computation round</summary>
        public Biomass StartLive { get; private set; } = null;

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
        //Also a DMPotentialAllocation present in this file
        //used as DMPotentialAllocation in genericorgan

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround => true;

        /// <summary>The live biomass</summary>
        [JsonIgnore]
        public Biomass Live { get; private set; }

        /// <summary>The dead biomass</summary>
        [JsonIgnore]
        public Biomass Dead { get; private set; }

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
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [JsonIgnore]
        public double NFixationCost => 0; //called from arbitrator

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation => potentialDMAllocation; 

        /// <summary>Gets the maximum N concentration.</summary>
        [JsonIgnore]
        public double MaxNconc => 0.0;

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        public double MinNconc => 0.0;

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        public double CritNconc => 0.0;

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        public double Wt => Live.Wt + Dead.Wt;

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        public double N => Live.N + Dead.N;

        /// <summary>Gets the total biomass</summary>
        [JsonIgnore]
        public Biomass Total => Live + Dead;

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [JsonIgnore]
        public double Nconc => MathUtilities.Divide(N, Wt, 0.0);

        /// <summary>Gets or sets the water allocation.</summary>
        [JsonIgnore]
        public double WaterAllocation { get; set; }

        /// <summary>Only water stress at this stage.</summary>
        /// Diff between potentialLAI and stressedLAI
        public double LossFromExpansionStress { get; set; }

        /// <summary>Total LAI as a result of senescence.</summary>
        public double SenescedLai { get; set; }

        /// <summary>Amount of N retranslocated today.</summary>
        public double DltRetranslocatedN { get; set; }
        
        /// <summary>Delta of N removed due to Senescence.</summary>
        public double DltSenescedN { get; set; }
        
        /// <summary>Delta of LAI removed due to N Senescence.</summary>
        public double DltSenescedLaiN { get; set; }
        
        /// <summary>Delta of LAI removed due to Senescence.</summary>
        public double DltSenescedLai { get; set; }
        
        /// <summary>Delta of LAI removed due to Light Senescence.</summary>
        public double DltSenescedLaiLight { get; set; }
        
        /// <summary>Delta of LAI removed due to Water Senescence.</summary>
        public double DltSenescedLaiWater { get; set; }
        
        /// <summary>Delta of LAI removed due to Frost Senescence.</summary>
        public double DltSenescedLaiFrost { get; set; }
        
        /// <summary>Delta of LAI removed due to age senescence.</summary>
        public double DltSenescedLaiAge { get; set; }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
            DMSupply.Clear();
            NSupply.Clear();
            DMDemand.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
            Height = 0;

            leafInitialised = false;

            LAI = 0;
            SLN = 0;
            SLN0 = 0;
            Live.StructuralN = 0;
            Live.StorageN = 0;

            DltSenescedLaiN = 0.0;
            SenescedLai = 0.0;
            CoverGreen = 0.0;
            CoverDead = 0.0;
            LAIDead = 0.0;
            LossFromExpansionStress = 0.0;
            culms.Initialize();
            NitrogenPhotoStress = 0;
            NitrogenPhenoStress = 0;

            MicroClimatePresent = false;
            potentialEP = 0;
            LightProfile = null;

            WaterDemand = 0;
            WaterAllocation = 0;

            SowingDensity = 0;
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Calculates the water demand.</summary>
        public double CalculateWaterDemand()
        {
            if(UseMicroClimate) return WaterDemand;

            if (waterDemandFunction != null)
                return waterDemandFunction.Value();

            return WaterDemand;
        }

        /// <summary>Update area.</summary>
        public void UpdateArea()
        {
            if (plant.IsEmerged)
            {
                if(leafInitialised)
                {
                    //areaActual in old model
                    // culms.AreaActual() will update this.DltLAI
                    DltLAI = culms.CalculateActualArea();
                    SenesceArea();
                }
            }
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }

        /// <summary>Sets the dry matter allocation.</summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // Check retranslocation
            if (MathUtilities.IsGreaterThan(dryMatter.Retranslocation, StartLive.StructuralWt))
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural, DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            Live.StructuralWt -= dryMatter.Retranslocation;
            Allocated.StructuralWt -= dryMatter.Retranslocation;

        }

        /// <summary>Sets the n allocation.</summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            SLN0 = MathUtilities.Divide(Live.N, LAI, 0);

            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            // Retranslocation
            ////TODO check what this is guarding - not sure on the relationship between NSupply and nitrogen
            //if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, StartLive.StorageN + StartLive.MetabolicN - NSupply.Retranslocation))
            //    throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            //sorghum can utilise structural as well
            //if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, StartLive.StorageN + StartLive.MetabolicN))
            //    throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            if (nitrogen.Retranslocation > Live.StorageN + Live.MetabolicN)
            {
                var strucuralNLost = nitrogen.Retranslocation - (Live.StorageN + Live.MetabolicN);
                Live.StructuralN -= strucuralNLost;
                Allocated.StructuralN -= strucuralNLost;

                Live.StorageN = 0.0;
                Live.MetabolicN = 0.0;
                Allocated.StorageN = 0;
                Allocated.MetabolicN = 0.0;
            }
            else if (nitrogen.Retranslocation > Live.StorageN)
            {
                var metabolicNLost = nitrogen.Retranslocation - Live.StorageN;
                Live.MetabolicN -= metabolicNLost;
                Allocated.MetabolicN -= metabolicNLost;
                Live.StorageN = 0.0;
                Allocated.StorageN = 0;
            }
            else
            {
                Live.StorageN -= nitrogen.Retranslocation;
                Allocated.StorageN -= nitrogen.Retranslocation;
            }
        }

        /// <summary>
        /// Adjustment function for calculating leaf demand.
        /// This should always be equal to -1 * structural N Demand.
        /// </summary>
        public double CalculateClassicDemandDelta()
        {
            if (MathUtilities.IsNegative(Live.N))
                throw new Exception($"Negative N in sorghum leaf '{Name}'");
            //n demand as calculated in apsim classic is different ot implementation of structural and metabolic
            // Same as metabolic demand in new apsim.
            var classicLeafDemand = Math.Max(0.0, CalcLAI() * targetSLN.Value() - Live.N);
            //need to remove pmf nDemand calcs from totalDemand to then add in what it should be from classic
            var pmfLeafDemand = nDemands.Structural.Value() + nDemands.Metabolic.Value();

            var structural = nDemands.Structural.Value();
            var diff = classicLeafDemand - pmfLeafDemand;

            return classicLeafDemand - pmfLeafDemand;
        }

        /// <summary>Calculate the amount of N to retranslocate</summary>
        public double ProvideNRetranslocation(BiomassArbitrationType BAT, double requiredN, bool forLeaf)
        {
            int leafIndex = 2;
            double laiToday = CalcLAI();
            //whether the retranslocation is added or removed is confusing
            //Leaf::CalcSLN uses - dltNRetranslocate - but dltNRetranslocate is -ve
            double dltNGreen = BAT.StructuralAllocation[leafIndex] + BAT.MetabolicAllocation[leafIndex];
            double nGreenToday = Live.N + dltNGreen + DltRetranslocatedN; //dltRetranslocation is -ve
            //double nGreenToday = Live.N + BAT.TotalAllocation[leafIndex] + BAT.Retranslocation[leafIndex];
            double slnToday = calcSLN(laiToday, nGreenToday);
            
            var thermalTime = dltTT.Value();
            var dilutionN = thermalTime * (NDilutionSlope * slnToday + NDilutionIntercept) * laiToday;
            dilutionN = Math.Max(dilutionN, 0);
            if (phenology.Between("Germination", "Flowering"))
            {
                // pre anthesis, get N from dilution, decreasing dltLai and senescence
                double nProvided = Math.Min(dilutionN, requiredN / 3.0);
                DltRetranslocatedN -= nProvided;
                nGreenToday -= nProvided; //jkb
                requiredN -= nProvided;
                if (requiredN <= 0.0001)
                    return nProvided;

                // decrease dltLai which will reduce the amount of new leaf that is produced
                if (MathUtilities.IsPositive(DltLAI))
                {
                    // Only half of the requiredN can be accounted for by reducing DltLAI
                    // If the RequiredN is large enough, it will result in 0 new growth
                    // Stem and Rachis can technically get to this point, but it doesn't occur in all of the validation data sets
                    double n = DltLAI * NewLeafSLN;
                    double laiN = Math.Min(n, requiredN);
                    // dh - we don't make this check in old apsim
                    if (MathUtilities.IsPositive(laiN))
                    {
                        DltLAI = (n - laiN) / NewLeafSLN;
                        if (forLeaf)
                        {
                            // should we update the StructuralDemand?
                            //BAT.StructuralDemand[leafIndex] = nDemands.Structural.Value();
                            requiredN -= laiN;
                        }
                    }
                }

                // recalc the SLN after this N has been removed
                laiToday = CalcLAI();
                slnToday = calcSLN(laiToday, nGreenToday);

                var maxN = thermalTime * (NDilutionSlope * slnToday + NDilutionIntercept) * laiToday;
                maxN = Math.Max(maxN, 0);
                requiredN = Math.Min(requiredN, maxN);

                double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - SenescedLeafSLN), 0.0), 0.0);

                // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                senescenceLAI = Math.Min(senescenceLAI, Live.N * SenescedLeafSLN - DltSenescedN);

                double newN = Math.Max(senescenceLAI * (slnToday - SenescedLeafSLN), 0.0);
                DltRetranslocatedN -= newN;
                nGreenToday += newN; // local variable
                nProvided += newN;
                DltSenescedLaiN += senescenceLAI;

                DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                DltSenescedN += senescenceLAI * SenescedLeafSLN;

                return nProvided;
            }
            else
            {
                // if sln > 1, dilution then senescence
                if (slnToday > 1.0)
                {
                    double nProvided = Math.Min(dilutionN, requiredN);
                    requiredN -= nProvided;
                    nGreenToday -= nProvided; //jkb
                    DltRetranslocatedN -= nProvided;

                    if (requiredN <= 0.0001)
                        return nProvided;

                    // rest from senescence
                    laiToday = CalcLAI();
                    slnToday = calcSLN(laiToday, nGreenToday);

                    var maxN = thermalTime * (NDilutionSlope * slnToday + NDilutionIntercept) * laiToday;
                    requiredN = Math.Min(requiredN, maxN);

                    double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - SenescedLeafSLN), 0.0), 0.0);

                    // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                    senescenceLAI = Math.Min(senescenceLAI, Live.N * SenescedLeafSLN - DltSenescedN);

                    double newN = Math.Max(senescenceLAI * (slnToday - SenescedLeafSLN), 0.0);
                    DltRetranslocatedN -= newN;
                    nGreenToday += newN;
                    nProvided += newN;
                    DltSenescedLaiN += senescenceLAI;

                    DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                    DltSenescedN += senescenceLAI * SenescedLeafSLN;
                    return nProvided;
                }
                else
                {
                    // half from dilution and half from senescence
                    double nProvided = Math.Min(dilutionN, requiredN / 2.0);
                    requiredN -= nProvided;
                    nGreenToday -= nProvided; //jkb // dh - this should be subtracted, not added
                    DltRetranslocatedN -= nProvided;

                    // rest from senescence
                    laiToday = CalcLAI();
                    slnToday = calcSLN(laiToday, nGreenToday);

                    var maxN = thermalTime * (NDilutionSlope * slnToday + NDilutionIntercept) * laiToday;
                    requiredN = Math.Min(requiredN, maxN);

                    double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - SenescedLeafSLN), 0.0), 0.0);

                    // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                    senescenceLAI = Math.Min(senescenceLAI, Live.N * SenescedLeafSLN - DltSenescedN);

                    double newN = Math.Max(senescenceLAI * (slnToday - SenescedLeafSLN), 0.0);
                    DltRetranslocatedN -= newN;
                    nGreenToday += newN;
                    nProvided += newN;
                    DltSenescedLaiN += senescenceLAI;

                    DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                    DltSenescedN += senescenceLAI * SenescedLeafSLN;
                    return nProvided;
                }
            }
        }

        /// <summary>Senesce the Leaf Area.</summary>
        private void SenesceArea()
        {
            DltSenescedLai = 0.0;
            DltSenescedLaiN = 0.0;

            DltSenescedLaiAge = 0;
            //sLai - is the running total of dltSLai
            //could be a stage issue here. should only be between fi and flag
            LossFromExpansionStress += (DltPotentialLAI - DltStressedLAI);
            var maxLaiPossible = LAI + SenescedLai - LossFromExpansionStress;

            var sen = new List<double> { DltSenescedLai };

            if (phenology.Between("Emergence", "HarvestRipe"))
                DltSenescedLaiAge = AgeSenescence.Value();
            sen.Add(DltSenescedLaiAge);

            DltSenescedLaiLight = LightSenescence.Value();
            sen.Add(DltSenescedLaiLight);

            DltSenescedLaiWater = WaterSenescence.Value();
            sen.Add(DltSenescedLaiWater);

            DltSenescedLaiFrost = FrostSenescence.Value();
            sen.Add(DltSenescedLaiFrost);

            DltSenescedLai = Math.Min(sen.Max(), LAI);
        }

        private void ApplySenescence()
        {
            if (!MathUtilities.IsPositive(Live.Wt)) return;

            // Derives seneseced plant dry matter (g/m^2) for the day
            //Should not include any retranloocated biomass
            // dh - old apsim does not take into account DltSenescedLai for this laiToday calc
            double laiToday = LAI + DltLAI/* - DltSenescedLai*/; // how much LAI we will end up with at end of day
            double slaToday = MathUtilities.Divide(laiToday, Live.Wt, 0.0); // m2/g?

            // This is equivalent to dividing by slaToday
            double dltSenescedBiomass = Live.Wt * MathUtilities.Divide(DltSenescedLai, laiToday, 0);
            if (MathUtilities.IsGreaterThan(dltSenescedBiomass, Live.Wt))
                throw new Exception($"Attempted to senesce more biomass than exists on leaf '{Name}'");

            if (!MathUtilities.IsPositive(dltSenescedBiomass)) return;

            double slnToday = MathUtilities.Divide(Live.N, laiToday, 0.0);
            DltSenescedN += DltSenescedLai * Math.Max(slnToday, 0.0);

            if (MathUtilities.IsGreaterThan(DltSenescedN, Live.N))
                throw new Exception($"Attempted to senesce more N than exists on leaf '{Name}'");

            double dmSenescingProportion = dltSenescedBiomass / Live.Wt;
            double nSenescingProportion = DltSenescedN / Live.N;

            //order is important as the proortion is calculated for each component of the live weight
            UpdateBiomassComponent(Dead, Live, dmSenescingProportion);
            UpdateBiomassComponent(Senesced, Live, dmSenescingProportion);
            //the proportion needs to be removed from liveweight - so pass the -ve
            UpdateBiomassComponent(Live, Live, dmSenescingProportion * -1);

            //order is important as the proortion is calculated for each component of the live weight
            UpdateNComponent(Dead, Live, nSenescingProportion);
            UpdateNComponent(Senesced, Live, nSenescingProportion);
            //the proportion needs to be removed from liveweight - so pass the -ve
            UpdateNComponent(Live, Live, nSenescingProportion * -1);
        }

        private void UpdateNComponent(Biomass nComponent, Biomass proportionComponent, double senescingProportion)
        {
            nComponent.StructuralN += proportionComponent.StructuralN * senescingProportion;
            nComponent.MetabolicN += proportionComponent.MetabolicN * senescingProportion;
            nComponent.StorageN += proportionComponent.StorageN * senescingProportion;
        }

        private void UpdateBiomassComponent(Biomass dmComponent, Biomass proportionComponent, double senescingProportion)
        {
            dmComponent.StructuralWt += proportionComponent.StructuralWt * senescingProportion;
            dmComponent.MetabolicWt += proportionComponent.MetabolicWt * senescingProportion;
            dmComponent.StorageWt += proportionComponent.StorageWt * senescingProportion;
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        private double AvailableDMRetranslocation()
        {
            var leafWt = StartLive.Wt + potentialDMAllocation.Total;
            var leafWtAvail = leafWt - minPlantWt.Value() * SowingDensity;

            double availableDM = Math.Max(0.0,  leafWtAvail);

            // Don't retranslocate more DM than we have available.
            availableDM = Math.Min(availableDM, StartLive.Wt);
            return availableDM;
        }

        /// <summary>
        /// calculates todays LAI values - can change during retranslocation calculations
        /// </summary>
        /// this should be private - called from CalcTillerAppearanceDynamic in leafculms which needs to be refactored
        public double CalcLAI()
        {
            return Math.Max(0.0, LAI + DltLAI - DltSenescedLai);
        }
        private double calcSLN(double laiToday, double nGreenToday)
        {
            return MathUtilities.Divide(nGreenToday, laiToday, 0.0);
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            NDemand = new BiomassPoolType();
            DMDemand = new BiomassPoolType();
            NSupply = new BiomassSupplyType();
            DMSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            StartLive = new Biomass();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
            Live = new Biomass();
            Dead = new Biomass();

            Clear();
        }

        [EventSubscribe("StartOfDay")]
        private void ResetDailyVariables(object sender, EventArgs e)
        {
            BiomassRUE = 0;
            BiomassTE = 0;
            DltLAI = 0;
            DltSenescedLai = 0;
            DltSenescedLaiAge = 0;
            DltSenescedLaiFrost = 0;
            DltSenescedLaiLight = 0;
            DltSenescedLaiN = 0;
            DltSenescedLaiWater = 0;
            DltSenescedN = 0;
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (plant.IsAlive)
            {
                Allocated.Clear();
                Senesced.Clear();
                Detached.Clear();
                Removed.Clear();

                //clear local variables
                // dh - DltLAI cannot be cleared here. It needs to retain its value from yesterday,
                // for when leaf retranslocates to itself in provideN().
                DltPotentialLAI = 0.0;
                DltRetranslocatedN = 0.0;
                DltSenescedLai = 0.0;
                DltSenescedLaiN = 0.0;
                DltSenescedN = 0.0;
                DltStressedLAI = 0.0;
            }
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == LeafInitialisationStage)
            {
                leafInitialised = true;
                culms.TilleringMethod = TilleringMethod;

                Live.StructuralWt = InitialDMWeight * SowingDensity;
                Live.StorageWt = 0.0;
                LAI = InitialLAI * SowingDensity.ConvertSqM2SqMM();
                SLN = InitialSLN;

                Live.StructuralN = LAI * SLN;
                Live.StorageN = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, EventArgs e)
        {
            var sowingData = e as SowingParameters;
            
            if (sowingData.SkipRow < 0 || sowingData.SkipRow > 2)
                throw new ApsimXException(this, $"Invalid SkipRow Configuration for '{plant.Name}'");

            //overriding SkipDensityScale as it was calculated differently for sorghum in Classic
            var outerSkips = sowingData.SkipRow > 0 ? 2 : 0;
            var nonSkipCover = Math.Min(sowingData.RowSpacing, CanopyWidth) * 2.0;
            
            //outerSkipCovered is > 0 only if canopy width is wider than rowSpacing
            var outerSkipCovered = Math.Max(0, (CanopyWidth - sowingData.RowSpacing) / 2) * outerSkips;
            
            var totalWidth = sowingData.RowSpacing * 2 + sowingData.RowSpacing * sowingData.SkipRow;
            var totalCover = nonSkipCover + outerSkipCovered;

            sowingData.SkipDensityScale = MathUtilities.Divide(totalWidth, totalCover, 1.0);

        }

        [EventSubscribe("PrePhenology")]
        private void OnPrePhenology(object sender, EventArgs args)
        {
            culms.FinalLeafNo = numberOfLeaves.Value();
            culms.CalculatePotentialArea();
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
                StartLive = ReflectionUtilities.Clone(Live) as Biomass;
            if (leafInitialised)
            {
                DltPotentialLAI = culms.dltPotentialLAI;
                DltStressedLAI = culms.dltStressedLAI;

                //old model calculated BiomRUE at the end of the day
                //this is done at staet of the day
                BiomassRUE = photosynthesis.Value();
                //var bimT = 0.009 / waterFunction.VPD / 0.001 * Arbitrator.WSupply;
                BiomassTE = potentialBiomassTEFunction.Value();

                Height = heightFunction.Value();
                LAIDead = SenescedLai;
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            // if (!parentPlant.IsAlive) return; wtf
            if (!plant.IsAlive) return;
            if (!leafInitialised) return;
            ApplySenescence();

            //UpdateVars
            SenescedLai += DltSenescedLai;
            nDeadLeaves += dltDeadLeaves;
            dltDeadLeaves = 0;

            LAI += DltLAI - DltSenescedLai;

            int flag = 6; //= phenology.StartStagePhaseIndex("FlagLeaf");
            if (phenology.Stage >= flag)
            {
                if (LAI - DltSenescedLai < 0.1)
                {
                    string message = "Crop failed due to loss of leaf area \r\n";
                    summary.WriteMessage(this, message, MessageType.Diagnostic);
                    //scienceAPI.write(" ********** Crop failed due to loss of leaf area ********");
                    plant.EndCrop();
                    return;
                }
            }
            LAIDead = SenescedLai;
            SLN = MathUtilities.Divide(Live.N, LAI, 0);

            CoverGreen = MathUtilities.Bound(MathUtilities.Divide(1.0 - Math.Exp(-extinctionCoefficientFunction.Value() * LAI * plant.SowingData.SkipDensityScale), plant.SowingData.SkipDensityScale, 0.0), 0.0, 0.999999999);// limiting to within 10^-9, so MicroClimate doesn't complain
            CoverDead = MathUtilities.Bound(1.0 - Math.Exp(-KDead * LAIDead), 0.0, 0.999999999);

            NitrogenPhotoStress = nPhotoStressFunction.Value();

            NitrogenPhenoStress = 1.0;
            if (phenology.Between("Emergence", "FlagLeaf"))
            {
                var phenoStress = (0.5 + 0.5 / 0.3 * (SLN - 0.7));
                NitrogenPhenoStress = MathUtilities.Bound(phenoStress, 0.5, 1.0);
            }
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        private void setDMSupply(object sender, EventArgs e)
        {
            //Reallocation usually comes form Storage - which sorghum doesn't utilise
            DMSupply.Reallocation = 0.0; //availableDMReallocation();
            DMSupply.Retranslocation = AvailableDMRetranslocation();
            DMSupply.Uptake = 0;
            DMSupply.Fixation = dMSupplyFixation.Value();
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        private void SetNSupply(object sender, EventArgs e)
        {
            UpdateArea(); //must be calculated before potential N partitioning
            
            var availableLaiN = DltLAI * NewLeafSLN;

            double laiToday = CalcLAI();
            double nGreenToday = Live.N;
            double slnToday = MathUtilities.Divide(nGreenToday, laiToday, 0.0);

            var dilutionN = dltTT.Value() * ( NDilutionSlope * slnToday + NDilutionIntercept) * laiToday;

            NSupply.Retranslocation = Math.Max(0, Math.Min(StartLive.N, availableLaiN + dilutionN));

            //NSupply.Retranslocation = Math.Max(0, (StartLive.StorageN + StartLive.MetabolicN) * (1 - SenescenceRate.Value()) * NRetranslocationFactor.Value());
            if (NSupply.Retranslocation < -biomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        private void SetDMDemand(object sender, EventArgs e)
        {
            DMDemand.Structural = dmDemands.Structural.Value(); // / dmConversionEfficiency.Value() + remobilisationCost.Value();
            DMDemand.Metabolic = Math.Max(0, dmDemands.Metabolic.Value());
            DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value()); // / dmConversionEfficiency.Value());
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        private void SetNDemand(object sender, EventArgs e)
        {
            //happening in potentialPlantPartitioning
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = nDemands.Metabolic.Value();
            NDemand.Storage = nDemands.Storage.Value();
        }

        /// <summary>Called when crop is being sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == plant)
            {
                //OnPlantSowing let structure do the clear so culms isn't cleared before initialising the first one
                //Clear();
                SowingDensity = data.Population;
                nDeadLeaves = 0;
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void DoPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, plant.PlantType, Name);
            }

            Clear();
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            // Add a heading and description.
            foreach (ITag tag in base.Document())
                yield return tag;

            // List the parameters, properties, and processes from this organ that need to be documented:

            // Document initial DM weight.
            yield return new Paragraph($"Initial DM mass = {InitialDMWeight} gm^-2^");

            // Document DM demands.
            List<ITag> dmDemandsTags = new List<ITag>();
            dmDemandsTags.Add(new Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool."));
            dmDemandsTags.AddRange(dmDemands.Document());
            yield return new Section("Dry Matter Demand", dmDemandsTags);

            // Document N demands.
            List<ITag> nDemandTags = new List<ITag>();
            nDemandTags.Add(new Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool."));
            nDemandTags.AddRange(nDemands.Document());
            yield return new Section("Nitrogen Demand", nDemandTags);

            // Document N concentration thresholds.
            yield return new Paragraph($"Minimum N Concentration = {MinNconc}");
            yield return new Paragraph($"Critical N Concentraion = {CritNconc}");
            yield return new Paragraph($"Maximum N Concentration = {MaxNconc}");

            // Document DM supplies.
            yield return new Section("Dry Matter Supply", new Paragraph($"{Name} does not reallocate DM when senescence of the organ occurs."));

            // Document DM retranslocation.
            yield return new Section("DM Retranslocation Factor", new Paragraph($"{Name} does not retranslocate non-structural DM."));

            // Document photosynthesis.
            yield return new Section("Photosynthesis", photosynthesis.Document());

            // Document N supplies.
            yield return new Section("Nitrogen Supply", new Paragraph($"{Name} does not reallocate N when senescence of the organ occurs."));

            // Document N retranslocation.
            yield return new Section("Nitrogen Retranslocation Factor", new Paragraph($"{Name} does not retranslocate non-structural N."));

            // todo: document LAI(/CoverTot?).
            List<ITag> canopyTags = new List<ITag>();
            canopyTags.AddRange(extinctionCoefficientFunction.Document());
            canopyTags.AddRange(heightFunction.Document());
            yield return new Section("Canopy Properties", canopyTags);

            // Document senescence and detachment.
            List<ITag> senescenceTags = new List<ITag>();
            senescenceTags.Add(new Paragraph($"{Name} has senescence parameterised to zero so all biomass in this organ will remain alive."));
            senescenceTags.Add(new Paragraph($"{Name} has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs."));
            senescenceTags.AddRange(biomassRemovalModel.Document());

            yield return new Section("Senescence and Detachment", senescenceTags);
        }
    }
}
