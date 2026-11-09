using APSIM.Core;
using APSIM.Numerics;
using Models.Core;
using Models.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Soils.Nutrients
{
    /// <summary>
    /// Moves mineral N (NH4 and NO3) that is in excess of a threshold into an organic pool (by default Humic),
    /// without any accompanying carbon. This represents the rapid, largely abiotic or microbially mediated
    /// stabilisation of fertiliser N in soil organic matter that is observed after large N additions,
    /// particularly in forest soils, and that is not captured by the carbon-driven immobilisation of the
    /// organic flows. Mineral N in the soil within the specified depth is summed; each day a fraction (Rate)
    /// of the amount above the Threshold is removed from NH4 and NO3 (in proportion to their amounts in each
    /// layer) and added to the destination pool's N. The N is then released only as the destination pool
    /// decomposes. When this model is absent, soil N behaves exactly as before.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class MineralNImmobilisation : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        private double[] immobilised;
        private IOrganicPool destination;

        [Link]
        private readonly IPhysical physical = null;

        /// <summary>Daily fraction of the mineral N above the threshold that is immobilised (/d).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction rate = null;

        /// <summary>Mineral N (NH4 + NO3) within Depth below which no immobilisation occurs (kg/ha).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction threshold = null;

        /// <summary>Soil depth over which mineral N is summed and immobilised (mm).</summary>
        [Description("Depth over which mineral N is summed and immobilised (mm)")]
        [Units("mm")]
        public double Depth { get; set; } = 300;

        /// <summary>Name of the organic pool that receives the immobilised N.</summary>
        [Description("Name of the organic pool that receives the immobilised N")]
        public string DestinationName { get; set; } = "Humic";

        /// <summary>Mineral N immobilised today in each layer (kg/ha).</summary>
        public IReadOnlyList<double> Immobilised => immobilised;

        /// <summary>Total mineral N immobilised today (kg/ha).</summary>
        public double TotalImmobilised => immobilised == null ? 0 : immobilised.Sum();

        /// <summary>Performs the initial checks and setup.</summary>
        /// <param name="numberLayers">Number of layers.</param>
        public void Initialise(int numberLayers)
        {
            immobilised = new double[numberLayers];
            destination = Structure.Find<IOrganicPool>(DestinationName);
            if (destination == null)
                throw new Exception($"{Name}: cannot find destination pool with name: {DestinationName}");
        }

        /// <summary>Perform the daily immobilisation.</summary>
        /// <param name="no3">The NO3 solute.</param>
        /// <param name="nh4">The NH4 solute.</param>
        public void DoFlow(ISolute no3, ISolute nh4)
        {
            Array.Clear(immobilised);
            double[] no3kgha = no3.kgha;
            double[] nh4kgha = nh4.kgha;
            double[] thickness = physical.Thickness;

            // Fraction of each layer that lies within Depth.
            double[] fraction = new double[thickness.Length];
            double top = 0;
            for (int i = 0; i < thickness.Length; i++)
            {
                fraction[i] = MathUtilities.Bound(MathUtilities.Divide(Depth - top, thickness[i], 0), 0, 1);
                top += thickness[i];
            }

            double mineralN = 0;
            for (int i = 0; i < thickness.Length; i++)
                mineralN += (no3kgha[i] + nh4kgha[i]) * fraction[i];

            double excess = mineralN - threshold.Value();
            if (excess <= 0 || mineralN <= 0)
                return;

            double proportion = MathUtilities.Bound(rate.Value() * excess / mineralN, 0, 1);
            if (proportion <= 0)
                return;

            for (int i = 0; i < thickness.Length; i++)
            {
                if (fraction[i] <= 0)
                    continue;
                double fromNH4 = nh4kgha[i] * fraction[i] * proportion;
                double fromNO3 = no3kgha[i] * fraction[i] * proportion;
                nh4kgha[i] -= fromNH4;
                no3kgha[i] -= fromNO3;
                immobilised[i] = fromNH4 + fromNO3;
                destination.Add(i, 0, immobilised[i], 0);
            }
            nh4.SetKgHa(SoluteSetterType.Soil, nh4kgha);
            no3.SetKgHa(SoluteSetterType.Soil, no3kgha);
        }
    }
}
