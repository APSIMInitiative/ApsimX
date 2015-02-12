// -----------------------------------------------------------------------
// <copyright file="Plant.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Models.Core;
    using Models.PMF.Organs;
    using Models.PMF.Phen;
    using Models.Soils;
    using Models.PMF.Interfaces;
    using Models.Soils.Arbitrator;

    /// <summary>
    /// The generic plant model
    /// </summary>
    [Serializable]
    public class Plant : ModelCollectionFromResource, ICrop
    {
        #region Class links
        /// <summary>The summary</summary>
        [Link] ISummary Summary = null;
        /// <summary>The phenology</summary>
        [Link(IsOptional = true)] public Phenology Phenology = null;
        /// <summary>The arbitrator</summary>
        [Link(IsOptional = true)] public OrganArbitrator Arbitrator = null;
        /// <summary>The structure</summary>
        [Link(IsOptional=true)] public Structure Structure = null;
        /// <summary>The soil</summary>
        [Link] Soils.Soil Soil = null;
        /// <summary>The leaf</summary>
        [Link(IsOptional=true)] public Leaf Leaf = null;
        /// <summary>The root</summary>
        [Link(IsOptional=true)] public Root Root = null;
        /// <summary>The base function class</summary>
        
        #endregion

        #region Class properties and fields
        /// <summary>
        /// MicroClimate will get 'CropType' and use it to look up
        /// canopy properties for this crop.
        /// </summary>
        public string CropType { get; set; }

        /// <summary>The sowing data</summary>
        [XmlIgnore]
        public SowPlant2Type SowingData;
        
        /// <summary>Gets the organs.</summary>
        [XmlIgnore]
        public IOrgan[] Organs { get; private set; }
        /// <summary>The local root data</summary>
        private RootProperties LocalRootData;
        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames
        {
            get
            {
                SortedSet<string> cultivarNames = new SortedSet<string>();
                foreach (Cultivar cultivar in this.Cultivars)
                {
                    cultivarNames.Add(cultivar.Name);
                    if (cultivar.Aliases != null)
                    {
                        foreach (string alias in cultivar.Aliases)
                            cultivarNames.Add(alias);
                    }
                }

                return new List<string>(cultivarNames).ToArray();
            }
        }
        /// <summary>A property to return all cultivar definitions.</summary>
        /// <value>The cultivars.</value>
        private List<Cultivar> Cultivars
        {
            get
            {
                List<Cultivar> cultivars = new List<Cultivar>();
                foreach (Model model in Apsim.Children(this, typeof(Cultivar)))
                {
                    cultivars.Add(model as Cultivar);
                }

                return cultivars;
            }
        }
        /// <summary>The current cultivar definition.</summary>
        private Cultivar cultivarDefinition;


        /// <summary>MicroClimate needs FRGR.</summary>
        /// <value>The FRGR.</value>
        public double FRGR
        {
            get
            {
                double frgr = 1;
                //foreach (IOrgan Child in Organs)
                //{
                //    if (Child.FRGR <= 1)
                //        frgr = Child.FRGR;
                //}
                return frgr;
            }
        }
        /// <summary>MicroClimate supplies light profile.</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        /// <summary>MicroClimate supplies Potential EP</summary>
        /// <value>The potential ep.</value>
        [XmlIgnore]
        public double PotentialEP
        {
            get
            {
                return double.MaxValue;
            }

            set
            {

            }
        }

        /// <summary>Gets the water supply demand ratio.</summary>
        /// <value>The water supply demand ratio.</value>
        [XmlIgnore]
        public double WaterSupplyDemandRatio
        {
            get
            {
                double F;
                if (demandWater > 0)
                    F = Utility.Math.Sum(uptakeWater) / demandWater;
                else
                    F = 1;
                return F;
            }
        }
        /// <summary>Gets or sets the population.</summary>
        /// <value>The population.</value>
        [XmlIgnore]
        [Description("Number of plants per meter2")]
        [Units("/m2")]
        public double Population { get; set; }
        /// <summary>Gets or sets the plant transpiration.</summary>
        /// <value>The plant transpiration.</value>
        [XmlIgnore]
        public double PlantTranspiration { get; set; }
        #endregion

        #region Interface properties
        /// <summary>Provides root data to Arbitrator.</summary>
        public RootProperties RootProperties { get { return LocalRootData; } }
        /// <summary>
        /// Potential evapotranspiration. Arbitrator calculates this and sets this property in the crop.
        /// </summary>
        [XmlIgnore]
        public double demandWater { get; set; }
        /// <summary>
        /// Actual transpiration by the crop. Calculated by Arbitrator based on PotentialEP across all crops, soil and root properties
        /// </summary>
        [XmlIgnore]
        public double[] uptakeWater { get; set; }
        /// <summary>Crop calculates potentialNitrogenDemand after getting its water allocation</summary>
        [XmlIgnore]
        public double demandNitrogen { get; set; }
        /// <summary>
        /// Arbitrator supplies actualNitrogenSupply based on soil supply and other crop demand
        /// </summary>
        [XmlIgnore]
        public double[] uptakeNitrogen { get; set; }
        /// <summary>The proportion of supplyNitrogen that is supplied as NO3, the remainder is NH4</summary>
        [XmlIgnore]
        public double[] uptakeNitrogenPropNO3 { get;  set; }
        /// <summary>
        /// The initial value of the extent to which the roots have penetrated the soil layer (0-1)
        /// </summary>
        /// <value>The local root exploration by layer.</value>
        [XmlIgnore] public double[] localRootExplorationByLayer { get; set; }
        /// <summary>The initial value of the root length densities for each soil layer (mm/mm3)</summary>
        /// <value>The local root length density by volume.</value>
        [XmlIgnore] public double[] localRootLengthDensityByVolume { get; set; }
        /// <summary>Is the plant in the ground?</summary>
        [XmlIgnore]
        public bool PlantInGround
        {
            get
            {
                return SowingData != null;
            }
        }
        /// <summary>Test if the plant has emerged</summary>
        [XmlIgnore]
        public bool PlantEmerged
        {
            get
            {
                if (Phenology != null)
                    return Phenology.Emerged;
                else
                    return true;
            }
        }


        /// <summary>
        /// Is the plant alive?
        /// </summary>
        public bool IsAlive
        { 
            get
            {
                return SowingData != null;
            }
        }
        #endregion

        #region Class Events
        /// <summary>Occurs when [sowing].</summary>
        public event EventHandler Sowing;
        /// <summary>Occurs when [harvesting].</summary>
        public event EventHandler Harvesting;
        /// <summary>Occurs when [cutting].</summary>
        public event EventHandler Cutting;
        /// <summary>Occurs when [plant ending].</summary>
        public event EventHandler PlantEnding;
        /// <summary>Occurs when [biomass removed].</summary>
        public event BiomassRemovedDelegate BiomassRemoved;
        /// <summary>Occurs when daily phenology timestep completed</summary>
        public event EventHandler PostPhenology;
        #endregion

        #region External Communications.  Method calls and EventHandlers
        /// <summary>Sow the crop with the specified parameters.</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
        {
            SowingData = new SowPlant2Type();
            SowingData.Population = population;
            SowingData.Depth = depth;
            SowingData.Cultivar = cultivar;
            SowingData.MaxCover = maxCover;
            SowingData.BudNumber = budNumber;
            SowingData.RowSpacing = rowSpacing;

            // Find cultivar and apply cultivar overrides.
            cultivarDefinition = PMF.Cultivar.Find(Cultivars, SowingData.Cultivar);
            cultivarDefinition.Apply(this);



            // Invoke a sowing event.
            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            this.Population = population;

            // tell all our children about sow
            foreach (IOrgan Child in Organs)
                Child.OnSow(SowingData);
            if (Structure != null)
               Structure.OnSow(SowingData);
            if (Phenology != null)
                Phenology.OnSow();
            if (Arbitrator != null)
                Arbitrator.OnSow();

            
       
            Summary.WriteMessage(this, string.Format("A crop of " + CropType +" (cultivar = " + cultivar + ") was sown today at a population of " + Population + " plants/m2 with " + budNumber + " buds per plant at a row spacing of " + rowSpacing + " and a depth of " + depth + " mm"));
        }
        /// <summary>Harvest the crop.</summary>
        public void Harvest()
        {
            // Invoke a harvesting event.
            if (Harvesting != null)
                Harvesting.Invoke(this, new EventArgs());

            // tell all our children about harvest
            foreach (IOrgan Child in Organs)
                Child.OnHarvest();

            Phenology.OnHarvest();

            Summary.WriteMessage(this, string.Format("A crop of " + CropType + " was harvested today, Yeahhh"));
        }
        /// <summary>End the crop.</summary>
        public void EndCrop()
        {
            Summary.WriteMessage(this, "Crop ending");

            if (PlantEnding != null)
                PlantEnding.Invoke(this, new EventArgs());

            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            BiomassRemovedData.crop_type = CropType;
            BiomassRemovedData.dm_type = new string[Organs.Length];
            BiomassRemovedData.dlt_crop_dm = new float[Organs.Length];
            BiomassRemovedData.dlt_dm_n = new float[Organs.Length];
            BiomassRemovedData.dlt_dm_p = new float[Organs.Length];
            BiomassRemovedData.fraction_to_residue = new float[Organs.Length];
            int i = 0;
            foreach (BaseOrgan O in Organs)
            {
                if (O is AboveGround)
                {
                    BiomassRemovedData.dm_type[i] = O.Name;
                    BiomassRemovedData.dlt_crop_dm[i] = (float)O.TotalDM * 10f;
                    BiomassRemovedData.dlt_dm_n[i] = (float)O.TotalN * 10f;
                    BiomassRemovedData.dlt_dm_p[i] = 0f;
                    BiomassRemovedData.fraction_to_residue[i] = 1f;
                }
                else
                {
                    BiomassRemovedData.dm_type[i] = O.Name;
                    BiomassRemovedData.dlt_crop_dm[i] = 0f;
                    BiomassRemovedData.dlt_dm_n[i] = 0f;
                    BiomassRemovedData.dlt_dm_p[i] = 0f;
                    BiomassRemovedData.fraction_to_residue[i] = 0f;
                }
                i++;
            }
            BiomassRemoved.Invoke(BiomassRemovedData);

            // tell all our children about endcrop
            foreach (IOrgan Child in Organs)
                Child.OnEndCrop();
            Clear();

            cultivarDefinition.Unapply();
        }
        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            SowingData = null;
            //WaterSupplyDemandRatio = 0;
            Population = 0;
            if (Structure != null)
               Structure.Clear();
            if (Phenology != null)
               Phenology.Clear();
            if (Arbitrator != null)
               Arbitrator.Clear();
        }
        /// <summary>Cut the crop.</summary>
        public void Cut()
        {
            if (Cutting != null)
                Cutting.Invoke(this, new EventArgs());

            // tell all our children about endcrop
            foreach (IOrgan Child in Organs)
                Child.OnCut();
        }
        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<IOrgan> organs = new List<IOrgan>();
            foreach (IOrgan organ in Apsim.Children(this, typeof(IOrgan)))
                organs.Add(organ);
            Organs = organs.ToArray();

            Clear();
            foreach (IOrgan o in Organs)
            {
                o.OnSimulationCommencing();
            }
            InitialiseInterfaceTypes();
        }
        
        /// <summary>Things that happen when the clock broadcasts DoPlantGrowth Event</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (PlantInGround)
            {
                
                if (Phenology != null)
                {
                    DoPhenology();
                    if (Phenology.Emerged == true)
                    {
                       
                        // Invoke a post phenology event.
                        if (PostPhenology != null)
                            PostPhenology.Invoke(this, new EventArgs());
                        DoDMSetUp();//Sets organs water limited DM supplys and demands
                        if (Arbitrator != null)
                        {
                            Arbitrator.DoWaterLimitedDMAllocations();
                            Arbitrator.DoNutrientDemandSetUp();
                            Arbitrator.SetNutrientUptake();
                        }
                    }
                }
                else
                {
                    DoDMSetUp();//Sets organs water limited DM supplys and demands
                    if (Arbitrator != null)
                    {
                        Arbitrator.DoWaterLimitedDMAllocations();
                        Arbitrator.DoNutrientDemandSetUp();
                        Arbitrator.SetNutrientUptake();
                    }
                }
            }
        }

        /// <summary>Called when [do actual plant growth].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (PlantInGround)
            {
                if (Phenology != null)
                {
                    if (Phenology.Emerged == true)
                    {
                        if (Arbitrator != null)
                        {
                            Arbitrator.DoNutrientAllocations();
                            Arbitrator.DoNutrientLimitedGrowth();
                        }
                    }
                }
                else
                    if (Arbitrator != null)
                    {
                        Arbitrator.DoNutrientAllocations();
                        Arbitrator.DoNutrientLimitedGrowth();
                    }
                DoActualGrowth();
            }
        }

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
        {
            double Supply = 0;
            double Demand = 0;
            double[] supply = null;
            foreach (IArbitration o in Organs)
            {
                double[] organSupply = o.WaterSupply(soilstate.Zones);
                if (organSupply != null)
                {
                    supply = organSupply;
                    Supply += Utility.Math.Sum(organSupply);
                }
                Demand += o.WaterDemand;
            }

            double FractionUsed = 0;
            if (Supply > 0)
                FractionUsed = Math.Min(1.0, Demand / Supply);
            
            ZoneWaterAndN uptake = new ZoneWaterAndN();
            uptake.Name = soilstate.Zones[0].Name;
            uptake.Water = Utility.Math.Multiply_Value(supply, FractionUsed);
            uptake.NO3N = new double[uptake.Water.Length];
            uptake.NH4N = new double[uptake.Water.Length];

            List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
            zones.Add(uptake);
            return zones;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetSWUptake(List<ZoneWaterAndN> zones)
        {
            double[] uptake = zones[0].Water;
            double Supply = Utility.Math.Sum(uptake);
            double Demand = 0;
            foreach (IArbitration o in Organs)
                Demand += o.WaterDemand;

            double fraction = 1;
            if (Demand > 0)
                fraction = Math.Min(1.0, Supply / Demand);

            foreach (IArbitration o in Organs)
                if (o.WaterDemand > 0)
                    o.WaterAllocation = fraction * o.WaterDemand;

            Root.DoWaterUptake(uptake);
        }

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<Soils.Arbitrator.ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            ZoneWaterAndN uptake = new ZoneWaterAndN();
                    
            if (Phenology != null)
                if (Phenology.Emerged == true)
                {
                    Arbitrator.DoNutrientUptake(soilstate);

                    //Pack results into uptake structure
                    uptake.NO3N = Arbitrator.NO3NSupply;
                    uptake.NH4N = Arbitrator.NH4NSupply;

                    //These two lines below must be REMOVED !!!!!!!!!!!!!
                    //Sending zeros until everything is working internally and the root N uptake is turned off
                    for (int i = 0; i < uptake.NO3N.Length; i++) { uptake.NO3N[i] = 0; }
                    for (int i = 0; i < uptake.NH4N.Length; i++) { uptake.NH4N[i] = 0; }
                }
                else //Uptakes are zero
                {
                    uptake.NO3N = new double[soilstate.Zones[0].NO3N.Length];
                    for (int i = 0; i < uptake.NO3N.Length; i++) { uptake.NO3N[i] = 0; }
                    uptake.NH4N = new double[soilstate.Zones[0].NH4N.Length];
                    for (int i = 0; i < uptake.NH4N.Length; i++) { uptake.NH4N[i] = 0; }
                }

            uptake.Name = soilstate.Zones[0].Name;
            uptake.Water = new double[uptake.NO3N.Length];

            List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
            zones.Add(uptake);
            return zones;

        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetNUptake(List<Soils.Arbitrator.ZoneWaterAndN> info)
        {
            
        }

        #endregion

        #region Internal Communications.  Method calls
        /// <summary>Does the phenology.</summary>
        private void DoPhenology()
        {
            if (Phenology != null)
            {
                Phenology.DoTimeStep();

            }
        }
        /// <summary>Does the dm set up.</summary>
        private void DoDMSetUp()
        {
            if (Structure != null)
                Structure.DoPotentialDM();
            foreach (IOrgan o in Organs)
                o.DoPotentialDM();
        }
        /// <summary>Does the nutrient set up.</summary>
        private void DoNutrientSetUp()
        {
            foreach (IOrgan o in Organs)
                o.DoPotentialNutrient();
        }

        /// <summary>Does the actual growth.</summary>
        private void DoActualGrowth()
        {
            if (Structure != null)
                Structure.DoActualGrowth();
            foreach (IOrgan o in Organs)
                o.DoActualGrowth();
        }
        /// <summary>Initialises the interface types.</summary>
        private void InitialiseInterfaceTypes()
        {
            uptakeWater = new double[Soil.Thickness.Length];
            uptakeNitrogen = new double[Soil.Thickness.Length];
            uptakeNitrogenPropNO3 = new double[Soil.Thickness.Length];

            //Set up CanopyData and root data types
            LocalRootData = new RootProperties();

            SoilCrop soilCrop = this.Soil.Crop(Name) as SoilCrop;

            RootProperties.KL = soilCrop.KL;
            RootProperties.LowerLimitDep = soilCrop.LL;
            RootProperties.RootDepth = 0;
            RootProperties.MaximumDailyNUptake = 0;
            RootProperties.KNO3 = Root.KNO3;
            RootProperties.KNH4 = Root.KNH4;

            localRootExplorationByLayer = new double[Soil.Thickness.Length];
            localRootLengthDensityByVolume = new double[Soil.Thickness.Length];

            demandWater = 0;
            demandNitrogen = 0;

            RootProperties.RootExplorationByLayer = localRootExplorationByLayer;
            RootProperties.RootLengthDensityByVolume = localRootLengthDensityByVolume;
        }

        #endregion
     }
}
