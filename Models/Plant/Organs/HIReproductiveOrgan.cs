using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A harvest index reproductive organ
    /// </summary>
    [Serializable]
    public class HIReproductiveOrgan : BaseOrgan, Reproductive, AboveGround
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>Gets or sets the above ground.</summary>
        /// <value>The above ground.</value>
        public Biomass AboveGround { get; set; }

        /// <summary>The water content</summary>
        [Link] Function WaterContent = null;
        /// <summary>The hi increment</summary>
        [Link] Function HIIncrement = null;
        /// <summary>The n conc</summary>
        [Link] Function NConc = null;

        /// <summary>The daily growth</summary>
        private double DailyGrowth = 0;

        /// <summary>Gets the live f wt.</summary>
        /// <value>The live f wt.</value>
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

        /// <summary>Occurs when [harvesting].</summary>
        public event NullTypeDelegate Harvesting;
        /// <summary>Called when [harvest].</summary>
        public override void OnHarvest()
        {
            Harvesting.Invoke();

            double YieldDW = (Live.Wt + Dead.Wt);

            string message = "Harvesting " + Name + " from " + Plant.Name + "\r\n" +
                             "  Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)";
            Summary.WriteMessage(this, message);

            Live.Clear();
            Dead.Clear();
        }

        /// <summary>Gets the hi.</summary>
        /// <value>The hi.</value>
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
        /// <summary>Gets or sets the dm demand.</summary>
        /// <value>The dm demand.</value>
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
        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        public override BiomassAllocationType DMAllocation
        {
            set { Live.StructuralWt += value.Structural; DailyGrowth = value.Structural; }
        }
        /// <summary>Gets or sets the n demand.</summary>
        /// <value>The n demand.</value>
        public override BiomassPoolType NDemand
        {
            get
            {
                double demand = Math.Max(0.0, (NConc.Value * Live.Wt) - Live.N);
                return new BiomassPoolType { Structural = demand };
            }

        }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        public override BiomassAllocationType NAllocation
        {
            set
            {
                Live.StructuralN += value.Structural;
            }
        }
    }
}
