namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;

    /// <summary>
    /// From the value of the first child function, subtract the values of the subsequent children functions
    /// </summary>
    [Serializable]
    public class DivideFunction : MathematicalBaseFunction, ICustomDocumentation
    {
        /// <summary>Returns the character to insert into auto-generated documentation</summary>
        protected override char OperatorCharForDocumentation { get { return '/'; } }

        /// <summary>Perform the mathematical operation</summary>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        protected override double PerformOperation(double value1, double value2)
        {
            return MathUtilities.Divide(value1, value2, 0.0);
        }
    }
}