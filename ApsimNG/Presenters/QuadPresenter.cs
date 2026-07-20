using Models.Core;
using UserInterface.Views;
using System.Collections.Generic;
using Models.Functions;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.WaterModel;
using Models.Factorial;
using UserInterface.Commands;
using System;
using UserInterface.EventArguments;
using System.Linq;

namespace UserInterface.Presenters
{
    /// <summary>A generic presenter displaying four boxes of info with grid, graph, text and properties</summary>
    public class QuadPresenter : IPresenter
    {
        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The base view.</summary>
        private QuadView view = null;

        /// <summary>The model.</summary>
        private IModel model;

        private bool hasSuccessfullyBuiltPresenters = false;

        /// <summary>Sub-presenters that are added to this presenter</summary>
        private List<ISubPresenter> presenters;

        /// <summary>Default constructor</summary>
        public QuadPresenter() {}

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="v">View to work with, must be a QuadView</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = v as QuadView;
            this.explorerPresenter = explorerPresenter;
            this.presenters = new List<ISubPresenter>();

            if (this.view == null)
                throw new System.Exception("QuadPresenter only works with a QuadView");

            Refresh();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            DestroyPresenters();
            view.Dispose();
        }

        /// <summary>Refresh this presenter and all sub presenters</summary>
        public void Refresh()
        {
            DisconnectEvents();

            if (!hasSuccessfullyBuiltPresenters)
                CreatePresenters();

            List<Exception> errors = new List<Exception>();
            if (model is FactorFromFile factorFromFile)
            {
                try { factorFromFile.GetCompositeFactors(); }
                catch (Exception exception) { errors.Add(exception); }
            }

            foreach (ISubPresenter presenter in presenters)
            {
                try
                {
                    presenter.Refresh();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }
            
            try
            {
                view.Refresh();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            if (errors.Count > 0)
                explorerPresenter.MainPresenter.ShowError(errors, overwrite:true);

            ConnectEvents();
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            foreach (ISubPresenter presenter in presenters)
            {
                presenter.ConnectEvents();
                if (presenter is GridPresenter grid)
                    grid.CellChanged += OnCellChanged;
                if (presenter is EditorPresenter editor)
                    editor.TextChanged += OnTextChanged;
                if (presenter is ListPresenter list)
                    list.SelectionChanged += OnListSelectionChanged;
            }

            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
            foreach (ISubPresenter presenter in presenters)
            {
                presenter.DisconnectEvents();
                if (presenter is GridPresenter grid)
                    grid.CellChanged -= OnCellChanged;
                if (presenter is EditorPresenter editor)
                    editor.TextChanged -= OnTextChanged;
                if (presenter is ListPresenter list)
                    list.SelectionChanged -= OnListSelectionChanged;
            }
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Destroys any existing presenters and rebuilds everything depending 
        /// on the model type.
        /// </summary>
        private void CreatePresenters()
        {
            try
            {
                DestroyPresenters();

                if (model is XYPairs)
                    CreateLayoutXYPairs();
                else if (model is Physical)
                    CreateLayoutPhysical();
                else if (model is WaterBalance)
                    CreateLayoutWaterBalance();
                else if (model is CompositeFactor)
                    CreateLayoutCompositeFactor();
                else if (model is FactorFromFile)
                    CreateLayoutFactorFromFile();
                else
                    CreateLayoutGeneric();
                hasSuccessfullyBuiltPresenters = true;
            }
            catch (Exception exception)
            {
                explorerPresenter.MainPresenter.ShowError(exception, overwrite:true);
            }
        }

        /// <summary>
        /// Destroys all the created presenters by detatching them
        /// </summary>
        private void DestroyPresenters()
        {
            foreach (ISubPresenter presenter in presenters)
            {
                if (presenter is GridPresenter grid)
                    grid.Detach();
                else if (presenter is PropertyPresenter properties)
                    properties.Detach();
                else if (presenter is QuadGraphPresenter graph)
                    graph.Detach();
                if (presenter is ListPresenter list)
                    list.Detach();
            }
        }

        /// <summary>
        /// Listener for if hte model is changed (most likely by a sub presenter)
        /// When this happens, it just tells all the presenters to refresh
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            model = changedModel as IModel;
            Refresh();
        }

        /// <summary>
        /// Listener for Grid cell change events.
        /// Does not use given parameters, just refreshes the presenters
        /// </summary>
        /// <param name="dataProvider">Data Provider for the grid</param>
        /// <param name="colIndices">column indexes changed</param>
        /// <param name="rowIndices">row indexes changed</param>
        /// <param name="values">values that were put in</param>
        private void OnCellChanged(Gtk.Sheet.IDataProvider dataProvider, int[] colIndices, int[] rowIndices, string[] values)
        {
            DisconnectEvents();
            try
            {
                foreach (ISubPresenter presenter in presenters)
                    presenter.Refresh();
            }
            catch (Exception exception)
            {
                explorerPresenter.MainPresenter.ShowError(exception);
            }
            finally
            {
                ConnectEvents();
            }
        }

        /// <summary>
        /// Listener for Text change events from a Code Editor
        /// Does not use given parameters, just refreshes the presenters
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="property">The property changed</param>
        /// <param name="lines">The lines it should be given</param>
        private void OnTextChanged(ICodeEditor model, string property, string[] lines)
        {
            DisconnectEvents();
            try
            {
                ChangeProperty command = new ChangeProperty(model, property, lines);
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception exception)
            {
                explorerPresenter.MainPresenter.ShowError(exception);
            }
            finally
            {
                ConnectEvents();
            }
        }

        /// <summary>
        /// Listener for List selection events from a List view
        /// Does nothing unless model is FactorFromFile, in which case the 
        /// code view is updated.
        /// </summary>
        private void OnListSelectionChanged(object sender, EventArgsValue e)
        {
            if (model is FactorFromFile factorFromFile)
            {
                DisconnectEvents();
                try
                {
                    int index = e.Value;
                    SetCode(factorFromFile.GetCommands(index).ToArray());
                }
                catch (Exception exception)
                {
                    explorerPresenter.MainPresenter.ShowError(exception);
                }
                finally
                {
                    ConnectEvents();
                }
            }
        }

        /// <summary>
        /// Add a graph presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddGraph(WidgetPosition position)
        {
            GraphView graphView = view.AddComponent(WidgetType.Graph, position) as GraphView;
            QuadGraphPresenter graphPresenter = new QuadGraphPresenter();
            graphPresenter.Attach(model, graphView, explorerPresenter);
            graphPresenter.Refresh();

            //Check if graph actually has content, hide if not
            if (graphView.Width > 0 && graphView.Height > 0)
            {
                presenters.Add(graphPresenter);
            }
            else
            {
                graphPresenter.Detach();
                view.RemoveComponent(position);
            }
        }

        /// <summary>
        /// Add a grid presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddGrid(WidgetPosition position)
        {
            ViewBase gridContainer = view.AddComponent(WidgetType.Grid, position);
            GridPresenter gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });
            gridPresenter.Refresh();

            presenters.Add(gridPresenter);
        }

