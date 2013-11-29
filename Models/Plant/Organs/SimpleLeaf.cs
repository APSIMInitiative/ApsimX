using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Functions.SupplyFunctions;

namespace Models.PMF.Organs
{
    public class SimpleLeaf : BaseOrgan, AboveGround
    {
        [Link]
        Plant Plant = null;
        [Link(IsOptional = true)]
        Structure structure = null;
        [Link]
        Summary Summary = null;



        private double _WaterAllocation;
        private double EP = 0;
        private double PEP = 0;
        private double NShortage = 0;   //if an N Shoratge how Much;

        
        public event NewPotentialGrowthDelegate NewPotentialGrowth;
        
        public event NewCanopyDelegate New_Canopy;

        //[Input]
        //public NewMetType MetData = null;

        public double _Height;         // Height of the canopy (mm) 
        public double _LAI;            // Leaf Area Index (Green)
        public double _LAIDead;        // Leaf Area Index (Dead)
        public double _Frgr;           // Relative Growth Rate Factor
        public XYPairs FT { get; set; }    // Temperature effect on Growth Interpolation Set
        public XYPairs FVPD { get; set; }   // VPD effect on Growth Interpolation Set
        public Function PotentialBiomass { get; set; }
        public Function DMDemandFunction { get; set; }
        public Function CoverFunction { get; set; }
        public Function NitrogenDemandSwitch { get; set; }
        public Function NConc { get; set; }
        public Function LaiFunction { get; set; }
        public RUEModel Photosynthesis { get; set; }



        public double K = 0.5;                      // Extinction Coefficient (Green)
        public double KDead = 0;                  // Extinction Coefficient (Dead)
        public double DeltaBiomass = 1;
        public double BiomassYesterday = 0;
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

