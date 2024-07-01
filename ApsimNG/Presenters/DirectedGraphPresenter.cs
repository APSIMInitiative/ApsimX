using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Nutrients;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
            model.DirectedGraphInfo.Clear();
            bool hasAtmosphereNode = false;
            
            IModel nutrient = model as IModel;
            foreach (OrganicPool pool in nutrient.FindAllInScope<OrganicPool>())
            {
                Node node = new Node();
                node.Name = pool.Name;
                node.Colour = ColourUtilities.ChooseColour(3);
                node.Location = getNodePosition(pool.Name);
                model.DirectedGraphInfo.AddNode(node);
            }

            foreach (OrganicPool pool in nutrient.FindAllInScope<OrganicPool>())
            {
                foreach (OrganicFlow cFlow in pool.FindAllChildren<OrganicFlow>())
                {
                    foreach (string destinationName in cFlow.DestinationNames)
                    {
                        string destName = destinationName;
                        if (destName == null)
                        {
                            destName = "Atmosphere";
                            if (!hasAtmosphereNode) {
                                hasAtmosphereNode = true;
                                createAtmosphereNode();
                            }
                        }

                        Arc arc = new Arc();
                        arc.Name = "";
                        arc.Source = model.DirectedGraphInfo.GetNodeByName(pool.Name);
                        arc.Destination = model.DirectedGraphInfo.GetNodeByName(destinationName);
                        arc.SourceID = arc.Source.ID;
                        arc.DestinationID = arc.Destination.ID;
                        arc.Conditions = new List<string>();
                        arc.Actions = new List<string>();
                        arc.BezierPoints = new List<System.Drawing.Point>();
                        arc.Location = new Point((arc.Destination.Location.X + arc.Source.Location.X) / 2, (arc.Destination.Location.Y + arc.Source.Location.Y) / 2);
                        model.DirectedGraphInfo.AddArc(arc);
                    }
                }
            }

            foreach (Solute solute in nutrient.FindAllInScope<ISolute>())
            {
                Node node = new Node();
                node.Name = solute.Name;
                node.Colour = ColourUtilities.ChooseColour(2);
                node.Location = getNodePosition(solute.Name);
                model.DirectedGraphInfo.AddNode(node);
            }
            foreach (Solute solute in nutrient.FindAllInScope<ISolute>()) {
                foreach (NFlow nitrogenFlow in nutrient.FindAllChildren<NFlow>().Where(flow => flow.SourceName == solute.Name)) {
                    {
                        string destName = nitrogenFlow.DestinationName;
                        if (destName == null)
                        {
                            destName = "Atmosphere";
                            if (!hasAtmosphereNode) {
                                hasAtmosphereNode = true;
                                createAtmosphereNode();
                            }
                        }

                        Arc arc = new Arc();
                        arc.Name = "";
                        arc.Source = model.DirectedGraphInfo.GetNodeByName(solute.Name);
                        arc.Destination = model.DirectedGraphInfo.GetNodeByName(destName);
                        arc.SourceID = arc.Source.ID;
                        arc.DestinationID = arc.Destination.ID;
                        arc.Conditions = new List<string>();
                        arc.Actions = new List<string>();
                        arc.BezierPoints = new List<System.Drawing.Point>();
                        arc.Location = new Point((arc.Destination.Location.X + arc.Source.Location.X) / 2, (arc.Destination.Location.Y + arc.Source.Location.Y) / 2);
                        model.DirectedGraphInfo.AddArc(arc);
                    }
                }
            }
        }

        /// <summary>Calculate / create a directed graph from model</summary>
        private void createAtmosphereNode()
        {
            Node node = new Node();
            node.Name = "Atmosphere";
            node.Colour = ColourUtilities.ChooseColour(1);
            node.Location = getNodePosition(node.Name);
            model.DirectedGraphInfo.AddNode(node);
        }

        private Point getNodePosition(string name)
        {
            int spacing = 250;
            int row1 = 100;
            int row2 = row1 + spacing;
            int row3 = row2 + spacing;

            int col1 = 100;
            int col2 = col1 + spacing;
            int col3 = col2 + spacing;
            int col4 = col3 + spacing;
            int col5 = col4 + spacing;
            int col6 = col5 + spacing;

            if (name.CompareTo("Inert") == 0) 
                return new Point(col4, row1);
            else if (name.CompareTo("FOMLignin") == 0) 
                return new Point(col2, row1);
            else if (name.CompareTo("FOMCellulose") == 0) 
                return new Point(col3, row1);
            else if (name.CompareTo("FOMCarbohydrate") == 0) 
                return new Point(col1, row1);
            else if (name.CompareTo("Microbial") == 0) 
                return new Point(col1, row2);
            else if (name.CompareTo("Humic") == 0) 
                return new Point(col3, row2);
            else if (name.CompareTo("SurfaceResidue") == 0) 
                return new Point(col2, row3);
            else if (name.CompareTo("NO3") == 0) 
                return new Point(col5, row2);
            else if (name.CompareTo("NH4") == 0) 
                return new Point(col6, row3);
            else if (name.CompareTo("Urea") == 0) 
                return new Point(col5, row3);
            else if (name.CompareTo("Atmosphere") == 0) 
                return new Point(col5, row1);
            else
                return new Point(0,0);
        }
    }
}
