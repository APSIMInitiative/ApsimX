using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.PMF.Phen;

namespace Models.PMF.Organs
{
    [Serializable]
    public class ReproductiveOrgan : BaseOrgan, Reproductive, AboveGround
    {
        [Link]
        ISummary Summary = null;

        #region Parameter Input Classes
        [Link]
        protected Plant Plant = null;
        [Link]
        protected Phenology Phenology = null;
        [Link] Function WaterContent = null;
        [Link] Function FillingRate = null;
        [Link] Function NumberFunction = null;
        [Link] Function NFillingRate = null;
        //[Link] Function MaxNConcDailyGrowth = null;
        [Link] Function NitrogenDemandSwitch = null;
        [Link] Function MaximumNConc = null;
        [Link] Function MinimumNConc = null;
        [Link] Function DMDemandFunction = null;
        #endregion

        #region Class Fields
        public double MaximumSize = 0;
        public string RipeStage = "";
        protected bool _ReadyForHarvest = false;
        protected double DailyGrowth = 0;
        private double PotentialDMAllocation = 0;
        #endregion

        #region Class Properties
        
        [Units("/m^2")]
        public double Number = 0;
        
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
        
        [Units("g")]
        private double Size
        {
            get
            {
                if (Number > 0)
                    return Live.Wt / Number;
                else
                    return 0;
            }
        }
        
        [Units("g")]
        private double FSize
        {
            get
            {
                if (Number > 0)
                {
                    if (WaterContent != null)
                        return (Live.Wt / Number) / (1 - WaterContent.Value);
                    else
                        return 0.0;
                }
                else
                    return 0;
            }
        }
        
        public int ReadyForHarvest
        {
            get
            {
                if (_ReadyForHarvest)
                    return 1;
                else
                    return 0;
            }
        }
        #endregion

        #region Functions
        public override void OnHarvest()
        {
            if (Harvesting != null)
                Harvesting.Invoke();

            double YieldDW = (Live.Wt + Dead.Wt);

            Summary.WriteMessage(FullPath, "Harvesting " + Name + " from " + Plant.Name);
            Summary.WriteMessage(FullPath, " Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)");
            Summary.WriteMessage(FullPath, " Size: " + Size.ToString("f2") + " (g)");
            Summary.WriteMessage(FullPath, " Number: " + Number.ToString("f2") + " (/m^2)");

            Live.Clear();
            Dead.Clear();
            Number = 0;
            _ReadyForHarvest = false;
        }
        #endregion

        #region Event handlers
        
        public event NullTypeDelegate Harvesting;
        public override void OnCut()
        {
            Summary.WriteMessage(FullPath, "Cutting " + Name + " from " + Plant.Name);

            Live.Clear();
            Dead.Clear();
            Number = 0;
            _ReadyForHarvest = false;
        }
        #endregion

        #region Arbitrator methods
        public override void DoActualGrowth()
        {
            base.DoActualGrowth();
            if (Phenology.OnDayOf(RipeStage))
                _ReadyForHarvest = true;
        }
        public override BiomassPoolType DMDemand
        {
            get
            {

                double Demand = 0;
                if (DMDemandFunction != null)
                {
                    Demand = DMDemandFunction.Value;
                }
                else
                {
                    Number = NumberFunction.Value;
                    if (Number > 0)
                    {
                        double demand = Number * FillingRate.Value;
                        // Ensure filling does not exceed a maximum size
                        Demand = Math.Min(demand, (MaximumSize - Live.Wt / Number) * Number);
                    }
                    else
                        Demand = 0;
                }
                return new BiomassPoolType { Structural = Demand };
            }
        }
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);
                PotentialDMAllocation = value.Structural;
            }
        }
        public override BiomassAllocationType DMAllocation
        { set { Live.StructuralWt += value.Structural; DailyGrowth = value.Structural; } }
        public override BiomassPoolType NDemand
        {
            get
            {
                double _NitrogenDemandSwitch = 1;
                if (NitrogenDemandSwitch != null) //Default of 1 means demand is always truned on!!!!
                    _NitrogenDemandSwitch = NitrogenDemandSwitch.Value;
                double demand = Number * NFillingRate.Value;
                demand = Math.Min(demand, MaximumNConc.Value * DailyGrowth) * _NitrogenDemandSwitch;
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
        public override double MaxNconc
        {
            get
            {
                return MaximumNConc.Value;
            }
        }
        public override double MinNconc
        {
            get
            {
                return MinimumNConc.Value;
            }
        }
        #endregion
    }
}
