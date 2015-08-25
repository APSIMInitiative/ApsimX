using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using APSIM.Shared.Utilities;

namespace Models.PMF.OldPlant
{
    /// <summary>
    /// plant soil water stress model
    /// </summary>
    [Serializable]
    [ValidParent(typeof(Plant15))]
    public class SWStress : Model
    {
        /// <summary>The _ photo</summary>
        private double _Photo = 1;
        /// <summary>The _ pheno</summary>
        private double _Pheno = 1;
        /// <summary>The _ pheno flower</summary>
        private double _PhenoFlower = 1;
        /// <summary>The _ pheno grain filling</summary>
        private double _PhenoGrainFilling = 1;
        /// <summary>The _ expansion</summary>
        private double _Expansion = 1;
        /// <summary>The _ fixation</summary>
        private double _Fixation = 1;
        /// <summary>The _ oxygen deficit photo</summary>
        private double _OxygenDeficitPhoto = 1;
        /// <summary>The _ photo sum</summary>
        private double _PhotoSum = 0;
        /// <summary>The _ photo count</summary>
        private int _PhotoCount = 0;
        /// <summary>The _ expansion sum</summary>
        private double _ExpansionSum = 0;
        /// <summary>The _ expansion count</summary>
        private int _ExpansionCount = 0;

        /// <summary>The plant</summary>
        [Link]
        Plant15 Plant = null;

        /// <summary>The expansion factor</summary>
        [Link]
        IFunction ExpansionFactor = null;
        /// <summary>The pheno factor</summary>
        [Link]
        IFunction PhenoFactor = null;
        /// <summary>The fixation factor</summary>
        [Link]
        IFunction FixationFactor = null;
        /// <summary>The oxygen deficit photo factor</summary>
        [Link]
        IFunction OxygenDeficitPhotoFactor = null;
        /// <summary>The pheno flower factor</summary>
        [Link]
        IFunction PhenoFlowerFactor = null;
        /// <summary>The pheno grain filling factor</summary>
        [Link]
        IFunction PhenoGrainFillingFactor = null;

        /// <summary>Gets the photo stress.</summary>
        /// <value>The photo stress.</value>
        public double PhotoStress { get { return 1 - Photo; } }
        /// <summary>Gets the pheno stress.</summary>
        /// <value>The pheno stress.</value>
        public double PhenoStress { get { return 1 - Pheno; } }
        /// <summary>Gets the expansion stress.</summary>
        /// <value>The expansion stress.</value>
        public double ExpansionStress { get { return 1 - Expansion; } }
        /// <summary>Gets the fixation stress.</summary>
        /// <value>The fixation stress.</value>
        public double FixationStress { get { return 1 - Fixation; } }

        /// <summary>Gets the photo.</summary>
        /// <value>The photo.</value>
        public double Photo { get { return _Photo; } }
        /// <summary>Gets the photo average.</summary>
        /// <value>The photo average.</value>
        public double PhotoAverage { get { return MathUtilities.Divide(_PhotoSum, _PhotoCount, 0.0); } }
        /// <summary>Gets the expansion average.</summary>
        /// <value>The expansion average.</value>
        public double ExpansionAverage { get { return MathUtilities.Divide(_ExpansionSum, _ExpansionCount, 0.0); } }
        /// <summary>Gets the fixation.</summary>
        /// <value>The fixation.</value>
        public double Fixation { get { return _Fixation; } }
        /// <summary>Gets the oxygen deficit photo.</summary>
        /// <value>The oxygen deficit photo.</value>
        public double OxygenDeficitPhoto { get { return _OxygenDeficitPhoto; } }
        /// <summary>Gets the expansion.</summary>
        /// <value>The expansion.</value>
        public double Expansion { get { return _Expansion; } }
        /// <summary>Gets the pheno.</summary>
        /// <value>The pheno.</value>
        public double Pheno { get { return _Pheno; } }
        /// <summary>Gets the pheno flower.</summary>
        /// <value>The pheno flower.</value>
        public double PhenoFlower { get { return _PhenoFlower; } }
        /// <summary>Gets the pheno grain filling.</summary>
        /// <value>The pheno grain filling.</value>
        public double PhenoGrainFilling { get { return _PhenoGrainFilling; } }


        /// <summary>Does the plant water stress.</summary>
        /// <param name="sw_demand">The sw_demand.</param>
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
        /// for photosysnthesis. 1 is no stress, 0 is full stress.
        /// </summary>
        /// <param name="sw_demand">The sw_demand.</param>
        /// <returns></returns>
        double SWDefPhoto(double sw_demand)
        {
            if (sw_demand > 0.0)
            {
                //get potential water that can be taken up when profile is full
                double SWUptake = 0;
                foreach (Organ1 Organ in Plant.Organ1s)
                    SWUptake += Organ.SWUptake;
                double sw_demand_ratio = MathUtilities.Divide(SWUptake, sw_demand, 1.0);
                return MathUtilities.Constrain(sw_demand_ratio, 0.0, 1.0);
            }
            else
                return 1.0;
        }

        /// <summary>Resets the average.</summary>
        internal void ResetAverage()
        {
            _PhotoSum = 0;
            _PhotoCount = 0;
            _ExpansionSum = 0;
            _ExpansionCount = 0;
        }

        /// <summary>Updates this instance.</summary>
        internal void Update()
        {
            _PhotoSum += Photo;
            _PhotoCount++;

            _ExpansionSum += Photo;
            _ExpansionCount++;
        }
    }
}
