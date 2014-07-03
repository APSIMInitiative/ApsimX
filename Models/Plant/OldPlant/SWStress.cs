using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.OldPlant
{
    [Serializable]
    public class SWStress : Model
    {
        private double _Photo = 1;
        private double _Pheno = 1;
        private double _PhenoFlower = 1;
        private double _PhenoGrainFilling = 1;
        private double _Expansion = 1;
        private double _Fixation = 1;
        private double _OxygenDeficitPhoto = 1;
        private double _PhotoSum = 0;
        private int _PhotoCount = 0;
        private double _ExpansionSum = 0;
        private int _ExpansionCount = 0;

        [Link]
        Plant15 Plant = null;

        [Link] Function ExpansionFactor = null;
        [Link] Function PhenoFactor = null;
        [Link] Function FixationFactor = null;
        [Link] Function OxygenDeficitPhotoFactor = null;
        [Link] Function PhenoFlowerFactor = null;
        [Link] Function PhenoGrainFillingFactor = null;

        public double PhotoStress { get { return 1 - Photo; } }
        public double PhenoStress { get { return 1 - Pheno; } }
        public double ExpansionStress { get { return 1 - Expansion; } }
        public double FixationStress { get { return 1 - Fixation; } }

        public double Photo { get { return _Photo; } }
        public double PhotoAverage { get { return Utility.Math.Divide(_PhotoSum, _PhotoCount, 0.0); } }
        public double ExpansionAverage { get { return Utility.Math.Divide(_ExpansionSum, _ExpansionCount, 0.0); } }
        public double Fixation { get { return _Fixation; } }
        public double OxygenDeficitPhoto { get { return _OxygenDeficitPhoto; } }
        public double Expansion { get { return _Expansion; } }
        public double Pheno { get { return _Pheno; } }
        public double PhenoFlower { get { return _PhenoFlower; } }
        public double PhenoGrainFilling { get { return _PhenoGrainFilling; } }


        public void DoPlantWaterStress(double sw_demand)
        {
            if (sw_demand > 0)
            {
                _Photo = SWDefPhoto(sw_demand);
                _Expansion = ExpansionFactor.Value;
            }
            else
            {
                _Photo = 1.0;
                _Expansion = 1.0;
            }
            _Pheno = PhenoFactor.Value;
            _PhenoFlower = PhenoFlowerFactor.Value;
            _PhenoGrainFilling = PhenoGrainFillingFactor.Value;
            _Fixation = FixationFactor.Value;
            _OxygenDeficitPhoto = OxygenDeficitPhotoFactor.Value;

            Util.Debug("SWStress.Photo=%f", _Photo);
            Util.Debug("SWStress.Pheno=%f", _Pheno);
            Util.Debug("SWStress.PhenoFlower=%f", _PhenoFlower);
            Util.Debug("SWStress.PhenoGrainFilling=%f", _PhenoGrainFilling);
            Util.Debug("SWStress.Expansion=%f", _Expansion);
            Util.Debug("SWStress.Fixation=%f", _Fixation);
            Util.Debug("SWStress.OxygenDeficitPhoto=%f", _OxygenDeficitPhoto);
        }

        /// <summary>
        /// Calculate the soil water supply to demand ratio and therefore the 0-1 stress factor
        ///       for photosysnthesis. 1 is no stress, 0 is full stress.
        /// </summary>
        double SWDefPhoto(double sw_demand)
        {
            if (sw_demand > 0.0)
            {
                //get potential water that can be taken up when profile is full
                double SWUptake = 0;
                foreach (Organ1 Organ in Plant.Organ1s)
                    SWUptake += Organ.SWUptake;
                double sw_demand_ratio = Utility.Math.Divide(SWUptake, sw_demand, 1.0);
                return Utility.Math.Constrain(sw_demand_ratio, 0.0, 1.0);
            }
            else
                return 1.0;
        }

        internal void ResetAverage()
        {
            _PhotoSum = 0;
            _PhotoCount = 0;
            _ExpansionSum = 0;
            _ExpansionCount = 0;
        }

        internal void Update()
        {
            _PhotoSum += Photo;
            _PhotoCount++;

            _ExpansionSum += Photo;
            _ExpansionCount++;
        }
    }
}
