using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A simple leaf organ
    /// </summary>
    [Serializable]
    public class SimpleLeaf : BaseOrgan, AboveGround
    {
        #region Class Links
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;
        #endregion

        #region Parameters
        /// <summary>The FRGR function</summary>
        [Link]
        Function FRGRFunction = null;   // VPD effect on Growth Interpolation Set
        /// <summary>The potential biomass</summary>
        [Link(IsOptional = true)]
        Function PotentialBiomass = null;
        /// <summary>The dm demand function</summary>
        [Link]
        Function DMDemandFunction = null;
        /// <summary>The cover function</summary>
        [Link(IsOptional = true)]
        Function CoverFunction = null;
        /// <summary>The nitrogen demand switch</summary>
        [Link(IsOptional = true)]
        Function NitrogenDemandSwitch = null;
        /// <summary>The n conc</summary>
        [Link]
        Function NConc = null;
        /// <summary>The lai function</summary>
        [Link(IsOptional = true)]
        Function LAIFunction = null;
        /// <summary>The extinction coefficient function</summary>
        [Link(IsOptional = true)]
        Function ExtinctionCoefficientFunction = null;
        /// <summary>The photosynthesis</summary>
        [Link(IsOptional = true)]
        RUEModel Photosynthesis = null;
        /// <summary>The height function</summary>
        [Link(IsOptional = true)]
        Function HeightFunction = null;
        /// <summary>The lai dead function</summary>
        [Link(IsOptional = true)]
        Function LaiDeadFunction = null;
        /// <summary>The structural fraction</summary>
        [Link(IsOptional = true)]
        Function StructuralFraction = null;
        #endregion

        #region States and variables
        /// <summary>The _ water allocation</summary>
        private double _WaterAllocation;
        /// <summary>The n shortage</summary>
        private double NShortage = 0;   //if an N Shoratge how Much;
        /// <summary>The biomass yesterday</summary>
        public double BiomassYesterday = 0;
        /// <summary>The _ structural fraction</summary>
        private double _StructuralFraction = 1;

        /// <summary>Gets or sets the ep.</summary>
        /// <value>The ep.</value>
        private double EP { get; set; }
        /// <summary>Gets or sets the k.</summary>
        /// <value>The k.</value>
        public double K { get; set; }                      // Extinction Coefficient (Green)
        /// <summary>Gets or sets the k dead.</summary>
        /// <value>The k dead.</value>
        public double KDead { get; set; }                  // Extinction Coefficient (Dead)
        /// <summary>Gets or sets the delta biomass.</summary>
        /// <value>The delta biomass.</value>
        public double DeltaBiomass { get; set; }
        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [Units("mm")]
        public override double WaterDemand
        {
            get
            {
                return Plant.PotentialEP;
            }
            set
            {
                Plant.PotentialEP = value;
            }
        }
        /// <summary>Gets the transpiration.</summary>
        /// <value>The transpiration.</value>
        public double Transpiration { get { return EP; } }
        /// <summary>Gets or sets the FRGR.</summary>
        /// <value>The FRGR.</value>
        [Units("mm")]
        public override double FRGR { get; set; }
        /// <summary>Gets the fw.</summary>
        /// <value>The fw.</value>
        public double Fw
        {
            get
            {
                double F = 0;
                if (WaterDemand > 0)
                    F = EP / WaterDemand;
                else
                    F = 1;
                return F;
            }
        }
        /// <summary>Gets the function.</summary>
        /// <value>The function.</value>
        public double Fn
        {
            get
            {
                double MaxNContent = Live.Wt * NConc.Value;
                return Live.N / MaxNContent;
            } //FIXME: Nitrogen stress factor should be implemented in simple leaf.
        }
        /// <summary>Gets or sets the lai.</summary>
        /// <value>The lai.</value>
        public double LAI { get; set; }
        /// <summary>Gets or sets the lai dead.</summary>
        /// <value>The lai dead.</value>
        public double LAIDead { get; set; }
        /// <summary>Gets or sets the height.</summary>
        /// <value>The height.</value>
        [Units("mm")]
        public double Height { get; set; }
        /// <summary>Gets the cover green.</summary>
        /// <value>The cover green.</value>
        public double CoverGreen
        {
            get
            {
                if (CoverFunction == null)
                    return 1.0 - Math.Exp((-1 * ExtinctionCoefficientFunction.Value) * LAI);
                return Math.Min(Math.Max(CoverFunction.Value, 0), 1);
            }
        }
        /// <summary>Gets the cover total.</summary>
        /// <value>The cover total.</value>
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }
        /// <summary>Gets the cover dead.</summary>
        /// <value>The cover dead.</value>
        public double CoverDead
        {
            get { return 1.0 - Math.Exp(-KDead * LAIDead); }
        }
        /// <summary>Gets the RAD int tot.</summary>
        /// <value>The RAD int tot.</value>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                return CoverGreen * MetData.Radn;
            }
        }
        #endregion

        #region Arbitrator Methods
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        public override double WaterAllocation
        {
            get { return _WaterAllocation; }
            set
            {
                _WaterAllocation = value;
                EP += _WaterAllocation;
            }
        }
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
        public override BiomassPoolType DMDemand
        {
            get
            {
                double Demand = 0;
                if (DMDemandFunction != null)
                    Demand = DMDemandFunction.Value;
                else
                    Demand = 1;
                return new BiomassPoolType { Structural = Demand };
            }
        }
        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override BiomassSupplyType DMSupply
        {
            get
            {
                if (Photosynthesis != null)
                    DeltaBiomass = Photosynthesis.Growth(RadIntTot);
                return new BiomassSupplyType { Fixation = DeltaBiomass, Retranslocation = 0, Reallocation = 0 };
            }
        }
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                Live.StructuralWt += value.Structural;
            }
        }
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        public override BiomassPoolType NDemand
        {
            get
            {
                double StructuralDemand = 0;
                double NDeficit = 0;
                if (NitrogenDemandSwitch == null)
                    NDeficit = 0;
                if (NitrogenDemandSwitch != null)
                {
                    if (NitrogenDemandSwitch.Value == 0)
                        NDeficit = 0;
                }

                if (NConc == null)
                    NDeficit = 0;
                else
                {
                    double DMDemandTot = DMDemand.Structural + DMDemand.NonStructural + DMDemand.Metabolic;
                    StructuralDemand = NConc.Value * DMDemandTot * _StructuralFraction;
                    NDeficit = Math.Max(0.0, NConc.Value * (Live.Wt + DMDemandTot) - Live.N) - StructuralDemand;
                } return new BiomassPoolType { Structural = StructuralDemand, NonStructural = NDeficit };
            }
        }

        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
       // public override BiomassPoolType NDemand { get; set; }

        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// Invalid allocation of N
        /// or
        /// N allocated to Leaf left over after allocation
        /// or
        /// UnKnown Leaf N allocation problem
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

            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc.</value>
        public override double MinNconc
        {
            get
            {
                if (StructuralFraction != null)
                    return NConc.Value * StructuralFraction.Value;
                else
                    return NConc.Value;
            }
        }
        #endregion

        #region Evnets
        /// <summary>Occurs when [new canopy].</summary>
        public event NewCanopyDelegate NewCanopy;

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (PotentialBiomass != null)
            {
                //FIXME.  Have changed potential Biomass function to give delta rather than accumulation.  MCSP will need to be altered
                DeltaBiomass = PotentialBiomass.Value; //- BiomassYesterday; //Over the defalt DM supply of 1 if there is a photosynthesis function present
                //BiomassYesterday = PotentialBiomass.Value;
            }

            EP = 0;
        }
        #endregion

        #region Component Process Functions
        /// <summary>Publishes the new canopy event.</summary>
        protected virtual void PublishNewCanopyEvent()
        {
            if (NewCanopy != null)
            {
                Plant.LocalCanopyData.sender = Plant.Name;
                Plant.LocalCanopyData.lai = (float)LAI;
                Plant.LocalCanopyData.lai_tot = (float)(LAI + LAIDead);
                Plant.LocalCanopyData.height = (float)Height;
                Plant.LocalCanopyData.depth = (float)Height;
                Plant.LocalCanopyData.cover = (float)CoverGreen;
                Plant.LocalCanopyData.cover_tot = (float)CoverTotal;
                NewCanopy.Invoke(Plant.LocalCanopyData);
            }
        }
        /// <summary>Called when [cut].</summary>
        public override void OnCut()
        {
            Summary.WriteMessage(this, "Cutting " + Name + " from " + Plant.Name);
            Live.Clear();
            Dead.Clear();
        }
        /// <summary>Called when [sow].</summary>
        /// <param name="Data">The data.</param>
        public override void OnSow(SowPlant2Type Data)
        {
            if (StructuralFraction != null)
                _StructuralFraction = StructuralFraction.Value;

            PublishNewCanopyEvent();
        }
        #endregion

        #region Top Level time step functions
        /// <summary>Does the potential dm.</summary>
        public override void DoPotentialDM()
        {
            FRGR = FRGRFunction.Value;
            if (CoverFunction != null)
                LAI = (Math.Log(1 - CoverGreen) / (ExtinctionCoefficientFunction.Value * -1));
            if (LAIFunction != null)
                LAI = LAIFunction.Value;

            Height = HeightFunction.Value;

            if (LaiDeadFunction != null)
                LAIDead = LaiDeadFunction.Value;
            else
                LAIDead = 0;
            PublishNewCanopyEvent();

            /*/Set N Demand
            double StructuralDemand = 0;
            double NDeficit = 0;
            if (NitrogenDemandSwitch == null)
                NDeficit = 0;
            if (NitrogenDemandSwitch != null)
            {
                if (NitrogenDemandSwitch.Value == 0)
                    NDeficit = 0;
            }

            if (NConc == null)
                NDeficit = 0;
            else
            {
                StructuralDemand = NConc.Value * DeltaBiomass * _StructuralFraction;
                NDeficit = Math.Max(0.0, NConc.Value * (Live.Wt + DMDemand.Structural + DMDemand.NonStructural + DMDemand.Metabolic) - Live.N) - StructuralDemand;
            } //return new BiomassPoolType { Structural = StructuralDemand, NonStructural = NDeficit };
            NDemand = new BiomassPoolType();           
            NDemand.Structural = StructuralDemand;
            NDemand.NonStructural = NDeficit;*/
        }
        #endregion

    }
}
