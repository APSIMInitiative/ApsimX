namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Interface for implementing how BiomassType is Retranslocated
    /// </summary>
    public interface IRetranslocateMethod
    {
        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="allocationType"></param>
        void Allocate(IOrgan organ, BiomassAllocationType allocationType);

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="allocationType"></param>
        void AllocateBiomass(IOrgan organ, BiomassAllocationType allocationType);

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        double Calculate(IOrgan organ);

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        double CalculateBiomass(IOrgan organ);

    }
}
