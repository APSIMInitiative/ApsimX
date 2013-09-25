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
        private CommandHistory CommandHistory;
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Graph = Model as Graph;
            GraphView = View as GraphView;
            this.CommandHistory = CommandHistory;

            GraphView.OnAxisClick += OnAxisClick;
            GraphView.OnPlotClick += OnPlotClick;
            CommandHistory.ModelChanged += OnGraphModelChanged;
            PopulateGraph();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            GraphView.OnAxisClick -= OnAxisClick;
            GraphView.OnPlotClick -= OnPlotClick;
            CommandHistory.ModelChanged -= OnGraphModelChanged;
        }


        private void OnGraphModelChanged(object Model)
        {
            if (Graph.Axes.Length == 2 && 
                (Model == Graph || Model == Graph.Axes[0] || Model == Graph.Axes[1]))
            {
                PopulateGraph();
            }
        }

        private void OnAxisClick(OxyPlot.Axes.AxisPosition AxisPosition)
        {
            AxisPresenter AxisPresenter = new AxisPresenter();
            AxisView A = new AxisView();
            GraphView.ShowEditorPanel(A);
            AxisPresenter.Attach(GetAxis(AxisPosition), A, CommandHistory);
        }

        private void OnPlotClick()
        {
            SeriesPresenter SeriesPresenter = new SeriesPresenter();
            SeriesView SeriesView = new SeriesView();
            GraphView.ShowEditorPanel(SeriesView);
            SeriesPresenter.Attach(Graph, SeriesView, CommandHistory);
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
            PopulateAxis(Axis);
        }

        private void PopulateGraph()
        {
            GraphView.ClearSeries();
            if (Graph.Series != null)
            {
                foreach (Series S in Graph.Series)
                {
                    DataTable Data = Graph.DataStore.GetData(S.SimulationName, S.TableName);
                    if (S.Type == Series.SeriesType.Line)
                        GraphView.CreateLineSeries(Data, S.Title,
                                                   S.X, S.Y,
                                                   OxyPlot.Axes.AxisPosition.Bottom, OxyPlot.Axes.AxisPosition.Left);
                    else if (S.Type == Series.SeriesType.Bar)
                        GraphView.CreateBarSeries(Data, S.Title,
                                                  S.X, S.Y,
                                                  OxyPlot.Axes.AxisPosition.Bottom, OxyPlot.Axes.AxisPosition.Left);
                }
            }
            foreach (Axis A in Graph.Axes)
                PopulateAxis(A);
            GraphView.RefreshGraph();
        }
        private void PopulateAxis(Axis Axis)
        {
            if (Axis.Type == Models.Graph.Axis.AxisType.Bottom)
                GraphView.PopulateAxis(OxyPlot.Axes.AxisPosition.Bottom, Axis.Title);
			else if (Axis.Type == Models.Graph.Axis.AxisType.Left)
                GraphView.PopulateAxis(OxyPlot.Axes.AxisPosition.Left, Axis.Title);
			else if (Axis.Type == Models.Graph.Axis.AxisType.Right)
                GraphView.PopulateAxis(OxyPlot.Axes.AxisPosition.Right, Axis.Title);
			else if (Axis.Type == Models.Graph.Axis.AxisType.Top)
                GraphView.PopulateAxis(OxyPlot.Axes.AxisPosition.Top, Axis.Title);
        }


    }
}
