using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Functions;
using Models.Plant.Phen;

namespace Models.Plant.Organs
{
    class ReproductiveOrgan : BaseOrgan, Reproductive, AboveGround
    {
        #region Parameter Input Classes
        [Link]
        protected Plant2 Plant = null;
        [Link]
        protected Phenology Phenology = null;
        public Function WaterContent { get; set; }
        public Function FillingRate { get; set; }
        public Function NumberFunction { get; set; }
        public Function NFillingRate { get; set; }
        public Function MaxNConcDailyGrowth { get; set; }
        public Function NitrogenDemandSwitch { get; set; }
        public Function MaximumNConc { get; set; }
        public Function MinimumNConc { get; set; }
        public Function DMDemandFunction { get; set; }
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
                    return Live.Wt / (1 - WaterContent.FunctionValue);
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
                        return (Live.Wt / Number) / (1 - WaterContent.FunctionValue);
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

            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Harvesting " + Name + " from " + Plant.Name;
            double YieldDW = (Live.Wt + Dead.Wt);

            Console.WriteLine("");
            Console.WriteLine(Title);
            Console.WriteLine(Indent + new string('-', Title.Length));
            Console.WriteLine(Indent + Name + " Yield DWt: " + YieldDW.ToString("f2") + " (g/m^2)");
            Console.WriteLine(Indent + Name + " Size: " + Size.ToString("f2") + " (g)");
            Console.WriteLine(Indent + Name + " Number: " + Number.ToString("f2") + " (/m^2)");
            Console.WriteLine("");


            Live.Clear();
            Dead.Clear();
            Number = 0;
            _ReadyForHarvest = false;
        }
        #endregion

        #region Event handlers
        
        public event NullTypeDelegate Harvesting;
        [EventSubscribe("Cut")]
        private void OnCut()
        {
            string Indent = "     ";
            string Title = Indent + Clock.Today.ToString("d MMMM yyyy") + "  - Cutting " + Name + " from " + Plant.Name;
            Console.WriteLine("");
            Console.WriteLine(Title);
            Console.WriteLine(Indent + new string('-', Title.Length));

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
                    Demand = DMDemandFunction.FunctionValue;
                }
                else
                {
                    Number = NumberFunction.FunctionValue;
                    if (Number > 0)
                    {
                        double demand = Number * FillingRate.FunctionValue;
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
                    _NitrogenDemandSwitch = NitrogenDemandSwitch.FunctionValue;
                double demand = Number * NFillingRate.FunctionValue;
                demand = Math.Min(demand, MaximumNConc.FunctionValue * DailyGrowth) * _NitrogenDemandSwitch;
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
                return MaximumNConc.FunctionValue;
            }
        }
        public override double MinNconc
        {
            get
            {
                return MinimumNConc.FunctionValue;
            }
        }
        #endregion
    }
}
