namespace Models.Optimisation
{
    /// <summary>
    /// Encapsulates an optimization method which can be used by <see cref="CroptimizR"/>.
    /// </summary>
    public interface IOptimizationMethod
    {
        /// <summary>
        /// Optimization type.
        /// </summary>
        OptimizationTypeEnum Type { get; }

        /// <summary>
        /// Get the string value for the optim_param variable passed
        /// into the CroptimizR estim_param method.
        /// </summary>
        string ROptimizerName { get; }

        /// <summary>
        /// Name of the Critical function used for optimization.
        /// </summary>
        string CritFunction { get; }

        /// <summary>
        /// A method which generates R code defining a variable with
        /// optimization parameters specific to this optimization method.
        /// </summary>
        /// <param name="variableName">Name of the variable to be generated.</param>
        string GenerateOptimizationOptions(string variableName);
    }
}
