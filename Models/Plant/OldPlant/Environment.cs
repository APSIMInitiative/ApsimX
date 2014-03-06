using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.PMF.OldPlant
{
    public class Environment : Model
    {

        [Link]
        WeatherFile MetData = null;
        
        public double CO2 = 350;

        public double MeanT { get { return (MetData.MaxT + MetData.MinT) / 2.0; } }

        public double VPD
        {
            get
            {
                const double SVPfrac = 0.75;

                return SVPfrac * (Utility.Met.svp(MetData.MaxT) - Utility.Met.svp(MetData.MinT)) / 10;
            }
        }

    }
}
