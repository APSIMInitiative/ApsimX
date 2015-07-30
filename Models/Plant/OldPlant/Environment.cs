using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// An environment model for plant
    /// </summary>
    [Serializable]
    public class Environment : Model
    {

        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;

        /// <summary>The c o2</summary>
        public double CO2 = 350;

        /// <summary>Gets the mean t.</summary>
        /// <value>The mean t.</value>
        public double MeanT { get { return (MetData.MaxT + MetData.MinT) / 2.0; } }

        /// <summary>Gets the VPD.</summary>
        /// <value>The VPD.</value>
        public double VPD
        {
            get
            {
                const double SVPfrac = 0.75;

                return SVPfrac * (MetUtilities.svp(MetData.MaxT) - MetUtilities.svp(MetData.MinT)) / 10;
            }
        }

    }
}
