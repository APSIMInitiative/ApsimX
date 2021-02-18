using Models.PMF.Interfaces;

namespace Models.LifeCycle
{
    /// <summary>
    /// Interface for Infest methods for LifeCycle
    /// </summary>
    public interface IInfest
    {
        /// Method to send infestation event to lifecycle class
        void Infest();

    }
}