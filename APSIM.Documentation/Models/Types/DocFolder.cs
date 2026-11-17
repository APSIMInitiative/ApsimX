using System.Collections.Generic;
using APSIM.Documentation.Tag;
using Models.Core;
using System.Linq;
using Models;
using Models.Storage;
using Models.Factorial;
using System.Data;
using System;
using ModelsGraph = Models.Graph;
using ModelsGraphPage = Models.GraphPage;
using ModelsMap = Models.Map;
using Markdig;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocFolder : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocFolder(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            if (section.Children.Count >= 2)
            {
                section.Children.RemoveAt(1); //remove Folder Summary from C#
                section.Children.RemoveAt(0); //remove Folder link
            }

            foreach(ModelsMap map in model.Node.FindChildren<ModelsMap>().Where(map => map.Enabled))
                section.Add(AutoDocumentation.DocumentModel(map));

            // Write experiment descriptions. We don't call experiment.Document() here,
            // because we want to just show the experiment design (a string) and put it
            // inside a table cell.
            IEnumerable<Experiment> experiments = model.Node.FindChildren<Experiment>().Where(experiment => experiment.Enabled);
            if (experiments.Any())
            {
                List<ITag> experimentsTag = new List<ITag>();
                DataTable table = new DataTable();
                table.Columns.Add("Experiment Name", typeof(string));
                table.Columns.Add("Design (Number of Treatments)", typeof(string));

                foreach (Experiment experiment in experiments)
                {
                    DataRow row = table.NewRow();
                    row[0] = experiment.Name;
                    row[1] = experiment.GetDesign();
                    table.Rows.Add(row);
                }
                experimentsTag.Add(new Table(table, model));
                section.Add(experimentsTag);
            }

            if (DocumentationSettings.GenerateGraphs)
                section.Add(GetChildGraphs(model, false));

            // Document graphs under a experiment
            foreach (Experiment exp in model.Node.FindChildren<Experiment>().Where(f => f.Enabled))
            {
                foreach (Memo memo in exp.Node.FindChildren<Memo>())
                    section.Add(AutoDocumentation.DocumentModel(memo));

                if (DocumentationSettings.GenerateGraphs)
                    section.Add(GetChildGraphs(exp, true));
            }

            // Document graphs under a simulation
            foreach (Simulation sim in model.Node.FindChildren<Simulation>().Where(f => f.Enabled))
            {
                foreach (Memo memo in sim.Node.FindChildren<Memo>())
                    section.Add(AutoDocumentation.DocumentModel(memo));

                if (DocumentationSettings.GenerateGraphs)
                    section.Add(GetChildGraphs(sim, true));
            }

            // Document child folders.
            foreach (Folder folder in model.Node.FindChildren<Folder>().Where(f => f.Enabled))
                section.Add(AutoDocumentation.DocumentModel(folder));

            return new List<ITag>() {section};
        }

        /// <summary>
        /// Gets child graphs from a folder model.
        /// </summary>
        /// <returns></returns>
        public List<ITag> GetChildGraphs(IModel model, bool recurse)
        {
            List<ModelsGraph> childGraphs = model.Node.FindChildren<ModelsGraph>(recurse: recurse).Where(f => f.Enabled).ToList();

            List<string> parents = new List<string>();
            foreach(ModelsGraph graph in childGraphs)
            {
                string path = graph.Node.Parent.FullNameAndPath;
                if (!parents.Contains(path))
                    parents.Add(path);
            }

            List<ITag> output = new List<ITag>();
            foreach(string parent in parents)
            {
                List<ITag> graphSections = new List<ITag>();

                foreach(ModelsGraph graph in childGraphs)
                {
                    if (graph.Node.Parent.FullNameAndPath == parent)
                    {
                        bool hide = graph.Node.FindParents<Folder>().Any(a => !a.ShowInDocs);
                        if (!hide)
                            graphSections.AddRange(AutoDocumentation.Document(graph));
                    }
                }

                List<ITag> graphs = new List<ITag>();
                if (graphSections.Count() > 0)
                {
                    List<ITag> justTheGraphs = new List<ITag>();
                    foreach(ITag graphSection in graphSections)
                        if (graphSection is Section gs)
                            foreach(ITag child in gs.Children)
                                if (child is APSIM.Documentation.Tag.Graph graph)
                                    justTheGraphs.Add(graph);

                    string markdown = WebDocs.ConvertToMarkdown(justTheGraphs, "", model, model);

                    MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    string html = Markdown.ToHtml(markdown, pipeline);
                    html = html.Replace("<p>", "");
                    html = html.Replace("</p>", "");

                    html = WebDocs.AddCSSClasses(html);
                    html = html.Replace("<img ", "<img style=\"width:40%;float:left;padding:0px;padding-left:6%;padding-right:4%\" ");

                    html = $"<div class=\"docs-table-container\">\n{html}</div>\n";

                    graphs.Add(new Paragraph(html, model));
                }
                if (graphs.Count() > 0)
                {
                    string title = parent.Substring(parent.LastIndexOf('.')+1);
                    output.Add(new Section(title, graphs, model));
                }
            }
            return output;
        }

    }


}
