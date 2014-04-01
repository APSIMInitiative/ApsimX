using System;
using UserInterface.Views;
using Models.Graph;
using System.Data;
using Models.Core;

namespace UserInterface.Presenters
{
    class GraphPresenter : IPresenter
    {
        private IGraphView GraphView;
        private Graph Graph;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Graph = Model as Graph;
            GraphView = View as GraphView;
            ExplorerPresenter = explorerPresenter;

            GraphView.OnAxisClick += OnAxisClick;
            GraphView.OnPlotClick += OnPlotClick;
            ExplorerPresenter.CommandHistory.ModelChanged += OnGraphModelChanged;
            GraphView.DrawGraph(Graph);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            GraphView.OnAxisClick -= OnAxisClick;
            GraphView.OnPlotClick -= OnPlotClick;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnGraphModelChanged;
        }


        private void OnGraphModelChanged(object Model)
        {
            if (Graph.Axes.Count >= 2 && 
                (Model == Graph || Model == Graph.Axes[0] || Model == Graph.Axes[1]))
            {
                GraphView.DrawGraph(Graph);
            }
        }

        private void OnAxisClick(OxyPlot.Axes.AxisPosition AxisPosition)
        {
            AxisPresenter AxisPresenter = new AxisPresenter();
            AxisView A = new AxisView();
            GraphView.ShowEditorPanel(A);
            AxisPresenter.Attach(GetAxis(AxisPosition), A, ExplorerPresenter);
        }

        private void OnPlotClick()
        {
            SeriesPresenter SeriesPresenter = new SeriesPresenter();
            SeriesView SeriesView = new SeriesView();
            GraphView.ShowEditorPanel(SeriesView);
            SeriesPresenter.Attach(Graph, SeriesView, ExplorerPresenter);
        }

        private object GetAxis(OxyPlot.Axes.AxisPosition AxisType)
        {
            foreach (Axis A in Graph.Axes)
                if (A.Type.ToString() == AxisType.ToString())
                    return A;
            throw new Exception("Cannot find axis with type: " + AxisType.ToString());
        }

        private void OnAxisChanged(Axis Axis)
        {
            GraphView.DrawGraph(Graph);
        }


    }
}
