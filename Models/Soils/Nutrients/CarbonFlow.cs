
namespace Models.Soils.Nutrients
{
    using Core;
    using Models.Functions;
    using System;
    using APSIM.Services.Documentation;
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// # [Name]
    /// Encapsulates a carbon and nutrient flow between pools.  This flow is characterised in terms of the rate of flow (fraction of the pool per day).  Carbon loss as CO2 is expressed in terms of the efficiency of C retension within the soil.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(NutrientPool))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class CarbonFlow : Model, ICustomDocumentation
    {
        private NutrientPool[] destinations;
        private double[] carbonFlowToDestination;
        private double[] nitrogenFlowToDestination;


        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction Rate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction CO2Efficiency = null;

        [Link(ByName = true)]
        ISolute NO3 = null;

        [Link(ByName = true)]
        ISolute NH4 = null;


        /// <summary>
        /// Net N Mineralisation
        /// </summary>
        public double[] MineralisedN { get; set; }

        /// <summary>
        /// CO2 lost to the atmosphere
        /// </summary>
        public double[] Catm { get; set; }

        /// <summary>
        /// Name of destination pool
        /// </summary>
        [Description("Names of destination pools (CSV)")]
        public string[] destinationNames { get; set; }
        /// <summary>
        /// Fractions for each destination pool
        /// </summary>
        [Description("Fractions of flow to each pool (CSV)")]
        public double[] destinationFraction { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            destinations = new NutrientPool[destinationNames.Length];

            for (int i = 0; i < destinationNames.Length; i++)
            {
                NutrientPool destination = FindInScope<NutrientPool>(destinationNames[i]);
                if (destination == null)
                    throw new Exception("Cannot find destination pool with name: " + destinationNames[i]);
                destinations[i] = destination;
            }
            MineralisedN = new double[(Parent as NutrientPool).C.Length];
            Catm = new double[(Parent as NutrientPool).C.Length];
            carbonFlowToDestination = new double[destinations.Length];
            nitrogenFlowToDestination = new double[destinations.Length];

        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            NutrientPool source = Parent as NutrientPool;

            double[] no3 = NO3.kgha;
            double[] nh4 = NH4.kgha;
            int numLayers = source.C.Length;
            int numDestinations = destinations.Length;
            for (int i = 0; i < numLayers; i++)
            {

                double carbonFlowFromSource = Rate.Value(i) * source.C[i];
                double nitrogenFlowFromSource = MathUtilities.Divide(carbonFlowFromSource * source.N[i], source.C[i], 0);

                double totalNitrogenFlowToDestinations = 0;
                double co2Efficiency = CO2Efficiency.Value(i);
                for (int j = 0; j < numDestinations; j++)
                {
                    var destination = destinations[j];
                    carbonFlowToDestination[j] = carbonFlowFromSource * co2Efficiency * destinationFraction[j];
                    nitrogenFlowToDestination[j] = MathUtilities.Divide(carbonFlowToDestination[j] * destination.N[i], destination.C[i], 0.0);
                    totalNitrogenFlowToDestinations += nitrogenFlowToDestination[j];
                }

                // some pools do not fully occupy a layer (e.g. residue decomposition) and so need to incorporate fraction of layer
                double mineralNSupply = (no3[i] + nh4[i]) * source.LayerFraction[i];
                double nSupply = nitrogenFlowFromSource + mineralNSupply;

                if (totalNitrogenFlowToDestinations > nSupply)
                {
                    double NSupplyFactor = MathUtilities.Bound(MathUtilities.Divide(mineralNSupply, totalNitrogenFlowToDestinations - nitrogenFlowFromSource, 1.0), 0.0, 1.0);

                    for (int j = 0; j < numDestinations; j++)
                    {
                        carbonFlowToDestination[j] *= NSupplyFactor;
                        nitrogenFlowToDestination[j] *= NSupplyFactor;
                    }
                    totalNitrogenFlowToDestinations *= NSupplyFactor;

                    carbonFlowFromSource *= NSupplyFactor;
                    nitrogenFlowFromSource *= NSupplyFactor;

                }

                source.C[i] -= carbonFlowFromSource;
                source.N[i] -= nitrogenFlowFromSource;
                Catm[i] = carbonFlowFromSource - carbonFlowToDestination.Sum();
                for (int j = 0; j < numDestinations; j++)
                {
                    destinations[j].C[i] += carbonFlowToDestination[j];
                    destinations[j].N[i] += nitrogenFlowToDestination[j];
                }

                if (totalNitrogenFlowToDestinations <= nitrogenFlowFromSource)
                {
                    MineralisedN[i] = nitrogenFlowFromSource - totalNitrogenFlowToDestinations;
                    nh4[i] += MineralisedN[i];
                }
                else
                {
                    double NDeficit = totalNitrogenFlowToDestinations - nitrogenFlowFromSource;
                    double NH4Immobilisation = Math.Min(nh4[i], NDeficit);
                    nh4[i] -= NH4Immobilisation;
                    NDeficit -= NH4Immobilisation;

                    double NO3Immobilisation = Math.Min(no3[i], NDeficit);
                    no3[i] -= NO3Immobilisation;
                    NDeficit -= NO3Immobilisation;

                    MineralisedN[i] = -NH4Immobilisation - NO3Immobilisation;

                    if (MathUtilities.IsGreaterThan(NDeficit, 0.0))
                        throw new Exception("Insufficient mineral N for immobilisation demand for C flow " + Name);
                }

            }
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        public override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            // Add a heading.
            yield return new Heading(Name, indent, headingLevel);

            // Write Phase Table
            yield return new Paragraph($"**Destination of C from {Name}**", indent);
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Destination Pool", typeof(string));
            tableData.Columns.Add("Carbon Fraction", typeof(string));

            if (destinationNames != null)
                for (int j = 0; j < destinationNames.Length; j++)
                {
                    DataRow row = tableData.NewRow();
                    row[0] = destinationNames[j];
                    row[1] = destinationFraction[j].ToString();
                    tableData.Rows.Add(row);
                }

            yield return new Table(tableData, indent);
        }
    }
}
