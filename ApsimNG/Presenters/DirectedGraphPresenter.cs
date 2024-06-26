using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Nutrients;
using System.Drawing;
using System.IO;
using System.Linq;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This presenter connects an instance of a Model with a 
    /// UserInterface.Views.DrawingView
    /// </summary>
    public class DirectedGraphPresenter : IPresenter, IExportable
    {
        /// <summary>The view object</summary>
        private IVisualiseAsDirectedGraph model;

        /// <summary>The view object</summary>
        private DirectedGraphView view;
        
        /// <summary>The explorer presenter used</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view for this presenter</param>
        /// <param name="explorerPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = view as DirectedGraphView;
            this.explorerPresenter = explorerPresenter;
            this.model = model as IVisualiseAsDirectedGraph;

            // Tell the view to populate the axis.
            this.PopulateView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            model.DirectedGraphInfo = view.DirectedGraph;
        }

        /// <summary>Export the view object to a file and return the file name</summary>
        public string ExportToPNG(string folder)
        {
            Gdk.Pixbuf image = view.Export();

            
            string fileName = Path.ChangeExtension(Path.Combine(folder, Path.GetRandomFileName()), ".png");
            image.Save(fileName, "png");

            return fileName;
        }

        /// <summary>Populate the view object</summary>
        private void PopulateView()
        {
            CalculateDirectedGraph();
            if (model.DirectedGraphInfo != null)
                view.DirectedGraph = model.DirectedGraphInfo;
        }

        /// <summary>Calculate / create a directed graph from model</summary>
        public void CalculateDirectedGraph()
        {
            DirectedGraph oldGraph = model.DirectedGraphInfo;
            if (model.DirectedGraphInfo == null)
                model.DirectedGraphInfo = new DirectedGraph();

            model.DirectedGraphInfo.Begin();
            /*
            bool needAtmosphereNode = false;
            
            IModel nutrient = model as IModel;
            foreach (OrganicPool pool in nutrient.FindAllInScope<OrganicPool>())
            {
                Point location = default(Point);
                Node oldNode;
                if (oldGraph != null && pool.Name != null && (oldNode = oldGraph.Nodes.Find(f => f.Name == pool.Name)) != null)
                    location = oldNode.Location;
                model.DirectedGraphInfo.AddNode(view.DirectedGraph.NextNodeID(), pool.Name, ColourUtilities.ChooseColour(3), Color.Black, location);

                foreach (OrganicFlow cFlow in pool.FindAllChildren<OrganicFlow>())
                {
                    foreach (string destinationName in cFlow.DestinationNames)
                    {
                        string destName = destinationName;
                        if (destName == null)
                        {
                            destName = "Atmosphere";
                            needAtmosphereNode = true;
                        }

                        location = default(Point);
                        Arc oldArc;
                        if (oldGraph != null && pool.Name != null && (oldArc = oldGraph.Arcs.Find(f => f.SourceName == pool.Name && f.DestinationName == destName)) != null)
                            location = oldArc.Location;

                        model.DirectedGraphInfo.AddArc(view.DirectedGraph.NextArcID(), null, pool.Name, destName, Color.Black, location);

                    }
                }
            }

            foreach (Solute solute in nutrient.FindAllInScope<ISolute>())
            {
                Point location = new Point(0, 0);
                Node oldNode;
                if (oldGraph != null && solute.Name != null && (oldNode = oldGraph.Nodes.Find(f => f.Name == solute.Name)) != null)
                    location = oldNode.Location;
                model.DirectedGraphInfo.AddNode(view.DirectedGraph.NextNodeID(), solute.Name, ColourUtilities.ChooseColour(2), Color.Black, location);
                foreach (NFlow nitrogenFlow in nutrient.FindAllChildren<NFlow>().Where(flow => flow.SourceName == solute.Name))
                {
                    string destName = nitrogenFlow.DestinationName;
                    if (destName == null)
                    {
                        destName = "Atmosphere";
                        needAtmosphereNode = true;
                    }
                    location = default(Point);
                    Arc oldArc;
                    if (oldGraph != null && solute.Name != null && (oldArc = oldGraph.Arcs.Find(f => f.SourceName == solute.Name && f.DestinationName == destName)) != null)
                        location = oldArc.Location;

                    model.DirectedGraphInfo.AddArc(view.DirectedGraph.NextArcID(), null, nitrogenFlow.SourceName, destName, Color.Black, location);
                }
                
        }

            if (needAtmosphereNode)
                model.DirectedGraphInfo.AddTransparentNode("Atmosphere");


            model.DirectedGraphInfo.End();
            */
        }

    }
}
