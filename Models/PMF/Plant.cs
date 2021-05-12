namespace Models.PMF
{
    using Models.Core;
    using Models.Functions;
    using Models.Interfaces;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using Models.PMF.Phen;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Data;
    using Newtonsoft.Json;
    using APSIM.Shared.Utilities;
    using System.Globalization;

    ///<summary>
    /// # [Name]
    /// The generic plant model
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ScopedModel]
    public class Plant : ModelCollectionFromResource, IPlant, ICustomDocumentation, IPlantDamage
    {
        /// <summary>The summary</summary>
        [Link]
        private ISummary summary = null;

        /// <summary> Clock </summary>
        [Link]
        private IClock clock = null;

        /// <summary>The plant mortality rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("")]
        private IFunction mortalityRate = null;

        /// <summary>The phenology</summary>
        [Link(Type = LinkType.Child)]
        public IPhenology Phenology = null;

        /// <summary>The arbitrator</summary>
        [Link(IsOptional = true)]
        public IArbitrator Arbitrator = null;

        /// <summary>The structure</summary>
        [Link(IsOptional = true)]
        public IStructure structure = null;

        /// <summary>The leaf</summary>
        [Link(IsOptional = true)]
        public ICanopy Leaf = null;

        /// <summary>The root</summary>
        [Link(IsOptional = true)]
        public IRoot Root = null;

        /// <summary>Above ground weight</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public Biomass AboveGround { get; set; }

        /// <summary>Above ground weight</summary>
        public Biomass AboveGroundHarvestable { get { return AboveGround; } }

        /// <summary>Used by several organs to determine the type of crop.</summary>
        public string PlantType { get; set; }

        /// <summary>The sowing data</summary>
        [JsonIgnore]
        public SowingParameters SowingData { get; set; } = new SowingParameters();

        /// <summary>Gets the organs.</summary>
        [JsonIgnore]
        public IOrgan[] Organs { get; private set; }

        /// <summary>Gets a list of cultivar names</summary>
        public string[] CultivarNames
        {
            get
            {
                return new SortedSet<string>(FindAllDescendants<Cultivar>().SelectMany(c => c.GetNames())).ToArray();
            }
        }

        /// <summary>Holds the number of plants.</summary>
        private double plantPopulation = 0.0;
        /// <summary>
        /// Holds the date of sowing
        /// </summary>
        [JsonIgnore]
        public DateTime SowingDate { get; set; }

        /// <summary>Gets or sets the plant population.</summary>
        [JsonIgnore]
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
                    if (structure != null)
                    {
                        structure.DeltaPlantPopulation = InitialPopn - value;
                        structure.ProportionPlantMortality = 1 - (value / InitialPopn);
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
                if (Phenology is Phenology phenology)
                    return phenology.Emerged;
                return false;
            }
        }

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        public bool IsReadyForHarvesting
        {
            get
            {
                return Phenology.CurrentPhase is EndPhase;
            }
        }

        /// <summary>Returns true if the crop is being ended.</summary>
        /// <remarks>Used to clean up data the day after an EndCrop, enabling some reporting.</remarks>
        public bool IsEnding { get; set; }

        /// <summary>Counter for the number of days after corp being ended.</summary>
        /// <remarks>USed to clean up data the day after an EndCrop, enabling some reporting.</remarks>
        [Units("d")]
        public int DaysAfterEnding { get; set; }

        /// <summary>
        /// Number of days after sowing.
        /// </summary>
        [Units("d")]
        public int DaysAfterSowing
        {
            get
            {
                if (SowingData == null || SowingDate == DateTime.MinValue)
                    return 0;

                return Convert.ToInt32((clock.Today - SowingDate).TotalDays);
            }
        }

        /// <summary>A list of organs that can be damaged.</summary>
        List<IOrganDamage> IPlantDamage.Organs { get { return Organs.Cast<IOrganDamage>().ToList(); } }

        /// <summary>
        /// Total plant green cover from all organs
        /// </summary>
        [Units("-")]
        public double CoverGreen
        {
            get
            {
                double cover = 0;
                foreach (ICanopy canopy in this.FindAllDescendants<ICanopy>())
                    cover = 1 - (1.0 - cover) * (1.0 - canopy.CoverGreen);
                return cover;
            }
        }

        /// <summary>
        /// Total plant cover from all organs
        /// </summary>
        [Units("-")]
        public double CoverTotal
        {
            get
            {
                double cover = 0;
                foreach (ICanopy canopy in this.FindAllDescendants<ICanopy>())
                    cover = 1 - (1.0 - cover) * (1.0 - canopy.CoverTotal);
                return cover;
            }
        }
        /// <summary>Leaf area index.</summary>
        [Units("m^2/m^2")]
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

        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler Sowing;
        /// <summary>Occurs when a plant is sown.</summary>
        public event EventHandler<SowingParameters> PlantSowing;
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

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<IOrgan> organs = new List<IOrgan>();          
            foreach (IOrgan organ in this.FindAllChildren<IOrgan>())
                organs.Add(organ);

            Organs = organs.ToArray();
            IsEnding = false;
            DaysAfterEnding = 0;
            Clear();
            IEnumerable<string> duplicates = CultivarNames.GroupBy(x => x).Where(g => g.Count() > 1).Select(x => x.Key);
            if (duplicates.Count() > 0)
                throw new Exception("Duplicate Names in " + this.Name + " has duplicate cultivar names " + string.Join(",",duplicates));
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (sender == this && Leaf != null && AboveGround != null)
            {
                string message = Phenology.CurrentPhase.Start + "\r\n";
                if (Leaf != null)
                {
                    message += "  LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)" + "\r\n";
                    message += "  Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)" + "\r\n";
                }
                summary.WriteMessage(this, message);
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
            if (Population > 0.0)
                Population -= Population * mortalityRate.Value();
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
            SowingDate = clock.Today;

            SowingData = new SowingParameters();
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
            Cultivar cultivarDefinition = FindAllDescendants<Cultivar>().FirstOrDefault(c => c.IsKnownAs(SowingData.Cultivar));
            if (cultivarDefinition == null)
                throw new ApsimXException(this, $"Cannot find a cultivar definition for '{SowingData.Cultivar}'");

            cultivarDefinition.Apply(this);

            // Invoke an AboutToSow event.
            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            // Invoke a sowing event.
            if (PlantSowing != null)
                PlantSowing.Invoke(this, SowingData);

            summary.WriteMessage(this, string.Format("A crop of " + PlantType + " (cultivar = " + cultivar + ") was sown today at a population of " + Population + " plants/m2 with " + budNumber + " buds per plant at a row spacing of " + rowSpacing + " and a depth of " + depth + " mm"));
        }

        /// <summary>Harvest the crop.</summary>
        public void Harvest(RemovalFractions removalData)
        {
            RemoveBiomass("Harvest", removalData);
        }

        /// <summary>Harvest the crop.</summary>
        public void RemoveBiomass(string biomassRemoveType, RemovalFractions removalData = null)
        {
            summary.WriteMessage(this, string.Format("Biomass removed from crop " + Name + " by " + biomassRemoveType.TrimEnd('e') + "ing"));

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
            if (removalData != null && removalData.SetPhenologyStage != 0 && Phenology is Phenology phenology)
                phenology.SetToStage(removalData.SetPhenologyStage);

            // Reduce plant and stem population if thinning proportion specified
            if (removalData != null && removalData.SetThinningProportion != 0 && structure != null)
                structure.DoThin(removalData.SetThinningProportion);

            // Remove nodes from the main-stem
            if (removalData != null && removalData.NodesToRemove > 0)
                structure.DoNodeRemoval(removalData.NodesToRemove);
        }

        /// <summary>End the crop.</summary>
        public void EndCrop()
        {
            if (IsAlive == false)
                throw new Exception("EndCrop method called when no crop is planted.  Either your planting rule is not working or your end crop is happening at the wrong time");
            summary.WriteMessage(this, "Crop ending");

            // Invoke a plant ending event.
            if (PlantEnding != null)
                PlantEnding.Invoke(this, new EventArgs());

            Clear();
            IsEnding = true;
            IsAlive = false;
        }

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            SowingData = new SowingParameters();
            plantPopulation = 0.0;
            IsAlive = false;
            SowingDate = DateTime.MinValue;
        }

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

                foreach (IModel child in this.FindAllChildren<IModel>())
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

                foreach (IModel child in this.FindAllChildren<IModel>())
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
        /// Force emergence on the date called if emergence has not occured already
        /// </summary>
        public void SetEmergenceDate(string emergencedate)
        {
            foreach (EmergingPhase ep in this.FindAllDescendants<EmergingPhase>())
                {
                    ep.EmergenceDate=emergencedate;
                }
            SetGerminationDate(SowingDate.ToString("d-MMM", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Force germination on the date called if germination has not occured already
        /// </summary>
        public void SetGerminationDate(string germinationdate)
        {
            {
                foreach (GerminatingPhase gp in this.FindAllDescendants<GerminatingPhase>())
                {
                    gp.GerminationDate = germinationdate;
                }
            }
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
                if (structure != null)
                {
                    structure.DeltaPlantPopulation = InitialPopn - newPlantPopulation;
                    structure.ProportionPlantMortality = 1 - (newPlantPopulation / InitialPopn);
                }
            }
        }
    }
}
