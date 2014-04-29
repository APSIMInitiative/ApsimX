using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.PMF.OldPlant
{
    [Serializable]
    public class PlantSpatial1 : Model
    {
        [Link]
        Plant15 Plant = null;

        double _CanopyWidth = 0;

        [XmlIgnore]
        public double Density { get { return Plant.SowingData.Population; } set { Plant.SowingData.Population = value; } }
        private double SkipRowFactor { get { return (2.0 + Plant.SowingData.SkipRow) / 2.0; } }
        [XmlIgnore]
        public double CanopyWidth { get { return _CanopyWidth; } set { _CanopyWidth = value; } }
        public double RowSpacing { get { return Plant.SowingData.RowSpacing; } }
        public double CanopyFactor
        {
            get
            {
                if (CanopyWidth > 0.0)
                    throw new NotImplementedException();
                else
                    return SkipRowFactor;
            }
        }

    }
}
