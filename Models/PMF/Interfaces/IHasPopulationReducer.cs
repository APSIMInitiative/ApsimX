namespace Models.PMF.Interfaces
{
    /// <summary>Interface used by models that can reduce their population.</summary>
    public interface IHasPopulationReducer
    {
        /// <summary>Plant population.</summary>
        double Population { get; }

        /// <summary>Reduce the plant population.</summary>
        /// <param name="newPlantPopulation">The new plant population.</param>
        void ReducePopulation(double newPlantPopulation);
    }
}
