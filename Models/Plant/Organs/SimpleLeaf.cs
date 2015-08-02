using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A simple leaf organ
    /// </summary>
    /// \retval LAI Leaf area index for green leaf (\f$\text{LAI}_{g}\f$, \f$m^2 m^{-2}\f$)
    /// \retval LAIDead Leaf area index for dead leaf  (\f$\text{LAI}_{d}\f$, \f$m^2 m^{-2}\f$)
    /// \retval LAITotal Total LAI including live and dead parts (\f$m^2 m^{-2}\f$)
    ///     \f[
    /// /// LAI = \text{LAI}_{g} + \text{LAI}_{d}
    ///     \f]
    /// \retval CoverGreen Cover for green leaf (\f$C_g\f$, unitless). The value of CoverFunction is returned 
    ///     if "CoverFunction" exists in the model. \f$C_g\f$ is calculated according to
    ///     extinction coefficient of green leaf (\f$k_{g}\f$) 
    ///     if "ExtinctionCoefficientFunction" exists in the model.
    ///     \f[
    /// /// C_g = 1-\exp(-k_{g} * \text{LAI}_{g})
    ///     \f]
    ///     where, \f$k\f$ is the extinction coefficient which calculates by "ExtinctionCoefficientFunction"
    /// \retval CoverDead Cover for dead leaf (\f$C_d\f$, unitless). \f$C_d\f$ is calculated according to 
    ///     extinction coefficient of dead leaf (\f$k_{d}\f$). 
    ///     \f[
    /// /// C_d = 1-\exp(-k_{d} * \text{LAI}_{d})
    ///     \f]
    /// <remarks>
    /// </remarks>
    [Serializable]
    public class SimpleLeaf : BaseOrgan, AboveGround, ICanopy
    {
        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI {get; set;}

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the cover green.</summary>
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
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }
        
        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height { get; set; }
        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth { get { return Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Gets or sets the FRGR.</summary>
        [Units("mm")]
        public double FRGR { get; set; }
        
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        public double PotentialEP { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        #endregion


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
        IFunction FRGRFunction = null;   // VPD effect on Growth Interpolation Set
        /// <summary>The potential biomass</summary>
        [Link(IsOptional = true)]
        IFunction PotentialBiomass = null;
        /// <summary>The dm demand function</summary>
        [Link]
        IFunction DMDemandFunction = null;
        /// <summary>The cover function</summary>
        [Link(IsOptional = true)]
        IFunction CoverFunction = null;
        /// <summary>The nitrogen demand switch</summary>
        [Link(IsOptional = true)]
        IFunction NitrogenDemandSwitch = null;
        /// <summary>The n conc</summary>
        [Link]
        IFunction NConc = null;
        /// <summary>The lai function</summary>
        [Link(IsOptional = true)]
        IFunction LAIFunction = null;
        /// <summary>The extinction coefficient function</summary>
        [Link(IsOptional = true)]
        IFunction ExtinctionCoefficientFunction = null;
        /// <summary>The photosynthesis</summary>
        [Link(IsOptional = true)]
        RUEModel Photosynthesis = null;
        /// <summary>The height function</summary>
        [Link(IsOptional = true)]
        IFunction HeightFunction = null;
        /// <summary>The lai dead function</summary>
        [Link(IsOptional = true)]
        IFunction LaiDeadFunction = null;
        /// <summary>The structural fraction</summary>
        [Link(IsOptional = true)]
        IFunction StructuralFraction = null;
        #endregion

        #region States and variables
        /// <summary>The _ water allocation</summary>
        private double _WaterAllocation;
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
                return PotentialEP;
            }
            //set
            //{
            //    Plant.PotentialEP = value;
            //}
        }
        /// <summary>Gets the transpiration.</summary>
        /// <value>The transpiration.</value>
        public double Transpiration { get { return EP; } }

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
            } 
        }

        /// <summary>Gets or sets the lai dead.</summary>
        /// <value>The lai dead.</value>
        public double LAIDead { get; set; }


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

        #region Events

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {

            EP = 0;
        }
        #endregion

        #region Component Process Functions


        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                Summary.WriteMessage(this, "Cutting " + Name + " from " + Plant.Name);
                Live.Clear();
                Dead.Clear();
            }
        }
        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                Clear();

                if (StructuralFraction != null)
                    _StructuralFraction = StructuralFraction.Value;
            }
        }

        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            base.Clear();
            Height = 0;
        }
        #endregion

        #region Top Level time step functions
        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                if (PotentialBiomass != null)
                {
                    DeltaBiomass = PotentialBiomass.Value; //Over the defalt DM supply of 1 if there is a photosynthesis function present
                }

                FRGR = FRGRFunction.Value;
                if (CoverFunction == null & ExtinctionCoefficientFunction == null)
                {
                    throw new Exception("\"CoverFunction\" or \"ExtinctionCoefficientFunction\" should be defined in " + this.Name);
                }
                if (CoverFunction != null)
                    LAI = (Math.Log(1 - CoverGreen) / (ExtinctionCoefficientFunction.Value * -1));
                if (LAIFunction != null)
                    LAI = LAIFunction.Value;

                Height = HeightFunction.Value;

                if (LaiDeadFunction != null)
                    LAIDead = LaiDeadFunction.Value;
                else
                    LAIDead = 0;

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
        }

        #endregion

    }
}