        public override BiomassSupplyType DMSupply
        {
            get
            {
                if (Photosynthesis != null)
                    DeltaBiomass = Photosynthesis.Growth(RadIntTot);
                return new BiomassSupplyType { Fixation = DeltaBiomass, Retranslocation = 0, Reallocation = 0 };
            }
        }
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                Live.StructuralWt += value.Structural;
            }
        }
        
        [Units("mm")]
        public override double WaterDemand { get { return PEP; } }
        
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
        
        public double Ft
        {
            get
            {
                double Tav = (MetData.MaxT + MetData.MinT) / 2.0;
                return FT.ValueIndexed(Tav);
            }
        }
        
        public double Fvpd
        {
            get
            {
                const double SVPfrac = 0.66;

                double VPDmint = Utility.Met.svp(MetData.MinT) - MetData.vp;
                VPDmint = Math.Max(VPDmint, 0.0);

                double VPDmaxt = Utility.Met.svp(MetData.MaxT) - MetData.vp;
                VPDmaxt = Math.Max(VPDmaxt, 0.0);

                double VPD = SVPfrac * VPDmaxt + (1.0 - SVPfrac) * VPDmint;

                return FVPD.ValueIndexed(VPD);
            }
        }
        
        public double Fw
        {
            get
            {
                double F = 0;
                if (PEP > 0)
                    F = EP / PEP;
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

                if (CoverFunction == null)
                    return 1.0 - Math.Exp(-K * LAI);
                return Math.Min(Math.Max(CoverFunction.Value, 0), 1);
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
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                return CoverGreen * MetData.Radn;
            }
        }
        public override void OnSow(SowPlant2Type Data)
        {
            if (structure != null) //could be optional ?
                structure.Population = Data.Population;
            PublishNewPotentialGrowth();
            PublishNewCanopyEvent();
        }

        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            if (PotentialBiomass != null)
            {
                DeltaBiomass = PotentialBiomass.Value - BiomassYesterday; //Over the defalt DM supply of 1 if there is a photosynthesis function present
                BiomassYesterday = PotentialBiomass.Value;
            }

            EP = 0;
            PublishNewPotentialGrowth();
        }
        [EventSubscribe("Canopy_Water_Balance")]
        private void OnCanopy_Water_Balance(CanopyWaterBalanceType CWB)
        {
            if (Plant.InGround)
            {
                Boolean found = false;
                int i = 0;
                while (!found && (i != CWB.Canopy.Length))
                {
                    if (CWB.Canopy[i].name.ToLower() == Plant.Name.ToLower())
                    {
                        PEP = CWB.Canopy[i].PotentialEp;
                        found = true;
                    }
                    else
                        i++;
                }
            }
        }
        private void PublishNewPotentialGrowth()
        {
            // Send out a NewPotentialGrowthEvent.
            if (NewPotentialGrowth != null)
            {
                NewPotentialGrowthType GrowthType = new NewPotentialGrowthType();
                GrowthType.sender = Plant.Name;
                GrowthType.frgr = (float)Math.Min(Math.Min(Frgr, Fvpd), Ft);
                NewPotentialGrowth.Invoke(GrowthType);
            }
        }
        private void PublishNewCanopyEvent()
        {
            if (New_Canopy != null)
            {
                NewCanopyType Canopy = new NewCanopyType();
                Canopy.sender = Plant.Name;
                Canopy.lai = (float)LAI;
                Canopy.lai_tot = (float)(LAI + LAIDead);
                Canopy.height = (float)Height;
                Canopy.depth = (float)Height;
                Canopy.cover = (float)CoverGreen;
                Canopy.cover_tot = (float)CoverTot;
                New_Canopy.Invoke(Canopy);
            }
        }

        public override void DoPotentialDM()
        {
            if (CoverFunction != null)
                // return _LAI;
                _LAI = (Math.Log(1 - CoverGreen) / -K);
            if (LaiFunction != null)
                _LAI = LaiFunction.Value;
        }
        public override void OnCut()
        {
            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Cutting " + Name + " from " + Plant.Name;
            Summary.WriteMessage(FullPath, "");
            Summary.WriteMessage(FullPath, Title);
            Summary.WriteMessage(FullPath, Indent + new string('-', Title.Length));

            Live.Clear();
            Dead.Clear();
        }


        public override BiomassPoolType NDemand
        {
            get
            {
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
                    NDeficit = Math.Max(0.0, NConc.Value * (Live.Wt + DeltaBiomass) - Live.N);
                return new BiomassPoolType { Structural = NDeficit };
            }
        }

        /*  public override void DoActualGrowth()
          {
              // Need to limiet potential growth if low on N and water

           return;
          }
          */


        public override BiomassAllocationType NAllocation
        {
            set
            {
                if (NDemand.Structural == 0)
                    if (value.Structural == 0) { }//All OK
                    else
                        throw new Exception("Invalid allocation of N");

                if (value.Structural == 0.0)
                { }// do nothing
                else
                {
                    double NSupplyValue = value.Structural;

                    if ((NSupplyValue > 0))
                    {
                        //What do we need to meat demand;
                        double ReqN = NDemand.Structural;

                        if (ReqN == NSupplyValue)
                        {
                            // All OK add and leave
                            NShortage = 0;


                            Live.StructuralN += ReqN;
                            Live.MetabolicN += 0;//Then partition N to Metabolic
                            Live.NonStructuralN += 0;
                            return;

                        }

                        if (NSupplyValue > ReqN)
                            throw new Exception("N allocated to Leaf left over after allocation");

                        //Thorecticaly only option left
                        if (NSupplyValue < ReqN)
                        {
                            NShortage = ReqN - NSupplyValue;
                            Live.StructuralN += NSupplyValue;
                            Live.MetabolicN += 0;//Then partition N to Metabolic
                            Live.NonStructuralN += 0;
                            return;
                        }

                        throw new Exception("UnKnown Leaf N allocation problem");
                    }
                }
            }
        }


    }
}
