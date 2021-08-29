namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Core;
    using Models.Interfaces;
    using Functions;
    using Interfaces;
    using Library;
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using PMF;
    /// <summary>
    /// This is the basic organ class that contains biomass structures and transfers
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]

    public class Organ : Model, IAmOrganHearMeRoar, ICustomDocumentation, IOrgan
    {
        ///1. Links
        ///--------------------------------------------------------------------------------------------------

        /// <summary>The parent plant</summary>
        [Link]
        public Plant parentPlant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The senescence rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction SenescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction detachmentRateFunction = null;

        /// <summary>Wt in each pool when plant is initialised</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/plant")]
        public NutrientPoolFunctions InitialWt = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction maintenanceRespirationFunction = null;

        /// <summary>Dry matter conversion efficiency</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        public IFunction DMConversionEfficiency = null;

        /// <summary>remobilisation cost</summary>
        [Units("g/g")]
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction RemobilisationCost = null;

        /// <summary>The list of nurtients to arbitration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public OrganNutrientDelta Carbon { get; set; }

        /// <summary>The list of nurtients to arbitration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public OrganNutrientDelta Nitrogen { get; set; }

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001;

        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Organ constructor</summary>
        public Organ()
        {
            Carbon = new OrganNutrientDelta();
            Nitrogen = new OrganNutrientDelta();
            Clear();
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------
        /// <summary> The organs uptake object if it has one</summary>
        ///         
        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        public IWaterNitrogenUptake WaterNitrogenUptakeObject
        {
            get
            {
                return this.FindChild<IWaterNitrogenUptake>();
            }
        }

        /// <summary> The canopy object </summary>
        public IHasWaterDemand CanopyObjects
        {
            get
            {
                return this.FindChild<IHasWaterDemand>();
            }
        }

        /// <summary>The Carbon concentration of the organ</summary>
        [Description("Carbon concentration")]
        [Units("g/g")]
        public double Cconc { get; set; } = 0.4;

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; } = true;

        /// <summary>The live biomass state at start of the computation round</summary>
        [JsonIgnore]
        public OrganNutrientStates StartLive { get; private set; }

        /// <summary>The live biomass</summary>
        [JsonIgnore]
        public OrganNutrientStates Live { get; private set; }

        /// <summary>The dead biomass</summary>
        [JsonIgnore]
        public OrganNutrientStates Dead { get; private set; }

        /// <summary>Gets the total biomass</summary>
        [JsonIgnore]
        public OrganNutrientStates Total { get { return Live + Dead; } }

        /// <summary>Gets the biomass reallocated from senescing material</summary>
        [JsonIgnore]
        public OrganNutrientStates ReAllocated { get; private set; }

        /// <summary>Gets the biomass reallocated from senescing material</summary>
        [JsonIgnore]
        public OrganNutrientStates ReTranslocated { get; private set; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [JsonIgnore]
        public OrganNutrientStates Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [JsonIgnore]
        public OrganNutrientStates Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [JsonIgnore]
        public OrganNutrientStates Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public OrganNutrientStates Removed { get; private set; }

        /// <summary>Rate of senescence for the day</summary>
        [JsonIgnore]
        public double senescenceRate { get; private set; }

        /// <summary>efficiency of converting allocated DM to wt</summary>
        [JsonIgnore]
        public double dmConversionEfficiency { get; private set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double GrowthRespiration { get; set; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [JsonIgnore]
        [Units("g DM/g N")]
        public virtual double NFixationCost { get { return 0; } }

        /// <summary>Gets the maximum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MaxNconc { get { return Nitrogen.Concentrations != null ? Nitrogen.Concentrations.Maximum : 0; } }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MinNconc { get { return Nitrogen.Concentrations != null ? Nitrogen.Concentrations.Minimum : 0; } }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double CritNconc { get { return Nitrogen.Concentrations != null ? Nitrogen.Concentrations.Critical : 0; } }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double N { get { return Live.Nitrogen.Total + Dead.Nitrogen.Total; } }

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
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
        /// Gets the nitrogen factor.
        /// </summary>
        public double Fn
        {
            get
            {
                if (Live != null)
                    return MathUtilities.Divide(N, Wt * MinNconc, 1);
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
                    factor = MathUtilities.Divide(N - Live.Nitrogen.Structural, Wt * (CritNconc - MinNconc), 1.0);
                return Math.Min(1.0, factor);
            }
        }


        ///6. Public methods
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public virtual void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            //biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }

        /// <summary>Clears this instance.</summary>
        protected virtual void Clear()
        {
            Live = new OrganNutrientStates(Cconc);
            Dead = new OrganNutrientStates(Cconc);
            StartLive = new OrganNutrientStates(Cconc);
            ReAllocated = new OrganNutrientStates(Cconc);
            ReTranslocated = new OrganNutrientStates(Cconc);
            Allocated = new OrganNutrientStates(Cconc);
            Senesced = new OrganNutrientStates(Cconc);
            Detached = new OrganNutrientStates(Cconc);
            Removed = new OrganNutrientStates(Cconc);

            Carbon.Clear();
            Nitrogen.Clear();
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            ReAllocated.Clear();
            ReTranslocated.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive || parentPlant.IsEnding)
                ClearBiomassFlows();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == parentPlant)
            {
                Clear();
                ClearBiomassFlows();
                Live.Weight.Structural = InitialWt.Structural.Value();
                Live.Weight.Metabolic = InitialWt.Metabolic.Value();
                Live.Weight.Storage = InitialWt.Storage.Value();
                Live.Nitrogen.Structural = Live.Weight.Total * Nitrogen.Concentrations.Minimum;
                Live.Nitrogen.Metabolic = Live.Weight.Total * (Nitrogen.Concentrations.Critical - Nitrogen.Concentrations.Minimum);
                Live.Nitrogen.Storage = Live.Weight.Total * (Nitrogen.Concentrations.Maximum - Nitrogen.Concentrations.Critical);
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
             // save current state
            if (parentPlant.IsAlive)
                StartLive.SetTo(Live);
            senescenceRate = SenescenceRate.Value();
            dmConversionEfficiency = DMConversionEfficiency.Value();
        }

        
        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {

                //do reallocation
                ReAllocated.Carbon = Carbon.SuppliesAllocated.ReAllocation;
                ReAllocated.Nitrogen = Nitrogen.SuppliesAllocated.ReAllocation;
                Live.SubtractDelta(ReAllocated);

                //do retranslocation
                ReTranslocated.Carbon = Nitrogen.SuppliesAllocated.ReTranslocation;
                ReTranslocated.Nitrogen = Nitrogen.Supplies.ReTranslocation;
                Live.SubtractDelta(ReTranslocated);
                
                //do allocation
                Allocated.Carbon = Carbon.DemandsAllocated;
                Allocated.Nitrogen = Nitrogen.DemandsAllocated;
                Live.AddDelta(Allocated);

                //do senescene
                Senesced.Carbon = StartLive.Carbon * senescenceRate - Carbon.SuppliesAllocated.ReAllocation;
                Senesced.Nitrogen = StartLive.Carbon * senescenceRate - Nitrogen.SuppliesAllocated.ReAllocation;
                Live.SubtractDelta(Senesced);
                Dead.AddDelta(Senesced);
                
                // Do detachment
                double detachedFrac = detachmentRateFunction.Value();
                if (Dead.Weight.Total * (1.0 - detachedFrac) < BiomassToleranceValue)
                    detachedFrac = 1.0;  // remaining amount too small, detach all
                Detached = Dead * detachedFrac;
                Dead.SubtractDelta(Detached);

                if (Detached.Wt > 0.0)
                {
                    surfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, parentPlant.PlantType, Name);
                }

                // Do maintenance respiration
                if (maintenanceRespirationFunction.Value() > 0)
                {
                    //MaintenanceRespiration = (Live.MetabolicWt + Live.StorageWt) * maintenanceRespirationFunction.Value();
                   // Live.MetabolicWt *= (1 - maintenanceRespirationFunction.Value());
                   // Live.StorageWt *= (1 - maintenanceRespirationFunction.Value());
                }
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.AddDelta(Live);
                Detached.AddDelta(Dead);
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, parentPlant.PlantType, Name);
            }

            Clear();
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
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
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                //// List the parameters, properties, and processes from this organ that need to be documented:

                // document DM demands
                tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", indent));
                IModel DMDemand = this.FindChild("dmDemands");
                AutoDocumentation.DocumentModel(DMDemand, tags, headingLevel + 2, indent);

                // document N demands
                tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", indent));
                IModel NDemand = this.FindChild("nDemands");
                AutoDocumentation.DocumentModel(NDemand, tags, headingLevel + 2, indent);

                // document N concentration thresholds
                IModel MinN = this.FindChild("MinimumNConc");
                AutoDocumentation.DocumentModel(MinN, tags, headingLevel + 2, indent);
                IModel CritN = this.FindChild("CriticalNConc");
                AutoDocumentation.DocumentModel(CritN, tags, headingLevel + 2, indent);
                IModel MaxN = this.FindChild("MaximumNConc");
                AutoDocumentation.DocumentModel(MaxN, tags, headingLevel + 2, indent);
                IModel NDemSwitch = this.FindChild("NitrogenDemandSwitch");
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
                IModel DMReallocFac = this.FindChild("DMReallocationFactor");
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
                IModel DMRetransFac = this.FindChild("DMRetranslocationFactor");
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

                // document N supplies
                tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
                IModel NReallocFac = this.FindChild("NReallocationFactor");
                if (NReallocFac is Constant)
                {
                    if ((NReallocFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " can reallocate up to " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day if required by the plant arbitrator to meet N demands.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NReallocFac, tags, headingLevel + 2, indent);
                }
                IModel NRetransFac = this.FindChild("NRetranslocationFactor");
                if (NRetransFac is Constant)
                {
                    if ((NRetransFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " can retranslocate up to " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day if required by the plant arbitrator to meet N demands.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NRetransFac, tags, headingLevel + 2, indent);
                }

                // document senescence and detachment
                tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
                IModel SenRate = this.FindChild("SenescenceRate");
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

                IModel DetRate = this.FindChild("DetachmentRateFunction");
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
