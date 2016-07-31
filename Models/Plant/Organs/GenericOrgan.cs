using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.PMF.Organs
{
 
    /// <summary>
    /// Model of generic organ 
    /// </summary>
    /// \param <b>(IFunction, Optional)</b> SenescenceRate Rate of organ senescence 
    ///     (default 0 if this parameter does not exist, i.e no senescence).
    /// \param <b>(IFunction, Optional)</b> StructuralFraction Fraction of organ structural component 
    ///     (default 1 if this parameter does not exist, i.e all biomass is structural).
    /// \param <b>(IFunction, Optional)</b> InitialWtFunction Initial weight of organ 
    ///      (default 0 if this parameter does not exist, i.e. no initial weight).
    /// \param <b>(IFunction, Optional)</b> InitialStructuralFraction Fraction of initial weight of organ 
    ///      (default 1 if this parameter does not exist, i.e. all initial biomass is structural).
    /// \param <b>(IFunction, Optional)</b> NReallocationFactor Factor of nitrogen reallocation  
    ///      (0-1; default 0 if this parameter does not exist, i.e. no nitrogen reallocation).
    /// \param <b>(IFunction, Optional)</b> NRetranslocationFactor Factor of nitrogen retranslocation  
    ///       (0-1; default 0 if this parameter does not exist, i.e. no nitrogen retranslocation).
    /// \param MinimumNConc MinimumNConc Minimum nitrogen concentration
    /// \param MinimumNConc MaximumNConc Maximum nitrogen concentration.
    /// \retval LiveFWt The live fresh weight (g m<sup>-2</sup>)
    /// <remarks>
    /// Biomass demand 
    /// -----------------------
    /// 
    /// Biomass supply
    /// -----------------------
    /// </remarks>
    [Serializable]
    public class GenericOrgan : BaseOrgan, IArbitration
    {

        #region Class Structures
        /// <summary>The start live</summary>
        private Biomass StartLive = new Biomass();
        #endregion

        #region Class Parameter Function Links
        /// <summary>The senescence rate function</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction SenescenceRate = null;
        /// <summary>The detachment rate function</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction DetachmentRateFunction = null;

        /// <summary>The n reallocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction NReallocationFactor = null;
        /// <summary>The n retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction NRetranslocationFactor = null;
        /// <summary>The nitrogen demand switch</summary>
        [Link(IsOptional = true)]
        IFunction NitrogenDemandSwitch = null;
        /// <summary>The dm retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction DMRetranslocationFactor = null;
        /// <summary>The structural fraction</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        IFunction StructuralFraction = null;
        /// <summary>The dm demand Function</summary>
        [Link(IsOptional = true)]
        [Units("g/m2/d")]
        IFunction DMDemandFunction = null;
        /// <summary>The initial wt function</summary>
        [Link(IsOptional = true)]
        [Units("g/m2")]
        IFunction InitialWtFunction = null;
        /// <summary>The initial structural fraction</summary>
        [Units("g/g")]
        [Link(IsOptional = true)]
        IFunction InitialStructuralFraction = null;
        /// <summary>The dry matter content</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        IFunction DryMatterContent = null;
        /// <summary>The maximum n conc</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        public IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Units("g/g")]
        [Link(IsOptional = true)]
        public IFunction MinimumNConc = null;
        /// <summary>The proportion of biomass repired each day</summary>
        [Link(IsOptional = true)]
        public IFunction MaintenanceRespirationFunction = null;
        /// <summary>Dry matter conversion efficiency</summary>
        [Link(IsOptional = true)]
        public IFunction DMConversionEfficiencyFunction = null;
        #endregion

        #region States
        /// <summary>The senescence rate</summary>
        private double mySenescenceRate = 0;
        /// <summary>The _ structural fraction</summary>
        private double _StructuralFraction = 1;
        /// <summary>The start n retranslocation supply</summary>
        private double StartNRetranslocationSupply = 0;
        /// <summary>The start n reallocation supply</summary>
        private double StartNReallocationSupply = 0;
        /// <summary>The potential dm allocation</summary>
        protected double PotentialDMAllocation = 0;
        /// <summary>The potential structural dm allocation</summary>
        protected double PotentialStructuralDMAllocation = 0;
        /// <summary>The potential metabolic dm allocation</summary>
        protected double PotentialMetabolicDMAllocation = 0;
        /// <summary>The structural dm demand</summary>
        protected double StructuralDMDemand = 0;
        /// <summary>The non structural dm demand</summary>
        protected double NonStructuralDMDemand = 0;
        /// <summary>The initial wt</summary>
        protected double InitialWt = 0;
        /// <summary>The initialize stut fraction</summary>
        private double InitStutFraction = 1;

        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            base.Clear();
            mySenescenceRate = 0;
            _StructuralFraction = 1;
            StartNRetranslocationSupply = 0;
            StartNReallocationSupply = 0;
            PotentialDMAllocation = 0;
            PotentialStructuralDMAllocation = 0;
            PotentialMetabolicDMAllocation = 0;
            StructuralDMDemand = 0;
            NonStructuralDMDemand = 0;
            InitialWt = 0;
            InitStutFraction = 1;
            LiveFWt = 0;
        }
        #endregion

        /// <summary>Growth Respiration</summary>
        public double GrowthRespiration { get; set; }


        #region Class properties

        /// <summary>Gets or sets the live f wt.</summary>
        /// <value>The live f wt.</value>
        [XmlIgnore]
        [Units("g/m^2")]
        public double LiveFWt { get; set; }

        #endregion

        #region Organ functions

        #endregion

        #region Arbitrator methods

        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        [Units("g/m^2")]
        public override BiomassPoolType DMDemand
        {
            get
            {
                if (DMDemandFunction != null)
                {
                    StructuralDMDemand = DMDemandFunction.Value * _StructuralFraction;

                    StructuralDMDemand /= DMConversionEfficiency;
                }
                else
                    StructuralDMDemand = 0;
                double MaximumDM = (StartLive.StructuralWt + StructuralDMDemand) * 1 / _StructuralFraction;
                MaximumDM = Math.Min(MaximumDM, 10000); // FIXME-EIT Temporary solution: Cealing value of 10000 g/m2 to ensure that infinite MaximumDM is not reached when 0% goes to structural fraction   
                NonStructuralDMDemand = Math.Max(0.0, MaximumDM - StructuralDMDemand - StartLive.StructuralWt - StartLive.NonStructuralWt);
                NonStructuralDMDemand /= DMConversionEfficiency;

                return new BiomassPoolType { Structural = StructuralDMDemand, NonStructural = NonStructuralDMDemand };
            }
        }
        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        /// <exception cref="System.Exception">Invalid allocation of potential DM in  + Name</exception>
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in " + Name);
                PotentialMetabolicDMAllocation = value.Metabolic;
                PotentialStructuralDMAllocation = value.Structural;
                PotentialDMAllocation = value.Structural + value.Metabolic;
            }
        }

        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override BiomassSupplyType DMSupply
        {
            get
            {
                return new BiomassSupplyType
                {
                    Fixation = 0.0,
                    Retranslocation = AvailableDMRetranslocation(),
                    Reallocation = 0.0
                };
            }
        }

        /// <summary>Gets the amount of DM available for retranslocation</summary>
        /// <returns>DM available to retranslocate</returns>
        public double AvailableDMRetranslocation()
        {
            if (DMRetranslocationFactor != null)
                return StartLive.NonStructuralWt * DMRetranslocationFactor.Value;
            else
            { //Default of 0 means retranslocation is always turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Gets or sets the N demand.</summary>
        /// <value>The N demand.</value>
        public override BiomassPoolType NDemand
        {
            get
            {
                double _NitrogenDemandSwitch = 1;
                if (NitrogenDemandSwitch != null) //Default of 1 means demand is always truned on!!!!
                    _NitrogenDemandSwitch = NitrogenDemandSwitch.Value;
                double NDeficit = Math.Max(0.0, MaximumNConc.Value * (Live.Wt + PotentialDMAllocation) - Live.N);
                NDeficit *= _NitrogenDemandSwitch;
                double StructuralNDemand = Math.Min(NDeficit, PotentialStructuralDMAllocation * MinimumNConc.Value);
                double NonStructuralNDemand = Math.Max(0, NDeficit - StructuralNDemand);
                return new BiomassPoolType { Structural = StructuralNDemand, NonStructural = NonStructuralNDemand };
            }
        }
        /// <summary>Gets or sets the N supply.</summary>
        /// <value>The N supply.</value>
        public override BiomassSupplyType NSupply
        {
            get
            {
                return new BiomassSupplyType()
                {
                    Reallocation = AvailableNReallocation(),
                    Retranslocation = AvailableNRetranslocation(),
                    Uptake = 0.0
                };
            }
        }

        /// <summary>Gets the N amount available for retranslocation</summary>
        /// <returns>N available to retranslocate</returns>
        public double AvailableNRetranslocation()
        {
            if (NRetranslocationFactor != null)
            {
                double LabileN = Math.Max(0, StartLive.NonStructuralN - StartLive.NonStructuralWt * MinimumNConc.Value);
                return (LabileN - StartNReallocationSupply) * NRetranslocationFactor.Value;
            }
            else
            {
                //Default of 0 means retranslocation is always turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Gets the N amount available for reallocation</summary>
        /// <returns>DM available to reallocate</returns>
        public double AvailableNReallocation()
        {
            if (NReallocationFactor != null)
                return mySenescenceRate * StartLive.NonStructuralN * NReallocationFactor.Value;
            else
            { //Default of 0 means reallocation is always turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        /// <exception cref="System.Exception">
        /// -ve NonStructuralDM Allocation to  + Name
        /// or
        /// StructuralDM Allocation to  + Name +  is in excess of its Capacity
        /// or
        /// Retranslocation exceeds nonstructural biomass in organ:  + Name
        /// </exception>
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                GrowthRespiration = 0;
                GrowthRespiration += value.Structural * (1-DMConversionEfficiency)
                                   + value.NonStructural * (1-DMConversionEfficiency);
                
                Live.StructuralWt += Math.Min(value.Structural* DMConversionEfficiency, StructuralDMDemand);
                
                // Excess allocation
                if (value.NonStructural < -0.0000000001)
                    throw new Exception("-ve NonStructuralDM Allocation to " + Name);
                if ((value.NonStructural*DMConversionEfficiency - DMDemand.NonStructural) > 0.0000000001)
                    throw new Exception("Non StructuralDM Allocation to " + Name + " is in excess of its Capacity");
                if (DMDemand.NonStructural > 0)
                    Live.NonStructuralWt += value.NonStructural * DMConversionEfficiency;

                // Retranslocation
                if (value.Retranslocation - StartLive.NonStructuralWt > 0.0000000001)
                    throw new Exception("Retranslocation exceeds nonstructural biomass in organ: " + Name);
                Live.NonStructuralWt -= value.Retranslocation;

                AllocatedWt = value.Structural + value.NonStructural - value.Retranslocation;
            }
        }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// N Retranslocation exceeds nonstructural nitrogen in organ:  + Name
        /// or
        /// -ve N Retranslocation requested from  + Name
        /// or
        /// N Reallocation exceeds nonstructural nitrogen in organ:  + Name
        /// or
        /// -ve N Reallocation requested from  + Name
        /// </exception>
        public override BiomassAllocationType NAllocation
        {
            set
            {
                // Allocation
                if (value.Structural > 0)
                {
                    Live.StructuralN += value.Structural;
                }
                if (value.NonStructural > 0)
                    Live.NonStructuralN += value.NonStructural;

                // Retranslocation
                if (MathUtilities.IsGreaterThan(value.Retranslocation, StartLive.NonStructuralN - StartNRetranslocationSupply))
                    throw new Exception("N Retranslocation exceeds nonstructural nitrogen in organ: " + Name);
                if (value.Retranslocation < -0.000000001)
                    throw new Exception("-ve N Retranslocation requested from " + Name);
                Live.NonStructuralN -= value.Retranslocation;

                // Reallocation
                if (MathUtilities.IsGreaterThan(value.Reallocation, StartLive.NonStructuralN))
                    throw new Exception("N Reallocation exceeds nonstructural nitrogen in organ: " + Name);
                if (value.Reallocation < -0.000000001)
                    throw new Exception("-ve N Reallocation requested from " + Name);
                Live.NonStructuralN -= value.Reallocation;

                AllocatedN = value.Structural + value.NonStructural - value.Retranslocation - value.Reallocation;
            }
        }

        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.</value>
        public override double MaxNconc
        {
            get
            {
                return MaximumNConc.Value;
            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        public override double MinNconc
        {
            get
            {
                return MinimumNConc.Value;
            }
        }
        #endregion

        #region Events and Event Handlers
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// 

        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
                Clear();

            if (DMConversionEfficiencyFunction != null)
                DMConversionEfficiency = DMConversionEfficiencyFunction.Value;
            else
                DMConversionEfficiency = 1.0;

        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                mySenescenceRate = 0;
                if (SenescenceRate != null) //Default of zero means no senescence
                    mySenescenceRate = SenescenceRate.Value;
                _StructuralFraction = 1;
                if (StructuralFraction != null) //Default of 1 means all biomass is structural
                    _StructuralFraction = StructuralFraction.Value;
                InitialWt = 0; //Default of zero means no initial Wt
                if (InitialWtFunction != null)
                    InitialWt = InitialWtFunction.Value;
                InitStutFraction = 1.0; //Default of 1 means all initial DM is structural
                if (InitialStructuralFraction != null)
                    InitStutFraction = InitialStructuralFraction.Value;

                //Initialise biomass and nitrogen
                if (Live.Wt == 0)
                {
                    Live.StructuralWt = InitialWt * InitStutFraction;
                    Live.NonStructuralWt = InitialWt * (1 - InitStutFraction);
                    Live.StructuralN = Live.StructuralWt * MinimumNConc.Value;
                    Live.NonStructuralN = (InitialWt * MaximumNConc.Value) - Live.StructuralN;
                }

                StartLive = Live;
                StartNReallocationSupply = NSupply.Reallocation;
                StartNRetranslocationSupply = NSupply.Retranslocation;
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        protected void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
            {
                Biomass Loss = new Biomass();
                Loss.StructuralWt = Live.StructuralWt * mySenescenceRate;
                Loss.NonStructuralWt = Live.NonStructuralWt * mySenescenceRate;
                Loss.StructuralN = Live.StructuralN * mySenescenceRate;
                Loss.NonStructuralN = Live.NonStructuralN * mySenescenceRate;

                Live.StructuralWt -= Loss.StructuralWt;
                Live.NonStructuralWt -= Loss.NonStructuralWt;
                Live.StructuralN -= Loss.StructuralN;
                Live.NonStructuralN -= Loss.NonStructuralN;

                Dead.StructuralWt += Loss.StructuralWt;
                Dead.NonStructuralWt += Loss.NonStructuralWt;
                Dead.StructuralN += Loss.StructuralN;
                Dead.NonStructuralN += Loss.NonStructuralN;

                SenescedWt = Loss.Wt;
                SenescedN = Loss.N;

                double DetachedFrac = 0;
                if (DetachmentRateFunction != null)
                    DetachedFrac = DetachmentRateFunction.Value;
                if (DetachedFrac > 0.0)
                {
                    double detachingWt = Dead.Wt * DetachedFrac;
                    double detachingN = Dead.N * DetachedFrac;

                    Dead.StructuralWt *= (1 - DetachedFrac);
                    Dead.StructuralN *= (1 - DetachedFrac);
                    Dead.NonStructuralWt *= (1 - DetachedFrac);
                    Dead.NonStructuralN *= (1 - DetachedFrac);
                    Dead.MetabolicWt *= (1 - DetachedFrac);
                    Dead.MetabolicN *= (1 - DetachedFrac);

                    if (detachingWt > 0.0)
                    {
                        DetachedWt += detachingWt;
                        DetachedN += detachingN;
                        SurfaceOrganicMatter.Add(detachingWt * 10, detachingN * 10, 0, Plant.CropType, Name);
                    }
                }

                MaintenanceRespiration = 0;
                //Do Maintenance respiration
                if (MaintenanceRespirationFunction != null)
                {
                    MaintenanceRespiration += Live.MetabolicWt * MaintenanceRespirationFunction.Value;
                    Live.MetabolicWt *= (1 - MaintenanceRespirationFunction.Value);
                    MaintenanceRespiration += Live.NonStructuralWt * MaintenanceRespirationFunction.Value;
                    Live.NonStructuralWt *= (1 - MaintenanceRespirationFunction.Value);
                }

                if (DryMatterContent != null) 
                    LiveFWt = Live.Wt / DryMatterContent.Value;
            }
        }

        #endregion
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            Name = this.Name;
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            tags.Add(new AutoDocumentation.Paragraph(Name + " is parameterised using a generic organ type as follows.", indent));
            
            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            tags.Add(new AutoDocumentation.Heading("Dry Matter Demands", headingLevel + 1));
            if (StructuralFraction != null)
                tags.Add(new AutoDocumentation.Paragraph("Of the organs total DM demand " + StructuralFraction.Value * 100 + "% is structural demand and " + (100 - StructuralFraction.Value * 100) + "is non-structural demand.", indent));
            else
                tags.Add(new AutoDocumentation.Paragraph("100% of the DM demanded from this organ is structural.", indent));

            if (DMDemandFunction != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("The daily DM demand from this organ is calculated using:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "DMDemandFunction")
                        child.Document(tags, headingLevel + 5, indent + 1);
                }
            }

            tags.Add(new AutoDocumentation.Heading("Nitrogen Demands", headingLevel + 1));
            if (MaximumNConc != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("The daily non-structural N demand from " + this.Name + " is the product of Total DM demand and a Maximum N concentration of " + MaximumNConc.Value * 100 + "% less the structural N demand.", indent));
            }
            if (MinimumNConc != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("The daily structural N demand from " + this.Name + " is the product of Total DM demand and a Minimum N concentration of " + MinimumNConc.Value * 100 + "%", indent));
            }
            if (NitrogenDemandSwitch != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("The Nitrogen demand swith is a multiplier applied to nitrogen demand so it can be turned off at certain phases.  For the " + Name + " Organ it is set as:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "NitrogenDemandSwitch")
                        child.Document(tags, headingLevel + 1, indent + 1);
                }
            }
            
            tags.Add(new AutoDocumentation.Heading("Nitrogen Supplies", headingLevel + 1));
            if (NReallocationFactor != null)
                tags.Add(new AutoDocumentation.Paragraph("As the organ senesces " +NReallocationFactor.Value * 100 + "% of senesced N is made available to the arbitrator as NReallocationSupply.", indent));
            else
                tags.Add(new AutoDocumentation.Paragraph("N is not reallocated from " + this.Name + ". ", indent));

            if (NRetranslocationFactor != null)
                tags.Add(new AutoDocumentation.Paragraph(NRetranslocationFactor.Value * 100 + "% of non-structural N is made available to the arbitrator as NRetranslocationSupply.", indent));
            else
                tags.Add(new AutoDocumentation.Paragraph("Non-structural N in " + this.Name + "  is not available for re-translocation to other organs.", indent));

            tags.Add(new AutoDocumentation.Heading("Dry Matter Supplies", headingLevel + 1));
            if (DMRetranslocationFactor != null)
                tags.Add(new AutoDocumentation.Paragraph(DMRetranslocationFactor.Value * 100 + "% of NonStructural DM is made available to the arbitrator as DMReTranslocationSupply.", indent));
            else
                tags.Add(new AutoDocumentation.Paragraph("DM is not retranslocated out of " + this.Name + ". ", indent));

            tags.Add(new AutoDocumentation.Heading("Biomass Senescece and Detachment", headingLevel + 1));
            if (SenescenceRate != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("Senescence is calculated daily as a proportion of the " + Name + "'s live DM and this proportion is calculated as:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "SenescenceRate")
                        child.Document(tags, headingLevel + 1, indent);
                }
            }
            else
                tags.Add(new AutoDocumentation.Paragraph("No senescence occurs from " + Name, indent));

            if (DetachmentRateFunction != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("Detachment of biomass into the surface organic matter pool is calculated daily as a proportion of the " + Name + "'s dead DM and this proportion is calculated as:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "DetachmentRateFunction")
                        child.Document(tags, headingLevel + 1, indent);
                }
            }
            else
                tags.Add(new AutoDocumentation.Paragraph("No Detachment occurs from " + Name, indent));


            // write children.
            bool NonStandardFunctions = false;
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (((child.Name != "StructuralFraction") 
                    | (child.Name != "DMDemandFunction")
                    | (child.Name != "MaximumNConc")
                    | (child.Name != "MinimumNConc")
                    | (child.Name != "NitrogenDemandSwitch") 
                    | (child.Name != "NReallocationFactor") 
                    | (child.Name != "NRetranslocationFactor")
                    | (child.Name != "DMRetranslocationFactor")
                    | (child.Name != "SenescenceRate") 
                    | (child.Name != "DetachmentRateFunctionFunction") 
                    | (child is Biomass))
                    && (child.GetType() != typeof(Memo)))
                {
                    NonStandardFunctions = true;
                }
            }

            if (NonStandardFunctions)
            {
                tags.Add(new AutoDocumentation.Heading("Other functionality", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("In addition to the core processes and parameterisation described above, the " + this.Name + " organ has extra functions which may be referenced by core parameterisation and create additional functionality", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if ((child.Name == "StructuralFraction") 
                        | (child.Name == "DMDemandFunction") 
                        | (child.Name == "MaximumNConc") 
                        | (child.Name == "MinimumNConc") 
                        | (child.Name == "NitrogenDemandSwitch") 
                        | (child.Name == "NReallocationFactor") 
                        | (child.Name == "NRetranslocationFactor") 
                        | (child.Name == "DMRetranslocationFactor") 
                        | (child.Name == "SenescenceRate") 
                        | (child.Name == "DetachmentRateFunctionFunction") 
                        | (child is Biomass) 
                        | (child.GetType() == typeof(Memo)))
                    {//Already documented 
                    }
                    else
                    {
                        //tags.Add(new AutoDocumentation.Heading(child.Name, headingLevel + 2));
                        child.Document(tags, headingLevel + 2, indent + 1);
                    }
                }
            }
        }
    }
}
