using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Factorial
{

    /// <summary>
    /// This class permutates all child models by each other.
    /// </summary>
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Factor))]
    [ValidParent(ParentType = typeof(Permutation))]
    [Serializable]
    public class Permutation : Model
    {
        /// <summary>
        /// Get a list of all permutations of child factors and compositefactors.
        /// </summary>
        internal List<List<CompositeFactor>> GetPermutations()
        {
            var factors = new List<List<CompositeFactor>>();
            foreach (Factor factor in this.FindAllChildren<Factor>())
            {
                if (factor.Enabled)
                    factors.Add(factor.GetCompositeFactors());
            }

            var compositeFactors = this.FindAllChildren<CompositeFactor>().Where(cf => cf.Enabled);

            var permutations = new List<List<CompositeFactor>>();
            if (compositeFactors.Count() > 0)
            {
                // Loop through each composite factor and permute with the factor children.
                foreach (CompositeFactor compositeFactor in compositeFactors)
                {
                    var valuesToPermutate = new List<List<CompositeFactor>>(factors);
                    valuesToPermutate.Add(new List<CompositeFactor>() { compositeFactor });
                    permutations.AddRange(MathUtilities.AllCombinationsOf<CompositeFactor>(valuesToPermutate.ToArray()));
                }
            }
            else
                permutations = MathUtilities.AllCombinationsOf<CompositeFactor>(factors.ToArray());

            return permutations;
        }
    }
}
