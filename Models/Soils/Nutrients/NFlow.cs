using Models.Core;
using Models.Functions;
using System;
using System.Collections.Generic;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// Encapsulates a nitrogen flow between mineral N pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class NFlow : Model
    {
        private double[] natm;
        private double[] n2oatm;
        private double[] value;
        private ISolute sourceSolute = null;
        private ISolute destinationSolute = null;


        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction rate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction NLoss = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction N2OFraction = null;


        /// <summary>Function to reduce the rate function above.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private readonly IFunction reduction = null;

        /// <summary>Name of source pool.</summary>
        [Description("Name of source pool")]
        public string SourceName { get; set; }

        /// <summary>Name of destination pool.</summary>
        [Description("Name of destination pool")]
        public string DestinationName { get; set; }


        /// <summary>Value of total N flow into destination.</summary>
        public IReadOnlyList<double> Value => value;

        /// <summary>Value of total loss.</summary>
        public IReadOnlyList<double> Natm => natm;

        /// <summary>N2O lost (kg/ha)</summary>
        public IReadOnlyList<double> N2Oatm => n2oatm;


        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="numberLayers">Number of layers.</param>
        public void Initialise(int numberLayers)
        {
            sourceSolute = FindInScope<ISolute>(SourceName);
            destinationSolute = FindInScope<ISolute>(DestinationName);

            value = new double[numberLayers];
            natm = new double[numberLayers];
            n2oatm = new double[numberLayers];
        }

        /// <summary>Perform nutrient flow from a source solute to a destination solute.</summary>
        public void DoFlow()
        {
            double[] source = sourceSolute.kgha;
            int numLayers = source.Length;

            double[] destination = null;
            if (DestinationName != null)
                destination = destinationSolute.kgha;

            for (int i = 0; i < numLayers; i++)
            {
                double nitrogenFlow = 0;
                if (source[i] > 0)
                    nitrogenFlow = rate.Value(i) * reduction.Value(i) * source[i];

                if (nitrogenFlow > 0)
                    natm[i] = nitrogenFlow * NLoss.Value(i);  // keep value of loss for use in output
                else
                    natm[i] = 0;

                if (natm[i] > 0)
                    n2oatm[i] = natm[i] * N2OFraction.Value(i);
                else
                    n2oatm[i] = 0;

                double nitrogenFlowToDestination = nitrogenFlow - Natm[i];

                if (destination == null && NLoss.Value(i) != 1)
                    throw new Exception("N loss fraction for N flow must be 1 if no destination is specified.");

                source[i] -= nitrogenFlow;
                value[i] = nitrogenFlowToDestination;  // keep value of flow for use in output
                if (destination != null)
                    destination[i] += nitrogenFlowToDestination;
            }
            sourceSolute.SetKgHa(SoluteSetterType.Soil, source);
            if (destination != null)
                destinationSolute.SetKgHa(SoluteSetterType.Soil, destination);
        }
    }
}
