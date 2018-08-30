namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.Functions;
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
    public class GenericOrgan : Model, IOrgan, IArbitration, ICustomDocumentation, IRemovableBiomass
    {
        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001; 

        /// <summary>The parent plant</summary>
        [Link]
        private Plant parentPlant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The senescence rate function</summary>
        [ChildLinkByName]
        [Units("/d")]
        protected IFunction senescenceRate = null;

        /// <summary>The detachment rate function</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction detachmentRateFunction = null;

        /// <summary>The N retranslocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        protected IFunction nRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        protected IFunction nReallocationFactor = null;

        /// <summary>The nitrogen demand switch</summary>
        [ChildLinkByName]
        private IFunction nitrogenDemandSwitch = null;

        /// <summary>The DM retranslocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction dmRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>The DM structural fraction</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction structuralFraction = null;

        /// <summary>The DM demand function</summary>
        [ChildLinkByName]
        [Units("g/m2/d")]
        private IFunction dmDemandFunction = null;

        /// <summary>The initial biomass dry matter weight</summary>
        [ChildLinkByName]
        [Units("g/m2")]
        private IFunction initialWtFunction = null;

        /// <summary>The maximum N concentration</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction maximumNConc = null;

        /// <summary>The minimum N concentration</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction minimumNConc = null;

        /// <summary>The critical N concentration</summary>
        [ChildLinkByName]
        [Units("g/g")]
        private IFunction criticalNConc = null;

        /// <summary>The proportion of biomass respired each day</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction maintenanceRespirationFunction = null;

        /// <summary>Dry matter conversion efficiency</summary>
        [ChildLinkByName]
        [Units("/d")]
        private IFunction dmConversionEfficiency = null;

        /// <summary>The cost for remobilisation</summary>
        [ChildLinkByName]
        [Units("")]
        private IFunction remobilisationCost = null;

        /// <summary>Carbon concentration</summary>
        /// [Units("-")]
        [Link]
        IFunction CarbonConcentration = null;


        /// <summary>The live biomass state at start of the computation round</summary>
        protected Biomass startLive = null;

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get;  set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get;  set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get;  set; }

        /// <summary>The dry matter potentially being allocated</summary>
        private BiomassPoolType potentialDMAllocation = new BiomassPoolType();

        /// <summary>The potential DM allocation</summary>
        private double potentialDMAllocating = 0.0;

        /// <summary>The potential structural DM allocation</summary>
        private double potentialStructuralDMAllocation = 0.0;

        /// <summary>The potential metabolic DM allocation</summary>
        private double potentialMetabolicDMAllocation = 0.0;

        /// <summary>Constructor</summary>
        public GenericOrgan()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>The live biomass</summary>
        [XmlIgnore]
        public Biomass Live { get; private set; }

        /// <summary>The dead biomass</summary>
        [XmlIgnore]
        public Biomass Dead { get; private set; }

        /// <summary>Gets the total biomass</summary>
        [XmlIgnore]
        public Biomass Total { get { return Live + Dead; } }

        
        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [XmlIgnore]
        public Biomass Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [XmlIgnore]
        public Biomass Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [XmlIgnore]
        public Biomass Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [XmlIgnore]
        public Biomass Removed { get; private set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        [XmlIgnore]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Growth Respiration</summary>
        [XmlIgnore]
        public double GrowthRespiration { get; private set; }

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation { get { return potentialDMAllocation; } }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public virtual double NFixationCost { get { return 0; } }

        /// <summary>Gets the maximum N concentration.</summary>
        [XmlIgnore]
        public double MaxNconc { get { return maximumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [XmlIgnore]
        public double MinNconc { get { return minimumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [XmlIgnore]
        public double CritNconc { get { return criticalNConc.Value(); } }

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
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public virtual void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }

        /// <summary>Computes the amount of structural DM demanded.</summary>
        public double DemandedDMStructural()
        {
            if (dmConversionEfficiency.Value() > 0.0)
            {
                double demandedDM = dmDemandFunction.Value() * structuralFraction.Value() / dmConversionEfficiency.Value();
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
            if (dmConversionEfficiency.Value() > 0.0)
            {
                double theoreticalMaximumDM = MathUtilities.Divide(startLive.StructuralWt + DMDemand.Structural, structuralFraction.Value(), 0);
                double baseAllocated = startLive.StructuralWt + startLive.StorageWt + DMDemand.Structural;
                double demandedDM = MathUtilities.Divide(Math.Max(0.0, theoreticalMaximumDM - baseAllocated), dmConversionEfficiency.Value(), 0);
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
            double availableDM = Math.Max(0.0, startLive.StorageWt - DMSupply.Reallocation) * dmRetranslocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM retranslocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            double availableDM = startLive.StorageWt * senescenceRate.Value() * dmReallocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        protected virtual void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.Reallocation = AvailableDMReallocation();
            DMSupply.Retranslocation = AvailableDMRetranslocation();         
            DMSupply.Fixation = 0;
            DMSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        protected virtual void SetNSupply(object sender, EventArgs e)
        {
            NSupply.Reallocation = Math.Max(0, (startLive.StorageN + startLive.MetabolicN) * senescenceRate.Value() * nReallocationFactor.Value());
            if (NSupply.Reallocation < -BiomassToleranceValue)
                throw new Exception("Negative N reallocation value computed for " + Name);

            NSupply.Retranslocation = Math.Max(0, (startLive.StorageN + startLive.MetabolicN) * (1 - senescenceRate.Value()) * nRetranslocationFactor.Value());
            if (NSupply.Retranslocation < -BiomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        protected virtual void SetDMDemand(object sender, EventArgs e)
        {
            DMDemand.Structural = DemandedDMStructural() + remobilisationCost.Value();
            DMDemand.Storage = DemandedDMStorage();
            DMDemand.Metabolic = 0;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        protected virtual void SetNDemand(object sender, EventArgs e)
        {
            double NDeficit = Math.Max(0.0, maximumNConc.Value() * (Live.Wt + potentialDMAllocating) - Live.N);
            NDeficit *= nitrogenDemandSwitch.Value();

            NDemand.Structural = Math.Min(NDeficit, potentialStructuralDMAllocation * minimumNConc.Value());
            NDemand.Metabolic = Math.Min(NDeficit, potentialStructuralDMAllocation * (criticalNConc.Value() - minimumNConc.Value()));
            NDemand.Storage = Math.Max(0, NDeficit - NDemand.Structural - NDemand.Metabolic);
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialMetabolicDMAllocation = dryMatter.Metabolic;
            potentialStructuralDMAllocation = dryMatter.Structural;
            potentialDMAllocating = dryMatter.Structural + dryMatter.Metabolic;
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Sets the dry matter allocation.</summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public virtual void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // Check retranslocation
            if (dryMatter.Retranslocation - startLive.StorageWt > BiomassToleranceValue)
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            // get DM lost by respiration (growth respiration)
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double growthRespFactor = ((1.0 / dmConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * CarbonConcentration.Value()) * 44.0 / 12.0;

            GrowthRespiration = 0.0;
            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural * dmConversionEfficiency.Value(), DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            GrowthRespiration += Allocated.StructuralWt * growthRespFactor;

            // allocate non structural DM
            if ((dryMatter.Storage * dmConversionEfficiency.Value() - DMDemand.Storage) > BiomassToleranceValue)
                throw new Exception("Non structural DM allocation to " + Name + " is in excess of its capacity");
            // Allocated.StorageWt = dryMatter.Storage * dmConversionEfficiency.Value();
            double diffWt = dryMatter.Storage - dryMatter.Retranslocation;
            if (diffWt > 0)
            {
                diffWt *= dmConversionEfficiency.Value();
                GrowthRespiration += diffWt * growthRespFactor;
            }
            Allocated.StorageWt = diffWt;
            Live.StorageWt += diffWt;
            // allocate metabolic DM
            Allocated.MetabolicWt = dryMatter.Metabolic * dmConversionEfficiency.Value();
            GrowthRespiration += Allocated.MetabolicWt * growthRespFactor;

        }

        /// <summary>Sets the n allocation.</summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public virtual void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            // Retranslocation
            if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, startLive.StorageN + startLive.MetabolicN - NSupply.Retranslocation))
                throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double StorageNRetranslocation = Math.Min(nitrogen.Retranslocation,startLive.StorageN * (1 - senescenceRate.Value()) * nRetranslocationFactor.Value());
            Live.StorageN -= StorageNRetranslocation;
            Live.MetabolicN -= (nitrogen.Retranslocation - StorageNRetranslocation);
            Allocated.StorageN -= nitrogen.Retranslocation;

            // Reallocation
            if (MathUtilities.IsGreaterThan(nitrogen.Reallocation, startLive.StorageN + startLive.MetabolicN))
                throw new Exception("N reallocation exceeds storage + metabolic nitrogen in organ: " + Name);
            double StorageNReallocation = Math.Min(nitrogen.Reallocation, startLive.StorageN * senescenceRate.Value() * nReallocationFactor.Value());
            Live.StorageN -= StorageNReallocation;
            Live.MetabolicN -= (nitrogen.Reallocation - StorageNReallocation);
            Allocated.StorageN -= nitrogen.Reallocation;
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


        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation && structuralFraction != null)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // Documment DM demands.
                tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("Total Dry matter demand is calculated by the DMDemandFunction.", indent));
                IModel DMDemand = Apsim.Child(this, "DMDemandFunction");
                AutoDocumentation.DocumentModel(DMDemand, tags, headingLevel+1, indent);
                IModel StrucFrac = Apsim.Child(this, "StructuralFraction");
                if (StrucFrac.GetType() == typeof(Constant))
                {
                    if (structuralFraction.Value() == 1.0)
                    {
                        tags.Add(new AutoDocumentation.Paragraph("All demand is structural and this organ has no Non-structural demand.", indent));
                    }
                    else
                    {
                        double StrucPercent = structuralFraction.Value() * 100;
                        tags.Add(new AutoDocumentation.Paragraph("Of total biomass, " + StrucPercent + "% of this is structural and the remainder is non-structural demand", indent));
                        tags.Add(new AutoDocumentation.Paragraph("Any Non-structural Demand Capacity (StructuralWt/StructuralFraction) that is not currently occupied is also included in Non-structural DM Demand", indent));
                    }
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of total biomass that is partitioned to structural is determined by the StructuralFraction", indent));
                    AutoDocumentation.DocumentModel(StrucFrac, tags, headingLevel + 1, indent);
                    tags.Add(new AutoDocumentation.Paragraph("Any Non-structural Demand Capacity (StructuralWt/StructuralFraction) that is not currently occupied is also included in Non-structural DM Demand", indent));
                }

                // Document Nitrogen Demand
                tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The daily structural N demand is the product of Total DM demand and a Minimum N concentration", indent));
                IModel MinN = Apsim.Child(this, "MinimumNConc");
                AutoDocumentation.DocumentModel(MinN, tags, headingLevel + 1, indent);
                tags.Add(new AutoDocumentation.Paragraph("The daily Storage N demand is the product of Total DM demand and a Maximum N concentration.", indent));
                IModel MaxN = Apsim.Child(this, "MaximumNConc");
                AutoDocumentation.DocumentModel(MaxN, tags, headingLevel + 1, indent);
                IModel NDemSwitch = Apsim.Child(this, "NitrogenDemandSwitch");
                if (NDemSwitch.GetType() == typeof(Constant))
                {
                    if (nitrogenDemandSwitch.Value() == 1.0)
                    {
                        //Dont bother docummenting as is does nothing
                    }
                    else
                    {
                        tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor of " + nitrogenDemandSwitch.Value() + " as specified by the NitrogenDemandFactor", indent));
                    }
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandFactor.", indent));
                    AutoDocumentation.DocumentModel(NDemSwitch, tags, headingLevel + 1, indent);
                }

                //Document DM supplies
                tags.Add(new AutoDocumentation.Heading("Dry Matter Supply", headingLevel + 1));
                IModel DMReallocFac = Apsim.Child(this, "DMReallocationFactor");
                if (DMReallocFac.GetType() == typeof(Constant))
                {
                    if (dmReallocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate DM when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + dmReallocationFactor.Value() * 100 + "% of DM that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing DM tha is allocated each day is quantified by the DMReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(DMReallocFac, tags, headingLevel + 1, indent);
                }
                IModel DMRetransFac = Apsim.Child(this, "DMRetranslocationFactor");
                if (DMRetransFac.GetType() == typeof(Constant))
                {
                    if (dmRetranslocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural DM.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + dmRetranslocationFactor.Value() * 100 + "% of non-structural DM each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural DM tha is allocated each day is quantified by the DMReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(DMRetransFac, tags, headingLevel + 1, indent);
                }

                //Document N supplies
                tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
                IModel NReallocFac = Apsim.Child(this, "NReallocationFactor");
                if (NReallocFac.GetType() == typeof(Constant))
                {
                    if (nReallocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + nReallocationFactor.Value() * 100 + "% of N that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N tha is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NReallocFac, tags, headingLevel + 1, indent);
                }
                IModel NRetransFac = Apsim.Child(this, "NRetranslocationFactor");
                if (NRetransFac.GetType() == typeof(Constant))
                {
                    if (nRetranslocationFactor.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + nRetranslocationFactor.Value() * 100 + "% of non-structural N each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NRetransFac, tags, headingLevel + 1, indent);
                }

                //Document Biomass Senescence and Detachment
                tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
                IModel Sen = Apsim.Child(this, "SenescenceRate");
                if (Sen.GetType() == typeof(Constant))
                {
                    if (senescenceRate.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has senescence parameterised to zero so all biomss in this organ will remain live.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " senesces " + senescenceRate.Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceFraction.", indent));
                    AutoDocumentation.DocumentModel(Sen, tags, headingLevel + 1, indent);
                }

                IModel Det = Apsim.Child(this, "DetachmentRateFunction");
                if (Sen.GetType() == typeof(Constant))
                {
                    if (detachmentRateFunction.Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has detachment parameterised to zero so all biomss in this organ will remain with the plant until a defoliation or harvest event occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " detaches " + detachmentRateFunction.Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.", indent));
                    AutoDocumentation.DocumentModel(Det, tags, headingLevel + 1, indent);
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
            DMSupply.Clear();
            NSupply.Clear();
            DMDemand.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
            potentialDMAllocating = 0.0;
            potentialStructuralDMAllocation = 0.0;
            potentialMetabolicDMAllocation = 0.0;
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
            NDemand = new BiomassPoolType();
            DMDemand = new BiomassPoolType();
            NSupply = new BiomassSupplyType();
            DMSupply = new BiomassSupplyType();
            startLive = new Biomass();
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
            if (parentPlant.IsAlive)
            {
                Allocated.Clear();
                Senesced.Clear();
                Detached.Clear();
                Removed.Clear();
            }
                    }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == parentPlant)
            {
                Clear();
                Live.StructuralWt = initialWtFunction.Value();
                Live.StorageWt = 0.0;
                Live.StructuralN = Live.StructuralWt * minimumNConc.Value();
                Live.StorageN = (initialWtFunction.Value() * maximumNConc.Value()) - Live.StructuralN;
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected virtual void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (parentPlant.IsEmerged)
                startLive = Live;
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                // Do senescence
                double senescedFrac = senescenceRate.Value();
                if (Live.Wt * (1.0 - senescedFrac) < BiomassToleranceValue)
                    senescedFrac = 1.0;  // remaining amount too small, senesce all
                Biomass Loss = Live * senescedFrac;
                Live.Subtract(Loss);
                Dead.Add(Loss);
                Senesced.Add(Loss);

                // Do detachment
                double detachedFrac = detachmentRateFunction.Value();
                if (Dead.Wt * (1.0 - detachedFrac) < BiomassToleranceValue)
                    detachedFrac = 1.0;  // remaining amount too small, detach all
                Biomass detaching = Dead * detachedFrac;
                Dead.Multiply(1.0 - detachedFrac);
                if (detaching.Wt > 0.0)
                {
                    Detached.Add(detaching);
                    surfaceOrganicMatter.Add(detaching.Wt * 10, detaching.N * 10, 0, parentPlant.CropType, Name);
                }

                // Do maintenance respiration
                MaintenanceRespiration = 0;
                MaintenanceRespiration += Live.MetabolicWt * maintenanceRespirationFunction.Value();
                MaintenanceRespiration += Live.StorageWt * maintenanceRespirationFunction.Value();
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
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, parentPlant.CropType, Name);
            }

            Clear();
        }
    }
}
