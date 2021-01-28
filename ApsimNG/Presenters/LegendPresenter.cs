namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Commands;
    using Models;
    using Models.Core;
    using Views;

    using Orientation = Models.Graph.LegendOrientationType;
    using Position = Models.Graph.LegendPositionType;

    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a 
    /// UserInterface.Views.AxisView
    /// </summary>
    public class LegendPresenter : IPresenter
    {

        private static readonly string[] positions = Enum.GetValues(typeof(Position))
                                    .Cast<Enum>()
                                    .Select(e => VariableProperty.GetEnumDescription(e)).ToArray();
        private static readonly string[] orientations = Enum.GetValues(typeof(Orientation))
                                    .Cast<Enum>()
                                    .Select(e => VariableProperty.GetEnumDescription(e))
                                    .ToArray();

        /// <summary>
        /// Graph object
        /// </summary>
        private Graph graph;

        /// <summary>
        /// The legend view
        /// </summary>
        private ILegendView view;

        /// <summary>
        /// The explorer object
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The graph presenter object
        /// </summary>
        private GraphPresenter graphPresenter;

        /// <summary>Initializes a new instance of the <see cref="LegendPresenter"/> class.</summary>
        /// <param name="graphPresenter">The graph presenter.</param>
        public LegendPresenter(GraphPresenter graphPresenter)
        {
            this.graphPresenter = graphPresenter;
        }

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The model to attach</param>
        /// <param name="view">The View</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.graph = model as Graph;
            this.view = view as ILegendView;
            this.explorerPresenter = explorerPresenter;

            // Trap change event from the model.
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            // Tell the view to populate the axis.
            this.PopulateView();
        
            // Trap events from the view.
            this.view.LegendInsideGraphChanged += this.OnLegendInsideGraphChanged;
            this.view.OrientationDropDown.Changed += OnOrientationChanged;
            this.view.PositionDropDown.Changed += OnPositionChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            // Trap change event from the model.
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            // Trap events from the view.
            this.view.DisabledSeriesChanged -= this.OnDisabledSeriesChanged;
            this.view.LegendInsideGraphChanged -= this.OnLegendInsideGraphChanged;
        }

        /// <summary>Populates the view.</summary>
        private void PopulateView()
        {
            view.OrientationDropDown.Values = orientations;
            view.PositionDropDown.Values = positions;
            view.OrientationDropDown.SelectedValue = graph.LegendOrientation.ToString();
            view.PositionDropDown.SelectedValue = graph.LegendPosition.ToString();

            List<string> seriesNames = this.GetSeriesNames();
            this.view.SetSeriesNames(seriesNames.ToArray());
            if (graph.DisabledSeries != null)
                this.view.SetDisabledSeriesNames(this.graph.DisabledSeries.ToArray());

            this.view.DisabledSeriesChanged += this.OnDisabledSeriesChanged;
        }

        /// <summary>
        /// Get the series names
        /// </summary>
        /// <returns>A list of the series names</returns>
        private List<string> GetSeriesNames()
        {
            List<string> seriesNames = new List<string>();
            foreach (string seriesName in this.graphPresenter.GetSeriesNames())
            {
                if (!seriesNames.Contains(seriesName))
                {
                    seriesNames.Add(seriesName);
                }
            }

            return seriesNames;
        }

        /// <summary>
        /// Called when the legend position is changed by the user.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnPositionChanged(object sender, EventArgs e)
        {
            string text = view.PositionDropDown.SelectedValue;
            if (Enum.TryParse<Position>(text, out Position position))
            {
                ICommand changePosition = new ChangeProperty(graph, nameof(graph.LegendPosition), position);
                explorerPresenter.CommandHistory.Add(changePosition);
            }
        }

        /// <summary>
        /// Called when the legend orientation is changed by the user.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnOrientationChanged(object sender, EventArgs args)
        {
            string text = view.OrientationDropDown.SelectedValue;
            if (Enum.TryParse<Orientation>(text, out Orientation orientation))
            {
                ICommand changeOrientation = new ChangeProperty(graph, nameof(graph.LegendOrientation), orientation);
                explorerPresenter.CommandHistory.Add(changeOrientation);
            }
        }

        /// <summary>Called when user changes a disabled series.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnDisabledSeriesChanged(object sender, EventArgs e)
        {
            try
            {
                this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

                List<string> disabledSeries = new List<string>();
                disabledSeries.AddRange(this.view.GetDisabledSeriesNames());
                if (disabledSeries.Count < this.GetSeriesNames().Count)
                {
                    this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.graph, "DisabledSeries", disabledSeries));
                }

                this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The 'Model' has changed so we need to update the 'View'. Usually the result of an 'Undo' or 'Redo'
        /// </summary>
        /// <param name="model">The model object</param>
        private void OnModelChanged(object model)
        {
            if (model == this.graph)
                PopulateView();
        }

        /// <summary>
        /// The user has toggled the check button which controls whether the legend is to be displayed
        /// inside the graph area or not. We need to apply the change.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnLegendInsideGraphChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(graph, nameof(graph.LegendOutsideGraph), !view.LegendInsideGraph));
        }
    }
}
