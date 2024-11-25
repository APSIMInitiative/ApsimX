using System;
using System.Collections.Generic;

namespace Models.PMF.Struct
{
    /// <summary>
    /// Represents a culm (either the main stem or a tiller).
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

        /// <summary>The area of the largest leaf on this culm.</summary>
        public double AreaOfLargestLeaf { get; set; }

        /// <summary>The position of the largest leaf on this culm.</summary>
        public double PositionOfLargestLeaf { get; set; }

        /// <summary>Constructor. </summary>
        /// <param name="leafAppearance"></param>
        public Culm(double leafAppearance)
        {
            LeafNoAtAppearance = leafAppearance;
            Initialize();
        }

        /// <summary>Potential Leaf sizes can be calculated early and then referenced</summary>
        public void UpdatePotentialLeafSizes(Culm mainCulm, ICulmLeafArea areaCalc)
        {
            LeafSizes.Clear();

            if (CulmNo == 0)
            {
                AreaOfLargestLeaf = areaCalc.CalculateAreaOfLargestLeaf(FinalLeafNo, CulmNo);
                PositionOfLargestLeaf = areaCalc.CalculateLargestLeafPosition(FinalLeafNo, CulmNo);
            }
            else
            {
                double relLeafSize = GetRelativeLeafSize();
                AreaOfLargestLeaf = mainCulm.AreaOfLargestLeaf * (1 - relLeafSize);
                PositionOfLargestLeaf = mainCulm.PositionOfLargestLeaf - (CulmNo + 1);
            }

            for (int i = 1; i < Math.Ceiling(FinalLeafNo) + 1; i++)
            {
                LeafSizes.Add(areaCalc.CalculateIndividualLeafArea(i, this));
            }
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

        private double GetRelativeLeafSize()
        {
            if (CulmNo == 1)
                return 0.23;
            else if (CulmNo < 5)
                return 0.13;
            else return 0.39;
        }
    }
}
