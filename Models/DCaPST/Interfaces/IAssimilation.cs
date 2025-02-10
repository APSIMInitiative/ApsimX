namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// 
    /// </summary>    
    public interface IAssimilation
    {
        /// <summary>
        /// Generates the function used to calculate assimilation
        /// </summary>
        /// <param name="pathway">The assimilating pathway</param>
        /// <param name="leaf">The leaf temperature response model</param>
        AssimilationFunction GetFunction(AssimilationPathway pathway, TemperatureResponse leaf);

        /// <summary>
        /// Updates the intercellular CO2 for a pathway
        /// </summary>
        /// <param name="pathway">The pathway to update</param>
        /// <param name="gt">Water conductance</param>
        /// <param name="waterUseMolsSecond">Water usage rate</param>
        void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond);

        /// <summary>
        /// Updates the partial pressures for a pathway
        /// </summary>
        /// <param name="pathway">The pathway to update</param>
        /// <param name="leafGmT">The leaf temperature response model GmT value</param>
        /// <param name="function">The assimilation function specific to the pathway</param>
        void UpdatePartialPressures(AssimilationPathway pathway, double leafGmT, AssimilationFunction function);

        /// <summary>
        /// Maximum number of iterations when calculating assimilation convergence
        /// </summary>
        int Iterations { get; set; }
    }
}
