using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;
using Models.PMF.Interfaces;
using Models.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Tree canopy model
    /// </summary>
    [Serializable]
    public class TreeCanopy : GenericOrgan, AboveGround, ICanopy
    {
        #region Canopy interface
        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Albedo.</summary>
        public double Albedo { get { return 0.29366217672824863; } }

        /// <summary>Gets or sets the gsmax.</summary>
        public double Gsmax { get { return 0.01; } }

        /// <summary>Gets or sets the R50.</summary>
        public double R50 { get { return 200; } }

        /// <summary>Gets or sets the lai.</summary>
        public double LAI
        {
            get
            {

                return _LAI;
            }
            set
            {
                _LAI = value;
            }
        }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the cover green.</summary>
        public double CoverGreen
        {
            get
            {
                return 1.0 - Math.Exp(-K * LAI);
            }
        }
        
        /// <summary>Gets the cover tot.</summary>
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }
        
        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height
        {
            get { return _Height; }
            set
            {
                _Height = value;
            }
        }

        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth { get { return Height; } }//  Fixme.  This needs to be replaced with something that give sensible numbers for tree crops

        /// <summary>Gets or sets the FRGR.</summary>
        public double FRGR
        {
            get { return _Frgr; }
            set
            {
                _Frgr = value;
            }
        }

        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        public double PotentialEP { get; set; }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; } 
        
        #endregion

        /// <summary>The _ water allocation</summary>
        private double _WaterAllocation;
        /// <summary>The _ height</summary>
        public double _Height;         // Height of the canopy (mm) 
        /// <summary>The _ lai</summary>
        public double _LAI;            // Leaf Area Index (Green)
        /// <summary>The _ lai dead</summary>
        public double _LAIDead;        // Leaf Area Index (Dead)
        /// <summary>The _ FRGR</summary>
        public double _Frgr;
        /// <summary>The ep</summary>
        private double EP = 0;


        // Relative Growth Rate Factor
        /// <summary>The k</summary>
        public double K = 0.5;                      // Extinction Coefficient (Green)
        /// <summary>The k dead</summary>
        public double KDead = 0;                  // Extinction Coefficient (Dead)
        /// <summary>The delta biomass</summary>
        public double DeltaBiomass = 0;

        /// <summary>The lai function</summary>
        [Link]
        IFunction LAIFunction = null;
        /// <summary>The photosynthesis</summary>
        [Link]
        RUEModel Photosynthesis = null;

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
        /// <summary>Gets or sets the water demand.</summary>
        /// <value>The water demand.</value>
        [Units("mm")]
        public override double WaterDemand { get { return PotentialEP; } }

        /// <summary>Gets the transpiration.</summary>
        /// <value>The transpiration.</value>
        [Units("mm")]
        public double Transpiration { get { return EP; } }
        /// <summary>Gets or sets the water allocation.</summary>
        public override double WaterAllocation
        {
            get { return _WaterAllocation; }
            set
            {
                _WaterAllocation = value;
                EP = EP + _WaterAllocation;
            }
        }

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
            get { return 1; } //FIXME: Nitrogen stress factor should be implemented in simple leaf.
        }

        /// <summary>Gets or sets the lai dead.</summary>
        /// <value>The lai dead.</value>
        public double LAIDead
        {
            get { return _LAIDead; }
            set
            {
                _LAIDead = value;
            }
        }

        /// <summary>Gets the cover dead.</summary>
        /// <value>The cover dead.</value>
        public double CoverDead
        {
            get { return 1.0 - Math.Exp(-KDead * LAIDead); }
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            EP = 0;
        }

        #region Arbitrator methods

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private new void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                if (LAIFunction != null)
                    _LAI = LAIFunction.Value;
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private new void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
                        
        }
        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override BiomassSupplyType DMSupply
        {
            get
            {
                DeltaBiomass = Photosynthesis.Value;
                return new BiomassSupplyType { Fixation = DeltaBiomass, Retranslocation = 0, Reallocation = 0 };
            }
        }
        
        #endregion
    }
}
