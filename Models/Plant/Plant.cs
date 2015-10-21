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
    using Models.Interfaces;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Models.PMF.Phen;
    using Models.Soils.Arbitrator;

    ///<summary>
    /// The generic plant model
    /// </summary>
    /// \pre Summary A Summary model has to exist to write summary message.
    /// \pre Phenology A \ref Models.PMF.Phen.Phenology Phenology model is 
    /// optional to check whether plant has emerged.
    /// \pre OrganArbitrator A OrganArbitrator model is optional (not used currently).
    /// \pre Structure A Structure model is optional (not used currently).
    /// \pre Leaf A \ref Models.PMF.Organs.Leaf Leaf model is optional 
    /// to calculate water supply and demand ratio.
    /// \pre Root A Models.PMF.Organs.Root Root model is optional 
    /// to calculate water supply and demand ratio.
    /// \param CropType Used by several organs to determine the type of crop.
    /// \retval Population Number of plants per square meter. 
    /// \retval IsAlive Return true if plant is alive and in the ground.
    /// \retval IsEmerged Return true if plant has emerged.
    /// 
    /// On commencing simulation
    /// ------------------------
    /// OnSimulationCommencing is called on commencing simulation. Organs contain 
    /// all children which derive from model IOrgan. The model variables 
    /// are reset.
    /// 
    /// On sowing 
    /// -------------------------
    /// Plant is sown by a manager script in a APSIM model. For example,    
    /// \code
    /// 2012-10-23 [Maize].Sow(population:11, cultivar:"Pioneer_3153", depth:50, rowSpacing:710);
    /// \endcode
    /// 
    /// Sowing parameters should be specified, i.e. cultivar, population, depth, rowSpacing,
    /// maxCover (optional), and budNumber (optional).
    /// 
    /// Two events "Sowing" and "PlantSowing" are invoked to notify other models 
    /// to execute sowing events.
    /// <remarks>
    /// </remarks>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    public class Plant : ModelCollectionFromResource, ICrop
    {
        #region Class links
        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;
        /// <summary>The phenology</summary>
        [Link(IsOptional = true)]
        public Phenology Phenology = null;
        /// <summary>The arbitrator</summary>
        [Link(IsOptional = true)]
        public OrganArbitrator Arbitrator = null;
        /// <summary>The structure</summary>
        [Link(IsOptional = true)]
        public Structure Structure = null;
        /// <summary>The leaf</summary>
        [Link(IsOptional = true)]
        public Leaf Leaf = null;
        /// <summary>The root</summary>
        [Link(IsOptional = true)]
        public Root Root = null;
        [Link]
        Biomass AboveGround = null;

        #endregion

        #region Class properties and fields
        /// <summary>Used by several organs to determine the type of crop.</summary>
        public string CropType { get; set; }

        /// <summary>The sowing data</summary>
        [XmlIgnore]
        public SowPlant2Type SowingData { get; set; }

        /// <summary>Gets the organs.</summary>
        [XmlIgnore]
        public IOrgan[] Organs { get; private set; }

        ///<summary></summary>
        public RemovalFractions BiomassRemovalData { get; set; }
       
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
        [Units("0-1")]
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
        [XmlIgnore]
        [Description("Number of plants per meter2")]
        [Units("/m2")]
        public double Population { get; set; }

        /// <summary>Return true if plant is alive and in the ground.</summary>
        public bool IsAlive { get { return SowingData != null; } }

        /// <summary>Return true if plant has emerged</summary>
        public bool IsEmerged
        {
            get
            {
                if (Phenology != null)
                    return Phenology.Emerged;
                    //If the crop model has phenology and the crop is emerged return true
                else
                    return IsAlive;
                    //Else if the crop is in the grown returen true
            }
        }
        
        #endregion

        #region Class Events
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler Sowing;
        /// <summary>Occurs when a plant is sown.</summary>
        public event EventHandler<SowPlant2Type> PlantSowing;
        /// <summary>Occurs when a plant is about to be harvested.</summary>
        public event EventHandler Harvesting;
        /// <summary>Occurs when a plant is about to be cut.</summary>
        public event EventHandler Cutting;
        /// <summary>Occurs when a plant is about to be grazed.</summary>
        public event EventHandler Grazing;
        /// <summary>Occurs when a plant is about to be pruned.</summary>
        public event EventHandler Pruning;
        /// <summary>Occurs when a plant is ended via EndCrop.</summary>
        public event EventHandler PlantEnding;
        #endregion

        #region External Communications.  Method calls and EventHandlers
        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<IOrgan> organs = new List<IOrgan>();
            
            foreach (IOrgan organ in Apsim.Children(this, typeof(IOrgan)))
            {
                organs.Add(organ);
            }

            Organs = organs.ToArray();

            BiomassRemovalData = new RemovalFractions(Organs);
            
            Clear();
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhaseChange">The phase change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            if (Phenology != null && Leaf != null)
            {
                string message = Phenology.CurrentPhase.Start + "\r\n";
                if (Leaf != null)
                {
                    message += "  LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)" + "\r\n";
                    message += "  Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)" + "\r\n";
                }
                Summary.WriteMessage(this, message);
            }
        }

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
            SowingData.Plant = this;
            SowingData.Population = population;
            SowingData.Depth = depth;
            SowingData.Cultivar = cultivar;
            SowingData.MaxCover = maxCover;
            SowingData.BudNumber = budNumber;
            SowingData.RowSpacing = rowSpacing;
            this.Population = population;

            // Find cultivar and apply cultivar overrides.
            cultivarDefinition = PMF.Cultivar.Find(Cultivars, SowingData.Cultivar);
            cultivarDefinition.Apply(this);
            
            // Invoke an AboutToSow event.
            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            // Invoke a sowing event.
            if (PlantSowing != null)
                PlantSowing.Invoke(this, SowingData);

            
            Summary.WriteMessage(this, string.Format("A crop of " + CropType + " (cultivar = " + cultivar + ") was sown today at a population of " + Population + " plants/m2 with " + budNumber + " buds per plant at a row spacing of " + rowSpacing + " and a depth of " + depth + " mm"));
        }
        
        /// <summary>Harvest the crop.</summary>
        public void Harvest(RemovalFractions ManagerRemovalData = null)
        {
            // Invoke a harvesting event.
            if (Harvesting != null)
                Harvesting.Invoke(this, new EventArgs());

            int i = 0;
            //Set up the default BiomassRemovalData values
            foreach (IOrgan organ in Organs)
            {
               BiomassRemovalData.OrganList[i].FractionRemoved = organ.HarvestDefault.FractionRemoved;
               BiomassRemovalData.OrganList[i].FractionToResidue = organ.HarvestDefault.FractionToResidue;
               i += 1;
            }
            
            //OverWriter default values in BiomassRemovalData with any manager specified values
            ApplyManagerSpecifiedOverwrites(ManagerRemovalData);
            
            //Remove biomas from organs
            RemoveBiomassFromOrgans(BiomassRemovalData);



            Summary.WriteMessage(this, string.Format("A crop of " + CropType + " was harvested today."));
            
            foreach (IOrgan O in Organs)
                O.DoHarvest();

        }
        /// <summary>Cut the crop.</summary>
        public void Cut(RemovalFractions ManagerRemovalData = null)
        {
            int i = 0;
            //Set up the default values
            foreach (IOrgan organ in Organs)
            {
                BiomassRemovalData.OrganList[i].FractionRemoved = organ.CutDefault.FractionRemoved;
                BiomassRemovalData.OrganList[i].FractionToResidue = organ.CutDefault.FractionToResidue;
                i += 1;
            }

            //Do adjustments to phenology
            if ((ManagerRemovalData != null) && (ManagerRemovalData.PhenologyStageSet != 0))
                Phenology.ReSetToStage(ManagerRemovalData.PhenologyStageSet);
            
            //OverWriter default values in BiomassRemovalData with any manager specified values
            ApplyManagerSpecifiedOverwrites(ManagerRemovalData);

            //Remove biomas from organs
            RemoveBiomassFromOrgans(BiomassRemovalData);

            foreach (IOrgan O in Organs)
                O.DoCut();

            // Invoke a cutting event.
            if (Cutting != null)
                Cutting.Invoke(this, new EventArgs());
        }
        /// <summary>Harvest the crop.</summary>
        public void Graze(RemovalFractions ManagerRemovalData = null)
        {
            int i = 0;
            //Set up the default BiomassRemovalData values
            foreach (IOrgan organ in Organs)
            {
                BiomassRemovalData.OrganList[i].FractionRemoved = organ.GrazeDefault.FractionRemoved;
                BiomassRemovalData.OrganList[i].FractionToResidue = organ.GrazeDefault.FractionToResidue;
                i += 1;
            }

            //Do adjustments to phenology
            if ((ManagerRemovalData != null) && (ManagerRemovalData.PhenologyStageSet != 0))
                Phenology.ReSetToStage(ManagerRemovalData.PhenologyStageSet);

            //OverWriter default values in BiomassRemovalData with any manager specified values
            ApplyManagerSpecifiedOverwrites(ManagerRemovalData);

            //Remove biomas from organs
            RemoveBiomassFromOrgans(BiomassRemovalData);

            foreach (IOrgan O in Organs)
                O.DoGraze();

            // Invoke a cutting event.
            if (Grazing != null)
                Grazing.Invoke(this, new EventArgs());
        }
        /// <summary>Cut the crop.</summary>
        public void Prune(RemovalFractions ManagerRemovalData = null)
        {
            int i = 0;
            //Set up the default values
            foreach (IOrgan organ in Organs)
            {
                BiomassRemovalData.OrganList[i].FractionRemoved = organ.PruneDefault.FractionRemoved;
                BiomassRemovalData.OrganList[i].FractionToResidue = organ.PruneDefault.FractionToResidue;
                i += 1;
            }

            //Do adjustments to phenology
            if ((ManagerRemovalData != null) && (ManagerRemovalData.PhenologyStageSet != 0))
                Phenology.ReSetToStage(ManagerRemovalData.PhenologyStageSet);

            //OverWriter default values in BiomassRemovalData with any manager specified values
            ApplyManagerSpecifiedOverwrites(ManagerRemovalData);

            //Remove biomas from organs
            RemoveBiomassFromOrgans(BiomassRemovalData);

            // Invoke a pruning event.
            if (Pruning != null)
                Pruning.Invoke(this, new EventArgs());
        }
        /// <summary>End the crop.</summary>

        public void EndCrop()
        {
            Summary.WriteMessage(this, "Crop ending");

            foreach (IOrgan O in Organs)
                O.DoPlantEnding();

            // Invoke a plant ending event.
            if (PlantEnding != null)
                PlantEnding.Invoke(this, new EventArgs());

            Clear();
            cultivarDefinition.Unapply();
        }
        #endregion

        #region Private methods
        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            SowingData = null;
            Population = 0;
        }

        private void RemoveBiomassFromOrgans(RemovalFractions BiomassRemovalData)
        {
            if (BiomassRemovalData != null)  //Defalt is to remove nothing so unless some data is sent from manager do nothing
            {
                int i = 0;
                foreach (IOrgan organ in Organs)
                {
                    organ.RemoveBiomass = BiomassRemovalData.OrganList[i];
                    i += 1;
                }
            }
        }

        private void ApplyManagerSpecifiedOverwrites(RemovalFractions ManagerRemovalData)
        {
            //OverWriter default values in BiomassRemovalData with any manager specified values
            if (ManagerRemovalData != null)
                foreach (OrganBiomassRemovalType m in ManagerRemovalData.OrganList) //Set through each organ in manager sent RemovalData list
                    foreach (OrganBiomassRemovalType p in BiomassRemovalData.OrganList) //Step throgh each organ in plant constructed RemovalData list
                        if (String.Equals(m.NameOfOrgan, p.NameOfOrgan, StringComparison.OrdinalIgnoreCase)) //find a match on orgn names
                        {//Over write data if there is data there
                            if (m.FractionRemoved >= 0)
                                p.FractionRemoved = m.FractionRemoved;
                            if (m.FractionToResidue >= 0)
                                p.FractionToResidue = m.FractionToResidue;
                        }
        }
        #endregion
        
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (child is Cultivar)
                {
                }
                else
                    child.Document(tags, headingLevel, indent);
            }

            // now write all cultivars in a list.
            tags.Add(new AutoDocumentation.Heading("Cultivars", headingLevel));
            tags.Add(new AutoDocumentation.Paragraph("The section below lists each of the cultivars currently included in the model and each of the parameters that they differ from the base model", indent));
            foreach (IModel child in Apsim.Children(this, typeof(Cultivar)))
                child.Document(tags, headingLevel, indent);
        }
    }
}
