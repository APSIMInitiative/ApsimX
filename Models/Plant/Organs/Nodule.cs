using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.Organs
{
    [Serializable]
    public class Nodule : GenericOrgan, BelowGround
    {
        #region Paramater Input Classes
        public Function FixationMetabolicCost { get; set; }
        public Function SpecificNitrogenaseActivity { get; set; }
        public Function FT { get; set; }
        public Function FW { get; set; }
        public Function FWlog { get; set; }
        #endregion

        #region Class Fields
        public double RespiredWt = 0;
        public double PropFixationDemand = 0;
        public double _NFixed = 0;
        #endregion

        #region Arbitrator methods
        public override double NFixationCost
        {
            get
            {
                return FixationMetabolicCost.Value;
            }
        }
        public override BiomassAllocationType NAllocation
        {
            set
            {
                base.NAllocation = value;    // give N allocation to base first.
                _NFixed = value.Fixation;    // now get our fixation value.
            }
        }
        
        public double RespiredWtFixation
        {
            get
            {
                return RespiredWt;
            }
        }
        public override BiomassSupplyType NSupply
        {
            get
            {
                BiomassSupplyType Supply = base.NSupply;   // get our base GenericOrgan to fill a supply structure first.
                if (Live != null)
                {
                    // Now add in our fixation
                    Supply.Fixation = Live.StructuralWt * SpecificNitrogenaseActivity.Value * Math.Min(FT.Value, Math.Min(FW.Value, FWlog.Value));
                }
                return Supply;
            }
        }
        public override BiomassAllocationType DMAllocation
        {
            set
            //This is the DM that is consumed to fix N.  this is calculated by the arbitrator and passed to the nodule to report
            {
                base.DMAllocation = value;      // Give the allocation to our base GenericOrgan first
                RespiredWt = value.Respired;    // Now get the respired value for ourselves.
            }
        }
        
        public double NFixed
        {
            get
            {
                return _NFixed;
            }
        }
        #endregion
    }
}
