namespace Models.PMF
{
    /// <summary>Common C4 leaf calculations</summary>
    public static class C4LeafCalculations
    {
		/// <summary>Calculate the laregest leaf position</summary>
		public static double CalculateLargestLeafPosition(
            double ax0s,
            double ax0i,
            double finalLeafNo
        )
		{
            double largestLeafPosition = (ax0s * finalLeafNo) + ax0i;
            return largestLeafPosition;
		}
	}
}
