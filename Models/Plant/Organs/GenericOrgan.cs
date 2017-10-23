namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Library;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This organ is simulated using a generic organ type.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class GenericOrgan : Model, IOrgan, IArbitration
    {
        /// <summary>The plant</summary>
        [Link]
        private Plant Plant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The senescence rate function</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction SenescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction DetachmentRateFunction = null;

        /// <summary>The N retranslocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction NRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction NReallocationFactor = null;

        /// <summary>The nitrogen demand switch</summary>
        [ChildLinkByName]
        private IFunction NitrogenDemandSwitch = null;

        /// <summary>The DM retranslocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction DMRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction DMReallocationFactor = null;

        /// <summary>The DM structural fraction</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction StructuralFraction = null;

        /// <summary>The DM demand function</summary>
        [ChildLinkByName]
        [Units("g/m2/d")]
        private IFunction DMDemandFunction = null;

        /// <summary>The initial biomass dry matter weight</summary>
        [ChildLinkByName]
        [Units("g/m2")]
        private IFunction InitialWtFunction = null;

        /// <summary>The maximum N concentration</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction MaximumNConc = null;

        /// <summary>The minimum N concentration</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction MinimumNConc = null;

        /// <summary>The critical N concentration</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction CriticalNConc = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction MaintenanceRespirationFunction = null;

        /// <summary>Dry matter conversion efficiency</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction DMConversionEfficiency = null;

        /// <summary>Tolerance for biomass comparisons</summary>
        private double BiomassToleranceValue = 0.0000000001;   // 10E-10

        /// <summary>The live biomass state at start of the computation round</summary>
        private Biomass StartLive = null;

        /// <summary>The dry matter supply</summary>
        private BiomassSupplyType dryMatterSupply = new BiomassSupplyType();

        /// <summary>The nitrogen supply</summary>
        private BiomassSupplyType nitrogenSupply = new BiomassSupplyType();

        /// <summary>The dry matter demand</summary>
        private BiomassPoolType dryMatterDemand = new BiomassPoolType();

        /// <summary>Structural nitrogen demand</summary>
        private BiomassPoolType nitrogenDemand = new BiomassPoolType();

        /// <summary>The potential DM allocation</summary>
        private double PotentialDMAllocation = 0.0;

        /// <summary>The potential structural DM allocation</summary>
        private double PotentialStructuralDMAllocation = 0.0;

        /// <summary>The potential metabolic DM allocation</summary>
        private double PotentialMetabolicDMAllocation = 0.0;

        /// <summary>Constructor</summary>
        public GenericOrgan()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>The live biomass</summary>
        [XmlIgnore]
        public Biomass Live { get; set; }

        /// <summary>The dead biomass</summary>
        [XmlIgnore]
        public Biomass Dead { get; set; }

        /// <summary>Gets the total biomass</summary>
        [XmlIgnore]
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [XmlIgnore]
        public Biomass Allocated { get; set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [XmlIgnore]
        public Biomass Senesced { get; set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        [XmlIgnore]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [XmlIgnore]
        public double GrowthRespiration { get; set; }

        /// <summary>Gets the DM demand for this computation round.</summary>
        public virtual BiomassPoolType DMDemand { get { return dryMatterDemand; } }

        /// <summary>Gets the DM supply for this computation round.</summary>
        [XmlIgnore]
        public virtual BiomassSupplyType DMSupply { get { return dryMatterSupply; } }

        /// <summary>Sets the dm potential allocation.</summary>
        [XmlIgnore]
        public BiomassPoolType DMPotentialAllocation
        {
            set
            {
                PotentialMetabolicDMAllocation = value.Metabolic;
                PotentialStructuralDMAllocation = value.Structural;
                PotentialDMAllocation = value.Structural + value.Metabolic;
            }
        }

        /// <summary>Gets the N demand for this computation round.</summary>
        [XmlIgnore]
        public virtual BiomassPoolType NDemand { get { return nitrogenDemand; } }

        /// <summary>Gets the N supply for this computation round.</summary>
        [XmlIgnore]
        public virtual BiomassSupplyType NSupply { get { return nitrogenSupply; } }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public virtual double NFixationCost { get { return 0; } set { } }

        /// <summary>Sets the dm allocation.</summary>
        [XmlIgnore]
        public virtual BiomassAllocationType DMAllocation
        {
            set
            {
                // get DM lost by respiration (growth respiration)
                GrowthRespiration = 0.0;
                GrowthRespiration += value.Structural * (1.0 - DMConversionEfficiency.Value())
                                  + value.Storage * (1.0 - DMConversionEfficiency.Value())
                                  + value.Metabolic * (1.0 - DMConversionEfficiency.Value());

                // allocate structural DM
                Allocated.StructuralWt = Math.Min(value.Structural * DMConversionEfficiency.Value(), dryMatterDemand.Structural);
                Live.StructuralWt += Allocated.StructuralWt;

                // allocate non structural DM
                if ((value.Storage * DMConversionEfficiency.Value() - DMDemand.Storage) > BiomassToleranceValue)
                    throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");
                if (DMDemand.Storage > 0.0)
                {
                    Allocated.StorageWt = value.Storage * DMConversionEfficiency.Value();
                    Live.StorageWt += Allocated.StorageWt;
                }

                // allocate metabolic DM
                Allocated.MetabolicWt = value.Metabolic * DMConversionEfficiency.Value();

                // Retranslocation
                if (value.Retranslocation - StartLive.StorageWt > BiomassToleranceValue)
                    throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);
                Live.StorageWt -= value.Retranslocation;
                Allocated.StorageWt -= value.Retranslocation;
            }
        }

        /// <summary>Sets the n allocation.</summary>
        [XmlIgnore]
        public virtual BiomassAllocationType NAllocation
        {
            set
            {
                Live.StructuralN += value.Structural;
                Live.StorageN += value.Storage;
                Live.MetabolicN += value.Metabolic;

                Allocated.StructuralN += value.Structural;
                Allocated.StorageN += value.Storage;
                Allocated.MetabolicN += value.Metabolic;

                // Retranslocation
                if (MathUtilities.IsGreaterThan(value.Retranslocation, StartLive.StorageN - nitrogenSupply.Retranslocation))
                    throw new Exception("N retranslocation exceeds non structural nitrogen in organ: " + Name);
                Live.StorageN -= value.Retranslocation;
                Allocated.StorageN -= value.Retranslocation;

                // Reallocation
                if (MathUtilities.IsGreaterThan(value.Reallocation, StartLive.StorageN))
                    throw new Exception("N reallocation exceeds non structural nitrogen in organ: " + Name);
                Live.StorageN -= value.Reallocation;
                Allocated.StorageN -= value.Reallocation;
            }
        }

        /// <summary>Gets the maximum N concentration.</summary>
        [XmlIgnore]
        public double MaxNconc { get { return MaximumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [XmlIgnore]
        public double MinNconc { get { return MinimumNConc.Value(); } }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [XmlIgnore]
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [XmlIgnore]
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [XmlIgnore]
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

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="value">The fractions of biomass to remove</param>
        public virtual void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType value)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, value, Live, Dead, Removed, Detached);
        }

        /// <summary>Computes the amount of structural DM demanded.</summary>
        public double DemandedDMStructural()
        {
            if (DMConversionEfficiency.Value() > 0.0)
            {
                double demandedDM = DMDemandFunction.Value() * StructuralFraction.Value() / DMConversionEfficiency.Value();
                return demandedDM;
            }
            else
            { // Conversion efficiency is zero!!!!
                return 0.0;
            }
        }

        /// <summary>Computes the amount of non structural DM demanded.</summary>
        public double DemandedDMStorage()
        {
            // Assumes that StructuralFraction is always greater than zero
            if (DMConversionEfficiency.Value() > 0.0)
            {
                double theoreticalMaximumDM = MathUtilities.Divide(StartLive.StructuralWt + dryMatterDemand.Structural, StructuralFraction.Value(), 0);
                double baseAllocated = StartLive.StructuralWt + StartLive.StorageWt + dryMatterDemand.Structural;
                double demandedDM = MathUtilities.Divide(Math.Max(0.0, theoreticalMaximumDM - baseAllocated), DMConversionEfficiency.Value(), 0);
                return demandedDM;
            }
            else
            { // Conversion efficiency is zero!!!!
                return 0.0;
            }
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        public double AvailableDMRetranslocation()
        {
            double availableDM = Math.Max(0.0, StartLive.StorageWt - dryMatterSupply.Reallocation) * DMRetranslocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM retranslocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            double availableDM = StartLive.StorageWt * SenescenceRate.Value() * DMReallocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        public virtual BiomassSupplyType CalculateDryMatterSupply()
        {
            dryMatterSupply.Retranslocation = AvailableDMRetranslocation();
            dryMatterSupply.Reallocation = AvailableDMReallocation();
            dryMatterSupply.Fixation = 0;
            dryMatterSupply.Uptake = 0;
            return dryMatterSupply;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        public virtual BiomassSupplyType CalculateNitrogenSupply()
        { 
            nitrogenSupply.Retranslocation = AvailableNRetranslocation();
            nitrogenSupply.Reallocation = AvailableNReallocation();
            nitrogenSupply.Fixation = 0;
            nitrogenSupply.Uptake = 0;
            return nitrogenSupply;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public virtual BiomassPoolType CalculateDryMatterDemand()
        {
            dryMatterDemand.Structural = DemandedDMStructural();
            dryMatterDemand.Storage = DemandedDMStorage();
            dryMatterDemand.Metabolic = 0;
            return dryMatterDemand;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        public virtual BiomassPoolType CalculateNitrogenDemand()
        {
            double NDeficit = Math.Max(0.0, MaximumNConc.Value() * (Live.Wt + PotentialDMAllocation) - Live.N);
            NDeficit *= NitrogenDemandSwitch.Value();

            nitrogenDemand.Structural = Math.Min(NDeficit, PotentialStructuralDMAllocation * MinimumNConc.Value());
            nitrogenDemand.Metabolic = Math.Min(NDeficit, PotentialStructuralDMAllocation * (CriticalNConc.Value() - MinimumNConc.Value()));
            nitrogenDemand.Storage = Math.Max(0, NDeficit - nitrogenDemand.Structural - nitrogenDemand.Metabolic);
            return nitrogenDemand;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation && StructuralFraction != null)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModel(this, tags, headingLevel, indent);

                // Documment DM demands.
                tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("Total Dry matter demand is calculated by the DMDemandFunction", indent));
                IModel DMDemand = Apsim.Child(this, "DMDemandFunction");
                DMDemand.Document(tags, -1, indent);
                IModel StrucFrac = Apsim.Child(this, "StructuralFraction");
                if (StrucFrac.GetType() == typeof(Constant))
                {
                    if (StructuralFraction.Value() == 1.0)
                    {
                        tags.Add(new AutoDocumentation.Paragraph("All demand is structural and this organ has no Non-structural demand", indent));
                    }
                    else
                    {
                        double StrucPercent = StructuralFraction.Value() * 100;
                        tags.Add(new AutoDocumentation.Paragraph("Of total biomass, " + StrucPercent + "% of this is structural and the remainder is non-structural demand", indent));
                        tags.Add(new AutoDocumentation.Paragraph("Any Non-structural Demand Capacity (StructuralWt/StructuralFraction) that is not currently occupied is also included in Non-structural DM Demand", indent));
                    }
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of total biomass that is partitioned to structural is determined by the StructuralFraction", indent));
                    StrucFrac.Document(tags, -1, indent);
                    tags.Add(new AutoDocumentation.Paragraph("Any Non-structural Demand Capacity (StructuralWt/StructuralFraction) that is not currently occupied is also included in Non-structural DM Demand", indent));
                }

                // Document Nitrogen Demand
                tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The daily structural N demand is the product of Total DM demand and a Minimum N concentration", indent));
                IModel MinN = Apsim.Child(this, "MinimumNConc");
                MinN.Document(tags, -1, indent);
                tags.Add(new AutoDocumentation.Paragraph("The daily Storage N demand is the product of Total DM demand and a Maximum N concentration", indent));
                IModel MaxN = Apsim.Child(this, "MaximumNConc");
                MaxN.Document(tags, -1, indent);
                IModel NDemSwitch = Apsim.Child(this, "NitrogenDemandSwitch");
                if (NDemSwitch.GetType() == typeof(Constant))
                {
                    if (NitrogenDemandSwitch.Value() == 1.0)
                    {
                        //Dont bother docummenting as is does nothing
                    }
                    else
                    {
                        tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor of " + NitrogenDemandSwitch.Value() + " as specified by the NitrogenDemandFactor", indent));
                    }
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandFactor", indent));
                    NDemSwitch.Document(tags, -1, indent);
                }

                //Document DM supplies
                tags.Add(new AutoDocumentation.Heading("Dry Matter Supply", headingLevel + 1));
                IModel DMReallocFac = Apsim.Child(this, "DMReallocationFactor");
                if (DMReallocFac.GetType() == typeof(Constant))
                {
                    if (DMReallocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate DM when senescence of the organ occurs", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + DMReallocationFactor.Value() * 100 + "% of DM that senesces each day", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing DM tha is allocated each day is quantified by the DMReallocationFactor", indent));
                    DMReallocFac.Document(tags, -1, indent);
                }
                IModel DMRetransFac = Apsim.Child(this, "DMRetranslocationFactor");
                if (DMRetransFac.GetType() == typeof(Constant))
                {
                    if (DMRetranslocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural DM", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + DMRetranslocationFactor.Value() * 100 + "% of non-structural DM each day", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural DM tha is allocated each day is quantified by the DMReallocationFactor", indent));
                    DMRetransFac.Document(tags, -1, indent);
                }

                //Document N supplies
                tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
                IModel NReallocFac = Apsim.Child(this, "NReallocationFactor");
                if (NReallocFac.GetType() == typeof(Constant))
                {
                    if (NReallocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + NReallocationFactor.Value() * 100 + "% of N that senesces each day", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N tha is allocated each day is quantified by the NReallocationFactor", indent));
                    NReallocFac.Document(tags, -1, indent);
                }
                IModel NRetransFac = Apsim.Child(this, "NRetranslocationFactor");
                if (NRetransFac.GetType() == typeof(Constant))
                {
                    if (NRetranslocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + NRetranslocationFactor.Value() * 100 + "% of non-structural N each day", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor", indent));
                    NRetransFac.Document(tags, -1, indent);
                }

                //Document Biomass Senescence and Detachment
                tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
                IModel Sen = Apsim.Child(this, "SenescenceRate");
                if (Sen.GetType() == typeof(Constant))
                {
                    if (SenescenceRate.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has senescence parameterised to zero so all biomss in this organ will remain live", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " senesces " + SenescenceRate.Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceFraction", indent));
                    Sen.Document(tags, -1, indent);
                }

                IModel Det = Apsim.Child(this, "DetachmentRateFunction");
                if (Sen.GetType() == typeof(Constant))
                {
                    if (DetachmentRateFunction.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has detachment parameterised to zero so all biomss in this organ will remain with the plant until a defoliation or harvest event occurs", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " detaches " + DetachmentRateFunction.Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction", indent));
                    Det.Document(tags, -1, indent);
                }

                if (biomassRemovalModel != null)
                    biomassRemovalModel.Document(tags, headingLevel + 1, indent);
            }
        }

        /// <summary>Clears this instance.</summary>
        protected virtual void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
            dryMatterSupply.Clear();
            nitrogenSupply.Clear();
            dryMatterDemand.Clear();
            nitrogenDemand.Clear();
            PotentialDMAllocation = 0.0;
            PotentialStructuralDMAllocation = 0.0;
            PotentialMetabolicDMAllocation = 0.0;
            dryMatterDemand.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Clears some variables.</summary>
        protected virtual void DoDailyCleanup()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// 
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            StartLive = new Biomass();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
            Live = new Biomass();
            Dead = new Biomass();
            Clear();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
                DoDailyCleanup();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            Clear();
        }

        /// <summary>Called when crop is emerging</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Event data</param>
        [EventSubscribe("PlantEmerging")]
        protected void OnPlantEmerging(object sender, EventArgs e)
        {
            //Initialise biomass and nitrogen
            Live.StructuralWt = InitialWtFunction.Value();
            Live.StorageWt = 0.0;
            Live.StructuralN = Live.StructuralWt * MinimumNConc.Value();
            Live.StorageN = (InitialWtFunction.Value() * MaximumNConc.Value()) - Live.StructuralN;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (Plant.IsEmerged)
                StartLive = Live;
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
            {
                // Do senescence
                double senescedFrac = SenescenceRate.Value();
                if (Live.Wt * (1.0 - senescedFrac) < BiomassToleranceValue)
                    senescedFrac = 1.0;  // remaining amount too small, senesce all
                Biomass Loss = Live * senescedFrac;
                Live.Subtract(Loss);
                Dead.Add(Loss);
                Senesced.Add(Loss);

                // Do detachment
                double detachedFrac = DetachmentRateFunction.Value();
                if (Dead.Wt * (1.0 - detachedFrac) < BiomassToleranceValue)
                    detachedFrac = 1.0;  // remaining amount too small, detach all
                Biomass detaching = Dead * detachedFrac;
                Dead.Multiply(1.0 - detachedFrac);
                if (detaching.Wt > 0.0)
                {
                    Detached.Add(detaching);
                    SurfaceOrganicMatter.Add(detaching.Wt * 10, detaching.N * 10, 0, Plant.CropType, Name);
                }

                // Do maintenance respiration
                MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value();
                Live.MetabolicWt *= (1 - MaintenanceRespirationFunction.Value());
                MaintenanceRespiration += Live.StorageWt * MaintenanceRespirationFunction.Value();
                Live.StorageWt *= (1 - MaintenanceRespirationFunction.Value());
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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

        /// <summary>Computes the N amount available for retranslocation.</summary>
        private double AvailableNRetranslocation()
        {
            // This is limited to ensure Nconc does not go below MinimumNConc
            double labileN = Math.Max(0.0, StartLive.StorageN - StartLive.StorageWt * MinimumNConc.Value());
            double availableN = Math.Max(0.0, labileN - nitrogenSupply.Reallocation) * NRetranslocationFactor.Value();
            if (availableN < -BiomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            return availableN;
        }

        /// <summary>Computes the N amount available for reallocation.</summary>
        private double AvailableNReallocation()
        {
            double availableN = StartLive.StorageN * SenescenceRate.Value() * NReallocationFactor.Value();
            if (availableN < -BiomassToleranceValue)
                throw new Exception("Negative N reallocation value computed for " + Name);

            return availableN;
        }
    }
}
