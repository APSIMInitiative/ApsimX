using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;

namespace Models.PMF.Functions.SupplyFunctions
{
    public class RUEModel : Model
    {
        [Link]
        Plant Plant = null;

        public Function RUE { get; set; }

        public Function FCO2 { get; set; }

        public Function FN { get; set; }

        public Function FT { get; set; }

        public Function FW { get; set; }

        public Function FVPD { get; set; }

        [Link]
        public WeatherFile MetData = null;


        #region Class Data Members
        //[Input]
        //public NewMetType MetData;

        
        public event NewPotentialGrowthDelegate NewPotentialGrowth;
        #endregion

        #region Associated variables
        
        public double VPD
        {
            get
            {
                const double SVPfrac = 0.66;

                double VPDmint = Utility.Met.svp((float)MetData.MinT) - MetData.vp;
                VPDmint = Math.Max(VPDmint, 0.0);

                double VPDmaxt = Utility.Met.svp((float)MetData.MaxT) - MetData.vp;
                VPDmaxt = Math.Max(VPDmaxt, 0.0);

                return SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            }
        }

        #endregion

        /// <summary>
        /// Total plant "actual" radiation use efficiency (for the day) corrected by reducing factors (g biomass/MJ global solar radiation) CHCK-EIT 
        /// </summary>
        [Units("gDM/MJ")]
        private double RueAct
        {
            get
            {
                double RueReductionFactor = Math.Min(FT.FunctionValue, Math.Min(FN.FunctionValue, FVPD.FunctionValue)) * FW.FunctionValue * FCO2.FunctionValue;
                return RUE.FunctionValue * RueReductionFactor;
            }
        }
        /// <summary>
        /// Daily growth increment of total plant biomass
        /// </summary>
        /// <param name="RadnInt">intercepted radiation</param>
        /// <returns>g dry matter/m2 soil/day</returns>
        public double Growth(double RadnInt)
        {
            return RadnInt * RueAct;
        }

        private void PublishNewPotentialGrowth()
        {
            // Send out a NewPotentialGrowthEvent.
            if (NewPotentialGrowth != null)
            {
                NewPotentialGrowthType GrowthType = new NewPotentialGrowthType();
                GrowthType.sender = Plant.Name;
                GrowthType.frgr = (float)Math.Min(FT.FunctionValue, Math.Min(FN.FunctionValue, FVPD.FunctionValue));
                NewPotentialGrowth.Invoke(GrowthType);
            }
        }
        [EventSubscribe("StartOfDay")]
        private void OnPrepare(object sender, EventArgs e)
        {
            PublishNewPotentialGrowth();
        }
    }
}