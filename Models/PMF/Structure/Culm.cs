using System;
using System.Collections.Generic;

namespace Models.PMF.Struct
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Culm
    {
        /// <summary> Used to allow for offset of number of leaves on the tiller</summary>
        public double LeafNoAtAppearance;

        /// <summary> Potential leaf growth for the day</summary>
        public double dltLeafNo;

        /// <summary> Vertical leaf adjustment.</summary>
        public double VertAdjValue { get; set; }

        /// <summary>Culm number.</summary>
        public int CulmNo { get; set; }

        /// <summary>Leaf proportion?</summary>
        public double Proportion { get; set; }

        /// <summary>Final leaf number.</summary>
        public double FinalLeafNo { get; set; }

        /// <summary>Current leaf number.</summary>
        public double CurrentLeafNo { get; set; }

        /// <summary>Leaf area.</summary>
        /// <remarks>Changes each day - doesn't include proportion of culm.</remarks>
        public double LeafArea { get; set; }

        /// <summary>Increase in Leaf area.</summary>
        /// <remarks>Changes each day - includes proportion effect.</remarks>
        public double DltLAI { get; set; }

        /// <summary>Increase in Leaf area reduced by stress effect.</summary>
        public double DltStressedLAI { get; set; }

        /// <summary>Accumulated lai for this culm.</summary>
        public double TotalLAI { get; set; }

        /// <summary>Calculated potential sizes for each leaf</summary>
        public List<double> LeafSizes { get; set; }

        // public Methods -------------------------------------------------------

        /// <summary>Constructor. </summary>
        /// <param name="leafAppearance"></param>
        public Culm(double leafAppearance)
        {
            LeafNoAtAppearance = leafAppearance;
            Initialize();
        }

        /// <summary> Potential Leaf sizes can be calculated early and then referenced</summary>
        public void UpdatePotentialLeafSizes(ICulmLeafArea areaCalc)
        {
            LeafSizes.Clear();
            //calculate the size of all leaves
            List<double> sizes = new List<double>();
            for (int i = 1; i < Math.Ceiling(FinalLeafNo) + 1; i++)
                sizes.Add(areaCalc.CalculateIndividualLeafArea(i, FinalLeafNo, VertAdjValue));

            // allow for the offset effect for subsequent tillers
            //tillers wil have less leaves - but they are initially larger
            //the effect is to shift the curve to the left
            int offset = CulmNo;
            for (int i = 0; i < Math.Ceiling(FinalLeafNo - (offset)); i++)
                LeafSizes.Add(sizes[i + offset]);
        }

        /// <summary>Leaf appearance is calculated in the tillering method</summary>
        public void AddNewLeaf(double dltLeaf)
        {
            dltLeafNo = dltLeaf;
            CurrentLeafNo += dltLeafNo;
        }

        /// <summary>Perform initialisation. </summary>
        public virtual void Initialize()
        {
            // leaf number
            FinalLeafNo = 0.0;
            dltLeafNo = 0.0;
            CurrentLeafNo = 0;
            VertAdjValue = 0.0;
            Proportion = 1.0;
            TotalLAI = 0.0;
            DltLAI = 0.0;
            DltStressedLAI = 0.0;
            CulmNo = 0;
            LeafSizes = new List<double>();
        }

    }
}
