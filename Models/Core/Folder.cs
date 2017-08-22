using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Factorial;
using Models.PMF.Interfaces;
using Models.Graph;

namespace Models.Core
{
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
    public class Folder : Model
    {
        /// <summary>Show page of graphs?</summary>
        public bool ShowPageOfGraphs { get; set; }

        /// <summary>Constructor</summary>
        public Folder()
        {
            ShowPageOfGraphs = true;
        }
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            if (ShowPageOfGraphs)
            {
                foreach (Memo memo in Apsim.Children(this, typeof(Memo)))
                    memo.Document(tags, headingLevel, indent);

                if (Apsim.Children(this, typeof(Experiment)).Count > 0)
                {
                    // Write Phase Table
                    tags.Add(new AutoDocumentation.Paragraph("**List of experiments.**", indent));
                    string line;
                    line = "|Experiment Name | Design (Number of Treatments) |" + System.Environment.NewLine;
                    line += "|--------------|:----------------|" + System.Environment.NewLine;

                    foreach (IModel child in Apsim.Children(this, typeof(Experiment)))
                    {
                        IModel Factors = Apsim.Child(child, typeof(Factors));
                        string Design = "";
                        foreach (IModel factor in Apsim.Children(Factors, typeof(Factor)))
                        {
                            if (Design != "")
                                Design += " x ";
                            Design += factor.Name;
                        }
                        Design += " (" + (child as Experiment).Names().Length+")";

                        line += "|" + child.Name + "|" + Design +"  |" + System.Environment.NewLine;
                    }
                    tags.Add(new AutoDocumentation.Paragraph(line, indent));

                }
                int pageNumber = 1;
                int i = 0;
                List<IModel> children = Apsim.Children(this, typeof(Graph.Graph));
                while (i < children.Count)
                {
                    GraphPage page = new GraphPage();
                    page.name = Name + pageNumber;
                    for (int j = i; j < i + 6 && j < children.Count; j++)
                        page.graphs.Add(children[j] as Graph.Graph);
                    tags.Add(page);
                    i += 6;
                    pageNumber++;
                }
            }
        }

    }
}
