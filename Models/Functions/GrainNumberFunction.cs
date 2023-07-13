using System;
using Models.Core;
using Models.PMF.Organs;

namespace Models.Functions
{
    /// <summary>
    /// Calculates the current grain number.
    /// </summary>
    [Serializable]
    [ValidParent(typeof(ReproductiveOrgan))]
    public class GrainNumberFunction : Model, IFunction
    {
        /// <summary>
        /// Potential total number of grains
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("Total Number of Seeds")]
        private IFunction potentialGrainNumber = null;

        /// <summary>
        /// Amount of grain growth per day
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("kg/ha/day")]
        private IFunction growthRateFactor = null;

        /// <summary>
        /// Effect of stress on daily growth (between 0-1).
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction stressFactor = null;

        /// <summary>
        /// Get the grain number.
        /// </summary>
        /// <param name="arrayIndex">Array index (irrelevant for this function).</param>
        public double Value(int arrayIndex = -1)
        {
            return potentialGrainNumber.Value(arrayIndex) * growthRateFactor.Value(arrayIndex) * stressFactor.Value(arrayIndex);
        }
    }
}
