namespace Models.PMF
{
    /// <summary>
    /// Calculate Individual leaf size within a Culm
    /// </summary>
    public interface ICulmLeafArea
    {
        /// <summary>Calclate the individual leaf area given the leaf number and Final number of leaves </summary>
        double CalculateIndividualLeafArea(double leafNo, double finalLeafNo, double vertAdjust = 0.0);
    }
}
