namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using Models;
    using Views;

    /// <summary>
    /// This presenter connects an instance of a Model.Graph.Axis with a 
    /// UserInterface.Views.AxisView
    /// </summary>
    public class LegendPresenter : IPresenter
    {
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

            // Trap events from the view.
            this.view.OnPositionChanged += this.OnTitleChanged;

            // Tell the view to populate the axis.
            this.PopulateView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            // Trap change event from the model.
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            // Trap events from the view.
            this.view.OnPositionChanged -= this.OnTitleChanged;

            this.view.DisabledSeriesChanged -= this.OnDisabledSeriesChanged;
        }

        /// <summary>Populates the view.</summary>
        private void PopulateView()
        {
            List<string> values = new List<string>();
            foreach (Graph.LegendPositionType value in Enum.GetValues(typeof(Graph.LegendPositionType)))
            {
                values.Add(value.ToString());
            }

            this.view.Populate(this.graph.LegendPosition.ToString(), values.ToArray());

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
            {
                this.PopulateView();
            }
        }

        /// <summary>
        /// The user has changed the title field on the form. Need to tell the model this via
        /// executing a command.
        /// </summary>
        /// <param name="newText">The text for the title</param>
        private void OnTitleChanged(string newText)
        {
            try
            {
                Graph.LegendPositionType legendPosition;
                Enum.TryParse<Graph.LegendPositionType>(newText, out legendPosition);
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.graph, "LegendPosition", legendPosition));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }
}
