using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Plant.OldPlant
{
    class PlantSpatial1
    {
        SowPlant2Type SowData;
        double _CanopyWidth = 0;

        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            SowData = Sow;
            if (Sow.SkipRow < 0 || Sow.SkipRow > 2)
                throw new Exception("Invalid SkipRow: " + Sow.SkipRow.ToString());
        }

        public double Density { get { return SowData.Population; } set { SowData.Population = value; } }
        private double SkipRowFactor { get { return (2.0 + SowData.SkipRow) / 2.0; } }
        public double CanopyWidth { get { return _CanopyWidth; } set { _CanopyWidth = value; } }
        public double RowSpacing { get { return SowData.RowSpacing; } }
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
