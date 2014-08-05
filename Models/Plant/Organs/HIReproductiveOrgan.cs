using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.Organs
{
    [Serializable]
    public class HIReproductiveOrgan : BaseOrgan, Reproductive, AboveGround
    {
        [Link]
        Plant Plant = null;

        [Link]
        ISummary Summary = null;

        public Biomass AboveGround { get; set; }

        [Link] Function WaterContent = null;
        [Link] Function HIIncrement = null;
        [Link] Function NConc = null;

        private double DailyGrowth = 0;

        [Units("g/m^2")]
        public double LiveFWt
        {
            get
            {

                if (WaterContent != null)
                    return Live.Wt / (1 - WaterContent.Value);
                else
                    return 0.0;
            }
        }
        
        public event NullTypeDelegate Harvesting;
        public override void OnHarvest()
        {
            Harvesting.Invoke();

            double YieldDW = (Live.Wt + Dead.Wt);

            string message = "Harvesting " + Name + " from " + Plant.Name + "\r\n" +
                             "  Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)";
            Summary.WriteMessage(FullPath, message);

            Live.Clear();
            Dead.Clear();
        }
        
        public double HI
        {
            get
            {
                double CurrentWt = (Live.Wt + Dead.Wt);
                if (AboveGround.Wt > 0)
                    return CurrentWt / AboveGround.Wt;
                else
                    return 0.0;
            }
        }
        public override BiomassPoolType DMDemand
        {
            get
            {
                double CurrentWt = (Live.Wt + Dead.Wt);
                double NewHI = HI + HIIncrement.Value;
                double NewWt = NewHI * AboveGround.Wt;
                double Demand = Math.Max(0.0, NewWt - CurrentWt);

                return new BiomassPoolType { Structural = Demand };
            }
        }
        public override BiomassAllocationType DMAllocation
        {
            set { Live.StructuralWt += value.Structural; DailyGrowth = value.Structural; }
        }
        public override BiomassPoolType NDemand
        {
            get
            {
                double demand = Math.Max(0.0, (NConc.Value * Live.Wt) - Live.N);
                return new BiomassPoolType { Structural = demand };
            }

        }
        public override BiomassAllocationType NAllocation
        {
            set
            {
                Live.StructuralN += value.Structural;
            }
        }
    }
}
