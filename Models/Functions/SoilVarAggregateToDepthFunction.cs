using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Functions
{
    /// <summary>
    /// Return child function value aggregated to a specified depth (mm).
    /// The layer structure should match the simulations [Soil].Physical component.
    /// No partial layers are considered - the entire layer containing the top/bottom 
    /// depth is used.
    /// </summary>
    [Serializable]
    [Description("Return child function value aggregated to a specified depth.")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SoilVarAggregateToDepthFunction : Model, IFunction
    {
        [Link]
        IPhysical soilPhysical = null;

        /// <summary>The child functions</summary>
        [Link(Type = LinkType.Child)]
        IFunction ValueInLayer = null;

        /// <summary>The depth of the top of the slice</summary>
        [Units("mm")]
        [Description("Depth of the top of profile slice to aggregate")]
        public double TopDepth { get; set; } = 0;

        /// <summary>The depth of the bottom of the slice</summary>
        [Units("mm")]
        [Description("Depth of the bottom of profile slice to aggregate")]
        public double BottomDepth { get; set; }

        /// <summary>Method used to aggregate sub daily values</summary>
        [Description("Method used to aggregate sub daily temperature function")]
        public AggregationMethod aggregationMethod { get; set; }

        /// <summary>Method used to aggregate sub daily values</summary>
        public enum AggregationMethod
        {
            /// <summary>Return average of sub daily values</summary>
            Average,
            /// <summary>Return sum of sub daily values</summary>
            Sum
        }

        private int topLayer = 0;
        private int bottomLayer = 0;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            topLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, TopDepth);
            bottomLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, BottomDepth);
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// </exception>
        public double Value(int arrayIndex = -1)
        {
            if (aggregationMethod == AggregationMethod.Average)
            {
                double depthWeightedSum = 0;
                double accumDepth = 0;

                for (int i = topLayer; i <= bottomLayer; i++)
                {
                    depthWeightedSum += ValueInLayer.Value(i) * soilPhysical.Thickness[i];
                    accumDepth += soilPhysical.Thickness[i];
                }
                return depthWeightedSum / accumDepth;
            }

            if (aggregationMethod == AggregationMethod.Sum)
            {
                double accumVar = 0;
                for (int i = topLayer; i <= bottomLayer; i++)
                {
                    accumVar += ValueInLayer.Value(i);
                }
                return accumVar;
            }

            else { throw new Exception("Select aggregation method"); }
        }
    }
}
