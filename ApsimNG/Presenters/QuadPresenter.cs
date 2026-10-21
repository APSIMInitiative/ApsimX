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
using Models.PreSimulationTools;

namespace UserInterface.Presenters
{
    /// <summary>A generic presenter displaying four boxes of info with grid, graph, text and properties</summary>
    public class QuadPresenter : IPresenter
    {
        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter _explorerPresenter;

        /// <summary>The base view.</summary>
        private QuadView _view = null;

        /// <summary>The model.</summary>
        private IModel _model;

        private bool _hasSuccessfullyBuiltPresenters = false;

        /// <summary>Sub-presenters that are added to this presenter</summary>
        private List<ISubPresenter> _presenters;

        /// <summary>Default constructor</summary>
        public QuadPresenter() {}

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="v">View to work with, must be a QuadView</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            _model = model as IModel;
            _view = v as QuadView;
            _explorerPresenter = explorerPresenter;
            _presenters = new List<ISubPresenter>();

            if (_view == null)
                throw new System.Exception("QuadPresenter only works with a QuadView");

            Refresh();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            DestroyPresenters();
            _view.Dispose();
        }

        /// <summary>Refresh this presenter and all sub presenters</summary>
        public void Refresh()
        {
            DisconnectEvents();

            if (!_hasSuccessfullyBuiltPresenters)
                CreatePresenters();

            List<Exception> errors = new List<Exception>();
            if (_model is FactorFromFile factorFromFile)
            {
                try { factorFromFile.GetCompositeFactors(); }
                catch (Exception exception) { errors.Add(exception); }
            }

            foreach (ISubPresenter presenter in _presenters)
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
                _view.Refresh();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            if (errors.Count > 0)
                _explorerPresenter.MainPresenter.ShowError(errors, overwrite:true);

            ConnectEvents();
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            foreach (ISubPresenter presenter in _presenters)
            {
                presenter.ConnectEvents();
                if (presenter is GridPresenter grid)
                    grid.CellChanged += OnCellChanged;
                if (presenter is EditorPresenter editor)
                    editor.TextChanged += OnTextChanged;
                if (presenter is ListPresenter list)
                    list.SelectionChanged += OnListSelectionChanged;
                if (presenter is UpdatePresenter update)
                    update.Click += OnUpdateClick;
            }

            _explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
            foreach (ISubPresenter presenter in _presenters)
            {
                presenter.DisconnectEvents();
                if (presenter is GridPresenter grid)
                    grid.CellChanged -= OnCellChanged;
                if (presenter is EditorPresenter editor)
                    editor.TextChanged -= OnTextChanged;
                if (presenter is ListPresenter list)
                    list.SelectionChanged -= OnListSelectionChanged;
                if (presenter is UpdatePresenter update)
                    update.Click -= OnUpdateClick;
            }
            _explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
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

                if (_model is XYPairs)
                    CreateLayoutXYPairs();
                else if (_model is Physical)
                    CreateLayoutPhysical();
                else if (_model is WaterBalance)
                    CreateLayoutWaterBalance();
                else if (_model is CompositeFactor)
                    CreateLayoutCompositeFactor();
                else if (_model is FactorFromFile)
                    CreateLayoutFactorFromFile();
                else if (_model is Virtual)
                    CreateLayoutVirtual();
                else if (_model is FrostHeatDamageFunctions)
                    CreateLayoutFrostHeatDamageFunctions();
                else
                    CreateLayoutGeneric();
                _hasSuccessfullyBuiltPresenters = true;
            }
            catch (Exception exception)
            {
                _explorerPresenter.MainPresenter.ShowError(exception, overwrite:true);
            }
        }

        /// <summary>
        /// Destroys all the created presenters by detatching them
        /// </summary>
        private void DestroyPresenters()
        {
            foreach (ISubPresenter presenter in _presenters)
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
            _model = changedModel as IModel;
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
                foreach (ISubPresenter presenter in _presenters)
                    presenter.Refresh();
            }
            catch (Exception exception)
            {
                _explorerPresenter.MainPresenter.ShowError(exception);
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
                _explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception exception)
            {
                _explorerPresenter.MainPresenter.ShowError(exception);
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
            if (_model is FactorFromFile factorFromFile)
            {
                DisconnectEvents();
                try
                {
                    int index = e.Value;
                    SetCode(factorFromFile.GetCommands(index).ToArray());
                }
                catch (Exception exception)
                {
                    _explorerPresenter.MainPresenter.ShowError(exception);
                }
                finally
                {
                    ConnectEvents();
                }
            }
        }

        /// <summary>
        /// Listener for click events from an UpdatePresenter
        /// Tells the view to refresh in case of changes.
        /// </summary>
        private void OnUpdateClick(object sender, EventArgsValue e)
        {
            Refresh();
        }

        /// <summary>
        /// Add a graph presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddGraph(WidgetPosition position)
        {
            GraphView graphView = _view.AddComponent(WidgetType.Graph, position) as GraphView;
            QuadGraphPresenter graphPresenter = new QuadGraphPresenter();
            graphPresenter.Attach(_model, graphView, _explorerPresenter);
            graphPresenter.Refresh();

            //Check if graph actually has content, hide if not
            if (graphView.Width > 0 && graphView.Height > 0)
            {
                _presenters.Add(graphPresenter);
            }
            else
            {
                graphPresenter.Detach();
                _view.RemoveComponent(position);
            }
        }

        /// <summary>
        /// Add a grid presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddGrid(WidgetPosition position)
        {
            ViewBase gridContainer = _view.AddComponent(WidgetType.Grid, position);
            GridPresenter gridPresenter = new GridPresenter();
            gridPresenter.Attach(_model, gridContainer, _explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });
            gridPresenter.Refresh();

            _presenters.Add(gridPresenter);
        }

        /// <summary>
        /// Add a markdown view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="text">Text to display in this view</param>
        private void AddText(WidgetPosition position, string text)
        {
            _view.AddComponent(WidgetType.Text, position);
            _view.SetLabelText(text);
        }

        /// <summary>
        /// Add a property presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddProperty(WidgetPosition position)
        {
            PropertyView propertyView = _view.AddComponent(WidgetType.Property, position) as PropertyView;
            PropertyPresenter propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(_model, propertyView, _explorerPresenter);

            //Check if properties actually has content, hide if not
            if (propertyView.AnyProperties)
            {
                _presenters.Add(propertyPresenter);
            }
            else
            {
                propertyPresenter.Detach();
                _view.RemoveComponent(position);
            }
        }

        /// <summary>
        /// Add a Editor view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="text">Text to display in this view</param>
        /// <param name="readOnly">Whether the text in this view can be changed by the user</param>
        private void AddCode(WidgetPosition position, bool readOnly)
        {
            EditorView editorView = _view.AddComponent(WidgetType.Code, position) as EditorView;
            editorView.ReadOnly = readOnly;
            EditorPresenter editorPresenter = new EditorPresenter();
            editorPresenter.Attach(_model, editorView, _explorerPresenter);
            _presenters.Add(editorPresenter);
        }

        /// <summary>
        /// Set the text contents of an Editor view
        /// </summary>
        /// <param name="lines"></param>
        private void SetCode(string[] lines)
        {
            foreach(ISubPresenter presenter in _presenters)
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
            ExperimentView experimentView = _view.AddComponent(WidgetType.List, position) as ExperimentView;
            ListPresenter listPresenter = new ListPresenter();
            listPresenter.Attach(_model, experimentView, _explorerPresenter);
            _presenters.Add(listPresenter);
        }

        /// <summary>
        /// Add a List view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="table"></param>
        private void AddUpdateButton(WidgetPosition position)
        {
            ButtonView buttonView = _view.AddComponent(WidgetType.Button, position) as ButtonView;
            UpdatePresenter updatePresenter = new UpdatePresenter();
            updatePresenter.Attach(_model, buttonView, _explorerPresenter);
            _presenters.Add(updatePresenter);
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
            DescriptionAttribute descriptionName = ReflectionUtilities.GetAttribute(_model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;

            XYPairs xypairs = _model as XYPairs;
            if (xypairs == null)
                throw new System.Exception($"Model {_model.Name} is not an XY Pairs but is trying to use the XY Pairs view layout");
            
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
            _view.OverrideSlider(0.6);
        }

        /// <summary>
        /// Create layout for a waterbalance, grid, graph and properties
        /// </summary>
        private void CreateLayoutWaterBalance()
        {
            CreateLayoutGeneric();
            _view.OverrideSlider(0.3);
        }

        /// <summary>
        /// Create layout for a CompositeFactor with code and grid
        /// </summary>
        private void CreateLayoutCompositeFactor()
        {
            AddCode(WidgetPosition.TopLeft, false);
            AddText(WidgetPosition.TopRight, "Simulation Descriptors:");
            AddGrid(WidgetPosition.BottomRight);
            _view.OverrideSlider(0.7);
        }

        /// <summary>
        /// Create layout for a FactorsFromFile, property, text, list and code
        /// </summary>
        private void CreateLayoutFactorFromFile()
        {
            AddProperty(WidgetPosition.TopLeft);
            AddUpdateButton(WidgetPosition.TopRight);
            AddList(WidgetPosition.BottomLeft);
            AddCode(WidgetPosition.BottomRight, true);
            _view.OverrideSlider(0.6);
        }

        /// <summary>
        /// Create layout for a Virtual, property and code
        /// </summary>
        private void CreateLayoutVirtual()
        {
            AddUpdateButton(WidgetPosition.TopRight);
            AddProperty(WidgetPosition.TopLeft);
            AddCode(WidgetPosition.BottomRight, true);
            _view.OverrideSlider(0.3);
        }

        /// <summary>
        /// Create layout for a FrostHeatDamageFunctions, property and text
        /// </summary>
        private void CreateLayoutFrostHeatDamageFunctions()
        {
            AddProperty(WidgetPosition.TopLeft);
            AddText(WidgetPosition.TopRight, FrostHeatDamageFunctions.HelpText);
        }
    }
}