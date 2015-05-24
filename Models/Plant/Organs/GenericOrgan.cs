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
     /*! 
        <summary>
        Model of generic organ 
        </summary>
        \param <b>(IFunction, Optional)</b> SenescenceRateFunction Rate of organ senescence 
            (default 0 if this parameter does not exist, i.e no senescence).
        \param <b>(IFunction, Optional)</b> StructuralFraction Fraction of organ structural component 
            (default 1 if this parameter does not exist, i.e all biomass is structural).
        \param <b>(IFunction, Optional)</b> InitialWtFunction Initial weight of organ 
            (default 0 if this parameter does not exist, i.e. no initial weight).
        \param <b>(IFunction, Optional)</b> InitialStructuralFraction Fraction of initial weight of organ 
            (default 1 if this parameter does not exist, i.e. all initial biomass is structural).
        \param <b>(IFunction, Optional)</b> NReallocationFactor Factor of nitrogen reallocation  
            (0-1; default 0 if this parameter does not exist, i.e. no nitrogen reallocation).
        \param <b>(IFunction, Optional)</b> NRetranslocationFactor Factor of nitrogen retranslocation  
            (0-1; default 0 if this parameter does not exist, i.e. no nitrogen retranslocation).
        \param MinimumNConc MinimumNConc Minimum nitrogen concentration
        \param MinimumNConc MaximumNConc Maximum nitrogen concentration.
        \retval LiveFWt The live fresh weight (g m<sup>-2</sup>)
        <remarks>
        </remarks>
    */


    /// <summary>
    /// Model of generic organ
    /// </summary>
    [Serializable]
    public class GenericOrgan : BaseOrgan, IArbitration
    {
        #region Class Dependency Links and Structures
        /// <summary>The plant</summary>
        [Link]
        protected Plant Plant = null;

        [Link]
        ISurfaceOrganicMatter SurfaceOrganicMatter = null;
        #endregion

        #region Class Structures
        /// <summary>The start live</summary>
        private Biomass StartLive = new Biomass();
        #endregion

        #region Class Parameter Function Links
        /// <summary>The senescence rate function</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction SenescenceRateFunction = null;
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
        IFunction MaximumNConc = null;
        /// <summary>The minimum n conc</summary>
        [Units("g/g")]
        [Link(IsOptional = true)]
        IFunction MinimumNConc = null;
        #endregion

        #region States
        /// <summary>The senescence rate</summary>
        private double SenescenceRate = 0;
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
            SenescenceRate = 0;
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
                StructuralDMDemand = DMDemandFunction.Value * _StructuralFraction;
                double MaximumDM = (StartLive.StructuralWt + StructuralDMDemand) * 1 / _StructuralFraction;
                MaximumDM = Math.Min(MaximumDM, 10000); // FIXME-EIT Temporary solution: Cealing value of 10000 g/m2 to ensure that infinite MaximumDM is not reached when 0% goes to structural fraction   
                NonStructuralDMDemand = Math.Max(0.0, MaximumDM - StructuralDMDemand - StartLive.StructuralWt - StartLive.NonStructuralWt);
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
                double _DMRetranslocationFactor = 0;
                if (DMRetranslocationFactor != null) //Default of 0 means retranslocation is always truned off!!!!
                    _DMRetranslocationFactor = DMRetranslocationFactor.Value;
                return new BiomassSupplyType
                {
                    Fixation = 0,
                    Retranslocation = StartLive.NonStructuralWt * _DMRetranslocationFactor,
                    Reallocation = 0
                };
            }
        }
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
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
        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        public override BiomassSupplyType NSupply
        {
            get
            {
                BiomassSupplyType Supply = new BiomassSupplyType();

                // Calculate Reallocation Supply.
                double _NReallocationFactor = 0;
                if (NReallocationFactor != null) //Default of zero means N reallocation is truned off
                    _NReallocationFactor = NReallocationFactor.Value;
                Supply.Reallocation = SenescenceRate * StartLive.NonStructuralN * _NReallocationFactor;

                // Calculate Retranslocation Supply.
                double _NRetranslocationFactor = 0;
                if (NRetranslocationFactor != null) //Default of zero means retranslocation is turned off
                    _NRetranslocationFactor = NRetranslocationFactor.Value;
                double LabileN = Math.Max(0, StartLive.NonStructuralN - StartLive.NonStructuralWt * MinimumNConc.Value);
                Supply.Retranslocation = (LabileN - StartNReallocationSupply) * _NRetranslocationFactor;

                return Supply;
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
                Live.StructuralWt += Math.Min(value.Structural, StructuralDMDemand);

                // Excess allocation
                if (value.NonStructural < -0.0000000001)
                    throw new Exception("-ve NonStructuralDM Allocation to " + Name);
                if ((value.NonStructural - DMDemand.NonStructural) > 0.0000000001)
                    throw new Exception("StructuralDM Allocation to " + Name + " is in excess of its Capacity");
                if (DMDemand.NonStructural > 0)
                    Live.NonStructuralWt += value.NonStructural;

                // Retranslocation
                if (value.Retranslocation - StartLive.NonStructuralWt > 0.0000000001)
                    throw new Exception("Retranslocation exceeds nonstructural biomass in organ: " + Name);
                Live.NonStructuralWt -= value.Retranslocation;
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
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        protected void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                SenescenceRate = 0;
                if (SenescenceRateFunction != null) //Default of zero means no senescence
                    SenescenceRate = SenescenceRateFunction.Value;
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
            Biomass Loss = new Biomass();
            Loss.StructuralWt = Live.StructuralWt * SenescenceRate;
            Loss.NonStructuralWt = Live.NonStructuralWt * SenescenceRate;
            Loss.StructuralN = Live.StructuralN * SenescenceRate;
            Loss.NonStructuralN = Live.NonStructuralN * SenescenceRate;

            Live.StructuralWt -= Loss.StructuralWt;
            Live.NonStructuralWt -= Loss.NonStructuralWt;
            Live.StructuralN -= Loss.StructuralN;
            Live.NonStructuralN -= Loss.NonStructuralN;

            Dead.StructuralWt += Loss.StructuralWt;
            Dead.NonStructuralWt += Loss.NonStructuralWt;
            Dead.StructuralN += Loss.StructuralN;
            Dead.NonStructuralN += Loss.NonStructuralN;

            double DetachedFrac = 0;
            if (DetachmentRateFunction != null)
                DetachedFrac = DetachmentRateFunction.Value;
            if (DetachedFrac > 0.0)
            {
                double DetachedWt = Dead.Wt * DetachedFrac;
                double DetachedN = Dead.N * DetachedFrac;

                Dead.StructuralWt *= (1 - DetachedFrac);
                Dead.StructuralN *= (1 - DetachedFrac);
                Dead.NonStructuralWt *= (1 - DetachedFrac);
                Dead.NonStructuralN *= (1 - DetachedFrac);
                Dead.MetabolicWt *= (1 - DetachedFrac);
                Dead.MetabolicN *= (1 - DetachedFrac);

                if (DetachedWt > 0)
                    SurfaceOrganicMatter.Add(DetachedWt * 10, DetachedN * 10, 0, Plant.CropType, Name);
            }

            if ((DryMatterContent != null) && (Live.Wt != 0))
                LiveFWt = Live.Wt / DryMatterContent.Value;
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                if (TotalDM > 0)
                    SurfaceOrganicMatter.Add(TotalDM * 10, TotalN * 10, 0, Plant.CropType, Name);
                Clear();
            }
        }
        #endregion
    }
}
