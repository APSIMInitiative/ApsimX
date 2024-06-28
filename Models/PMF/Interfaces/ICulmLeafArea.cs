using Models.PMF.Struct;

namespace Models.PMF
{
    /// <summary>
    /// Calculate Individual leaf size within a Culm
    /// </summary>
    public interface ICulmLeafArea
    {
        /// <summary>Calculate the individual leaf area given the leaf number and Final number of leaves.</summary>
        double CalculateIndividualLeafArea(double leafNo, Culm culm);

        /// <summary>Calclate the area if of the largest leaf.</summary>
        double CalculateAreaOfLargestLeaf(double finalLeafNo, int culmNo);

        /// <summary>Calclate the size of the largest leaf.</summary>
        double CalculateLargestLeafPosition(double finalLeafNo, int culmNo);
    }
}
