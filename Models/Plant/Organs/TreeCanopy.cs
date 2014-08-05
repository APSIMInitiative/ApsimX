using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;

namespace Models.PMF.Organs
{
    [Serializable]
    public class TreeCanopy : GenericOrgan, BelowGround
    {

        private double _WaterAllocation;
        private double EP = 0;
        public double _Height;         // Height of the canopy (mm) 
        public double _LAI;            // Leaf Area Index (Green)
        public double _LAIDead;        // Leaf Area Index (Dead)
        public double _Frgr;           // Relative Growth Rate Factor
        public double K = 0.5;                      // Extinction Coefficient (Green)
        public double KDead = 0;                  // Extinction Coefficient (Dead)
        public double DeltaBiomass = 0;

        public event NewCanopyDelegate New_Canopy;

        [Link]
        Function LAIFunction = null;
        [Link]
        RUEModel Photosynthesis = null;


        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                return CoverGreen * MetData.Radn;
            }
        }
        [Units("mm")]
        public override double WaterDemand { get { return Plant.PotentialEP; } }

        [Units("mm")]
        public double Transpiration { get { return EP; } }
        public override double WaterAllocation
        {
            get { return _WaterAllocation; }
            set
            {
                _WaterAllocation = value;
                EP = EP + _WaterAllocation;
            }
        }
        public double Frgr
        {
            get { return _Frgr; }
            set
            {
                _Frgr = value;
                PublishNewCanopyEvent();
            }
        }
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
        public double Fn
        {
            get { return 1; } //FIXME: Nitrogen stress factor should be implemented in simple leaf.
        }
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
        public double LAIDead
        {
            get { return _LAIDead; }
            set
            {
                _LAIDead = value;
                PublishNewCanopyEvent();
            }
        }
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
        public double CoverGreen
        {
            get
            {
                return 1.0 - Math.Exp(-K * LAI);
            }
        }
        public double CoverTot
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }
        public double CoverDead
        {
            get { return 1.0 - Math.Exp(-KDead * LAIDead); }
        }
        public override void OnSow(SowPlant2Type Data)
        {
            PublishNewCanopyEvent();
        }
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            EP = 0;
        }

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

        public override void DoPotentialDM()
        {
            base.DoPotentialDM();
            if (LAIFunction != null)
                _LAI = LAIFunction.Value;
        }
        public override void DoActualGrowth()
        {
            base.DoActualGrowth();
            
        }
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
