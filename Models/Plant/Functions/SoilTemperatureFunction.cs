using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the temperature of the surface soil laye
    /// </summary>
    [Serializable]
    [Description("returns the temperature of the surface soil layer")]
    public class SoilTemperatureFunction : Model, Function
    {
        #region Class Data Members
        /// <summary>The xy pairs</summary>
        public XYPairs XYPairs { get; set; }    // Temperature effect on Growth Interpolation Set

        /// <summary>The maxt_soil_surface</summary>
        public double maxt_soil_surface = 20; //Fixme.  These need to be connected to soil temp model when complete
        /// <summary>The mint_soil_surface</summary>
        public double mint_soil_surface = 10; //Fixme.  These need to be connected to soil temp model when complete
        
        #endregion


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("deg.day")]
        public double Value
        {
            get
            {
                AirTemperatureFunction airtempfunction = new AirTemperatureFunction();
                airtempfunction.XYPairs = XYPairs;
                return airtempfunction.Linint3hrlyTemp(maxt_soil_surface, mint_soil_surface, XYPairs);
            }
        }
    }
}
   
