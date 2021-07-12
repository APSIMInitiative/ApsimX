namespace Models.Core
{
    using Models.Core.Run;
    using Models.Factorial;
    using Models;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using System;
    using APSIM.Services.Documentation;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// A folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [ScopedModel]
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class Folder : Model
    {
        /// <summary>Show page of graphs?</summary>
        public bool ShowPageOfGraphs { get; set; }

        /// <summary>Constructor</summary>
        public Folder()
        {
            ShowPageOfGraphs = true;
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> GetTags()
        {
            if (ShowPageOfGraphs)
            {
                if (FindAllChildren<Experiment>().Any())
                {
                    // Write Phase Table.
                    yield return new Paragraph("**List of experiments.**");
                    DataTable table = new DataTable();
                    table.Columns.Add("Experiment Name", typeof(string));
                    table.Columns.Add("Design (Number of Treatments)", typeof(string));

                    foreach (IModel child in FindAllChildren<Experiment>())
                    {
                        Factors Factors = child.FindChild<Factors>();
                        string design = GetTreatmentDescription(Factors);
                        foreach (Permutation permutation in Factors.FindAllChildren<Permutation>())
                            design += GetTreatmentDescription(permutation);

                        var simulationNames = (child as Experiment).GenerateSimulationDescriptions().Select(s => s.Name);
                        design += " (" + simulationNames.ToArray().Length + ")";

                        DataRow row = table.NewRow();
                        row[0] = child.Name;
                        row[1] = design;
                        table.Rows.Add(row);
                    }
                    yield return new Table(table);
                }
                var children = FindAllChildren<Models.Graph>().ToList();
                int graphsPerPage = 6;
                var graphs = new List<APSIM.Services.Documentation.Graph>();
                for (int i = 0; i < children.Count; i++)
                {
                    graphs.Add(children[i].ToGraph());
                    if (graphs.Count == graphsPerPage)
                    {
                        yield return new GraphPage(graphs);
                        graphs.Clear();
                    }
                }
                if (graphs.Count > 0)
                    yield return new GraphPage(graphs);
                graphs.Clear();
            }
        }

        private string GetTreatmentDescription(IModel factors)
        {
            string design = "";
            foreach (Factor factor in factors.FindAllChildren<Factor>())
            {
                if (design != "")
                    design += " x ";
                design += factor.Name;
            }
            return design;
        }
    }
}
