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
            double finalLeafNo,
            int culmNo
        )
        {
            double largestLeafPosition = (ax0s * finalLeafNo) + ax0i;
            return largestLeafPosition - (culmNo == 0 ? 0 : culmNo + 1);
        }

        /// <summary>
        /// Calculate the area of the largest leaf. 
        /// </summary>
        public static double CalculateAreaOfLargestLeaf(
            double amaxi,
            double amaxs,
            double finalLeafNo,
            int culmNo
        )
        {
            double relLeafSize;
            if (culmNo == 0) relLeafSize = 0.0;
            else if (culmNo == 1) relLeafSize = 0.23;
            else if (culmNo < 5) relLeafSize = 0.13;
            else relLeafSize = 0.39;

            double areaOfLargestLeaf = (amaxs * finalLeafNo) + amaxi;
            return areaOfLargestLeaf * (1 - relLeafSize);
        }
    }
}
