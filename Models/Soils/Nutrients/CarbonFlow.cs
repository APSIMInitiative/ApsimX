using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// Encapsulates a carbon and nutrient flow between pools.  This flow is characterised in terms of the rate of flow (fraction of the pool per day).  Carbon loss as CO2 is expressed in terms of the efficiency of C retension within the soil.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(NutrientPool))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class CarbonFlow : Model
    {
        private NutrientPool[] destinations;
        private double[] carbonFlowToDestination;
        private double[] nitrogenFlowToDestination;
        private double[] phosphorusFlowToDestination;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction Rate = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction CO2Efficiency = null;

        [Link(ByName = true)]
        ISolute NO3 = null;

        [Link(ByName = true)]
        ISolute NH4 = null;

        [Link(ByName = true, IsOptional = true)]
        ISolute LabileP = null;

        /// <summary>
        /// Net N Mineralisation
        /// </summary>
        public double[] MineralisedN { get; set; }

        /// <summary>
        /// Net N Mineralisation
        /// </summary>
        public double[] MineralisedP { get; set; }

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
            MineralisedP = new double[(Parent as NutrientPool).C.Length];
            Catm = new double[(Parent as NutrientPool).C.Length];
            carbonFlowToDestination = new double[destinations.Length];
            nitrogenFlowToDestination = new double[destinations.Length];
            phosphorusFlowToDestination = new double[destinations.Length];
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

            int numLayers = source.C.Length;

            double[] no3 = NO3.kgha;
            double[] nh4 = NH4.kgha;
            double[] labileP;
            if (LabileP == null)
                labileP = new double[numLayers];
            else
                labileP = LabileP.kgha;

            int numDestinations = destinations.Length;
            for (int i = 0; i < numLayers; i++)
            {

                double carbonFlowFromSource = Rate.Value(i) * source.C[i];
                double nitrogenFlowFromSource = MathUtilities.Divide(carbonFlowFromSource * source.N[i], source.C[i], 0);
                double phosphorusFlowFromSource = MathUtilities.Divide(carbonFlowFromSource * source.P[i], source.C[i], 0);

                double totalNitrogenFlowToDestinations = 0;
                double totalPhosphorusFlowToDestinations = 0;

                double co2Efficiency = CO2Efficiency.Value(i);
                for (int j = 0; j < numDestinations; j++)
                {
                    var destination = destinations[j];
                    carbonFlowToDestination[j] = carbonFlowFromSource * co2Efficiency * destinationFraction[j];
                    nitrogenFlowToDestination[j] = MathUtilities.Divide(carbonFlowToDestination[j] * destination.N[i], destination.C[i], 0.0);
                    totalNitrogenFlowToDestinations += nitrogenFlowToDestination[j];
                    phosphorusFlowToDestination[j] = MathUtilities.Divide(carbonFlowToDestination[j] * destination.P[i], destination.C[i], 0.0);
                    totalPhosphorusFlowToDestinations += phosphorusFlowToDestination[j];

                }

                // some pools do not fully occupy a layer (e.g. residue decomposition) and so need to incorporate fraction of layer
                double mineralNSupply = (no3[i] + nh4[i]) * source.LayerFraction[i];
                double nSupply = nitrogenFlowFromSource + mineralNSupply;
                double mineralPSupply = labileP[i] * source.LayerFraction[i];
                double pSupply = phosphorusFlowFromSource + mineralPSupply;

                double SupplyFactor = 1;

                if (totalNitrogenFlowToDestinations > nSupply)
                    SupplyFactor = MathUtilities.Bound(MathUtilities.Divide(mineralNSupply, totalNitrogenFlowToDestinations - nitrogenFlowFromSource, 1.0), 0.0, 1.0);
                if (totalPhosphorusFlowToDestinations > pSupply)
                {
                    double pSupplyFactor = MathUtilities.Bound(MathUtilities.Divide(mineralPSupply, totalPhosphorusFlowToDestinations - phosphorusFlowFromSource, 1.0), 0.0, 1.0);
                    // ALERT
                    pSupplyFactor = 1; // remove P constraint until P model fully operational
                    SupplyFactor = Math.Min(SupplyFactor, pSupplyFactor);
                }

                if (SupplyFactor < 1)
                {
                    for (int j = 0; j < numDestinations; j++)
                    {
                        carbonFlowToDestination[j] *= SupplyFactor;
                        nitrogenFlowToDestination[j] *= SupplyFactor;
                        phosphorusFlowToDestination[j] *= SupplyFactor;
                    }
                    totalNitrogenFlowToDestinations *= SupplyFactor;
                    totalPhosphorusFlowToDestinations *= SupplyFactor;

                    carbonFlowFromSource *= SupplyFactor;
                    nitrogenFlowFromSource *= SupplyFactor;
                    phosphorusFlowFromSource *= SupplyFactor;

                }

                source.C[i] -= carbonFlowFromSource;
                source.N[i] -= nitrogenFlowFromSource;
                source.P[i] -= phosphorusFlowFromSource;

                Catm[i] = carbonFlowFromSource - carbonFlowToDestination.Sum();
                for (int j = 0; j < numDestinations; j++)
                {
                    destinations[j].C[i] += carbonFlowToDestination[j];
                    destinations[j].N[i] += nitrogenFlowToDestination[j];
                    destinations[j].P[i] += phosphorusFlowToDestination[j];
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

                if (totalPhosphorusFlowToDestinations <= phosphorusFlowFromSource)
                {
                    MineralisedP[i] = phosphorusFlowFromSource - totalPhosphorusFlowToDestinations;
                    labileP[i] += MineralisedP[i];
                }
                else
                {
                    double PDeficit = totalPhosphorusFlowToDestinations - phosphorusFlowFromSource;
                    double PImmobilisation = Math.Min(labileP[i], PDeficit);
                    labileP[i] -= PImmobilisation;
                    PDeficit -= PImmobilisation;
                    MineralisedP[i] = -PImmobilisation;

                    // ALERT
                    //if (MathUtilities.IsGreaterThan(PDeficit, 0.0))
                    //    throw new Exception("Insufficient mineral P for immobilisation demand for C flow " + Name);
                }


            }
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document()
        {
            // Write Phase Table
            yield return new Paragraph($"**Destination of C from {Name}**");
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

            yield return new Table(tableData);

            foreach (ITag tag in DocumentChildren<Memo>())
                yield return tag;
        }
    }
}
