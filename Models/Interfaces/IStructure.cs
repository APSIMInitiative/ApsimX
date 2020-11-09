using Models.Core;

namespace Models.Interfaces
{
    /// <summary>
    /// An interface for morphological plant development.
    /// </summary>
    public interface IStructure : IModel
    {
        /// <summary> The change in plant population due to plant mortality set in the plant class </summary>
        double DeltaPlantPopulation { get; set; }

        /// <summary>The proportion plant mortality.</summary>
        double ProportionPlantMortality { get; set; }

        /// <summary>Called when crop recieves a remove biomass event from manager</summary>
        void DoThin(double ProportionRemoved);

        /// <summary> Removes nodes from main-stem in defoliation event  </summary>
        void DoNodeRemoval(int NodesToRemove);
    }
}
