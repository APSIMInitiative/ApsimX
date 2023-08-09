using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;

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
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction rate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction NLoss = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction N2OFraction = null;

        [NonSerialized]
        private ISolute sourceSolute = null;
        [NonSerialized]
        private ISolute destinationSolute = null;

        private double[] natm;
        private double[] n2oatm;
        private double[] value;

        /// <summary>Value of total N flow into destination.</summary>
        public IReadOnlyList<double> Value => value;

        /// <summary>Value of total loss.</summary>
        public IReadOnlyList<double> Natm => natm;

        /// <summary>N2O lost (kg/ha)</summary>
        public IReadOnlyList<double> N2Oatm => n2oatm;

        /// <summary>
        /// Name of source pool
        /// </summary>
        [Description("Name of source pool")]
        public string sourceName { get; set; }

        /// <summary>
        /// Name of destination pool
        /// </summary>
        [Description("Name of destination pool")]
        public string destinationName { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="numberLayers">Number of layers.</param>
        public void Initialise(int numberLayers)
        {
            if (sourceSolute == null)
            {
                sourceSolute = FindInScope<ISolute>(sourceName);
                destinationSolute = FindInScope<ISolute>(destinationName);
            }

            //double[] source = sourceSolute.kgha;
            //int numLayers = source.Length;
             value = new double[numberLayers];
             natm = new double[numberLayers];
             n2oatm = new double[numberLayers];
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            double[] source = sourceSolute.kgha;
            int numLayers = source.Length;

            double[] destination = null;
            if (destinationName != null)
                destination = destinationSolute.kgha;

            for (int i = 0; i < numLayers; i++)
            {
                double nitrogenFlow = 0;
                if (source[i] > 0)
                    nitrogenFlow = rate.Value(i) * source[i];

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
