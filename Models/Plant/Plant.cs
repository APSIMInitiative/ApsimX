namespace Models.PMF
{
    using Models.Core;
    using Models.Functions;
    using Models.Interfaces;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Models.PMF.Phen;
    using Models.PMF.Struct;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Data;
    using System.Xml.Serialization;
    using APSIM.Shared.Utilities;

    ///<summary>
    /// # [Name]
    /// The generic plant model
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ScopedModel]
    public class Plant : ModelCollectionFromResource, IPlant, ICustomDocumentation, IPlantDamage
    {
        #region Class links
        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary> The plant's zone</summary>
        [Link]
        public Zone Zone = null;

        /// <summary>The phenology</summary>
        [Link]
        public Phenology Phenology = null;
        /// <summary>The arbitrator</summary>
        [Link(IsOptional = true)]
        public IArbitrator Arbitrator = null;
        /// <summary>The structure</summary>
        [Link(IsOptional = true)]
        public Structure Structure = null;
        /// <summary>The Canopy</summary>
        [Link(IsOptional = true)]
        public ICanopy Canopy = null;
        /// <summary>The leaf</summary>
        [Link(IsOptional = true)]
        public ICanopy Leaf = null;
        /// <summary>The root</summary>
        [Link(IsOptional = true)]
        public Root Root = null;

        /// <summary>Above ground weight</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public Biomass AboveGround { get; set; }

        /// <summary>Above ground weight</summary>
        public Biomass AboveGroundHarvestable { get { return AboveGround; } }

        /// <summary> Clock </summary>
        [Link]
        public Clock Clock = null;

        #endregion

        #region Class properties and fields

        /// <summary>Used by several organs to determine the type of crop.</summary>
        public string CropType { get; set; }

        /// <summary>Gets a value indicating how leguminous a plant is</summary>
        [XmlIgnore]
        public double Legumosity { get; }

        /// <summary>Gets a value indicating whether the biomass is from a c4 plant or not</summary>
        [XmlIgnore]
        public bool IsC4 { get; }

        /// <summary>The sowing data</summary>
        [XmlIgnore]
        public SowPlant2Type SowingData { get; set; }

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
                    string name = cultivar.Name;
                    List<IModel> memos = Apsim.Children(cultivar, typeof(Memo));
                    foreach (IModel memo in memos)
                    {
                        name += '|' + ((Memo)memo).Text;
                    }

                    cultivarNames.Add(name);
                    if (cultivar.Alias != null)
                    {
                        foreach (string alias in cultivar.Alias)
                            cultivarNames.Add(alias + "|Alias for " + cultivar.Name);
                    }
                }

                return new List<string>(cultivarNames).ToArray();
            }
        }

        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarList
        {
            get
            {
                List<string> cultivarNames = new List<string>();
                foreach (Cultivar cultivar in this.Cultivars)
                {
                    string name = cultivar.Name;
                    cultivarNames.Add(name);
                    if (cultivar.Alias != null)
                    {
                        foreach (string alias in cultivar.Alias)
                            cultivarNames.Add(alias);
                    }
                }
                cultivarNames.Sort();
                return cultivarNames.ToArray();
            }
        }


        /// <summary>A property to return all cultivar definitions.</summary>
        private List<Cultivar> Cultivars
        {
            get
            {
                List<Cultivar> cultivars = new List<Cultivar>();
                foreach (Model model in Apsim.ChildrenRecursively(this, typeof(Cultivar)))
                    cultivars.Add(model as Cultivar);
                return cultivars;
            }
        }

        /// <summary>The current cultivar definition.</summary>
        private Cultivar cultivarDefinition;

        /// <summary>
        /// Constructor
        /// </summary>
        public Plant()
        {
            SowingData = new SowPlant2Type();
            IsAlive = false;

            string photosyntheticPathway = (string) Apsim.Get(this, "Leaf.Photosynthesis.FCO2.PhotosyntheticPathway");
            IsC4 = photosyntheticPathway != null && photosyntheticPathway == "C4";
            Legumosity = 0;
        }

        /// <summary>Holds the number of plants.</summary>
        private double plantPopulation = 0.0;
        /// <summary>
        /// Holds the date of sowing
        /// </summary>
        [XmlIgnore]
        public DateTime SowingDate { get; set; }

        /// <summary>Gets or sets the plant population.</summary>
        [XmlIgnore]
        [Description("Number of plants per meter2")]
        [Units("/m2")]
        public double Population
        {
            get { return plantPopulation; }
            set
            {
                double InitialPopn = plantPopulation;
                if (IsAlive && value <= 0.01)                    
                    EndCrop();  // the plant is dying due to population decline
                else
                {
                    plantPopulation = value;
                    if (Structure != null)
                    {
                        Structure.DeltaPlantPopulation = InitialPopn - value;
                        Structure.ProportionPlantMortality = 1 - (value / InitialPopn);
                    }
                }
            }
        }

        /// <summary>Return true if plant is alive and in the ground.</summary>
        public bool IsAlive { get; private set; }

        /// <summary>Return true if plant has emerged</summary>
        public bool IsEmerged
        {
            get
            {
                if (Phenology != null)
                    return Phenology.Emerged;
                return false;
            }
        }

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        public bool IsReadyForHarvesting
        {
            get
            {
                return Phenology.CurrentPhaseName == "ReadyForHarvesting";
            }
        }

        /// <summary>Returns true if the crop is being ended.</summary>
        /// <remarks>Used to clean up data the day after an EndCrop, enabling some reporting.</remarks>
        public bool IsEnding { get; set; }

        /// <summary>Counter for the number of days after corp being ended.</summary>
        /// <remarks>USed to clean up data the day after an EndCrop, enabling some reporting.</remarks>
        public int DaysAfterEnding { get; set; }

        /// <summary>A list of organs that can be damaged.</summary>
        List<IOrganDamage> IPlantDamage.Organs { get { return Organs.Cast<IOrganDamage>().ToList(); } }

        /// <summary>Leaf area index.</summary>
        public double LAI
        { 
            get
            {
                var leaf = Organs.FirstOrDefault(o => o is Leaf) as Leaf;
                if (leaf != null)
                    return leaf.LAI;

                var simpleLeaf = Organs.FirstOrDefault(o => o is SimpleLeaf) as SimpleLeaf;
                if (simpleLeaf != null)
                    return simpleLeaf.LAI;

                var perennialLeaf = Organs.FirstOrDefault(o => o is PerennialLeaf) as PerennialLeaf;
                if (perennialLeaf != null)
                    return perennialLeaf.LAI;

                return 0;
            }
        }


        /// <summary>Amount of assimilate available to be damaged.</summary>
        public double AssimilateAvailable => throw new NotImplementedException();

        /// <summary>Harvest the crop</summary>
        public void Harvest() { Harvest(null); }

        /// <summary>The plant mortality rate</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("")]
        IFunction MortalityRate = null;
        #endregion

        #region Class Events
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler Sowing;
        /// <summary>Occurs when a plant is sown.</summary>
        public event EventHandler<SowPlant2Type> PlantSowing;
        /// <summary>Occurs when a plant is about to be harvested.</summary>
        public event EventHandler Harvesting;
        /// <summary>Occurs when a plant is ended via EndCrop.</summary>
        public event EventHandler PlantEnding;
        /// <summary>Occurs when a plant is about to be winter pruned.</summary>
        public event EventHandler Pruning;
        /// <summary>Occurs when a plant is about to be leaf plucking.</summary>
        public event EventHandler LeafPlucking;
        /// <summary>Occurs when a plant is about to be cutted.</summary>
        public event EventHandler Cutting;
        /// <summary>Occurs when a plant is about to be grazed.</summary>
        public event EventHandler Grazing;
        /// <summary>Occurs when a plant is about to flower</summary>
        public event EventHandler Flowering;
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
                organs.Add(organ);

            Organs = organs.ToArray();
            IsEnding = false;
            DaysAfterEnding = 0;
            Clear();
            IEnumerable<string> duplicates = CultivarList.GroupBy(x => x).Where(g => g.Count() > 1).Select(x => x.Key);
            if (duplicates.Count() > 0)
                throw new Exception("Duplicate Names in " + this.Name + " has duplicate cultivar names " + string.Join(",",duplicates));
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (sender == this && Canopy != null && AboveGround != null)
            {
                string message = Phenology.CurrentPhase.Start + "\r\n";
                if (Canopy != null)
                {
                    message += "  LAI = " + Canopy.LAI.ToString("f2") + " (m^2/m^2)" + "\r\n";
                    message += "  Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)" + "\r\n";
                }
                Summary.WriteMessage(this, message);
                if (Phenology.CurrentPhase.Start == "Flowering" && Flowering != null)
                    Flowering.Invoke(this, null);
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            //Reduce plant population in case of mortality
            if (Population > 0.0 && MortalityRate != null)
                Population -= Population * MortalityRate.Value();
        }

        /// <summary>Called at the end of the day.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfDay")]
        private void EndOfDay(object sender, EventArgs e)
        {
            // Check whether the plant was terminated (yesterday), complete termination
            if (IsEnding)
                if (DaysAfterEnding > 0)
                    IsEnding = false;
                else
                    DaysAfterEnding += 1;
        }

        /// <summary>Sow the crop with the specified parameters.</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth mm.</param>
        /// <param name="rowSpacing">The row spacing mm.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        /// <param name="rowConfig">SkipRow configuration.</param>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1, double rowConfig = 0)
        {
            SowingDate = Clock.Today;

            SowingData = new SowPlant2Type();
            SowingData.Plant = this;
            SowingData.Population = population;
            SowingData.Depth = depth;
            SowingData.Cultivar = cultivar;
            SowingData.MaxCover = maxCover;
            SowingData.BudNumber = budNumber;
            SowingData.RowSpacing = rowSpacing;
            SowingData.SkipType = rowConfig;

            if (rowConfig == 0)
            {
                // No skip row
                SowingData.SkipPlant = 1.0;
                SowingData.SkipRow = 0.0;
            }
            if (rowConfig == 1)
            {
                // Alternate rows (plant 1 – skip 1)
                SowingData.SkipPlant = 1.0;
                SowingData.SkipRow = 1.0;
            }
            if (rowConfig == 2)
            {
                // Planting two rows and skipping one row (plant 2 – skip 1)
                SowingData.SkipPlant = 2.0;
                SowingData.SkipRow = 1.0;
            }
            if (rowConfig == 3)
            {
                // Alternate pairs of rows (plant 2 – skip 2)
                SowingData.SkipPlant = 2.0;
                SowingData.SkipRow = 2.0;
            }

            // Adjusting number of plant per meter in each row
            SowingData.SkipDensityScale = 1.0 + SowingData.SkipRow / SowingData.SkipPlant;

            IsAlive = true;

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
        public void Harvest(RemovalFractions removalData)
        {
            RemoveBiomass("Harvest", removalData);
        }

        /// <summary>Harvest the crop.</summary>
        public void RemoveBiomass(string biomassRemoveType, RemovalFractions removalData = null)
        {
            Summary.WriteMessage(this, string.Format("Biomass removed from crop " + Name + " by " + biomassRemoveType.TrimEnd('e') + "ing"));

            // Invoke specific defoliation events.
            if (biomassRemoveType == "Harvest" && Harvesting != null)
                Harvesting.Invoke(this, new EventArgs());
            
            if (biomassRemoveType == "Prune" && Pruning != null)
                Pruning.Invoke(this, new EventArgs());

            if (biomassRemoveType == "LeafPluck" && LeafPlucking != null)
                LeafPlucking.Invoke(this, new EventArgs());

            if (biomassRemoveType == "Cut" && Cutting != null)
                Cutting.Invoke(this, new EventArgs());

            if (biomassRemoveType == "Graze" && Grazing != null)
                Grazing.Invoke(this, new EventArgs());

            // Set up the default BiomassRemovalData values
            foreach (IOrgan organ in Organs)
            {
                // Get the default removal fractions
                OrganBiomassRemovalType biomassRemoval = null;
                if (removalData != null)
                    biomassRemoval = removalData.GetFractionsForOrgan(organ.Name);
                organ.RemoveBiomass(biomassRemoveType, biomassRemoval);
            }

            // Reset the phenology if SetPhenologyStage specified.
            if (removalData != null && removalData.SetPhenologyStage != 0)
                Phenology.SetToStage(removalData.SetPhenologyStage);

            // Reduce plant and stem population if thinning proportion specified
            if (removalData != null && removalData.SetThinningProportion != 0 && Structure != null)
                Structure.doThin(removalData.SetThinningProportion);

            // Remove nodes from the main-stem
            if (removalData != null && removalData.NodesToRemove > 0)
                Structure.doNodeRemoval(removalData.NodesToRemove);
        }

        /// <summary>End the crop.</summary>
        public void EndCrop()
        {
            if (IsAlive == false)
                throw new Exception("EndCrop method called when no crop is planted.  Either your planting rule is not working or your end crop is happening at the wrong time");
            Summary.WriteMessage(this, "Crop ending");

            // Invoke a plant ending event.
            if (PlantEnding != null)
                PlantEnding.Invoke(this, new EventArgs());

            Clear();
            IsEnding = true;
            if (cultivarDefinition != null)
                cultivarDefinition.Unapply();
            IsAlive = false;
        }
        #endregion

        #region Private methods
        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            SowingData = new SowPlant2Type();
            plantPopulation = 0.0;
            IsAlive = false;
        }
        #endregion

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                tags.Add(new AutoDocumentation.Paragraph("The " + this.Name + " model is constructed from the following list of software components.  Details of the implementation and model parameterisation are provided in the following sections.", indent));
                // Write Plant Model Table
                tags.Add(new AutoDocumentation.Paragraph("**List of Plant Model Components.**", indent));
                DataTable tableData = new DataTable();
                tableData.Columns.Add("Component Name", typeof(string));
                tableData.Columns.Add("Component Type", typeof(string));

                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.GetType() != typeof(Memo) && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(CultivarFolder) && child.GetType() != typeof(CompositeBiomass))
                    {
                        DataRow row = tableData.NewRow();
                        row[0] = child.Name;
                        row[1] = child.GetType().ToString();
                        tableData.Rows.Add(row);
                    }
                }
                tags.Add(new AutoDocumentation.Table(tableData, indent));

                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent, true);
            }
        }

        /// <summary>Removes a given amount of biomass (and N) from the plant.</summary>
        /// <param name="amountToRemove">The amount of biomass to remove (kg/ha)</param>
        public Biomass RemoveBiomass(double amountToRemove)
        {
            var defoliatedBiomass = new Biomass();
            var preRemovalBiomass = AboveGround.Wt*10;
            foreach (var organ in Organs.Cast<IOrganDamage>())
            {
                if (organ.IsAboveGround)
                {
                    // These calculations convert organ live weight from g/m2 to kg/ha
                    var amountLiveToRemove = organ.Live.Wt * 10 / preRemovalBiomass * amountToRemove;
                    var amountDeadToRemove = organ.Dead.Wt * 10 / preRemovalBiomass * amountToRemove;
                    var fractionLiveToRemove = MathUtilities.Divide(amountLiveToRemove, (organ.Live.Wt * 10), 0);
                    var fractionDeadToRemove = MathUtilities.Divide(amountDeadToRemove, (organ.Dead.Wt * 10), 0);
                    var defoliatedDigestibility = organ.Live.DMDOfStructural * fractionLiveToRemove
                                                + organ.Dead.DMDOfStructural * fractionDeadToRemove;
                    var defoliatedDM = amountLiveToRemove + amountDeadToRemove;
                    var defoliatedN = organ.Live.N * 10 * fractionLiveToRemove + organ.Dead.N * 10 * fractionDeadToRemove;
                    if (defoliatedDM > 0)
                    {
                        RemoveBiomass(organ.Name, "Graze",
                                      new OrganBiomassRemovalType()
                                      {
                                          FractionLiveToRemove = fractionLiveToRemove,
                                          FractionDeadToRemove = fractionDeadToRemove
                                      });

                        defoliatedBiomass += new Biomass()
                        {
                            StructuralWt = defoliatedDM,
                            StructuralN = defoliatedN,
                            DMDOfStructural = defoliatedDigestibility
                        };
                    }
                }
            }
            return defoliatedBiomass;
        }

        /// <summary>
        /// Remove biomass from an organ.
        /// </summary>
        /// <param name="organName">Name of organ.</param>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove.</param>
        public void RemoveBiomass(string organName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            var organ = Organs.FirstOrDefault(o => o.Name.Equals(organName, StringComparison.InvariantCultureIgnoreCase));
            if (organ == null)
                throw new Exception("Cannot find organ to remove biomass from. Organ: " + organName);
            organ.RemoveBiomass(biomassRemoveType, biomassToRemove);

            // Also need to reduce LAI if canopy.
            if (organ is ICanopy)
            {
                var totalFractionToRemove = biomassToRemove.FractionLiveToRemove + biomassToRemove.FractionLiveToResidue;
                var leaf = Organs.FirstOrDefault(o => o is ICanopy) as ICanopy;
                var lai = leaf.LAI;
                ReduceCanopy(lai * totalFractionToRemove);
            }
        }

        /// <summary>
        /// Set the plant leaf area index.
        /// </summary>
        /// <param name="deltaLAI">Delta LAI.</param>
        public void ReduceCanopy(double deltaLAI)
        {
            var leaf = Organs.FirstOrDefault(o => o is ICanopy) as ICanopy;
            var lai = leaf.LAI;
            if (lai > 0)
                leaf.LAI = lai - deltaLAI;
        }

        /// <summary>
        /// Set the plant root length density.
        /// </summary>
        /// <param name="rootLengthModifier">The root length modifier due to root damage (0-1).</param>
        public void ReduceRootLengthDensity(double rootLengthModifier)
        {
            if (Root != null)
                Root.RootLengthDensityModifierDueToDamage = rootLengthModifier;
        }

        /// <summary>
        /// Remove an amount of assimilate from the plant.
        /// </summary>
        /// <param name="deltaAssimilate">The amount of assimilate to remove (g/m2).</param>
        public void RemoveAssimilate(double deltaAssimilate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reduce the plant population.
        /// </summary>
        /// <param name="newPlantPopulation">The new plant population.</param>
        public void ReducePopulation(double newPlantPopulation)
        {
            double InitialPopn = plantPopulation;
            if (IsAlive && newPlantPopulation <= 0.01)
                EndCrop();  // the plant is dying due to population decline
            else
            {
                plantPopulation = newPlantPopulation;
                if (Structure != null)
                {
                    Structure.DeltaPlantPopulation = InitialPopn - newPlantPopulation;
                    Structure.ProportionPlantMortality = 1 - (newPlantPopulation / InitialPopn);
                }
            }
        }
    }
}
