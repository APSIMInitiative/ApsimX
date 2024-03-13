using Models.Core;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Interface for managing tillering.
    /// Tillers are stored in Culms in the Leaf organ where the first Culm is the main stem and the remaining culms are the tillers.
    /// </summary>
    public interface ITilleringMethod : IModel
    {
        /// <summary> Update number of leaves for all culms </summary>
        double CalcLeafNumber();
        /// <summary> Calculate the potential leaf area for the tillers</summary>
        double CalcPotentialLeafArea();

        /// <summary> Calculate the actual Area for the Culms</summary>
        double CalcActualLeafArea(double dltStressedLAI);

        /// <summary> Fertile tiller Number (at harvest) </summary>
        double FertileTillerNumber { get; set; }

        /// <summary>Current Number of Tillers</summary>
        double CurrentTillerNumber { get; set; }

        /// <summary>Calculated Tiller Number</summary>
        double CalculatedTillerNumber { get; set; }

        /// <summary>Number of potential Fertile Tillers at harvest</summary>
        double MaxSLA { get; set; }
    }
}
