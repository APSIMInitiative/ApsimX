using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("returns the temperature of the surface soil layer")]
    public class SoilTemperatureFunction : Function
    {
        #region Class Data Members
        [Link]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set
        
        public double maxt_soil_surface = 20; //Fixme.  These need to be connected to soil temp model when complete
        public double mint_soil_surface = 10; //Fixme.  These need to be connected to soil temp model when complete
        
        #endregion

        
        [Units("deg.day")]
        public override double Value
        {
            get
            {
                return AirTemperatureFunction.Linint3hrlyTemp(maxt_soil_surface, mint_soil_surface, XYPairs);
            }
        }
    }
}
   
