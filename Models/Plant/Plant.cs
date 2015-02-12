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
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Models.PMF.Phen;
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

        /// <summary>Gets the water supply demand ratio.</summary>
        /// <value>The water supply demand ratio.</value>
        [XmlIgnore]
        public double WaterSupplyDemandRatio
        {
            get
            {
                double F;
                
                if (Leaf != null && Leaf.WaterDemand > 0)
                    F = Root.WaterUptake / Leaf.WaterDemand;
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

        #endregion

        #region Interface properties

        /// <summary>Return true if plant is alive and in the ground.</summary>
        public bool IsAlive { get { return SowingData != null; } }
        
        /// <summary>Return true if plant has emerged</summary>
        public bool IsEmerged
        {
            get
            {
                if (Phenology != null)
                    return Phenology.Emerged;
                else
                    return true;
            }
        }

        #endregion

        #region Class Events
        /// <summary>Occurs when a plant is sown.</summary>
        public event EventHandler Sowing;
        /// <summary>Occurs when a plant is about to be harvested.</summary>
        public event EventHandler Harvesting;
        /// <summary>Occurs when a plant is about to be cut.</summary>
        public event EventHandler Cutting;
        /// <summary>Occurs when a plant is ended via EndCrop.</summary>
        public event EventHandler PlantEnding;
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
                o.OnSimulationCommencing();
        }
        
        /// <summary>Things that happen when the clock broadcasts DoPlantGrowth Event</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (IsAlive)
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
            if (IsAlive)
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
        #endregion
     }
}