        /// <summary>
        /// Add a markdown view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="text">Text to display in this view</param>
        private void AddText(WidgetPosition position, string text)
        {
            view.AddComponent(WidgetType.Text, position);
            view.SetLabelText(text);
        }

        /// <summary>
        /// Add a property presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddProperty(WidgetPosition position)
        {
            PropertyView propertyView = view.AddComponent(WidgetType.Property, position) as PropertyView;
            PropertyPresenter propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            //Check if properties actually has content, hide if not
            if (propertyView.AnyProperties)
            {
                presenters.Add(propertyPresenter);
            }
            else
            {
                propertyPresenter.Detach();
                view.RemoveComponent(position);
            }
        }

        /// <summary>
        /// Add a Editor view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="text">Text to display in this view</param>
        private void AddCode(WidgetPosition position)
        {
            EditorView editorView = view.AddComponent(WidgetType.Code, position) as EditorView;
            EditorPresenter editorPresenter = new EditorPresenter();
            editorPresenter.Attach(model, editorView, explorerPresenter);
            presenters.Add(editorPresenter);
        }

        /// <summary>
        /// Set the text contents of an Editor view
        /// </summary>
        /// <param name="lines"></param>
        private void SetCode(string[] lines)
        {
            foreach(ISubPresenter presenter in presenters)
                if (presenter is EditorPresenter editor)
                    editor.SetCode(lines);
        }

        /// <summary>
        /// Add a List view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="table"></param>
        private void AddList(WidgetPosition position)
        {
            ExperimentView experimentView = view.AddComponent(WidgetType.List, position) as ExperimentView;
            ListPresenter listPresenter = new ListPresenter();
            listPresenter.Attach(model, experimentView, explorerPresenter);
            presenters.Add(listPresenter);
        }

        /// <summary>
        /// Setup a generic layout with grid, graph and properties
        /// </summary>
        private void CreateLayoutGeneric()
        {
            AddGrid(WidgetPosition.BottomLeft);
            AddGraph(WidgetPosition.BottomRight);
            AddProperty(WidgetPosition.TopRight);
        }

        /// <summary>
        /// Create layout for an XY pairs, text, grid and graph
        /// </summary>
        private void CreateLayoutXYPairs()
        {
            DescriptionAttribute descriptionName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;

            XYPairs xypairs = model as XYPairs;
            if (xypairs == null)
                throw new System.Exception($"Model {model.Name} is not an XY Pairs but is trying to use the XY Pairs view layout");
            
            string description = "";
            if (descriptionName != null)
                description = descriptionName.ToString();

            if (!string.IsNullOrEmpty(description))
                AddText(WidgetPosition.TopLeft, description);
            AddGrid(WidgetPosition.BottomLeft);
            AddGraph(WidgetPosition.BottomRight);
        }

        /// <summary>
        /// Create layout for a physical, text, grid and graph
        /// </summary>
        private void CreateLayoutPhysical()
        {
            CreateLayoutGeneric();
            string warnings = "Note: values in red are estimates only and needed for the simulation of soil temperature. Overwrite with local values wherever possible";
            AddText(WidgetPosition.TopLeft, warnings);
            view.OverrideSlider(0.6);
        }

        /// <summary>
        /// Create layout for a waterbalance, grid, graph and properties
        /// </summary>
        private void CreateLayoutWaterBalance()
        {
            CreateLayoutGeneric();
            view.OverrideSlider(0.3);
        }

        /// <summary>
        /// Create layout for a CompositeFactor with code and grid
        /// </summary>
        private void CreateLayoutCompositeFactor()
        {
            AddCode(WidgetPosition.TopLeft);
            AddText(WidgetPosition.TopRight, "Simulation Descriptors:");
            AddGrid(WidgetPosition.BottomRight);
            view.OverrideSlider(0.7);
        }

        /// <summary>
        /// Create layout for a FactorsFromFile, property, text, list and code
        /// </summary>
        private void CreateLayoutFactorFromFile()
        {
            AddProperty(WidgetPosition.TopLeft);
            AddText(WidgetPosition.TopRight, "Commands:");
            AddList(WidgetPosition.BottomLeft);
            AddCode(WidgetPosition.BottomRight);
            view.OverrideSlider(0.6);
        }
    }
}