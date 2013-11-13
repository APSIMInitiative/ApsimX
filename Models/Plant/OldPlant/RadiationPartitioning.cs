using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Plant.OldPlant
{
    public class RadiationPartitioning: Model
    {
        public double FractIncidentRadn = 0.0;

        public string[] RadiationPartitioningOrder = null;

        [Link]
        WeatherFile MetData = null;

        public void DoRadiationPartition()
        {
            double incomingSolarRadiation = MetData.Radn * FractIncidentRadn;

            foreach (string OrganName in RadiationPartitioningOrder)
            {
                Organ1 Organ = this.Find(OrganName) as Organ1;

                // calc the total interception from this part - what is left is transmitted
                // to the other parts.
                incomingSolarRadiation -= Organ.interceptRadiation(incomingSolarRadiation);
            }
            Util.Debug("RadiationPartitioning.IncomingSolarRadiation=%f", incomingSolarRadiation);
        }

    }
}
