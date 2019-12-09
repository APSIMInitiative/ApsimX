
namespace Models.Soils.Nutrients
{
    using Core;
    using Models.Functions;
    using System;
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using Interfaces;
    using System.Data;
    /// <summary>
    /// # [Name]
    /// Encapsulates a carbon and nutrient flow between pools.  This flow is characterised in terms of the rate of flow (fraction of the pool per day).  Carbon loss as CO2 is expressed in terms of the efficiency of C retension within the soil.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(NutrientPool))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.GridView")]
    public class CarbonFlow : Model, ICustomDocumentation
    {
        private List<NutrientPool> destinations = new List<NutrientPool>();

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
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (string destinationName in destinationNames)
            {
                NutrientPool destination = Apsim.Find(this, destinationName) as NutrientPool;
                if (destination == null)
                    throw new Exception("Cannot find destination pool with name: " + destinationName);
                destinations.Add(destination);
            }
            MineralisedN = new double[(Parent as NutrientPool).C.Length];
            Catm = new double[(Parent as NutrientPool).C.Length];
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

            for (int i = 0; i < source.C.Length; i++)
            {

                double carbonFlowFromSource = Rate.Value(i) * source.C[i];
                double nitrogenFlowFromSource = MathUtilities.Divide(carbonFlowFromSource, source.CNRatio[i], 0);

                double[] carbonFlowToDestination = new double[destinations.Count];
                double[] nitrogenFlowToDestination = new double[destinations.Count];

                for (int j = 0; j < destinations.Count; j++)
                {
                    carbonFlowToDestination[j] = carbonFlowFromSource * CO2Efficiency.Value(i) * destinationFraction[j];
                    nitrogenFlowToDestination[j] = MathUtilities.Divide(carbonFlowToDestination[j], destinations[j].CNRatio[i], 0.0);
                }

                double TotalNitrogenFlowToDestinations = MathUtilities.Sum(nitrogenFlowToDestination);
                // some pools do not fully occupy a layer (e.g. residue decomposition) and so need to incorporate fraction of layer
                double MineralNSupply = (NO3.kgha[i] + NH4.kgha[i]) * source.LayerFraction[i];
                double NSupply = nitrogenFlowFromSource + MineralNSupply;

                if (TotalNitrogenFlowToDestinations > NSupply)
                {
                    double NSupplyFactor = MathUtilities.Bound(MathUtilities.Divide(MineralNSupply, TotalNitrogenFlowToDestinations - nitrogenFlowFromSource, 1.0), 0.0, 1.0);

                    for (int j = 0; j < destinations.Count; j++)
                    {
                        carbonFlowToDestination[j] *= NSupplyFactor;
                        nitrogenFlowToDestination[j] *= NSupplyFactor;
                    }
                    TotalNitrogenFlowToDestinations *= NSupplyFactor;

                    carbonFlowFromSource *= NSupplyFactor;
                    nitrogenFlowFromSource *= NSupplyFactor;

                }

                source.C[i] -= carbonFlowFromSource;
                source.N[i] -= nitrogenFlowFromSource;
                Catm[i] = carbonFlowFromSource - MathUtilities.Sum(carbonFlowToDestination);
                for (int j = 0; j < destinations.Count; j++)
                {
                    destinations[j].C[i] += carbonFlowToDestination[j];
                    destinations[j].N[i] += nitrogenFlowToDestination[j];
                }

                if (TotalNitrogenFlowToDestinations <= nitrogenFlowFromSource)
                {
                    MineralisedN[i] = nitrogenFlowFromSource - TotalNitrogenFlowToDestinations;
                    NH4.kgha[i] += MineralisedN[i];
                }
                else
                {
                    double NDeficit = TotalNitrogenFlowToDestinations - nitrogenFlowFromSource;
                    double NH4Immobilisation = Math.Min(NH4.kgha[i], NDeficit);
                    NH4.kgha[i] -= NH4Immobilisation;
                    NDeficit -= NH4Immobilisation;

                    double NO3Immobilisation = Math.Min(NO3.kgha[i], NDeficit);
                    NO3.kgha[i] -= NO3Immobilisation;
                    NDeficit -= NO3Immobilisation;

                    MineralisedN[i] = -NH4Immobilisation - NO3Immobilisation;

                    if (MathUtilities.IsGreaterThan(NDeficit, 0.0))
                        throw new Exception("Insufficient mineral N for immobilisation demand for C flow " + Name);
                }

            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // Write Phase Table
                tags.Add(new AutoDocumentation.Paragraph("**Destination of C from " + this.Name + "**", indent));
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

                tags.Add(new AutoDocumentation.Table(tableData, indent));

                // write remaining children
                foreach (IModel memo in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

            }
        }

    }
}