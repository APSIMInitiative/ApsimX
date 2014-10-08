using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Tree canopy model
    /// </summary>
    [Serializable]
    public class TreeCanopy : GenericOrgan, BelowGround
    {

        /// <summary>The _ water allocation</summary>
        private double _WaterAllocation;
        /// <summary>The ep</summary>
        private double EP = 0;
        /// <summary>The _ height</summary>
        public double _Height;         // Height of the canopy (mm) 
        /// <summary>The _ lai</summary>
        public double _LAI;            // Leaf Area Index (Green)
        /// <summary>The _ lai dead</summary>
        public double _LAIDead;        // Leaf Area Index (Dead)
        /// <summary>The _ FRGR</summary>
        public double _Frgr;           // Relative Growth Rate Factor
        /// <summary>The k</summary>
        public double K = 0.5;                      // Extinction Coefficient (Green)
        /// <summary>The k dead</summary>
        public double KDead = 0;                  // Extinction Coefficient (Dead)
        /// <summary>The delta biomass</summary>
        public double DeltaBiomass = 0;

        /// <summary>Occurs when [new_ canopy].</summary>
        public event NewCanopyDelegate New_Canopy;

        /// <summary>The lai function</summary>
        [Link]
        Function LAIFunction = null;
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
        public override double WaterDemand { get { return Plant.PotentialEP; } }

        /// <summary>Gets the transpiration.</summary>
        /// <value>The transpiration.</value>
        [Units("mm")]
        public double Transpiration { get { return EP; } }
        /// <summary>Gets or sets the water allocation.</summary>
        /// <value>The water allocation.</value>
        public override double WaterAllocation
        {
            get { return _WaterAllocation; }
            set
            {
                _WaterAllocation = value;
                EP = EP + _WaterAllocation;
            }
        }
        /// <summary>Gets or sets the FRGR.</summary>
        /// <value>The FRGR.</value>
        public double Frgr
        {
            get { return _Frgr; }
            set
            {
                _Frgr = value;
                PublishNewCanopyEvent();
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
        /// <summary>Gets or sets the lai.</summary>
        /// <value>The lai.</value>
        public double LAI
        {
            get
            {

                return _LAI;
            }
            set
            {
                _LAI = value;
                PublishNewCanopyEvent();
            }
        }
        /// <summary>Gets or sets the lai dead.</summary>
        /// <value>The lai dead.</value>
        public double LAIDead
        {
            get { return _LAIDead; }
            set
            {
                _LAIDead = value;
                PublishNewCanopyEvent();
            }
        }
        /// <summary>Gets or sets the height.</summary>
        /// <value>The height.</value>
        [Units("mm")]
        public double Height
        {
            get { return _Height; }
            set
            {
                _Height = value;
                PublishNewCanopyEvent();
            }
        }
        /// <summary>Gets the cover green.</summary>
        /// <value>The cover green.</value>
        public double CoverGreen
        {
            get
            {
                return 1.0 - Math.Exp(-K * LAI);
            }
        }
        /// <summary>Gets the cover tot.</summary>
        /// <value>The cover tot.</value>
        public double CoverTot
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }
        /// <summary>Gets the cover dead.</summary>
        /// <value>The cover dead.</value>
        public double CoverDead
        {
            get { return 1.0 - Math.Exp(-KDead * LAIDead); }
        }
        /// <summary>Called when [sow].</summary>
        /// <param name="Data">The data.</param>
        public override void OnSow(SowPlant2Type Data)
        {
            PublishNewCanopyEvent();
        }
        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            EP = 0;
        }

        /// <summary>Publishes the new canopy event.</summary>
        private void PublishNewCanopyEvent()
        {
            if (New_Canopy != null)
            {
                Plant.LocalCanopyData.sender = Plant.Name;
                Plant.LocalCanopyData.lai = (float)LAI;
                Plant.LocalCanopyData.lai_tot = (float)(LAI + LAIDead);
                Plant.LocalCanopyData.height = (float)Height;
                Plant.LocalCanopyData.depth = (float)Height;
                Plant.LocalCanopyData.cover = (float)CoverGreen;
                Plant.LocalCanopyData.cover_tot = (float)CoverTot;
                New_Canopy.Invoke(Plant.LocalCanopyData);
            }
        }

        #region Arbitrator methods

        /// <summary>Does the potential dm.</summary>
        public override void DoPotentialDM()
        {
            base.DoPotentialDM();
            if (LAIFunction != null)
                _LAI = LAIFunction.Value;
        }
        /// <summary>Does the actual growth.</summary>
        public override void DoActualGrowth()
        {
            base.DoActualGrowth();
            
        }
        /// <summary>Gets or sets the dm supply.</summary>
        /// <value>The dm supply.</value>
        public override BiomassSupplyType DMSupply
        {
            get
            {
                DeltaBiomass = Photosynthesis.Growth(RadIntTot);
                return new BiomassSupplyType { Fixation = DeltaBiomass, Retranslocation = 0, Reallocation = 0 };
            }
        }
        
        #endregion
    }
}
