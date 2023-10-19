using System;
using Models.Core;
using Models.Soils.Nutrients;

namespace Models.Functions
{
    /// <summary>A class that calculates CERES decomposition.</summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(OrganicFlow))]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class CERESDecomposition : Model, IFunction
    {
        [Link(ByName = true)]
        private IFunction TF = null;

        [Link(ByName = true)]
        private IFunction WF = null;

        [Link(Type = LinkType.Ancestor)]
        private Nutrient nutrient = null;

        /// <summary>The potential rate of decomposition</summary>
        [Description("The potential rate of decomposition")]
        public double PotentialRate { get; set; } = 0.0095;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double rate = PotentialRate * TF.Value(arrayIndex) * WF.Value(arrayIndex);
            rate *= nutrient.CNRF[arrayIndex];
            return rate;
        }
    }
}