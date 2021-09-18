namespace Models.PMF
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

    public class Organ : Model, ICustomDocumentation, IOrgan
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
        public IFunction senescenceRateFunction = null;

        /// <summary>The detachment rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction detachmentRateFunction = null;

        /// <summary>Wt in each pool when plant is initialised</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/plant")]
        public IFunction InitialWt = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction TotalDMDemand = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction maintenanceRespirationFunction = null;

        /// <summary>The list of nurtients to arbitration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public OrganNutrientDelta Carbon = null;

        /// <summary>The list of nurtients to arbitration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public OrganNutrientDelta Nitrogen = null;

        
        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double tolerence = 1e-12;

        private double startLiveWt { get; set; }
        private double startLiveN { get; set; }


        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Organ constructor</summary>
        public Organ()
        {
            //Carbon = new OrganNutrientDelta();
            //Nitrogen = new OrganNutrientDelta();
            Clear();
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------
        /// <summary> The organs uptake object if it has one</summary>
        ///         
        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Interface to uptakes</summary>
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

        /// <summary>
        /// Object that contains root specific functionality.  Only present if the organ is representing a root
        /// </summary>
        public RootNetwork RootNetworkObject { get; set; }
        
        /// <summary>The Carbon concentration of the organ</summary>
        [Description("Carbon concentration")]
        [Units("g/g")]
        public double Cconc { get; set; } = 0.4;

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        [Description("Is organ above ground?")]
        public bool IsAboveGround { get; set; } = true;

        /// <summary>The live biomass</summary>
        public OrganNutrientsState Live { get; private set; }

        /// <summary>The dead biomass</summary>
        public OrganNutrientsState Dead { get; private set; }

        /// <summary>Gets the total biomass</summary>
        [JsonIgnore]
        public OrganNutrientsState Total { get { return Live + Dead; } }

        /// <summary>Gets the biomass reallocated from senescing material</summary>
        [JsonIgnore]
        public OrganNutrientsState ReAllocated { get; private set; }

        /// <summary>Gets the biomass reallocated from senescing material</summary>
        [JsonIgnore]
        public OrganNutrientsState ReTranslocated { get; private set; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [JsonIgnore]
        public OrganNutrientsState Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [JsonIgnore]
        public OrganNutrientsState Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [JsonIgnore]
        public OrganNutrientsState Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public OrganNutrientsState LiveRemoved { get; private set; }
        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public OrganNutrientsState DeadRemoved { get; private set; }

        /// <summary>Rate of senescence for the day</summary>
        [JsonIgnore]
        public double totalDMDemand { get; private set; }

        /// <summary>Rate of senescence for the day</summary>
        [JsonIgnore]
        public double senescenceRate { get; private set; }

        /// <summary>the detachment rate for the day</summary>
        [JsonIgnore]
        public double detachmentRate { get; private set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double GrowthRespiration { get; set; }

        /// <summary>Gets the maximum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MaxNconc { get; private set; }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double MinNconc { get; private set; }

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double CritNconc { get; private set; }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double Wt { get; private set; }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        [Units("g/m^2")]
        public double N { get; private set; }
        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [JsonIgnore]
        [Units("g/g")]
        public double Nconc { get; private set; }

        /// <summary>
        /// Gets the nitrogen factor.
        /// </summary>
        public double Fn { get; private set; }

        /// <summary>
        /// Gets the metabolic N concentration factor.
        /// </summary>
        public double FNmetabolic { get; private set; }


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
            Live = new OrganNutrientsState(Cconc, this);
            Dead = new OrganNutrientsState(Cconc, this);
            ReAllocated = new OrganNutrientsState(Cconc, null);
            ReTranslocated = new OrganNutrientsState(Cconc, null);
            Allocated = new OrganNutrientsState(Cconc, null);
            Senesced = new OrganNutrientsState(Cconc, null);
            Detached = new OrganNutrientsState(Cconc, null);
            LiveRemoved = new OrganNutrientsState(Cconc, null);
            DeadRemoved = new OrganNutrientsState(Cconc, null);
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            ReAllocated.Clear();
            ReTranslocated.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            LiveRemoved.Clear();
            DeadRemoved.Clear();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            RootNetworkObject = this.FindChild<RootNetwork>();
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
            totalDMDemand = TotalDMDemand.Value();
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
                Nitrogen.setConcentrationsOrProportions();
                
                NutrientPoolsState initC = new NutrientPoolsState(
                    InitialWt.Value() * Cconc * Carbon.ConcentrationOrFraction.Structural,
                    InitialWt.Value() * Cconc * Carbon.ConcentrationOrFraction.Metabolic,
                    InitialWt.Value() * Cconc * Carbon.ConcentrationOrFraction.Storage,
                    null);
                Live.Carbon.SetTo(initC, Live);
                Dead.Carbon.SetTo(new NutrientPoolsState(0, 0, 0, null), Dead);
                
                NutrientPoolsState initN = new NutrientPoolsState(
                    Live.Weight.Total * Nitrogen.ConcentrationOrFraction.Structural,
                    Live.Weight.Total * (Nitrogen.ConcentrationOrFraction.Metabolic - Nitrogen.ConcentrationOrFraction.Structural),
                    Live.Weight.Total * (Nitrogen.ConcentrationOrFraction.Storage - Nitrogen.ConcentrationOrFraction.Metabolic),
                    null);
                Live.Nitrogen.SetTo(initN, Live);
                Dead.Nitrogen.SetTo(new NutrientPoolsState(0, 0, 0,null),Dead);
                
                if (RootNetworkObject != null)
                    RootNetworkObject.InitailiseNetwork(Live);
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsEmerged)
            {
                startLiveN = Live.N;
                startLiveWt = Live.Wt;
                senescenceRate = senescenceRateFunction.Value();
                detachmentRate = detachmentRateFunction.Value();
                Carbon.SetSuppliesAndDemands();
            }
        }

        
        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsEmerged)
            {
                //Calculate biomass to be lost from senescene
                if (senescenceRate > 0)
                {
                    Senesced.SetTo(Live * senescenceRate);
                    Live.SubtractDelta(Senesced);

                    //Catch the bits that were reallocated and add the bits that wernt into dead.
                    ReAllocated.Carbon.SetTo(Carbon.SuppliesAllocated.ReAllocation, ReAllocated);
                    ReAllocated.Nitrogen.SetTo(Nitrogen.SuppliesAllocated.ReAllocation, ReAllocated);
                    Senesced.SubtractDelta(ReAllocated);
                    Dead.AddDelta(Senesced);
                }

                //Retranslocate from live pools
                ReTranslocated.Carbon.SetTo(Carbon.SuppliesAllocated.ReTranslocation,ReTranslocated);
                ReTranslocated.Nitrogen.SetTo(Nitrogen.SuppliesAllocated.ReTranslocation,ReTranslocated);
                Live.SubtractDelta(ReTranslocated);

                //Add in todays fresh allocation
                Allocated.Carbon.SetTo(Carbon.DemandsAllocated, Allocated);
                Allocated.Nitrogen.SetTo(Nitrogen.DemandsAllocated, Allocated);
                Live.AddDelta(Allocated);

                // Do detachment
                if ((detachmentRate > 0) && (Dead.Wt > 0))
                {
                    if (Dead.Weight.Total * (1.0 - detachmentRate) < 0.00000001)
                        detachmentRate = 1.0;  // remaining amount too small, detach all
                    Detached = Dead * detachmentRate;
                    Dead.SubtractDelta(Detached);
                    surfaceOrganicMatter.Add(Detached.Wt * 10, Detached.N * 10, 0, parentPlant.PlantType, Name);
                }

                
                // Do maintenance respiration
                if (maintenanceRespirationFunction.Value() > 0)
                {
                    //MaintenanceRespiration = (Live.MetabolicWt + Live.StorageWt) * maintenanceRespirationFunction.Value();
                   // Live.MetabolicWt *= (1 - maintenanceRespirationFunction.Value());
                   // Live.StorageWt *= (1 - maintenanceRespirationFunction.Value());
                }

                if (RootNetworkObject != null)
                {
                    RootNetworkObject.PartitionBiomassThroughSoil(ReAllocated, ReTranslocated, Allocated, Senesced, Detached, LiveRemoved, DeadRemoved);
                    RootNetworkObject.GrowRootDepth();
                }

              /*  double nBal = Math.Abs(Live.N - (startLiveN + Allocated.N - Senesced.N - ReAllocated.N - ReTranslocated.N));
                if (nBal > tolerence)
                    throw new Exception("N mass balance violation");
                double cBal = Math.Abs(Live.Wt - (startLiveWt + Allocated.Wt - Senesced.Wt - ReAllocated.Wt - ReTranslocated.Wt));
                if (cBal > tolerence)
                    throw new Exception("mass balance violation");*/

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

        /// <summary> Update properties </summary>
        public void UpdateProperties()
        {
            MaxNconc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Storage : 0;
            MinNconc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Structural : 0;
            CritNconc = Nitrogen.ConcentrationOrFraction != null ? Nitrogen.ConcentrationOrFraction.Metabolic : 0;
            Wt = Live.Wt + Dead.Wt; 
            N = Live.Nitrogen.Total + Dead.Nitrogen.Total;
            Nconc = Wt > 0.0 ? N / Wt : 0.0;
            Fn = Live != null ? MathUtilities.Divide(Live.Nitrogen.Total, Live.Wt * MaxNconc, 1) : 0;
            FNmetabolic = (Live != null) ? Math.Min(1.0, MathUtilities.Divide(Nconc - MinNconc, CritNconc - MinNconc, 0)) : 0;
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
