namespace Models.PMF
{
    /// <summary>Common C4 leaf calculations</summary>
    public static class C4LeafCalculations
    {
        /// <summary>
        /// Calculate the largest leaf position. For Buster aX0S = 0.6 and aX0I = 3.58. So for a 16 leaf buster plant,
        /// the largest leaf, counting from the bottom is: aX0S * FLN + aX0I: 0.6 * 16 + 3.58 = 13.18.
        /// </summary>
        public static double CalculateLargestLeafPosition(
            double ax0i,
            double ax0s,
            double finalLeafNo
        )
        {
            double largestLeafPosition = (ax0s * finalLeafNo) + ax0i;
            return largestLeafPosition;
        }


        /// <summary>
        /// Calculate the area of the largest leaf. 
        /// </summary>
        public static double CalculateAreaOfLargestLeaf(
            double amaxi,
            double amaxs,
            double finalLeafNo
        )
        {
            double areaOfLargestLeaf = (amaxs * finalLeafNo) + amaxi;
            return areaOfLargestLeaf;
        }
    }
}
