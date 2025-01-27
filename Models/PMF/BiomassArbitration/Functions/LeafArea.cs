using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;

namespace Models.PMF
{
    /// <summary>
    /// This function calculates the leaf area index of the crop from the initial LAI, the LAI grown durig the crop and " +
    /// the LAI senesced and removed
    /// </summary>
    [Serializable]
    [Description("This function calculates the leaf area index of the crop from the initial LAI, the LAI grown durig the crop and " +
        "the LAI senesced and removed")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(EnergyBalance))]
    public class LeafArea : Model, IFunction
    {
        /// <summary>Leaf Area Index on the day of emergence</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Description("Leaf Area Index on the day of emergence")]
        [Units("m2/m2")]
        private IFunction Initial = null;

        /// <summary>Leaf Area grown since emergence</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Description("Leaf Area grown since emergence")]
        [Units("m2/m2")]
        private IFunction AreaGrown = null;

        /// <summary>Leaf Area grown since emergence</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Description("Leaf Area Senesced since emergence")]
        [Units("m2/m2")]
        private IFunction AreaSenesced = null;

        /// <summary>Leaf Area removed since emergence</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Description("Leaf Area grown since emergence")]
        [Units("m2/m2")]
        private IFunction AreaRemoved = null;


        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return Math.Max(0,Initial.Value() + AreaGrown.Value() - AreaSenesced.Value() - AreaRemoved.Value());
        }
    }
}

