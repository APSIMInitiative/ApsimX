using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.Interfaces;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// Radiation partitioning
    /// </summary>
    [Serializable]
    public class RadiationPartitioning : Model
    {
        /// <summary>Gets or sets the fract incident radn.</summary>
        /// <value>The fract incident radn.</value>
        public double FractIncidentRadn { get; set; }

        /// <summary>Gets or sets the radiation partitioning order.</summary>
        /// <value>The radiation partitioning order.</value>
        public string[] RadiationPartitioningOrder { get; set; }

        /// <summary>The met data</summary>
        [Link]
        IWeather MetData = null;

        /// <summary>Does the radiation partition.</summary>
        public void DoRadiationPartition()
        {
            double incomingSolarRadiation = MetData.Radn * FractIncidentRadn;

            foreach (string OrganName in RadiationPartitioningOrder)
            {
                Organ1 Organ = Apsim.Find(this, OrganName) as Organ1;

                // calc the total interception from this part - what is left is transmitted
                // to the other parts.
                incomingSolarRadiation -= Organ.interceptRadiation(incomingSolarRadiation);
            }
            Util.Debug("RadiationPartitioning.IncomingSolarRadiation=%f", incomingSolarRadiation);
        }

    }
}
