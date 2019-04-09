namespace Models.PMF.Interfaces
{
    using Organs;

    /// <summary>
    /// Interface for implementing how BiomassType is Retranslocated
    /// </summary>
    public interface IRetranslocateMethod
    {
        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="allocationType"></param>
        void AllocateN(GenericOrgan organ, BiomassAllocationType allocationType);

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        /// <param name="allocationType"></param>
        void AllocateBiomass(GenericOrgan organ, BiomassAllocationType allocationType);

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        double CalculateN(GenericOrgan organ);

        /// <summary>Allocate the retranslocated material</summary>
        /// <param name="organ"></param>
        double CalculateBiomass(GenericOrgan organ);

    }
}
