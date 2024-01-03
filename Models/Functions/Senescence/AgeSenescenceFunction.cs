using System;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Organs;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>Calculate Senescence due to age for C4Maize</summary>
    [Serializable]
    [Description("Calculate AgeSenescence")]
    public class AgeSenescenceFunction : Model, IFunction
    {
        [Link(Type = LinkType.Ancestor)]
        private SorghumLeaf leaf = null;

        /// <summary>Phenology</summary>
        [Link]
        private Phenology phenology = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoDeadIntercept = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoDeadSlope = null;

        private double nDeadLeaves;
        private double dltDeadLeaves;
        private const double squareMM2squareM = 1.0 / 1000000.0;      //! conversion factor of mm^2 to m^2

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            nDeadLeaves = 0.0;
            dltDeadLeaves = 0.0;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            nDeadLeaves = 0.0;
            dltDeadLeaves = 0.0;
        }

        private double CalcDltDeadLeaves()
        {
            double nDeadYesterday = nDeadLeaves;
            double nDeadToday = leaf.FinalLeafNo * (leafNoDeadIntercept.Value() + leafNoDeadSlope.Value() * phenology.AccumulatedEmergedTT);
            nDeadToday = MathUtilities.Bound(nDeadToday, nDeadYesterday, leaf.FinalLeafNo);
            return nDeadToday - nDeadYesterday;
        }

        /// <summary>Calculate Senescence due to age for C4Maize</summary>
        public double Value(int arrayIndex = -1)
        {
            dltDeadLeaves = CalcDltDeadLeaves();
            double deadLeaves = nDeadLeaves + dltDeadLeaves;
            double laiSenescenceAge = 0;
            if (MathUtilities.IsPositive(deadLeaves))
            {
                int leafDying = (int)Math.Ceiling(deadLeaves);
                double areaDying = (deadLeaves % 1.0) * leaf.culms.LeafSizes[leafDying - 1];
                laiSenescenceAge = (leaf.culms.LeafSizes.Take(leafDying - 1).Sum() + areaDying) * squareMM2squareM * leaf.SowingDensity;
            }
            return Math.Max(laiSenescenceAge - leaf.SenescedLai, 0);
        }
    }
}
