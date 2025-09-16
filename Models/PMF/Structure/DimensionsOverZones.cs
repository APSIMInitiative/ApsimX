using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Zones;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.PMF
{
    /// <summary>
    /// This class calculates dimensions relative to the spread of the plants canopy over the home and neighbouring zone.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class DimensionsOverZones : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The parent plant</summary>
        [Link(Type = LinkType.Ancestor)]
        private Plant parentPlant = null;

        /// <summary>List of zones the plant model interacts with</summary>
        public List<Zone> Zones = null;

        /// <summary>Clear all variables</summary>
        private double totalZoneWidth = 0;
        /// <summary>The relative area of plant covering each zone</summary>
        public double[] RelativeAreaOverZone = null;
        /// <summary>Clear all variables</summary>
        public double PlantArea = 0;

        /// <summary>Clear all variables</summary>
        private void Clear()
        {
        }

        /// <summary>
        /// The width of the organ is assumed to be the width of the parent plant.  
        /// If parent plant does not have width model it is set as the width of the parent zone
        /// </summary>
        private double PlantWidth
        {
            get
            {
                IFunction width = Structure.FindChild<IFunction>("Width", relativeTo: parentPlant) as IFunction;
                if (width != null)
                    return width.Value() * Constants.mm2m; //Convert from mm to m
                else
                {
                    throw new Exception(parentPlant.Name + "Must have a Width function to determine dimensions over each zone");
                }
            }
        }

        /// <summary>Called when crop is sowing</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnPotentialPlantGrowth(object sender, EventArgs e)
        {
            totalZoneWidth = 0;
            Zone parentZone = Structure.FindParent<Zone>(recurse: true);
            int zi = 0;
            foreach (Zone z in Zones)
            {
                if (z is RectangularZone)
                {
                    totalZoneWidth += (z as RectangularZone).Width;
                }
                else
                {
                    totalZoneWidth = 1.0;
                }
                zi += 1;
            }
            double plantWidth = Math.Min(PlantWidth, totalZoneWidth);
            double zoneLength = 1.0;
            double[] plantWidthOverZone = new double[Zones.Count];
            zi = 0;
            foreach (Zone z in Zones)
            {
                if (z is RectangularZone)
                {
                    if (z.Name == parentZone.Name)
                    {
                        plantWidthOverZone[zi] = Math.Min(plantWidth,(z as RectangularZone).Width);
                        zoneLength = (z as RectangularZone).Length;
                    }
                    else
                    {
                        double overlap = Math.Max(0,plantWidth - (parentZone as RectangularZone).Width);
                        plantWidthOverZone[zi] = overlap;
                    }
                }
                else
                {
                    plantWidthOverZone[zi] = 1.0;
                }
                RelativeAreaOverZone[zi] = plantWidthOverZone[zi] / plantWidth;
                zi += 1;
            }
            double plantLength = Math.Min(zoneLength, PlantWidth); //Assume plant is square, length represents spacing so will not exceed zone length as plants start touching
            PlantArea = plantWidth * plantLength;
            
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, EventArgs e)
        {
            Simulation sim = Structure.FindParent<Simulation>();
            Zones = Structure.FindAll<Zone>(relativeTo: sim).ToList();
            RelativeAreaOverZone = new double[Zones.Count];
        }
    }
}
